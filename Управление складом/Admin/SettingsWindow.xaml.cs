using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using System.ComponentModel;

namespace УправлениеСкладом
{
	public partial class SettingsWindow : Window, INotifyPropertyChanged
	{
		// Свойства для биндинга
		private string _newPassword;
		public string NewPassword
		{
			get => _newPassword;
			set
			{
				_newPassword = value;
				OnPropertyChanged(nameof(NewPassword));
			}
		}

		private string _confirmPassword;
		public string ConfirmPassword
		{
			get => _confirmPassword;
			set
			{
				_confirmPassword = value;
				OnPropertyChanged(nameof(ConfirmPassword));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		// Строка подключения к базе данных
		private readonly string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		public SettingsWindow()
		{
			InitializeComponent();
			DataContext = this;
			InitializeSettings();

			// Получаем имя текущего пользователя из глобальных свойств приложения
			string currentUsername = GetLoggedInUsername();
			if (!string.IsNullOrWhiteSpace(currentUsername))
			{
				UserManager.LoadCurrentUser(connectionString, currentUsername);
			}
			else
			{
				MessageBox.Show("Не удалось определить текущего пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		// Метод для получения имени пользователя, сохранённого при авторизации
		private string GetLoggedInUsername()
		{
			return Application.Current.Properties["CurrentUsername"]?.ToString() ?? string.Empty;
		}

		private void InitializeSettings()
		{
			ThemeComboBox.SelectedIndex = ThemeManager.IsDarkTheme ? 1 : 0;
			UpdateThemeIcon();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void SaveSettings_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("Настройки успешно сохранены.", "Настройки", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			ThemeManager.ToggleTheme();
			UpdateThemeIcon();
			ThemeComboBox.SelectedIndex = ThemeManager.IsDarkTheme ? 1 : 0;
		}

		public void UpdateThemeIcon()
		{
			if (ThemeIcon != null)
			{
				ThemeIcon.Kind = ThemeManager.IsDarkTheme ? PackIconMaterialKind.WeatherNight : PackIconMaterialKind.WeatherSunny;
			}
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}

		// Обработчики изменения пароля
		private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
		{
			NewPassword = NewPasswordBox.Password;
			ValidatePasswords();
		}

		private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
		{
			ConfirmPassword = ConfirmPasswordBox.Password;
			ValidatePasswords();
		}

		// Метод валидации паролей и обновления иконок
		private void ValidatePasswords()
		{
			bool isNewPasswordValid = !string.IsNullOrWhiteSpace(NewPassword);
			bool isConfirmPasswordValid = !string.IsNullOrWhiteSpace(ConfirmPassword);
			bool doPasswordsMatch = NewPassword == ConfirmPassword;

			var newPasswordStatusIcon = (PackIconMaterial)NewPasswordBox.Template.FindName("NewPasswordStatusIcon", NewPasswordBox);
			var confirmPasswordStatusIcon = (PackIconMaterial)ConfirmPasswordBox.Template.FindName("ConfirmPasswordStatusIcon", ConfirmPasswordBox);

			if (newPasswordStatusIcon != null)
			{
				if (isNewPasswordValid)
				{
					newPasswordStatusIcon.Kind = PackIconMaterialKind.CheckCircleOutline;
					newPasswordStatusIcon.Foreground = System.Windows.Media.Brushes.Green;
					newPasswordStatusIcon.Visibility = Visibility.Visible;
				}
				else
				{
					newPasswordStatusIcon.Kind = PackIconMaterialKind.CloseCircleOutline;
					newPasswordStatusIcon.Foreground = System.Windows.Media.Brushes.Red;
					newPasswordStatusIcon.Visibility = Visibility.Visible;
				}
			}

			if (confirmPasswordStatusIcon != null)
			{
				if (isConfirmPasswordValid && doPasswordsMatch)
				{
					confirmPasswordStatusIcon.Kind = PackIconMaterialKind.CheckCircleOutline;
					confirmPasswordStatusIcon.Foreground = System.Windows.Media.Brushes.Green;
					confirmPasswordStatusIcon.Visibility = Visibility.Visible;
				}
				else
				{
					confirmPasswordStatusIcon.Kind = PackIconMaterialKind.CloseCircleOutline;
					confirmPasswordStatusIcon.Foreground = System.Windows.Media.Brushes.Red;
					confirmPasswordStatusIcon.Visibility = Visibility.Visible;
				}
			}
		}

		private void SavePassword_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword))
			{
				MessageBox.Show("Пароль не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (NewPassword != ConfirmPassword)
			{
				MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (UserManager.CurrentUser == null)
			{
				MessageBox.Show("Не удалось определить текущего пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			bool isChanged = UserManager.ChangePassword(connectionString, NewPassword);
			if (isChanged)
			{
				MessageBox.Show("Пароль успешно изменен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
				NewPasswordBox.Password = string.Empty;
				ConfirmPasswordBox.Password = string.Empty;

				var newPasswordStatusIcon = (PackIconMaterial)NewPasswordBox.Template.FindName("NewPasswordStatusIcon", NewPasswordBox);
				var confirmPasswordStatusIcon = (PackIconMaterial)ConfirmPasswordBox.Template.FindName("ConfirmPasswordStatusIcon", ConfirmPasswordBox);

				if (newPasswordStatusIcon != null)
					newPasswordStatusIcon.Visibility = Visibility.Collapsed;
				if (confirmPasswordStatusIcon != null)
					confirmPasswordStatusIcon.Visibility = Visibility.Collapsed;
			}
			else
			{
				MessageBox.Show("Не удалось изменить пароль. Пожалуйста, попробуйте снова.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ThemeComboBox.SelectedItem is ComboBoxItem selectedItem)
			{
				string selectedTheme = selectedItem.Content.ToString();
				if (selectedTheme == "Светлая" && ThemeManager.IsDarkTheme)
				{
					ThemeManager.ToggleTheme();
					UpdateThemeIcon();
				}
				else if (selectedTheme == "Тёмная" && !ThemeManager.IsDarkTheme)
				{
					ThemeManager.ToggleTheme();
					UpdateThemeIcon();
				}
			}
		}

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		/// <summary>
		/// Класс для управления пользователями.
		/// </summary>
		public static class UserManager
		{
			public static User CurrentUser { get; set; }

			public class User
			{
				public string Username { get; set; }
				public int RoleID { get; set; }
			}

			/// <summary>
			/// Загружает данные пользователя по заданному имени.
			/// </summary>
			public static void LoadCurrentUser(string connectionString, string username)
			{
				try
				{
					using (SqlConnection conn = new SqlConnection(connectionString))
					{
						conn.Open();
						string query = "SELECT ИмяПользователя, РольID FROM Пользователи WHERE ИмяПользователя = @ИмяПользователя";
						using (SqlCommand cmd = new SqlCommand(query, conn))
						{
							cmd.Parameters.AddWithValue("@ИмяПользователя", username);
							using (SqlDataReader reader = cmd.ExecuteReader())
							{
								if (reader.Read())
								{
									CurrentUser = new User
									{
										Username = reader["ИмяПользователя"].ToString(),
										RoleID = Convert.ToInt32(reader["РольID"])
									};
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка при загрузке текущего пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}

			/// <summary>
			/// Меняет пароль текущего пользователя в базе.
			/// </summary>
			public static bool ChangePassword(string connectionString, string newPassword)
			{
				if (CurrentUser == null)
					return false;

				try
				{
					using (SqlConnection conn = new SqlConnection(connectionString))
					{
						conn.Open();
						string query = "UPDATE Пользователи SET Пароль = @Пароль WHERE ИмяПользователя = @ИмяПользователя";
						using (SqlCommand cmd = new SqlCommand(query, conn))
						{
							cmd.Parameters.AddWithValue("@Пароль", newPassword);
							cmd.Parameters.AddWithValue("@ИмяПользователя", CurrentUser.Username);
							int rowsAffected = cmd.ExecuteNonQuery();
							return rowsAffected > 0;
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка при смене пароля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					return false;
				}
			}
		}
	}
}
