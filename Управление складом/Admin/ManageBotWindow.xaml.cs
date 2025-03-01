using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using УправлениеСкладом; // Для доступа к AdministratorWindow

namespace Управление_складом.Админ
{
	/// <summary>
	/// Логика взаимодействия для ManageBotWindow.xaml.
	/// Окно для управления Telegram-пользователями (для бота).
	/// </summary>
	public partial class ManageBotWindow : Window
	{
		private static readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True;TrustServerCertificate=True";

		// Хранит выбранный TelegramUserID (из базы), если пользователь выбран для обновления.
		private long? _selectedTelegramUserId = null;

		/// <summary>
		/// Конструктор окна.
		/// Инициализирует компоненты и обновляет список пользователей.
		/// </summary>
		public ManageBotWindow()
		{
			InitializeComponent();
			RefreshUsersList();
		}

		/// <summary>
		/// Обработчик нажатия кнопки закрытия окна.
		/// Создаёт окно администратора, устанавливает его как главное и закрывает текущее окно.
		/// </summary>
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			// 1. Создаём окно администратора.
			var adminWindow = new AdministratorWindow();
			// 2. Делаем его главным окном приложения (чтобы при закрытии этого окна приложение не завершалось).
			Application.Current.MainWindow = adminWindow;
			// 3. Показываем окно администратора.
			adminWindow.Show();
			// 4. Закрываем текущее окно.
			this.Close();
		}

		/// <summary>
		/// Обработчик нажатия кнопки добавления нового пользователя.
		/// Валидирует ввод и добавляет пользователя в базу данных.
		/// </summary>
		private void AddUser_Click(object sender, RoutedEventArgs e)
		{
			// Проверка корректности ввода и получение значений.
			if (!TryGetUserInputs(out long telegramId, out string role))
				return;

			try
			{
				using (SqlConnection conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					// Запрос для добавления нового пользователя.
					string query = "INSERT INTO TelegramUsers (TelegramUserID, Роль) VALUES (@TelegramUserID, @Role)";
					using (SqlCommand cmd = new SqlCommand(query, conn))
					{
						cmd.Parameters.AddWithValue("@TelegramUserID", telegramId);
						cmd.Parameters.AddWithValue("@Role", role);
						int rows = cmd.ExecuteNonQuery();
						if (rows > 0)
						{
							MessageBox.Show("Пользователь успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
							RefreshUsersList();
							ClearForm();
						}
						else
						{
							MessageBox.Show("Не удалось добавить пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// Обработчик нажатия кнопки обновления выбранного пользователя.
		/// Валидирует ввод и обновляет данные пользователя в базе данных.
		/// </summary>
		private void UpdateUser_Click(object sender, RoutedEventArgs e)
		{
			// Проверка, выбран ли пользователь для обновления.
			if (!_selectedTelegramUserId.HasValue)
			{
				MessageBox.Show("Выберите пользователя для обновления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// Получаем новые данные из формы.
			if (!TryGetUserInputs(out long newTelegramId, out string newRole))
				return;

			try
			{
				using (SqlConnection conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					// Запрос для обновления данных пользователя.
					string query = "UPDATE TelegramUsers SET TelegramUserID = @NewTelegramUserID, Роль = @Role WHERE TelegramUserID = @OldTelegramUserID";
					using (SqlCommand cmd = new SqlCommand(query, conn))
					{
						cmd.Parameters.AddWithValue("@NewTelegramUserID", newTelegramId);
						cmd.Parameters.AddWithValue("@Role", newRole);
						cmd.Parameters.AddWithValue("@OldTelegramUserID", _selectedTelegramUserId.Value);
						int rows = cmd.ExecuteNonQuery();
						if (rows > 0)
						{
							MessageBox.Show("Данные пользователя обновлены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
							RefreshUsersList();
							ClearForm();
						}
						else
						{
							MessageBox.Show("Не удалось обновить данные пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// Обработчик нажатия кнопки удаления выбранного пользователя.
		/// Удаляет пользователя из базы данных после подтверждения.
		/// </summary>
		private void DeleteUser_Click(object sender, RoutedEventArgs e)
		{
			// Проверяем, выбран ли пользователь.
			if (!_selectedTelegramUserId.HasValue)
			{
				MessageBox.Show("Выберите пользователя для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// Подтверждение удаления.
			var result = MessageBox.Show("Вы действительно хотите удалить выбранного пользователя?",
										  "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
			if (result != MessageBoxResult.Yes)
				return;

			try
			{
				using (SqlConnection conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					// Запрос для удаления пользователя.
					string query = "DELETE FROM TelegramUsers WHERE TelegramUserID = @TelegramUserID";
					using (SqlCommand cmd = new SqlCommand(query, conn))
					{
						cmd.Parameters.AddWithValue("@TelegramUserID", _selectedTelegramUserId.Value);
						int rows = cmd.ExecuteNonQuery();
						if (rows > 0)
						{
							MessageBox.Show("Пользователь успешно удалён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
							RefreshUsersList();
							ClearForm();
						}
						else
						{
							MessageBox.Show("Не удалось удалить пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// Обновляет список пользователей, полученных из базы данных, и отображает их в ListBox.
		/// </summary>
		private void RefreshUsersList()
		{
			UsersListBox.Items.Clear();
			_selectedTelegramUserId = null;

			try
			{
				using (SqlConnection conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					// Запрос для получения списка пользователей.
					string query = "SELECT TelegramUserID, Роль FROM TelegramUsers";
					using (SqlCommand cmd = new SqlCommand(query, conn))
					using (SqlDataReader reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							long userId = reader.GetInt64(0);
							string role = reader.GetString(1);
							// Форматируем строку для отображения: "ID: {userId} | Роль: {role}".
							UsersListBox.Items.Add($"ID: {userId} | Роль: {role}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при получении списка пользователей: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// Обработчик изменения выбранного элемента в ListBox.
		/// При выборе пользователя данные его Telegram ID и роли заполняются в соответствующие поля формы.
		/// </summary>
		private void UsersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (UsersListBox.SelectedItem != null)
			{
				string selectedText = UsersListBox.SelectedItem.ToString();
				// Ожидаемый формат: "ID: {userId} | Роль: {role}"
				try
				{
					string[] parts = selectedText.Split('|');
					if (parts.Length >= 2)
					{
						// Обработка части с ID.
						string idPart = parts[0].Trim();
						if (idPart.StartsWith("ID:"))
						{
							string idStr = idPart.Substring("ID:".Length).Trim();
							if (long.TryParse(idStr, out long userId))
							{
								_selectedTelegramUserId = userId;
								TelegramIdTextBox.Text = userId.ToString();
							}
						}
						// Обработка части с ролью.
						string rolePart = parts[1].Trim();
						if (rolePart.StartsWith("Роль:"))
						{
							string roleStr = rolePart.Substring("Роль:".Length).Trim();
							foreach (var item in RoleComboBox.Items)
							{
								if (item is ComboBoxItem comboBoxItem &&
									comboBoxItem.Content.ToString().Equals(roleStr, StringComparison.OrdinalIgnoreCase))
								{
									RoleComboBox.SelectedItem = comboBoxItem;
									break;
								}
							}
						}
					}
				}
				catch
				{
					// Если произошла ошибка при разборе строки, игнорируем её.
				}
			}
		}

		/// <summary>
		/// Очищает поля ввода формы и сбрасывает выбранного пользователя.
		/// </summary>
		private void ClearForm()
		{
			TelegramIdTextBox.Text = "";
			RoleComboBox.SelectedItem = null;
			_selectedTelegramUserId = null;
			UsersListBox.SelectedItem = null;
		}

		/// <summary>
		/// Валидирует ввод пользователя в форму.
		/// Проверяет наличие Telegram ID, выбранной роли и корректность формата Telegram ID.
		/// </summary>
		/// <param name="telegramId">Выходной параметр для Telegram ID.</param>
		/// <param name="role">Выходной параметр для выбранной роли.</param>
		/// <returns>True, если ввод корректен, иначе False.</returns>
		private bool TryGetUserInputs(out long telegramId, out string role)
		{
			telegramId = 0;
			role = string.Empty;

			if (string.IsNullOrWhiteSpace(TelegramIdTextBox.Text))
			{
				MessageBox.Show("Введите Telegram ID.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			if (RoleComboBox.SelectedItem == null)
			{
				MessageBox.Show("Выберите роль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			if (!long.TryParse(TelegramIdTextBox.Text, out telegramId))
			{
				MessageBox.Show("Telegram ID должен быть числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
			role = ((ComboBoxItem)RoleComboBox.SelectedItem).Content.ToString();
			return true;
		}

		/// <summary>
		/// Обработчик события нажатия кнопки мыши на окне.
		/// Если клик вне ListBox, очищает форму. Также позволяет перетаскивать окно.
		/// </summary>
		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				// Если клик не внутри UsersListBox, очищаем форму.
				if (!IsClickInsideElement(UsersListBox, e.OriginalSource as DependencyObject))
				{
					ClearForm();
				}
				try
				{
					DragMove();
				}
				catch
				{
					// Игнорируем возможные ошибки перетаскивания.
				}
			}
		}

		/// <summary>
		/// Проверяет, находится ли клик внутри заданного элемента.
		/// </summary>
		/// <param name="parentElement">Элемент, относительно которого проверяется клик.</param>
		/// <param name="clickedObject">Объект, по которому произведён клик.</param>
		/// <returns>True, если клик был внутри элемента, иначе False.</returns>
		private bool IsClickInsideElement(FrameworkElement parentElement, DependencyObject clickedObject)
		{
			if (clickedObject == null || parentElement == null)
				return false;

			DependencyObject current = clickedObject;
			while (current != null)
			{
				if (current == parentElement)
					return true;
				current = VisualTreeHelper.GetParent(current);
			}
			return false;
		}
	}
}
