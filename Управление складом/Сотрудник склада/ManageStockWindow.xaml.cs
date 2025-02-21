using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;
using Vosk;
using Управление_складом.Class;

namespace УправлениеСкладом.Сотрудник_склада
{
	public partial class ManageStockWindow : Window, IThemeable
	{
		public class Item
		{
			public int ТоварID { get; set; }
			public string Наименование { get; set; }
			public string Категория { get; set; }
			public int Количество { get; set; }
			public decimal Цена { get; set; }
		}

		private List<Item> items;
		private List<Item> displayedItems;
		private string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		private Model modelRu;
		private VoiceInputService voiceService;

		public ManageStockWindow()
		{
			InitializeComponent();
			LoadItems(false);
			UpdateShowAllIcon(false);
			ShowAllToggleButton.IsChecked = false;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			InitializeVoskAsync();
		}

		private async void InitializeVoskAsync()
		{
			try
			{
				Vosk.Vosk.SetLogLevel(0);
				string baseDir = AppDomain.CurrentDomain.BaseDirectory;
				string ruPath = System.IO.Path.Combine(baseDir, "Models", "ru");
				if (System.IO.Directory.Exists(ruPath))
					modelRu = await System.Threading.Tasks.Task.Run(() => new Model(ruPath));

				if (modelRu != null)
				{
					voiceService = new VoiceInputService(modelRu);
					voiceService.TextRecognized += (recognizedText) =>
					{
						Dispatcher.Invoke(() =>
						{
							SearchTextBox.Text = recognizedText;
							ApplyFilters();
						});
					};
				}
				else
				{
					MessageBox.Show("Отсутствует модель Vosk для ru в папке Models.",
									"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка инициализации Vosk: {ex.Message}",
								"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			voiceService?.Stop();
			base.OnClosed(e);
		}

		private void LoadItems(bool showAll)
		{
			items = new List<Item>();
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = showAll ?
						@"SELECT t.ТоварID, t.Наименование, t.Категория, ISNULL(SUM(sp.Количество), 0) AS Количество, ISNULL(t.Цена, 0) AS Цена
                          FROM Товары t
                          LEFT JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
                          GROUP BY t.ТоварID, t.Наименование, t.Категория, t.Цена"
						:
						@"SELECT t.ТоварID, t.Наименование, t.Категория, ISNULL(SUM(sp.Количество), 0) AS Количество, ISNULL(t.Цена, 0) AS Цена
                          FROM Товары t
                          LEFT JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
                          GROUP BY t.ТоварID, t.Наименование, t.Категория, t.Цена
                          HAVING ISNULL(SUM(sp.Количество), 0) = 0";
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

		private void Filter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			ApplyFilters();
		}

		private void ApplyFilters()
		{
			string searchText = SearchTextBox.Text.Trim().ToLower();
			int quantityMin = 0;
			int quantityMax = int.MaxValue;

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

		private void ShowAllToggleButton_Click(object sender, RoutedEventArgs e)
		{
			bool showAll = ShowAllToggleButton.IsChecked == true;
			LoadItems(showAll);
			UpdateShowAllIcon(showAll);
			ApplyFilters();
		}

		private void UpdateShowAllIcon(bool showAll)
		{
			if (ShowAllIcon != null)
				ShowAllIcon.Kind = showAll ? PackIconMaterialKind.FilterRemove : PackIconMaterialKind.FilterVariant;
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
				ThemeIcon.Kind = ThemeManager.IsDarkTheme ? PackIconMaterialKind.WeatherNight : PackIconMaterialKind.WeatherSunny;
		}

		private void VoiceSearchButton_Click(object sender, RoutedEventArgs e)
		{
			if (voiceService == null)
			{
				MessageBox.Show("Модель Vosk не загружена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			if (voiceService.IsRunning)
			{
				voiceService.Stop();
				VoiceIcon.Kind = PackIconMaterialKind.Microphone;
				VoiceIcon.Foreground = (System.Windows.Media.Brush)FindResource("PrimaryBrush");
			}
			else
			{
				VoiceIcon.Kind = PackIconMaterialKind.RecordCircle;
				VoiceIcon.Foreground = System.Windows.Media.Brushes.Red;
				voiceService.Start();
			}
		}
	}
}
