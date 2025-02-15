using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes; // Пространство имен для ThemeManager

namespace УправлениеСкладом
{
	/// <summary>
	/// Логика взаимодействия для ManageInventoryWindow.xaml
	/// </summary>
	public partial class ManageInventoryWindow : Window, IThemeable
	{
		#region Модели

		// Модель складской позиции
		public class InventoryItem
		{
			public int Id { get; set; }
			public string Name { get; set; } // Название товара
			public decimal Price { get; set; } // Цена товара
			public int Quantity { get; set; }
			public string Location { get; set; } // Название склада
		}

		// Модель товара
		public class Product
		{
			public int Id { get; set; }
			public string Name { get; set; }
			public decimal Price { get; set; }
			public int SupplierId { get; set; } // ПоставщикID
		}

		// Модель склада
		public class Warehouse
		{
			public int Id { get; set; }
			public string Name { get; set; }
		}

		// Модель поставщика
		public class Supplier
		{
			public int Id { get; set; }
			public string Name { get; set; }
		}

		#endregion

		#region Поля

		private ObservableCollection<InventoryItem> InventoryItems;
		private List<Product> Products;
		private List<Warehouse> Warehouses;
		private List<Supplier> Suppliers;

		private bool IsEditMode = false;
		private InventoryItem SelectedItem;
		private readonly string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		#endregion

		#region Конструктор

		public ManageInventoryWindow()
		{
			InitializeComponent();

			// Инициализация коллекций
			Products = new List<Product>();
			Warehouses = new List<Warehouse>();
			Suppliers = new List<Supplier>();

			// Загрузка данных
			LoadProducts();
			LoadWarehouses();
			LoadSuppliers();
			LoadInventory();

			UpdateThemeIcon();
		}

		#endregion

		#region Загрузка данных из БД

		private void LoadProducts()
		{
			Products.Clear();
			const string query = "SELECT ТоварID, Наименование, Цена, ПоставщикID FROM Товары";

			try
			{
				using var connection = new SqlConnection(connectionString);
				connection.Open();
				using var command = new SqlCommand(query, connection);
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					Products.Add(new Product
					{
						Id = reader.GetInt32(reader.GetOrdinal("ТоварID")),
						Name = reader.GetString(reader.GetOrdinal("Наименование")),
						Price = reader.IsDBNull(reader.GetOrdinal("Цена")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Цена")),
						SupplierId = reader.GetInt32(reader.GetOrdinal("ПоставщикID"))
					});
				}
			}
			catch (SqlException ex)
			{
				MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void LoadWarehouses()
		{
			Warehouses.Clear();
			const string query = "SELECT СкладID, Наименование FROM Склады";

			try
			{
				using var connection = new SqlConnection(connectionString);
				connection.Open();
				using var command = new SqlCommand(query, connection);
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					Warehouses.Add(new Warehouse
					{
						Id = reader.GetInt32(reader.GetOrdinal("СкладID")),
						Name = reader.GetString(reader.GetOrdinal("Наименование"))
					});
				}
				WarehouseComboBox.ItemsSource = Warehouses;
			}
			catch (SqlException ex)
			{
				MessageBox.Show($"Ошибка загрузки складов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void LoadSuppliers()
		{
			Suppliers.Clear();
			const string query = "SELECT ПоставщикID, Наименование FROM Поставщики";

			try
			{
				using var connection = new SqlConnection(connectionString);
				connection.Open();
				using var command = new SqlCommand(query, connection);
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					Suppliers.Add(new Supplier
					{
						Id = reader.GetInt32(reader.GetOrdinal("ПоставщикID")),
						Name = reader.GetString(reader.GetOrdinal("Наименование"))
					});
				}
				SupplierComboBox.ItemsSource = Suppliers;
			}
			catch (SqlException ex)
			{
				MessageBox.Show($"Ошибка загрузки поставщиков: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void LoadInventory()
		{
			InventoryItems = new ObservableCollection<InventoryItem>();
			const string query = @"
                SELECT sp.ПозицияID AS Id, t.Наименование AS Name, t.Цена AS Price, sp.Количество AS Quantity, s.Наименование AS Location
                FROM СкладскиеПозиции sp
                INNER JOIN Товары t ON sp.ТоварID = t.ТоварID
                INNER JOIN Склады s ON sp.СкладID = s.СкладID";

			try
			{
				using var connection = new SqlConnection(connectionString);
				connection.Open();
				using var command = new SqlCommand(query, connection);
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					InventoryItems.Add(new InventoryItem
					{
						Id = reader.GetInt32(reader.GetOrdinal("Id")),
						Name = reader.GetString(reader.GetOrdinal("Name")),
						Price = reader.IsDBNull(reader.GetOrdinal("Price")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Price")),
						Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
						Location = reader.GetString(reader.GetOrdinal("Location"))
					});
				}
			}
			catch (SqlException ex)
			{
				MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			RefreshInventoryDataGrid();
		}

		#endregion

		#region Обновление UI

		private void RefreshInventoryDataGrid()
		{
			InventoryDataGrid.ItemsSource = null;
			InventoryDataGrid.ItemsSource = InventoryItems;
		}

		private void ClearInputFields()
		{
			NameTextBox.Text = string.Empty;
			PriceTextBox.Text = string.Empty;
			QuantityTextBox.Text = string.Empty;
			SupplierComboBox.SelectedIndex = -1;
			WarehouseComboBox.SelectedIndex = -1;
		}

		private void PopulateInputFields(InventoryItem item)
		{
			NameTextBox.Text = item.Name;
			PriceTextBox.Text = item.Price.ToString("F2");
			QuantityTextBox.Text = item.Quantity.ToString();
			WarehouseComboBox.SelectedItem = Warehouses.FirstOrDefault(w => w.Name == item.Location);

			// Определяем поставщика по связанному товару
			var product = Products.FirstOrDefault(p => p.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
			if (product != null)
			{
				var supplier = Suppliers.FirstOrDefault(s => s.Id == product.SupplierId);
				SupplierComboBox.SelectedItem = supplier;
			}
			else
			{
				SupplierComboBox.SelectedIndex = -1;
			}
		}

		#endregion

		#region Обработчики событий

		// Перетаскивание окна
		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}

		// Закрытие окна
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		// Показ панели добавления позиции
		private void AddInventory_Click(object sender, RoutedEventArgs e)
		{
			IsEditMode = false;
			PanelTitle.Text = "Добавить складскую позицию";
			ClearInputFields();
			ShowPanel();
		}

		// Показ панели редактирования позиции
		private void EditInventory_Click(object sender, RoutedEventArgs e)
		{
			if (InventoryDataGrid.SelectedItem is InventoryItem selectedItem)
			{
				IsEditMode = true;
				SelectedItem = selectedItem;
				PanelTitle.Text = "Редактировать складскую позицию";
				PopulateInputFields(selectedItem);
				ShowPanel();
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите позицию для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Удаление позиции и связанных записей
		private void DeleteInventory_Click(object sender, RoutedEventArgs e)
		{
			if (InventoryDataGrid.SelectedItem is InventoryItem selectedItem)
			{
				var result = MessageBox.Show($"Вы уверены, что хотите удалить позицию: {selectedItem.Name}?", "Удаление позиции", MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result == MessageBoxResult.Yes)
				{
					DeleteInventoryPosition(selectedItem);
				}
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите позицию для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Закрытие панели
		private void ClosePanel_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}

		// Отмена операции
		private void CancelInventory_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}

		// Сохранение изменений (добавление или редактирование)
		private void SaveInventory_Click(object sender, RoutedEventArgs e)
		{
			if (IsEditMode)
				SaveEditInventory();
			else
				SaveAddInventory();
		}

		// Переключение темы
		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			ThemeManager.ToggleTheme();
			UpdateThemeIcon();
		}

		#endregion

		#region Методы панели анимации

		private void ShowPanel()
		{
			RootGrid.ColumnDefinitions[1].Width = new GridLength(400);
			var showStoryboard = (Storyboard)FindResource("ShowPanelStoryboard");
			showStoryboard.Begin();
		}

		private void HidePanel()
		{
			var hideStoryboard = (Storyboard)FindResource("HidePanelStoryboard");
			hideStoryboard.Completed += (s, e) =>
			{
				RootGrid.ColumnDefinitions[1].Width = new GridLength(0);
			};
			hideStoryboard.Begin();
		}

		#endregion

		#region Методы работы с базой данных

		/// <summary>
		/// Метод для вставки нового поставщика или получения существующего.
		/// </summary>
		private Supplier GetOrInsertSupplier(string supplierName, SqlConnection connection, SqlTransaction transaction)
		{
			var supplier = Suppliers.FirstOrDefault(s => s.Name.Equals(supplierName, StringComparison.OrdinalIgnoreCase));
			if (supplier != null)
				return supplier;

			const string insertSupplierQuery = @"
                INSERT INTO Поставщики (Наименование)
                VALUES (@SupplierName);
                SELECT CAST(scope_identity() AS int);";

			using var command = new SqlCommand(insertSupplierQuery, connection, transaction);
			command.Parameters.AddWithValue("@SupplierName", supplierName);
			var result = command.ExecuteScalar();
			int newSupplierId = (result != null) ? (int)result : 0;
			if (newSupplierId > 0)
			{
				supplier = new Supplier { Id = newSupplierId, Name = supplierName };
				Suppliers.Add(supplier);
			}
			else
			{
				throw new Exception("Не удалось добавить нового поставщика.");
			}
			return supplier;
		}

		/// <summary>
		/// Метод для вставки нового товара или обновления цены, если товар существует.
		/// </summary>
		private Product GetOrInsertProduct(string productName, decimal price, int supplierId, SqlConnection connection, SqlTransaction transaction)
		{
			var product = Products.FirstOrDefault(p => p.Name.Equals(productName, StringComparison.OrdinalIgnoreCase));
			if (product == null)
			{
				const string insertProductQuery = @"
                    INSERT INTO Товары (Наименование, Цена, ПоставщикID)
                    VALUES (@ProductName, @Price, @SupplierID);
                    SELECT CAST(scope_identity() AS int);";

				using var command = new SqlCommand(insertProductQuery, connection, transaction);
				command.Parameters.AddWithValue("@ProductName", productName);
				command.Parameters.AddWithValue("@Price", price);
				command.Parameters.AddWithValue("@SupplierID", supplierId);
				var result = command.ExecuteScalar();
				int newProductId = (result != null) ? (int)result : 0;
				if (newProductId > 0)
				{
					product = new Product { Id = newProductId, Name = productName, Price = price, SupplierId = supplierId };
					Products.Add(product);
				}
				else
				{
					throw new Exception("Не удалось добавить новый товар.");
				}
			}
			else if (product.Price != price)
			{
				// Обновление цены товара, если необходимо
				const string updateProductQuery = "UPDATE Товары SET Цена = @Price WHERE ТоварID = @ProductId";
				using var command = new SqlCommand(updateProductQuery, connection, transaction);
				command.Parameters.AddWithValue("@Price", price);
				command.Parameters.AddWithValue("@ProductId", product.Id);
				command.ExecuteNonQuery();
				product.Price = price;
			}
			return product;
		}

		private void SaveAddInventory()
		{
			string name = NameTextBox.Text.Trim();
			string priceText = PriceTextBox.Text.Trim();
			string quantityText = QuantityTextBox.Text.Trim();
			string supplierName = SupplierComboBox.Text.Trim();
			var selectedWarehouse = WarehouseComboBox.SelectedItem as Warehouse;

			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(priceText) ||
				string.IsNullOrEmpty(quantityText) || string.IsNullOrEmpty(supplierName) || selectedWarehouse == null)
			{
				MessageBox.Show("Пожалуйста, заполните все поля и выберите склад.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!decimal.TryParse(priceText, out decimal price) || price < 0)
			{
				MessageBox.Show("Цена должна быть положительным числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!int.TryParse(quantityText, out int quantity) || quantity < 0)
			{
				MessageBox.Show("Количество должно быть положительным целым числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			try
			{
				using var connection = new SqlConnection(connectionString);
				connection.Open();
				using var transaction = connection.BeginTransaction();

				// Получение или вставка поставщика
				var supplier = GetOrInsertSupplier(supplierName, connection, transaction);
				// Получение или вставка товара
				var product = GetOrInsertProduct(name, price, supplier.Id, connection, transaction);

				const string insertInventoryQuery = @"
                    INSERT INTO СкладскиеПозиции (ТоварID, СкладID, Количество)
                    VALUES (@ItemId, @WarehouseId, @Quantity);
                    SELECT CAST(scope_identity() AS int);";

				using var command = new SqlCommand(insertInventoryQuery, connection, transaction);
				command.Parameters.AddWithValue("@ItemId", product.Id);
				command.Parameters.AddWithValue("@WarehouseId", selectedWarehouse.Id);
				command.Parameters.AddWithValue("@Quantity", quantity);

				var result = command.ExecuteScalar();
				int newId = (result != null) ? (int)result : 0;
				if (newId > 0)
				{
					transaction.Commit();
					InventoryItems.Add(new InventoryItem
					{
						Id = newId,
						Name = product.Name,
						Price = product.Price,
						Quantity = quantity,
						Location = selectedWarehouse.Name
					});
					MessageBox.Show("Позиция успешно добавлена.", "Добавление позиции", MessageBoxButton.OK, MessageBoxImage.Information);
					RefreshInventoryDataGrid();
					HidePanel();
					ClearInputFields();
				}
				else
				{
					transaction.Rollback();
					MessageBox.Show("Не удалось добавить позицию.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			catch (SqlException ex)
			{
				MessageBox.Show($"Ошибка сохранения в базу данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void SaveEditInventory()
		{
			string name = NameTextBox.Text.Trim();
			string priceText = PriceTextBox.Text.Trim();
			string quantityText = QuantityTextBox.Text.Trim();
			string supplierName = SupplierComboBox.Text.Trim();
			var selectedWarehouse = WarehouseComboBox.SelectedItem as Warehouse;

			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(priceText) ||
				string.IsNullOrEmpty(quantityText) || string.IsNullOrEmpty(supplierName) || selectedWarehouse == null)
			{
				MessageBox.Show("Пожалуйста, заполните все поля и выберите склад.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!decimal.TryParse(priceText, out decimal price) || price < 0)
			{
				MessageBox.Show("Цена должна быть положительным числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!int.TryParse(quantityText, out int quantity) || quantity < 0)
			{
				MessageBox.Show("Количество должно быть положительным целым числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			try
			{
				using var connection = new SqlConnection(connectionString);
				connection.Open();
				using var transaction = connection.BeginTransaction();

				// Получение или вставка поставщика
				var supplier = GetOrInsertSupplier(supplierName, connection, transaction);
				// Получение или вставка товара
				var product = GetOrInsertProduct(name, price, supplier.Id, connection, transaction);

				const string updateInventoryQuery = @"
                    UPDATE СкладскиеПозиции
                    SET ТоварID = @ItemId, СкладID = @WarehouseId, Количество = @Quantity, ДатаОбновления = GETDATE()
                    WHERE ПозицияID = @Id";

				using var command = new SqlCommand(updateInventoryQuery, connection, transaction);
				command.Parameters.AddWithValue("@ItemId", product.Id);
				command.Parameters.AddWithValue("@WarehouseId", selectedWarehouse.Id);
				command.Parameters.AddWithValue("@Quantity", quantity);
				command.Parameters.AddWithValue("@Id", SelectedItem.Id);

				int rowsAffected = command.ExecuteNonQuery();
				if (rowsAffected > 0)
				{
					transaction.Commit();
					// Обновление локальной коллекции
					SelectedItem.Name = product.Name;
					SelectedItem.Price = product.Price;
					SelectedItem.Quantity = quantity;
					SelectedItem.Location = selectedWarehouse.Name;

					MessageBox.Show("Позиция успешно обновлена.", "Редактирование позиции", MessageBoxButton.OK, MessageBoxImage.Information);
					RefreshInventoryDataGrid();
					HidePanel();
					ClearInputFields();
				}
				else
				{
					transaction.Rollback();
					MessageBox.Show("Не удалось обновить позицию.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			catch (SqlException ex)
			{
				MessageBox.Show($"Ошибка обновления в базе данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// Удаление позиции, а также удаление товара и поставщика (если не используются в других записях)
		/// </summary>
		private void DeleteInventoryPosition(InventoryItem selectedItem)
		{
			try
			{
				using var connection = new SqlConnection(connectionString);
				connection.Open();
				using var transaction = connection.BeginTransaction();

				int productId = 0;
				int supplierId = 0;

				// Получение идентификаторов товара и поставщика
				const string getProductQuery = @"
                    SELECT t.ТоварID, t.ПоставщикID
                    FROM СкладскиеПозиции sp
                    INNER JOIN Товары t ON sp.ТоварID = t.ТоварID
                    WHERE sp.ПозицияID = @PositionId";
				using (var command = new SqlCommand(getProductQuery, connection, transaction))
				{
					command.Parameters.AddWithValue("@PositionId", selectedItem.Id);
					using var reader = command.ExecuteReader();
					if (reader.Read())
					{
						productId = reader.GetInt32(reader.GetOrdinal("ТоварID"));
						supplierId = reader.GetInt32(reader.GetOrdinal("ПоставщикID"));
					}
				}

				// Удаление позиции
				const string deletePositionQuery = "DELETE FROM СкладскиеПозиции WHERE ПозицияID = @Id";
				using (var command = new SqlCommand(deletePositionQuery, connection, transaction))
				{
					command.Parameters.AddWithValue("@Id", selectedItem.Id);
					command.ExecuteNonQuery();
				}

				// Проверка, используется ли товар в других позициях
				bool isProductUsedElsewhere = false;
				const string checkProductUsageQuery = "SELECT COUNT(*) FROM СкладскиеПозиции WHERE ТоварID = @ProductId";
				using (var command = new SqlCommand(checkProductUsageQuery, connection, transaction))
				{
					command.Parameters.AddWithValue("@ProductId", productId);
					int count = (int)command.ExecuteScalar();
					isProductUsedElsewhere = count > 0;
				}

				if (!isProductUsedElsewhere)
				{
					// Удаление товара
					const string deleteProductQuery = "DELETE FROM Товары WHERE ТоварID = @ProductId";
					using (var command = new SqlCommand(deleteProductQuery, connection, transaction))
					{
						command.Parameters.AddWithValue("@ProductId", productId);
						command.ExecuteNonQuery();
					}

					// Проверка использования поставщика
					bool isSupplierUsedElsewhere = false;
					const string checkSupplierUsageQuery = "SELECT COUNT(*) FROM Товары WHERE ПоставщикID = @SupplierId";
					using (var command = new SqlCommand(checkSupplierUsageQuery, connection, transaction))
					{
						command.Parameters.AddWithValue("@SupplierId", supplierId);
						int count = (int)command.ExecuteScalar();
						isSupplierUsedElsewhere = count > 0;
					}

					if (!isSupplierUsedElsewhere)
					{
						// Удаление поставщика
						const string deleteSupplierQuery = "DELETE FROM Поставщики WHERE ПоставщикID = @SupplierId";
						using (var command = new SqlCommand(deleteSupplierQuery, connection, transaction))
						{
							command.Parameters.AddWithValue("@SupplierId", supplierId);
							command.ExecuteNonQuery();
						}
					}
				}

				transaction.Commit();

				// Обновление локальных коллекций
				InventoryItems.Remove(selectedItem);
				RefreshInventoryDataGrid();

				if (!isProductUsedElsewhere)
				{
					var productToRemove = Products.FirstOrDefault(p => p.Id == productId);
					if (productToRemove != null)
						Products.Remove(productToRemove);
				}

				if (!isProductUsedElsewhere && supplierId > 0)
				{
					bool isSupplierUsedInLocalProducts = Products.Any(p => p.SupplierId == supplierId);
					if (!isSupplierUsedInLocalProducts)
					{
						var supplierToRemove = Suppliers.FirstOrDefault(s => s.Id == supplierId);
						if (supplierToRemove != null)
							Suppliers.Remove(supplierToRemove);
					}
				}

				MessageBox.Show("Позиция и связанные данные успешно удалены.", "Удаление позиции", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (SqlException ex)
			{
				MessageBox.Show($"Ошибка удаления из базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		#endregion

		#region Методы темы

		public void UpdateThemeIcon()
		{
			if (ThemeIcon != null)
			{
				ThemeIcon.Kind = ThemeManager.IsDarkTheme ? PackIconMaterialKind.WeatherNight : PackIconMaterialKind.WeatherSunny;
			}
		}

		#endregion
	}
}
