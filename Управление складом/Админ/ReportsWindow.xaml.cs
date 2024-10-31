// ReportsWindow.xaml.cs
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;

namespace УправлениеСкладом
{
	public partial class ReportsWindow : Window, IThemeable
	{
		public ReportsWindow()
		{
			InitializeComponent();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void GenerateReport_Click(object sender, RoutedEventArgs e)
		{
			// Логика генерации отчёта
			MessageBox.Show("Отчёт успешно сгенерирован.", "Отчёты", MessageBoxButton.OK, MessageBoxImage.Information);
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
