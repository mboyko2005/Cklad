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

		public void ShowWindow()
		{
			Show();
		}

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

		private void ManageUsers_Click(object sender, RoutedEventArgs e)
		{
			ManageUsersWindow usersWindow = new ManageUsersWindow
			{
				Owner = this
			};
			usersWindow.ShowDialog(); // Открытие как модального окна
		}

		private void ManageInventory_Click(object sender, RoutedEventArgs e)
		{
			ManageInventoryWindow inventoryWindow = new ManageInventoryWindow
			{
				Owner = this
			};
			inventoryWindow.ShowDialog();
		}

		private void Reports_Click(object sender, RoutedEventArgs e)
		{
			ReportsWindow reportsWindow = new ReportsWindow
			{
				Owner = this
			};
			reportsWindow.ShowDialog();
		}

		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			SettingsWindow settingsWindow = new SettingsWindow
			{
				Owner = this
			};
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
				ThemeIcon.Kind = ThemeManager.IsDarkTheme ? PackIconMaterialKind.WeatherNight : PackIconMaterialKind.WeatherSunny;
			}
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}
	}
}
