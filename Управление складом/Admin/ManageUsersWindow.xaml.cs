using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;
using Vosk;
using Управление_складом.Class;

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

		// Голосовой ввод (Vosk)
		private Model modelRu;
		private VoiceInputService voiceService;

		public ManageUsersWindow()
		{
			InitializeComponent();
			LoadRoles();
			LoadUsers();
			UpdateThemeIcon();
			Loaded += ManageUsersWindow_Loaded;
		}

		private async void ManageUsersWindow_Loaded(object sender, RoutedEventArgs e)
		{
			await InitializeVoskAsync();
			if (modelRu != null)
			{
				voiceService = new VoiceInputService(modelRu);
				voiceService.TextRecognized += text =>
				{
					Dispatcher.Invoke(() =>
					{
						SearchTextBox.Text = text;
						ApplyUserFilter();
					});
				};
			}
		}

		#region Инициализация Vosk
		private async Task InitializeVoskAsync()
		{
			try
			{
				Vosk.Vosk.SetLogLevel(0); // Уменьшаем логирование
				string ruPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "ru");
				if (System.IO.Directory.Exists(ruPath))
				{
					modelRu = await Task.Run(() => new Model(ruPath));
				}
				if (modelRu == null)
				{
					MessageBox.Show("Не найдена офлайн-модель Vosk (Models/ru).", "Ошибка инициализации Vosk",
									MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка инициализации Vosk: " + ex.Message, "Ошибка",
								MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		#endregion

		#region Загрузка данных (Роли и Пользователи)
		private void LoadRoles()
		{
			Roles = new List<Role>();
			try
			{
				using (var connection = new SqlConnection(connectionString))
				{
					connection.Open();
					string query = "SELECT РольID, Наименование FROM Роли";
					using (var cmd = new SqlCommand(query, connection))
					using (var reader = cmd.ExecuteReader())
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
				RoleComboBox.ItemsSource = Roles;
			}
			catch (SqlException ex)
			{
				MessageBox.Show($"Ошибка загрузки ролей: {ex.Message}", "Ошибка",
								MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void LoadUsers()
		{
			Users = new List<User>();
			try
			{
				using (var connection = new SqlConnection(connectionString))
				{
					connection.Open();
					string query = @"
                        SELECT 
                            ПользовательID,
                            ИмяПользователя,
                            Роли.Наименование AS RoleName,
                            Роли.РольID
                        FROM Пользователи
                        INNER JOIN Роли ON Пользователи.РольID = Роли.РольID
                        ORDER BY ПользовательID";
					using (var cmd = new SqlCommand(query, connection))
					using (var reader = cmd.ExecuteReader())
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
				RefreshUsersDataGrid();
			}
			catch (SqlException ex)
			{
				MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка",
								MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		#endregion

		#region Обновление DataGrid
		private void RefreshUsersDataGrid()
		{
			UsersDataGrid.ItemsSource = null;
			UsersDataGrid.ItemsSource = Users;
		}
		#endregion

		#region Поиск и голосовой ввод
		private void Filter_TextChanged(object sender, TextChangedEventArgs e)
		{
			ApplyUserFilter();
		}

		private void ApplyUserFilter()
		{
			string searchText = SearchTextBox.Text.Trim().ToLower();
			var filtered = Users.Where(u =>
				string.IsNullOrEmpty(searchText) ||
				u.Id.ToString().Contains(searchText) ||
				u.Name.ToLower().Contains(searchText) ||
				u.RoleName.ToLower().Contains(searchText)
			).ToList();
			UsersDataGrid.ItemsSource = filtered;
		}

		private void VoiceSearchButton_Click(object sender, RoutedEventArgs e)
		{
			if (voiceService == null)
			{
				MessageBox.Show("Модель Vosk не загружена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (voiceService.IsRunning)
			{
				voiceService.Stop();
				VoiceIcon.Kind = PackIconMaterialKind.Microphone;
				VoiceIcon.Foreground = (Brush)FindResource("PrimaryBrush");
			}
			else
			{
				VoiceIcon.Kind = PackIconMaterialKind.RecordCircle;
				VoiceIcon.Foreground = Brushes.Red;
				voiceService.Start();
			}
		}
		#endregion

		#region Темы и управление окном
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

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
				DragMove();
		}
		#endregion

		#region Выдвижная панель
		private void ShowPanel()
		{
			RootGrid.ColumnDefinitions[1].Width = new GridLength(300);
			Storyboard showStoryboard = (Storyboard)FindResource("ShowPanelStoryboard");
			showStoryboard.Begin();
		}

		private void HidePanel()
		{
			Storyboard hideStoryboard = (Storyboard)FindResource("HidePanelStoryboard");
			hideStoryboard.Completed += (s, e) =>
			{
				RootGrid.ColumnDefinitions[1].Width = new GridLength(0);
			};
			hideStoryboard.Begin();
		}

		private void ClosePanel_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}
		#endregion

		#region Управление пользователями (Добавить, Редактировать, Удалить)
		private void AddUser_Click(object sender, RoutedEventArgs e)
		{
			IsEditMode = false;
			PanelTitle.Text = "Добавить пользователя";
			ClearInputFields();
			ShowPanel();
		}

		private void EditUser_Click(object sender, RoutedEventArgs e)
		{
			if (UsersDataGrid.SelectedItem is User user)
			{
				IsEditMode = true;
				SelectedUser = user;
				PanelTitle.Text = "Редактировать пользователя";
				PopulateInputFields(user);
				ShowPanel();
			}
			else
			{
				MessageBox.Show("Выберите пользователя для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void DeleteUser_Click(object sender, RoutedEventArgs e)
		{
			if (UsersDataGrid.SelectedItem is User user)
			{
				MessageBoxResult result = MessageBox.Show($"Удалить пользователя '{user.Name}'?", "Подтверждение",
														   MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result == MessageBoxResult.Yes)
				{
					try
					{
						using (var connection = new SqlConnection(connectionString))
						{
							connection.Open();
							// Удаление связанных записей
							string deleteRelatedQuery = "DELETE FROM ДвиженияТоваров WHERE ПользовательID = @UserId";
							using (var cmdRelated = new SqlCommand(deleteRelatedQuery, connection))
							{
								cmdRelated.Parameters.AddWithValue("@UserId", user.Id);
								cmdRelated.ExecuteNonQuery();
							}
							// Удаление пользователя
							string deleteUserQuery = "DELETE FROM Пользователи WHERE ПользовательID = @UserId";
							using (var cmdDelUser = new SqlCommand(deleteUserQuery, connection))
							{
								cmdDelUser.Parameters.AddWithValue("@UserId", user.Id);
								cmdDelUser.ExecuteNonQuery();
							}
						}
						Users.Remove(user);
						RefreshUsersDataGrid();
						MessageBox.Show("Пользователь удалён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
					}
					catch (SqlException ex)
					{
						MessageBox.Show("Ошибка удаления: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
			}
			else
			{
				MessageBox.Show("Выберите пользователя для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
		#endregion

		#region Сохранение пользователя (Добавление/Редактирование)
		private void SaveUser_Click(object sender, RoutedEventArgs e)
		{
			if (IsEditMode)
				SaveEditUser();
			else
				SaveAddUser();
		}

		// Валидация ввода (поля и совпадение паролей)
		private bool ValidateUserInput(out string username, out string password, out Role selectedRole)
		{
			username = UsernameTextBox.Text.Trim();
			password = PasswordBox.Password;
			selectedRole = RoleComboBox.SelectedItem as Role;
			if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) ||
				string.IsNullOrEmpty(ConfirmPasswordBox.Password) || selectedRole == null)
			{
				MessageBox.Show("Заполните все поля и выберите роль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return false;
			}
			if (password != ConfirmPasswordBox.Password)
			{
				MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return false;
			}
			return true;
		}

		private void SaveAddUser()
		{
			if (!ValidateUserInput(out string username, out string password, out Role selectedRole))
				return;

			try
			{
				using (var connection = new SqlConnection(connectionString))
				{
					connection.Open();
					// Проверка существования пользователя с таким именем
					string checkQuery = "SELECT COUNT(*) FROM Пользователи WHERE ИмяПользователя = @Username";
					using (var cmdCheck = new SqlCommand(checkQuery, connection))
					{
						cmdCheck.Parameters.AddWithValue("@Username", username);
						int count = (int)cmdCheck.ExecuteScalar();
						if (count > 0)
						{
							MessageBox.Show("Пользователь с таким именем уже существует.", "Ошибка",
											MessageBoxButton.OK, MessageBoxImage.Warning);
							return;
						}
					}
					// Добавление пользователя
					string insertQuery = @"
                        INSERT INTO Пользователи (ИмяПользователя, Пароль, РольID)
                        VALUES (@Username, @Password, @RoleId);
                        SELECT SCOPE_IDENTITY();";
					using (var cmdInsert = new SqlCommand(insertQuery, connection))
					{
						cmdInsert.Parameters.AddWithValue("@Username", username);
						cmdInsert.Parameters.AddWithValue("@Password", password);
						cmdInsert.Parameters.AddWithValue("@RoleId", selectedRole.Id);
						int newId = Convert.ToInt32(cmdInsert.ExecuteScalar());
						if (newId > 0)
						{
							Users.Add(new User { Id = newId, Name = username, RoleName = selectedRole.Name, RoleId = selectedRole.Id });
							RefreshUsersDataGrid();
							MessageBox.Show("Пользователь добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
							HidePanel();
							ClearInputFields();
						}
						else
						{
							MessageBox.Show("Не удалось добавить пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			catch (SqlException ex)
			{
				MessageBox.Show("Ошибка базы: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void SaveEditUser()
		{
			if (!ValidateUserInput(out string username, out string password, out Role selectedRole))
				return;

			try
			{
				using (var connection = new SqlConnection(connectionString))
				{
					connection.Open();
					// Проверка наличия другого пользователя с таким именем
					string checkQuery = @"
                        SELECT COUNT(*) FROM Пользователи 
                        WHERE ИмяПользователя = @Username AND ПользовательID <> @UserId";
					using (var cmdCheck = new SqlCommand(checkQuery, connection))
					{
						cmdCheck.Parameters.AddWithValue("@Username", username);
						cmdCheck.Parameters.AddWithValue("@UserId", SelectedUser.Id);
						int count = (int)cmdCheck.ExecuteScalar();
						if (count > 0)
						{
							MessageBox.Show("Другой пользователь с таким именем уже существует.", "Ошибка",
											MessageBoxButton.OK, MessageBoxImage.Warning);
							return;
						}
					}
					// Обновление данных пользователя
					string updateQuery = @"
                        UPDATE Пользователи
                        SET ИмяПользователя = @Username,
                            Пароль = @Password,
                            РольID = @RoleId
                        WHERE ПользовательID = @UserId";
					using (var cmdUpdate = new SqlCommand(updateQuery, connection))
					{
						cmdUpdate.Parameters.AddWithValue("@Username", username);
						cmdUpdate.Parameters.AddWithValue("@Password", password);
						cmdUpdate.Parameters.AddWithValue("@RoleId", selectedRole.Id);
						cmdUpdate.Parameters.AddWithValue("@UserId", SelectedUser.Id);
						int rows = cmdUpdate.ExecuteNonQuery();
						if (rows > 0)
						{
							SelectedUser.Name = username;
							SelectedUser.RoleName = selectedRole.Name;
							SelectedUser.RoleId = selectedRole.Id;
							RefreshUsersDataGrid();
							MessageBox.Show("Пользователь обновлён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
							HidePanel();
							ClearInputFields();
						}
						else
						{
							MessageBox.Show("Не удалось обновить пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			catch (SqlException ex)
			{
				MessageBox.Show("Ошибка базы: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		#endregion

		#region Подтверждение пароля
		private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
		{
			if (!(sender is PasswordBox confirmBox))
				return;

			var icon = confirmBox.Template.FindName("ConfirmStatusIcon", confirmBox) as PackIconMaterial;
			if (icon == null)
				return;

			if (string.IsNullOrEmpty(confirmBox.Password))
			{
				icon.Visibility = Visibility.Collapsed;
				return;
			}
			if (PasswordBox.Password == confirmBox.Password)
			{
				icon.Kind = PackIconMaterialKind.CheckCircleOutline;
				icon.Foreground = Brushes.Green;
				icon.Visibility = Visibility.Visible;
			}
			else
			{
				icon.Kind = PackIconMaterialKind.CloseCircleOutline;
				icon.Foreground = Brushes.Red;
				icon.Visibility = Visibility.Visible;
			}
		}
		#endregion

		#region Очистка и заполнение полей ввода
		private void ClearInputFields()
		{
			UsernameTextBox.Text = "";
			PasswordBox.Password = "";
			ConfirmPasswordBox.Password = "";
			RoleComboBox.SelectedItem = null;
			var icon = ConfirmPasswordBox.Template.FindName("ConfirmStatusIcon", ConfirmPasswordBox) as PackIconMaterial;
			if (icon != null)
				icon.Visibility = Visibility.Collapsed;
		}

		private void PopulateInputFields(User user)
		{
			UsernameTextBox.Text = user.Name;
			PasswordBox.Password = "";
			ConfirmPasswordBox.Password = "";
			RoleComboBox.SelectedItem = Roles.FirstOrDefault(r => r.Id == user.RoleId);
			var icon = ConfirmPasswordBox.Template.FindName("ConfirmStatusIcon", ConfirmPasswordBox) as PackIconMaterial;
			if (icon != null)
				icon.Visibility = Visibility.Collapsed;
		}

		private void CancelUser_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}
		#endregion
	}
}
