using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;

namespace УправлениеСкладом
{
	public partial class ManageUsersWindow : Window, IThemeable
	{
		// Модель пользователя
		public class User
		{
			public int Id { get; set; }
			public string Name { get; set; }
			public string RoleName { get; set; }
			public int RoleId { get; set; }
		}

		// Модель роли
		public class Role
		{
			public int Id { get; set; }
			public string Name { get; set; }
		}

		private List<User> Users;
		private List<Role> Roles;
		private bool IsEditMode = false;
		private User SelectedUser;
		private string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		public ManageUsersWindow()
		{
			InitializeComponent();
			LoadRoles();
			LoadUsers();
			UpdateThemeIcon();
		}

		// Загрузка ролей из базы данных
		private void LoadRoles()
		{
			Roles = new List<Role>();

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = "SELECT РольID, Наименование FROM Роли";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								Roles.Add(new Role
								{
									Id = reader.GetInt32(0),
									Name = reader.GetString(1)
								});
							}
						}
					}

					// Привязка списка ролей к ComboBox
					RoleComboBox.ItemsSource = Roles;
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки ролей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Загрузка пользователей из базы данных
		private void LoadUsers()
		{
			Users = new List<User>();

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = @"
                            SELECT Пользователи.ПользовательID, Пользователи.ИмяПользователя, Роли.Наименование AS RoleName, Роли.РольID
                            FROM Пользователи
                            INNER JOIN Роли ON Пользователи.РольID = Роли.РольID
                            ORDER BY Пользователи.ПользовательID";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								Users.Add(new User
								{
									Id = reader.GetInt32(0),
									Name = reader.GetString(1),
									RoleName = reader.GetString(2),
									RoleId = reader.GetInt32(3)
								});
							}
						}
					}

					// Привязка списка пользователей к DataGrid
					UsersDataGrid.ItemsSource = Users;
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Обработчик кнопки закрытия окна
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		// Показать панель добавления
		private void AddUser_Click(object sender, RoutedEventArgs e)
		{
			IsEditMode = false;
			PanelTitle.Text = "Добавить пользователя";
			ClearInputFields();
			ShowPanel();
		}

		// Показать панель редактирования
		private void EditUser_Click(object sender, RoutedEventArgs e)
		{
			if (UsersDataGrid.SelectedItem is User selectedUser)
			{
				IsEditMode = true;
				SelectedUser = selectedUser;
				PanelTitle.Text = "Редактировать пользователя";
				PopulateInputFields(selectedUser);
				ShowPanel();
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите пользователя для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Удаление пользователя
		private void DeleteUser_Click(object sender, RoutedEventArgs e)
		{
			if (UsersDataGrid.SelectedItem is User selectedUser)
			{
				MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить пользователя: {selectedUser.Name}?",
					"Удаление пользователя", MessageBoxButton.YesNo, MessageBoxImage.Warning);

				if (result == MessageBoxResult.Yes)
				{
					using (SqlConnection connection = new SqlConnection(connectionString))
					{
						try
						{
							connection.Open();

							// Удаление связанных записей из таблицы ДвиженияТоваров
							string deleteRelatedQuery = "DELETE FROM ДвиженияТоваров WHERE ПользовательID = @UserId";
							using (SqlCommand deleteRelatedCommand = new SqlCommand(deleteRelatedQuery, connection))
							{
								deleteRelatedCommand.Parameters.AddWithValue("@UserId", selectedUser.Id);
								deleteRelatedCommand.ExecuteNonQuery();
							}

							// Удаление пользователя
							string deleteQuery = "DELETE FROM Пользователи WHERE ПользовательID = @UserId";
							using (SqlCommand deleteCommand = new SqlCommand(deleteQuery, connection))
							{
								deleteCommand.Parameters.AddWithValue("@UserId", selectedUser.Id);
								deleteCommand.ExecuteNonQuery();
							}

							// Обновление списка пользователей
							Users.Remove(selectedUser);
							UsersDataGrid.ItemsSource = null;
							UsersDataGrid.ItemsSource = Users;
							MessageBox.Show("Пользователь успешно удалён.", "Удаление пользователя", MessageBoxButton.OK, MessageBoxImage.Information);
						}
						catch (SqlException ex)
						{
							MessageBox.Show($"Ошибка при удалении пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите пользователя для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Показать панель добавления/редактирования
		private void ShowPanel()
		{
			// Установить ширину SlidePanel и начать анимацию
			this.RootGrid.ColumnDefinitions[1].Width = new GridLength(300);
			Storyboard showStoryboard = (Storyboard)FindResource("ShowPanelStoryboard");
			showStoryboard.Begin();
		}

		// Скрыть панель добавления/редактирования
		private void HidePanel()
		{
			Storyboard hideStoryboard = (Storyboard)FindResource("HidePanelStoryboard");
			hideStoryboard.Completed += (s, e) =>
			{
				this.RootGrid.ColumnDefinitions[1].Width = new GridLength(0);
			};
			hideStoryboard.Begin();
		}

		// Обработчик кнопки закрытия панели
		private void ClosePanel_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}

		// Обработчик кнопки отмены
		private void CancelUser_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}

		// Сохранение нового пользователя или обновление существующего
		private void SaveUser_Click(object sender, RoutedEventArgs e)
		{
			if (IsEditMode)
			{
				SaveEditUser();
			}
			else
			{
				SaveAddUser();
			}
		}

		// Сохранение нового пользователя
		private void SaveAddUser()
		{
			string username = UsernameTextBox.Text.Trim();
			string password = PasswordBox.Password;
			Role selectedRole = RoleComboBox.SelectedItem as Role;

			if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || selectedRole == null)
			{
				MessageBox.Show("Пожалуйста, заполните все поля и выберите роль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();

					// Проверка на существование пользователя с таким же именем
					string checkUserQuery = "SELECT COUNT(*) FROM Пользователи WHERE ИмяПользователя = @Username";
					using (SqlCommand command = new SqlCommand(checkUserQuery, connection))
					{
						command.Parameters.AddWithValue("@Username", username);
						int userCount = (int)command.ExecuteScalar();
						if (userCount > 0)
						{
							MessageBox.Show("Пользователь с таким именем уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
							return;
						}
					}

					// Хеширование пароля можно добавить здесь

					// Добавление нового пользователя
					string insertUserQuery = @"
                            INSERT INTO Пользователи (ИмяПользователя, Пароль, РольID)
                            VALUES (@Username, @Password, @RoleId);
                            SELECT CAST(scope_identity() AS int);";

					int newUserId;
					using (SqlCommand command = new SqlCommand(insertUserQuery, connection))
					{
						command.Parameters.AddWithValue("@Username", username);
						command.Parameters.AddWithValue("@Password", password); // Замените на хешированный пароль при необходимости
						command.Parameters.AddWithValue("@RoleId", selectedRole.Id);

						object result = command.ExecuteScalar();
						newUserId = (result != null) ? (int)result : 0;
					}

					if (newUserId > 0)
					{
						User newUser = new User
						{
							Id = newUserId,
							Name = username,
							RoleName = selectedRole.Name,
							RoleId = selectedRole.Id
						};
						Users.Add(newUser);
						UsersDataGrid.ItemsSource = null;
						UsersDataGrid.ItemsSource = Users;
						MessageBox.Show("Пользователь успешно добавлен.", "Добавление пользователя", MessageBoxButton.OK, MessageBoxImage.Information);
						HidePanel();
						ClearInputFields();
					}
					else
					{
						MessageBox.Show("Не удалось добавить пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка сохранения в базу данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Сохранение изменений в существующем пользователе
		private void SaveEditUser()
		{
			string username = UsernameTextBox.Text.Trim();
			string password = PasswordBox.Password;
			Role selectedRole = RoleComboBox.SelectedItem as Role;

			if (string.IsNullOrEmpty(username) || selectedRole == null)
			{
				MessageBox.Show("Пожалуйста, заполните все поля и выберите роль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();

					// Проверка на существование другого пользователя с таким же именем
					string checkUserQuery = "SELECT COUNT(*) FROM Пользователи WHERE ИмяПользователя = @Username AND ПользовательID <> @UserId";
					using (SqlCommand command = new SqlCommand(checkUserQuery, connection))
					{
						command.Parameters.AddWithValue("@Username", username);
						command.Parameters.AddWithValue("@UserId", SelectedUser.Id);
						int userCount = (int)command.ExecuteScalar();
						if (userCount > 0)
						{
							MessageBox.Show("Другой пользователь с таким именем уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
							return;
						}
					}

					// Обновление пользователя
					string updateUserQuery = @"
                            UPDATE Пользователи
                            SET ИмяПользователя = @Username, РольID = @RoleId{0}
                            WHERE ПользовательID = @UserId";

					string passwordUpdateClause = string.Empty;
					if (!string.IsNullOrEmpty(password))
					{
						// Хеширование пароля можно добавить здесь
						passwordUpdateClause = ", Пароль = @Password";
					}

					updateUserQuery = string.Format(updateUserQuery, passwordUpdateClause);

					using (SqlCommand command = new SqlCommand(updateUserQuery, connection))
					{
						command.Parameters.AddWithValue("@Username", username);
						command.Parameters.AddWithValue("@RoleId", selectedRole.Id);
						command.Parameters.AddWithValue("@UserId", SelectedUser.Id);

						if (!string.IsNullOrEmpty(password))
						{
							command.Parameters.AddWithValue("@Password", password); // Замените на хешированный пароль при необходимости
						}

						int rowsAffected = command.ExecuteNonQuery();

						if (rowsAffected > 0)
						{
							SelectedUser.Name = username;
							SelectedUser.RoleName = selectedRole.Name;
							SelectedUser.RoleId = selectedRole.Id;
							UsersDataGrid.ItemsSource = null;
							UsersDataGrid.ItemsSource = Users;
							MessageBox.Show("Пользователь успешно обновлён.", "Редактирование пользователя", MessageBoxButton.OK, MessageBoxImage.Information);
							HidePanel();
							ClearInputFields();
						}
						else
						{
							MessageBox.Show("Не удалось обновить пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка обновления в базе данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Очистка полей ввода
		private void ClearInputFields()
		{
			UsernameTextBox.Text = string.Empty;
			PasswordBox.Password = string.Empty;
			RoleComboBox.SelectedIndex = -1;
		}

		// Заполнение полей редактирования
		private void PopulateInputFields(User user)
		{
			UsernameTextBox.Text = user.Name;
			PasswordBox.Password = string.Empty; // Пароль не отображаем
			RoleComboBox.SelectedItem = Roles.FirstOrDefault(r => r.Id == user.RoleId);
		}

		// Переключение темы
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

		// Обработка перетаскивания окна
		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}
	}
}
