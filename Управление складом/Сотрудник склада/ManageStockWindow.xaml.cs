using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;

namespace УправлениеСкладом.Сотрудник_склада
{
	/// <summary>
	/// Логика взаимодействия для ManageStockWindow.xaml
	/// </summary>
	public partial class ManageStockWindow : Window, IThemeable
	{
		// Модель товара
		public class Item
		{
			public int ТоварID { get; set; }
			public string Наименование { get; set; }
			public string Категория { get; set; }
			public int Количество { get; set; }
			public decimal Цена { get; set; }
		}

		private List<Item> items; // Полный список товаров
		private List<Item> displayedItems; // Список отображаемых товаров после фильтрации
		private string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		public ManageStockWindow()
		{
			InitializeComponent();
			LoadItems(false); // По умолчанию загружаем только отсутствующие товары
			UpdateShowAllIcon(false); // Устанавливаем иконку по умолчанию
			ShowAllToggleButton.IsChecked = false;
		}

		// Загрузка товаров из базы данных
		private void LoadItems(bool showAll)
		{
			items = new List<Item>();

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query;

					if (showAll)
					{
						query = @"
                            SELECT t.ТоварID, t.Наименование, t.Категория, ISNULL(SUM(sp.Количество), 0) AS Количество, ISNULL(t.Цена, 0) AS Цена
                            FROM Товары t
                            LEFT JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
                            GROUP BY t.ТоварID, t.Наименование, t.Категория, t.Цена
                        ";
					}
					else
					{
						query = @"
                            SELECT t.ТоварID, t.Наименование, t.Категория, ISNULL(SUM(sp.Количество), 0) AS Количество, ISNULL(t.Цена, 0) AS Цена
                            FROM Товары t
                            LEFT JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
                            GROUP BY t.ТоварID, t.Наименование, t.Категория, t.Цена
                            HAVING ISNULL(SUM(sp.Количество), 0) = 0
                        ";
					}

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
									Цена = reader.GetDecimal(reader.GetOrdinal("Цена"))
								});
							}
						}
					}

					displayedItems = new List<Item>(items);
					StockDataGrid.ItemsSource = displayedItems;
					StockDataGrid.SelectedIndex = -1;
					StockDataGrid.UnselectAll();
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		// Обработчик изменения текста в любых полях фильтрации
		private void Filter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			ApplyFilters();
		}

		// Обработчик нажатия на ToggleButton "Показывать все товары"
		private void ShowAllToggleButton_Click(object sender, RoutedEventArgs e)
		{
			bool showAll = ShowAllToggleButton.IsChecked == true;
			LoadItems(showAll);
			UpdateShowAllIcon(showAll);
			ApplyFilters(); // Применяем фильтры после загрузки новых данных
		}

		// Метод для обновления иконки ToggleButton в зависимости от состояния
		private void UpdateShowAllIcon(bool showAll)
		{
			if (ShowAllIcon != null)
			{
				ShowAllIcon.Kind = showAll ? PackIconMaterialKind.FilterRemove : PackIconMaterialKind.FilterVariant;
			}
		}

		// Применение фильтров к списку товаров
		private void ApplyFilters()
		{
			string searchText = SearchTextBox.Text.Trim().ToLower();
			int quantityMin = 0;
			int quantityMax = int.MaxValue;

			// Парсинг минимального количества
			if (!string.IsNullOrEmpty(QuantityMinTextBox.Text))
			{
				if (!int.TryParse(QuantityMinTextBox.Text, out quantityMin))
				{
					MessageBox.Show("Минимальное количество должно быть числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
			}

			// Парсинг максимального количества
			if (!string.IsNullOrEmpty(QuantityMaxTextBox.Text))
			{
				if (!int.TryParse(QuantityMaxTextBox.Text, out quantityMax))
				{
					MessageBox.Show("Максимальное количество должно быть числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
			}

			// Применение фильтрации
			displayedItems = items.Where(item =>
				(string.IsNullOrEmpty(searchText) ||
				 item.Наименование.ToLower().Contains(searchText) ||
				 item.Категория.ToLower().Contains(searchText)) &&
				(item.Количество >= quantityMin && item.Количество <= quantityMax)
			).ToList();

			StockDataGrid.ItemsSource = displayedItems;
			StockDataGrid.SelectedIndex = -1;
			StockDataGrid.UnselectAll();
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
		public void UpdateThemeIcon()
		{
			if (ThemeIcon != null)
			{
				ThemeIcon.Kind = ThemeManager.IsDarkTheme ? PackIconMaterialKind.WeatherNight : PackIconMaterialKind.WeatherSunny;
			}
		}
	}
}
