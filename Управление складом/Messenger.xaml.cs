using Microsoft.Win32;
using MahApps.Metro.IconPacks;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace УправлениеСкладом
{
	public partial class MessengerWindow : Window
	{
		// Строка подключения
		private string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True;TrustServerCertificate=True";

		// Текущий пользователь
		private int currentUserId;

		// Выбранный контакт
		private int selectedContactId = 0;

		// Параметризованный конструктор
		public MessengerWindow(int userId)
		{
			InitializeComponent();
			this.currentUserId = userId;
			ContactsListBox.SelectionChanged += ContactsListBox_SelectionChanged;
			LoadContactsFromDb();
		}

		// Параметрless конструктор для XAML (будет использовать дефолтный ID = 1)
		public MessengerWindow() : this(1)
		{
		}

		// Загрузка контактов
		private void LoadContactsFromDb()
		{
			ContactsListBox.Items.Clear();
			try
			{
				using (SqlConnection connection = new SqlConnection(connectionString))
				{
					connection.Open();
					string sql;
					SqlCommand command;
					if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
					{
						sql = @"
                            SELECT DISTINCT u.ПользовательID, u.ИмяПользователя, r.Наименование AS Роль
                            FROM Пользователи u
                            INNER JOIN Роли r ON u.РольID = r.РольID
                            WHERE u.ПользовательID IN (
	                            SELECT CASE WHEN ОтправительID = @currentUserId THEN ПолучательID ELSE ОтправительID END
	                            FROM Сообщения
	                            WHERE ОтправительID = @currentUserId OR ПолучательID = @currentUserId
                            )";
						command = new SqlCommand(sql, connection);
						command.Parameters.AddWithValue("@currentUserId", currentUserId);
					}
					else
					{
						sql = @"
                            SELECT u.ПользовательID, u.ИмяПользователя, r.Наименование AS Роль
                            FROM Пользователи u
                            INNER JOIN Роли r ON u.РольID = r.РольID
                            WHERE u.ИмяПользователя LIKE @searchText";
						command = new SqlCommand(sql, connection);
						command.Parameters.AddWithValue("@searchText", "%" + SearchTextBox.Text.Trim() + "%");
					}

					using (SqlDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							int userId = reader.GetInt32(0);
							string userName = reader.GetString(1);
							string role = reader.GetString(2);

							if (userId == currentUserId)
								continue;

							var contact = new ContactInfo
							{
								UserId = userId,
								Login = userName,
								Role = role,
								UnreadCount = 2 // для демонстрации
							};

							ContactsListBox.Items.Add(contact);
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка загрузки контактов: " + ex.Message);
			}
		}

		private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			LoadContactsFromDb();
		}

		// Выбор контакта
		private void ContactsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ContactsListBox.SelectedItem is ContactInfo info)
			{
				ChatTitle.Text = info.Login;
				ChatUserLogin.Text = $"Логин: {info.Login}   Роль: {info.Role}";
				selectedContactId = info.UserId;

				// Сбрасываем непрочитанные
				info.UnreadCount = 0;
				ContactsListBox.Items.Refresh();

				LoadConversation(selectedContactId);
			}
		}

		// Загрузка переписки
		private void LoadConversation(int contactId)
		{
			MessagesPanel.Children.Clear();
			try
			{
				using (SqlConnection connection = new SqlConnection(connectionString))
				{
					connection.Open();
					string sql = @"
                        SELECT СообщениеID, ОтправительID, Текст, ДатаОтправки
                        FROM Сообщения
                        WHERE (ОтправительID = @currentUserId AND ПолучательID = @contactId)
                           OR (ОтправительID = @contactId AND ПолучательID = @currentUserId)
                        ORDER BY ДатаОтправки ASC";
					using (SqlCommand command = new SqlCommand(sql, connection))
					{
						command.Parameters.AddWithValue("@currentUserId", currentUserId);
						command.Parameters.AddWithValue("@contactId", contactId);

						using (SqlDataReader reader = command.ExecuteReader())
						{
							int prevSenderId = -1;

							while (reader.Read())
							{
								int senderId = reader.GetInt32(1);
								byte[] encryptedBytes = (byte[])reader["Текст"];
								string encryptedBase64 = Convert.ToBase64String(encryptedBytes);
								string decryptedText = EncryptionHelper.DecryptString(encryptedBase64);
								bool isOutgoing = (senderId == currentUserId);

								StackPanel messageRow = new StackPanel
								{
									Orientation = Orientation.Horizontal,
									Margin = new Thickness(5)
								};

								bool showAvatar = (senderId != prevSenderId);

								Border messageBubble = new Border
								{
									CornerRadius = new CornerRadius(10),
									Background = isOutgoing ? (Brush)FindResource("PrimaryBrush") : (Brush)FindResource("ButtonBackgroundBrush"),
									Padding = new Thickness(10),
									MaxWidth = 300
								};
								TextBlock msgText = new TextBlock
								{
									Text = decryptedText,
									Foreground = isOutgoing ? Brushes.White : Brushes.Black,
									FontFamily = MessageTextBox.FontFamily,
									TextWrapping = TextWrapping.Wrap
								};
								messageBubble.Child = msgText;

								if (isOutgoing)
								{
									messageRow.HorizontalAlignment = HorizontalAlignment.Right;
									TextBlock statusText = new TextBlock
									{
										Text = "✓",
										FontSize = 12,
										Margin = new Thickness(5, 0, 0, 0),
										VerticalAlignment = VerticalAlignment.Bottom,
										Foreground = Brushes.Gray
									};

									messageRow.Children.Add(messageBubble);
									messageRow.Children.Add(statusText);
									if (showAvatar)
									{
										PackIconMaterial avatarIcon = new PackIconMaterial
										{
											Kind = PackIconMaterialKind.AccountCircle,
											Width = 30,
											Height = 30,
											VerticalAlignment = VerticalAlignment.Center,
											Margin = new Thickness(5),
											Foreground = Brushes.Gray
										};
										messageRow.Children.Add(avatarIcon);
									}
								}
								else
								{
									messageRow.HorizontalAlignment = HorizontalAlignment.Left;
									if (showAvatar)
									{
										PackIconMaterial avatarIcon = new PackIconMaterial
										{
											Kind = PackIconMaterialKind.AccountCircle,
											Width = 30,
											Height = 30,
											VerticalAlignment = VerticalAlignment.Center,
											Margin = new Thickness(5),
											Foreground = Brushes.Gray
										};
										messageRow.Children.Add(avatarIcon);
									}
									messageRow.Children.Add(messageBubble);
								}

								MessagesPanel.Children.Add(messageRow);
								prevSenderId = senderId;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка загрузки сообщений: " + ex.Message);
			}
		}

		// Отправка текстового сообщения
		private void SendMessage_Click(object sender, RoutedEventArgs e)
		{
			if (selectedContactId == 0)
			{
				MessageBox.Show("Сначала выберите контакт!");
				return;
			}
			string message = MessageTextBox.Text.Trim();
			if (!string.IsNullOrEmpty(message))
			{
				string encryptedBase64 = EncryptionHelper.EncryptString(message);
				byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);
				try
				{
					using (SqlConnection connection = new SqlConnection(connectionString))
					{
						connection.Open();
						string sql = @"
                            INSERT INTO Сообщения (ОтправительID, ПолучательID, Текст)
                            VALUES (@sender, @receiver, @text)";
						using (SqlCommand command = new SqlCommand(sql, connection))
						{
							command.Parameters.AddWithValue("@sender", currentUserId);
							command.Parameters.AddWithValue("@receiver", selectedContactId);
							command.Parameters.Add("@text", SqlDbType.VarBinary).Value = encryptedBytes;
							command.ExecuteNonQuery();
						}
					}
					LoadConversation(selectedContactId);
					MessageTextBox.Clear();
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка отправки сообщения: " + ex.Message);
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
					LoadConversation(selectedContactId);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка отправки файла: " + ex.Message);
				}
			}
		}

		// Обработчик клика по чипу (подсказке)
		private void Suggestion_Click(object sender, MouseButtonEventArgs e)
		{
			if (sender is Border border && border.Child is TextBlock textBlock)
			{
				MessageTextBox.Text = textBlock.Text;
				SuggestionPanel.Visibility = Visibility.Collapsed;
			}
		}

		// Если пользователь начинает вводить текст, скрываем панель подсказок
		private void MessageTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (SuggestionPanel.Visibility == Visibility.Visible && !string.IsNullOrEmpty(MessageTextBox.Text))
			{
				SuggestionPanel.Visibility = Visibility.Collapsed;
			}
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}

	// Класс контакта
	public class ContactInfo
	{
		public int UserId { get; set; }
		public string Login { get; set; }
		public string Role { get; set; }
		public int UnreadCount { get; set; }
		public string Status => UnreadCount > 0 ? UnreadCount.ToString() : "✓";
	}
}
