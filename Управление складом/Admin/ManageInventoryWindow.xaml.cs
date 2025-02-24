using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;
using УправлениеСкладом.QR;
using Vosk;
using Управление_складом.Class;

namespace УправлениеСкладом
{
	public partial class ManageInventoryWindow : Window, IThemeable
	{
		// Модель складской позиции (для DataGrid)
		public class InventoryItem
		{
			public int Id { get; set; }        // ПозицияID (если записи нет – будет равен ProductId)
			public int ProductId { get; set; } // ТоварID
			public string Name { get; set; }   // Название товара
			public string Category { get; set; } // Категория товара
			public decimal Price { get; set; } // Цена товара
			public int Quantity { get; set; }
			public string Location { get; set; } // Название склада или "Нет на складе"
		}

		// Модель товара
		public class Product
		{
			public int Id { get; set; }
			public string Name { get; set; }
			public decimal Price { get; set; } // Цена товара
			public int SupplierId { get; set; } // ID поставщика
			public string Category { get; set; } // Категория товара
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

		// Параметры голосового ввода
		private Model modelRu;
		private VoiceInputService voiceService;

		public ManageInventoryWindow()
		{
			InitializeComponent();
			LoadProducts();
			LoadWarehouses();
			LoadSuppliers();
			LoadInventory();
			UpdateThemeIcon();

			// Подписка на событие Loaded для инициализации Vosk
			this.Loaded += ManageInventoryWindow_Loaded;
		}

		private async void ManageInventoryWindow_Loaded(object sender, RoutedEventArgs e)
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
						ApplyFilters();
					});
				};
			}
		}

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
					await Dispatcher.InvokeAsync(() =>
						MessageBox.Show("Отсутствует офлайн-модель Vosk для ru в папке Models.",
						"Ошибка инициализации Vosk", MessageBoxButton.OK, MessageBoxImage.Warning));
				}
			}
			catch (Exception ex)
			{
				await Dispatcher.InvokeAsync(() =>
					MessageBox.Show("Ошибка инициализации Vosk: " + ex.Message,
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error));
			}
		}

		// Загрузка всех товаров
		private void LoadProducts()
		{
			Products = new List<Product>();
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = "SELECT ТоварID, Наименование, Цена, ПоставщикID, Категория FROM Товары";
					using (SqlCommand command = new SqlCommand(query, connection))
					using (SqlDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							Products.Add(new Product
							{
								Id = reader.GetInt32(reader.GetOrdinal("ТоварID")),
								Name = reader.GetString(reader.GetOrdinal("Наименование")),
								Price = reader.IsDBNull(reader.GetOrdinal("Цена"))
									? 0m
									: reader.GetDecimal(reader.GetOrdinal("Цена")),
								SupplierId = reader.GetInt32(reader.GetOrdinal("ПоставщикID")),
								Category = reader.IsDBNull(reader.GetOrdinal("Категория"))
									? ""
									: reader.GetString(reader.GetOrdinal("Категория"))
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

		// Загрузка складов
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

		// Загрузка поставщиков
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

		// Загрузка данных для DataGrid:
		// Показываем ВСЕ товары (LEFT JOIN), даже если нет записи в СкладскиеПозиции.
		private void LoadInventory()
		{
			InventoryItems = new ObservableCollection<InventoryItem>();

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = @"
                        SELECT
                            t.ТоварID AS ProductId,
                            t.Наименование AS Name,
                            t.Цена AS Price,
                            t.Категория AS Category,
                            sp.ПозицияID AS PositionId,
                            sp.Количество AS Quantity,
                            s.Наименование AS Location
                        FROM Товары t
                        LEFT JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
                        LEFT JOIN Склады s ON sp.СкладID = s.СкладID;
                    ";

					using (SqlCommand command = new SqlCommand(query, connection))
					using (SqlDataReader reader = command.ExecuteReader())
					{
						bool foundAny = false;
						while (reader.Read())
						{
							foundAny = true;
							// Если записи в СкладскиеПозиции нет, используем ProductId в качестве Id
							int posId = reader.IsDBNull(reader.GetOrdinal("PositionId"))
								? reader.GetInt32(reader.GetOrdinal("ProductId"))
								: reader.GetInt32(reader.GetOrdinal("PositionId"));

							int quantity = reader.IsDBNull(reader.GetOrdinal("Quantity"))
								? 0
								: reader.GetInt32(reader.GetOrdinal("Quantity"));

							string location = reader.IsDBNull(reader.GetOrdinal("Location"))
								? "Нет на складе"
								: reader.GetString(reader.GetOrdinal("Location"));

							decimal price = reader.IsDBNull(reader.GetOrdinal("Price"))
								? 0m
								: reader.GetDecimal(reader.GetOrdinal("Price"));

							string category = reader.IsDBNull(reader.GetOrdinal("Category"))
								? ""
								: reader.GetString(reader.GetOrdinal("Category"));

							InventoryItems.Add(new InventoryItem
							{
								ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
								Id = posId,
								Name = reader.GetString(reader.GetOrdinal("Name")),
								Price = price,
								Quantity = quantity,
								Category = category,
								Location = location
							});
						}

						if (!foundAny)
						{
							MessageBox.Show("В базе нет ни одного товара.",
								"Информация", MessageBoxButton.OK, MessageBoxImage.Information);
						}
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}",
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			RefreshInventoryDataGrid();
		}

		private void RefreshInventoryDataGrid()
		{
			InventoryDataGrid.ItemsSource = null;
			InventoryDataGrid.ItemsSource = InventoryItems;
			ApplyFilters();
		}

		private void ClearInputFields()
		{
			NameTextBox.Text = "";
			CategoryTextBox.Text = "";
			PriceTextBox.Text = "";
			QuantityTextBox.Text = "";
			SupplierComboBox.SelectedIndex = -1;
			WarehouseComboBox.SelectedIndex = -1;
		}

		// Заполняем поля для редактирования
		private void PopulateInputFields(InventoryItem item)
		{
			NameTextBox.Text = item.Name;
			CategoryTextBox.Text = item.Category;
			PriceTextBox.Text = item.Price.ToString("F2");
			QuantityTextBox.Text = item.Quantity.ToString();
			WarehouseComboBox.SelectedItem = Warehouses.FirstOrDefault(w => w.Name == item.Location);

			// Ищем товар по ProductId
			Product product = Products.FirstOrDefault(p => p.Id == item.ProductId);
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
				MessageBox.Show("Пожалуйста, выберите позицию для редактирования.",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Удаление записи (с удалением записей из ДвиженияТоваров, СкладскихПозиции, Товаров и Поставщиков)
		private void DeleteInventory_Click(object sender, RoutedEventArgs e)
		{
			if (InventoryDataGrid.SelectedItem is InventoryItem selectedItem)
			{
				MessageBoxResult result = MessageBox.Show(
					$"Вы уверены, что хотите удалить позицию (товар): {selectedItem.Name}?",
					"Удаление позиции",
					MessageBoxButton.YesNo,
					MessageBoxImage.Question
				);
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
									int productId = selectedItem.ProductId;
									int supplierId = 0;

									// Получаем SupplierId из локального списка
									Product prod = Products.FirstOrDefault(p => p.Id == productId);
									if (prod != null)
									{
										supplierId = prod.SupplierId;
									}
									else
									{
										// Если не нашли, делаем запрос
										string getSupplierQuery = "SELECT ПоставщикID FROM Товары WHERE ТоварID = @ProdId";
										using (SqlCommand cmd = new SqlCommand(getSupplierQuery, connection, transaction))
										{
											cmd.Parameters.AddWithValue("@ProdId", productId);
											object obj = cmd.ExecuteScalar();
											if (obj != null)
												supplierId = Convert.ToInt32(obj);
										}
									}

									// 1. Удаляем все записи из ДвиженияТоваров для данного товара
									string deleteMovQuery = "DELETE FROM ДвиженияТоваров WHERE ТоварID = @ProdId";
									using (SqlCommand cmd = new SqlCommand(deleteMovQuery, connection, transaction))
									{
										cmd.Parameters.AddWithValue("@ProdId", productId);
										cmd.ExecuteNonQuery();
									}

									// 2. Если имеется запись в СкладскиеПозиции (Id != 0), удаляем её
									if (selectedItem.Id != 0)
									{
										string deletePosQuery = "DELETE FROM СкладскиеПозиции WHERE ПозицияID = @PosId";
										using (SqlCommand cmd = new SqlCommand(deletePosQuery, connection, transaction))
										{
											cmd.Parameters.AddWithValue("@PosId", selectedItem.Id);
											cmd.ExecuteNonQuery();
										}
									}

									// 3. Проверяем, используется ли товар в других позициях
									bool isProductUsedElsewhere = false;
									string checkProdQuery = "SELECT COUNT(*) FROM СкладскиеПозиции WHERE ТоварID = @ProdId";
									using (SqlCommand cmd = new SqlCommand(checkProdQuery, connection, transaction))
									{
										cmd.Parameters.AddWithValue("@ProdId", productId);
										int count = (int)cmd.ExecuteScalar();
										isProductUsedElsewhere = (count > 0);
									}

									// 4. Если товар больше нигде не используется, удаляем его
									if (!isProductUsedElsewhere)
									{
										string deleteProdQuery = "DELETE FROM Товары WHERE ТоварID = @ProdId";
										using (SqlCommand cmd = new SqlCommand(deleteProdQuery, connection, transaction))
										{
											cmd.Parameters.AddWithValue("@ProdId", productId);
											cmd.ExecuteNonQuery();
										}

										// 5. Проверяем, используется ли поставщик в других товарах
										bool isSupplierUsedElsewhere = false;
										string checkSuppQuery = "SELECT COUNT(*) FROM Товары WHERE ПоставщикID = @SupId";
										using (SqlCommand cmd = new SqlCommand(checkSuppQuery, connection, transaction))
										{
											cmd.Parameters.AddWithValue("@SupId", supplierId);
											int count = (int)cmd.ExecuteScalar();
											isSupplierUsedElsewhere = (count > 0);
										}

										// 6. Если поставщик не используется, удаляем его
										if (!isSupplierUsedElsewhere && supplierId != 0)
										{
											string deleteSuppQuery = "DELETE FROM Поставщики WHERE ПоставщикID = @SupId";
											using (SqlCommand cmd = new SqlCommand(deleteSuppQuery, connection, transaction))
											{
												cmd.Parameters.AddWithValue("@SupId", supplierId);
												cmd.ExecuteNonQuery();
											}
										}
									}

									transaction.Commit();

									// Удаляем запись из локальной коллекции и обновляем DataGrid
									InventoryItems.Remove(selectedItem);
									RefreshInventoryDataGrid();

									// Если товар удален, убираем его из локального списка
									if (!isProductUsedElsewhere)
									{
										Product prodToRemove = Products.FirstOrDefault(p => p.Id == productId);
										if (prodToRemove != null)
											Products.Remove(prodToRemove);
									}

									MessageBox.Show("Позиция и связанные данные успешно удалены (включая историю движений).",
										"Удаление позиции", MessageBoxButton.OK, MessageBoxImage.Information);
								}
								catch (Exception ex)
								{
									transaction.Rollback();
									MessageBox.Show($"Ошибка удаления из базы данных: {ex.Message}",
										"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
								}
							}
						}
						catch (SqlException ex)
						{
							MessageBox.Show($"Ошибка удаления из базы данных: {ex.Message}",
								"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите позицию для удаления.",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void SaveInventory_Click(object sender, RoutedEventArgs e)
		{
			if (IsEditMode)
				SaveEditInventory();
			else
				SaveAddInventory();
		}

		// Добавление новой позиции (создание записи в СкладскиеПозиции)
		private void SaveAddInventory()
		{
			string name = NameTextBox.Text.Trim();
			string category = CategoryTextBox.Text.Trim();
			string priceText = PriceTextBox.Text.Trim();
			string quantityText = QuantityTextBox.Text.Trim();
			string supplierName = SupplierComboBox.Text.Trim();
			Warehouse selectedWarehouse = WarehouseComboBox.SelectedItem as Warehouse;

			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(category) ||
				string.IsNullOrEmpty(priceText) || string.IsNullOrEmpty(quantityText) ||
				string.IsNullOrEmpty(supplierName) || selectedWarehouse == null)
			{
				MessageBox.Show("Пожалуйста, заполните все поля, включая категорию, и выберите склад.",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!decimal.TryParse(priceText, out decimal price) || price < 0)
			{
				MessageBox.Show("Цена должна быть положительным числом.",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!int.TryParse(quantityText, out int quantity) || quantity < 0)
			{
				MessageBox.Show("Количество должно быть положительным целым числом.",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			Supplier selectedSupplier = Suppliers.FirstOrDefault(s =>
				s.Name.Equals(supplierName, StringComparison.OrdinalIgnoreCase));

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					// Если поставщика нет, добавляем его
					if (selectedSupplier == null)
					{
						string insertSupplierQuery = @"
                            INSERT INTO Поставщики (Наименование)
                            VALUES (@SupplierName);
                            SELECT CAST(SCOPE_IDENTITY() AS int);";
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
								MessageBox.Show("Не удалось добавить нового поставщика.",
									"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
								return;
							}
						}
					}

					// Проверяем наличие товара
					Product selectedProduct = Products.FirstOrDefault(p =>
						p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

					if (selectedProduct == null)
					{
						string insertProductQuery = @"
                            INSERT INTO Товары (Наименование, Цена, ПоставщикID, Категория)
                            VALUES (@ProductName, @Price, @SupplierID, @Category);
                            SELECT CAST(SCOPE_IDENTITY() AS int);";
						using (SqlCommand command = new SqlCommand(insertProductQuery, connection))
						{
							command.Parameters.AddWithValue("@ProductName", name);
							command.Parameters.AddWithValue("@Price", price);
							command.Parameters.AddWithValue("@SupplierID", selectedSupplier.Id);
							command.Parameters.AddWithValue("@Category", category);
							object result = command.ExecuteScalar();
							int newProductId = (result != null) ? (int)result : 0;
							if (newProductId > 0)
							{
								selectedProduct = new Product
								{
									Id = newProductId,
									Name = name,
									Price = price,
									SupplierId = selectedSupplier.Id,
									Category = category
								};
								Products.Add(selectedProduct);
							}
							else
							{
								MessageBox.Show("Не удалось добавить новый товар.",
									"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
								return;
							}
						}
					}
					else
					{
						// Если товар существует, обновляем его при необходимости
						if (selectedProduct.Price != price ||
							!selectedProduct.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
						{
							string updateProductQuery = @"
                                UPDATE Товары 
                                SET Цена = @Price, Категория = @Category
                                WHERE ТоварID = @ProductId";
							using (SqlCommand command = new SqlCommand(updateProductQuery, connection))
							{
								command.Parameters.AddWithValue("@Price", price);
								command.Parameters.AddWithValue("@Category", category);
								command.Parameters.AddWithValue("@ProductId", selectedProduct.Id);
								command.ExecuteNonQuery();

								selectedProduct.Price = price;
								selectedProduct.Category = category;
							}
						}
					}

					// Добавляем новую запись в СкладскиеПозиции
					string insertInventoryQuery = @"
                        INSERT INTO СкладскиеПозиции (ТоварID, СкладID, Количество)
                        VALUES (@ItemId, @WarehouseId, @Quantity);
                        SELECT CAST(SCOPE_IDENTITY() AS int);";
					int newPosId;
					using (SqlCommand command = new SqlCommand(insertInventoryQuery, connection))
					{
						command.Parameters.AddWithValue("@ItemId", selectedProduct.Id);
						command.Parameters.AddWithValue("@WarehouseId", selectedWarehouse.Id);
						command.Parameters.AddWithValue("@Quantity", quantity);
						object result = command.ExecuteScalar();
						newPosId = (result != null) ? (int)result : 0;
					}

					if (newPosId > 0)
					{
						InventoryItems.Add(new InventoryItem
						{
							Id = newPosId, // ПозицияID
							ProductId = selectedProduct.Id,
							Name = selectedProduct.Name,
							Category = selectedProduct.Category,
							Price = selectedProduct.Price,
							Quantity = quantity,
							Location = selectedWarehouse.Name
						});
						MessageBox.Show("Позиция успешно добавлена.",
							"Добавление позиции", MessageBoxButton.OK, MessageBoxImage.Information);
						RefreshInventoryDataGrid();
						HidePanel();
						ClearInputFields();
					}
					else
					{
						MessageBox.Show("Не удалось добавить позицию.",
							"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка сохранения в базу данных: {ex.Message}",
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Редактирование существующей позиции (Id != 0)
		private void SaveEditInventory()
		{
			string name = NameTextBox.Text.Trim();
			string category = CategoryTextBox.Text.Trim();
			string priceText = PriceTextBox.Text.Trim();
			string quantityText = QuantityTextBox.Text.Trim();
			string supplierName = SupplierComboBox.Text.Trim();
			Warehouse selectedWarehouse = WarehouseComboBox.SelectedItem as Warehouse;

			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(category) ||
				string.IsNullOrEmpty(priceText) || string.IsNullOrEmpty(quantityText) ||
				string.IsNullOrEmpty(supplierName) || selectedWarehouse == null)
			{
				MessageBox.Show("Пожалуйста, заполните все поля, включая категорию, и выберите склад.",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!decimal.TryParse(priceText, out decimal price) || price < 0)
			{
				MessageBox.Show("Цена должна быть положительным числом.",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!int.TryParse(quantityText, out int quantity) || quantity < 0)
			{
				MessageBox.Show("Количество должно быть положительным целым числом.",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			Supplier selectedSupplier = Suppliers.FirstOrDefault(s =>
				s.Name.Equals(supplierName, StringComparison.OrdinalIgnoreCase));

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					// Если поставщика нет — добавляем
					if (selectedSupplier == null)
					{
						string insertSupplierQuery = @"
                            INSERT INTO Поставщики (Наименование)
                            VALUES (@SupplierName);
                            SELECT CAST(SCOPE_IDENTITY() AS int);";
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
								MessageBox.Show("Не удалось добавить нового поставщика.",
									"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
								return;
							}
						}
					}

					// Проверяем наличие товара
					Product selectedProduct = Products.FirstOrDefault(p =>
						p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

					if (selectedProduct == null)
					{
						string insertProductQuery = @"
                            INSERT INTO Товары (Наименование, Цена, ПоставщикID, Категория)
                            VALUES (@ProductName, @Price, @SupplierID, @Category);
                            SELECT CAST(SCOPE_IDENTITY() AS int);";
						using (SqlCommand command = new SqlCommand(insertProductQuery, connection))
						{
							command.Parameters.AddWithValue("@ProductName", name);
							command.Parameters.AddWithValue("@Price", price);
							command.Parameters.AddWithValue("@SupplierID", selectedSupplier.Id);
							command.Parameters.AddWithValue("@Category", category);
							object result = command.ExecuteScalar();
							int newProductId = (result != null) ? (int)result : 0;
							if (newProductId > 0)
							{
								selectedProduct = new Product
								{
									Id = newProductId,
									Name = name,
									Price = price,
									SupplierId = selectedSupplier.Id,
									Category = category
								};
								Products.Add(selectedProduct);
							}
							else
							{
								MessageBox.Show("Не удалось добавить новый товар.",
									"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
								return;
							}
						}
					}
					else
					{
						if (selectedProduct.Price != price ||
							!selectedProduct.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
						{
							string updateProductQuery = @"
                                UPDATE Товары 
                                SET Цена = @Price, Категория = @Category
                                WHERE ТоварID = @ProductId";
							using (SqlCommand command = new SqlCommand(updateProductQuery, connection))
							{
								command.Parameters.AddWithValue("@Price", price);
								command.Parameters.AddWithValue("@Category", category);
								command.Parameters.AddWithValue("@ProductId", selectedProduct.Id);
								command.ExecuteNonQuery();

								selectedProduct.Price = price;
								selectedProduct.Category = category;
							}
						}
					}

					// Обновляем запись в СкладскиеПозиции (для существующей позиции)
					if (SelectedItem.Id == 0)
					{
						// Если позиции ещё нет, создаём её
						string insertPosQuery = @"
                            INSERT INTO СкладскиеПозиции (ТоварID, СкладID, Количество)
                            VALUES (@ItemId, @WarehouseId, @Quantity);
                            SELECT CAST(SCOPE_IDENTITY() AS int);";
						using (SqlCommand cmd = new SqlCommand(insertPosQuery, connection))
						{
							cmd.Parameters.AddWithValue("@ItemId", selectedProduct.Id);
							cmd.Parameters.AddWithValue("@WarehouseId", selectedWarehouse.Id);
							cmd.Parameters.AddWithValue("@Quantity", quantity);
							object res = cmd.ExecuteScalar();
							int newPosId = (res != null) ? (int)res : 0;
							if (newPosId > 0)
							{
								SelectedItem.Id = newPosId;
							}
							else
							{
								MessageBox.Show("Не удалось создать складскую позицию.", "Ошибка",
									MessageBoxButton.OK, MessageBoxImage.Error);
								return;
							}
						}
					}
					else
					{
						string updateInventoryQuery = @"
                            UPDATE СкладскиеПозиции
                            SET ТоварID = @ItemId, 
                                СкладID = @WarehouseId, 
                                Количество = @Quantity, 
                                ДатаОбновления = GETDATE()
                            WHERE ПозицияID = @Id";
						using (SqlCommand command = new SqlCommand(updateInventoryQuery, connection))
						{
							command.Parameters.AddWithValue("@ItemId", selectedProduct.Id);
							command.Parameters.AddWithValue("@WarehouseId", selectedWarehouse.Id);
							command.Parameters.AddWithValue("@Quantity", quantity);
							command.Parameters.AddWithValue("@Id", SelectedItem.Id);

							int rowsAffected = command.ExecuteNonQuery();
							if (rowsAffected <= 0)
							{
								MessageBox.Show("Не удалось обновить позицию в БД.",
									"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
								return;
							}
						}
					}

					// Обновляем локальные данные
					SelectedItem.ProductId = selectedProduct.Id;
					SelectedItem.Name = selectedProduct.Name;
					SelectedItem.Category = selectedProduct.Category;
					SelectedItem.Price = selectedProduct.Price;
					SelectedItem.Quantity = quantity;
					SelectedItem.Location = selectedWarehouse.Name;

					MessageBox.Show("Позиция успешно обновлена.",
						"Редактирование позиции", MessageBoxButton.OK, MessageBoxImage.Information);
					RefreshInventoryDataGrid();
					HidePanel();
					ClearInputFields();
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка обновления в базе данных: {ex.Message}",
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Фильтрация DataGrid по тексту поиска
		private void ApplyFilters()
		{
			if (InventoryItems == null)
				return;
			var view = CollectionViewSource.GetDefaultView(InventoryItems);
			string searchText = SearchTextBox.Text.Trim().ToLower();
			view.Filter = obj =>
			{
				if (obj is InventoryItem item)
				{
					return string.IsNullOrEmpty(searchText) || item.Name.ToLower().Contains(searchText);
				}
				return true;
			};
		}

		private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			ApplyFilters();
		}

		// Показ панели редактирования/добавления
		private void ShowPanel()
		{
			RootGrid.ColumnDefinitions[1].Width = new GridLength(400);
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

		private void ClosePanel_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}

		private void CancelInventory_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}

		// Переключение темы
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

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}

		// Просмотр QR-кода: теперь передаем ProductId, чтобы поиск шел по товару
		private void ShowQr_Click(object sender, RoutedEventArgs e)
		{
			if (InventoryDataGrid.SelectedItem is InventoryItem selectedInventory)
			{
				var qrWindow = new ViewQrWindow(selectedInventory.ProductId);
				qrWindow.Owner = this;
				qrWindow.ShowDialog();
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите позицию для просмотра QR-кода.",
					"Информация", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Голосовой поиск
		private void VoiceSearchButton_Click(object sender, RoutedEventArgs e)
		{
			if (voiceService == null)
			{
				MessageBox.Show("Модель не загружена.",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (voiceService.IsRunning)
			{
				voiceService.Stop();
				Dispatcher.Invoke(() =>
				{
					VoiceIcon.Kind = PackIconMaterialKind.Microphone;
					VoiceIcon.Foreground = (Brush)FindResource("PrimaryBrush");
				});
				return;
			}
			Dispatcher.Invoke(() =>
			{
				VoiceIcon.Kind = PackIconMaterialKind.RecordCircle;
				VoiceIcon.Foreground = Brushes.Red;
			});
			voiceService.Start();
		}
	}
}
