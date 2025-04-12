using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;
using Vosk;
using Управление_складом.Class;
using УправлениеСкладом.QR;

namespace УправлениеСкладом.Сотрудник_склада
{
	public partial class ViewItemsWindow : Window, IThemeable
	{
		public class Item
		{
			public int ТоварID { get; set; }
			public string Наименование { get; set; }
			public string Категория { get; set; }
			public int Количество { get; set; }
			public decimal Цена { get; set; }
			public string Склад { get; set; }
		}

		private List<Item> items;
		private List<Item> displayedItems;
		private string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		private Model modelRu;
		private VoiceInputService voiceService;

		public ViewItemsWindow()
		{
			InitializeComponent();
			UpdateThemeIcon();
			Loaded += ViewItemsWindow_Loaded;
		}

		private async void ViewItemsWindow_Loaded(object sender, RoutedEventArgs e)
		{
			await LoadItemsAsync();
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

		protected override void OnClosed(EventArgs e)
		{
			voiceService?.Stop();
			base.OnClosed(e);
		}

		#region Загрузка товаров (асинхронно)
		private async Task LoadItemsAsync()
		{
			var loadedItems = new List<Item>();
			try
			{
				using (SqlConnection connection = new SqlConnection(connectionString))
				{
					await connection.OpenAsync();
					string query = @"
                        SELECT t.ТоварID, t.Наименование, t.Категория, sp.Количество, ISNULL(t.Цена, 0) AS Цена, 
                               s.Наименование AS Склад
                        FROM Товары t
                        JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
                        JOIN Склады s ON sp.СкладID = s.СкладID
                        WHERE sp.Количество > 0
                        ORDER BY t.ТоварID";
					using (SqlCommand command = new SqlCommand(query, connection))
					using (SqlDataReader reader = await command.ExecuteReaderAsync())
					{
						while (await reader.ReadAsync())
						{
							loadedItems.Add(new Item
							{
								ТоварID = reader.GetInt32(reader.GetOrdinal("ТоварID")),
								Наименование = reader.GetString(reader.GetOrdinal("Наименование")),
								Категория = reader.IsDBNull(reader.GetOrdinal("Категория"))
									? string.Empty
									: reader.GetString(reader.GetOrdinal("Категория")),
								Количество = reader.GetInt32(reader.GetOrdinal("Количество")),
								Цена = reader.GetDecimal(reader.GetOrdinal("Цена")),
								Склад = reader.GetString(reader.GetOrdinal("Склад"))
							});
						}
					}
				}
			}
			catch (SqlException ex)
			{
				await Dispatcher.InvokeAsync(() =>
					MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}",
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error));
			}
			items = loadedItems;
			displayedItems = new List<Item>(items);
			ItemsDataGrid.ItemsSource = displayedItems;
			ItemsDataGrid.SelectedIndex = -1;
			ItemsDataGrid.UnselectAll();
		}
		#endregion

		#region Фильтрация товаров
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
					MessageBox.Show("Минимальное количество должно быть числом.",
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
			}
			if (!string.IsNullOrEmpty(QuantityMaxTextBox.Text))
			{
				if (!int.TryParse(QuantityMaxTextBox.Text, out quantityMax))
				{
					MessageBox.Show("Максимальное количество должно быть числом.",
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
			}
			if (!string.IsNullOrEmpty(PriceMinTextBox.Text))
			{
				if (!decimal.TryParse(PriceMinTextBox.Text, out priceMin))
				{
					MessageBox.Show("Минимальная цена должна быть числом.",
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
			}
			if (!string.IsNullOrEmpty(PriceMaxTextBox.Text))
			{
				if (!decimal.TryParse(PriceMaxTextBox.Text, out priceMax))
				{
					MessageBox.Show("Максимальная цена должна быть числом.",
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
			}
			displayedItems = items.Where(item =>
				(string.IsNullOrEmpty(searchText) ||
				 item.Наименование.ToLower().Contains(searchText) ||
				 item.Категория.ToLower().Contains(searchText) ||
				 item.Склад.ToLower().Contains(searchText)) &&
				(item.Количество >= quantityMin && item.Количество <= quantityMax) &&
				(item.Цена >= priceMin && item.Цена <= priceMax)
			).ToList();
			ItemsDataGrid.ItemsSource = displayedItems;
			ItemsDataGrid.SelectedIndex = -1;
			ItemsDataGrid.UnselectAll();
		}
		#endregion

		#region Управление UI (закрытие, тема, QR)
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
				ThemeIcon.Kind = ThemeManager.IsDarkTheme
					? PackIconMaterialKind.WeatherNight
					: PackIconMaterialKind.WeatherSunny;
			}
		}

		private void ShowQr_Click(object sender, RoutedEventArgs e)
		{
			if (ItemsDataGrid.SelectedItem is Item selectedItem)
			{
				var qrWindow = new ViewQrWindow(selectedItem.ТоварID);
				qrWindow.Owner = this;
				qrWindow.ShowDialog();
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите товар для просмотра QR-кода.",
					"Информация", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
		#endregion

		#region Инициализация Vosk
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
		#endregion

		#region Голосовой ввод через VoiceInputService
		private void VoiceSearchButton_Click(object sender, RoutedEventArgs e)
		{
			if (voiceService == null)
			{
				MessageBox.Show("Модель не загружена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			// Если голосовой ввод уже ведётся, то остановить его
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
			// Меняем иконку на красную точку и запускаем ввод
			Dispatcher.Invoke(() =>
			{
				VoiceIcon.Kind = PackIconMaterialKind.RecordCircle;
				VoiceIcon.Foreground = Brushes.Red;
			});
			voiceService.Start();
		}
		#endregion
	}
}
