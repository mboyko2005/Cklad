using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using System.Windows.Media.Animation;
using System.ComponentModel;
using System.Windows.Data;

namespace УправлениеСкладом.Сотрудник_склада
{
	public partial class MoveItemsWindow : Window
	{
		// Модель товара
		public class Item
		{
			public int ТоварID { get; set; }
			public string Наименование { get; set; }
			public string Категория { get; set; }
			public int Количество { get; set; }
			public string Поставщик { get; set; }
		}

		// Модель движения товара (если необходимо)
		public class Movement
		{
			public int ДвижениеID { get; set; }
			public int ТоварID { get; set; }
			public string Товар { get; set; }
			public string Склад { get; set; }
			public int Количество { get; set; }
			public DateTime Дата { get; set; }
			public string ТипДвижения { get; set; }
			public string Пользователь { get; set; }
		}

		private List<Item> items; // Полный список товаров
		private List<Item> displayedItems; // Список отображаемых товаров после фильтрации
		private string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		private bool isMovementPanelOpen = false; // Состояние панели перемещения

		public MoveItemsWindow()
		{
			InitializeComponent();
			LoadData();
			UpdateThemeIcon();
		}

		// Загрузка данных в DataGrid и ComboBox
		private void LoadData()
		{
			LoadItems();
			LoadCategories();
			LoadLocations();
		}

		// Загрузка товаров из базы данных
		private void LoadItems()
		{
			items = new List<Item>();

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = @"
                        SELECT t.ТоварID, t.Наименование, t.Категория, ISNULL(SUM(sp.Количество), 0) AS Количество, p.Наименование AS Поставщик
                        FROM Товары t
                        LEFT JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
                        LEFT JOIN Поставщики p ON t.ПоставщикID = p.ПоставщикID
                        GROUP BY t.ТоварID, t.Наименование, t.Категория, p.Наименование";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								items.Add(new Item
								{
									ТоварID = reader.GetInt32(reader.GetOrdinal("ТоварID")),
									Наименование = reader.GetString(reader.GetOrdinal("Наименование")),
									Категория = reader.IsDBNull(reader.GetOrdinal("Категория")) ? string.Empty : reader.GetString(reader.GetOrdinal("Категория")),
									Количество = reader.GetInt32(reader.GetOrdinal("Количество")),
									Поставщик = reader.IsDBNull(reader.GetOrdinal("Поставщик")) ? string.Empty : reader.GetString(reader.GetOrdinal("Поставщик"))
								});
							}
						}
					}

					displayedItems = new List<Item>(items);
					ItemsDataGrid.ItemsSource = displayedItems;
					ItemsDataGrid.SelectedIndex = -1;
					ItemsDataGrid.UnselectAll();
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Загрузка категорий в ComboBox
		private void LoadCategories()
		{
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				string query = @"
                    SELECT DISTINCT Категория
                    FROM Товары
                    WHERE Категория IS NOT NULL";

				SqlCommand command = new SqlCommand(query, connection);
				try
				{
					connection.Open();
					SqlDataReader reader = command.ExecuteReader();
					List<string> categories = new List<string> { "Все" };
					while (reader.Read())
					{
						categories.Add(reader["Категория"].ToString());
					}
					CategoryComboBox.ItemsSource = categories;
					CategoryComboBox.SelectedIndex = 0;
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Загрузка местоположений в ComboBox
		private void LoadLocations()
		{
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				string query = @"
                    SELECT Наименование
                    FROM Склады";

				SqlCommand command = new SqlCommand(query, connection);
				try
				{
					connection.Open();
					SqlDataReader reader = command.ExecuteReader();
					List<string> locations = new List<string>();
					while (reader.Read())
					{
						locations.Add(reader["Наименование"].ToString());
					}

					// Заполнение целевого и исходного местоположений
					TargetLocationComboBox.ItemsSource = locations;
					SourceLocationComboBox.ItemsSource = locations;

					if (locations.Count > 0)
					{
						TargetLocationComboBox.SelectedIndex = 0;
						SourceLocationComboBox.SelectedIndex = 0;
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки местоположений: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Обработчик изменения текста в полях фильтрации
		private void Filter_TextChanged(object sender, RoutedEventArgs e)
		{
			ApplyFilters();
		}

		// Применение фильтров к списку товаров
		private void ApplyFilters()
		{
			if (items == null)
				return;

			string searchText = SearchTextBox.Text.Trim().ToLower();
			string selectedCategory = CategoryComboBox.SelectedItem as string;

			if (string.IsNullOrEmpty(selectedCategory))
				selectedCategory = "Все";

			// Применение фильтрации
			displayedItems = items.Where(item =>
				(string.IsNullOrEmpty(searchText) ||
				 item.Наименование.ToLower().Contains(searchText) ||
				 item.Категория.ToLower().Contains(searchText)) &&
				(selectedCategory == "Все" || item.Категория == selectedCategory)
			).ToList();

			ItemsDataGrid.ItemsSource = displayedItems;
			ItemsDataGrid.SelectedIndex = -1;
			ItemsDataGrid.UnselectAll();
		}

		// Метод для обработки события при нажатии на окно (перемещение окна)
		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
				this.DragMove();
		}

		// Метод для обработки нажатия на кнопку "Закрыть"
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		// Метод для переключения темы
		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			ThemeManager.ToggleTheme();
			UpdateThemeIcon();
		}

		// Обновление иконки темы
		private void UpdateThemeIcon()
		{
			if (ThemeIcon != null)
			{
				ThemeIcon.Kind = ThemeManager.IsDarkTheme ? PackIconMaterialKind.WeatherNight : PackIconMaterialKind.WeatherSunny;
			}
		}

		// Метод для выполнения перемещения товара
		private void MoveButton_Click(object sender, RoutedEventArgs e)
		{
			// Получение выбранного товара
			var selectedItem = ItemsDataGrid.SelectedItem as Item;
			if (selectedItem == null)
			{
				MessageBox.Show("Пожалуйста, выберите товар для перемещения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// Получение параметров перемещения
			string sourceLocation = SourceLocationComboBox.SelectedItem as string;
			string targetLocation = TargetLocationComboBox.SelectedItem as string;

			if (string.IsNullOrEmpty(sourceLocation) || string.IsNullOrEmpty(targetLocation))
			{
				MessageBox.Show("Пожалуйста, выберите исходное и целевое местоположение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (sourceLocation == targetLocation)
			{
				MessageBox.Show("Исходное и целевое местоположение должны быть разными.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (!int.TryParse(MoveQuantityTextBox.Text, out int moveQuantity) || moveQuantity <= 0)
			{
				MessageBox.Show("Пожалуйста, введите корректное количество для перемещения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// Получение количества товара в исходном складе
			int currentQuantity = GetQuantityInWarehouse(selectedItem.ТоварID, sourceLocation);

			if (moveQuantity > currentQuantity)
			{
				MessageBox.Show("Недостаточно товара на исходном складе для перемещения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// Получение ID складов
			int sourceSkladID = GetSkladIDByName(sourceLocation);
			int targetSkladID = GetSkladIDByName(targetLocation);

			if (sourceSkladID == -1 || targetSkladID == -1)
			{
				MessageBox.Show("Ошибка при определении складов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// Получение ID текущего пользователя (для примера используем ПользовательID = 3)
			int currentUserID = GetCurrentUserID();

			// Выполнение перемещения
			bool success = MoveItem(selectedItem.ТоварID, sourceSkladID, targetSkladID, moveQuantity, currentUserID);
			if (success)
			{
				MessageBox.Show("Товар успешно перемещён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
				LoadItems(); // Обновление данных
				ApplyFilters(); // Обновление фильтров

				MoveQuantityTextBox.Clear();
			}
			else
			{
				MessageBox.Show("Произошла ошибка при перемещении товара.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// Получение ID склада по его названию
		/// </summary>
		private int GetSkladIDByName(string skladName)
		{
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				string query = "SELECT СкладID FROM Склады WHERE Наименование = @Name";
				SqlCommand command = new SqlCommand(query, connection);
				command.Parameters.AddWithValue("@Name", skladName);
				try
				{
					connection.Open();
					object result = command.ExecuteScalar();
					return result != null ? Convert.ToInt32(result) : -1;
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка при получении ID склада: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					return -1;
				}
			}
		}

		/// <summary>
		/// Метод для получения текущего пользователя
		/// </summary>
		private int GetCurrentUserID()
		{
			// Реализуйте получение текущего пользователя, например, через аутентификацию
			// Для примера возвращаем пользователя с ID = 3 (Сотрудник склада)
			return 3;
		}

		/// <summary>
		/// Получение количества товара на складе
		/// </summary>
		private int GetQuantityInWarehouse(int товарID, string skladName)
		{
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				string query = @"
                    SELECT sp.Количество
                    FROM СкладскиеПозиции sp
                    INNER JOIN Склады s ON sp.СкладID = s.СкладID
                    WHERE sp.ТоварID = @ТоварID AND s.Наименование = @SkladName";

				SqlCommand command = new SqlCommand(query, connection);
				command.Parameters.AddWithValue("@ТоварID", товарID);
				command.Parameters.AddWithValue("@SkladName", skladName);

				try
				{
					connection.Open();
					object result = command.ExecuteScalar();
					return result != null ? Convert.ToInt32(result) : 0;
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка при получении количества товара на складе: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					return 0;
				}
			}
		}

		/// <summary>
		/// Метод для получения списка складов, где хранится товар
		/// </summary>
		private List<string> GetWarehousesForProduct(int товарID)
		{
			List<string> warehouses = new List<string>();

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				string query = @"
                    SELECT s.Наименование
                    FROM СкладскиеПозиции sp
                    INNER JOIN Склады s ON sp.СкладID = s.СкладID
                    WHERE sp.ТоварID = @ТоварID AND sp.Количество > 0";

				SqlCommand command = new SqlCommand(query, connection);
				command.Parameters.AddWithValue("@ТоварID", товарID);
				try
				{
					connection.Open();
					SqlDataReader reader = command.ExecuteReader();
					while (reader.Read())
					{
						warehouses.Add(reader["Наименование"].ToString());
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка при получении складов товара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}

			return warehouses;
		}

		/// <summary>
		/// Установка SourceLocationComboBox в зависимости от выбранного товара
		/// </summary>
		private void SetSourceLocationComboBox(int товарID)
		{
			List<string> warehouses = GetWarehousesForProduct(товарID);

			if (warehouses.Count == 0)
			{
				// Нет мест хранения, отключить ComboBox без добавления опций
				SourceLocationComboBox.ItemsSource = null;
				SourceLocationComboBox.IsEnabled = false;
			}
			else if (warehouses.Count == 1)
			{
				// Один склад, установить и отключить ComboBox
				SourceLocationComboBox.ItemsSource = warehouses;
				SourceLocationComboBox.SelectedIndex = 0;
				SourceLocationComboBox.IsEnabled = false;
			}
			else
			{
				// Множественные склады, установить и включить ComboBox
				SourceLocationComboBox.ItemsSource = warehouses;
				SourceLocationComboBox.SelectedIndex = 0;
				SourceLocationComboBox.IsEnabled = true;
			}
		}

		// Метод для обработки события при выборе товара
		private void ItemsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			var selectedItem = ItemsDataGrid.SelectedItem as Item;
			if (selectedItem != null)
			{
				SetSourceLocationComboBox(selectedItem.ТоварID);
			}
			else
			{
				// Если ничего не выбрано, очистить SourceLocationComboBox
				SourceLocationComboBox.ItemsSource = null;
				SourceLocationComboBox.IsEnabled = false;
			}
		}

		// Метод для обработки нажатия на кнопку для выдвижения/скрытия панели перемещения
		private void ToggleMovementPanel_Click(object sender, RoutedEventArgs e)
		{
			if (isMovementPanelOpen)
			{
				// Скрыть панель
				Storyboard slideOut = (Storyboard)this.Resources["SlideOut"];
				slideOut.Begin();
				isMovementPanelOpen = false;
			}
			else
			{
				// Показать панель
				Storyboard slideIn = (Storyboard)this.Resources["SlideIn"];
				slideIn.Begin();
				isMovementPanelOpen = true;
			}
		}

		// Метод для обработки загрузки DataGrid и установки сортировки
		private void ItemsDataGrid_Loaded(object sender, RoutedEventArgs e)
		{
			if (ItemsDataGrid.ItemsSource != null)
			{
				ICollectionView view = CollectionViewSource.GetDefaultView(ItemsDataGrid.ItemsSource);
				view.SortDescriptions.Clear();
				view.SortDescriptions.Add(new SortDescription("ТоварID", ListSortDirection.Ascending));
				view.Refresh();
			}
		}

		// Валидация ввода чисел в TextBox
		private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			foreach (char c in e.Text)
			{
				if (!char.IsDigit(c))
				{
					e.Handled = true;
					break;
				}
			}
		}

		/// <summary>
		/// Метод для выполнения перемещения товара
		/// </summary>
		private bool MoveItem(int товарID, int sourceSkladID, int targetSkladID, int quantity, int userID)
		{
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				connection.Open();
				SqlTransaction transaction = connection.BeginTransaction();
				try
				{
					// 1. Проверка наличия товара на исходном складе
					string checkQuery = @"
                        SELECT COUNT(*)
                        FROM СкладскиеПозиции
                        WHERE ТоварID = @ТоварID AND СкладID = @СкладID AND Количество >= @Количество";
					SqlCommand checkCmd = new SqlCommand(checkQuery, connection, transaction);
					checkCmd.Parameters.AddWithValue("@ТоварID", товарID);
					checkCmd.Parameters.AddWithValue("@СкладID", sourceSkladID);
					checkCmd.Parameters.AddWithValue("@Количество", quantity);

					int count = (int)checkCmd.ExecuteScalar();
					if (count == 0)
					{
						transaction.Rollback();
						return false;
					}

					// 2. Уменьшение количества на исходном складе
					string updateSourceQuery = @"
                        UPDATE СкладскиеПозиции
                        SET Количество = Количество - @Количество,
                            ДатаОбновления = GETDATE()
                        WHERE ТоварID = @ТоварID AND СкладID = @СкладID";
					SqlCommand updateSourceCmd = new SqlCommand(updateSourceQuery, connection, transaction);
					updateSourceCmd.Parameters.AddWithValue("@ТоварID", товарID);
					updateSourceCmd.Parameters.AddWithValue("@СкладID", sourceSkladID);
					updateSourceCmd.Parameters.AddWithValue("@Количество", quantity);
					updateSourceCmd.ExecuteNonQuery();

					// 3. Увеличение количества на целевом складе или создание новой записи
					string checkTargetQuery = @"
                        SELECT COUNT(*)
                        FROM СкладскиеПозиции
                        WHERE ТоварID = @ТоварID AND СкладID = @СкладID";
					SqlCommand checkTargetCmd = new SqlCommand(checkTargetQuery, connection, transaction);
					checkTargetCmd.Parameters.AddWithValue("@ТоварID", товарID);
					checkTargetCmd.Parameters.AddWithValue("@СкладID", targetSkladID);
					int targetCount = (int)checkTargetCmd.ExecuteScalar();

					if (targetCount > 0)
					{
						string updateTargetQuery = @"
                            UPDATE СкладскиеПозиции
                            SET Количество = Количество + @Количество,
                                ДатаОбновления = GETDATE()
                            WHERE ТоварID = @ТоварID AND СкладID = @СкладID";
						SqlCommand updateTargetCmd = new SqlCommand(updateTargetQuery, connection, transaction);
						updateTargetCmd.Parameters.AddWithValue("@ТоварID", товарID);
						updateTargetCmd.Parameters.AddWithValue("@СкладID", targetSkladID);
						updateTargetCmd.Parameters.AddWithValue("@Количество", quantity);
						updateTargetCmd.ExecuteNonQuery();
					}
					else
					{
						string insertTargetQuery = @"
                            INSERT INTO СкладскиеПозиции (ТоварID, СкладID, Количество, ДатаОбновления)
                            VALUES (@ТоварID, @СкладID, @Количество, GETDATE())";
						SqlCommand insertTargetCmd = new SqlCommand(insertTargetQuery, connection, transaction);
						insertTargetCmd.Parameters.AddWithValue("@ТоварID", товарID);
						insertTargetCmd.Parameters.AddWithValue("@СкладID", targetSkladID);
						insertTargetCmd.Parameters.AddWithValue("@Количество", quantity);
						insertTargetCmd.ExecuteNonQuery();
					}

					// 4. Запись в таблицу ДвиженияТоваров
					string insertMovementQuery = @"
                        INSERT INTO ДвиженияТоваров (ТоварID, СкладID, Количество, ТипДвижения, ПользовательID)
                        VALUES (@ТоварID, @СкладID, @Количество, @ТипДвижения, @ПользовательID)";
					SqlCommand insertMovementCmd = new SqlCommand(insertMovementQuery, connection, transaction);
					// Расход с исходного склада
					insertMovementCmd.Parameters.AddWithValue("@ТоварID", товарID);
					insertMovementCmd.Parameters.AddWithValue("@СкладID", sourceSkladID);
					insertMovementCmd.Parameters.AddWithValue("@Количество", -quantity);
					insertMovementCmd.Parameters.AddWithValue("@ТипДвижения", "Перемещение: Расход");
					insertMovementCmd.Parameters.AddWithValue("@ПользовательID", userID);
					insertMovementCmd.ExecuteNonQuery();

					// Приход на целевой склад
					insertMovementCmd.Parameters["@СкладID"].Value = targetSkladID;
					insertMovementCmd.Parameters["@Количество"].Value = quantity;
					insertMovementCmd.Parameters["@ТипДвижения"].Value = "Перемещение: Приход";
					insertMovementCmd.ExecuteNonQuery();

					// Фиксация транзакции
					transaction.Commit();
					return true;
				}
				catch (Exception ex)
				{
					transaction.Rollback();
					MessageBox.Show($"Ошибка при перемещении товара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					return false;
				}
			}
		}
	}
}
