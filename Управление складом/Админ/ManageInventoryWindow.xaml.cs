using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes; // Для ThemeManager

namespace УправлениеСкладом
{
	public partial class ManageInventoryWindow : Window, IThemeable
	{
		// Модель складской позиции (для DataGrid)
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
			public decimal Price { get; set; } // Цена товара
			public int SupplierId { get; set; } // ID поставщика
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

		// Коллекции данных
		private ObservableCollection<InventoryItem> InventoryItems;
		private List<Product> Products;
		private List<Warehouse> Warehouses;
		private List<Supplier> Suppliers;

		private bool IsEditMode = false;
		private InventoryItem SelectedItem;
		private string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		public ManageInventoryWindow()
		{
			InitializeComponent();
			LoadProducts();
			LoadWarehouses();
			LoadSuppliers();
			LoadInventory();
			UpdateThemeIcon();
		}

		private void LoadProducts()
		{
			Products = new List<Product>();
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = "SELECT ТоварID, Наименование, Цена, ПоставщикID FROM Товары";
					using (SqlCommand command = new SqlCommand(query, connection))
					using (SqlDataReader reader = command.ExecuteReader())
					{
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
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

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
					using (SqlDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							Warehouses.Add(new Warehouse
							{
								Id = reader.GetInt32(reader.GetOrdinal("СкладID")),
								Name = reader.GetString(reader.GetOrdinal("Наименование"))
							});
						}
					}
					WarehouseComboBox.ItemsSource = Warehouses;
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки складов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

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
					using (SqlDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							Suppliers.Add(new Supplier
							{
								Id = reader.GetInt32(reader.GetOrdinal("ПоставщикID")),
								Name = reader.GetString(reader.GetOrdinal("Наименование"))
							});
						}
					}
					SupplierComboBox.ItemsSource = Suppliers;
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки поставщиков: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void LoadInventory()
		{
			InventoryItems = new ObservableCollection<InventoryItem>();
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = @"
                        SELECT sp.ПозицияID AS Id, 
                               t.Наименование AS Name, 
                               t.Цена AS Price, 
                               sp.Количество AS Quantity, 
                               s.Наименование AS Location
                        FROM СкладскиеПозиции sp
                        INNER JOIN Товары t ON sp.ТоварID = t.ТоварID
                        INNER JOIN Склады s ON sp.СкладID = s.СкладID";
					using (SqlCommand command = new SqlCommand(query, connection))
					using (SqlDataReader reader = command.ExecuteReader())
					{
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
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			RefreshInventoryDataGrid();
		}

		private void RefreshInventoryDataGrid()
		{
			InventoryDataGrid.ItemsSource = null;
			InventoryDataGrid.ItemsSource = InventoryItems;
		}

		private void ClearInputFields()
		{
			NameTextBox.Text = "";
			PriceTextBox.Text = "";
			QuantityTextBox.Text = "";
			SupplierComboBox.SelectedIndex = -1;
			WarehouseComboBox.SelectedIndex = -1;
		}

		private void PopulateInputFields(InventoryItem item)
		{
			NameTextBox.Text = item.Name;
			PriceTextBox.Text = item.Price.ToString("F2");
			QuantityTextBox.Text = item.Quantity.ToString();
			WarehouseComboBox.SelectedItem = Warehouses.FirstOrDefault(w => w.Name == item.Location);

			Product product = Products.FirstOrDefault(p =>
				p.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
			if (product != null)
			{
				Supplier supplier = Suppliers.FirstOrDefault(s => s.Id == product.SupplierId);
				SupplierComboBox.SelectedItem = supplier;
			}
			else
			{
				SupplierComboBox.SelectedIndex = -1;
			}
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void AddInventory_Click(object sender, RoutedEventArgs e)
		{
			IsEditMode = false;
			PanelTitle.Text = "Добавить складскую позицию";
			ClearInputFields();
			ShowPanel();
		}

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

		private void DeleteInventory_Click(object sender, RoutedEventArgs e)
		{
			if (InventoryDataGrid.SelectedItem is InventoryItem selectedItem)
			{
				MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить позицию: {selectedItem.Name}?",
					"Удаление позиции", MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result == MessageBoxResult.Yes)
				{
					using (SqlConnection connection = new SqlConnection(connectionString))
					{
						try
						{
							connection.Open();
							using (SqlTransaction transaction = connection.BeginTransaction())
							{
								try
								{
									int productId = 0;
									int supplierId = 0;
									string getProductQuery = @"
                                        SELECT t.ТоварID, t.ПоставщикID
                                        FROM СкладскиеПозиции sp
                                        INNER JOIN Товары t ON sp.ТоварID = t.ТоварID
                                        WHERE sp.ПозицияID = @PositionId";
									using (SqlCommand cmd = new SqlCommand(getProductQuery, connection, transaction))
									{
										cmd.Parameters.AddWithValue("@PositionId", selectedItem.Id);
										using (SqlDataReader reader = cmd.ExecuteReader())
										{
											if (reader.Read())
											{
												productId = reader.GetInt32(reader.GetOrdinal("ТоварID"));
												supplierId = reader.GetInt32(reader.GetOrdinal("ПоставщикID"));
											}
										}
									}

									string deletePositionQuery = "DELETE FROM СкладскиеПозиции WHERE ПозицияID = @PosId";
									using (SqlCommand cmd = new SqlCommand(deletePositionQuery, connection, transaction))
									{
										cmd.Parameters.AddWithValue("@PosId", selectedItem.Id);
										cmd.ExecuteNonQuery();
									}

									// Проверяем, остались ли ещё позиции, связанные с этим товаром
									bool isProductUsedElsewhere = false;
									string checkProdUsageQuery = "SELECT COUNT(*) FROM СкладскиеПозиции WHERE ТоварID = @ProdId";
									using (SqlCommand cmd = new SqlCommand(checkProdUsageQuery, connection, transaction))
									{
										cmd.Parameters.AddWithValue("@ProdId", productId);
										int count = (int)cmd.ExecuteScalar();
										isProductUsedElsewhere = (count > 0);
									}

									// Если товар больше не используется, удаляем его
									if (!isProductUsedElsewhere)
									{
										string deleteProductQuery = "DELETE FROM Товары WHERE ТоварID = @ProdId";
										using (SqlCommand cmd = new SqlCommand(deleteProductQuery, connection, transaction))
										{
											cmd.Parameters.AddWithValue("@ProdId", productId);
											cmd.ExecuteNonQuery();
										}

										// Проверяем, остались ли товары, связанные с поставщиком
										bool isSupplierUsedElsewhere = false;
										string checkSupplierUsageQuery = "SELECT COUNT(*) FROM Товары WHERE ПоставщикID = @SupId";
										using (SqlCommand cmd = new SqlCommand(checkSupplierUsageQuery, connection, transaction))
										{
											cmd.Parameters.AddWithValue("@SupId", supplierId);
											int count = (int)cmd.ExecuteScalar();
											isSupplierUsedElsewhere = (count > 0);
										}

										// Если больше нет товаров у поставщика — удаляем и его
										if (!isSupplierUsedElsewhere)
										{
											string deleteSupplierQuery = "DELETE FROM Поставщики WHERE ПоставщикID = @SupId";
											using (SqlCommand cmd = new SqlCommand(deleteSupplierQuery, connection, transaction))
											{
												cmd.Parameters.AddWithValue("@SupId", supplierId);
												cmd.ExecuteNonQuery();
											}
										}
									}

									transaction.Commit();
									InventoryItems.Remove(selectedItem);
									RefreshInventoryDataGrid();

									// Удаляем из локального списка Products, если товар удалён из БД
									if (!isProductUsedElsewhere)
									{
										Product productToRemove = Products.FirstOrDefault(p => p.Id == productId);
										if (productToRemove != null)
											Products.Remove(productToRemove);
									}

									// Если мы удалили товар и, соответственно, проверили, что поставщик более не используется
									if (!isProductUsedElsewhere && supplierId > 0)
									{
										bool isSupplierUsedInLocalProducts = Products.Any(p => p.SupplierId == supplierId);
										if (!isSupplierUsedInLocalProducts)
										{
											Supplier supplierToRemove = Suppliers.FirstOrDefault(s => s.Id == supplierId);
											if (supplierToRemove != null)
												Suppliers.Remove(supplierToRemove);
										}
									}

									MessageBox.Show("Позиция и связанные данные успешно удалены.",
										"Удаление позиции", MessageBoxButton.OK, MessageBoxImage.Information);
								}
								catch (Exception ex)
								{
									transaction.Rollback();
									MessageBox.Show($"Ошибка удаления из базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
								}
							}
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

		private void SaveInventory_Click(object sender, RoutedEventArgs e)
		{
			if (IsEditMode)
				SaveEditInventory();
			else
				SaveAddInventory();
		}

		// Сохранение новой позиции
		private void SaveAddInventory()
		{
			string name = NameTextBox.Text.Trim();
			string priceText = PriceTextBox.Text.Trim();
			string quantityText = QuantityTextBox.Text.Trim();
			string supplierName = SupplierComboBox.Text.Trim();
			Warehouse selectedWarehouse = WarehouseComboBox.SelectedItem as Warehouse;

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

			Supplier selectedSupplier = Suppliers
				.FirstOrDefault(s => s.Name.Equals(supplierName, StringComparison.OrdinalIgnoreCase));

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					// Если поставщика нет в списке — добавим его
					if (selectedSupplier == null)
					{
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

					// Проверяем, есть ли уже такой товар
					Product selectedProduct = Products
						.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

					if (selectedProduct == null)
					{
						// Добавляем новый товар
						string insertProductQuery = @"
                            INSERT INTO Товары (Наименование, Цена, ПоставщикID)
                            VALUES (@ProductName, @Price, @SupplierID);
                            SELECT CAST(scope_identity() AS int);";
						using (SqlCommand command = new SqlCommand(insertProductQuery, connection))
						{
							command.Parameters.AddWithValue("@ProductName", name);
							command.Parameters.AddWithValue("@Price", price);
							command.Parameters.AddWithValue("@SupplierID", selectedSupplier.Id);
							object result = command.ExecuteScalar();
							int newProductId = (result != null) ? (int)result : 0;
							if (newProductId > 0)
							{
								selectedProduct = new Product
								{
									Id = newProductId,
									Name = name,
									Price = price,
									SupplierId = selectedSupplier.Id
								};
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
						// Обновляем цену, если отличается
						if (selectedProduct.Price != price)
						{
							string updateProductQuery = "UPDATE Товары SET Цена = @Price WHERE ТоварID = @ProductId";
							using (SqlCommand command = new SqlCommand(updateProductQuery, connection))
							{
								command.Parameters.AddWithValue("@Price", price);
								command.Parameters.AddWithValue("@ProductId", selectedProduct.Id);
								command.ExecuteNonQuery();
								selectedProduct.Price = price;
							}
						}
					}

					// Добавляем запись в СкладскиеПозиции
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
						object result = command.ExecuteScalar();
						newId = (result != null) ? (int)result : 0;
					}

					if (newId > 0)
					{
						InventoryItems.Add(new InventoryItem
						{
							Id = newId,
							Name = selectedProduct.Name,
							Price = selectedProduct.Price,
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
			string priceText = PriceTextBox.Text.Trim();
			string quantityText = QuantityTextBox.Text.Trim();
			string supplierName = SupplierComboBox.Text.Trim();
			Warehouse selectedWarehouse = WarehouseComboBox.SelectedItem as Warehouse;

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

			Supplier selectedSupplier = Suppliers
				.FirstOrDefault(s => s.Name.Equals(supplierName, StringComparison.OrdinalIgnoreCase));

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					// Если поставщика нет в списке — добавим его
					if (selectedSupplier == null)
					{
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

					// Проверяем, есть ли уже такой товар
					Product selectedProduct = Products
						.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

					if (selectedProduct == null)
					{
						// Добавляем новый товар
						string insertProductQuery = @"
                            INSERT INTO Товары (Наименование, Цена, ПоставщикID)
                            VALUES (@ProductName, @Price, @SupplierID);
                            SELECT CAST(scope_identity() AS int);";
						using (SqlCommand command = new SqlCommand(insertProductQuery, connection))
						{
							command.Parameters.AddWithValue("@ProductName", name);
							command.Parameters.AddWithValue("@Price", price);
							command.Parameters.AddWithValue("@SupplierID", selectedSupplier.Id);
							object result = command.ExecuteScalar();
							int newProductId = (result != null) ? (int)result : 0;
							if (newProductId > 0)
							{
								selectedProduct = new Product
								{
									Id = newProductId,
									Name = name,
									Price = price,
									SupplierId = selectedSupplier.Id
								};
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
						// Обновляем цену, если отличается
						if (selectedProduct.Price != price)
						{
							string updateProductQuery = "UPDATE Товары SET Цена = @Price WHERE ТоварID = @ProductId";
							using (SqlCommand command = new SqlCommand(updateProductQuery, connection))
							{
								command.Parameters.AddWithValue("@Price", price);
								command.Parameters.AddWithValue("@ProductId", selectedProduct.Id);
								command.ExecuteNonQuery();
								selectedProduct.Price = price;
							}
						}
					}

					// Обновляем позицию
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
							SelectedItem.Name = selectedProduct.Name;
							SelectedItem.Price = selectedProduct.Price;
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

		// --- Показ / скрытие панели добавления/редактирования ---
		private void ShowPanel()
		{
			RootGrid.ColumnDefinitions[1].Width = new GridLength(400);
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

		private void CancelInventory_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}

		// --- Переключение темы ---
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

		// --- Просмотр QR-кода ---
		private void ShowQr_Click(object sender, RoutedEventArgs e)
		{
			if (InventoryDataGrid.SelectedItem is InventoryItem selectedInventory)
			{
				var qrWindow = new ViewQrWindow(selectedInventory.Id);
				qrWindow.Owner = this;
				qrWindow.ShowDialog();
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите позицию для просмотра QR-кода.", "Информация", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
	}
}
