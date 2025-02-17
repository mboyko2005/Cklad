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
using Microsoft.VisualBasic; // Для использования InputBox

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
			public string Type { get; set; } // "Приход" или "Расход"
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

		// При загрузке окна выполняется проверка отсутствующих товаров
		private async void Window_Loaded(object sender, RoutedEventArgs e)
		{
			await CheckAndNotifyOutOfStockItemsAsync();
		}

		// Метод проверяет наличие товаров с нулевым количеством и уведомляет менеджера
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
			if (ThemeIcon != null)
			{
				ThemeIcon.Kind = ThemeManager.IsDarkTheme ? PackIconMaterialKind.WeatherNight : PackIconMaterialKind.WeatherSunny;
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
					AddMovementToDatabase(selectedEntry.ТоварID, selectedEntry.СкладID, qtyToAdd, "Приход", _currentUserId);
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
					AddMovementToDatabase(selectedEntry.ТоварID, selectedEntry.СкладID, -qtyToSubtract, "Расход", _currentUserId);
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

		// Добавление прихода для отсутствующего товара (склад по умолчанию = 1)
		private async void AddStockToSelectedItem_Click(object sender, RoutedEventArgs e)
		{
			if (OutOfStockDataGrid.SelectedItem is OutOfStockItem selectedItem)
			{
				if (int.TryParse(AddQuantityTextBox.Text, out int qty) && qty > 0)
				{
					int defaultWarehouseId = 1;
					AddMovementToDatabase(selectedItem.ТоварID, defaultWarehouseId, qty, "Приход", _currentUserId);
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
						$"Запись удалена. Новый остаток товара '{selectedEntry.ItemName}': {stock} единиц.");
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

		// Получает наименование товара по его ID
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

		// Добавляет движение (приход или расход) в базу
		private void AddMovementToDatabase(int товарID, int складID, int количество, string типДвижения, int пользовательID)
		{
			if (пользовательID == -1) return;

			try
			{
				using var conn = new SqlConnection(connectionString);
				conn.Open();

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
					cmd.ExecuteNonQuery();
				}

				UpdateStockPosition(conn, товарID, складID);

				// Если после обновления остаток равен 0, уведомляем менеджера
				int newStock = GetStockQuantity(товарID, складID);
				if (newStock == 0)
				{
					string itemName = GetItemName(товарID);
					TelegramNotifier.SendNotificationAsync(
						$"Внимание! Товар '{itemName}' закончился на складе. Остаток: 0 единиц.", true)
						.GetAwaiter().GetResult();
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

		private void UpdateStockPosition(SqlConnection conn, int товарID, int складID)
		{
			const string combinedQuery = @"
                SELECT 
                    ISNULL(SUM(dt.Количество),0) AS TotalQty,
                    CASE WHEN sp.ТоварID IS NOT NULL THEN 1 ELSE 0 END AS PosExists
                FROM ДвиженияТоваров dt
                LEFT JOIN СкладскиеПозиции sp ON dt.ТоварID=sp.ТоварID AND dt.СкладID=sp.СкладID
                WHERE dt.ТоварID=@ТоварID AND dt.СкладID=@СкладID
                GROUP BY sp.ТоварID";

			int newQuantity = 0;
			int posExists = 0;

			using (var cmdCombined = new SqlCommand(combinedQuery, conn))
			{
				cmdCombined.Parameters.Add("@ТоварID", SqlDbType.Int).Value = товарID;
				cmdCombined.Parameters.Add("@СкладID", SqlDbType.Int).Value = складID;

				using var reader = cmdCombined.ExecuteReader();
				if (reader.Read())
				{
					newQuantity = reader.GetInt32(reader.GetOrdinal("TotalQty"));
					posExists = reader.GetInt32(reader.GetOrdinal("PosExists"));
				}
				else
				{
					newQuantity = 0;
					posExists = 0;
				}
			}

			if (newQuantity != 0)
			{
				if (posExists == 1)
				{
					const string updateQuery = @"
                        UPDATE СкладскиеПозиции
                        SET Количество=@NewQuantity,
                            ДатаОбновления=GETDATE()
                        WHERE ТоварID=@ТоварID AND СкладID=@СкладID";

					using var cmdUpdate = new SqlCommand(updateQuery, conn);
					cmdUpdate.Parameters.Add("@ТоварID", SqlDbType.Int).Value = товарID;
					cmdUpdate.Parameters.Add("@СкладID", SqlDbType.Int).Value = складID;
					cmdUpdate.Parameters.Add("@NewQuantity", SqlDbType.Int).Value = newQuantity;
					cmdUpdate.ExecuteNonQuery();
				}
				else
				{
					const string insertPositionQuery = @"
                        INSERT INTO СкладскиеПозиции (ТоварID, СкладID, Количество, ДатаОбновления)
                        VALUES (@ТоварID, @СкладID, @NewQuantity, GETDATE())";

					using var cmdInsertPos = new SqlCommand(insertPositionQuery, conn);
					cmdInsertPos.Parameters.Add("@ТоварID", SqlDbType.Int).Value = товарID;
					cmdInsertPos.Parameters.Add("@СкладID", SqlDbType.Int).Value = складID;
					cmdInsertPos.Parameters.Add("@NewQuantity", SqlDbType.Int).Value = newQuantity;
					cmdInsertPos.ExecuteNonQuery();
				}
			}
			else
			{
				if (posExists == 1)
				{
					const string deletePosQuery = @"
                        DELETE FROM СкладскиеПозиции
                        WHERE ТоварID=@ТоварID AND СкладID=@СкладID";

					using var cmdDelPos = new SqlCommand(deletePosQuery, conn);
					cmdDelPos.Parameters.Add("@ТоварID", SqlDbType.Int).Value = товарID;
					cmdDelPos.Parameters.Add("@СкладID", SqlDbType.Int).Value = складID;
					cmdDelPos.ExecuteNonQuery();
				}
			}
		}
	}
}
