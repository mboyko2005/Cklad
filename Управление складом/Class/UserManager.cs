using System;
using System.Data.SqlClient;
using System.Windows;

namespace УправлениеСкладом.Class
{
    public class UserManager
    {
        private string connectionString;
        private int currentUserId;
        private string currentUserName;
        
        public int CurrentUserId => currentUserId;
        public string CurrentUserName => currentUserName;
        
        public UserManager(string connectionString, int userId)
        {
            this.connectionString = connectionString;
            this.currentUserId = userId;
            LoadCurrentUserInfo();
        }
        
        // Загрузка информации о текущем пользователе
        public void LoadCurrentUserInfo()
        {
            try
            {
                // Получаем логин пользователя из глобальных свойств приложения или из базы данных
                string savedUsername = Application.Current.Properties["CurrentUsername"]?.ToString();
                
                if (!string.IsNullOrEmpty(savedUsername))
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "SELECT ИмяПользователя FROM Пользователи WHERE ПользовательID = @userId";
                        
                        // Если есть сохраненный логин, проверяем, соответствует ли он ID пользователя
                        if (!string.IsNullOrEmpty(savedUsername))
                        {
                            query = "SELECT ИмяПользователя FROM Пользователи WHERE ПользовательID = @userId AND ИмяПользователя = @username";
                        }
                        
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@userId", currentUserId);
                            
                            if (!string.IsNullOrEmpty(savedUsername))
                            {
                                command.Parameters.AddWithValue("@username", savedUsername);
                            }
                            
                            var result = command.ExecuteScalar();
                            
                            if (result != null)
                            {
                                currentUserName = result.ToString();
                            }
                            else if (!string.IsNullOrEmpty(savedUsername))
                            {
                                // Если логин не соответствует ID, получаем логин по ID
                                SqlCommand newCommand = new SqlCommand("SELECT ИмяПользователя FROM Пользователи WHERE ПользовательID = @userId", connection);
                                newCommand.Parameters.AddWithValue("@userId", currentUserId);
                                result = newCommand.ExecuteScalar();
                                
                                if (result != null)
                                {
                                    currentUserName = result.ToString();
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Если нет сохраненного логина, получаем его из базы данных
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "SELECT ИмяПользователя FROM Пользователи WHERE ПользовательID = @userId";
                        
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@userId", currentUserId);
                            var result = command.ExecuteScalar();
                            
                            if (result != null)
                            {
                                currentUserName = result.ToString();
                            }
                            else
                            {
                                currentUserName = "Пользователь #" + currentUserId;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке информации о пользователе: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Удаление старых сообщений
        public int DeleteOldMessages(int daysOld)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    
                    // Запрос для пометки сообщений как скрытых для текущего пользователя
                    string sql = @"
                        UPDATE Сообщения
                        SET СкрытоОтправителем = CASE WHEN ОтправительID = @userId THEN 1 ELSE СкрытоОтправителем END,
                            СкрытоПолучателем = CASE WHEN ПолучательID = @userId THEN 1 ELSE СкрытоПолучателем END
                        WHERE ДатаОтправки < @oldDate
                        AND (
                            (ОтправительID = @userId AND СкрытоОтправителем = 0)
                            OR 
                            (ПолучательID = @userId AND СкрытоПолучателем = 0)
                        )";
                        
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        DateTime oldDate = DateTime.Now.AddDays(-daysOld);
                        command.Parameters.AddWithValue("@userId", currentUserId);
                        command.Parameters.AddWithValue("@oldDate", oldDate);
                        
                        int affectedRows = command.ExecuteNonQuery();
                        return affectedRows;
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не показываем пользователю
                System.Diagnostics.Debug.WriteLine($"Ошибка при удалении старых сообщений: {ex.Message}");
                return 0;
            }
        }
    }
} 