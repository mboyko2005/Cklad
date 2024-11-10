// ManageOrdersWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;

namespace УправлениеСкладом
{
	public partial class ManageOrdersWindow : Window, IThemeable
	{
		// Модель товара
		public class Product
		{
			public int Id { get; set; }
			public int SupplierId { get; set; }
			public string SupplierName { get; set; }
			public string Name { get; set; }
			public string Category { get; set; }
			public decimal Price { get; set; }
		}

		// Модель поставщика
		public class Supplier
		{
			public int Id { get; set; }
			public string Name { get; set; }
		}

		private List<Product> Products;
		private List<Supplier> Suppliers;
		private bool IsEditMode = false;
		private Product SelectedProduct;
		private string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		public ManageOrdersWindow()
		{
			InitializeComponent();
			LoadSuppliers();
			LoadProducts();
			UpdateThemeIcon();
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
									Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
								});
							}
						}
					}

					// Привязка списка поставщиков к ComboBox
					ClientComboBox.ItemsSource = Suppliers;
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки поставщиков: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
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
					string query = @"
                        SELECT t.ТоварID, t.ПоставщикID, p.Наименование AS Поставщик, t.Наименование, t.Категория, t.Цена
                        FROM Товары t
                        INNER JOIN Поставщики p ON t.ПоставщикID = p.ПоставщикID
                        ORDER BY t.ТоварID";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								Products.Add(new Product
								{
									Id = reader.GetInt32(0),
									SupplierId = reader.GetInt32(1),
									SupplierName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
									Name = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
									Category = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
									Price = reader.IsDBNull(5) ? 0m : reader.GetDecimal(5)
								});
							}
						}
					}

					// Привязка списка товаров к DataGrid
					OrdersDataGrid.ItemsSource = Products;
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Обработчик кнопки закрытия окна
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		// Показать панель добавления
		private void AddOrder_Click(object sender, RoutedEventArgs e)
		{
			IsEditMode = false;
			PanelTitle.Text = "Добавить товар";
			ClearInputFields();
			ShowPanel();
		}

		// Показать панель редактирования
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
				MessageBox.Show("Пожалуйста, выберите товар для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Удаление товара
		private void DeleteOrder_Click(object sender, RoutedEventArgs e)
		{
			if (OrdersDataGrid.SelectedItem is Product selectedProduct)
			{
				MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить товар '{selectedProduct.Name}'?",
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

							// Обновление списка товаров
							Products.Remove(selectedProduct);
							OrdersDataGrid.ItemsSource = null;
							OrdersDataGrid.ItemsSource = Products;
							MessageBox.Show("Товар успешно удалён.", "Удаление товара", MessageBoxButton.OK, MessageBoxImage.Information);
						}
						catch (SqlException ex)
						{
							MessageBox.Show($"Ошибка при удалении товара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
						}
					}
				}
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите товар для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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
		private void CancelOrder_Click(object sender, RoutedEventArgs e)
		{
			HidePanel();
		}

		// Сохранение нового товара или обновление существующего
		private void SaveOrder_Click(object sender, RoutedEventArgs e)
		{
			if (IsEditMode)
			{
				SaveEditProduct();
			}
			else
			{
				SaveAddProduct();
			}
		}

		// Сохранение нового товара
		private void SaveAddProduct()
		{
			Supplier selectedSupplier = ClientComboBox.SelectedItem as Supplier;
			string productName = NameTextBox.Text.Trim();
			string category = CategoryTextBox.Text.Trim();
			string priceText = PriceTextBox.Text.Trim();

			if (selectedSupplier == null || string.IsNullOrEmpty(productName) || string.IsNullOrEmpty(category) || string.IsNullOrEmpty(priceText))
			{
				MessageBox.Show("Пожалуйста, заполните все поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!decimal.TryParse(priceText, out decimal price))
			{
				MessageBox.Show("Некорректная цена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();

					// Добавление нового товара
					string insertProductQuery = @"
                        INSERT INTO Товары (Наименование, Категория, Цена, ПоставщикID)
                        VALUES (@Name, @Category, @Price, @SupplierId);
                        SELECT CAST(scope_identity() AS int);";

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
							Price = price
						};
						Products.Add(newProduct);
						OrdersDataGrid.ItemsSource = null;
						OrdersDataGrid.ItemsSource = Products;
						MessageBox.Show("Товар успешно добавлен.", "Добавление товара", MessageBoxButton.OK, MessageBoxImage.Information);
						HidePanel();
						ClearInputFields();
					}
					else
					{
						MessageBox.Show("Не удалось добавить товар.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка сохранения в базу данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Сохранение изменений в существующем товаре
		private void SaveEditProduct()
		{
			Supplier selectedSupplier = ClientComboBox.SelectedItem as Supplier;
			string productName = NameTextBox.Text.Trim();
			string category = CategoryTextBox.Text.Trim();
			string priceText = PriceTextBox.Text.Trim();

			if (selectedSupplier == null || string.IsNullOrEmpty(productName) || string.IsNullOrEmpty(category) || string.IsNullOrEmpty(priceText))
			{
				MessageBox.Show("Пожалуйста, заполните все поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!decimal.TryParse(priceText, out decimal price))
			{
				MessageBox.Show("Некорректная цена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();

					// Обновление товара
					string updateProductQuery = @"
                        UPDATE Товары
                        SET Наименование = @Name, Категория = @Category, Цена = @Price, ПоставщикID = @SupplierId
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
							SelectedProduct.SupplierId = selectedSupplier.Id;
							SelectedProduct.SupplierName = selectedSupplier.Name;
							SelectedProduct.Name = productName;
							SelectedProduct.Category = category;
							SelectedProduct.Price = price;
							OrdersDataGrid.ItemsSource = null;
							OrdersDataGrid.ItemsSource = Products;
							MessageBox.Show("Товар успешно обновлён.", "Редактирование товара", MessageBoxButton.OK, MessageBoxImage.Information);
							HidePanel();
							ClearInputFields();
						}
						else
						{
							MessageBox.Show("Не удалось обновить товар.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
			ClientComboBox.SelectedIndex = -1;
			NameTextBox.Text = string.Empty;
			CategoryTextBox.Text = string.Empty;
			PriceTextBox.Text = string.Empty;
		}

		// Заполнение полей редактирования
		private void PopulateInputFields(Product product)
		{
			ClientComboBox.SelectedItem = Suppliers.FirstOrDefault(s => s.Id == product.SupplierId);
			NameTextBox.Text = product.Name;
			CategoryTextBox.Text = product.Category;
			PriceTextBox.Text = product.Price.ToString();
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
