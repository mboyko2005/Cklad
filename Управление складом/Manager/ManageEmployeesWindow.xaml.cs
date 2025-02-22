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
		private bool isEditMode = false;    // Объявлено один раз
		private Employee selectedEmployee;  // Объявлено один раз

		private int warehouseRoleId;
		private string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		// Параметры для голосового ввода (Vosk)
		private Model modelRu;
		private VoiceInputService voiceService;

		public ManageEmployeesWindow()
		{
			InitializeComponent();
			LoadWarehouseRoleId();
			LoadEmployees();
			UpdateThemeIcon();
			this.Loaded += ManageEmployeesWindow_Loaded;
		}

		private async void ManageEmployeesWindow_Loaded(object sender, RoutedEventArgs e)
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
						ApplyEmployeeFilter();
					});
				};
			}
		}

		#region Инициализация Vosk
		private async Task InitializeVoskAsync()
		{
			try
			{
				Vosk.Vosk.SetLogLevel(0);
				string baseDir = AppDomain.CurrentDomain.BaseDirectory;
				string ruPath = System.IO.Path.Combine(baseDir, "Models", "ru");
				if (System.IO.Directory.Exists(ruPath))
				{
					modelRu = await Task.Run(() => new Model(ruPath));
				}
				if (modelRu == null)
				{
					MessageBox.Show("Отсутствует офлайн-модель Vosk для ru в папке Models.",
						"Ошибка инициализации Vosk", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка инициализации Vosk: " + ex.Message,
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		#endregion

		#region Поиск и фильтрация
		private void Filter_TextChanged(object sender, TextChangedEventArgs e)
		{
			ApplyEmployeeFilter();
		}

		private void ApplyEmployeeFilter()
		{
			string searchText = SearchTextBox.Text.Trim().ToLower();
			var filtered = employees.Where(emp =>
				string.IsNullOrEmpty(searchText)
				|| emp.ИмяПользователя.ToLower().Contains(searchText)
				|| emp.ПользовательID.ToString().Contains(searchText)
				|| emp.Роль.ToLower().Contains(searchText)
			).ToList();
			EmployeesDataGrid.ItemsSource = filtered;
		}

		private void VoiceSearchButton_Click(object sender, RoutedEventArgs e)
		{
			if (voiceService == null)
			{
				MessageBox.Show("Модель не загружена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (voiceService.IsRunning)
			{
				voiceService.Stop();
				VoiceIcon.Kind = PackIconMaterialKind.Microphone;
				VoiceIcon.Foreground = (Brush)FindResource("PrimaryBrush");
				return;
			}
			VoiceIcon.Kind = PackIconMaterialKind.RecordCircle;
			VoiceIcon.Foreground = Brushes.Red;
			voiceService.Start();
		}
		#endregion

		#region Загрузка роли и сотрудников
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
					MessageBox.Show($"Ошибка загрузки роли: {ex.Message}",
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

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
					EmployeesDataGrid.UnselectAll();
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}",
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
		#endregion

		#region Управление UI (закрыть окно, перетащить)
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
				this.DragMove();
		}
		#endregion

		#region Темы
		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			ThemeManager.ToggleTheme();
			UpdateThemeIcon();
		}

		public void UpdateThemeIcon()
		{
			if (ThemeIcon != null)
			{
				ThemeIcon.Kind = ThemeManager.IsDarkTheme
					? PackIconMaterialKind.WeatherNight
					: PackIconMaterialKind.WeatherSunny;
			}
		}
		#endregion

		#region Открытие/закрытие панели
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

		#region Добавление, редактирование, удаление
		// Очистка полей
		private void ClearInputFields()
		{
			UsernameTextBox.Text = "";
			PasswordBox.Password = "";
			ConfirmPasswordBox.Password = "";
			// Скрываем индикатор подтверждения
			var icon = (PackIconMaterial)ConfirmPasswordBox.Template.FindName("ConfirmStatusIcon", ConfirmPasswordBox);
			if (icon != null) icon.Visibility = Visibility.Collapsed;
		}

		// Заполнение полей при редактировании
		private void PopulateInputFields(Employee employee)
		{
			UsernameTextBox.Text = employee.ИмяПользователя;
			PasswordBox.Password = "";
			ConfirmPasswordBox.Password = "";
			var icon = (PackIconMaterial)ConfirmPasswordBox.Template.FindName("ConfirmStatusIcon", ConfirmPasswordBox);
			if (icon != null) icon.Visibility = Visibility.Collapsed;
		}

		// Добавить сотрудника (открытие панели)
		private void AddEmployee_Click(object sender, RoutedEventArgs e)
		{
			isEditMode = false;
			PanelTitle.Text = "Добавить сотрудника";
			ClearInputFields();
			ShowPanel();
		}

		// Редактировать сотрудника
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
				MessageBox.Show("Выберите сотрудника для редактирования.",
					"Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Удалить сотрудника
		private void DeleteEmployee_Click(object sender, RoutedEventArgs e)
		{
			if (EmployeesDataGrid.SelectedItem is Employee employee)
			{
				MessageBoxResult result = MessageBox.Show(
					$"Удалить сотрудника '{employee.ИмяПользователя}'?",
					"Подтверждение",
					MessageBoxButton.YesNo,
					MessageBoxImage.Question);

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
							EmployeesDataGrid.UnselectAll();

							MessageBox.Show("Сотрудник удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
						}
						catch (SqlException ex)
						{
							MessageBox.Show($"Ошибка удаления сотрудника: {ex.Message}",
								"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			else
			{
				MessageBox.Show("Выберите сотрудника для удаления.",
					"Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
		#endregion

		#region Сохранение (Добавление / Редактирование)
		private void SaveEmployee_Click(object sender, RoutedEventArgs e)
		{
			string username = UsernameTextBox.Text.Trim();
			string password = PasswordBox.Password;
			string confirmPassword = ConfirmPasswordBox.Password;

			if (string.IsNullOrEmpty(username)
				|| string.IsNullOrEmpty(password)
				|| string.IsNullOrEmpty(confirmPassword))
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
					// Проверка на существующее имя пользователя
					string checkQuery = "SELECT COUNT(*) FROM Пользователи WHERE ИмяПользователя = @Username";
					using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
					{
						checkCommand.Parameters.AddWithValue("@Username", username);
						int count = (int)checkCommand.ExecuteScalar();
						if (count > 0)
						{
							MessageBox.Show("Пользователь с таким именем уже существует.",
								"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
							return;
						}
					}

					// Добавление
					string insertQuery = @"
                        INSERT INTO Пользователи (ИмяПользователя, Пароль, РольID)
                        VALUES (@Username, @Password, @RoleId);
                        SELECT SCOPE_IDENTITY();";
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
						EmployeesDataGrid.UnselectAll();

						MessageBox.Show("Сотрудник добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
						HidePanel();
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка добавления сотрудника: {ex.Message}",
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Обновление существующего сотрудника
		private void UpdateEmployee(string username, string password)
		{
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					// Проверка на существующее имя пользователя (кроме текущего)
					string checkQuery = @"
                        SELECT COUNT(*) 
                        FROM Пользователи 
                        WHERE ИмяПользователя = @Username AND ПользовательID != @UserId";
					using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
					{
						checkCommand.Parameters.AddWithValue("@Username", username);
						checkCommand.Parameters.AddWithValue("@UserId", selectedEmployee.ПользовательID);

						int count = (int)checkCommand.ExecuteScalar();
						if (count > 0)
						{
							MessageBox.Show("Пользователь с таким именем уже существует.",
								"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
							return;
						}
					}

					// Обновление
					string updateQuery = @"
                        UPDATE Пользователи
                        SET ИмяПользователя = @Username, Пароль = @Password
                        WHERE ПользовательID = @UserId";
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
						EmployeesDataGrid.UnselectAll();

						MessageBox.Show("Сотрудник обновлён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
						HidePanel();
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка обновления сотрудника: {ex.Message}",
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
		#endregion

		#region Проверка совпадения пароля (иконка справа)
		// Обработчик события PasswordChanged для ConfirmPasswordBox
		private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
		{
			var confirmBox = (PasswordBox)sender;
			// Находим элемент ConfirmStatusIcon внутри шаблона
			var icon = (PackIconMaterial)confirmBox.Template.FindName("ConfirmStatusIcon", confirmBox);
			if (icon == null) return;

			// Если поле пустое, скрываем иконку
			if (string.IsNullOrEmpty(confirmBox.Password))
			{
				icon.Visibility = Visibility.Collapsed;
				return;
			}

			// Сравниваем пароль из PasswordBox и ConfirmPasswordBox
			if (PasswordBox.Password == confirmBox.Password)
			{
				// Пароли совпадают: показываем зелёную галочку
				icon.Visibility = Visibility.Visible;
				icon.Kind = PackIconMaterialKind.CheckCircleOutline;
				icon.Foreground = Brushes.Green;
			}
			else
			{
				// Пароли не совпадают: показываем красный крестик
				icon.Visibility = Visibility.Visible;
				icon.Kind = PackIconMaterialKind.CloseCircleOutline;
				icon.Foreground = Brushes.Red;
			}
		}
		#endregion

		#region Обработчик кнопки Отмена
		private void CancelEmployee_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}
		#endregion
	}
}
