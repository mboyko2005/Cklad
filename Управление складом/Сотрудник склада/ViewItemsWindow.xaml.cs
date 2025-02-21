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
using NAudio.Wave;

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

		private List<Item> items;
		private List<Item> displayedItems;
		private string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		// Модель Vosk (только русская)
		private Model modelRu;

		// Захват аудио через NAudio
		private WaveInEvent waveIn;

		// Распознаватели Vosk (только русский)
		private VoskRecognizer recognizerRu;

		// Флаги для управления распознаванием
		private bool stopRequested;
		private bool isRecognizing = false;

		public ViewItemsWindow()
		{
			InitializeComponent();
			UpdateThemeIcon();
			// Подписываемся на событие Loaded, чтобы выполнять тяжелые операции асинхронно после показа окна
			this.Loaded += ViewItemsWindow_Loaded;
		}

		private async void ViewItemsWindow_Loaded(object sender, RoutedEventArgs e)
		{
			// Асинхронно загружаем товары и модель Vosk
			await LoadItemsAsync();
			await InitializeVoskAsync();
		}

		protected override void OnClosed(EventArgs e)
		{
			// При закрытии окна обязательно останавливаем распознавание
			StopRecognition();
			base.OnClosed(e);
		}

		#region Загрузка и отображение товаров (асинхронно)
		private async Task LoadItemsAsync()
		{
			var loadedItems = new List<Item>();

			try
			{
				using (SqlConnection connection = new SqlConnection(connectionString))
				{
					await connection.OpenAsync();
					string query = @"
                        SELECT t.ТоварID, t.Наименование, t.Категория, ISNULL(SUM(sp.Количество), 0) AS Количество, ISNULL(t.Цена, 0) AS Цена
                        FROM Товары t
                        LEFT JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
                        GROUP BY t.ТоварID, t.Наименование, t.Категория, t.Цена";
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
								Цена = reader.GetDecimal(reader.GetOrdinal("Цена"))
							});
						}
					}
				}
			}
			catch (SqlException ex)
			{
				// Отображаем сообщение об ошибке на UI-потоке
				await Dispatcher.InvokeAsync(() =>
					MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}",
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error));
			}

			items = loadedItems;
			displayedItems = new List<Item>(items);
			// Обновляем UI
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
				 item.Категория.ToLower().Contains(searchText)) &&
				(item.Количество >= quantityMin && item.Количество <= quantityMax) &&
				(item.Цена >= priceMin && item.Цена <= priceMax)
			).ToList();

			ItemsDataGrid.ItemsSource = displayedItems;
			ItemsDataGrid.SelectedIndex = -1;
			ItemsDataGrid.UnselectAll();
		}
		#endregion

		#region Закрытие окна, переключение темы, QR-код
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

		#region Инициализация Vosk (асинхронно)
		private async Task InitializeVoskAsync()
		{
			try
			{
				Vosk.Vosk.SetLogLevel(0);

				string baseDir = AppDomain.CurrentDomain.BaseDirectory;
				string ruPath = System.IO.Path.Combine(baseDir, "Models", "ru");

				if (System.IO.Directory.Exists(ruPath))
				{
					// Загружаем модель в отдельном потоке, чтобы не блокировать UI
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

		#region Голосовой поиск (кнопка микрофона)
		private void VoiceSearchButton_Click(object sender, RoutedEventArgs e)
		{
			// Если распознавание уже идёт, то остановить его
			if (isRecognizing)
			{
				StopRecognition();
				return;
			}

			if (modelRu == null)
			{
				MessageBox.Show("Нет доступной модели Vosk (ru). Скопируйте её в папку Models.",
					"Распознавание речи недоступно", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			// Меняем иконку на красную точку
			Dispatcher.Invoke(() =>
			{
				VoiceIcon.Kind = PackIconMaterialKind.RecordCircle;
				VoiceIcon.Foreground = Brushes.Red;
			});

			waveIn = new WaveInEvent { WaveFormat = new WaveFormat(16000, 1) };
			recognizerRu = new VoskRecognizer(modelRu, 16000.0f);

			stopRequested = false;
			isRecognizing = true;

			waveIn.DataAvailable += WaveIn_DataAvailable;
			waveIn.StartRecording();
		}

		private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
		{
			if (stopRequested || recognizerRu == null)
				return;

			bool resultRu = false;
			try
			{
				resultRu = recognizerRu.AcceptWaveform(e.Buffer, e.BytesRecorded);
			}
			catch (Exception)
			{
				StopRecognition();
				return;
			}

			if (resultRu)
			{
				string json = string.Empty;
				try
				{
					json = recognizerRu.Result();
				}
				catch (Exception)
				{
					StopRecognition();
					return;
				}
				ProcessVoskResult(json, "ru");
			}
		}

		private void ProcessVoskResult(string json, string lang)
		{
			string recognizedText = ExtractTextFromVoskJson(json);

			if (!string.IsNullOrWhiteSpace(recognizedText))
			{
				Dispatcher.Invoke(() =>
				{
					SearchTextBox.Text = recognizedText;
					ApplyFilters();
				});
			}
			StopRecognition();
		}

		private string ExtractTextFromVoskJson(string json)
		{
			const string pattern = "\"text\" : \"";
			int idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
			if (idx < 0) return "";
			idx += pattern.Length;
			int end = json.IndexOf("\"", idx);
			if (end < 0) return "";
			return json.Substring(idx, end - idx).Trim();
		}

		private void StopRecognition()
		{
			if (stopRequested)
				return;

			stopRequested = true;

			if (waveIn != null)
			{
				try
				{
					waveIn.DataAvailable -= WaveIn_DataAvailable;
					waveIn.StopRecording();
				}
				catch (Exception)
				{
					// Игнорируем исключения при остановке записи
				}
				finally
				{
					waveIn.Dispose();
					waveIn = null;
				}
			}

			if (recognizerRu != null)
			{
				try
				{
					recognizerRu.Dispose();
				}
				catch (Exception)
				{
					// Игнорируем исключения при освобождении распознавателя
				}
				finally
				{
					recognizerRu = null;
				}
			}

			isRecognizing = false;

			// Возвращаем иконку микрофона к исходному виду
			Dispatcher.Invoke(() =>
			{
				VoiceIcon.Kind = PackIconMaterialKind.Microphone;
				VoiceIcon.Foreground = (Brush)FindResource("PrimaryBrush");
			});
		}
		#endregion
	}
}
