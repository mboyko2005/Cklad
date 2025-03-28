using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace УправлениеСкладом.Class
{
    /// <summary>
    /// Класс для поиска сообщений в мессенджере
    /// </summary>
    public class SearchManager
    {
        private readonly string connectionString;
        private readonly int currentUserId;

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="connectionString">Строка подключения к базе данных</param>
        /// <param name="userId">Идентификатор текущего пользователя</param>
        public SearchManager(string connectionString, int userId)
        {
            this.connectionString = connectionString;
            this.currentUserId = userId;
        }

        /// <summary>
        /// Выполнить поиск сообщений в переписке с указанным контактом
        /// </summary>
        /// <param name="contactId">ID контакта</param>
        /// <param name="searchQuery">Поисковый запрос</param>
        /// <returns>Список найденных сообщений</returns>
        public async Task<List<SearchResult>> SearchMessagesAsync(int contactId, string searchQuery)
        {
            List<SearchResult> results = new List<SearchResult>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    string sql = @"
                        SELECT СообщениеID, ОтправительID, Текст, ДатаОтправки
                        FROM Сообщения
                        WHERE ((ОтправительID = @currentUserId AND ПолучательID = @contactId AND СкрытоОтправителем = 0)
                           OR (ОтправительID = @contactId AND ПолучательID = @currentUserId AND СкрытоПолучателем = 0))
                        ORDER BY ДатаОтправки ASC";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@currentUserId", currentUserId);
                        command.Parameters.AddWithValue("@contactId", contactId);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                int messageId = reader.GetInt32(0);
                                int senderId = reader.GetInt32(1);
                                byte[] encryptedBytes = (byte[])reader["Текст"];
                                string encryptedBase64 = Convert.ToBase64String(encryptedBytes);
                                string decryptedText = EncryptionHelper.DecryptString(encryptedBase64);
                                DateTime messageDate = reader.GetDateTime(3);
                                
                                // Проверяем, содержит ли текст сообщения поисковый запрос
                                if (!string.IsNullOrEmpty(decryptedText) && 
                                    decryptedText.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    results.Add(new SearchResult 
                                    { 
                                        MessageId = messageId,
                                        SenderId = senderId,
                                        Text = decryptedText,
                                        Date = messageDate,
                                        IsOutgoing = (senderId == currentUserId)
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при поиске сообщений: {ex.Message}", "Ошибка", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }

            return results;
        }

        /// <summary>
        /// Получить количество результатов поиска для всех контактов
        /// </summary>
        /// <param name="searchQuery">Поисковый запрос</param>
        /// <returns>Словарь с ID контактов и количеством найденных сообщений</returns>
        public async Task<Dictionary<int, int>> GetSearchResultCountsAsync(string searchQuery)
        {
            Dictionary<int, int> results = new Dictionary<int, int>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Получаем список всех контактов, с которыми есть переписка
                    string contactsSql = @"
                        SELECT DISTINCT 
                            CASE 
                                WHEN ОтправительID = @currentUserId THEN ПолучательID 
                                ELSE ОтправительID 
                            END AS ContactId
                        FROM Сообщения
                        WHERE ОтправительID = @currentUserId OR ПолучательID = @currentUserId";

                    List<int> contacts = new List<int>();
                    using (SqlCommand command = new SqlCommand(contactsSql, connection))
                    {
                        command.Parameters.AddWithValue("@currentUserId", currentUserId);
                        
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                contacts.Add(reader.GetInt32(0));
                            }
                        }
                    }
                    
                    // Для каждого контакта выполняем поиск и подсчитываем результаты
                    foreach (int contactId in contacts)
                    {
                        string sql = @"
                            SELECT СообщениеID, Текст
                            FROM Сообщения
                            WHERE ((ОтправительID = @currentUserId AND ПолучательID = @contactId AND СкрытоОтправителем = 0)
                                OR (ОтправительID = @contactId AND ПолучательID = @currentUserId AND СкрытоПолучателем = 0))";

                        int count = 0;
                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@currentUserId", currentUserId);
                            command.Parameters.AddWithValue("@contactId", contactId);

                            using (SqlDataReader reader = await command.ExecuteReaderAsync())
                            {
                                while (reader.Read())
                                {
                                    byte[] encryptedBytes = (byte[])reader["Текст"];
                                    string encryptedBase64 = Convert.ToBase64String(encryptedBytes);
                                    string decryptedText = EncryptionHelper.DecryptString(encryptedBase64);
                                    
                                    if (!string.IsNullOrEmpty(decryptedText) && 
                                        decryptedText.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        count++;
                                    }
                                }
                            }
                        }
                        
                        if (count > 0)
                        {
                            results[contactId] = count;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при подсчете результатов поиска: {ex.Message}", "Ошибка", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }

            return results;
        }
    }
} 