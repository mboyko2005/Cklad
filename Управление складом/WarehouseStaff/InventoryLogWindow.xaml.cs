using MahApps.Metro.IconPacks;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using Управление_складом.Themes;
using УправлениеСкладом.Class;
using Microsoft.VisualBasic; 
using OfficeOpenXml;
using System.IO;
using Microsoft.Win32;
using System.Windows.Controls;

namespace УправлениеСкладом.Сотрудник_склада
{
	public partial class InventoryLogWindow : Window, IThemeable
	{
		public class InventoryLogEntry
		{
			public int MovementID { get; set; }
			public int ТоварID { get; set; }
			public int СкладID { get; set; }
			public DateTime Date { get; set; }
			public string Type { get; set; }
			public string ItemName { get; set; }
			public int Quantity { get; set; }
		}

		public class OutOfStockItem
		{
			public int ТоварID { get; set; }
			public string Наименование { get; set; }
			public int Количество { get; set; }
		}

		private ObservableCollection<InventoryLogEntry> _entries = new ObservableCollection<InventoryLogEntry>();
		private ObservableCollection<OutOfStockItem> _outOfStockItems = new ObservableCollection<OutOfStockItem>();

		private string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";
		private int _currentUserId;

		public InventoryLogWindow()
		{
			InitializeComponent();
			InventoryLogDataGrid.ItemsSource = _entries;
			OutOfStockDataGrid.ItemsSource = _outOfStockItems;

			_currentUserId = GetExistingUserId();
			UpdateThemeIcon();
			RefreshAllData();
		}

		private async void Window_Loaded(object sender, RoutedEventArgs e)
		{
			await CheckAndNotifyOutOfStockItemsAsync();
		}

		private async Task CheckAndNotifyOutOfStockItemsAsync()
		{
			try
			{
				using var conn = new SqlConnection(connectionString);
				await conn.OpenAsync();
				string query = @"
                    SELECT t.Наименование
                    FROM Товары t
                    LEFT JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
                    WHERE ISNULL(sp.Количество,0) <= 0";
				using var cmd = new SqlCommand(query, conn);
				using var reader = await cmd.ExecuteReaderAsync();
				var outOfStockList = new System.Collections.Generic.List<string>();
				while (await reader.ReadAsync())
				{
					outOfStockList.Add(reader.GetString(0));
				}
				if (outOfStockList.Count > 0)
				{
					string message = "Внимание! Товары отсутствуют на складе: " + string.Join(", ", outOfStockList);
					await TelegramNotifier.SendNotificationAsync(message, toManager: true);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка проверки товаров на складе: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
				DragMove();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			ThemeManager.ToggleTheme();
			UpdateThemeIcon();
		}

		public void UpdateThemeIcon()
		{
			if (ThemeIcon != null && ThemeIcon is PackIconMaterial iconMaterial)
			{
				iconMaterial.Kind = ThemeManager.IsDarkTheme ? PackIconMaterialKind.WeatherNight : PackIconMaterialKind.WeatherSunny;
			}
		}

		private int GetExistingUserId()
		{
			using var conn = new SqlConnection(connectionString);
			const string query = "SELECT TOP 1 ПользовательID FROM Пользователи ORDER BY ПользовательID";
			var cmd = new SqlCommand(query, conn);
			conn.Open();
			object result = cmd.ExecuteScalar();
			if (result != null && int.TryParse(result.ToString(), out int userId))
			{
				return userId;
			}
			else
			{
				MessageBox.Show("В базе данных нет ни одного пользователя. Невозможно работать с данными.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return -1;
			}
		}

		private void LoadDataFromDatabase()
		{
			_entries.Clear();
			using var conn = new SqlConnection(connectionString);
			const string query = @"
                SELECT dt.ДвижениеID,
                       dt.ТоварID,
                       dt.СкладID,
                       dt.Дата,
                       dt.ТипДвижения,
                       t.Наименование AS ItemName,
                       dt.Количество
                FROM ДвиженияТоваров dt
                INNER JOIN Товары t ON dt.ТоварID = t.ТоварID
                ORDER BY dt.Дата DESC";
			var cmd = new SqlCommand(query, conn);
			conn.Open();
			using var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				var entry = new InventoryLogEntry
				{
					MovementID = reader.GetInt32(reader.GetOrdinal("ДвижениеID")),
					ТоварID = reader.GetInt32(reader.GetOrdinal("ТоварID")),
					СкладID = reader.GetInt32(reader.GetOrdinal("СкладID")),
					Date = reader.GetDateTime(reader.GetOrdinal("Дата")),
					Type = reader.GetString(reader.GetOrdinal("ТипДвижения")),
					ItemName = reader.GetString(reader.GetOrdinal("ItemName")),
					Quantity = reader.GetInt32(reader.GetOrdinal("Количество"))
				};
				_entries.Add(entry);
			}
		}

		private void LoadOutOfStockItems()
		{
			_outOfStockItems.Clear();
			using var conn = new SqlConnection(connectionString);
			const string query = @"
                SELECT t.ТоварID, t.Наименование, ISNULL(sp.Количество,0) AS Количество
                FROM Товары t
                LEFT JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
                WHERE ISNULL(sp.Количество,0) <= 0
                ORDER BY t.Наименование";
			var cmd = new SqlCommand(query, conn);
			conn.Open();
			using var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				var item = new OutOfStockItem
				{
					ТоварID = reader.GetInt32(reader.GetOrdinal("ТоварID")),
					Наименование = reader.GetString(reader.GetOrdinal("Наименование")),
					Количество = reader.GetInt32(reader.GetOrdinal("Количество"))
				};
				_outOfStockItems.Add(item);
			}
		}

		// Добавление прихода с вводом количества (для выбранной записи из журнала)
		private async void AddIncome_Click(object sender, RoutedEventArgs e)
		{
			if (InventoryLogDataGrid.SelectedItem is InventoryLogEntry selectedEntry)
			{
				string input = Interaction.InputBox("Введите количество для прихода:", "Добавить приход", "10");
				if (int.TryParse(input, out int qtyToAdd) && qtyToAdd > 0)
				{
					await AddMovementToDatabaseAsync(selectedEntry.ТоварID, selectedEntry.СкладID, qtyToAdd, "Приход", _currentUserId);
					int stock = GetStockQuantity(selectedEntry.ТоварID, selectedEntry.СкладID);
					await TelegramNotifier.SendNotificationAsync(
						$"Поступил товар: {selectedEntry.ItemName} в количестве {qtyToAdd} единиц. Новый остаток: {stock} единиц.");
					MessageBox.Show($"Новый остаток на складе: {stock} единиц.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
					RefreshAllData();
				}
				else
				{
					MessageBox.Show("Введите корректное количество (больше 0).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
			else
			{
				MessageBox.Show("Выберите товар для добавления прихода.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		// Добавление расхода с вводом количества (для выбранной записи из журнала)
		private async void AddExpense_Click(object sender, RoutedEventArgs e)
		{
			if (InventoryLogDataGrid.SelectedItem is InventoryLogEntry selectedEntry)
			{
				string input = Interaction.InputBox("Введите количество для расхода:", "Добавить расход", "5");
				if (int.TryParse(input, out int qtyToSubtract) && qtyToSubtract > 0)
				{
					await AddMovementToDatabaseAsync(selectedEntry.ТоварID, selectedEntry.СкладID, -qtyToSubtract, "Расход", _currentUserId);
					int stock = GetStockQuantity(selectedEntry.ТоварID, selectedEntry.СкладID);
					await TelegramNotifier.SendNotificationAsync(
						$"Списан товар: {selectedEntry.ItemName} в количестве {qtyToSubtract} единиц. Новый остаток: {stock} единиц.");
					RefreshAllData();
				}
				else
				{
					MessageBox.Show("Введите корректное количество (больше 0).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
			else
			{
				MessageBox.Show("Выберите товар для добавления расхода.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private async void AddStockToSelectedItem_Click(object sender, RoutedEventArgs e)
		{
			if (OutOfStockDataGrid.SelectedItem is OutOfStockItem selectedItem)
			{
				if (int.TryParse(AddQuantityTextBox.Text, out int qty) && qty > 0)
				{
					int defaultWarehouseId = 1;
					await AddMovementToDatabaseAsync(selectedItem.ТоварID, defaultWarehouseId, qty, "Приход", _currentUserId);
					int stock = GetStockQuantity(selectedItem.ТоварID, defaultWarehouseId);
					await TelegramNotifier.SendNotificationAsync(
						$"Поступил товар: {selectedItem.Наименование} в количестве {qty} единиц на склад (ID: {defaultWarehouseId}). Новый остаток: {stock} единиц.");
					RefreshAllData();
					AddQuantityTextBox.Clear();
				}
				else
				{
					MessageBox.Show("Введите корректное количество (больше 0).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
			else
			{
				MessageBox.Show("Выберите товар из списка отсутствующих.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private async void DeleteSelectedRecord_Click(object sender, RoutedEventArgs e)
		{
			if (InventoryLogDataGrid.SelectedItem is InventoryLogEntry selectedEntry)
			{
				var result = MessageBox.Show("Вы действительно хотите удалить выбранную запись?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (result == MessageBoxResult.Yes)
				{
					DeleteMovementFromDatabase(selectedEntry.MovementID, selectedEntry.ТоварID, selectedEntry.СкладID);
					int stock = GetStockQuantity(selectedEntry.ТоварID, selectedEntry.СкладID);
					await TelegramNotifier.SendNotificationAsync(
						$"Запись удалена. Новый остаток товара {selectedEntry.ItemName}: {stock} единиц.");
					RefreshAllData();
				}
			}
			else
			{
				MessageBox.Show("Выберите запись для удаления.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void RefreshAllData()
		{
			LoadDataFromDatabase();
			LoadOutOfStockItems();
		}

		private int GetStockQuantity(int товарID, int складID)
		{
			using var conn = new SqlConnection(connectionString);
			conn.Open();
			string query = "SELECT ISNULL(Количество, 0) FROM СкладскиеПозиции WHERE ТоварID=@ТоварID AND СкладID=@СкладID";
			using var cmd = new SqlCommand(query, conn);
			cmd.Parameters.Add("@ТоварID", SqlDbType.Int).Value = товарID;
			cmd.Parameters.Add("@СкладID", SqlDbType.Int).Value = складID;
			object result = cmd.ExecuteScalar();
			if (result != null && int.TryParse(result.ToString(), out int quantity))
				return quantity;
			return 0;
		}

		private string GetItemName(int товарID)
		{
			using var conn = new SqlConnection(connectionString);
			conn.Open();
			string query = "SELECT Наименование FROM Товары WHERE ТоварID=@ТоварID";
			using var cmd = new SqlCommand(query, conn);
			cmd.Parameters.Add("@ТоварID", SqlDbType.Int).Value = товарID;
			object result = cmd.ExecuteScalar();
			return result?.ToString() ?? "Неизвестный товар";
		}
		private async Task AddMovementToDatabaseAsync(int товарID, int складID, int количество, string типДвижения, int пользовательID)
		{
			if (пользовательID == -1) return;

			try
			{
				using var conn = new SqlConnection(connectionString);
				await conn.OpenAsync();

				const string insertQuery = @"
                    INSERT INTO ДвиженияТоваров (ТоварID, СкладID, Количество, ТипДвижения, ПользовательID, Дата)
                    VALUES (@ТоварID, @СкладID, @Количество, @ТипДвижения, @ПользовательID, GETDATE())";

				using (var cmd = new SqlCommand(insertQuery, conn))
				{
					cmd.Parameters.Add("@ТоварID", SqlDbType.Int).Value = товарID;
					cmd.Parameters.Add("@СкладID", SqlDbType.Int).Value = складID;
					cmd.Parameters.Add("@Количество", SqlDbType.Int).Value = количество;
					cmd.Parameters.Add("@ТипДвижения", SqlDbType.NVarChar, 50).Value = типДвижения;
					cmd.Parameters.Add("@ПользовательID", SqlDbType.Int).Value = пользовательID;
					await cmd.ExecuteNonQueryAsync();
				}
				UpdateStockPosition(conn, товарID, складID);
				int newStock = GetStockQuantity(товарID, складID);
				if (newStock == 0)
				{
					string itemName = GetItemName(товарID);
					await TelegramNotifier.SendNotificationAsync(
						$"Внимание! Товар {itemName} закончился на складе. Остаток: 0 единиц.", true);
				}
			}
			catch (SqlException ex)
			{
				MessageBox.Show("Ошибка при добавлении записи: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void DeleteMovementFromDatabase(int движениеID, int товарID, int складID)
		{
			try
			{
				using var conn = new SqlConnection(connectionString);
				conn.Open();

				const string deleteQuery = @"
                    DELETE FROM ДвиженияТоваров 
                    WHERE ДвижениеID=@ДвижениеID";

				using (var cmdDel = new SqlCommand(deleteQuery, conn))
				{
					cmdDel.Parameters.Add("@ДвижениеID", SqlDbType.Int).Value = движениеID;
					cmdDel.ExecuteNonQuery();
				}

				UpdateStockPosition(conn, товарID, складID);
			}
			catch (SqlException ex)
			{
				MessageBox.Show("Ошибка при удалении записи: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		// Обновление записи о товаре в таблице СкладскиеПозиции
		private void UpdateStockPosition(SqlConnection conn, int товарID, int складID)
		{
			int totalQty = 0;
			int posCount = 0;
			const string query = @"
                SELECT 
                    (SELECT ISNULL(SUM(Количество), 0) FROM ДвиженияТоваров WHERE ТоварID=@ТоварID AND СкладID=@СкладID) AS TotalQty,
                    (SELECT COUNT(*) FROM СкладскиеПозиции WHERE ТоварID=@ТоварID AND СкладID=@СкладID) AS PosCount";
			using (var cmd = new SqlCommand(query, conn))
			{
				cmd.Parameters.Add("@ТоварID", SqlDbType.Int).Value = товарID;
				cmd.Parameters.Add("@СкладID", SqlDbType.Int).Value = складID;
				using (var reader = cmd.ExecuteReader())
				{
					if (reader.Read())
					{
						totalQty = reader.GetInt32(reader.GetOrdinal("TotalQty"));
						posCount = reader.GetInt32(reader.GetOrdinal("PosCount"));
					}
				}
			}

			if (totalQty != 0)
			{
				if (posCount > 0)
				{
					const string updateQuery = @"
                        UPDATE СкладскиеПозиции
                        SET Количество=@NewQuantity,
                            ДатаОбновления=GETDATE()
                        WHERE ТоварID=@ТоварID AND СкладID=@СкладID";
					using (var cmdUpdate = new SqlCommand(updateQuery, conn))
					{
						cmdUpdate.Parameters.Add("@ТоварID", SqlDbType.Int).Value = товарID;
						cmdUpdate.Parameters.Add("@СкладID", SqlDbType.Int).Value = складID;
						cmdUpdate.Parameters.Add("@NewQuantity", SqlDbType.Int).Value = totalQty;
						cmdUpdate.ExecuteNonQuery();
					}
				}
				else
				{
					const string insertPositionQuery = @"
                        INSERT INTO СкладскиеПозиции (ТоварID, СкладID, Количество, ДатаОбновления)
                        VALUES (@ТоварID, @СкладID, @NewQuantity, GETDATE())";
					using (var cmdInsertPos = new SqlCommand(insertPositionQuery, conn))
					{
						cmdInsertPos.Parameters.Add("@ТоварID", SqlDbType.Int).Value = товарID;
						cmdInsertPos.Parameters.Add("@СкладID", SqlDbType.Int).Value = складID;
						cmdInsertPos.Parameters.Add("@NewQuantity", SqlDbType.Int).Value = totalQty;
						cmdInsertPos.ExecuteNonQuery();
					}
				}
			}
			else
			{
				if (posCount > 0)
				{
					const string deletePosQuery = @"
                        DELETE FROM СкладскиеПозиции
                        WHERE ТоварID=@ТоварID AND СкладID=@СкладID";
					using (var cmdDelPos = new SqlCommand(deletePosQuery, conn))
					{
						cmdDelPos.Parameters.Add("@ТоварID", SqlDbType.Int).Value = товарID;
						cmdDelPos.Parameters.Add("@СкладID", SqlDbType.Int).Value = складID;
						cmdDelPos.ExecuteNonQuery();
					}
				}
			}
		}

		private async void PrintWriteOffAct_Click(object sender, RoutedEventArgs e)
		{
			if (InventoryLogDataGrid.SelectedItem is InventoryLogEntry selectedEntry)
			{
				if (selectedEntry.Type != "Расход")
				{
					MessageBox.Show("Пожалуйста, выберите запись с типом 'Расход' для создания акта о списании.", 
						"Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				// Получаем дополнительную информацию о товаре
				string organizationName = "ООО \"Фирма ОренКлип\"";  // Значение по умолчанию
				string departmentName = "Склад";  // Значение по умолчанию
				string okpoCode = "12345678";  // Значение по умолчанию
				decimal itemPrice = 0;
				string unitName = "шт.";
				string unitCode = "796"; // Код ОКЕИ для штук
				
				using (var conn = new SqlConnection(connectionString))
				{
					try 
					{
						await conn.OpenAsync();

						// Получаем цену товара
						var cmdPrice = new SqlCommand("SELECT Цена FROM Товары WHERE ТоварID = @ТоварID", conn);
						cmdPrice.Parameters.AddWithValue("@ТоварID", selectedEntry.ТоварID);
						var priceResult = await cmdPrice.ExecuteScalarAsync();
						if (priceResult != null && priceResult != DBNull.Value)
						{
							itemPrice = Convert.ToDecimal(priceResult);
						}
					}
					catch (Exception ex)
					{
						// Логируем ошибку, но продолжаем выполнение
						Console.WriteLine($"Ошибка при получении данных: {ex.Message}");
					}
				}

				var saveFileDialog = new SaveFileDialog
				{
					Filter = "Excel файлы (*.xlsx)|*.xlsx",
					FileName = $"Акт_списания_{DateTime.Now:yyyy-MM-dd_HH-mm}"
				};

				if (saveFileDialog.ShowDialog() == true)
				{
					try
					{
						ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
						using (var package = new ExcelPackage())
						{
							var worksheet = package.Workbook.Worksheets.Add("Акт списания");

							// Настройка размеров и стилей
							worksheet.DefaultColWidth = 10;
							worksheet.DefaultRowHeight = 15;

							// Установка ширины столбцов (в пикселях)
							worksheet.Column(1).Width = 20;  // A
							worksheet.Column(2).Width = 20;  // B
							worksheet.Column(3).Width = 15;  // C
							worksheet.Column(4).Width = 15;  // D
							worksheet.Column(5).Width = 30;  // E
							worksheet.Column(6).Width = 15;  // F
							worksheet.Column(7).Width = 15;  // G
							worksheet.Column(8).Width = 15;  // H

							// Верхний блок
							worksheet.Cells["A1:F1"].Merge = true;
							worksheet.Cells["A1"].Value = "Унифицированная форма № ТОРГ-16";
							worksheet.Cells["A1"].Style.Font.Bold = true;
							worksheet.Cells["G1:H1"].Merge = true;
							worksheet.Cells["G1"].Value = "Код";

							worksheet.Cells["A2:F2"].Merge = true;
							worksheet.Cells["A2"].Value = "Утверждена постановлением Госкомстата России от 25.12.98г. № 132";
							worksheet.Cells["G2"].Value = "Форма по ОКУД";
							worksheet.Cells["H2"].Value = "0330216";

							worksheet.Cells["G3"].Value = "по ОКПО";
							worksheet.Cells["H3"].Value = okpoCode;

							// Организация
							worksheet.Cells["A4:H4"].Merge = true;
							worksheet.Cells["A4"].Value = organizationName;
							worksheet.Cells["A4"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
							worksheet.Cells["A4"].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Hair;

							worksheet.Cells["A5"].Value = "(организация)";
							worksheet.Cells["A5"].Style.Font.Size = 8;
							worksheet.Cells["A5"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

							worksheet.Cells["A6:H6"].Merge = true;
							worksheet.Cells["A6"].Value = departmentName;
							worksheet.Cells["A6"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
							worksheet.Cells["A6"].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Hair;

							worksheet.Cells["A7"].Value = "(структурное подразделение)";
							worksheet.Cells["A7"].Style.Font.Size = 8;
							worksheet.Cells["A7"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

							// Основание
							worksheet.Cells["A8"].Value = "Основание для составления акта:";
							worksheet.Cells["F8:G8"].Merge = true;
							worksheet.Cells["F8"].Value = "Вид деятельности по ОКДП";

							worksheet.Cells["A9:E9"].Merge = true;
							worksheet.Cells["A9"].Value = "приказ, распоряжение";
							worksheet.Cells["A9"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
							worksheet.Cells["A9"].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Hair;

							worksheet.Cells["A10"].Value = "(нужное зачеркнуть)";
							worksheet.Cells["A10"].Style.Font.Size = 8;
							worksheet.Cells["A10"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

							// Номер документа и дата
							worksheet.Cells["F9"].Value = "номер";
							worksheet.Cells["F10"].Value = "дата";

							// УТВЕРЖДАЮ блок
							worksheet.Cells["F11"].Value = "УТВЕРЖДАЮ";
							worksheet.Cells["F11"].Style.Font.Bold = true;
							worksheet.Cells["F12"].Value = "Руководитель";
							worksheet.Cells["F13"].Value = "(должность)";
							worksheet.Cells["F13"].Style.Font.Size = 8;
							worksheet.Cells["F14"].Value = "(подпись)";
							worksheet.Cells["F14"].Style.Font.Size = 8;
							worksheet.Cells["F15"].Value = "(расшифровка подписи)";
							worksheet.Cells["F15"].Style.Font.Size = 8;
							worksheet.Cells["F16"].Value = $"\"{DateTime.Now:dd}\" {DateTime.Now:MMMM} {DateTime.Now:yyyy} г.";

							// АКТ
							worksheet.Cells["A17:H17"].Merge = true;
							worksheet.Cells["A17"].Value = "АКТ";
							worksheet.Cells["A17"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
							worksheet.Cells["A17"].Style.Font.Bold = true;

							worksheet.Cells["A18:H18"].Merge = true;
							worksheet.Cells["A18"].Value = "О СПИСАНИИ ТОВАРОВ";
							worksheet.Cells["A18"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
							worksheet.Cells["A18"].Style.Font.Bold = true;

							// Номер документа и дата составления
							worksheet.Cells["F19"].Value = "Номер документа";
							worksheet.Cells["G19"].Value = "Дата составления";
							worksheet.Cells["F20"].Value = selectedEntry.MovementID.ToString();
							worksheet.Cells["G20"].Value = DateTime.Now.ToString("dd.MM.yyyy");

							// Таблица
							worksheet.Cells["A21:B21"].Merge = true;
							worksheet.Cells["A21"].Value = "Дата";
							worksheet.Cells["C21:D21"].Merge = true;
							worksheet.Cells["C21"].Value = "Товарная накладная";
							worksheet.Cells["E21:F21"].Merge = true;
							worksheet.Cells["E21"].Value = "Признаки понижения качества (причины списания)";

							// Подзаголовки таблицы
							worksheet.Cells["A22"].Value = "поступления товара";
							worksheet.Cells["B22"].Value = "списания товара";
							worksheet.Cells["C22"].Value = "номер";
							worksheet.Cells["D22"].Value = "дата";
							worksheet.Cells["E22"].Value = "наименование";
							worksheet.Cells["F22"].Value = "код";

							// Нумерация столбцов
							worksheet.Cells["A23"].Value = "1";
							worksheet.Cells["B23"].Value = "2";
							worksheet.Cells["C23"].Value = "3";
							worksheet.Cells["D23"].Value = "4";
							worksheet.Cells["E23"].Value = "5";
							worksheet.Cells["F23"].Value = "6";

							// Данные
							worksheet.Cells["A24"].Value = selectedEntry.Date.ToString("dd.MM.yyyy");
							worksheet.Cells["B24"].Value = DateTime.Now.ToString("dd.MM.yyyy");
							worksheet.Cells["C24"].Value = selectedEntry.MovementID.ToString();
							worksheet.Cells["D24"].Value = selectedEntry.Date.ToString("dd.MM.yyyy");
							worksheet.Cells["E24"].Value = "Физический износ";
							worksheet.Cells["F24"].Value = "01";

							// Форматирование таблицы
							using (var range = worksheet.Cells["A21:F24"])
							{
								range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
								range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
								range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
								range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
								range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
							}

							// Подписи
							int currentRow = 26;
							worksheet.Cells[$"A{currentRow}"].Value = "Члены комиссии:";
							currentRow++;

							// Председатель комиссии
							worksheet.Cells[$"A{currentRow}"].Value = "Председатель";
							worksheet.Cells[$"B{currentRow}:D{currentRow}"].Merge = true;
							worksheet.Cells[$"B{currentRow}"].Value = "_____________________";
							worksheet.Cells[$"E{currentRow}"].Value = "(должность)";
							worksheet.Cells[$"E{currentRow}"].Style.Font.Size = 8;
							worksheet.Cells[$"F{currentRow}"].Value = "(подпись)";
							worksheet.Cells[$"F{currentRow}"].Style.Font.Size = 8;
							worksheet.Cells[$"G{currentRow}:H{currentRow}"].Merge = true;
							worksheet.Cells[$"G{currentRow}"].Value = "(расшифровка подписи)";
							worksheet.Cells[$"G{currentRow}"].Style.Font.Size = 8;
							currentRow++;

							// Члены комиссии
							for (int i = 0; i < 3; i++)
							{
								worksheet.Cells[$"A{currentRow}"].Value = "Члены комиссии";
								worksheet.Cells[$"B{currentRow}:D{currentRow}"].Merge = true;
								worksheet.Cells[$"B{currentRow}"].Value = "_____________________";
								worksheet.Cells[$"E{currentRow}"].Value = "(должность)";
								worksheet.Cells[$"E{currentRow}"].Style.Font.Size = 8;
								worksheet.Cells[$"F{currentRow}"].Value = "(подпись)";
								worksheet.Cells[$"F{currentRow}"].Style.Font.Size = 8;
								worksheet.Cells[$"G{currentRow}:H{currentRow}"].Merge = true;
								worksheet.Cells[$"G{currentRow}"].Value = "(расшифровка подписи)";
								worksheet.Cells[$"G{currentRow}"].Style.Font.Size = 8;
								currentRow++;
							}

							// Оборотная сторона
							var worksheet2 = package.Workbook.Worksheets.Add("Оборотная сторона");

							// Заголовок оборотной стороны
							worksheet2.Cells["A1:J1"].Merge = true;
							worksheet2.Cells["A1"].Value = "Оборотная сторона формы № ТОРГ-16";
							worksheet2.Cells["A1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
							worksheet2.Cells["A1"].Style.Font.Bold = true;

							// Настройка размеров столбцов
							worksheet2.Column(1).Width = 25;  // Наименование товара
							worksheet2.Column(2).Width = 10;  // Код
							worksheet2.Column(3).Width = 15;  // Ед. изм. наименование
							worksheet2.Column(4).Width = 12;  // Код по ОКЕИ
							worksheet2.Column(5).Width = 12;  // Количество
							worksheet2.Column(6).Width = 12;  // Масса одного места
							worksheet2.Column(7).Width = 12;  // Масса нетто
							worksheet2.Column(8).Width = 15;  // Цена
							worksheet2.Column(9).Width = 15;  // Стоимость
							worksheet2.Column(10).Width = 15; // Примечание

							// Заголовки таблицы
							worksheet2.Cells["A3:B3"].Merge = true;
							worksheet2.Cells["A3"].Value = "Товар";
							worksheet2.Cells["C3:D3"].Merge = true;
							worksheet2.Cells["C3"].Value = "Единица измерения";
							worksheet2.Cells["E3"].Value = "Количество";
							worksheet2.Cells["F3"].Value = "Масса";
							worksheet2.Cells["G3"].Value = "Масса";
							worksheet2.Cells["H3"].Value = "Цена,";
							worksheet2.Cells["I3"].Value = "Стоимость,";
							worksheet2.Cells["J3"].Value = "Примечание";

							worksheet2.Cells["E4"].Value = "мест";
							worksheet2.Cells["F4"].Value = "одного места";
							worksheet2.Cells["G4"].Value = "нетто";
							worksheet2.Cells["H4"].Value = "руб. коп.";
							worksheet2.Cells["I4"].Value = "руб. коп.";

							worksheet2.Cells["A5"].Value = "наименование";
							worksheet2.Cells["B5"].Value = "код";
							worksheet2.Cells["C5"].Value = "наименование";
							worksheet2.Cells["D5"].Value = "код по ОКЕИ";
							worksheet2.Cells["E5"].Value = "(штук)";
							worksheet2.Cells["F5"].Value = "(штуки)";

							// Нумерация столбцов
							for (int i = 0; i < 10; i++)
							{
								worksheet2.Cells[6, i + 1].Value = (i + 1).ToString();
							}

							// Данные
							worksheet2.Cells["A7"].Value = selectedEntry.ItemName;
							worksheet2.Cells["B7"].Value = selectedEntry.ТоварID;
							worksheet2.Cells["C7"].Value = unitName;
							worksheet2.Cells["D7"].Value = unitCode;
							worksheet2.Cells["E7"].Value = Math.Abs(selectedEntry.Quantity);
							worksheet2.Cells["H7"].Value = itemPrice;
							worksheet2.Cells["I7"].Value = itemPrice * Math.Abs(selectedEntry.Quantity);

							// Форматирование таблицы
							using (var range = worksheet2.Cells["A3:J7"])
							{
								range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
								range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
								range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
								range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
								range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
								range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
							}

							// Итого
							worksheet2.Cells["A9"].Value = "Итого:";
							worksheet2.Cells["I9"].Value = itemPrice * Math.Abs(selectedEntry.Quantity);

							// Сумма прописью
							decimal totalSum = itemPrice * Math.Abs(selectedEntry.Quantity);
							string sumInWords = ConvertNumberToWords(totalSum);
							worksheet2.Cells["A11"].Value = "Сумма списания";
							worksheet2.Cells["B11:J11"].Merge = true;
							worksheet2.Cells["B11"].Value = sumInWords;
							worksheet2.Cells["A12"].Value = "(прописью)";
							worksheet2.Cells["A12"].Style.Font.Size = 8;

							// Предупреждение
							worksheet2.Cells["A14:J14"].Merge = true;
							worksheet2.Cells["A14"].Value = "Все члены комиссии предупреждены об ответственности за подписание акта, содержащего данные, не соответствующие действительности";

							// Подписи
							currentRow = 16;
							worksheet2.Cells[$"A{currentRow}"].Value = "Председатель комиссии";
							worksheet2.Cells[$"B{currentRow}"].Value = "(должность)";
							worksheet2.Cells[$"B{currentRow}"].Style.Font.Size = 8;
							worksheet2.Cells[$"D{currentRow}"].Value = "(подпись)";
							worksheet2.Cells[$"D{currentRow}"].Style.Font.Size = 8;
							worksheet2.Cells[$"F{currentRow}"].Value = "(расшифровка подписи)";
							worksheet2.Cells[$"F{currentRow}"].Style.Font.Size = 8;

							currentRow += 2;
							for (int i = 0; i < 3; i++)
							{
								worksheet2.Cells[$"A{currentRow + i}"].Value = "Члены комиссии";
								worksheet2.Cells[$"B{currentRow + i}"].Value = "(должность)";
								worksheet2.Cells[$"B{currentRow + i}"].Style.Font.Size = 8;
								worksheet2.Cells[$"D{currentRow + i}"].Value = "(подпись)";
								worksheet2.Cells[$"D{currentRow + i}"].Style.Font.Size = 8;
								worksheet2.Cells[$"F{currentRow + i}"].Value = "(расшифровка подписи)";
								worksheet2.Cells[$"F{currentRow + i}"].Style.Font.Size = 8;
							}

							currentRow += 4;
							worksheet2.Cells[$"A{currentRow}"].Value = "Материально ответственное лицо";
							worksheet2.Cells[$"B{currentRow}"].Value = "(должность)";
							worksheet2.Cells[$"B{currentRow}"].Style.Font.Size = 8;
							worksheet2.Cells[$"D{currentRow}"].Value = "(подпись)";
							worksheet2.Cells[$"D{currentRow}"].Style.Font.Size = 8;
							worksheet2.Cells[$"F{currentRow}"].Value = "(расшифровка подписи)";
							worksheet2.Cells[$"F{currentRow}"].Style.Font.Size = 8;

							currentRow += 2;
							worksheet2.Cells[$"A{currentRow}"].Value = "Решение руководителя:";
							currentRow++;
							worksheet2.Cells[$"A{currentRow}:J{currentRow}"].Merge = true;
							worksheet2.Cells[$"A{currentRow}"].Value = "Стоимость списанного товара отнести на счет_____________________________________________";
							currentRow++;
							worksheet2.Cells[$"A{currentRow}:J{currentRow}"].Merge = true;
							worksheet2.Cells[$"A{currentRow}"].Value = "(указать источник (себестоимость, прибыль, материально ответственное лицо и т.д.))";
							worksheet2.Cells[$"A{currentRow}"].Style.Font.Size = 8;

							// Общие настройки для обоих листов
							foreach (var ws in new[] { worksheet, worksheet2 })
							{
								ws.View.ShowGridLines = false;
								ws.PrinterSettings.PaperSize = ePaperSize.A4;
								ws.PrinterSettings.Orientation = eOrientation.Portrait;
								ws.PrinterSettings.FitToPage = true;
							}

							// Сохранение файла
							package.SaveAs(new FileInfo(saveFileDialog.FileName));
						}

						MessageBox.Show("Акт о списании успешно создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show($"Ошибка при создании акта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите запись для создания акта о списании.", 
					"Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private string ConvertNumberToWords(decimal number)
		{
			string[] units = { "", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять", "десять" };
			string[] teens = { "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать" };
			string[] tens = { "", "десять", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто" };
			string[] hundreds = { "", "сто", "двести", "триста", "четыреста", "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот" };

			if (number == 0)
				return "ноль рублей 00 копеек";

			int rubles = (int)Math.Floor(number);
			int kopeks = (int)Math.Round((number - rubles) * 100);

			string result = "";

			if (rubles > 0)
			{
				// Обработка сотен
				int hundreds_num = rubles / 100;
				if (hundreds_num > 0 && hundreds_num < 10)
					result += hundreds[hundreds_num] + " ";

				// Обработка десятков и единиц
				int remainder = rubles % 100;
				if (remainder >= 10 && remainder <= 19)
				{
					result += teens[remainder - 11] + " ";
				}
				else
				{
					int tens_num = (remainder / 10);
					if (tens_num > 0)
						result += tens[tens_num] + " ";

					int units_num = remainder % 10;
					if (units_num > 0)
					{
						// Особые случаи для "один" и "два"
						if (units_num == 1)
							result += "один ";
						else if (units_num == 2)
							result += "два ";
						else
							result += units[units_num] + " ";
					}
				}

				// Добавление слова "рубль" в правильной форме
				remainder = rubles % 100;
				if (remainder >= 11 && remainder <= 19)
				{
					result += "рублей ";
				}
				else
				{
					int units_num = rubles % 10;
					if (units_num == 1)
						result += "рубль ";
					else if (units_num >= 2 && units_num <= 4)
						result += "рубля ";
					else
						result += "рублей ";
				}
			}

			// Добавление копеек
			result += kopeks.ToString("00") + " ";
			
			// Добавление слова "копейка" в правильной форме
			if (kopeks >= 11 && kopeks <= 19)
			{
				result += "копеек";
			}
			else
			{
				int last_digit = kopeks % 10;
				if (last_digit == 1)
					result += "копейка";
				else if (last_digit >= 2 && last_digit <= 4)
					result += "копейки";
				else
					result += "копеек";
			}

			return result.Trim();
		}
	}
}
