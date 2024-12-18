
using MahApps.Metro.IconPacks;
using System.Windows;
using System.Windows.Input;
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

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
				this.DragMove();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
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
			manageStockWindow.Owner = this; // Устанавливаем владельца
			manageStockWindow.ShowDialog(); // Открываем окно модально
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

	}
}
