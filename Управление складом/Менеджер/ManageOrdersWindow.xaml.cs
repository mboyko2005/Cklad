using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls; // <-- Нужно для SelectionChangedEventArgs
using System.Windows.Input;
using System.Windows.Media.Animation;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;

namespace УправлениеСкладом
{
	public partial class ManageOrdersWindow : Window, IThemeable
	{
		// Модель товара (добавили свойство Location для местоположения)
		public class Product
		{
			public int Id { get; set; }
			public int SupplierId { get; set; }
			public string SupplierName { get; set; }
			public string Name { get; set; }
			public string Category { get; set; }
			public decimal Price { get; set; }
			public string Location { get; set; }  // <-- Добавлено
		}

		// Модель поставщика
		public class Supplier
		{
			public int Id { get; set; }
			public string Name { get; set; }
		}

		private List<Product> Products;
		private List<Supplier> Suppliers;

		// Режим редактирования (true = редактирование, false = добавление)
		private bool IsEditMode = false;
		private Product SelectedProduct;

		// Строка подключения к базе
		private string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		public ManageOrdersWindow()
		{
			InitializeComponent();
			LoadSuppliers();
			LoadProducts();
			UpdateThemeIcon();
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

					// Привязываем список поставщиков к ComboBox
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
		/// Для получения местоположения делаем LEFT JOIN на СкладскиеПозиции и Склады,
		/// используя MIN(...) — если у товара несколько складских позиций, берём первое.
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
									Location = reader.IsDBNull(reader.GetOrdinal("Местоположение"))
										? string.Empty
										: reader.GetString(reader.GetOrdinal("Местоположение"))
								};
								Products.Add(product);
							}
						}
					}

					// Привязываем список товаров к DataGrid
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
		/// Обработчик изменения выделения в DataGrid (пустой, чтобы не было ошибки).
		/// </summary>
		private void OrdersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Оставляем пустым или реализуйте свою логику, если нужно
		}

		/// <summary>
		/// Закрытие окна
		/// </summary>
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// Кнопка "Добавить" – показ панели для добавления нового товара
		/// </summary>
		private void AddOrder_Click(object sender, RoutedEventArgs e)
		{
			IsEditMode = false;
			PanelTitle.Text = "Добавить товар";
			ClearInputFields();
			ShowPanel();
		}

		/// <summary>
		/// Кнопка "Редактировать" – показ панели для редактирования выбранного товара
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
		/// Кнопка "Удалить" – удаление выбранного товара
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
							// Удаление товара
							string deleteQuery = "DELETE FROM Товары WHERE ТоварID = @ProductId";
							using (SqlCommand deleteCommand = new SqlCommand(deleteQuery, connection))
							{
								deleteCommand.Parameters.AddWithValue("@ProductId", selectedProduct.Id);
								deleteCommand.ExecuteNonQuery();
							}

							// Удаляем из списка и обновляем DataGrid
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
		/// Кнопка "Сохранить" в правой панели
		/// </summary>
		private void SaveOrder_Click(object sender, RoutedEventArgs e)
		{
			if (IsEditMode)
				SaveEditProduct();
			else
				SaveAddProduct();
		}

		/// <summary>
		/// Кнопка "Отмена" в правой панели
		/// </summary>
		private void CancelOrder_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}

		/// <summary>
		/// Отображение правой панели
		/// </summary>
		private void ShowPanel()
		{
			RootGrid.ColumnDefinitions[1].Width = new GridLength(300);
			Storyboard showStoryboard = (Storyboard)FindResource("ShowPanelStoryboard");
			showStoryboard.Begin();
		}

		/// <summary>
		/// Сокрытие правой панели
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
		/// Очистка полей для добавления/редактирования
		/// </summary>
		private void ClearInputFields()
		{
			ClientComboBox.SelectedIndex = -1;
			NameTextBox.Text = string.Empty;
			CategoryTextBox.Text = string.Empty;
			PriceTextBox.Text = string.Empty;
		}

		/// <summary>
		/// Заполнение полей для редактирования выбранного товара
		/// </summary>
		private void PopulateInputFields(Product product)
		{
			ClientComboBox.SelectedItem = Suppliers.FirstOrDefault(s => s.Id == product.SupplierId);
			NameTextBox.Text = product.Name;
			CategoryTextBox.Text = product.Category;
			PriceTextBox.Text = product.Price.ToString("F2");
		}

		/// <summary>
		/// Сохранение нового товара (INSERT)
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

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string insertProductQuery = @"
                        INSERT INTO Товары (Наименование, Категория, Цена, ПоставщикID)
                        VALUES (@Name, @Category, @Price, @SupplierId);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

					int newProductId;
					using (SqlCommand command = new SqlCommand(insertProductQuery, connection))
					{
						command.Parameters.AddWithValue("@Name", productName);
						command.Parameters.AddWithValue("@Category", category);
						command.Parameters.AddWithValue("@Price", price);
						command.Parameters.AddWithValue("@SupplierId", selectedSupplier.Id);

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
		/// Сохранение изменений в существующем товаре (UPDATE)
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
		/// Перетаскивание окна (для стилизованного окна без стандартного заголовка)
		/// </summary>
		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}

		/// <summary>
		/// Закрытие панели редактирования
		/// </summary>
		private void ClosePanel_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}

		/// <summary>
		/// Переключение темы приложения
		/// </summary>
		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			ThemeManager.ToggleTheme();
			UpdateThemeIcon();
		}

		/// <summary>
		/// Обновление иконки темы (день/ночь)
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
	}
}
