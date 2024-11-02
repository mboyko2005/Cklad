using System.Windows;
using System.Windows.Input;

namespace УправлениеСкладом
{
	public partial class WarehouseStaffWindow : Window, IRoleWindow
	{
		public WarehouseStaffWindow()
		{
			InitializeComponent();
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

		// Метод для обработки нажатия на кнопку "Переключение темы"
		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			// Логика переключения темы
		}

		// Метод для обработки нажатия на кнопку "Просмотр товаров"
		private void ViewItems_Click(object sender, RoutedEventArgs e)
		{
			// Логика просмотра товаров
		}

		// Метод для обработки нажатия на кнопку "Управление складом"
		private void ManageStock_Click(object sender, RoutedEventArgs e)
		{
			// Логика управления складом
		}

		// Метод для обработки нажатия на кнопку "Перемещение товаров"
		private void MoveItems_Click(object sender, RoutedEventArgs e)
		{
			// Логика перемещения товаров
		}

		// Метод для обработки нажатия на кнопку "Журнал инвентаризации"
		private void InventoryLog_Click(object sender, RoutedEventArgs e)
		{
			// Логика для журнала инвентаризации
		}

		public void ShowWindow()
		{
			Show();
		}
	}
}
