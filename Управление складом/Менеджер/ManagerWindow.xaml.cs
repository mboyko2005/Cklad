// ManagerWindow.xaml.cs
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;

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
				ThemeIcon.Kind = ThemeManager.IsDarkTheme ? PackIconMaterialKind.WeatherNight : PackIconMaterialKind.WeatherSunny;
			}
		}

		// Метод для обработки нажатия на кнопку "Управление заказами"
		private void ManageOrders_Click(object sender, RoutedEventArgs e)
		{
			// Логика управления заказами
		}

		// Метод для обработки нажатия на кнопку "Управление клиентами"
		private void ManageClients_Click(object sender, RoutedEventArgs e)
		{
			// Логика управления клиентами
		}

		// Метод для обработки нажатия на кнопку "Просмотр отчетов"
		private void ViewReports_Click(object sender, RoutedEventArgs e)
		{
			// Логика просмотра отчетов
		}

		// Метод для обработки нажатия на кнопку "Настройки"
		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			// Открытие окна настроек
			SettingsWindow settingsWindow = new SettingsWindow();
			settingsWindow.Owner = this;
			settingsWindow.ShowDialog();
		}
	}
}
