using Microsoft.Win32;
using MahApps.Metro.IconPacks;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Управление_складом.Themes;
using УправлениеСкладом.Class;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace УправлениеСкладом
{
	public partial class MessengerWindow : Window, INotifyPropertyChanged, IThemeable
	{
		// Строка подключения
		private string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True;TrustServerCertificate=True";

		// Информация о текущем пользователе
		private int currentUserId;
		private string currentUserName = ""; // Логин пользователя

		// Выбранный контакт
		private int selectedContactId = 0;
		private string selectedContactName;

		// Словарь для отслеживания ID сообщений и их UI элементов
		private Dictionary<int, FrameworkElement> messageElements = new Dictionary<int, FrameworkElement>();
		
		// Флаг редактирования сообщения
		private bool isEditingMessage = false;
		private int editingMessageId = 0;
		
		// ID сообщения, на которое отвечаем
		private int replyToMessageId = 0;
		private string replyToMessageText = "";
		private int replyToSenderId = 0;
		
		// Timer для автоматического обновления сообщений
		private DispatcherTimer updateTimer;
		
		// Менеджеры для разных компонентов
		private MessageManager messageManager;
		private ContactManager contactManager;
		private ConversationManager conversationManager;
		private SearchManager searchManager;
		private MessengerUIManager uiManager;
		private SearchService searchService;
		private UserManager userManager;
		private ClipboardManager clipboardManager;
		private SearchUIManager searchUIManager;
		
		// Переменная для текущего вложения
		private MessageAttachment currentAttachment;

		// Менеджер удаления сообщений
		private MessageDeleteManager messageDeleteManager;
		
		// Менеджер превью вложений
		private AttachmentPreviewManager attachmentPreviewManager;
		
		// Событие изменения свойства
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		// Параметризованный конструктор
		public MessengerWindow(int userId)
		{
			InitializeComponent();
			
			// Сохраняем ID пользователя
			currentUserId = userId;
			
			// Устанавливаем значения по умолчанию
			ChatTitle.Text = "Выберите контакт";
			
			// Загружаем информацию о текущем пользователе
			LoadCurrentUserInfo();
			
			// Инициализируем менеджеры
			messageManager = new MessageManager(connectionString, currentUserId, currentUserName);
			contactManager = new ContactManager(connectionString, currentUserId, ContactsListBox);
			conversationManager = new ConversationManager(connectionString, currentUserId, MessagesPanel, messageElements);
			searchManager = new SearchManager(connectionString, currentUserId);
			messageDeleteManager = new MessageDeleteManager(messageManager, messageElements, MessagesPanel);
			attachmentPreviewManager = new AttachmentPreviewManager(AttachmentPreviewContent, AttachmentPreviewPanel);
			
			// Инициализируем менеджер пользователей
			userManager = new UserManager(connectionString, currentUserId);
			
			// Инициализируем UI менеджер
			uiManager = new MessengerUIManager(
				this,
				MessagesPanel,
				MessagesScrollViewer,
				SearchPanelBorder,
				SearchMessageTextBox,
				SearchResultsCountText,
				SearchResultsList,
				SuggestedMessagesPanel,
				MessageTextBox,
				ChatTitle,
				ChatUserLogin
			);
			
			// Инициализируем сервис поиска
			searchService = new SearchService(searchManager, uiManager, messageElements);
			
			// Инициализируем менеджер поиска UI
			searchUIManager = new SearchUIManager(
				this,
				searchService,
				SearchMessageTextBox,
				SearchPanelBorder,
				SearchResultsCountText,
				SearchResultsList,
				uiManager
			);
			
			// Инициализируем менеджер буфера обмена
			clipboardManager = new ClipboardManager(messageManager, MessageTextBox);
			
			// Устанавливаем заголовок окна с именем пользователя
			this.Title = $"Мессенджер - {currentUserName}";
			
			// Устанавливаем контекст данных
			DataContext = this;
			
			// Явно устанавливаем обработчик события выбора контакта
			ContactsListBox.SelectionChanged += ContactsListBox_SelectionChanged;
			
			// Обработчик нажатия Ctrl+V для вставки изображений из буфера обмена
			MessageTextBox.PreviewKeyDown += MessageTextBox_PreviewKeyDown;
			
			// Загружаем список контактов
			contactManager.LoadContacts();
			
			// Настраиваем автоматическое обновление
			SetupAutoUpdate();
			
			// Устанавливаем правильные ресурсы темы при загрузке
			this.Loaded -= ThemeResourcesLoaded; // Удаляем, если уже был добавлен
			this.Loaded += ThemeResourcesLoaded;
			
			// Обновляем элементы интерфейса в зависимости от темы
			ApplyTheme();
			
			// Настраиваем предложенные сообщения
			uiManager.SetupSuggestedMessages();
			
			// Настраиваем таймер автоудаления старых сообщений
			SetupAutoDeleteTimer();
			
			// Прокрутка сообщений при изменении
			MessagesPanel.SizeChanged += (s, e) => uiManager.ScrollToBottom();
			
			// Настройка горячих клавиш для поиска
			this.KeyDown += MessengerWindow_KeyDown;
		}
		
		// Обработчик горячих клавиш
		private void MessengerWindow_KeyDown(object sender, KeyEventArgs e)
		{
			// Ctrl+F для открытия поиска
			if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
			{
				e.Handled = true;
				uiManager.ToggleSearchPanel(true);
			}
			// Esc для закрытия поиска
			else if (e.Key == Key.Escape)
			{
				e.Handled = true;
				uiManager.ToggleSearchPanel(false);
			}
		}

		// Загрузка информации о текущем пользователе
		private void LoadCurrentUserInfo()
		{
			try
			{
				// Получаем логин пользователя из глобальных свойств приложения или из базы данных
				string savedUsername = Application.Current.Properties["CurrentUsername"]?.ToString();
				
				if (!string.IsNullOrEmpty(savedUsername))
				{
					using (SqlConnection connection = new SqlConnection(connectionString))
					{
						connection.Open();
						string query = "SELECT ИмяПользователя FROM Пользователи WHERE ПользовательID = @userId";
						
						// Если есть сохраненный логин, проверяем, соответствует ли он ID пользователя
						if (!string.IsNullOrEmpty(savedUsername))
						{
							query = "SELECT ИмяПользователя FROM Пользователи WHERE ПользовательID = @userId AND ИмяПользователя = @username";
						}
						
						using (SqlCommand command = new SqlCommand(query, connection))
						{
							command.Parameters.AddWithValue("@userId", currentUserId);
							
							if (!string.IsNullOrEmpty(savedUsername))
							{
								command.Parameters.AddWithValue("@username", savedUsername);
							}
							
							var result = command.ExecuteScalar();
							
							if (result != null)
							{
								currentUserName = result.ToString();
							}
							else if (!string.IsNullOrEmpty(savedUsername))
							{
								// Если логин не соответствует ID, получаем логин по ID
								SqlCommand newCommand = new SqlCommand("SELECT ИмяПользователя FROM Пользователи WHERE ПользовательID = @userId", connection);
								newCommand.Parameters.AddWithValue("@userId", currentUserId);
								result = newCommand.ExecuteScalar();
								
								if (result != null)
								{
									currentUserName = result.ToString();
								}
							}
						}
					}
				}
				else
				{
					// Если нет сохраненного логина, получаем его из базы данных
					using (SqlConnection connection = new SqlConnection(connectionString))
					{
						connection.Open();
						string query = "SELECT ИмяПользователя FROM Пользователи WHERE ПользовательID = @userId";
						
						using (SqlCommand command = new SqlCommand(query, connection))
						{
							command.Parameters.AddWithValue("@userId", currentUserId);
							var result = command.ExecuteScalar();
							
							if (result != null)
							{
								currentUserName = result.ToString();
							}
							else
							{
								currentUserName = "Пользователь #" + currentUserId;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при загрузке информации о пользователе: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		// Настройка автоматического обновления сообщений
		private void SetupAutoUpdate()
		{
			// Настраиваем таймер обновления для проверки новых сообщений
			updateTimer = new DispatcherTimer();
			updateTimer.Interval = TimeSpan.FromSeconds(5); // Проверка каждые 5 секунд
			updateTimer.Tick += (s, e) =>
			{
				// Обновляем список контактов для проверки новых сообщений
				contactManager.LoadContacts();
				
				// Если выбран контакт, обновляем переписку без повторных анимаций
				if (selectedContactId > 0)
				{
					conversationManager.LoadConversationWithoutAnimation(selectedContactId);
				}
			};
			updateTimer.Start();
		}

		// Настройка таймера автоудаления старых сообщений
		private void SetupAutoDeleteTimer()
		{
			// Таймер для автоматического удаления сообщений старше N дней
			DispatcherTimer autoDeleteTimer = new DispatcherTimer();
			autoDeleteTimer.Interval = TimeSpan.FromHours(24); // Проверяем раз в день
			autoDeleteTimer.Tick += (s, e) =>
			{
				int deletedCount = userManager.DeleteOldMessages(30); // Удаляем сообщения старше 30 дней
				
				if (deletedCount > 0 && selectedContactId > 0)
				{
					// Перезагружаем текущую переписку, если сообщения были удалены
					conversationManager.LoadConversation(selectedContactId);
				}
			};
			autoDeleteTimer.Start();
			
			// Запускаем первичную проверку при старте
			userManager.DeleteOldMessages(30);
		}

		// Выбор контакта
		private void ContactsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ContactsListBox.SelectedItem is ContactInfo info)
			{
				ChatTitle.Text = info.Login;
				ChatUserLogin.Text = $"Роль: {info.Role}";
				selectedContactId = info.UserId;
				selectedContactName = info.Login;

				// Обновляем ID выбранного контакта в сервисе поиска
				searchService.SetSelectedContactId(selectedContactId);
				
				// Обновляем ID выбранного контакта в менеджере буфера обмена
				clipboardManager.SetSelectedContactId(selectedContactId);

				// Сбрасываем непрочитанные
				contactManager.ResetUnreadCount(info.UserId);

				// Обнуляем флаги редактирования/ответа
				isEditingMessage = false;
				editingMessageId = 0;
				replyToMessageId = 0;
				replyToMessageText = "";
				replyToSenderId = 0;

				// Загружаем переписку
				conversationManager.LoadConversation(selectedContactId);
				
				// Помечаем сообщения как прочитанные
				conversationManager.MarkMessagesAsRead(selectedContactId);
			}
		}

		// Обновление иконки темы
		public void UpdateThemeIcon()
		{
			if (ThemeIcon != null)
			{
				// Устанавливаем иконку в зависимости от текущей темы
				ThemeIcon.Kind = ThemeManager.IsDarkTheme ? 
					PackIconMaterialKind.WeatherNight : 
					PackIconMaterialKind.WeatherSunny;
				
				// Обновляем подсказку
				var themeButton = ThemeIcon.Parent as FrameworkElement;
				if (themeButton != null)
				{
					themeButton.ToolTip = ThemeManager.IsDarkTheme ? 
						"Переключить на светлую тему" : 
						"Переключить на темную тему";
				}
			}
		}

		// Обработчик изменения текста поиска
		private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			contactManager.LoadContacts(SearchTextBox.Text);
		}

		// Обработчик нажатия Enter в поле ввода
		private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
			{
				e.Handled = true; // Предотвращаем добавление новой строки
				SendMessage_Click(sender, e);
			}
			else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Shift)
			{
				// Shift+Enter добавляет новую строку, ничего не делаем
			}
		}

		// Отправка сообщения
		private async void SendMessage_Click(object sender, RoutedEventArgs e)
		{
			if (selectedContactId <= 0)
			{
				MessageBox.Show("Пожалуйста, выберите контакт для отправки сообщения.");
				return;
			}

			string messageText = MessageTextBox?.Text.Trim() ?? string.Empty;
			
			// Проверяем, что сообщение не пустое, если нет вложения
			MessageAttachment attachmentToSend = attachmentPreviewManager.GetCurrentAttachment();
			if (string.IsNullOrWhiteSpace(messageText) && attachmentToSend == null)
			{
				return; // Не отправляем пустое сообщение без вложений
			}
			
			try
			{
				// Кнопка отправки и текстовое поле могут быть временно недоступны
				bool sendButtonWasEnabled = false;
				
				// Находим кнопку отправки
				Button sendButton = null;
				foreach (var element in GetLogicalChildrenByType<Button>(this))
				{
					if (element.ToolTip?.ToString() == "Отправить сообщение")
					{
						sendButton = element;
						sendButtonWasEnabled = sendButton.IsEnabled;
						sendButton.IsEnabled = false; // Блокируем кнопку на время отправки
						break;
					}
				}
				
				// Сохраняем текст сообщения
				string textToSend = messageText;
				
				// Очищаем текстовое поле
				if (MessageTextBox != null)
				{
					MessageTextBox.Clear();
					MessageTextBox.IsEnabled = false; // Блокируем поле на время отправки
				}
				
				// Сохраняем ссылку на вложение
				var attachmentForSending = attachmentToSend;
				
				// Скрываем предпросмотр вложения
				if (attachmentToSend != null)
				{
					attachmentPreviewManager.HidePreview();
					currentAttachment = null;
				}
				
				// Сохраняем флаги редактирования
				bool isEditMode = isEditingMessage;
				int editingId = editingMessageId;
				
				// Сбрасываем флаги редактирования и ответа
				if (isEditingMessage)
				{
					isEditingMessage = false;
					editingMessageId = 0;
				}
				
				replyToMessageId = 0;
				replyToMessageText = "";
				replyToSenderId = 0;
				
				int messageId = 0;
				
				try
				{
					if (isEditMode)
					{
						// Режим редактирования
						bool success = await messageManager.EditMessageAsync(editingId, textToSend);
						if (success)
						{
							// Обновляем сообщение в интерфейсе
							if (messageElements.TryGetValue(editingId, out var element))
							{
								if (element is StackPanel panel)
								{
									Border messageBubble = UIMessageFactory.FindFirstBorderExplicit(panel);
									if (messageBubble != null && messageBubble.Child is StackPanel contentPanel)
									{
										TextBlock messageBlock = UIMessageFactory.FindTextBlock(contentPanel);
										if (messageBlock != null)
										{
											messageBlock.Text = textToSend;
											uiManager.AnimateMessageUpdate(messageBubble);
										}
									}
								}
							}
						}
					}
					else
					{
						// Режим отправки нового сообщения
						if (attachmentForSending != null)
						{
							// Отправляем сообщение с вложением
							messageId = await messageManager.SendMessageWithAttachmentAsync(selectedContactId, textToSend, attachmentForSending);
						}
						else
						{
							// Отправляем текстовое сообщение
							messageId = await messageManager.SendTextMessageAsync(selectedContactId, textToSend);
						}
						
						// Если сообщение успешно отправлено, обновляем чат
						if (messageId > 0)
						{
							// Обновляем переписку через менеджер без полной перезагрузки
							conversationManager.LoadConversationWithoutAnimation(selectedContactId);
						}
					}
				}
				finally
				{
					// Восстанавливаем доступность кнопки и поля ввода
					if (sendButton != null)
					{
						sendButton.IsEnabled = sendButtonWasEnabled;
					}
					
					if (MessageTextBox != null)
					{
						MessageTextBox.IsEnabled = true;
						MessageTextBox.Focus();
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при отправке сообщения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				
				// Восстанавливаем текст в поле ввода в случае ошибки
				if (MessageTextBox != null && !string.IsNullOrEmpty(messageText))
				{
					MessageTextBox.Text = messageText;
					MessageTextBox.SelectionStart = MessageTextBox.Text.Length;
					MessageTextBox.IsEnabled = true;
				}
			}
		}

		// Отправка медиафайла
		private void AttachButton_Click(object sender, RoutedEventArgs e)
		{
			if (selectedContactId == 0)
			{
				MessageBox.Show("Сначала выберите контакт.");
				return;
			}

			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "Все поддерживаемые файлы|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.mp4;*.avi;*.mp3;*.wav;*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.zip;*.rar|" +
						"Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif|" +
						"Видео|*.mp4;*.avi|" +
						"Аудио|*.mp3;*.wav|" +
						"Документы|*.pdf;*.doc;*.docx;*.xls;*.xlsx|" +
						"Архивы|*.zip;*.rar";

			if (ofd.ShowDialog() == true)
			{
				try
				{
					byte[] fileBytes = File.ReadAllBytes(ofd.FileName);
					string ext = Path.GetExtension(ofd.FileName).ToLower();
					
					// Определяем тип файла
					AttachmentType attachmentType;
					if (new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" }.Contains(ext))
						attachmentType = AttachmentType.Image;
					else if (new[] { ".mp4", ".avi" }.Contains(ext))
						attachmentType = AttachmentType.Video;
					else if (new[] { ".mp3", ".wav" }.Contains(ext))
						attachmentType = AttachmentType.Audio;
					else if (new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx" }.Contains(ext))
						attachmentType = AttachmentType.Document;
					else
						attachmentType = AttachmentType.Other;

					// Создаем объект вложения
					currentAttachment = new MessageAttachment
					{
						Type = attachmentType,
						FileName = Path.GetFileName(ofd.FileName),
						FileSize = fileBytes.Length,
						Content = fileBytes,
						FileType = ext
					};

					// Создаем миниатюру для изображений
					if (attachmentType == AttachmentType.Image)
					{
						currentAttachment.CreateThumbnail();
					}

					// Показываем превью с помощью менеджера
					ShowAttachmentPreview();
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}");
				}
			}
		}

		// Метод для показа превью вложения
		private void ShowAttachmentPreview()
		{
			if (currentAttachment != null)
			{
				attachmentPreviewManager.ShowPreview(currentAttachment);
				
				// Скрываем панель предложенных сообщений когда есть вложение
				if (SuggestedMessagesPanel != null)
					SuggestedMessagesPanel.Visibility = Visibility.Collapsed;
			}
		}

		// Метод для удаления вложения
		private void RemoveAttachment_Click(object sender, RoutedEventArgs e)
		{
			currentAttachment = null;
			attachmentPreviewManager.HidePreview();
			
			// Показываем панель предложенных сообщений когда нет вложения
			if (SuggestedMessagesPanel != null)
				SuggestedMessagesPanel.Visibility = Visibility.Visible;
		}

		// Обработчик клика по чипу (подсказке)
		private void Suggestion_Click(object sender, MouseButtonEventArgs e)
		{
			// Получаем текст предложенного сообщения
			if (sender is Border border && border.Child is TextBlock textBlock)
			{
				MessageTextBox.Text = textBlock.Text;
				SuggestedMessagesPanel.Visibility = Visibility.Collapsed;
				MessageTextBox.Focus();
				MessageTextBox.SelectionStart = MessageTextBox.Text.Length;
			}
		}

		private void MessageTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (SuggestedMessagesPanel.Visibility == Visibility.Visible && !string.IsNullOrEmpty(MessageTextBox.Text))
			{
				SuggestedMessagesPanel.Visibility = Visibility.Collapsed;
			}
		}

		// Перемещение окна
		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}

		// Закрытие окна
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			// Останавливаем таймер обновления
			if (updateTimer != null)
			{
				updateTimer.Stop();
			}
			Close();
		}
		
		// Переключение темы
		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				// Переключаем тему через стандартный менеджер
				ThemeManager.ToggleTheme();
				
				// Обновляем иконку темы
				UpdateThemeIcon();
				
				// Определяем текущую тему
				bool isDarkTheme = ThemeManager.IsDarkTheme;
				
				// Получаем словарь нужной темы
				var themeDictKey = isDarkTheme ? "DarkThemeDict" : "LightThemeDict";
				var themeDict = Resources[themeDictKey] as ResourceDictionary;
				
				if (themeDict != null)
				{
					// Обновляем все ключевые ресурсы в текущем словаре
					foreach (var key in themeDict.Keys)
					{
						if (Resources.Contains(key))
						{
							Resources[key] = themeDict[key];
						}
					}
				}
				
				// Применяем тему ко всем элементам интерфейса
				ApplyTheme();
				
				// Принудительно перерисовываем UI
				InvalidateVisual();
				UpdateLayout();
				
				// Обновляем все сообщения в переписке для корректного применения темы
				if (selectedContactId > 0)
				{
					// Сохраняем позицию прокрутки
					double scrollPosition = MessagesScrollViewer.VerticalOffset;
					
					// Перезагружаем переписку без анимации для обновления стилей
					conversationManager.LoadConversationWithoutAnimation(selectedContactId);
					
					// Восстанавливаем позицию прокрутки
					MessagesScrollViewer.ScrollToVerticalOffset(scrollPosition);
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка при переключении темы: {ex.Message}");
			}
		}
		
		// Новый чат (заглушка)
		private void NewChat_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("Функция создания нового чата будет реализована в следующей версии.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
		}
		
		// Открытие меню (заглушка)
		private void Menu_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("Дополнительные функции управления чатами будут реализованы в следующей версии.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
		}
		
		// Поиск в чате
		private void Search_Click(object sender, RoutedEventArgs e)
		{
			uiManager.ToggleSearchPanel(SearchPanelBorder.Visibility != Visibility.Visible);
		}
		
		// Обработчик изменения текста в поисковом поле
		private void SearchMessageTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			searchUIManager.HandleSearchTextChanged(SearchMessageTextBox.Text);
		}
		
		// Обработчик нажатия Enter в поисковом поле
		private void SearchMessageTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			searchUIManager.HandleSearchKeyDown(e);
		}
		
		// Очистка текста поиска
		private void ClearSearchText_Click(object sender, RoutedEventArgs e)
		{
			searchUIManager.ClearSearchText();
		}
		
		// Переход к предыдущему результату поиска
		private void PreviousSearchResult_Click(object sender, RoutedEventArgs e)
		{
			searchUIManager.NavigateToPreviousResult();
		}
		
	// Переход к следующему результату поиска
		private void NextSearchResult_Click(object sender, RoutedEventArgs e)
		{
			searchUIManager.NavigateToNextResult();
		}
		
		// Выбор элемента в списке результатов
		private void SearchResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			searchUIManager.HandleSearchResultSelection(SearchResultsList.SelectedIndex);
		}
		
		// Информация о контакте (заглушка)
		private void Info_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("Функция просмотра информации о контакте будет реализована в следующей версии.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
		}
		
		// Открытие настроек (заглушка)
		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			SettingsWindow settingsWindow = new SettingsWindow();
			settingsWindow.ShowDialog();
		}
		
		// Удаление чата
		private async void DeleteChat_Click(object sender, RoutedEventArgs e)
		{
			if (selectedContactId == 0)
			{
				MessageBox.Show("Сначала выберите контакт для удаления переписки.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			
			MessageBoxResult result = MessageBox.Show(
				$"Вы уверены, что хотите удалить всю переписку с пользователем {selectedContactName}?\nЭто действие нельзя отменить.",
				"Удаление переписки",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);
				
			if (result == MessageBoxResult.Yes)
			{
				try
				{
					// Сохраняем ссылку на элемент контакта перед удалением
					object contactItem = null;
					foreach (var item in ContactsListBox.Items)
					{
						if (item is ContactInfo info && info.UserId == selectedContactId)
						{
							contactItem = item;
							break;
						}
					}
					
					// Анимированно удаляем все сообщения
					List<int> messageIds = new List<int>(messageElements.Keys);
					foreach (int messageId in messageIds)
					{
						// Проверяем, от какого контакта сообщение
						using (SqlConnection connection = new SqlConnection(connectionString))
						{
							connection.Open();
							string sql = "SELECT ОтправительID FROM Сообщения WHERE СообщениеID = @messageId";
							using (SqlCommand command = new SqlCommand(sql, connection))
							{
								command.Parameters.AddWithValue("@messageId", messageId);
								var senderId = command.ExecuteScalar();
								if (senderId != null)
								{
									bool isOutgoing = Convert.ToInt32(senderId) == currentUserId;
									// Анимируем удаление
									AnimationHelper.MessageRemoveAnimation(messageElements[messageId], isOutgoing, true);
								}
							}
						}
						
						// Небольшая задержка между анимациями для эффекта
						await Task.Delay(50);
					}
					
					// Используем MessageManager для удаления переписки
					int deletedMessages = await messageManager.DeleteConversationAsync(selectedContactId);
					
					// Очищаем UI остатки сообщений, которые могли не анимироваться
					MessagesPanel.Children.Clear();
					messageElements.Clear();
					
					// Показываем сообщение об успешном удалении
					ChatTitle.Text = "Переписка удалена";
					
					// Создаем текстовый блок с информацией
					TextBlock infoText = new TextBlock
					{
						Text = $"Переписка с пользователем {selectedContactName} была удалена.",
						FontSize = 14,
						Foreground = ThemeManager.IsDarkTheme ? 
							new SolidColorBrush(Colors.LightGray) : 
							new SolidColorBrush(Colors.DarkGray),
						TextWrapping = TextWrapping.Wrap,
						TextAlignment = TextAlignment.Center,
						Margin = new Thickness(20, 50, 20, 0)
					};
					
					MessagesPanel.Children.Add(infoText);
					
					// Удаляем контакт из списка, если найден
					if (contactItem != null)
					{
						ContactsListBox.Items.Remove(contactItem);
					}
					else
					{
						// Перезагружаем список контактов
						contactManager.LoadContacts();
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка при удалении переписки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
		
		// Вставка эмодзи
		private void Emoji_Click(object sender, RoutedEventArgs e)
		{
			// Создаем и показываем всплывающее окно со смайликами
			var emojiPicker = new Popup
			{
				Width = 250,
				Height = 200,
				IsOpen = true,
				StaysOpen = false,
				Placement = PlacementMode.Bottom,
				PlacementTarget = sender as UIElement
			};
			
			// Создаем контейнер для эмодзи с прокруткой
			ScrollViewer scrollViewer = new ScrollViewer
			{
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
				Background = new SolidColorBrush(Colors.White)
			};
			
			// Создаем сетку эмодзи
			WrapPanel emojiPanel = new WrapPanel
			{
				Orientation = Orientation.Horizontal
			};
			
			// Список популярных смайликов в стиле iPhone
			string[] emojis = new string[]
			{
				"😀", "😃", "😄", "😁", "😆", "😅", "🤣", "😂", 
				"🙂", "🙃", "😉", "😊", "😇", "😍", "🥰", "😘", 
				"😗", "😙", "😚", "😋", "😛", "😜", "🤪", "😝", 
				"🤑", "🤗", "🤭", "🤫", "🤔", "🤐", "🤨", "😐", 
				"😑", "😶", "😏", "😒", "🙄", "😬", "🤥", "😌", 
				"😔", "😪", "🤤", "😴", "😷", "🤒", "🤕", "🤢", 
				"🤮", "🤧", "🥵", "🥶", "🥴", "😵", "🤯", "🤠", 
				"👋", "🤚", "✋", "🖖", "👌", "🤏", "✌️", "❤️", 
				"🧡", "💛", "💚", "💙", "💜", "🖤", "🤍",
				"👍", "👎", "👏", "🙌", "🤝", "🙏", "💪", "🎉"
			};
			
			// Добавляем смайлики в панель
			foreach (var emoji in emojis)
			{
				Button emojiButton = new Button
				{
					Content = emoji,
					Margin = new Thickness(5),
					Padding = new Thickness(5),
					FontSize = 20,
					Background = Brushes.Transparent,
					BorderThickness = new Thickness(0)
				};
				
				emojiButton.Click += (s, args) =>
				{
					// Вставляем выбранный смайлик в текущую позицию курсора
					int caretIndex = MessageTextBox.CaretIndex;
					string text = MessageTextBox.Text;
					
					MessageTextBox.Text = text.Insert(caretIndex, emoji);
					MessageTextBox.CaretIndex = caretIndex + emoji.Length;
					MessageTextBox.Focus();
					
					// Закрываем popup
					emojiPicker.IsOpen = false;
				};
				
				emojiPanel.Children.Add(emojiButton);
			}
			
			// Добавляем панель в скроллер
			scrollViewer.Content = emojiPanel;
			
			// Добавляем рамку для лучшего вида
			Border border = new Border
			{
				BorderThickness = new Thickness(1),
				BorderBrush = new SolidColorBrush(Colors.LightGray),
				CornerRadius = new CornerRadius(10),
				Child = scrollViewer
			};
			
			// Добавляем скроллер во всплывающее окно
			emojiPicker.Child = border;
		}

		// Обработчик события загрузки окна для применения темы
		private void ThemeResourcesLoaded(object sender, RoutedEventArgs e)
		{
			// Определяем текущую тему
			bool isDarkTheme = ThemeManager.IsDarkTheme;
			
			// Получаем словарь нужной темы
			var themeDictKey = isDarkTheme ? "DarkThemeDict" : "LightThemeDict";
			var themeDict = Resources[themeDictKey] as ResourceDictionary;
			
			if (themeDict != null)
			{
				// Обновляем все ключевые ресурсы в текущем словаре
				foreach (var key in themeDict.Keys)
				{
					if (Resources.Contains(key))
					{
						Resources[key] = themeDict[key];
					}
				}
			}
			
			// Обновляем иконку темы
			UpdateThemeIcon();
			
			// Применяем тему ко всем элементам интерфейса
			ApplyTheme();
		}

		// Применение выбранной темы к элементам интерфейса
		public void ApplyTheme()
		{
			try
			{
				// Определяем текущую тему
				bool isDarkTheme = ThemeManager.IsDarkTheme;
				
				// Получаем словарь нужной темы
				var themeDictKey = isDarkTheme ? "DarkThemeDict" : "LightThemeDict";
				var themeDict = Resources[themeDictKey] as ResourceDictionary;
				
				if (themeDict != null)
				{
					// Обновляем все ключевые ресурсы в текущем словаре
					foreach (var key in themeDict.Keys)
					{
						if (Resources.Contains(key))
						{
							Resources[key] = themeDict[key];
						}
					}
				}
				
				// Обновляем все сообщения в переписке для корректного применения темы
				foreach (var messageElement in messageElements.Values)
				{
					if (messageElement is StackPanel messagePanel)
					{
						foreach (var child in UIMessageFactory.FindAllChildren<Border>(messagePanel))
						{
							if (child.Name == "MessageBubble" || child.Name == "AttachmentBubble")
							{
								// Определяем, входящее или исходящее сообщение
								bool isOutgoing = messagePanel.HorizontalAlignment == HorizontalAlignment.Right;
								
								// Применяем цвет фона в зависимости от типа сообщения и темы
								if (isOutgoing)
								{
									child.Background = isDarkTheme ? 
										new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A9D8F")) : 
										new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCF8C6"));
								}
								else
								{
									child.Background = isDarkTheme ? 
										new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3A3A")) : 
										new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
								}
								
								// Обновляем цвет текста
								foreach (var textBlock in UIMessageFactory.FindAllChildren<TextBlock>(child))
								{
									textBlock.Foreground = isDarkTheme ? 
										new SolidColorBrush(Colors.LightGray) : 
										new SolidColorBrush(Colors.Black);
								}
							}
						}
					}
				}
				
				// Обновляем стиль для разделителей дат
				foreach (UIElement element in MessagesPanel.Children)
				{
					if (element is Border border && border.Tag != null && border.Tag is string tagString)
					{
						if (tagString.Contains("yyyy-MM-dd"))
						{
							// Обновляем стиль разделителя даты
							border.Background = new SolidColorBrush(Color.FromArgb(
								80, 
								isDarkTheme ? (byte)150 : (byte)200, 
								isDarkTheme ? (byte)150 : (byte)200, 
								isDarkTheme ? (byte)150 : (byte)200));
							
							// Обновляем цвет текста в разделителе
							if (border.Child is TextBlock dateText)
							{
								dateText.Foreground = isDarkTheme ? 
									new SolidColorBrush(Colors.LightGray) : 
									new SolidColorBrush(Colors.Black);
							}
						}
					}
				}
				
				// Принудительно перерисовываем UI
				InvalidateVisual();
				UpdateLayout();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка при применении темы: {ex.Message}");
			}
		}

		// Обработчик нажатия Ctrl+V для вставки изображений из буфера обмена
		private void MessageTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
			{
				e.Handled = true; // Предотвращаем стандартное поведение
				clipboardManager.HandlePaste();
			}
		}

		// Публичные методы для работы с контекстным меню сообщений
		
		// Метод для копирования сообщения
		public void CopyMessage(int messageId)
		{
			try
			{
				string messageText = "";
				
				// Получаем текст сообщения из БД
				using (SqlConnection connection = new SqlConnection(connectionString))
				{
					connection.Open();
					string query = "SELECT Текст FROM Сообщения WHERE СообщениеID = @messageId";
					using (SqlCommand command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@messageId", messageId);
						var result = command.ExecuteScalar();
						if (result != null && result != DBNull.Value)
						{
							byte[] encryptedBytes = (byte[])result;
							string encryptedBase64 = Convert.ToBase64String(encryptedBytes);
							messageText = EncryptionHelper.DecryptString(encryptedBase64);
						}
					}
				}
				
				if (!string.IsNullOrEmpty(messageText))
				{
					Clipboard.SetText(messageText);
					// Можно добавить уведомление о копировании
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при копировании сообщения: " + ex.Message);
			}
		}
		
		// Метод для редактирования сообщения
		public void EditMessage(int messageId)
		{
			try
			{
				// Получаем текст сообщения из базы
				string messageText = "";
				
				// Получаем текст сообщения из БД
				using (SqlConnection connection = new SqlConnection(connectionString))
				{
					connection.Open();
					string query = "SELECT Текст FROM Сообщения WHERE СообщениеID = @messageId";
					using (SqlCommand command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@messageId", messageId);
						var result = command.ExecuteScalar();
						if (result != null && result != DBNull.Value)
						{
							byte[] encryptedBytes = (byte[])result;
							string encryptedBase64 = Convert.ToBase64String(encryptedBytes);
							messageText = EncryptionHelper.DecryptString(encryptedBase64);
						}
					}
				}
				
				if (!string.IsNullOrEmpty(messageText))
				{
					// Устанавливаем текст в поле ввода
					MessageTextBox.Text = messageText;
					
					// Устанавливаем флаг редактирования и ID редактируемого сообщения
					isEditingMessage = true;
					editingMessageId = messageId;
					
					// Фокусируемся на поле ввода
					MessageTextBox.Focus();
					MessageTextBox.SelectionStart = MessageTextBox.Text.Length;
					
					// Меняем иконку на кнопке отправки (находим кнопку с иконкой Send)
					foreach (var element in GetLogicalChildrenByType<Button>(this))
					{
						if (element.ToolTip?.ToString() == "Отправить сообщение")
						{
							if (element.Content is PackIconMaterial iconMaterial)
							{
								iconMaterial.Kind = PackIconMaterialKind.PencilOutline;
								break;
							}
						}
					}
					
					// Показываем уведомление о редактировании
					ShowNotification("Редактирование сообщения");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при редактировании сообщения: " + ex.Message);
			}
		}
		
		// Метод для удаления сообщения только для текущего пользователя
		public async void DeleteMessageForMe(int messageId)
		{
			await messageDeleteManager.DeleteMessageForMeAsync(messageId);
		}
		
		// Метод для удаления сообщения для всех участников
		public async void DeleteMessageForAll(int messageId)
		{
			await messageDeleteManager.DeleteMessageForAllAsync(messageId);
		}
		
		// Вспомогательный метод для отображения временного уведомления
		private void ShowNotification(string message)
		{
			// Создаем уведомление в стиле всплывающего окна
			var notificationPanel = new Border
			{
				Background = new SolidColorBrush(Color.FromArgb(230, 60, 60, 60)),
				CornerRadius = new CornerRadius(8),
				Padding = new Thickness(15, 10, 15, 10),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
				Margin = new Thickness(0, 0, 0, 20),
				Effect = new System.Windows.Media.Effects.DropShadowEffect
				{
					BlurRadius = 10,
					ShadowDepth = 0,
					Opacity = 0.3
				}
			};
			
			// Текст уведомления
			var textBlock = new TextBlock
			{
				Text = message,
				Foreground = Brushes.White,
				FontFamily = new FontFamily("Segoe UI"),
				FontSize = 14,
				TextWrapping = TextWrapping.Wrap
			};
			
			notificationPanel.Child = textBlock;
			
			// Добавляем уведомление в Grid чата
			var grid = MessagesScrollViewer.Parent as Grid;
			if (grid != null)
			{
				grid.Children.Add(notificationPanel);
				
				// Настраиваем анимацию для уведомления
				var animation = new DoubleAnimation
				{
					From = 0,
					To = 1,
					Duration = TimeSpan.FromSeconds(0.3),
					EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
				};
				
				notificationPanel.Opacity = 0;
				notificationPanel.BeginAnimation(UIElement.OpacityProperty, animation);
				
				// Таймер для автоматического скрытия уведомления
				var timer = new DispatcherTimer();
				timer.Interval = TimeSpan.FromSeconds(3);
				timer.Tick += (s, e) =>
				{
					var fadeOut = new DoubleAnimation
					{
						From = 1,
						To = 0,
						Duration = TimeSpan.FromSeconds(0.5),
						EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
					};
					
					fadeOut.Completed += (s2, e2) =>
					{
						grid.Children.Remove(notificationPanel);
					};
					
					notificationPanel.BeginAnimation(UIElement.OpacityProperty, fadeOut);
					timer.Stop();
				};
				timer.Start();
			}
		}

		// Получение логических дочерних элементов определенного типа
		private IEnumerable<T> GetLogicalChildrenByType<T>(DependencyObject parent) where T : DependencyObject
		{
			var count = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);
				if (child is T t)
				{
					yield return t;
				}
				
				foreach (var grandChild in GetLogicalChildrenByType<T>(child))
				{
					yield return grandChild;
				}
			}
		}
	}
}
