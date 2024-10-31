using System.Windows;
using System.Windows.Input;

namespace УправлениеСкладом
{
	public partial class AdministratorWindow : Window, IRoleWindow
	{
		public AdministratorWindow()
		{
			InitializeComponent();
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
			// Логика для открытия окна управления пользователями
			MessageBox.Show("Открыть управление пользователями", "Управление пользователями", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void ManageInventory_Click(object sender, RoutedEventArgs e)
		{
			// Логика для открытия окна управления складскими позициями
			MessageBox.Show("Открыть управление складскими позициями", "Управление складскими позициями", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void Reports_Click(object sender, RoutedEventArgs e)
		{
			// Логика для открытия окна отчётов
			MessageBox.Show("Открыть отчёты", "Отчёты", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			// Логика для открытия окна настроек
			MessageBox.Show("Открыть настройки", "Настройки", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}
	}
}
