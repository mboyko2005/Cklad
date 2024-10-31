// SettingsWindow.xaml.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;

namespace УправлениеСкладом
{
	public partial class SettingsWindow : Window, IThemeable
	{
		public SettingsWindow()
		{
			InitializeComponent();
			// Инициализация настроек, если необходимо
			InitializeSettings();
		}

		private void InitializeSettings()
		{
			// Установка текущей темы в ComboBox
			ThemeComboBox.SelectedIndex = ThemeManager.IsDarkTheme ? 1 : 0;
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void SaveSettings_Click(object sender, RoutedEventArgs e)
		{
			string newPassword = NewPasswordBox.Password;
			string selectedTheme = (ThemeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

			if (string.IsNullOrWhiteSpace(newPassword))
			{
				MessageBox.Show("Пароль не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (string.IsNullOrWhiteSpace(selectedTheme))
			{
				MessageBox.Show("Пожалуйста, выберите тему приложения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			// Логика сохранения настроек
			// Например, обновление в базе данных или файле конфигурации
			MessageBox.Show("Настройки успешно сохранены.", "Настройки", MessageBoxButton.OK, MessageBoxImage.Information);

			// Применение выбранной темы
			if (selectedTheme == "Светлая" && ThemeManager.IsDarkTheme)
			{
				ThemeManager.ToggleTheme();
			}
			else if (selectedTheme == "Тёмная" && !ThemeManager.IsDarkTheme)
			{
				ThemeManager.ToggleTheme();
			}
		}

		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			ThemeManager.ToggleTheme();
			UpdateThemeIcon();
			// Обновление ComboBox при ручном переключении темы
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
	}
}
