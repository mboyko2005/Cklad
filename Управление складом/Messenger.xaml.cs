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
			
			currentUserId = userId;
			
			// Получаем логин текущего пользователя из базы данных
			LoadCurrentUserInfo();
			
			// Инициализируем менеджеры
			messageManager = new MessageManager(connectionString, currentUserId, currentUserName);
			contactManager = new ContactManager(connectionString, currentUserId, ContactsListBox);
			conversationManager = new ConversationManager(connectionString, currentUserId, MessagesPanel, messageElements);
			
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
			
			// Обновляем иконку темы
			UpdateThemeIcon();
			
			// Обновляем элементы интерфейса в зависимости от темы
			ApplyTheme();
			
			// Устанавливаем заглушки предложенных сообщений
			SetupSuggestedMessages();
			
			// Настраиваем таймер автоудаления старых сообщений
			SetupAutoDeleteTimer();
			
			// Прокрутка сообщений при изменении
			MessagesPanel.SizeChanged += (s, e) => ScrollToBottom();
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

		// Выбор контакта
		private void ContactsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ContactsListBox.SelectedItem is ContactInfo info)
			{
				ChatTitle.Text = info.Login;
				ChatUserLogin.Text = $"Роль: {info.Role}";
				selectedContactId = info.UserId;
				selectedContactName = info.Login;

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

		// Настройка таймера автоудаления старых сообщений
		private void SetupAutoDeleteTimer()
		{
			// Таймер для автоматического удаления сообщений старше N дней
			DispatcherTimer autoDeleteTimer = new DispatcherTimer();
			autoDeleteTimer.Interval = TimeSpan.FromHours(24); // Проверяем раз в день
			autoDeleteTimer.Tick += (s, e) =>
			{
				DeleteOldMessages(30); // Удаляем сообщения старше 30 дней
			};
			autoDeleteTimer.Start();
			
			// Запускаем первичную проверку при старте
			DeleteOldMessages(30);
		}
		
		// Удаление старых сообщений
		private void DeleteOldMessages(int daysOld)
		{
			try
			{
				using (SqlConnection connection = new SqlConnection(connectionString))
				{
					connection.Open();
					
					// Запрос для пометки сообщений как скрытых для текущего пользователя
					string sql = @"
						UPDATE Сообщения
						SET СкрытоОтправителем = CASE WHEN ОтправительID = @userId THEN 1 ELSE СкрытоОтправителем END,
							СкрытоПолучателем = CASE WHEN ПолучательID = @userId THEN 1 ELSE СкрытоПолучателем END
						WHERE ДатаОтправки < @oldDate
						AND (
							(ОтправительID = @userId AND СкрытоОтправителем = 0)
							OR 
							(ПолучательID = @userId AND СкрытоПолучателем = 0)
						)";
						
						using (SqlCommand command = new SqlCommand(sql, connection))
						{
							DateTime oldDate = DateTime.Now.AddDays(-daysOld);
							command.Parameters.AddWithValue("@userId", currentUserId);
							command.Parameters.AddWithValue("@oldDate", oldDate);
							
							int affectedRows = command.ExecuteNonQuery();
							
							if (affectedRows > 0 && selectedContactId > 0)
							{
								// Перезагружаем текущую переписку, если сообщения были удалены
								conversationManager.LoadConversation(selectedContactId);
							}
						}
				}
			}
			catch (Exception ex)
			{
				// Логируем ошибку, но не показываем пользователю
				System.Diagnostics.Debug.WriteLine($"Ошибка при удалении старых сообщений: {ex.Message}");
			}
		}

		// Параметрless конструктор для XAML (будет использовать дефолтный ID = 1)
		public MessengerWindow() : this(1)
		{
		}
		
		// Обновление иконки темы
		public void UpdateThemeIcon()
		{
			try
			{
				if (ThemeIcon != null)
				{
					ThemeIcon.Kind = ThemeManager.IsDarkTheme 
						? PackIconMaterialKind.WeatherNight
						: PackIconMaterialKind.WeatherSunny;
				}
			}
			catch (Exception ex)
			{
				// Игнорируем ошибку, если элемент не найден
				Console.WriteLine("Ошибка при обновлении иконки темы: " + ex.Message);
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

			string messageText = MessageTextBox.Text.Trim();
			if (string.IsNullOrEmpty(messageText))
			{
				return;
			}

			try
			{
				int messageId;
				
				if (isEditingMessage)
				{
					// Редактируем существующее сообщение
					bool success = await messageManager.EditMessageAsync(editingMessageId, messageText);
					if (success)
					{
						// Обновляем сообщение в интерфейсе
						if (messageElements.TryGetValue(editingMessageId, out var element))
						{
							if (element is StackPanel panel)
							{
								FrameworkElement messageBubble = UIMessageFactory.FindFirstBorder(panel);
								if (messageBubble is Border border && border.Child is StackPanel contentPanel)
								{
									TextBlock messageBlock = UIMessageFactory.FindTextBlock(contentPanel);
									if (messageBlock != null)
									{
										messageBlock.Text = messageText;
										
										// Анимация обновления сообщения
										AnimateMessageUpdate(messageBubble);
									}
								}
							}
						}
					}
					
					// Сбрасываем состояние редактирования
					isEditingMessage = false;
					editingMessageId = 0;
					
					// Очищаем поле ввода
					MessageTextBox.Clear();
				}
				else
				{
					// Отправляем новое сообщение
					messageId = await messageManager.SendTextMessageAsync(selectedContactId, messageText);
					
					if (messageId > 0)
					{
						// Создаем и добавляем новое сообщение в интерфейс
						DateTime now = DateTime.Now;
						StackPanel messageBubble = UIMessageFactory.CreateMessageBubble(messageId, currentUserId, messageText, now, true, false, false, false, currentUserId);
						MessagesPanel.Children.Add(messageBubble);
						messageElements[messageId] = messageBubble;
						
						// Прокручиваем к новому сообщению
						ScrollToBottom();
						
						// Очищаем поле ввода и сбрасываем ответ
						MessageTextBox.Clear();
						replyToMessageId = 0;
						replyToMessageText = "";
						replyToSenderId = 0;
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при отправке сообщения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		
		// Анимация обновления сообщения
		private void AnimateMessageUpdate(FrameworkElement message)
		{
			// Сохраняем начальный цвет фона
			Brush originalBackground = null;
			if (message is Border border)
			{
				originalBackground = border.Background;
			}
			
			// Создаем анимацию фона
			ColorAnimation colorAnimation = null;
			if (originalBackground is SolidColorBrush brush)
			{
				// Светло-желтый цвет для подсветки изменения
				Color highlightColor = Color.FromRgb(255, 255, 200);
				Color originalColor = brush.Color;
				
				colorAnimation = new ColorAnimation
				{
					From = highlightColor,
					To = originalColor,
					Duration = TimeSpan.FromMilliseconds(1500),
					EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
				};
				
				// Создаем кисть для анимации
				SolidColorBrush animationBrush = new SolidColorBrush(highlightColor);
				if (message is Border borderElement)
				{
					borderElement.Background = animationBrush;
				}
				
				// Запускаем анимацию цвета
				animationBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
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
			ofd.Filter = "Image and Video Files|*.jpg;*.jpeg;*.png;*.bmp;*.mp4;*.avi";
			if (ofd.ShowDialog() == true)
			{
				byte[] fileBytes = File.ReadAllBytes(ofd.FileName);
				string ext = System.IO.Path.GetExtension(ofd.FileName).ToLower();
				string fileType = (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp") ? "Фото"
								  : (ext == ".mp4" || ext == ".avi") ? "Видео" : "Файл";

				try
				{
					int messageId = 0;
					using (SqlConnection connection = new SqlConnection(connectionString))
					{
						connection.Open();
						string sqlMsg = @"
                            INSERT INTO Сообщения (ОтправительID, ПолучательID, Текст)
                            OUTPUT INSERTED.СообщениеID
                            VALUES (@sender, @receiver, @text)";
						using (SqlCommand cmdMsg = new SqlCommand(sqlMsg, connection))
						{
							string encryptedBase64 = EncryptionHelper.EncryptString("[Медиафайл]");
							byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);
							cmdMsg.Parameters.AddWithValue("@sender", currentUserId);
							cmdMsg.Parameters.AddWithValue("@receiver", selectedContactId);
							cmdMsg.Parameters.Add("@text", SqlDbType.VarBinary).Value = encryptedBytes;
							messageId = (int)cmdMsg.ExecuteScalar();
						}

						string sqlMedia = @"
                            INSERT INTO МедиаФайлы (СообщениеID, Тип, Файл)
                            VALUES (@msgId, @type, @file)";
						using (SqlCommand cmdMedia = new SqlCommand(sqlMedia, connection))
						{
							cmdMedia.Parameters.AddWithValue("@msgId", messageId);
							cmdMedia.Parameters.AddWithValue("@type", fileType);
							byte[] encryptedFile = EncryptionHelper.EncryptBytes(fileBytes);
							cmdMedia.Parameters.AddWithValue("@file", encryptedFile);
							cmdMedia.ExecuteNonQuery();
						}
					}
					conversationManager.LoadConversation(selectedContactId);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка отправки файла: " + ex.Message);
				}
			}
		}
		
		// Удаление сообщения только для текущего пользователя
		private async void DeleteMessageForMe_Click(object sender, RoutedEventArgs e)
		{
			if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu &&
				contextMenu.PlacementTarget is FrameworkElement element && element.Tag is int messageId)
			{
				try
				{
					// Определяем, является ли сообщение исходящим
					bool isOutgoing = false;
					using (SqlConnection connection = new SqlConnection(connectionString))
					{
						connection.Open();
						string checkSql = "SELECT ОтправительID FROM Сообщения WHERE СообщениеID = @messageId";
						using (SqlCommand checkCmd = new SqlCommand(checkSql, connection))
						{
							checkCmd.Parameters.AddWithValue("@messageId", messageId);
							var senderId = checkCmd.ExecuteScalar();
							isOutgoing = senderId != null && Convert.ToInt32(senderId) == currentUserId;
						}
					}
					
					// Используем MessageManager для удаления сообщения
					bool success = await messageManager.DeleteMessageForMeAsync(messageId);
					
					if (success)
					{
						// Анимируем удаление сообщения
						AnimateMessageRemoval(messageId, isOutgoing);
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка при удалении сообщения: " + ex.Message);
				}
			}
		}
		
		// Удаление сообщения для всех
		private async void DeleteMessageForAll_Click(object sender, RoutedEventArgs e)
		{
			if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu &&
				contextMenu.PlacementTarget is FrameworkElement element && element.Tag is int messageId)
			{
				MessageBoxResult result = MessageBox.Show(
					"Вы уверены, что хотите удалить это сообщение для всех участников?",
					"Удаление сообщения",
					MessageBoxButton.YesNo,
					MessageBoxImage.Question);
					
				if (result == MessageBoxResult.Yes)
				{
					try
					{
						// Определяем, является ли сообщение исходящим
						bool isOutgoing = false;
						using (SqlConnection connection = new SqlConnection(connectionString))
						{
							connection.Open();
							string checkSql = "SELECT ОтправительID FROM Сообщения WHERE СообщениеID = @messageId";
							using (SqlCommand checkCmd = new SqlCommand(checkSql, connection))
							{
								checkCmd.Parameters.AddWithValue("@messageId", messageId);
								var senderId = checkCmd.ExecuteScalar();
								isOutgoing = senderId != null && Convert.ToInt32(senderId) == currentUserId;
							}
						}
						
						// Используем MessageManager для удаления сообщения для всех
						bool success = await messageManager.DeleteMessageForAllAsync(messageId);
						
						if (success)
						{
							// Анимируем удаление сообщения
							AnimateMessageRemoval(messageId, isOutgoing);
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show("Ошибка при удалении сообщения: " + ex.Message);
					}
				}
			}
		}
		
		// Копирование текста сообщения
		private void CopyMessage_Click(object sender, RoutedEventArgs e)
		{
			if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu &&
				contextMenu.PlacementTarget is FrameworkElement element && element.Tag is int messageId)
			{
				try
				{
					using (SqlConnection connection = new SqlConnection(connectionString))
					{
						connection.Open();
						string sql = @"
                            SELECT Текст FROM Сообщения WHERE СообщениеID = @messageId";
						using (SqlCommand command = new SqlCommand(sql, connection))
						{
							command.Parameters.AddWithValue("@messageId", messageId);
							
							byte[] encryptedBytes = (byte[])command.ExecuteScalar();
							string encryptedBase64 = Convert.ToBase64String(encryptedBytes);
							string decryptedText = EncryptionHelper.DecryptString(encryptedBase64);
							
							// Копируем в буфер обмена
							Clipboard.SetText(decryptedText);
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка при копировании сообщения: " + ex.Message);
				}
			}
		}
		
		// Редактирование сообщения
		private void EditMessage_Click(object sender, RoutedEventArgs e)
		{
			if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu &&
				contextMenu.PlacementTarget is FrameworkElement element && element.Tag is int messageId)
			{
				try
				{
					using (SqlConnection connection = new SqlConnection(connectionString))
					{
						connection.Open();
						
						// Проверяем, является ли пользователь отправителем
						string checkSql = @"
                            SELECT ОтправительID, Текст FROM Сообщения 
                            WHERE СообщениеID = @messageId";
						using (SqlCommand command = new SqlCommand(checkSql, connection))
						{
							command.Parameters.AddWithValue("@messageId", messageId);
							
							using (SqlDataReader reader = command.ExecuteReader())
							{
								if (reader.Read())
								{
									int senderId = reader.GetInt32(0);
									
									// Если это не наше сообщение, выходим
									if (senderId != currentUserId)
									{
										MessageBox.Show("Вы можете редактировать только свои сообщения.");
										return;
									}
									
									// Получаем текст сообщения
									byte[] encryptedBytes = (byte[])reader["Текст"];
									string encryptedBase64 = Convert.ToBase64String(encryptedBytes);
									string decryptedText = EncryptionHelper.DecryptString(encryptedBase64);
									
									// Заполняем поле ввода текстом сообщения
									MessageTextBox.Text = decryptedText;
									MessageTextBox.SelectAll();
									MessageTextBox.Focus();
									
									// Устанавливаем флаг редактирования
									isEditingMessage = true;
									editingMessageId = messageId;
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка при редактировании сообщения: " + ex.Message);
				}
			}
		}
		
		// Ответ на сообщение
		private void ReplyMessage_Click(object sender, RoutedEventArgs e)
		{
			if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu &&
				contextMenu.PlacementTarget is FrameworkElement element && element.Tag is int messageId)
			{
				// TODO: Реализовать функционал ответа на сообщение
				MessageBox.Show("Функция ответа на сообщение будет реализована в следующей версии.");
			}
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
			ThemeManager.ToggleTheme();
			UpdateThemeIcon();
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
		
		// Поиск в чате (заглушка)
		private void Search_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("Функция поиска в чате будет реализована в следующей версии.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
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
									AnimateMessageRemoval(messageId, isOutgoing);
								}
							}
						}
						
						// Небольшая задержка между анимациями для эффекта
						await Task.Delay(50);
					}
					
					// Используем новый MessageManager для удаления переписки
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
				"👋", "🤚", "✋", "🖖", "👌", "🤌", "🤏", "✌️", 
				"❤️", "🧡", "💛", "💚", "💙", "💜", "🖤", "🤍",
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

		// Прокрутка к последнему сообщению
		private void ScrollToBottom()
		{
			try
			{
				if (MessagesScrollViewer != null)
					MessagesScrollViewer.ScrollToEnd();
			}
			catch (Exception ex)
			{
				// Игнорируем ошибку, если элемент не найден
				Console.WriteLine("Ошибка при прокрутке: " + ex.Message);
			}
		}

		// Применение выбранной темы к элементам интерфейса
		public void ApplyTheme()
		{
			// Определяем текущую тему
			bool isDarkTheme = Application.Current.Resources.MergedDictionaries.Any(d =>
				d.Source != null && d.Source.ToString().Contains("DarkTheme.xaml"));

			// Получаем цвета в зависимости от темы
			var backgroundColor = isDarkTheme ? 
				new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2B2B2B")) : 
				new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"));
			
			var foregroundColor = isDarkTheme ? 
				new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")) : 
				new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"));
			
			// Применяем цвета к элементам
			this.Background = backgroundColor;
		}

		// Настройка предложенных сообщений
		private void SetupSuggestedMessages()
		{
			// Примеры предложенных сообщений
			var suggestedMessages = new List<string>
			{
				"Добрый день!",
				"Как обстоят дела с заказом?",
				"Спасибо за информацию",
				"Подтверждаю получение",
				"Требуется дополнительная информация"
			};
			
			// Очищаем панель предложенных сообщений
			SuggestedMessagesPanel.Children.Clear();
			
			// Добавляем предложенные сообщения в панель
			foreach (var message in suggestedMessages)
			{
				Button button = new Button
				{
					Content = message,
					Style = (Style)FindResource("SuggestedMessageButtonStyle")
				};
				
				button.Click += (s, e) =>
				{
					MessageTextBox.Text = message;
					MessageTextBox.Focus();
					MessageTextBox.SelectionStart = MessageTextBox.Text.Length;
				};
				
				SuggestedMessagesPanel.Children.Add(button);
			}
		}

		// Анимация удаления сообщения
		private void AnimateMessageRemoval(int messageId, bool isOutgoing)
		{
			if (messageElements.TryGetValue(messageId, out var element))
			{
				// Используем AnimationHelper для анимации удаления
				AnimationHelper.MessageRemoveAnimation(element, isOutgoing, true);
				
				// Удаляем ссылку на элемент из словаря
				messageElements.Remove(messageId);
			}
		}

		// Обработчик нажатия Ctrl+V для вставки изображений из буфера обмена
		private void MessageTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
			{
				e.Handled = true; // Предотвращаем стандартное поведение
				HandlePaste();
			}
		}

		// Обработчик вставки содержимого из буфера обмена
		private void HandlePaste()
		{
			if (Clipboard.ContainsImage())
			{
				// Получаем изображение из буфера обмена
				BitmapSource bitmapSource = Clipboard.GetImage();
				
				if (bitmapSource != null)
				{
					// Если выбран контакт для отправки
					if (selectedContactId <= 0)
					{
						MessageBox.Show("Пожалуйста, выберите контакт для отправки изображения.");
						return;
					}
					
					try
					{
						// Преобразуем BitmapSource в массив байтов
						byte[] imageBytes = ConvertBitmapSourceToBytes(bitmapSource);
						
						// Создаем объект вложения
						MessageAttachment attachment = new MessageAttachment
						{
							Type = УправлениеСкладом.Class.AttachmentType.Image,
							FileName = $"clip_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.png",
							FileSize = imageBytes.Length,
							Content = imageBytes,
							Thumbnail = CreateThumbnail(bitmapSource, 100, 100)
						};
						
						// Отправляем сообщение с вложением
						SendMessageWithAttachment(attachment);
					}
					catch (Exception ex)
					{
						MessageBox.Show($"Ошибка при вставке изображения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
			}
			else if (Clipboard.ContainsText())
			{
				// Если в буфере текст, вставляем его в поле ввода
				string clipboardText = Clipboard.GetText();
				int caretIndex = MessageTextBox.CaretIndex;
				string text = MessageTextBox.Text;
				
				MessageTextBox.Text = text.Insert(caretIndex, clipboardText);
				MessageTextBox.CaretIndex = caretIndex + clipboardText.Length;
			}
		}
		
		// Преобразование BitmapSource в массив байтов
		private byte[] ConvertBitmapSourceToBytes(BitmapSource bitmapSource)
		{
			JpegBitmapEncoder encoder = new JpegBitmapEncoder();
			encoder.QualityLevel = 80; // Качество сжатия
			encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
			
			using (MemoryStream ms = new MemoryStream())
			{
				encoder.Save(ms);
				return ms.ToArray();
			}
		}
		
		// Создание миниатюры для изображения
		private byte[] CreateThumbnail(BitmapSource source, int maxWidth, int maxHeight)
		{
			// Вычисляем новые размеры с сохранением соотношения сторон
			double scale = Math.Min((double)maxWidth / source.PixelWidth, (double)maxHeight / source.PixelHeight);
			int newWidth = (int)(source.PixelWidth * scale);
			int newHeight = (int)(source.PixelHeight * scale);
			
			// Создаем миниатюру
			TransformedBitmap thumbnail = new TransformedBitmap(source, new ScaleTransform(scale, scale));
			
			// Сохраняем в формате JPEG
			JpegBitmapEncoder encoder = new JpegBitmapEncoder();
			encoder.QualityLevel = 70; // Качество миниатюры
			encoder.Frames.Add(BitmapFrame.Create(thumbnail));
			
			using (MemoryStream ms = new MemoryStream())
			{
				encoder.Save(ms);
				return ms.ToArray();
			}
		}
		
		// Отправка сообщения с вложением
		private async void SendMessageWithAttachment(MessageAttachment attachment)
		{
			try
			{
				// Получаем текст сообщения (если есть)
				string messageText = MessageTextBox.Text.Trim();
				
				// Отправляем сообщение с вложением
				int messageId = await messageManager.SendMessageWithAttachmentAsync(selectedContactId, messageText, attachment);
				
				if (messageId > 0)
				{
					// Очищаем поле ввода
					MessageTextBox.Clear();
					
					// Создаем и добавляем новое сообщение в интерфейс с текстом о вложении
					string displayText = $"[Изображение] {attachment.FileName}";
					DateTime now = DateTime.Now;
					
					StackPanel messageBubble = UIMessageFactory.CreateMessageBubble(messageId, currentUserId, displayText, now, true, false, false, false, currentUserId);
					MessagesPanel.Children.Add(messageBubble);
					messageElements[messageId] = messageBubble;
					
					// Прокручиваем к новому сообщению
					ScrollToBottom();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при отправке сообщения с вложением: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}
