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
	/// Логика взаимодействия для ManageBotWindow.xaml
	/// </summary>
	public partial class ManageBotWindow : Window
	{
		// Тот же connection string, что и в TelegramNotifier
		private static readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True;TrustServerCertificate=True";

		// Хранит выбранный TelegramUserID (из базы), если пользователь выбран для обновления
		private long? _selectedTelegramUserId = null;

		public ManageBotWindow()
		{
			InitializeComponent();
			RefreshUsersList();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			// 1. Создаём окно администратора
			var adminWindow = new AdministratorWindow();

			// 2. Делаем его главным окном приложения (чтобы при закрытии этого окна приложение не завершалось)
			Application.Current.MainWindow = adminWindow;

			// 3. Показываем окно администратора
			adminWindow.Show();

			// 4. Закрываем текущее окно (ManageBotWindow)
			this.Close();
		}

		private void AddUser_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(TelegramIdTextBox.Text))
			{
				MessageBox.Show("Введите Telegram ID.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (RoleComboBox.SelectedItem == null)
			{
				MessageBox.Show("Выберите роль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (!long.TryParse(TelegramIdTextBox.Text, out long telegramId))
			{
				MessageBox.Show("Telegram ID должен быть числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			string role = ((ComboBoxItem)RoleComboBox.SelectedItem).Content.ToString();

			try
			{
				using (SqlConnection conn = new SqlConnection(_connectionString))
				{
					conn.Open();
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

		private void UpdateUser_Click(object sender, RoutedEventArgs e)
		{
			if (!_selectedTelegramUserId.HasValue)
			{
				MessageBox.Show("Выберите пользователя для обновления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (string.IsNullOrWhiteSpace(TelegramIdTextBox.Text))
			{
				MessageBox.Show("Введите Telegram ID.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (RoleComboBox.SelectedItem == null)
			{
				MessageBox.Show("Выберите роль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (!long.TryParse(TelegramIdTextBox.Text, out long newTelegramId))
			{
				MessageBox.Show("Telegram ID должен быть числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			string newRole = ((ComboBoxItem)RoleComboBox.SelectedItem).Content.ToString();

			try
			{
				using (SqlConnection conn = new SqlConnection(_connectionString))
				{
					conn.Open();
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

		private void RefreshUsersList()
		{
			UsersListBox.Items.Clear();
			_selectedTelegramUserId = null;
			try
			{
				using (SqlConnection conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = "SELECT TelegramUserID, Роль FROM TelegramUsers";
					using (SqlCommand cmd = new SqlCommand(query, conn))
					using (SqlDataReader reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							long userId = reader.GetInt64(0);
							string role = reader.GetString(1);
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
						string idPart = parts[0].Trim();
						string rolePart = parts[1].Trim();
						if (idPart.StartsWith("ID:"))
						{
							string idStr = idPart.Substring("ID:".Length).Trim();
							if (long.TryParse(idStr, out long userId))
							{
								_selectedTelegramUserId = userId;
								TelegramIdTextBox.Text = userId.ToString();
							}
						}
						if (rolePart.StartsWith("Роль:"))
						{
							string roleStr = rolePart.Substring("Роль:".Length).Trim();
							foreach (var item in RoleComboBox.Items)
							{
								if (item is ComboBoxItem comboBoxItem)
								{
									if (comboBoxItem.Content.ToString().Equals(roleStr, StringComparison.OrdinalIgnoreCase))
									{
										RoleComboBox.SelectedItem = comboBoxItem;
										break;
									}
								}
							}
						}
					}
				}
				catch { }
			}
		}

		private void ClearForm()
		{
			TelegramIdTextBox.Text = "";
			RoleComboBox.SelectedItem = null;
			_selectedTelegramUserId = null;
			UsersListBox.SelectedItem = null;
		}

		/// <summary>
		/// При клике вне ListBox (на пустое место), очищаем форму. Также DragMove для перетаскивания окна.
		/// </summary>
		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				if (!IsClickInsideElement(UsersListBox, e.OriginalSource as DependencyObject))
				{
					ClearForm();
				}
				try
				{
					DragMove();
				}
				catch { }
			}
		}

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
