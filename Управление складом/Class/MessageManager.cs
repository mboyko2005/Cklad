using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace УправлениеСкладом.Class
{
    /// <summary>
    /// Класс для управления сообщениями в мессенджере
    /// </summary>
    public class MessageManager
    {
        private readonly string connectionString;
        private readonly int currentUserId;
        private readonly string currentUserLogin;
        
        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="connectionString">Строка подключения к базе данных</param>
        /// <param name="userId">Идентификатор текущего пользователя</param>
        /// <param name="userLogin">Логин текущего пользователя</param>
        public MessageManager(string connectionString, int userId, string userLogin)
        {
            this.connectionString = connectionString;
            this.currentUserId = userId;
            this.currentUserLogin = userLogin;
        }

        /// <summary>
        /// Отправить текстовое сообщение
        /// </summary>
        /// <param name="recipientId">ID получателя</param>
        /// <param name="messageText">Текст сообщения</param>
        /// <returns>ID отправленного сообщения или -1 в случае ошибки</returns>
        public async Task<int> SendTextMessageAsync(int recipientId, string messageText)
        {
            try
            {
                // Шифруем сообщение
                string encryptedBase64 = EncryptionHelper.EncryptString(messageText);
                byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    // Удаляем ссылку на поле Редактировано
                    string sql = @"
                        INSERT INTO Сообщения (ОтправительID, ПолучательID, Текст, ДатаОтправки, Прочитано, СкрытоОтправителем, СкрытоПолучателем)
                        VALUES (@senderId, @recipientId, @messageText, @sendDate, 0, 0, 0);
                        SELECT SCOPE_IDENTITY();";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@senderId", currentUserId);
                        command.Parameters.AddWithValue("@recipientId", recipientId);
                        command.Parameters.AddWithValue("@messageText", encryptedBytes);
                        command.Parameters.AddWithValue("@sendDate", DateTime.Now);

                        // Получаем ID нового сообщения
                        var result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке сообщения: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Отправить сообщение с вложением
        /// </summary>
        /// <param name="recipientId">ID получателя</param>
        /// <param name="messageText">Текст сообщения (может быть пустым)</param>
        /// <param name="attachment">Вложение</param>
        /// <returns>ID отправленного сообщения или -1 в случае ошибки</returns>
        public async Task<int> SendMessageWithAttachmentAsync(int recipientId, string messageText, MessageAttachment attachment)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Начинаем транзакцию
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Сначала добавляем сообщение
                            int messageId;
                            string encryptedBase64 = EncryptionHelper.EncryptString(messageText);
                            byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);
                            
                            // Удаляем ссылку на поле Редактировано
                            string sql = @"
                                INSERT INTO Сообщения (ОтправительID, ПолучательID, Текст, ДатаОтправки, Прочитано, СкрытоОтправителем, СкрытоПолучателем)
                                VALUES (@senderId, @recipientId, @messageText, @sendDate, 0, 0, 0);
                                SELECT SCOPE_IDENTITY();";

                            using (SqlCommand command = new SqlCommand(sql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@senderId", currentUserId);
                                command.Parameters.AddWithValue("@recipientId", recipientId);
                                command.Parameters.AddWithValue("@messageText", encryptedBytes);
                                command.Parameters.AddWithValue("@sendDate", DateTime.Now);

                                var result = await command.ExecuteScalarAsync();
                                messageId = Convert.ToInt32(result);
                            }

                            // Добавляем вложение
                            string attachmentSql = @"
                                INSERT INTO Вложения (СообщениеID, ТипВложения, ИмяФайла, РазмерФайла, Содержимое, Миниатюра)
                                VALUES (@messageId, @attachmentType, @fileName, @fileSize, @content, @thumbnail);
                                SELECT SCOPE_IDENTITY();";

                            using (SqlCommand attachmentCommand = new SqlCommand(attachmentSql, connection, transaction))
                            {
                                attachmentCommand.Parameters.AddWithValue("@messageId", messageId);
                                attachmentCommand.Parameters.AddWithValue("@attachmentType", attachment.Type.ToString());
                                attachmentCommand.Parameters.AddWithValue("@fileName", attachment.FileName);
                                attachmentCommand.Parameters.AddWithValue("@fileSize", attachment.FileSize);
                                attachmentCommand.Parameters.AddWithValue("@content", attachment.Content);
                                attachmentCommand.Parameters.Add("@thumbnail", SqlDbType.VarBinary).Value = 
                                    (attachment.Thumbnail != null) ? (object)attachment.Thumbnail : DBNull.Value;

                                await attachmentCommand.ExecuteNonQueryAsync();
                            }

                            // Фиксируем транзакцию
                            transaction.Commit();
                            
                            return messageId;
                        }
                        catch (Exception ex)
                        {
                            // Откатываем транзакцию в случае ошибки
                            transaction.Rollback();
                            throw new Exception($"Ошибка при отправке сообщения с вложением: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке сообщения с вложением: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Редактировать сообщение
        /// </summary>
        /// <param name="messageId">ID сообщения</param>
        /// <param name="newText">Новый текст сообщения</param>
        /// <returns>true если редактирование прошло успешно</returns>
        public async Task<bool> EditMessageAsync(int messageId, string newText)
        {
            try
            {
                // Проверяем, является ли пользователь отправителем
                if (!await IsMessageSenderAsync(messageId))
                {
                    MessageBox.Show("Вы можете редактировать только свои сообщения.");
                    return false;
                }

                string encryptedBase64 = EncryptionHelper.EncryptString(newText);
                byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Исправляем запрос, удаляя ссылку на несуществующий столбец "Редактировано"
                    string sql = @"
                        UPDATE Сообщения
                        SET Текст = @newText
                        WHERE СообщениеID = @messageId AND ОтправительID = @senderId";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@messageId", messageId);
                        command.Parameters.AddWithValue("@newText", encryptedBytes);
                        command.Parameters.AddWithValue("@senderId", currentUserId);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании сообщения: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Проверяет, является ли текущий пользователь отправителем сообщения
        /// </summary>
        private async Task<bool> IsMessageSenderAsync(int messageId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = "SELECT ОтправительID FROM Сообщения WHERE СообщениеID = @messageId";
                    
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@messageId", messageId);
                        var result = await command.ExecuteScalarAsync();
                        
                        if (result != null && result != DBNull.Value)
                        {
                            int senderId = Convert.ToInt32(result);
                            return senderId == currentUserId;
                        }
                        
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Удалить сообщение для текущего пользователя
        /// </summary>
        /// <param name="messageId">ID сообщения</param>
        /// <returns>true если удаление прошло успешно</returns>
        public async Task<bool> DeleteMessageForMeAsync(int messageId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql;
                    
                    // Проверяем, является ли пользователь отправителем или получателем
                    sql = "SELECT ОтправительID, ПолучательID FROM Сообщения WHERE СообщениеID = @messageId";
                    bool isSender = false;
                    bool isRecipient = false;
                    
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@messageId", messageId);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                int senderId = reader.GetInt32(0);
                                int recipientId = reader.GetInt32(1);
                                
                                isSender = (senderId == currentUserId);
                                isRecipient = (recipientId == currentUserId);
                            }
                        }
                    }
                    
                    if (isSender)
                    {
                        sql = @"
                            UPDATE Сообщения
                            SET СкрытоОтправителем = 1
                            WHERE СообщениеID = @messageId AND ОтправительID = @userId";
                    }
                    else if (isRecipient)
                    {
                        sql = @"
                            UPDATE Сообщения
                            SET СкрытоПолучателем = 1
                            WHERE СообщениеID = @messageId AND ПолучательID = @userId";
                    }
                    else
                    {
                        return false; // Пользователь не является ни отправителем, ни получателем
                    }
                    
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@messageId", messageId);
                        command.Parameters.AddWithValue("@userId", currentUserId);
                        
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении сообщения: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Удалить сообщение для всех участников диалога
        /// </summary>
        /// <param name="messageId">ID сообщения</param>
        /// <returns>true если удаление прошло успешно</returns>
        public async Task<bool> DeleteMessageForAllAsync(int messageId)
        {
            try
            {
                // Проверяем, является ли пользователь отправителем
                if (!await IsMessageSenderAsync(messageId))
                {
                    MessageBox.Show("Вы можете удалять для всех только свои сообщения.");
                    return false;
                }

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = @"
                        UPDATE Сообщения
                        SET СкрытоОтправителем = 1, СкрытоПолучателем = 1
                        WHERE СообщениеID = @messageId AND ОтправительID = @senderId";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@messageId", messageId);
                        command.Parameters.AddWithValue("@senderId", currentUserId);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении сообщения для всех: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Удалить все сообщения в диалоге для текущего пользователя
        /// </summary>
        /// <param name="contactId">ID контакта</param>
        /// <returns>Количество удаленных сообщений</returns>
        public async Task<int> DeleteConversationAsync(int contactId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = @"
                        UPDATE Сообщения
                        SET СкрытоОтправителем = 1
                        WHERE ОтправительID = @currentUserId AND ПолучательID = @contactId;
                        
                        UPDATE Сообщения
                        SET СкрытоПолучателем = 1
                        WHERE ОтправительID = @contactId AND ПолучательID = @currentUserId;";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@currentUserId", currentUserId);
                        command.Parameters.AddWithValue("@contactId", contactId);

                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении диалога: {ex.Message}");
                return 0;
            }
        }
        
        /// <summary>
        /// Получить вложение сообщения
        /// </summary>
        /// <param name="messageId">ID сообщения</param>
        /// <returns>Вложение или null, если вложения нет</returns>
        public async Task<MessageAttachment> GetMessageAttachmentAsync(int messageId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = @"
                        SELECT ВложениеID, ТипВложения, ИмяФайла, РазмерФайла, Содержимое, Миниатюра
                        FROM Вложения
                        WHERE СообщениеID = @messageId";
                    
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@messageId", messageId);
                        
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                MessageAttachment attachment = new MessageAttachment
                                {
                                    AttachmentId = reader.GetInt32(0),
                                    MessageId = messageId,
                                    Type = ParseAttachmentType(reader.GetString(1)),
                                    FileName = reader.GetString(2),
                                    FileSize = reader.GetInt64(3),
                                    Content = (byte[])reader.GetValue(4)
                                };
                                
                                if (!reader.IsDBNull(5))
                                {
                                    attachment.Thumbnail = (byte[])reader.GetValue(5);
                                }
                                
                                return attachment;
                            }
                        }
                    }
                }
                
                return null; // Вложение не найдено
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении вложения: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Преобразовать строку в тип вложения
        /// </summary>
        private AttachmentType ParseAttachmentType(string typeString)
        {
            if (Enum.TryParse(typeString, out AttachmentType type))
            {
                return type;
            }
            
            return AttachmentType.Other;
        }
        
        /// <summary>
        /// Метод для определения типа вложения на основе расширения файла
        /// </summary>
        public static AttachmentType GetAttachmentTypeFromFileName(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            
            // Изображения
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff" };
            if (imageExtensions.Contains(extension))
                return AttachmentType.Image;
                
            // Документы
            string[] documentExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf" };
            if (documentExtensions.Contains(extension))
                return AttachmentType.Document;
                
            // Аудио
            string[] audioExtensions = { ".mp3", ".wav", ".ogg", ".flac", ".m4a", ".aac" };
            if (audioExtensions.Contains(extension))
                return AttachmentType.Audio;
                
            // Видео
            string[] videoExtensions = { ".mp4", ".mov", ".avi", ".mkv", ".wmv", ".flv" };
            if (videoExtensions.Contains(extension))
                return AttachmentType.Video;
                
            // Другое
            return AttachmentType.Other;
        }
        
        /// <summary>
        /// Отметить все сообщения в диалоге как прочитанные
        /// </summary>
        public async Task MarkAllMessagesAsReadAsync(int contactId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = @"
                        UPDATE Сообщения
                        SET Прочитано = 1
                        WHERE ОтправительID = @contactId
                          AND ПолучательID = @currentUserId
                          AND Прочитано = 0";
                          
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@contactId", contactId);
                        command.Parameters.AddWithValue("@currentUserId", currentUserId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении статуса сообщений: {ex.Message}");
            }
        }
    }
} 