using MahApps.Metro.IconPacks;
using System.Windows;
using System.Windows.Input;
using Управление_складом.Themes;
using System.Data.SqlClient;

namespace УправлениеСкладом.Сотрудник_склада
{
	public partial class WarehouseStaffWindow : Window, IRoleWindow, IThemeable
	{
		public WarehouseStaffWindow()
		{
			InitializeComponent();
			UpdateThemeIcon();
		}

		public void ShowWindow()
		{
			this.Show();
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
				this.DragMove();
		}

		// При закрытии окна открывается MainWindow
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow mainWindow = new MainWindow();
			mainWindow.Show();
			this.Close();
		}

		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			ThemeManager.ToggleTheme();
			UpdateThemeIcon();
		}

		public void UpdateThemeIcon()
		{
			if (ThemeIcon != null)
			{
				ThemeIcon.Kind = ThemeManager.IsDarkTheme ? PackIconMaterialKind.WeatherNight : PackIconMaterialKind.WeatherSunny;
			}
		}

		private void ViewItems_Click(object sender, RoutedEventArgs e)
		{
			ViewItemsWindow viewItemsWindow = new ViewItemsWindow();
			viewItemsWindow.Owner = this;
			viewItemsWindow.ShowDialog();
		}

		private void ManageStock_Click(object sender, RoutedEventArgs e)
		{
			ManageStockWindow manageStockWindow = new ManageStockWindow();
			manageStockWindow.Owner = this;
			manageStockWindow.ShowDialog();
		}

		private void MoveItems_Click(object sender, RoutedEventArgs e)
		{
			MoveItemsWindow moveItemsWindow = new MoveItemsWindow();
			moveItemsWindow.Owner = this;
			moveItemsWindow.ShowDialog();
		}

		private void InventoryLog_Click(object sender, RoutedEventArgs e)
		{
			InventoryLogWindow inventoryLogWindow = new InventoryLogWindow();
			inventoryLogWindow.Owner = this;
			inventoryLogWindow.ShowDialog();
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
