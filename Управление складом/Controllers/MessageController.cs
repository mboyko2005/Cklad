using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace УправлениеСкладом.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly string _connectionString =
            @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True;TrustServerCertificate=True";

        [HttpGet("messages/{userId}")]
        public IActionResult GetMessages(int userId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT m.СообщениеID, m.ОтправительID, m.ПолучательID, m.Текст, m.ДатаОтправки, m.Прочитано 
                        FROM Сообщения m 
                        WHERE m.ОтправительID = @userId OR m.ПолучательID = @userId 
                        ORDER BY m.ДатаОтправки DESC";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            var messages = new List<object>();
                            while (reader.Read())
                            {
                                // Получаем зашифрованный текст и расшифровываем его
                                byte[] encryptedBytes = (byte[])reader["Текст"];
                                string encryptedBase64 = Convert.ToBase64String(encryptedBytes);
                                string decryptedText = EncryptionHelper.DecryptString(encryptedBase64);

                                messages.Add(new
                                {
                                    messageId = reader.GetInt32(reader.GetOrdinal("СообщениеID")),
                                    senderId = reader.GetInt32(reader.GetOrdinal("ОтправительID")),
                                    receiverId = reader.GetInt32(reader.GetOrdinal("ПолучательID")),
                                    text = decryptedText,
                                    timestamp = reader.GetDateTime(reader.GetOrdinal("ДатаОтправки")),
                                    isRead = reader.GetBoolean(reader.GetOrdinal("Прочитано"))
                                });
                            }
                            return Ok(new { success = true, messages });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("send")]
        public IActionResult SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.text))
                {
                    return BadRequest(new { success = false, message = "Текст сообщения не может быть пустым" });
                }

                if (request.senderId <= 0 || request.receiverId <= 0)
                {
                    return BadRequest(new { success = false, message = "Некорректные данные отправителя или получателя" });
                }

                // Проверяем, существуют ли пользователи
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string checkUsersSql = @"
                        SELECT COUNT(*) FROM Пользователи 
                        WHERE (ПользовательID = @senderId OR ПользовательID = @receiverId)";

                    using (var checkCmd = new SqlCommand(checkUsersSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@senderId", request.senderId);
                        checkCmd.Parameters.AddWithValue("@receiverId", request.receiverId);
                        int userCount = (int)checkCmd.ExecuteScalar();
                        
                        if (userCount != 2)
                        {
                            return BadRequest(new { success = false, message = "Один или оба пользователя не существуют" });
                        }
                    }

                    // Шифруем текст сообщения
                    string encryptedBase64 = EncryptionHelper.EncryptString(request.text);
                    byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);

                    string sql = @"
                        INSERT INTO Сообщения (ОтправительID, ПолучательID, Текст, ДатаОтправки, Прочитано, СкрытоОтправителем, СкрытоПолучателем)
                        VALUES (@senderId, @receiverId, @text, @timestamp, 0, 0, 0);
                        SELECT SCOPE_IDENTITY();";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@senderId", request.senderId);
                        cmd.Parameters.AddWithValue("@receiverId", request.receiverId);
                        cmd.Parameters.AddWithValue("@text", encryptedBytes);
                        cmd.Parameters.AddWithValue("@timestamp", DateTime.Now);

                        var messageId = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        // Получаем только что отправленное сообщение для возврата клиенту
                        string getMessageSql = @"
                            SELECT m.СообщениеID, m.ОтправительID, m.ПолучательID, m.Текст, m.ДатаОтправки, m.Прочитано 
                            FROM Сообщения m 
                            WHERE m.СообщениеID = @messageId";
                            
                        using (var getCmd = new SqlCommand(getMessageSql, conn))
                        {
                            getCmd.Parameters.AddWithValue("@messageId", messageId);
                            using (var reader = getCmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    var message = new
                                    {
                                        messageId = reader.GetInt32(reader.GetOrdinal("СообщениеID")),
                                        senderId = reader.GetInt32(reader.GetOrdinal("ОтправительID")),
                                        receiverId = reader.GetInt32(reader.GetOrdinal("ПолучательID")),
                                        text = request.text, // Используем оригинальный текст, т.к. он у нас есть
                                        timestamp = reader.GetDateTime(reader.GetOrdinal("ДатаОтправки")),
                                        isRead = reader.GetBoolean(reader.GetOrdinal("Прочитано"))
                                    };
                                    
                                    return Ok(new { success = true, message });
                                }
                            }
                        }
                        
                        return Ok(new { success = true, messageId });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("conversation/{userId}/{contactId}")]
        public IActionResult GetConversation(int userId, int contactId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT m.СообщениеID, m.ОтправительID, m.ПолучательID, m.Текст, m.ДатаОтправки, m.Прочитано 
                        FROM Сообщения m 
                        WHERE (m.ОтправительID = @userId AND m.ПолучательID = @contactId AND m.СкрытоОтправителем = 0)
                        OR (m.ОтправительID = @contactId AND m.ПолучательID = @userId AND m.СкрытоПолучателем = 0)
                        ORDER BY m.ДатаОтправки ASC";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@contactId", contactId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            var messages = new List<object>();
                            while (reader.Read())
                            {
                                // Получаем зашифрованный текст и расшифровываем его
                                byte[] encryptedBytes = (byte[])reader["Текст"];
                                string encryptedBase64 = Convert.ToBase64String(encryptedBytes);
                                string decryptedText = EncryptionHelper.DecryptString(encryptedBase64);

                                messages.Add(new
                                {
                                    messageId = reader.GetInt32(reader.GetOrdinal("СообщениеID")),
                                    senderId = reader.GetInt32(reader.GetOrdinal("ОтправительID")),
                                    receiverId = reader.GetInt32(reader.GetOrdinal("ПолучательID")),
                                    text = decryptedText,
                                    timestamp = reader.GetDateTime(reader.GetOrdinal("ДатаОтправки")),
                                    isRead = reader.GetBoolean(reader.GetOrdinal("Прочитано"))
                                });
                            }
                            return Ok(new { success = true, messages });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("read/{userId}/{contactId}")]
        public IActionResult MarkMessagesAsRead(int userId, int contactId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Обновляем статус сообщений "прочитано" для всех сообщений от контакта к текущему пользователю
                    string sql = @"
                        UPDATE Сообщения 
                        SET Прочитано = 1
                        WHERE ОтправительID = @contactId AND ПолучательID = @userId AND Прочитано = 0";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@contactId", contactId);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        return Ok(new { success = true, messagesMarkedAsRead = rowsAffected });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("delete-conversation/{userId}/{contactId}")]
        public IActionResult DeleteConversation(int userId, int contactId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Удаляем связанные файлы из переписки
                    string deleteFilesSQL = @"
                        DELETE FROM МедиаФайлы 
                        WHERE СообщениеID IN (
                            SELECT СообщениеID 
                            FROM Сообщения 
                            WHERE (ОтправительID = @userId AND ПолучательID = @contactId) 
                                OR (ОтправительID = @contactId AND ПолучательID = @userId)
                        )";
                        
                    using (var fileCommand = new SqlCommand(deleteFilesSQL, conn))
                    {
                        fileCommand.Parameters.AddWithValue("@userId", userId);
                        fileCommand.Parameters.AddWithValue("@contactId", contactId);
                        
                        fileCommand.ExecuteNonQuery();
                    }
                    
                    // Удаляем все сообщения в переписке
                    string sql = @"
                        DELETE FROM Сообщения
                        WHERE (ОтправительID = @userId AND ПолучательID = @contactId)
                           OR (ОтправительID = @contactId AND ПолучательID = @userId)";

                    using (var command = new SqlCommand(sql, conn))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@contactId", contactId);

                        int rowsAffected = command.ExecuteNonQuery();
                        return Ok(new { success = true, messagesDeleted = rowsAffected });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("delete-for-me/{messageId}")]
        public IActionResult DeleteMessageForMe(int messageId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Сначала определяем, является ли пользователь отправителем или получателем
                    string checkSql = "SELECT ОтправительID, ПолучательID FROM Сообщения WHERE СообщениеID = @messageId";
                    
                    int senderId = 0;
                    int receiverId = 0;
                    
                    using (var checkCmd = new SqlCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@messageId", messageId);
                        
                        using (var reader = checkCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                senderId = reader.GetInt32(reader.GetOrdinal("ОтправительID"));
                                receiverId = reader.GetInt32(reader.GetOrdinal("ПолучательID"));
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "Сообщение не найдено" });
                            }
                        }
                    }
                    
                    // Обновляем соответствующее поле в зависимости от роли пользователя
                    string sql = "";
                    
                    if (Request.Headers.TryGetValue("UserId", out var userIdValue) && 
                        int.TryParse(userIdValue, out int userId))
                    {
                        if (userId == senderId)
                        {
                            sql = "UPDATE Сообщения SET СкрытоОтправителем = 1 WHERE СообщениеID = @messageId";
                        }
                        else if (userId == receiverId)
                        {
                            sql = "UPDATE Сообщения SET СкрытоПолучателем = 1 WHERE СообщениеID = @messageId";
                        }
                        else
                        {
                            return Forbid();
                        }
                    }
                    else
                    {
                        return BadRequest(new { success = false, message = "Не указан ID пользователя" });
                    }
                    
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@messageId", messageId);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        return Ok(new { success = true, messageDeleted = rowsAffected > 0 });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("delete-for-all/{messageId}")]
        public IActionResult DeleteMessageForAll(int messageId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    
                    // Проверяем, является ли текущий пользователь отправителем
                    string checkSql = "SELECT ОтправительID FROM Сообщения WHERE СообщениеID = @messageId";
                    
                    int senderId = 0;
                    
                    using (var checkCmd = new SqlCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@messageId", messageId);
                        var result = checkCmd.ExecuteScalar();
                        
                        if (result != null)
                        {
                            senderId = Convert.ToInt32(result);
                        }
                        else
                        {
                            return NotFound(new { success = false, message = "Сообщение не найдено" });
                        }
                    }
                    
                    // Проверяем, имеет ли пользователь право удалять сообщение для всех
                    if (Request.Headers.TryGetValue("UserId", out var userIdValue) && 
                        int.TryParse(userIdValue, out int userId) && 
                        userId == senderId)
                    {
                        string sql = @"
                            UPDATE Сообщения
                            SET СкрытоОтправителем = 1, СкрытоПолучателем = 1
                            WHERE СообщениеID = @messageId";
                        
                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@messageId", messageId);
                            int rowsAffected = cmd.ExecuteNonQuery();
                            
                            return Ok(new { success = true, messageDeleted = rowsAffected > 0 });
                        }
                    }
                    else
                    {
                        return Forbid();
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    public class SendMessageRequest
    {
        public int senderId { get; set; }
        public int receiverId { get; set; }
        public string text { get; set; }
    }
} 