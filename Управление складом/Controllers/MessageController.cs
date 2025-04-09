using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using System.Text;

namespace УправлениеСкладом.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly string _connectionString =
            @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True;TrustServerCertificate=True";

        // Размер чанка по умолчанию (2 МБ)
        private const int DEFAULT_CHUNK_SIZE = 2 * 1024 * 1024;
        
        // Максимальный размер файла (1.5 ГБ)
        private const long MAX_FILE_SIZE = 1536L * 1024 * 1024;
        
        // Директория для временного хранения чанков
        private readonly string _tempChunksDir = Path.Combine(Path.GetTempPath(), "MessageMediaChunks");

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
                                    isRead = reader.GetBoolean(reader.GetOrdinal("Прочитано")),
                                    hasAttachment = CheckMessageHasAttachment(reader.GetInt32(reader.GetOrdinal("СообщениеID")))
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

        // Проверяет, имеет ли сообщение вложения
        private bool CheckMessageHasAttachment(int messageId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT COUNT(*) FROM МедиаФайлы WHERE СообщениеID = @messageId";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@messageId", messageId);
                        int count = (int)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        // Получение вложений для сообщения
        [HttpGet("media/{messageId}")]
        public IActionResult GetMessageMedia(int messageId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT ФайлID, Тип, Файл 
                        FROM МедиаФайлы 
                        WHERE СообщениеID = @messageId";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@messageId", messageId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                byte[] fileData = (byte[])reader["Файл"];
                                string fileType = reader.GetString(reader.GetOrdinal("Тип"));
                                
                                // Улучшенная настройка кэширования для предотвращения повторной загрузки файлов
                                Response.Headers.Add("Cache-Control", "public, max-age=31536000, immutable"); // Год + immutable флаг
                                Response.Headers.Add("Expires", DateTime.UtcNow.AddYears(1).ToString("R"));
                                Response.Headers.Add("ETag", $"W/\"{messageId}-{DateTime.UtcNow.Ticks}\"");
                                Response.Headers.Add("Pragma", "public");
                                
                                // Определяем тип контента в зависимости от типа файла и его содержимого
                                string contentType = GetContentType(fileType);
                                
                                // Для изображений дополнительно проверяем заголовок файла
                                if (fileType.ToLower() == "image" && fileData.Length > 4)
                                {
                                    string detectedContentType = DetectImageContentType(fileData);
                                    if (!string.IsNullOrEmpty(detectedContentType))
                                    {
                                        contentType = detectedContentType;
                                    }
                                }
                                
                                // Возвращаем файл напрямую, без расшифровки
                                return File(fileData, contentType);
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "Медиафайл не найден" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении медиафайла: {ex.Message}");
                Console.WriteLine($"Трассировка стека: {ex.StackTrace}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Расширяем API для поддержки частичной загрузки (range requests)
        [HttpGet("media/{messageId}/stream")]
        public IActionResult GetMessageMediaStream(int messageId)
        {
            try
            {
                if (!Request.Headers.ContainsKey("Range"))
                {
                    // Если нет заголовка Range, перенаправляем на обычный метод
                    return RedirectToAction(nameof(GetMessageMedia), new { messageId });
                }

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT ФайлID, Тип, Файл, 
                        (SELECT DATALENGTH(Файл) FROM МедиаФайлы WHERE СообщениеID = @messageId) as FileSize
                        FROM МедиаФайлы 
                        WHERE СообщениеID = @messageId";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@messageId", messageId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                byte[] fileData = (byte[])reader["Файл"];
                                string fileType = reader.GetString(reader.GetOrdinal("Тип"));
                                long fileSize = Convert.ToInt64(reader["FileSize"]);
                                
                                // Парсим заголовок Range
                                var rangeHeader = Request.Headers["Range"].ToString();
                                var range = GetRangeFromHeader(rangeHeader, fileSize);
                                
                                // Улучшенная настройка кэширования
                                Response.Headers.Add("Cache-Control", "public, max-age=31536000, immutable"); // Год + immutable флаг
                                Response.Headers.Add("Expires", DateTime.UtcNow.AddYears(1).ToString("R"));
                                Response.Headers.Add("ETag", $"W/\"{messageId}-{DateTime.UtcNow.Ticks}\"");
                                Response.Headers.Add("Pragma", "public");
                                Response.Headers.Add("Accept-Ranges", "bytes");
                                
                                // Определяем тип контента
                                string contentType = GetContentType(fileType);
                                if (fileType.ToLower() == "image" && fileData.Length > 4)
                                {
                                    string detectedContentType = DetectImageContentType(fileData);
                                    if (!string.IsNullOrEmpty(detectedContentType))
                                    {
                                        contentType = detectedContentType;
                                    }
                                }
                                
                                // Отправляем частичное содержимое
                                Response.StatusCode = 206; // Partial Content
                                Response.Headers.Add("Content-Range", $"bytes {range.Start}-{range.End}/{fileSize}");
                                
                                // Вырезаем нужный чанк из файла
                                var length = range.End - range.Start + 1;
                                var buffer = new byte[length];
                                Array.Copy(fileData, range.Start, buffer, 0, length);
                                
                                return File(buffer, contentType);
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "Медиафайл не найден" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при потоковой передаче медиафайла: {ex.Message}");
                Console.WriteLine($"Трассировка стека: {ex.StackTrace}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        
        // Парсинг заголовка Range
        private (long Start, long End) GetRangeFromHeader(string rangeHeader, long fileSize)
        {
            // По умолчанию: первый чанк размером 1MB
            long start = 0;
            long end = Math.Min(1024 * 1024 - 1, fileSize - 1);
            
            if (!string.IsNullOrEmpty(rangeHeader))
            {
                // range: bytes=0-1023
                var parts = rangeHeader.Replace("bytes=", "").Split('-');
                if (parts.Length == 2)
                {
                    if (long.TryParse(parts[0], out start))
                    {
                        if (long.TryParse(parts[1], out end))
                        {
                            if (end >= fileSize)
                                end = fileSize - 1;
                        }
                        else
                        {
                            end = Math.Min(start + 1024 * 1024 - 1, fileSize - 1);
                        }
                    }
                }
            }
            
            return (start, end);
        }

        // Определяет MIME-тип изображения по его первым байтам
        private string DetectImageContentType(byte[] data)
        {
            if (data == null || data.Length < 4)
                return null;
            
            // JPEG: начинается с FF D8 FF
            if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
                return "image/jpeg";
            
            // PNG: начинается с 89 50 4E 47
            if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                return "image/png";
            
            // GIF: начинается с GIF87a или GIF89a
            if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x38)
                return "image/gif";
            
            // BMP: начинается с BM
            if (data[0] == 0x42 && data[1] == 0x4D)
                return "image/bmp";
            
            // WEBP: содержит WEBP в определенной позиции
            if (data.Length > 12 && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
                return "image/webp";
            
            return "image/jpeg"; // По умолчанию используем JPEG
        }

        // Получение списка медиафайлов для сообщения (без содержимого)
        [HttpGet("media-info/{messageId}")]
        public IActionResult GetMessageMediaInfo(int messageId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT ФайлID, Тип 
                        FROM МедиаФайлы 
                        WHERE СообщениеID = @messageId";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@messageId", messageId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            var mediaFiles = new List<object>();
                            while (reader.Read())
                            {
                                mediaFiles.Add(new
                                {
                                    fileId = reader.GetInt32(reader.GetOrdinal("ФайлID")),
                                    type = reader.GetString(reader.GetOrdinal("Тип"))
                                });
                            }
                            return Ok(new { success = true, mediaFiles });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Добавление медиафайла к сообщению напрямую (без чанков)
        [HttpPost("upload-media")]
        public async Task<IActionResult> UploadMedia(IFormFile file, [FromForm] int messageId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Файл отсутствует" });
                }
                
                Console.WriteLine($"Загрузка файла для сообщения {messageId}, размер: {file.Length / 1024} КБ");
                
                // Проверяем авторизацию
                if (!Request.Headers.TryGetValue("UserId", out var userIdHeader) || 
                    !int.TryParse(userIdHeader, out int userId))
                {
                    return Unauthorized(new { success = false, message = "Требуется авторизация" });
                }
                
                // Проверяем размер файла
                if (file.Length > MAX_FILE_SIZE)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = $"Размер файла превышает максимально допустимый ({MAX_FILE_SIZE / (1024 * 1024)} МБ)" 
                    });
                }

                // Проверяем, существует ли сообщение и принадлежит ли оно текущему пользователю
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string checkSql = "SELECT ОтправительID FROM Сообщения WHERE СообщениеID = @messageId";
                    
                    using (var checkCmd = new SqlCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@messageId", messageId);
                        var result = checkCmd.ExecuteScalar();
                        
                        if (result == null)
                        {
                            return BadRequest(new { success = false, message = "Сообщение не найдено" });
                        }
                        
                        int senderId = Convert.ToInt32(result);
                        if (userId != senderId)
                        {
                            return Forbid();
                        }
                    }
                    
                    // Читаем файл в память
                    byte[] fileBytes;
                    using (var ms = new MemoryStream())
                    {
                        await file.CopyToAsync(ms);
                        fileBytes = ms.ToArray();
                    }
                    
                    // Определяем тип файла по его содержимому
                    string contentType = DetectFileContentType(fileBytes, file.ContentType);
                    string fileType = DetermineFileType(contentType);
                    
                    // Сохраняем файл в базе данных
                    string sql = @"
                        INSERT INTO МедиаФайлы (СообщениеID, Тип, Файл)
                        VALUES (@messageId, @fileType, @fileData);
                        SELECT SCOPE_IDENTITY();";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@messageId", messageId);
                        cmd.Parameters.AddWithValue("@fileType", fileType);
                        cmd.Parameters.AddWithValue("@fileData", fileBytes);

                        var dbFileId = Convert.ToInt32(cmd.ExecuteScalar());
                        Console.WriteLine($"Файл успешно сохранен в базе данных, ID: {dbFileId}");
                        
                        // Возвращаем информацию о файле
                        return Ok(new 
                        { 
                            success = true, 
                            fileId = dbFileId,
                            fileUrl = $"/api/message/media/{messageId}",
                            type = fileType,
                            message = "Файл успешно загружен" 
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке файла: {ex.Message}");
                Console.WriteLine($"Трассировка стека: {ex.StackTrace}");
                return StatusCode(500, new { 
                    success = false, 
                    message = "Ошибка при загрузке файла",
                    error = ex.Message
                });
            }
        }

        // Добавление медиафайла к сообщению через чанки
        [HttpPost("upload-media-chunk")]
        public async Task<IActionResult> UploadMediaChunk(IFormFile file, [FromForm] int messageId, [FromForm] int chunkIndex, [FromForm] int totalChunks, [FromForm] string fileId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Чанк файла отсутствует" });
                }
                
                Console.WriteLine($"Загрузка чанка {chunkIndex + 1} из {totalChunks}. ID файла: {fileId}");

                // Проверяем авторизацию
                if (!Request.Headers.TryGetValue("UserId", out var userIdHeader) || 
                    !int.TryParse(userIdHeader, out int userId))
                {
                    return Unauthorized(new { success = false, message = "Требуется авторизация" });
                }

                // Проверяем, существует ли сообщение и принадлежит ли оно текущему пользователю
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string checkSql = "SELECT ОтправительID FROM Сообщения WHERE СообщениеID = @messageId";
                    
                    using (var checkCmd = new SqlCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@messageId", messageId);
                        var result = checkCmd.ExecuteScalar();
                        
                        if (result == null)
                        {
                            return BadRequest(new { success = false, message = "Сообщение не найдено" });
                        }
                        
                        int senderId = Convert.ToInt32(result);
                        if (userId != senderId)
                        {
                            return Forbid();
                        }
                    }
                }

                // Создаем папку для временного хранения чанков, если она не существует
                var userChunkDir = Path.Combine(_tempChunksDir, userId.ToString(), fileId);
                Directory.CreateDirectory(userChunkDir);
                
                // Сохраняем чанк во временный файл
                var chunkPath = Path.Combine(userChunkDir, $"chunk_{chunkIndex}");
                using (var stream = new FileStream(chunkPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                
                // Проверяем, все ли чанки загружены
                if (chunkIndex == totalChunks - 1)
                {
                    // Собираем все чанки в один файл
                    var completeFilePath = await MergeChunks(userChunkDir, totalChunks);
                    
                    // Определяем размер итогового файла
                    var fileInfo = new FileInfo(completeFilePath);
                    var fileSize = fileInfo.Length;
                    
                    // Проверяем размер файла
                    if (fileSize > MAX_FILE_SIZE)
                    {
                        // Удаляем временные файлы
                        Directory.Delete(userChunkDir, true);
                        return BadRequest(new { 
                            success = false, 
                            message = $"Размер файла превышает максимально допустимый ({MAX_FILE_SIZE / (1024 * 1024)} МБ)" 
                        });
                    }
                    
                    // Читаем собранный файл в память
                    byte[] fileBytes = System.IO.File.ReadAllBytes(completeFilePath);
                    
                    // Определяем тип файла по его содержимому
                    string contentType = DetectFileContentType(fileBytes, file.ContentType);
                    string fileType = DetermineFileType(contentType);
                    
                    // Сохраняем файл в базе данных
                    try
                    {
                        using (var conn = new SqlConnection(_connectionString))
                        {
                            conn.Open();
                            string sql = @"
                                INSERT INTO МедиаФайлы (СообщениеID, Тип, Файл)
                                VALUES (@messageId, @fileType, @fileData);
                                SELECT SCOPE_IDENTITY();";

                            using (var cmd = new SqlCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@messageId", messageId);
                                cmd.Parameters.AddWithValue("@fileType", fileType);
                                cmd.Parameters.AddWithValue("@fileData", fileBytes);

                                var dbFileId = Convert.ToInt32(cmd.ExecuteScalar());
                                Console.WriteLine($"Файл успешно сохранен в базе данных, ID: {dbFileId}");

                                // Удаляем временные файлы
                                Directory.Delete(userChunkDir, true);
                                
                                // Возвращаем информацию о файле
                                return Ok(new 
                                { 
                                    success = true, 
                                    fileId = dbFileId,
                                    fileUrl = $"/api/message/media/{messageId}",
                                    type = fileType,
                                    message = "Файл успешно загружен" 
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при сохранении файла в базе данных: {ex.Message}");
                        // Удаляем временные файлы в случае ошибки
                        Directory.Delete(userChunkDir, true);
                        return StatusCode(500, new { 
                            success = false, 
                            message = "Ошибка при сохранении файла в базе данных",
                            error = ex.Message
                        });
                    }
                }
                
                // Если еще не все чанки загружены, возвращаем промежуточный результат
                return Ok(new 
                { 
                    success = true, 
                    chunkIndex, 
                    totalChunks,
                    message = $"Чанк {chunkIndex + 1} из {totalChunks} успешно загружен" 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка при загрузке чанка: {ex.Message}");
                Console.WriteLine($"Трассировка стека: {ex.StackTrace}");
                return StatusCode(500, new { 
                    success = false, 
                    message = "Ошибка при загрузке чанка",
                    error = ex.Message
                });
            }
        }
        
        // Объединение чанков в один файл
        private async Task<string> MergeChunks(string chunkDir, int totalChunks)
        {
            var outputPath = Path.Combine(chunkDir, "complete_file");
            
            using (var outputStream = new FileStream(outputPath, FileMode.Create))
            {
                for (int i = 0; i < totalChunks; i++)
                {
                    var chunkPath = Path.Combine(chunkDir, $"chunk_{i}");
                    using (var inputStream = new FileStream(chunkPath, FileMode.Open))
                    {
                        await inputStream.CopyToAsync(outputStream);
                    }
                }
            }
            
            return outputPath;
        }
        
        // Определяет тип файла на основе его содержимого и Content-Type
        private string DetectFileContentType(byte[] data, string contentType)
        {
            // Если передан content-type, проверяем его первым
            if (!string.IsNullOrEmpty(contentType) && contentType != "application/octet-stream")
            {
                return contentType;
            }
            
            // Если нет content-type или он generic, определяем по сигнатуре файла
            if (data == null || data.Length < 4)
                return "application/octet-stream";
            
            // JPEG
            if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
                return "image/jpeg";
            
            // PNG
            if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                return "image/png";
            
            // GIF
            if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x38)
                return "image/gif";
            
            // BMP
            if (data[0] == 0x42 && data[1] == 0x4D)
                return "image/bmp";
            
            // WEBP
            if (data.Length > 12 && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
                return "image/webp";
            
            // PDF
            if (data[0] == 0x25 && data[1] == 0x50 && data[2] == 0x44 && data[3] == 0x46)
                return "application/pdf";
            
            // ZIP
            if (data[0] == 0x50 && data[1] == 0x4B && data[2] == 0x03 && data[3] == 0x04)
                return "application/zip";
            
            // MP3
            if (data[0] == 0x49 && data[1] == 0x44 && data[2] == 0x33)
                return "audio/mpeg";
            
            // MP4
            if (data.Length > 8 && (
                (data[4] == 0x66 && data[5] == 0x74 && data[6] == 0x79 && data[7] == 0x70) ||
                (data[0] == 0x00 && data[1] == 0x00 && data[2] == 0x00 && data[3] == 0x18)))
                return "video/mp4";
            
            return "application/octet-stream";
        }

        // Проверка статуса загрузки чанков
        [HttpGet("check-upload-status")]
        public IActionResult CheckUploadStatus([FromQuery] string fileId, [FromQuery] int userId)
        {
            try
            {
                var userChunkDir = Path.Combine(_tempChunksDir, userId.ToString(), fileId);
                
                if (!Directory.Exists(userChunkDir))
                {
                    return NotFound(new { success = false, message = "Загрузка не найдена" });
                }
                
                var files = Directory.GetFiles(userChunkDir, "chunk_*");
                
                return Ok(new
                {
                    success = true,
                    chunksUploaded = files.Length,
                    isComplete = Directory.Exists(Path.Combine(userChunkDir, "complete_file"))
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Отмена загрузки чанков
        [HttpDelete("cancel-upload")]
        public IActionResult CancelUpload([FromQuery] string fileId)
        {
            try
            {
                if (!Request.Headers.TryGetValue("UserId", out var userIdHeader) || 
                    !int.TryParse(userIdHeader, out int userId))
                {
                    return Unauthorized(new { success = false, message = "Требуется авторизация" });
                }
                
                var userChunkDir = Path.Combine(_tempChunksDir, userId.ToString(), fileId);
                
                if (Directory.Exists(userChunkDir))
                {
                    Directory.Delete(userChunkDir, true);
                    return Ok(new { success = true, message = "Загрузка отменена" });
                }
                
                return NotFound(new { success = false, message = "Загрузка не найдена" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Возвращает MIME-тип для типа файла
        private string GetContentType(string fileType)
        {
            switch (fileType.ToLower())
            {
                case "image":
                    return "image/jpeg";
                case "video":
                    return "video/mp4";
                case "audio":
                    return "audio/mpeg";
                case "pdf":
                    return "application/pdf";
                case "doc":
                case "docx":
                    return "application/msword";
                case "xls":
                case "xlsx":
                    return "application/vnd.ms-excel";
                case "txt":
                    return "text/plain";
                default:
                    return "application/octet-stream";
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

        // Определяет тип файла на основе MIME-типа
        private string DetermineFileType(string contentType)
        {
            if (contentType.StartsWith("image/"))
            {
                return "Image";
            }
            else if (contentType.StartsWith("video/"))
            {
                return "Video";
            }
            else if (contentType.StartsWith("audio/"))
            {
                return "Audio";
            }
            else if (contentType.StartsWith("application/pdf"))
            {
                return "PDF";
            }
            else
            {
                return "Document";
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