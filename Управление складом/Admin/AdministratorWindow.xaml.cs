using System.Windows;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;
using Управление_складом.Админ;

namespace УправлениеСкладом
{
	/// <summary>
	/// Окно администратора с функционалом управления пользователями, инвентарем, отчетами, настройками, ботом и проверкой API.
	/// </summary>
	public partial class AdministratorWindow : Window, IRoleWindow, IThemeable
	{
		public AdministratorWindow()
		{
			InitializeComponent();
			UpdateThemeIcon();
		}

		// Отображает окно администратора.
		public void ShowWindow() => Show();

		// Обработчик нажатия кнопки закрытия.
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow mainWindow = new MainWindow();
			mainWindow.Show();
			this.Close();
		}

		// Обработчик нажатия кнопки управления пользователями.
		private void ManageUsers_Click(object sender, RoutedEventArgs e)
		{
			ShowDialogWindow(new ManageUsersWindow());
		}

		// Обработчик нажатия кнопки управления инвентарем.
		private void ManageInventory_Click(object sender, RoutedEventArgs e)
		{
			ShowDialogWindow(new ManageInventoryWindow());
		}

		// Обработчик нажатия кнопки отчетов.
		private void Reports_Click(object sender, RoutedEventArgs e)
		{
			ShowDialogWindow(new ReportsWindow());
		}

		// Обработчик нажатия кнопки настроек.
		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			ShowDialogWindow(new SettingsWindow());
		}

		// Обработчик нажатия кнопки проверки API.
		private void CheckApiButton_Click(object sender, RoutedEventArgs e)
		{
			ApiStatusWindow statusWindow = new ApiStatusWindow();
			statusWindow.Owner = this;
			statusWindow.ShowDialog();
		}

		// Обработчик нажатия кнопки управления ботом.
		private void ManageBot_Click(object sender, RoutedEventArgs e)
		{
			var manageBotWindow = new ManageBotWindow();
			manageBotWindow.Show();
			this.Close();
		}

		// Обработчик нажатия кнопки переключения темы.
		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			ThemeManager.ToggleTheme();
			UpdateThemeIcon();
		}

		// Обновляет иконку темы.
		public void UpdateThemeIcon()
		{
			if (ThemeIcon != null)
			{
				ThemeIcon.Kind = ThemeManager.IsDarkTheme
					? PackIconMaterialKind.WeatherNight
					: PackIconMaterialKind.WeatherSunny;
			}
		}

		// Позволяет перетаскивать окно.
		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}

		#region Вспомогательные методы

		// Устанавливает владельца для переданного окна и открывает его как модальный диалог.
		private void ShowDialogWindow(Window window)
		{
			window.Owner = this;
			window.ShowDialog();
		}

		#endregion
	}
}
