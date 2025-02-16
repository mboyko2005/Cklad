using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;

namespace УправлениеСкладом.Сотрудник_склада
{
	public partial class ViewItemsWindow : Window, IThemeable
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

		private List<Item> items;           // Полный список товаров
		private List<Item> displayedItems;  // Список товаров после фильтрации
		private string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		public ViewItemsWindow()
		{
			InitializeComponent();
			LoadItems();
			UpdateThemeIcon();
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
                        SELECT t.ТоварID, t.Наименование, t.Категория, ISNULL(SUM(sp.Количество), 0) AS Количество, ISNULL(t.Цена, 0) AS Цена
                        FROM Товары t
                        LEFT JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
                        GROUP BY t.ТоварID, t.Наименование, t.Категория, t.Цена";
					using (SqlCommand command = new SqlCommand(query, connection))
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

		// Фильтрация товаров по заданным параметрам
		private void Filter_TextChanged(object sender, TextChangedEventArgs e)
		{
			ApplyFilters();
		}

		private void ApplyFilters()
		{
			string searchText = SearchTextBox.Text.Trim().ToLower();
			int quantityMin = 0;
			int quantityMax = int.MaxValue;
			decimal priceMin = 0m;
			decimal priceMax = decimal.MaxValue;

			if (!string.IsNullOrEmpty(QuantityMinTextBox.Text))
			{
				if (!int.TryParse(QuantityMinTextBox.Text, out quantityMin))
				{
					MessageBox.Show("Минимальное количество должно быть числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
			}
			if (!string.IsNullOrEmpty(QuantityMaxTextBox.Text))
			{
				if (!int.TryParse(QuantityMaxTextBox.Text, out quantityMax))
				{
					MessageBox.Show("Максимальное количество должно быть числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
			}
			if (!string.IsNullOrEmpty(PriceMinTextBox.Text))
			{
				if (!decimal.TryParse(PriceMinTextBox.Text, out priceMin))
				{
					MessageBox.Show("Минимальная цена должна быть числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
			}
			if (!string.IsNullOrEmpty(PriceMaxTextBox.Text))
			{
				if (!decimal.TryParse(PriceMaxTextBox.Text, out priceMax))
				{
					MessageBox.Show("Максимальная цена должна быть числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
			}

			displayedItems = items.Where(item =>
				(string.IsNullOrEmpty(searchText) ||
				 item.Наименование.ToLower().Contains(searchText) ||
				 item.Категория.ToLower().Contains(searchText)) &&
				(item.Количество >= quantityMin && item.Количество <= quantityMax) &&
				(item.Цена >= priceMin && item.Цена <= priceMax)
			).ToList();

			ItemsDataGrid.ItemsSource = displayedItems;
			ItemsDataGrid.SelectedIndex = -1;
			ItemsDataGrid.UnselectAll();
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
				this.DragMove();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

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

		// Добавляем обработчик кнопки "Показать QR"
		private void ShowQr_Click(object sender, RoutedEventArgs e)
		{
			if (ItemsDataGrid.SelectedItem is Item selectedItem)
			{
				// Открываем окно ViewQrWindow, передавая идентификатор выбранного товара (ТоварID)
				var qrWindow = new ViewQrWindow(selectedItem.ТоварID);
				qrWindow.Owner = this;
				qrWindow.ShowDialog();
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите товар для просмотра QR-кода.", "Информация", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
	}
}
