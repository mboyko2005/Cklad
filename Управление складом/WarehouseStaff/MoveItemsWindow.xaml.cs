using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using System.Windows.Data;
using System.Windows.Media;
using System.ComponentModel;
using System.Threading.Tasks;
using Vosk;
using Управление_складом.Class;
using Управление_складом.Themes;

namespace УправлениеСкладом.Сотрудник_склада
{
	public partial class MoveItemsWindow : Window
	{
		public class Item
		{
			public int ТоварID { get; set; }
			public string Наименование { get; set; }
			public string Категория { get; set; }
			public int Количество { get; set; }
			public string Поставщик { get; set; }
			public string Склад { get; set; }
		}

		private List<Item> items;
		private List<Item> displayedItems;
		private string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		private Model modelRu;
		private VoiceInputService voiceService;

		public MoveItemsWindow()
		{
			InitializeComponent();
			LoadData();
			UpdateThemeIcon();
			this.Loaded += MoveItemsWindow_Loaded;
		}

		private async void MoveItemsWindow_Loaded(object sender, RoutedEventArgs e)
		{
			await InitializeVoskAsync();
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
		}

		protected override void OnClosed(EventArgs e)
		{
			voiceService?.Stop();
			base.OnClosed(e);
		}

		private async Task InitializeVoskAsync()
		{
			try
			{
				Vosk.Vosk.SetLogLevel(0);
				string baseDir = AppDomain.CurrentDomain.BaseDirectory;
				string ruPath = System.IO.Path.Combine(baseDir, "Models", "ru");
				if (System.IO.Directory.Exists(ruPath))
					modelRu = await Task.Run(() => new Model(ruPath));

				if (modelRu == null)
					MessageBox.Show("Отсутствует офлайн-модель Vosk для ru в папке Models.",
									"Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка инициализации Vosk: {ex.Message}",
								"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
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
				VoiceIcon.Foreground = (Brush)FindResource("PrimaryBrush");
			}
			else
			{
				VoiceIcon.Kind = PackIconMaterialKind.RecordCircle;
				VoiceIcon.Foreground = Brushes.Red;
				voiceService.Start();
			}
		}

		private void LoadData()
		{
			LoadItems();
			LoadCategories();
			LoadLocations();
		}

		private void LoadItems()
		{
			items = new List<Item>();
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = @"
						SELECT t.ТоварID, t.Наименование, t.Категория, sp.Количество, p.Наименование AS Поставщик, s.Наименование AS Склад
						FROM Товары t
						JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
						JOIN Склады s ON sp.СкладID = s.СкладID
						LEFT JOIN Поставщики p ON t.ПоставщикID = p.ПоставщикID
						WHERE sp.Количество > 0
						ORDER BY t.ТоварID";

					using (SqlCommand cmd = new SqlCommand(query, connection))
					using (SqlDataReader reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							items.Add(new Item
							{
								ТоварID = reader.GetInt32(0),
								Наименование = reader.GetString(1),
								Категория = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
								Количество = reader.GetInt32(3),
								Поставщик = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
								Склад = reader.GetString(5)
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

		private void LoadCategories()
		{
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = "SELECT DISTINCT Категория FROM Товары WHERE Категория IS NOT NULL AND LTRIM(RTRIM(Категория)) <> ''";
					using (SqlCommand cmd = new SqlCommand(query, connection))
					using (SqlDataReader reader = cmd.ExecuteReader())
					{
						List<string> categories = new List<string> { "Все" };
						while (reader.Read())
						{
							string cat = reader.GetString(0).Trim();
							if (!string.IsNullOrEmpty(cat)) categories.Add(cat);
						}
						CategoryComboBox.ItemsSource = categories;
						CategoryComboBox.SelectedIndex = 0;
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void LoadLocations()
		{
			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				try
				{
					connection.Open();
					string query = "SELECT Наименование FROM Склады WHERE LTRIM(RTRIM(Наименование)) <> ''";
					using (SqlCommand cmd = new SqlCommand(query, connection))
					using (SqlDataReader reader = cmd.ExecuteReader())
					{
						List<string> locs = new List<string>();
						while (reader.Read())
						{
							string loc = reader.GetString(0).Trim();
							if (!string.IsNullOrEmpty(loc)) locs.Add(loc);
						}
						TargetLocationComboBox.ItemsSource = locs;
						if (locs.Count > 0) TargetLocationComboBox.SelectedIndex = 0;
					}
				}
				catch (SqlException ex)
				{
					MessageBox.Show($"Ошибка загрузки местоположений: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void Filter_TextChanged(object sender, RoutedEventArgs e)
		{
			ApplyFilters();
		}

		private void ApplyFilters()
		{
			if (items == null) return;
			string searchText = SearchTextBox.Text.Trim().ToLower();
			string selectedCategory = CategoryComboBox.SelectedItem as string ?? "Все";

			displayedItems = items
				.Where(i =>
					(string.IsNullOrEmpty(searchText)
					 || i.Наименование.ToLower().Contains(searchText)
					 || i.Категория.ToLower().Contains(searchText)
					 || i.Склад.ToLower().Contains(searchText))
					&& (selectedCategory == "Все" || i.Категория == selectedCategory))
				.ToList();

			ItemsDataGrid.ItemsSource = displayedItems;
			ItemsDataGrid.SelectedIndex = -1;
			ItemsDataGrid.UnselectAll();
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed) this.DragMove();
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

		private void UpdateThemeIcon()
		{
			if (ThemeIcon != null)
				ThemeIcon.Kind = ThemeManager.IsDarkTheme
					? PackIconMaterialKind.WeatherNight
					: PackIconMaterialKind.WeatherSunny;
		}

		private void MoveButton_Click(object sender, RoutedEventArgs e)
		{
			var item = ItemsDataGrid.SelectedItem as Item;
			if (item == null)
			{
				MessageBox.Show("Пожалуйста, выберите товар.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			string src = SourceLocationComboBox.SelectedItem as string;
			string dst = TargetLocationComboBox.SelectedItem as string;
			if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(dst))
			{
				MessageBox.Show("Выберите оба местоположения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			if (src == dst)
			{
				MessageBox.Show("Местоположения должны отличаться.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			if (!int.TryParse(MoveQuantityTextBox.Text, out int moveQty) || moveQty <= 0)
			{
				MessageBox.Show("Некорректное количество.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			int currentQty = GetQuantityInWarehouse(item.ТоварID, src);
			if (moveQty > currentQty)
			{
				MessageBox.Show("Недостаточно товара.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			int srcId = GetSkladIDByName(src);
			int dstId = GetSkladIDByName(dst);
			if (srcId == -1 || dstId == -1)
			{
				MessageBox.Show("Склад не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			int userID = GetCurrentUserID();
			if (MoveItem(item.ТоварID, srcId, dstId, moveQty, userID))
			{
				MessageBox.Show("Товар перемещён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
				
				// Создаем и печатаем накладную
				try
				{
					Управление_складом.Class.InvoiceGenerator invoiceGenerator = new Управление_складом.Class.InvoiceGenerator(
						connectionString, 
						item.ТоварID, 
						src, 
						dst, 
						moveQty, 
						userID);
					
					invoiceGenerator.GenerateAndPrintInvoice();
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка при печати накладной: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				
				// Обновляем данные
				LoadItems();
				ApplyFilters();
				MoveQuantityTextBox.Clear();
			}
			else
			{
				MessageBox.Show("Ошибка перемещения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private int GetSkladIDByName(string name)
		{
			using (SqlConnection conn = new SqlConnection(connectionString))
			{
				try
				{
					conn.Open();
					string query = "SELECT СкладID FROM Склады WHERE Наименование = @n";
					using (SqlCommand cmd = new SqlCommand(query, conn))
					{
						cmd.Parameters.AddWithValue("@n", name);
						object res = cmd.ExecuteScalar();
						return res != null ? Convert.ToInt32(res) : -1;
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка ID склада: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					return -1;
				}
			}
		}

		private int GetCurrentUserID()
		{
			// Условно ID = 3
			return 3;
		}

		private int GetQuantityInWarehouse(int товарID, string skladName)
		{
			// Получаем количество из загруженного списка товаров
			var item = items.FirstOrDefault(i => i.ТоварID == товарID && i.Склад == skladName);
			if (item != null)
				return item.Количество;
			return 0;
		}

		private List<string> GetWarehousesForProduct(int товарID)
		{
			List<string> list = new List<string>();
			using (SqlConnection conn = new SqlConnection(connectionString))
			{
				try
				{
					conn.Open();
					string q = @"SELECT s.Наименование
								 FROM СкладскиеПозиции sp
								 JOIN Склады s ON sp.СкладID = s.СкладID
								 WHERE sp.ТоварID = @tid AND sp.Количество > 0";
					using (SqlCommand cmd = new SqlCommand(q, conn))
					{
						cmd.Parameters.AddWithValue("@tid", товарID);
						using (SqlDataReader rdr = cmd.ExecuteReader())
						{
							while (rdr.Read())
								list.Add(rdr.GetString(0));
						}
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка складов товара: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			return list;
		}

		private void SetSourceLocationComboBox(int товарID)
		{
			// Получаем все склады, где есть данный товар с положительным количеством
			var list = items
				.Where(i => i.ТоварID == товарID && i.Количество > 0)
				.Select(i => i.Склад)
				.Distinct()
				.ToList();
			
			if (list.Count == 0)
			{
				SourceLocationComboBox.ItemsSource = null;
				SourceLocationComboBox.IsEnabled = false;
			}
			else if (list.Count == 1)
			{
				SourceLocationComboBox.ItemsSource = list;
				SourceLocationComboBox.SelectedIndex = 0;
				SourceLocationComboBox.IsEnabled = false;
			}
			else
			{
				SourceLocationComboBox.ItemsSource = list;
				SourceLocationComboBox.SelectedIndex = 0;
				SourceLocationComboBox.IsEnabled = true;
			}
		}

		private void ItemsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			var selected = ItemsDataGrid.SelectedItem as Item;
			if (selected != null) SetSourceLocationComboBox(selected.ТоварID);
			else
			{
				SourceLocationComboBox.ItemsSource = null;
				SourceLocationComboBox.IsEnabled = false;
			}
		}

		private void ItemsDataGrid_Loaded(object sender, RoutedEventArgs e)
		{
			if (ItemsDataGrid.ItemsSource != null)
			{
				ICollectionView view = CollectionViewSource.GetDefaultView(ItemsDataGrid.ItemsSource);
				view.SortDescriptions.Clear();
				view.SortDescriptions.Add(new SortDescription("ТоварID", ListSortDirection.Ascending));
				view.Refresh();
			}
		}

		private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			foreach (char c in e.Text) if (!char.IsDigit(c)) { e.Handled = true; break; }
		}

		private bool MoveItem(int товарID, int srcSkladID, int dstSkladID, int qty, int userID)
		{
			using (SqlConnection conn = new SqlConnection(connectionString))
			{
				conn.Open();
				using (SqlTransaction tran = conn.BeginTransaction())
				{
					try
					{
						string checkQ = @"SELECT COUNT(*) FROM СкладскиеПозиции WHERE ТоварID=@tid AND СкладID=@sid AND Количество>=@q";
						using (SqlCommand cmd = new SqlCommand(checkQ, conn, tran))
						{
							cmd.Parameters.AddWithValue("@tid", товарID);
							cmd.Parameters.AddWithValue("@sid", srcSkladID);
							cmd.Parameters.AddWithValue("@q", qty);
							int count = (int)cmd.ExecuteScalar();
							if (count == 0) { tran.Rollback(); return false; }
						}

						// Проверка текущего количества товара на исходном складе
						string getQtyQuery = @"SELECT Количество FROM СкладскиеПозиции WHERE ТоварID=@tid AND СкладID=@sid";
						int currentQty;
						using (SqlCommand cmd = new SqlCommand(getQtyQuery, conn, tran))
						{
							cmd.Parameters.AddWithValue("@tid", товарID);
							cmd.Parameters.AddWithValue("@sid", srcSkladID);
							currentQty = (int)cmd.ExecuteScalar();
						}

						// Если перемещаем все, удаляем запись
						if (currentQty == qty)
						{
							string delSrc = @"DELETE FROM СкладскиеПозиции WHERE ТоварID=@tid AND СкладID=@sid";
							using (SqlCommand cmd = new SqlCommand(delSrc, conn, tran))
							{
								cmd.Parameters.AddWithValue("@tid", товарID);
								cmd.Parameters.AddWithValue("@sid", srcSkladID);
								cmd.ExecuteNonQuery();
							}
						}
						else
						{
							// Иначе уменьшаем количество
							string updSrc = @"UPDATE СкладскиеПозиции SET Количество = Количество - @q, ДатаОбновления=GETDATE()
											  WHERE ТоварID=@tid AND СкладID=@sid";
							using (SqlCommand cmd = new SqlCommand(updSrc, conn, tran))
							{
								cmd.Parameters.AddWithValue("@q", qty);
								cmd.Parameters.AddWithValue("@tid", товарID);
								cmd.Parameters.AddWithValue("@sid", srcSkladID);
								cmd.ExecuteNonQuery();
							}
						}

						string checkDst = @"SELECT COUNT(*) FROM СкладскиеПозиции WHERE ТоварID=@tid AND СкладID=@sid";
						int dstCount;
						using (SqlCommand cmd = new SqlCommand(checkDst, conn, tran))
						{
							cmd.Parameters.AddWithValue("@tid", товарID);
							cmd.Parameters.AddWithValue("@sid", dstSkladID);
							dstCount = (int)cmd.ExecuteScalar();
						}

						if (dstCount > 0)
						{
							string updDst = @"UPDATE СкладскиеПозиции SET Количество = Количество + @q, ДатаОбновления=GETDATE()
											   WHERE ТоварID=@tid AND СкладID=@sid";
							using (SqlCommand cmd = new SqlCommand(updDst, conn, tran))
							{
								cmd.Parameters.AddWithValue("@q", qty);
								cmd.Parameters.AddWithValue("@tid", товарID);
								cmd.Parameters.AddWithValue("@sid", dstSkladID);
								cmd.ExecuteNonQuery();
							}
						}
						else
						{
							string insDst = @"INSERT INTO СкладскиеПозиции(ТоварID, СкладID, Количество, ДатаОбновления)
											  VALUES(@tid, @sid, @q, GETDATE())";
							using (SqlCommand cmd = new SqlCommand(insDst, conn, tran))
							{
								cmd.Parameters.AddWithValue("@q", qty);
								cmd.Parameters.AddWithValue("@tid", товарID);
								cmd.Parameters.AddWithValue("@sid", dstSkladID);
								cmd.ExecuteNonQuery();
							}
						}

						string insMov = @"INSERT INTO ДвиженияТоваров(ТоварID, СкладID, Количество, ТипДвижения, ПользовательID)
										  VALUES(@tid, @sid, @cnt, @type, @uid)";
						using (SqlCommand cmd = new SqlCommand(insMov, conn, tran))
						{
							cmd.Parameters.AddWithValue("@tid", товарID);
							cmd.Parameters.AddWithValue("@sid", srcSkladID);
							cmd.Parameters.AddWithValue("@cnt", -qty);
							cmd.Parameters.AddWithValue("@type", "Перемещение: Расход");
							cmd.Parameters.AddWithValue("@uid", userID);
							cmd.ExecuteNonQuery();

							cmd.Parameters["@sid"].Value = dstSkladID;
							cmd.Parameters["@cnt"].Value = qty;
							cmd.Parameters["@type"].Value = "Перемещение: Приход";
							cmd.ExecuteNonQuery();
						}

						tran.Commit();
						return true;
					}
					catch
					{
						tran.Rollback();
						return false;
					}
				}
			}
		}
	}
}
