using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;

namespace УправлениеСкладом
{
	public partial class ManageUsersWindow : Window, IThemeable
	{
		// Пример модели пользователя с виртуальным ID для отображения
		public class User
		{
			public int DisplayId { get; set; } // Виртуальный ID для отображения
			public int Id { get; set; } // Реальный ID из базы данных
			public string Name { get; set; }
			public string Role { get; set; }
		}

		// Пример строки подключения к базе данных
		private static string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		public ManageUsersWindow()
		{
			InitializeComponent();
			LoadUsers();
		}

		private void LoadUsers()
		{
			// Создаем список пользователей
			List<User> users = new List<User>();

			// Подключаемся к базе данных и выполняем запрос
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = @"
                        SELECT Пользователи.ПользовательID, Пользователи.ИмяПользователя, Роли.Наименование AS Role
                        FROM Пользователи
                        INNER JOIN Роли ON Пользователи.РольID = Роли.РольID
                        ORDER BY Пользователи.ПользовательID";

					SqlCommand command = new SqlCommand(query, connection);
					SqlDataReader reader = command.ExecuteReader();

					// Читаем данные из результата запроса и добавляем виртуальный ID
					int displayId = 1;
					while (reader.Read())
					{
						users.Add(new User
						{
							DisplayId = displayId++,
							Id = reader.GetInt32(0),
							Name = reader.GetString(1),
							Role = reader.GetString(2)
						});
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}

			// Привязываем список пользователей к DataGrid
			UsersDataGrid.ItemsSource = users;
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void AddUser_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("Функция добавления пользователя ещё не реализована.", "Добавить пользователя", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void EditUser_Click(object sender, RoutedEventArgs e)
		{
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
			if (UsersDataGrid.SelectedItem is User selectedUser)
			{
				MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить пользователя: {selectedUser.Name}?",
					"Удалить пользователя", MessageBoxButton.YesNo, MessageBoxImage.Warning);

				if (result == MessageBoxResult.Yes)
				{
					using (SqlConnection connection = new SqlConnection(connectionString))
					{
						try
						{
							connection.Open();

							// Удаление связанных записей из таблицы ДвиженияТоваров
							string deleteRelatedQuery = "DELETE FROM ДвиженияТоваров WHERE ПользовательID = @UserId";
							SqlCommand deleteRelatedCommand = new SqlCommand(deleteRelatedQuery, connection);
							deleteRelatedCommand.Parameters.AddWithValue("@UserId", selectedUser.Id);
							deleteRelatedCommand.ExecuteNonQuery();

							// Удаление пользователя
							string deleteQuery = "DELETE FROM Пользователи WHERE ПользовательID = @UserId";
							SqlCommand deleteCommand = new SqlCommand(deleteQuery, connection);
							deleteCommand.Parameters.AddWithValue("@UserId", selectedUser.Id);
							deleteCommand.ExecuteNonQuery();
						}
						catch (Exception ex)
						{
							MessageBox.Show($"Ошибка при удалении пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}

					// Обновляем список пользователей после удаления
					LoadUsers();
				}
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
