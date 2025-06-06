﻿using System.Windows;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;
using УправлениеСкладом.Менеджер;
using System.Data.SqlClient;

namespace УправлениеСкладом
{
	public partial class ManagerWindow : Window, IRoleWindow, IThemeable
	{
		public ManagerWindow()
		{
			InitializeComponent();
			UpdateThemeIcon();
		}

		// Реализация метода ShowWindow из интерфейса IRoleWindow
		public void ShowWindow()
		{
			this.Show();
		}

		// Метод для обработки события при нажатии на окно (перемещение окна)
		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}

		// Метод для обработки нажатия на кнопку "Закрыть"
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow mainWindow = new MainWindow();
			mainWindow.Show();
			this.Close();
		}

		// Метод для переключения темы
		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			ThemeManager.ToggleTheme();
			UpdateThemeIcon();
		}

		// Обновление иконки темы
		public void UpdateThemeIcon()
		{
			if (ThemeIcon != null)
			{
				ThemeIcon.Kind = ThemeManager.IsDarkTheme
					? PackIconMaterialKind.WeatherNight
					: PackIconMaterialKind.WeatherSunny;
			}
		}

		// Метод для обработки нажатия на кнопку "Управление заказами"
		private void ManageOrders_Click(object sender, RoutedEventArgs e)
		{
			// Открытие окна управления заказами
			ManageOrdersWindow manageOrdersWindow = new ManageOrdersWindow();
			manageOrdersWindow.Owner = this;
			manageOrdersWindow.ShowDialog();
		}

		// Метод для обработки нажатия на кнопку "Управление сотрудниками"
		private void ManageClients_Click(object sender, RoutedEventArgs e)
		{
			// Открытие окна управления сотрудниками склада
			ManageEmployeesWindow manageEmployeesWindow = new ManageEmployeesWindow();
			manageEmployeesWindow.Owner = this;
			manageEmployeesWindow.ShowDialog();
		}

		// Метод для обработки нажатия на кнопку "Просмотр отчетов"
		private void ViewReports_Click(object sender, RoutedEventArgs e)
		{
			// Открытие окна просмотра отчетов
			ReportsWindow reportsWindow = new ReportsWindow();
			reportsWindow.Owner = this;
			reportsWindow.ShowDialog();
		}

		// Метод для обработки нажатия на кнопку "Настройки"
		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			// Открытие окна настроек
			SettingsWindow settingsWindow = new SettingsWindow();
			settingsWindow.Owner = this;
			settingsWindow.ShowDialog();
		}

		// Метод для обработки нажатия на кнопку "Мессенджер"
		private void Messenger_Click(object sender, RoutedEventArgs e)
		{
			// Получаем имя текущего пользователя
			string currentUsername = Application.Current.Properties["CurrentUsername"]?.ToString();
			int userId = 1; // По умолчанию ID=1
			
			// Если имя пользователя известно, находим его ID
			if (!string.IsNullOrEmpty(currentUsername))
			{
				using (SqlConnection connection = new SqlConnection(@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True;TrustServerCertificate=True"))
				{
					try
					{
						connection.Open();
						string query = "SELECT ПользовательID FROM Пользователи WHERE ИмяПользователя = @username";
						using (SqlCommand command = new SqlCommand(query, connection))
						{
							command.Parameters.AddWithValue("@username", currentUsername);
							var result = command.ExecuteScalar();
							if (result != null)
							{
								userId = Convert.ToInt32(result);
							}
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show("Ошибка при получении данных пользователя: " + ex.Message);
					}
				}
			}
			
			// Убедимся, что логин пользователя сохранен в свойствах приложения
			if (!string.IsNullOrEmpty(currentUsername))
			{
				Application.Current.Properties["CurrentUsername"] = currentUsername;
			}
			
			// Открываем окно мессенджера с ID пользователя
			MessengerWindow messengerWindow = new MessengerWindow(userId);
			messengerWindow.Show();
		}
	}
}
