// ManageInventoryWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;

namespace УправлениеСкладом
{
	public partial class ManageInventoryWindow : Window, IThemeable
	{
		// Модель складской позиции
		public class InventoryItem
		{
			public int Id { get; set; }
			public string Name { get; set; } // Название товара
			public int Quantity { get; set; }
			public string Location { get; set; } // Название склада
		}

		// Модель товара
		public class Product
		{
			public int Id { get; set; }
			public string Name { get; set; }
			public int SupplierId { get; set; } // Добавлено поле ПоставщикID
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

		// Коллекции для DataGrid и хранения данных товаров, складов и поставщиков
		private ObservableCollection<InventoryItem> InventoryItems;
		private List<Product> Products;
		private List<Warehouse> Warehouses;
		private List<Supplier> Suppliers;

		private bool IsEditMode = false;
		private InventoryItem SelectedItem;
		private string connectionString = "Data Source=DESKTOP-Q11QP9V\\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True"; // Замените на вашу строку подключения

		public ManageInventoryWindow()
		{
			InitializeComponent();
			LoadProducts();
			LoadWarehouses();
			LoadSuppliers(); // Загрузка поставщиков
			LoadInventory();
			UpdateThemeIcon();
		}

		// Загрузка товаров из базы данных
		private void LoadProducts()
		{
			Products = new List<Product>();

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = "SELECT ТоварID, Наименование, ПоставщикID FROM Товары";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								Products.Add(new Product
								{
									Id = reader.GetInt32(0),
									Name = reader.GetString(1),
									SupplierId = reader.GetInt32(2)
								});
							}
						}
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Загрузка складов из базы данных
		private void LoadWarehouses()
		{
			Warehouses = new List<Warehouse>();

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = "SELECT СкладID, Наименование FROM Склады";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								Warehouses.Add(new Warehouse
								{
									Id = reader.GetInt32(0),
									Name = reader.GetString(1)
								});
							}
						}
					}

					// Привязка списка складов к ComboBox
					WarehouseComboBox.ItemsSource = Warehouses;
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки складов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Загрузка поставщиков из базы данных
		private void LoadSuppliers()
		{
			Suppliers = new List<Supplier>();

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = "SELECT ПоставщикID, Наименование FROM Поставщики";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								Suppliers.Add(new Supplier
								{
									Id = reader.GetInt32(0),
									Name = reader.GetString(1)
								});
							}
						}
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки поставщиков: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Загрузка складских позиций из базы данных
		private void LoadInventory()
		{
			InventoryItems = new ObservableCollection<InventoryItem>();

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = @"
                        SELECT sp.ПозицияID AS Id, t.Наименование AS Name, sp.Количество AS Quantity, s.Наименование AS Location
                        FROM СкладскиеПозиции sp
                        INNER JOIN Товары t ON sp.ТоварID = t.ТоварID
                        INNER JOIN Склады s ON sp.СкладID = s.СкладID";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								InventoryItems.Add(new InventoryItem
								{
									Id = reader.GetInt32(0),
									Name = reader.GetString(1),
									Quantity = reader.GetInt32(2),
									Location = reader.GetString(3)
								});
							}
						}
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}

			RefreshInventoryDataGrid();
		}

		// Обновление DataGrid
		private void RefreshInventoryDataGrid()
		{
			InventoryDataGrid.ItemsSource = null;
			InventoryDataGrid.ItemsSource = InventoryItems;
		}

		// Закрытие окна
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		// Показать панель добавления
		private void AddInventory_Click(object sender, RoutedEventArgs e)
		{
			IsEditMode = false;
			PanelTitle.Text = "Добавить складскую позицию";
			ClearInputFields();
			ShowPanel();
		}

		// Показать панель редактирования
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

		// Удаление складской позиции
		private void DeleteInventory_Click(object sender, RoutedEventArgs e)
		{
			if (InventoryDataGrid.SelectedItem is InventoryItem selectedItem)
			{
				MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить позицию: {selectedItem.Name}?", "Удаление позиции", MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result == MessageBoxResult.Yes)
				{
					using (SqlConnection connection = new SqlConnection(connectionString))
					{
						try
						{
							connection.Open();
							string query = "DELETE FROM СкладскиеПозиции WHERE ПозицияID = @Id";
							using (SqlCommand command = new SqlCommand(query, connection))
							{
								command.Parameters.AddWithValue("@Id", selectedItem.Id);
								command.ExecuteNonQuery();
							}

							InventoryItems.Remove(selectedItem);
							RefreshInventoryDataGrid();
							MessageBox.Show("Позиция успешно удалена.", "Удаление позиции", MessageBoxButton.OK, MessageBoxImage.Information);
						}
						catch (SqlException ex)
						{
							MessageBox.Show($"Ошибка удаления из базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите позицию для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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
		private void CancelInventory_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}

		// Сохранение новой позиции или обновление существующей
		private void SaveInventory_Click(object sender, RoutedEventArgs e)
		{
			if (IsEditMode)
			{
				SaveEditInventory();
			}
			else
			{
				SaveAddInventory();
			}
		}

		// Сохранение новой позиции
		private void SaveAddInventory()
		{
			string name = NameTextBox.Text.Trim();
			string quantityText = QuantityTextBox.Text.Trim();
			string supplierName = SupplierTextBox.Text.Trim();
			Warehouse selectedWarehouse = WarehouseComboBox.SelectedItem as Warehouse;

			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(quantityText) || string.IsNullOrEmpty(supplierName) || selectedWarehouse == null)
			{
				MessageBox.Show("Пожалуйста, заполните все поля и выберите склад.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!int.TryParse(quantityText, out int quantity) || quantity < 0)
			{
				MessageBox.Show("Количество должно быть положительным целым числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			// Найти или добавить поставщика
			Supplier selectedSupplier = Suppliers.FirstOrDefault(s => s.Name.Equals(supplierName, StringComparison.OrdinalIgnoreCase));
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();

					if (selectedSupplier == null)
					{
						// Добавить нового поставщика
						string insertSupplierQuery = @"
                            INSERT INTO Поставщики (Наименование)
                            VALUES (@SupplierName);
                            SELECT CAST(scope_identity() AS int);";

						using (SqlCommand command = new SqlCommand(insertSupplierQuery, connection))
						{
							command.Parameters.AddWithValue("@SupplierName", supplierName);
							object result = command.ExecuteScalar();
							int newSupplierId = (result != null) ? (int)result : 0;
							if (newSupplierId > 0)
							{
								selectedSupplier = new Supplier { Id = newSupplierId, Name = supplierName };
								Suppliers.Add(selectedSupplier);
							}
							else
							{
								MessageBox.Show("Не удалось добавить нового поставщика.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
								return;
							}
						}
					}

					// Найти или добавить товар
					Product selectedProduct = Products.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
					if (selectedProduct == null)
					{
						string insertProductQuery = @"
                            INSERT INTO Товары (Наименование, ПоставщикID)
                            VALUES (@ProductName, @SupplierID);
                            SELECT CAST(scope_identity() AS int);";

						using (SqlCommand command = new SqlCommand(insertProductQuery, connection))
						{
							command.Parameters.AddWithValue("@ProductName", name);
							command.Parameters.AddWithValue("@SupplierID", selectedSupplier.Id);
							object result = command.ExecuteScalar();
							int newProductId = (result != null) ? (int)result : 0;
							if (newProductId > 0)
							{
								selectedProduct = new Product { Id = newProductId, Name = name, SupplierId = selectedSupplier.Id };
								Products.Add(selectedProduct);
							}
							else
							{
								MessageBox.Show("Не удалось добавить новый товар.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
								return;
							}
						}
					}

					// Теперь можем добавить складскую позицию
					string insertInventoryQuery = @"
                        INSERT INTO СкладскиеПозиции (ТоварID, СкладID, Количество)
                        VALUES (@ItemId, @WarehouseId, @Quantity);
                        SELECT CAST(scope_identity() AS int);";

					int newId;
					using (SqlCommand command = new SqlCommand(insertInventoryQuery, connection))
					{
						command.Parameters.AddWithValue("@ItemId", selectedProduct.Id);
						command.Parameters.AddWithValue("@WarehouseId", selectedWarehouse.Id);
						command.Parameters.AddWithValue("@Quantity", quantity);

						// Получение сгенерированного ID
						object result = command.ExecuteScalar();
						newId = (result != null) ? (int)result : 0;
					}

					if (newId > 0)
					{
						InventoryItems.Add(new InventoryItem
						{
							Id = newId,
							Name = selectedProduct.Name,
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
						MessageBox.Show("Не удалось добавить позицию.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка сохранения в базу данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Сохранение изменений в существующей позиции
		private void SaveEditInventory()
		{
			string name = NameTextBox.Text.Trim();
			string quantityText = QuantityTextBox.Text.Trim();
			string supplierName = SupplierTextBox.Text.Trim();
			Warehouse selectedWarehouse = WarehouseComboBox.SelectedItem as Warehouse;

			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(quantityText) || string.IsNullOrEmpty(supplierName) || selectedWarehouse == null)
			{
				MessageBox.Show("Пожалуйста, заполните все поля и выберите склад.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!int.TryParse(quantityText, out int quantity) || quantity < 0)
			{
				MessageBox.Show("Количество должно быть положительным целым числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			// Найти или добавить поставщика
			Supplier selectedSupplier = Suppliers.FirstOrDefault(s => s.Name.Equals(supplierName, StringComparison.OrdinalIgnoreCase));
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();

					if (selectedSupplier == null)
					{
						// Добавить нового поставщика
						string insertSupplierQuery = @"
                            INSERT INTO Поставщики (Наименование)
                            VALUES (@SupplierName);
                            SELECT CAST(scope_identity() AS int);";

						using (SqlCommand command = new SqlCommand(insertSupplierQuery, connection))
						{
							command.Parameters.AddWithValue("@SupplierName", supplierName);
							object result = command.ExecuteScalar();
							int newSupplierId = (result != null) ? (int)result : 0;
							if (newSupplierId > 0)
							{
								selectedSupplier = new Supplier { Id = newSupplierId, Name = supplierName };
								Suppliers.Add(selectedSupplier);
							}
							else
							{
								MessageBox.Show("Не удалось добавить нового поставщика.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
								return;
							}
						}
					}

					// Найти или добавить товар
					Product selectedProduct = Products.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
					if (selectedProduct == null)
					{
						string insertProductQuery = @"
                            INSERT INTO Товары (Наименование, ПоставщикID)
                            VALUES (@ProductName, @SupplierID);
                            SELECT CAST(scope_identity() AS int);";

						using (SqlCommand command = new SqlCommand(insertProductQuery, connection))
						{
							command.Parameters.AddWithValue("@ProductName", name);
							command.Parameters.AddWithValue("@SupplierID", selectedSupplier.Id);
							object result = command.ExecuteScalar();
							int newProductId = (result != null) ? (int)result : 0;
							if (newProductId > 0)
							{
								selectedProduct = new Product { Id = newProductId, Name = name, SupplierId = selectedSupplier.Id };
								Products.Add(selectedProduct);
							}
							else
							{
								MessageBox.Show("Не удалось добавить новый товар.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
								return;
							}
						}
					}
					else
					{
						// Обновить ПоставщикID у товара, если необходимо
						if (selectedProduct.SupplierId != selectedSupplier.Id)
						{
							string updateProductQuery = "UPDATE Товары SET ПоставщикID = @SupplierID WHERE ТоварID = @ProductId";
							using (SqlCommand command = new SqlCommand(updateProductQuery, connection))
							{
								command.Parameters.AddWithValue("@SupplierID", selectedSupplier.Id);
								command.Parameters.AddWithValue("@ProductId", selectedProduct.Id);
								command.ExecuteNonQuery();
								selectedProduct.SupplierId = selectedSupplier.Id;
							}
						}
					}

					// Теперь можем обновить складскую позицию
					string updateInventoryQuery = @"
                        UPDATE СкладскиеПозиции
                        SET ТоварID = @ItemId, СкладID = @WarehouseId, Количество = @Quantity, ДатаОбновления = GETDATE()
                        WHERE ПозицияID = @Id";

					using (SqlCommand command = new SqlCommand(updateInventoryQuery, connection))
					{
						command.Parameters.AddWithValue("@ItemId", selectedProduct.Id);
						command.Parameters.AddWithValue("@WarehouseId", selectedWarehouse.Id);
						command.Parameters.AddWithValue("@Quantity", quantity);
						command.Parameters.AddWithValue("@Id", SelectedItem.Id);

						int rowsAffected = command.ExecuteNonQuery();

						if (rowsAffected > 0)
						{
							// Обновление локального объекта
							SelectedItem.Name = selectedProduct.Name;
							SelectedItem.Quantity = quantity;
							SelectedItem.Location = selectedWarehouse.Name;
							MessageBox.Show("Позиция успешно обновлена.", "Редактирование позиции", MessageBoxButton.OK, MessageBoxImage.Information);
							RefreshInventoryDataGrid();
							HidePanel();
							ClearInputFields();
						}
						else
						{
							MessageBox.Show("Не удалось обновить позицию.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
			NameTextBox.Text = string.Empty;
			QuantityTextBox.Text = string.Empty;
			SupplierTextBox.Text = string.Empty;
			WarehouseComboBox.SelectedIndex = -1;
		}

		// Заполнение полей редактирования
		private void PopulateInputFields(InventoryItem item)
		{
			NameTextBox.Text = item.Name;
			QuantityTextBox.Text = item.Quantity.ToString();
			WarehouseComboBox.SelectedItem = Warehouses.FirstOrDefault(w => w.Name == item.Location);

			// Найти товар и поставить соответствующего поставщика
			Product product = Products.FirstOrDefault(p => p.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
			if (product != null)
			{
				Supplier supplier = Suppliers.FirstOrDefault(s => s.Id == product.SupplierId);
				if (supplier != null)
				{
					SupplierTextBox.Text = supplier.Name;
				}
				else
				{
					SupplierTextBox.Text = string.Empty;
				}
			}
			else
			{
				SupplierTextBox.Text = string.Empty;
			}
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
