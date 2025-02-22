using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
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
	public partial class ManageOrdersWindow : Window, IThemeable
	{
		// Модель товара (обновлено – добавлено свойство Quantity, которое теперь не редактируется с панели)
		public class Product
		{
			public int Id { get; set; }
			public int SupplierId { get; set; }
			public string SupplierName { get; set; }
			public string Name { get; set; }
			public string Category { get; set; }
			public decimal Price { get; set; }
			public int Quantity { get; set; } // Значение по умолчанию будет 0
			public string Location { get; set; }
		}

		// Модель поставщика
		public class Supplier
		{
			public int Id { get; set; }
			public string Name { get; set; }
		}

		private List<Product> Products;
		private List<Supplier> Suppliers;

		// Режим редактирования (true – редактирование, false – добавление)
		private bool IsEditMode = false;
		private Product SelectedProduct;

		// Строка подключения к базе
		private string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		// Поля для голосового поиска
		private Model modelRu;
		private VoiceInputService voiceService;

		public ManageOrdersWindow()
		{
			InitializeComponent();
			LoadSuppliers();
			LoadProducts();
			UpdateThemeIcon();
			// Подписываемся на событие Loaded для инициализации Vosk
			this.Loaded += ManageOrdersWindow_Loaded;
		}

		private async void ManageOrdersWindow_Loaded(object sender, RoutedEventArgs e)
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

		/// <summary>
		/// Обработчик изменения выделения в DataGrid.
		/// </summary>
		private void OrdersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Можно добавить логику обработки выделения или оставить пустым.
		}

		/// <summary>
		/// Загрузка списка поставщиков из таблицы "Поставщики".
		/// </summary>
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
									Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
								});
							}
						}
					}

					ClientComboBox.ItemsSource = Suppliers;
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки поставщиков: {ex.Message}",
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		/// <summary>
		/// Загрузка списка товаров из базы данных.
		/// Запрос модифицирован для получения суммы количества товара.
		/// </summary>
		private void LoadProducts()
		{
			Products = new List<Product>();

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = @"
                        SELECT t.ТоварID,
                               t.ПоставщикID,
                               p.Наименование AS Поставщик,
                               t.Наименование,
                               t.Категория,
                               t.Цена,
                               ISNULL(SUM(sp.Количество), 0) AS Количество,
                               MIN(s.Наименование) AS Местоположение
                        FROM Товары t
                        INNER JOIN Поставщики p ON t.ПоставщикID = p.ПоставщикID
                        LEFT JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
                        LEFT JOIN Склады s ON sp.СкладID = s.СкладID
                        GROUP BY t.ТоварID, t.ПоставщикID, p.Наименование, t.Наименование, t.Категория, t.Цена
                        ORDER BY t.ТоварID;
                    ";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								Product product = new Product
								{
									Id = reader.GetInt32(reader.GetOrdinal("ТоварID")),
									SupplierId = reader.GetInt32(reader.GetOrdinal("ПоставщикID")),
									SupplierName = reader.GetString(reader.GetOrdinal("Поставщик")),
									Name = reader.GetString(reader.GetOrdinal("Наименование")),
									Category = reader.IsDBNull(reader.GetOrdinal("Категория"))
										? string.Empty
										: reader.GetString(reader.GetOrdinal("Категория")),
									Price = reader.IsDBNull(reader.GetOrdinal("Цена"))
										? 0m
										: reader.GetDecimal(reader.GetOrdinal("Цена")),
									Quantity = reader.IsDBNull(reader.GetOrdinal("Количество"))
										? 0
										: reader.GetInt32(reader.GetOrdinal("Количество")),
									Location = reader.IsDBNull(reader.GetOrdinal("Местоположение"))
										? string.Empty
										: reader.GetString(reader.GetOrdinal("Местоположение"))
								};
								Products.Add(product);
							}
						}
					}

					OrdersDataGrid.ItemsSource = Products;
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}",
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		/// <summary>
		/// Фильтрация товаров по поисковому запросу.
		/// </summary>
		private void ApplyFilters()
		{
			string searchText = SearchTextBox.Text.Trim().ToLower();
			var filtered = Products.Where(p =>
				string.IsNullOrEmpty(searchText) ||
				p.Name.ToLower().Contains(searchText) ||
				p.Category.ToLower().Contains(searchText)
			).ToList();
			OrdersDataGrid.ItemsSource = filtered;
		}

		private void Filter_TextChanged(object sender, TextChangedEventArgs e)
		{
			ApplyFilters();
		}

		/// <summary>
		/// Голосовой поиск – запуск/остановка голосового ввода.
		/// </summary>
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

		/// <summary>
		/// Закрытие окна.
		/// </summary>
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// Кнопка "Добавить" – открытие панели для добавления нового товара.
		/// </summary>
		private void AddOrder_Click(object sender, RoutedEventArgs e)
		{
			IsEditMode = false;
			PanelTitle.Text = "Добавить товар";
			ClearInputFields();
			ShowPanel();
		}

		/// <summary>
		/// Кнопка "Редактировать" – открытие панели для редактирования выбранного товара.
		/// </summary>
		private void EditOrder_Click(object sender, RoutedEventArgs e)
		{
			if (OrdersDataGrid.SelectedItem is Product selectedProduct)
			{
				IsEditMode = true;
				SelectedProduct = selectedProduct;
				PanelTitle.Text = "Редактировать товар";
				PopulateInputFields(selectedProduct);
				ShowPanel();
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите товар для редактирования.",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		/// <summary>
		/// Кнопка "Удалить" – удаление выбранного товара.
		/// </summary>
		private void DeleteOrder_Click(object sender, RoutedEventArgs e)
		{
			if (OrdersDataGrid.SelectedItem is Product selectedProduct)
			{
				MessageBoxResult result = MessageBox.Show(
					$"Вы уверены, что хотите удалить товар '{selectedProduct.Name}'?",
					"Удаление товара", MessageBoxButton.YesNo, MessageBoxImage.Warning);

				if (result == MessageBoxResult.Yes)
				{
					using (SqlConnection connection = new SqlConnection(connectionString))
					{
						try
						{
							connection.Open();
							string deleteQuery = "DELETE FROM Товары WHERE ТоварID = @ProductId";
							using (SqlCommand deleteCommand = new SqlCommand(deleteQuery, connection))
							{
								deleteCommand.Parameters.AddWithValue("@ProductId", selectedProduct.Id);
								deleteCommand.ExecuteNonQuery();
							}

							Products.Remove(selectedProduct);
							OrdersDataGrid.ItemsSource = null;
							OrdersDataGrid.ItemsSource = Products;

							MessageBox.Show("Товар успешно удалён.",
								"Удаление товара", MessageBoxButton.OK, MessageBoxImage.Information);
						}
						catch (SqlException ex)
						{
							MessageBox.Show($"Ошибка при удалении товара: {ex.Message}",
								"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите товар для удаления.",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		/// <summary>
		/// Кнопка "Сохранить" – обработка добавления или редактирования товара.
		/// </summary>
		private void SaveOrder_Click(object sender, RoutedEventArgs e)
		{
			if (IsEditMode)
				SaveEditProduct();
			else
				SaveAddProduct();
		}

		/// <summary>
		/// Кнопка "Отмена" – скрытие панели редактирования.
		/// </summary>
		private void CancelOrder_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}

		/// <summary>
		/// Отображение правой панели.
		/// </summary>
		private void ShowPanel()
		{
			RootGrid.ColumnDefinitions[1].Width = new GridLength(300);
			Storyboard showStoryboard = (Storyboard)FindResource("ShowPanelStoryboard");
			showStoryboard.Begin();
		}

		/// <summary>
		/// Сокрытие правой панели.
		/// </summary>
		private void HidePanel()
		{
			Storyboard hideStoryboard = (Storyboard)FindResource("HidePanelStoryboard");
			hideStoryboard.Completed += (s, e) =>
			{
				RootGrid.ColumnDefinitions[1].Width = new GridLength(0);
			};
			hideStoryboard.Begin();
		}

		/// <summary>
		/// Очистка полей для добавления/редактирования.
		/// </summary>
		private void ClearInputFields()
		{
			ClientComboBox.SelectedIndex = -1;
			NameTextBox.Text = string.Empty;
			CategoryTextBox.Text = string.Empty;
			PriceTextBox.Text = string.Empty;
			// Поле количества убрано, поэтому удалена очистка QuantityTextBox
		}

		/// <summary>
		/// Заполнение полей для редактирования выбранного товара.
		/// </summary>
		private void PopulateInputFields(Product product)
		{
			ClientComboBox.SelectedItem = Suppliers.FirstOrDefault(s => s.Id == product.SupplierId);
			NameTextBox.Text = product.Name;
			CategoryTextBox.Text = product.Category;
			PriceTextBox.Text = product.Price.ToString("F2");
			// Поле количества не заполняется, так как редактирование количества не предусмотрено
		}

		/// <summary>
		/// Сохранение нового товара (INSERT).
		/// </summary>
		private void SaveAddProduct()
		{
			Supplier selectedSupplier = ClientComboBox.SelectedItem as Supplier;
			string productName = NameTextBox.Text.Trim();
			string category = CategoryTextBox.Text.Trim();
			string priceText = PriceTextBox.Text.Trim();

			if (selectedSupplier == null || string.IsNullOrEmpty(productName) ||
				string.IsNullOrEmpty(category) || string.IsNullOrEmpty(priceText))
			{
				MessageBox.Show("Пожалуйста, заполните все поля.",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!decimal.TryParse(priceText, out decimal price))
			{
				MessageBox.Show("Некорректная цена.",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			// Количество теперь не вводится – сохраняем значение по умолчанию 0.
			int quantity = 0;

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string insertProductQuery = @"
                        INSERT INTO Товары (Наименование, Категория, Цена, ПоставщикID, Количество)
                        VALUES (@Name, @Category, @Price, @SupplierId, @Quantity);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

					int newProductId;
					using (SqlCommand command = new SqlCommand(insertProductQuery, connection))
					{
						command.Parameters.AddWithValue("@Name", productName);
						command.Parameters.AddWithValue("@Category", category);
						command.Parameters.AddWithValue("@Price", price);
						command.Parameters.AddWithValue("@SupplierId", selectedSupplier.Id);
						command.Parameters.AddWithValue("@Quantity", quantity);

						object result = command.ExecuteScalar();
						newProductId = (result != null) ? (int)result : 0;
					}

					if (newProductId > 0)
					{
						Product newProduct = new Product
						{
							Id = newProductId,
							SupplierId = selectedSupplier.Id,
							SupplierName = selectedSupplier.Name,
							Name = productName,
							Category = category,
							Price = price,
							Quantity = quantity,
							Location = "" // Новому товару пока не присвоено местоположение
						};
						Products.Add(newProduct);

						OrdersDataGrid.ItemsSource = null;
						OrdersDataGrid.ItemsSource = Products;

						MessageBox.Show("Товар успешно добавлен.",
							"Добавление товара", MessageBoxButton.OK, MessageBoxImage.Information);

						HidePanel();
						ClearInputFields();
					}
					else
					{
						MessageBox.Show("Не удалось добавить товар.",
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

		/// <summary>
		/// Сохранение изменений в существующем товаре (UPDATE).
		/// </summary>
		private void SaveEditProduct()
		{
			if (SelectedProduct == null)
				return;

			Supplier selectedSupplier = ClientComboBox.SelectedItem as Supplier;
			string productName = NameTextBox.Text.Trim();
			string category = CategoryTextBox.Text.Trim();
			string priceText = PriceTextBox.Text.Trim();

			if (selectedSupplier == null || string.IsNullOrEmpty(productName) ||
				string.IsNullOrEmpty(category) || string.IsNullOrEmpty(priceText))
			{
				MessageBox.Show("Пожалуйста, заполните все поля.",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!decimal.TryParse(priceText, out decimal price))
			{
				MessageBox.Show("Некорректная цена.",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			// Количество не редактируется – оставляем прежнее значение (или 0)
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string updateProductQuery = @"
                        UPDATE Товары
                        SET Наименование = @Name,
                            Категория = @Category,
                            Цена = @Price,
                            ПоставщикID = @SupplierId
                        WHERE ТоварID = @ProductId";

					using (SqlCommand command = new SqlCommand(updateProductQuery, connection))
					{
						command.Parameters.AddWithValue("@Name", productName);
						command.Parameters.AddWithValue("@Category", category);
						command.Parameters.AddWithValue("@Price", price);
						command.Parameters.AddWithValue("@SupplierId", selectedSupplier.Id);
						command.Parameters.AddWithValue("@ProductId", SelectedProduct.Id);

						int rowsAffected = command.ExecuteNonQuery();
						if (rowsAffected > 0)
						{
							// Обновляем свойства в SelectedProduct
							SelectedProduct.SupplierId = selectedSupplier.Id;
							SelectedProduct.SupplierName = selectedSupplier.Name;
							SelectedProduct.Name = productName;
							SelectedProduct.Category = category;
							SelectedProduct.Price = price;

							OrdersDataGrid.ItemsSource = null;
							OrdersDataGrid.ItemsSource = Products;

							MessageBox.Show("Товар успешно обновлён.",
								"Редактирование товара", MessageBoxButton.OK, MessageBoxImage.Information);

							HidePanel();
							ClearInputFields();
						}
						else
						{
							MessageBox.Show("Не удалось обновить товар.",
								"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка обновления в базе данных: {ex.Message}",
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		/// <summary>
		/// Перетаскивание окна (для стилизованного окна без стандартного заголовка).
		/// </summary>
		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}

		/// <summary>
		/// Закрытие панели редактирования.
		/// </summary>
		private void ClosePanel_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}

		/// <summary>
		/// Переключение темы приложения.
		/// </summary>
		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			ThemeManager.ToggleTheme();
			UpdateThemeIcon();
		}

		/// <summary>
		/// Обновление иконки темы (день/ночь).
		/// </summary>
		public void UpdateThemeIcon()
		{
			if (ThemeIcon != null)
			{
				ThemeIcon.Kind = ThemeManager.IsDarkTheme
					? PackIconMaterialKind.WeatherNight
					: PackIconMaterialKind.WeatherSunny;
			}
		}

		/// <summary>
		/// Инициализация Vosk для голосового ввода.
		/// </summary>
		private async System.Threading.Tasks.Task InitializeVoskAsync()
		{
			try
			{
				Vosk.Vosk.SetLogLevel(0);
				string baseDir = AppDomain.CurrentDomain.BaseDirectory;
				string ruPath = System.IO.Path.Combine(baseDir, "Models", "ru");
				if (System.IO.Directory.Exists(ruPath))
				{
					modelRu = await System.Threading.Tasks.Task.Run(() => new Model(ruPath));
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
	}
}
