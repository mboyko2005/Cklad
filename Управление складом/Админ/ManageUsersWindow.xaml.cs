// ManageUsersWindow.xaml.cs
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;

namespace УправлениеСкладом
{
	public partial class ManageUsersWindow : Window, IThemeable
	{
		// Пример модели пользователя
		public class User
		{
			public int Id { get; set; }
			public string Name { get; set; }
			public string Role { get; set; }
			// Добавьте другие свойства по необходимости
		}

		// Пример списка пользователей
		private List<User> Users;

		public ManageUsersWindow()
		{
			InitializeComponent();
			LoadUsers();
		}

		private void LoadUsers()
		{
			// Здесь должна быть логика загрузки пользователей из базы данных
			// Для примера используем статический список
			Users = new List<User>
			{
				new User { Id = 1, Name = "Иван Иванов", Role = "Администратор" },
				new User { Id = 2, Name = "Мария Петрова", Role = "Менеджер" },
				new User { Id = 3, Name = "Сергей Смирнов", Role = "Сотрудник склада" }
			};

			UsersDataGrid.ItemsSource = Users;
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void AddUser_Click(object sender, RoutedEventArgs e)
		{
			// Логика добавления пользователя
			// Например, открытие диалогового окна для ввода данных нового пользователя
			MessageBox.Show("Функция добавления пользователя ещё не реализована.", "Добавить пользователя", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void EditUser_Click(object sender, RoutedEventArgs e)
		{
			// Логика редактирования выбранного пользователя
			if (UsersDataGrid.SelectedItem is User selectedUser)
			{
				MessageBox.Show($"Редактирование пользователя: {selectedUser.Name}", "Редактировать пользователя", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите пользователя для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void DeleteUser_Click(object sender, RoutedEventArgs e)
		{
			// Логика удаления выбранного пользователя
			if (UsersDataGrid.SelectedItem is User selectedUser)
			{
				MessageBox.Show($"Удаление пользователя: {selectedUser.Name}", "Удалить пользователя", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите пользователя для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
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
