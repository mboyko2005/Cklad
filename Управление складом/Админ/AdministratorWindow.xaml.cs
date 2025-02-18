using System.Windows;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;

namespace УправлениеСкладом
{
	public partial class AdministratorWindow : Window, IRoleWindow, IThemeable
	{
		public AdministratorWindow()
		{
			InitializeComponent();
			UpdateThemeIcon();
		}

		public void ShowWindow() => Show();

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
			MainWindow mainWindow = new MainWindow();
			mainWindow.Show();
			this.Close();
		}

		private void ManageUsers_Click(object sender, RoutedEventArgs e)
		{
			var usersWindow = new ManageUsersWindow { Owner = this };
			usersWindow.ShowDialog();
		}

		private void ManageInventory_Click(object sender, RoutedEventArgs e)
		{
			var inventoryWindow = new ManageInventoryWindow { Owner = this };
			inventoryWindow.ShowDialog();
		}

		private void Reports_Click(object sender, RoutedEventArgs e)
		{
			var reportsWindow = new ReportsWindow { Owner = this };
			reportsWindow.ShowDialog();
		}

		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			var settingsWindow = new SettingsWindow { Owner = this };
			settingsWindow.ShowDialog();
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
				ThemeIcon.Kind = ThemeManager.IsDarkTheme
					? PackIconMaterialKind.WeatherNight
					: PackIconMaterialKind.WeatherSunny;
			}
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				DragMove();
			}
		}
	}
}