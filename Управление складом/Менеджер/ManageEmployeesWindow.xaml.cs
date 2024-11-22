using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;

namespace УправлениеСкладом.Менеджер
{
	public partial class ManageEmployeesWindow : Window, IThemeable
	{
		// Модель сотрудника
		public class Employee
		{
			public int ПользовательID { get; set; }
			public string ИмяПользователя { get; set; }
			public string Роль { get; set; }
		}

		private List<Employee> employees;
		private bool isEditMode = false;
		private Employee selectedEmployee;
		private int warehouseRoleId; 
		private string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		public ManageEmployeesWindow()
		{
			InitializeComponent();
			LoadWarehouseRoleId();
			LoadEmployees();
			UpdateThemeIcon();
		}

		// Получение РольID для "Сотрудник склада"
		private void LoadWarehouseRoleId()
		{
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = "SELECT РольID FROM Роли WHERE Наименование = N'Сотрудник склада'";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						warehouseRoleId = (int)command.ExecuteScalar();
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки роли: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Загрузка сотрудников склада из базы данных
		private void LoadEmployees()
		{
			employees = new List<Employee>();

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = @"
                        SELECT 
                            ПользовательID, 
                            ИмяПользователя, 
                            (SELECT Наименование FROM Роли WHERE РольID = Пользователи.РольID) AS Роль
                        FROM Пользователи
                        WHERE РольID = @RoleId";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@RoleId", warehouseRoleId);

						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								employees.Add(new Employee
								{
									ПользовательID = reader.GetInt32(0),
									ИмяПользователя = reader.GetString(1),
									Роль = reader.GetString(2)
								});
							}
						}
					}

					EmployeesDataGrid.ItemsSource = employees;
					EmployeesDataGrid.SelectedIndex = -1;
					EmployeesDataGrid.UnselectAll(); // Добавлено для сброса выделения
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Обработчик кнопки закрытия окна
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		// Обработчик кнопки закрытия панели
		private void ClosePanel_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}

		// Обработчик кнопки отмены
		private void CancelEmployee_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}

		// Показать панель добавления сотрудника
		private void AddEmployee_Click(object sender, RoutedEventArgs e)
		{
			isEditMode = false;
			PanelTitle.Text = "Добавить сотрудника";
			ClearInputFields();
			ShowPanel();
		}

		// Показать панель редактирования сотрудника
		private void EditEmployee_Click(object sender, RoutedEventArgs e)
		{
			if (EmployeesDataGrid.SelectedItem is Employee employee)
			{
				isEditMode = true;
				selectedEmployee = employee;
				PanelTitle.Text = "Редактировать сотрудника";
				PopulateInputFields(employee);
				ShowPanel();
			}
			else
			{
				MessageBox.Show("Выберите сотрудника для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Удаление сотрудника
		private void DeleteEmployee_Click(object sender, RoutedEventArgs e)
		{
			if (EmployeesDataGrid.SelectedItem is Employee employee)
			{
				MessageBoxResult result = MessageBox.Show($"Удалить сотрудника '{employee.ИмяПользователя}'?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result == MessageBoxResult.Yes)
				{
					using (SqlConnection connection = new SqlConnection(connectionString))
					{
						try
						{
							connection.Open();
							string deleteQuery = "DELETE FROM Пользователи WHERE ПользовательID = @UserId";

							using (SqlCommand command = new SqlCommand(deleteQuery, connection))
							{
								command.Parameters.AddWithValue("@UserId", employee.ПользовательID);
								command.ExecuteNonQuery();
							}

							employees.Remove(employee);
							EmployeesDataGrid.ItemsSource = null;
							EmployeesDataGrid.ItemsSource = employees;
							EmployeesDataGrid.SelectedIndex = -1;
							EmployeesDataGrid.UnselectAll(); // Добавлено для сброса выделения

							MessageBox.Show("Сотрудник удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
						}
						catch (SqlException ex)
						{
							MessageBox.Show($"Ошибка удаления сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			else
			{
				MessageBox.Show("Выберите сотрудника для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Сохранение сотрудника (добавление или редактирование)
		private void SaveEmployee_Click(object sender, RoutedEventArgs e)
		{
			string username = UsernameTextBox.Text.Trim();
			string password = PasswordBox.Password;
			string confirmPassword = ConfirmPasswordBox.Password;

			if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
			{
				MessageBox.Show("Заполните все поля.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (password != confirmPassword)
			{
				MessageBox.Show("Пароли не совпадают.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (isEditMode)
			{
				UpdateEmployee(username, password);
			}
			else
			{
				AddEmployee(username, password);
			}
		}

		// Добавление нового сотрудника
		private void AddEmployee(string username, string password)
		{
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();

					// Проверка на существование пользователя с таким именем
					string checkQuery = "SELECT COUNT(*) FROM Пользователи WHERE ИмяПользователя = @Username";
					using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
					{
						checkCommand.Parameters.AddWithValue("@Username", username);
						int count = (int)checkCommand.ExecuteScalar();
						if (count > 0)
						{
							MessageBox.Show("Пользователь с таким именем уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
							return;
						}
					}

					string insertQuery = "INSERT INTO Пользователи (ИмяПользователя, Пароль, РольID) VALUES (@Username, @Password, @RoleId); SELECT SCOPE_IDENTITY();";

					using (SqlCommand command = new SqlCommand(insertQuery, connection))
					{
						command.Parameters.AddWithValue("@Username", username);
						command.Parameters.AddWithValue("@Password", password);
						command.Parameters.AddWithValue("@RoleId", warehouseRoleId);

						int newId = Convert.ToInt32(command.ExecuteScalar());

						Employee newEmployee = new Employee
						{
							ПользовательID = newId,
							ИмяПользователя = username,
							Роль = "Сотрудник склада"
						};

						employees.Add(newEmployee);
						EmployeesDataGrid.ItemsSource = null;
						EmployeesDataGrid.ItemsSource = employees;
						EmployeesDataGrid.SelectedIndex = -1;
						EmployeesDataGrid.UnselectAll(); // Добавлено для сброса выделения

						MessageBox.Show("Сотрудник добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
						HidePanel();
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка добавления сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Обновление сотрудника
		private void UpdateEmployee(string username, string password)
		{
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();

					// Проверка на существование пользователя с таким именем (кроме текущего)
					string checkQuery = "SELECT COUNT(*) FROM Пользователи WHERE ИмяПользователя = @Username AND ПользовательID != @UserId";
					using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
					{
						checkCommand.Parameters.AddWithValue("@Username", username);
						checkCommand.Parameters.AddWithValue("@UserId", selectedEmployee.ПользовательID);
						int count = (int)checkCommand.ExecuteScalar();
						if (count > 0)
						{
							MessageBox.Show("Пользователь с таким именем уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
							return;
						}
					}

					string updateQuery = "UPDATE Пользователи SET ИмяПользователя = @Username, Пароль = @Password WHERE ПользовательID = @UserId";

					using (SqlCommand command = new SqlCommand(updateQuery, connection))
					{
						command.Parameters.AddWithValue("@Username", username);
						command.Parameters.AddWithValue("@Password", password);
						command.Parameters.AddWithValue("@UserId", selectedEmployee.ПользовательID);

						command.ExecuteNonQuery();

						selectedEmployee.ИмяПользователя = username;

						EmployeesDataGrid.ItemsSource = null;
						EmployeesDataGrid.ItemsSource = employees;
						EmployeesDataGrid.SelectedIndex = -1;
						EmployeesDataGrid.UnselectAll(); // Добавлено для сброса выделения

						MessageBox.Show("Сотрудник обновлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
						HidePanel();
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка обновления сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Показ панели
		private void ShowPanel()
		{
			RootGrid.ColumnDefinitions[1].Width = new GridLength(300);
			Storyboard showStoryboard = (Storyboard)FindResource("ShowPanelStoryboard");
			showStoryboard.Begin();
		}

		// Скрытие панели
		private void HidePanel()
		{
			Storyboard hideStoryboard = (Storyboard)FindResource("HidePanelStoryboard");
			hideStoryboard.Completed += (s, e) =>
			{
				RootGrid.ColumnDefinitions[1].Width = new GridLength(0);
			};
			hideStoryboard.Begin();
		}

		// Очистка полей ввода
		private void ClearInputFields()
		{
			UsernameTextBox.Text = "";
			PasswordBox.Password = "";
			ConfirmPasswordBox.Password = "";
		}

		// Заполнение полей для редактирования
		private void PopulateInputFields(Employee employee)
		{
			UsernameTextBox.Text = employee.ИмяПользователя;
			PasswordBox.Password = "";
			ConfirmPasswordBox.Password = "";
		}

		// Перетаскивание окна
		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
				DragMove();
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
	}
}
