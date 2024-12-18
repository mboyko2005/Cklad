using MahApps.Metro.IconPacks;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using Управление_складом.Themes; // Предполагается, что есть ThemeManager

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

        private ObservableCollection<InventoryLogEntry> _entries;
        private ObservableCollection<OutOfStockItem> _outOfStockItems;

        private string connectionString = @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

        // Поле для хранения ID пользователя, который реально существует в БД
        private int _currentUserId;

        public InventoryLogWindow()
        {
            InitializeComponent();
            _entries = new ObservableCollection<InventoryLogEntry>();
            _outOfStockItems = new ObservableCollection<OutOfStockItem>();
            InventoryLogDataGrid.ItemsSource = _entries;
            OutOfStockDataGrid.ItemsSource = _outOfStockItems;
            
            // Получаем реальный пользовательский ID из базы
            _currentUserId = GetExistingUserId();

            UpdateThemeIcon();
            LoadDataFromDatabase();
            LoadOutOfStockItems();
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

        private int GetExistingUserId()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT TOP 1 ПользовательID FROM Пользователи ORDER BY ПользовательID";
                SqlCommand cmd = new SqlCommand(query, conn);
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
        }

        private void LoadDataFromDatabase()
        {
            _entries.Clear();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"
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

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
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
            }
        }

        private void LoadOutOfStockItems()
        {
            _outOfStockItems.Clear();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT t.ТоварID, t.Наименование, ISNULL(sp.Количество,0) AS Количество
                    FROM Товары t
                    LEFT JOIN СкладскиеПозиции sp ON t.ТоварID=sp.ТоварID
                    WHERE ISNULL(sp.Количество,0) <= 0
                    ORDER BY t.Наименование";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
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
            }
        }

        private void AddIncome_Click(object sender, RoutedEventArgs e)
        {
            AddMovementToDatabase(1, 1, 10, "Приход", _currentUserId);
            RefreshAllData();
        }

        private void AddExpense_Click(object sender, RoutedEventArgs e)
        {
            AddMovementToDatabase(2, 1, -5, "Расход", _currentUserId);
            RefreshAllData();
        }

        private void AddStockToSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            if (OutOfStockDataGrid.SelectedItem is OutOfStockItem selectedItem)
            {
                if (int.TryParse(AddQuantityTextBox.Text, out int qty) && qty > 0)
                {
                    AddMovementToDatabase(selectedItem.ТоварID, 1, qty, "Приход", _currentUserId);
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
                MessageBox.Show("Выберите товар из списка.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteSelectedRecord_Click(object sender, RoutedEventArgs e)
        {
            if (InventoryLogDataGrid.SelectedItem is InventoryLogEntry selectedEntry)
            {
                var result = MessageBox.Show("Вы действительно хотите удалить выбранную запись?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    DeleteMovementFromDatabase(selectedEntry.MovementID, selectedEntry.ТоварID, selectedEntry.СкладID);
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

        private void AddMovementToDatabase(int товарID, int складID, int количество, string типДвижения, int пользовательID)
        {
            if (пользовательID == -1)
            {
                // Нет пользователя в БД, прерываем
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    
                    // 1. Добавляем запись о движении товара
                    string insertQuery = @"
                        INSERT INTO ДвиженияТоваров (ТоварID, СкладID, Количество, ТипДвижения, ПользовательID, Дата)
                        VALUES (@ТоварID, @СкладID, @Количество, @ТипДвижения, @ПользовательID, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.Add("@ТоварID", SqlDbType.Int).Value = товарID;
                        cmd.Parameters.Add("@СкладID", SqlDbType.Int).Value = складID;
                        cmd.Parameters.Add("@Количество", SqlDbType.Int).Value = количество;
                        cmd.Parameters.Add("@ТипДвижения", SqlDbType.NVarChar, 50).Value = типДвижения;
                        cmd.Parameters.Add("@ПользовательID", SqlDbType.Int).Value = пользовательID;

                        cmd.ExecuteNonQuery();
                    }

                    UpdateStockPosition(conn, товарID, складID);
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
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Удаляем запись о движении
                    string deleteQuery = @"
                        DELETE FROM ДвиженияТоваров 
                        WHERE ДвижениеID=@ДвижениеID";
                    
                    using (SqlCommand cmdDel = new SqlCommand(deleteQuery, conn))
                    {
                        cmdDel.Parameters.Add("@ДвижениеID", SqlDbType.Int).Value = движениеID;
                        cmdDel.ExecuteNonQuery();
                    }

                    UpdateStockPosition(conn, товарID, складID);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Ошибка при удалении записи: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStockPosition(SqlConnection conn, int товарID, int складID)
        {
            // Суммируем все движения для данного товара и склада
            string sumQuery = @"
                SELECT ISNULL(SUM(Количество),0)
                FROM ДвиженияТоваров
                WHERE ТоварID=@ТоварID AND СкладID=@СкладID";

            int newQuantity;
            using (SqlCommand cmdSum = new SqlCommand(sumQuery, conn))
            {
                cmdSum.Parameters.Add("@ТоварID", SqlDbType.Int).Value = товарID;
                cmdSum.Parameters.Add("@СкладID", SqlDbType.Int).Value = складID;
                newQuantity = (int)cmdSum.ExecuteScalar();
            }

            // Проверим, есть ли запись в СкладскиеПозиции
            string checkPositionQuery = @"
                SELECT COUNT(*) 
                FROM СкладскиеПозиции
                WHERE ТоварID=@ТоварID AND СкладID=@СкладID";

            int count;
            using (SqlCommand cmdCheck = new SqlCommand(checkPositionQuery, conn))
            {
                cmdCheck.Parameters.Add("@ТоварID", SqlDbType.Int).Value = товарID;
                cmdCheck.Parameters.Add("@СкладID", SqlDbType.Int).Value = складID;
                count = (int)cmdCheck.ExecuteScalar();
            }

            if (newQuantity != 0)
            {
                // Если количество не равно нулю
                if (count > 0)
                {
                    string updateQuery = @"
                        UPDATE СкладскиеПозиции
                        SET Количество=@NewQuantity,
                            ДатаОбновления=GETDATE()
                        WHERE ТоварID=@ТоварID AND СкладID=@СкладID";

                    using (SqlCommand cmdUpdate = new SqlCommand(updateQuery, conn))
                    {
                        cmdUpdate.Parameters.Add("@ТоварID", SqlDbType.Int).Value = товарID;
                        cmdUpdate.Parameters.Add("@СкладID", SqlDbType.Int).Value = складID;
                        cmdUpdate.Parameters.Add("@NewQuantity", SqlDbType.Int).Value = newQuantity;
                        cmdUpdate.ExecuteNonQuery();
                    }
                }
                else
                {
                    // Если записи не было, создаём новую
                    string insertPositionQuery = @"
                        INSERT INTO СкладскиеПозиции (ТоварID, СкладID, Количество, ДатаОбновления)
                        VALUES (@ТоварID, @СкладID, @NewQuantity, GETDATE())";

                    using (SqlCommand cmdInsertPos = new SqlCommand(insertPositionQuery, conn))
                    {
                        cmdInsertPos.Parameters.Add("@ТоварID", SqlDbType.Int).Value = товарID;
                        cmdInsertPos.Parameters.Add("@СкладID", SqlDbType.Int).Value = складID;
                        cmdInsertPos.Parameters.Add("@NewQuantity", SqlDbType.Int).Value = newQuantity;
                        cmdInsertPos.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                // Если итоговое количество = 0, возможно удаляем позицию со склада, если она есть
                // Так как у нас условие "отсутствующие товары" - можно либо оставить ноль, либо удалить запись.
                // Решим удалить запись, чтобы OutOfStockItems корректно показывались.

                if (count > 0)
                {
                    string deletePosQuery = @"
                        DELETE FROM СкладскиеПозиции
                        WHERE ТоварID=@ТоварID AND СкладID=@СкладID";

                    using (SqlCommand cmdDelPos = new SqlCommand(deletePosQuery, conn))
                    {
                        cmdDelPos.Parameters.Add("@ТоварID", SqlDbType.Int).Value = товарID;
                        cmdDelPos.Parameters.Add("@СкладID", SqlDbType.Int).Value = складID;
                        cmdDelPos.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
