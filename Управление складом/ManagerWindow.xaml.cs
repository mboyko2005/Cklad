using System.Windows;
using System.Windows.Input;

namespace УправлениеСкладом
{
	public partial class ManagerWindow : Window, IRoleWindow
	{
		public ManagerWindow()
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
			// Логика для настроек
		}

		public void ShowWindow()
		{
			Show();
		}
	}
}
