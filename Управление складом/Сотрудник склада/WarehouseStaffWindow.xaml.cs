using System.Windows;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;

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

		// Метод для обработки события при нажатии на окно (перемещение окна)
		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
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

		// Метод для обработки нажатия на кнопку "Просмотр товаров"
		private void ViewItems_Click(object sender, RoutedEventArgs e)
		{
			ViewItemsWindow viewItemsWindow = new ViewItemsWindow();
			viewItemsWindow.Owner = this; // Устанавливаем владельца окна
			viewItemsWindow.ShowDialog(); // Открываем как диалоговое окно
		}

		// Метод для обработки нажатия на кнопку "Управление запасами"
		private void ManageStock_Click(object sender, RoutedEventArgs e)
		{
			// Логика управления запасами
		}

		// Метод для обработки нажатия на кнопку "Перемещение товаров"
		private void MoveItems_Click(object sender, RoutedEventArgs e)
		{
			// Логика перемещения товаров
		}

		// Метод для обработки нажатия на кнопку "Учёт приходов/расходов"
		private void InventoryLog_Click(object sender, RoutedEventArgs e)
		{
			// Логика для журнала инвентаризации
		}
	}
}
