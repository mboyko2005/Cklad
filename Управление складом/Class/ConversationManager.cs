using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro.IconPacks;

namespace УправлениеСкладом.Class
{
    /// <summary>
    /// Класс для управления перепиской
    /// </summary>
    public class ConversationManager
    {
        private readonly string connectionString;
        private readonly int currentUserId;
        private readonly Dictionary<int, FrameworkElement> messageElements;
        private readonly StackPanel messagesPanel;
        
        /// <summary>
        /// Конструктор класса
        /// </summary>
        public ConversationManager(string connectionString, int currentUserId, StackPanel messagesPanel, Dictionary<int, FrameworkElement> messageElements)
        {
            this.connectionString = connectionString;
            this.currentUserId = currentUserId;
            this.messagesPanel = messagesPanel;
            this.messageElements = messageElements;
        }
        
        /// <summary>
        /// Загрузка переписки
        /// </summary>
        public void LoadConversation(int contactId)
        {
            try
            {
                // Очищаем панель сообщений и коллекцию элементов
                messagesPanel.Children.Clear();
                messageElements.Clear();
                
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    
                    // Получаем список ID сообщений с вложениями
                    HashSet<int> messagesWithAttachments = new HashSet<int>();
                    string attachmentSql = @"
                        SELECT DISTINCT СообщениеID
                        FROM МедиаФайлы
                        WHERE СообщениеID IN (
                            SELECT СообщениеID
                            FROM Сообщения
                            WHERE ((ОтправительID = @currentUserId AND ПолучательID = @contactId AND СкрытоОтправителем = 0)
                               OR (ОтправительID = @contactId AND ПолучательID = @currentUserId AND СкрытоПолучателем = 0))
                        )";
                    
                    using (SqlCommand attachCommand = new SqlCommand(attachmentSql, connection))
                    {
                        attachCommand.Parameters.AddWithValue("@currentUserId", currentUserId);
                        attachCommand.Parameters.AddWithValue("@contactId", contactId);
                        
                        using (SqlDataReader attachReader = attachCommand.ExecuteReader())
                        {
                            while (attachReader.Read())
                            {
                                messagesWithAttachments.Add(attachReader.GetInt32(0));
                            }
                        }
                    }
                    
                    string sql = @"
                        SELECT СообщениеID, ОтправительID, Текст, ДатаОтправки, Отредактировано, Прочитано
                        FROM Сообщения
                        WHERE ((ОтправительID = @currentUserId AND ПолучательID = @contactId AND СкрытоОтправителем = 0)
                           OR (ОтправительID = @contactId AND ПолучательID = @currentUserId AND СкрытоПолучателем = 0))
                        ORDER BY ДатаОтправки ASC";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@currentUserId", currentUserId);
                        command.Parameters.AddWithValue("@contactId", contactId);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            int prevSenderId = -1;
                            DateTime? lastDateSeparator = null;
                            
                            while (reader.Read())
                            {
                                int messageId = reader.GetInt32(0);
                                int senderId = reader.GetInt32(1);
                                byte[] encryptedBytes = (byte[])reader["Текст"];
                                string encryptedBase64 = Convert.ToBase64String(encryptedBytes);
                                string decryptedText = EncryptionHelper.DecryptString(encryptedBase64);
                                DateTime messageDate = reader.GetDateTime(3);
                                bool isEdited = reader.GetBoolean(4);
                                bool isRead = reader.GetBoolean(5);
                                bool isOutgoing = (senderId == currentUserId);
                                
                                // Добавляем разделитель даты только если это первое сообщение за день
                                if (lastDateSeparator == null || lastDateSeparator.Value.Date != messageDate.Date)
                                {
                                    var dateSeparator = UIMessageFactory.CreateDateSeparator(messageDate);
                                    messagesPanel.Children.Add(dateSeparator);
                                    lastDateSeparator = messageDate;
                                }
                                
                                // Проверяем, есть ли у сообщения вложение
                                if (messagesWithAttachments.Contains(messageId))
                                {
                                    // Загружаем вложение асинхронно
                                    MessageManager messageManager = new MessageManager(connectionString, currentUserId, "");
                                    LoadMessageWithAttachmentDirectly(messageId, senderId, decryptedText, messageDate, isOutgoing, isEdited, isRead, prevSenderId != senderId, messageManager);
                                }
                                else
                                {
                                    // Создаем обычное сообщение без вложения
                                    StackPanel messageBubble = UIMessageFactory.CreateMessageBubble(
                                        messageId, senderId, decryptedText, messageDate, isOutgoing, isEdited, isRead, prevSenderId != senderId, currentUserId);
                                    
                                    // Сохраняем дату в тэге для сортировки
                                    messageBubble.Tag = messageDate;
                                    
                                    // Добавляем сообщение на панель
                                    messagesPanel.Children.Add(messageBubble);
                                    
                                    // Сохраняем ссылку на элемент
                                    messageElements[messageId] = messageBubble;
                                }
                                
                                prevSenderId = senderId;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки сообщений: " + ex.Message);
            }
        }
        
        /// <summary>
        /// Асинхронная загрузка сообщения с вложением напрямую на панель
        /// </summary>
        private async void LoadMessageWithAttachmentDirectly(int messageId, int senderId, string text, DateTime messageTime, bool isOutgoing, bool isEdited, bool isRead, bool showAvatar, MessageManager messageManager)
        {
            try
            {
                // Получаем вложение
                var attachment = await messageManager.GetMessageAttachmentAsync(messageId);
                if (attachment != null)
                {
                    // Создаем сообщение с вложением
                    var messageBubble = UIMessageFactory.CreateMessageWithAttachmentBubble(
                        messageId, senderId, text, messageTime, isOutgoing, isEdited, isRead, showAvatar, currentUserId, attachment);
                    
                    // ВАЖНО: Сохраняем дату в теге для правильной сортировки и определения положения
                    messageBubble.Tag = messageTime;
                    
                    // Сохраняем в переменные для использования в Dispatcher
                    var bubbleToAdd = messageBubble;
                    var msgId = messageId;
                    
                    // Добавляем в панель и сохраняем ссылку напрямую - СИНХРОННО с UI потоком,
                    // чтобы сохранить правильный порядок сообщений
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Найдем правильную позицию для вставки сообщения по дате
                        int insertIndex = FindMessageInsertPosition(messageTime);
                        
                        if (insertIndex >= 0 && insertIndex < messagesPanel.Children.Count)
                        {
                            messagesPanel.Children.Insert(insertIndex, bubbleToAdd);
                        }
                        else
                        {
                            // Если не нашли подходящую позицию, добавляем в конец
                            messagesPanel.Children.Add(bubbleToAdd);
                        }
                        
                        messageElements[msgId] = bubbleToAdd;
                    });
                }
                else
                {
                    // Если вложение не удалось загрузить, создаем обычное сообщение
                    var messageBubble = UIMessageFactory.CreateMessageBubble(
                        messageId, senderId, text, messageTime, isOutgoing, isEdited, isRead, showAvatar, currentUserId);
                    
                    // ВАЖНО: Сохраняем дату в теге для правильной сортировки
                    messageBubble.Tag = messageTime;
                    
                    // Добавляем в панель и сохраняем ссылку
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Найдем правильную позицию для вставки сообщения по дате
                        int insertIndex = FindMessageInsertPosition(messageTime);
                        
                        if (insertIndex >= 0 && insertIndex < messagesPanel.Children.Count)
                        {
                            messagesPanel.Children.Insert(insertIndex, messageBubble);
                        }
                        else
                        {
                            // Если не нашли подходящую позицию, добавляем в конец
                            messagesPanel.Children.Add(messageBubble);
                        }
                        
                        messageElements[messageId] = messageBubble;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки вложения: {ex.Message}");
                
                // Создаем обычное сообщение в случае ошибки
                var messageBubble = UIMessageFactory.CreateMessageBubble(
                    messageId, senderId, text, messageTime, isOutgoing, isEdited, isRead, showAvatar, currentUserId);
                
                // ВАЖНО: Сохраняем дату в теге для правильной сортировки
                messageBubble.Tag = messageTime;
                
                // Добавляем в панель и сохраняем ссылку
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Найдем правильную позицию для вставки сообщения по дате
                    int insertIndex = FindMessageInsertPosition(messageTime);
                    
                    if (insertIndex >= 0 && insertIndex < messagesPanel.Children.Count)
                    {
                        messagesPanel.Children.Insert(insertIndex, messageBubble);
                    }
                    else
                    {
                        // Если не нашли подходящую позицию, добавляем в конец
                        messagesPanel.Children.Add(messageBubble);
                    }
                    
                    messageElements[messageId] = messageBubble;
                });
            }
        }
        
        /// <summary>
        /// Загрузка переписки без повторной анимации для существующих сообщений
        /// </summary>
        public void LoadConversationWithoutAnimation(int contactId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    
                    // Получаем список ID сообщений с вложениями
                    HashSet<int> messagesWithAttachments = new HashSet<int>();
                    string attachmentSql = @"
                        SELECT DISTINCT СообщениеID
                        FROM МедиаФайлы
                        WHERE СообщениеID IN (
                            SELECT СообщениеID
                            FROM Сообщения
                            WHERE ((ОтправительID = @currentUserId AND ПолучательID = @contactId AND СкрытоОтправителем = 0)
                               OR (ОтправительID = @contactId AND ПолучательID = @currentUserId AND СкрытоПолучателем = 0))
                        )";
                    
                    using (SqlCommand attachCommand = new SqlCommand(attachmentSql, connection))
                    {
                        attachCommand.Parameters.AddWithValue("@currentUserId", currentUserId);
                        attachCommand.Parameters.AddWithValue("@contactId", contactId);
                        
                        using (SqlDataReader attachReader = attachCommand.ExecuteReader())
                        {
                            while (attachReader.Read())
                            {
                                messagesWithAttachments.Add(attachReader.GetInt32(0));
                            }
                        }
                    }
                    
                    string sql = @"
                        SELECT СообщениеID, ОтправительID, Текст, ДатаОтправки, Отредактировано, Прочитано
                        FROM Сообщения
                        WHERE ((ОтправительID = @currentUserId AND ПолучательID = @contactId AND СкрытоОтправителем = 0)
                           OR (ОтправительID = @contactId AND ПолучательID = @currentUserId AND СкрытоПолучателем = 0))
                        ORDER BY ДатаОтправки ASC";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@currentUserId", currentUserId);
                        command.Parameters.AddWithValue("@contactId", contactId);

                        // Создаем множество существующих и новых ID для определения изменений
                        HashSet<int> existingIds = new HashSet<int>(messageElements.Keys);
                        HashSet<int> currentIds = new HashSet<int>();
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            int prevSenderId = -1;
                            DateTime prevDate = DateTime.MinValue;
                            DateTime currentDate;
                            
                            // Собираем сообщения
                            while (reader.Read())
                            {
                                int messageId = reader.GetInt32(0);
                                currentIds.Add(messageId);
                                
                                // Если сообщение уже есть в интерфейсе, пропускаем его
                                if (messageElements.ContainsKey(messageId))
                                {
                                    continue; // Пропускаем существующие сообщения
                                }
                                
                                // Иначе это новое сообщение, которое нужно добавить
                                int senderId = reader.GetInt32(1);
                                byte[] encryptedBytes = (byte[])reader["Текст"];
                                string encryptedBase64 = Convert.ToBase64String(encryptedBytes);
                                string decryptedText = EncryptionHelper.DecryptString(encryptedBase64);
                                DateTime messageDate = reader.GetDateTime(3);
                                currentDate = messageDate.Date;
                                bool isEdited = reader.GetBoolean(4);
                                bool isRead = reader.GetBoolean(5);
                                bool isOutgoing = (senderId == currentUserId);
                                
                                // Проверяем, нужно ли добавить разделитель даты
                                bool needDateSeparator = true;
                                
                                // Проверяем, есть ли уже разделитель с такой датой
                                foreach (UIElement element in messagesPanel.Children)
                                {
                                    if (element is Border border && border.Tag != null && border.Tag is string tagString)
                                    {
                                        // Если разделитель с такой датой уже есть, не добавляем новый
                                        if (tagString == currentDate.ToString("yyyy-MM-dd"))
                                        {
                                            needDateSeparator = false;
                                            break;
                                        }
                                    }
                                }
                                
                                // Если нужен разделитель даты и это первое сообщение за новую дату
                                if (needDateSeparator && prevDate != currentDate)
                                {
                                    FrameworkElement dateSeparator = UIMessageFactory.CreateDateSeparator(messageDate);
                                    
                                    // Найдем позицию для вставки разделителя
                                    int separatorIndex = FindMessageInsertPosition(messageDate);
                                    if (separatorIndex >= 0 && separatorIndex < messagesPanel.Children.Count)
                                    {
                                        messagesPanel.Children.Insert(separatorIndex, dateSeparator);
                                    }
                                    else
                                    {
                                        messagesPanel.Children.Add(dateSeparator);
                                    }
                                    
                                    prevDate = currentDate;
                                }
                                
                                // Проверяем, есть ли у сообщения вложение
                                if (messagesWithAttachments.Contains(messageId))
                                {
                                    // Загружаем вложение асинхронно
                                    MessageManager messageManager = new MessageManager(connectionString, currentUserId, "");
                                    LoadMessageWithAttachmentDirectly(messageId, senderId, decryptedText, messageDate, isOutgoing, isEdited, isRead, prevSenderId != senderId, messageManager);
                                }
                                else
                                {
                                    // Создаем сообщение
                                    StackPanel messageBubble = UIMessageFactory.CreateMessageBubble(
                                        messageId, senderId, decryptedText, messageDate, isOutgoing, isEdited, isRead, prevSenderId != senderId, currentUserId);
                                    
                                    // Сохраняем дату в тэге для сортировки
                                    messageBubble.Tag = messageDate;
                                    
                                    // Найдем позицию для вставки
                                    int insertIndex = FindMessageInsertPosition(messageDate);
                                    if (insertIndex >= 0 && insertIndex < messagesPanel.Children.Count)
                                    {
                                        messagesPanel.Children.Insert(insertIndex, messageBubble);
                                    }
                                    else
                                    {
                                        messagesPanel.Children.Add(messageBubble);
                                    }
                                    
                                    // Сохраняем ссылку на элемент
                                    messageElements[messageId] = messageBubble;
                                }
                                
                                prevSenderId = senderId;
                            }
                        }
                        
                        // Удаляем сообщения, которых больше нет в базе
                        var messagesToRemove = existingIds.Except(currentIds).ToList();
                        foreach (int messageId in messagesToRemove)
                        {
                            if (messageElements.TryGetValue(messageId, out var element))
                            {
                                messagesPanel.Children.Remove(element);
                                messageElements.Remove(messageId);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки сообщений: " + ex.Message);
            }
        }
        
        /// <summary>
        /// Обновление статуса сообщения (прочитано/не прочитано)
        /// </summary>
        private void UpdateMessageStatus(int messageId, bool isRead)
        {
            if (messageElements.TryGetValue(messageId, out var element))
            {
                if (element is StackPanel panel)
                {
                    // Находим иконку статуса сообщения в панели метаданных
                    var childElements = UIMessageFactory.FindAllChildren<PackIconMaterial>(panel);
                    PackIconMaterial statusIcon = childElements.FirstOrDefault(x => 
                        x.Kind == PackIconMaterialKind.Check || 
                        x.Kind == PackIconMaterialKind.CheckAll);
                    
                    if (statusIcon != null)
                    {
                        // Обновляем иконку в зависимости от статуса
                        statusIcon.Kind = isRead ? PackIconMaterialKind.CheckAll : PackIconMaterialKind.Check;
                        statusIcon.Foreground = isRead ? 
                            new SolidColorBrush(Color.FromRgb(79, 195, 247)) : // Голубой если прочитано
                            new SolidColorBrush(Color.FromRgb(189, 189, 189));  // Серый если не прочитано
                        
                        // Добавляем небольшую анимацию для привлечения внимания к изменению
                        AnimationHelper.PulseAnimation(statusIcon);
                    }
                }
            }
        }
        
        /// <summary>
        /// Отметка сообщений как прочитанных
        /// </summary>
        public void MarkMessagesAsRead(int contactId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    
                    // Получаем список непрочитанных сообщений перед обновлением
                    List<int> unreadMessageIds = new List<int>();
                    string selectSql = @"
                        SELECT СообщениеID
                        FROM Сообщения
                        WHERE ОтправительID = @contactId
                          AND ПолучательID = @currentUserId
                          AND Прочитано = 0";
                    
                    using (SqlCommand selectCmd = new SqlCommand(selectSql, connection))
                    {
                        selectCmd.Parameters.AddWithValue("@contactId", contactId);
                        selectCmd.Parameters.AddWithValue("@currentUserId", currentUserId);
                        
                        using (SqlDataReader reader = selectCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                unreadMessageIds.Add(reader.GetInt32(0));
                            }
                        }
                    }
                    
                    // Обновляем статус в базе данных
                    string updateSql = @"
                        UPDATE Сообщения
                        SET Прочитано = 1
                        WHERE ОтправительID = @contactId
                          AND ПолучательID = @currentUserId
                          AND Прочитано = 0";
                    using (SqlCommand updateCmd = new SqlCommand(updateSql, connection))
                    {
                        updateCmd.Parameters.AddWithValue("@contactId", contactId);
                        updateCmd.Parameters.AddWithValue("@currentUserId", currentUserId);
                        updateCmd.ExecuteNonQuery();
                    }
                    
                    // Визуально обновляем статус сообщений
                    foreach (int messageId in unreadMessageIds)
                    {
                        UpdateMessageStatus(messageId, true);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обновлении статуса сообщений: " + ex.Message);
            }
        }

        /// <summary>
        /// Загрузить сообщения для выбранного контакта
        /// </summary>
        public async Task LoadMessagesAsync(int contactId, int currentUserId)
        {
            try
            {
                // Очищаем панель сообщений и словарь элементов
                messagesPanel.Children.Clear();
                messageElements.Clear();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = @"
                        SELECT m.СообщениеID, m.ОтправительID, m.ПолучательID, m.Текст, m.ДатаОтправки, m.Прочитано
                        FROM Сообщения m
                        WHERE ((m.ОтправительID = @currentUserId AND m.ПолучательID = @contactId AND m.СкрытоОтправителем = 0)
                            OR (m.ОтправительID = @contactId AND m.ПолучательID = @currentUserId AND m.СкрытоПолучателем = 0))
                        ORDER BY m.ДатаОтправки ASC";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@currentUserId", currentUserId);
                        command.Parameters.AddWithValue("@contactId", contactId);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            DateTime? lastMessageDate = null;
                            
                            while (await reader.ReadAsync())
                            {
                                int messageId = reader.GetInt32(0);
                                int senderId = reader.GetInt32(1);
                                // int recipientId = reader.GetInt32(2);
                                byte[] encryptedText = (byte[])reader.GetValue(3);
                                DateTime messageTime = reader.GetDateTime(4);
                                bool isRead = reader.GetBoolean(5);

                                // Проверяем, нужно ли добавлять разделитель даты
                                if (lastMessageDate == null || lastMessageDate.Value.Date != messageTime.Date)
                                {
                                    // Добавляем разделитель с датой
                                    var dateSeparator = UIMessageFactory.CreateDateSeparator(messageTime.Date);
                                    messagesPanel.Children.Add(dateSeparator);
                                    
                                    // Обновляем последнюю дату
                                    lastMessageDate = messageTime.Date;
                                }

                                // Дешифруем текст сообщения
                                string encryptedBase64 = Convert.ToBase64String(encryptedText);
                                string decryptedText = EncryptionHelper.DecryptString(encryptedBase64);

                                bool isOutgoing = (senderId == currentUserId);
                                bool isEdited = false; // Заглушка, т.к. нет поля "Редактировано"

                                // Проверяем, есть ли вложение
                                bool hasAttachment = await HasAttachmentAsync(messageId, connection);
                                
                                if (hasAttachment)
                                {
                                    // Получаем вложение
                                    MessageAttachment attachment = await GetAttachmentAsync(messageId, connection);
                                    
                                    if (attachment != null)
                                    {
                                        // Создаем элемент сообщения с вложением
                                        var messageElement = UIMessageFactory.CreateMessageWithAttachmentBubble(
                                            messageId, 
                                            senderId, 
                                            decryptedText, 
                                            messageTime, 
                                            isOutgoing, 
                                            isEdited, 
                                            isRead, 
                                            true, 
                                            currentUserId, 
                                            attachment);
                                            
                                        // Добавляем сообщение в UI
                                        messagesPanel.Children.Add(messageElement);
                                        
                                        // Добавляем в словарь
                                        messageElements[messageId] = messageElement;
                                    }
                                }
                                else
                                {
                                    // Создаем элемент обычного текстового сообщения
                                    var messageElement = UIMessageFactory.CreateMessageBubble(
                                        messageId, 
                                        senderId, 
                                        decryptedText, 
                                        messageTime, 
                                        isOutgoing, 
                                        isEdited, 
                                        isRead, 
                                        true, 
                                        currentUserId);
                                        
                                    // Добавляем сообщение в UI
                                    messagesPanel.Children.Add(messageElement);
                                    
                                    // Добавляем в словарь
                                    messageElements[messageId] = messageElement;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке сообщений: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверяет, есть ли вложение у сообщения
        /// </summary>
        private async Task<bool> HasAttachmentAsync(int messageId, SqlConnection connection)
        {
            try
            {
                string sql = @"
                    SELECT COUNT(*) 
                    FROM МедиаФайлы 
                    WHERE СообщениеID = @messageId";
                
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@messageId", messageId);
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        /// <summary>
        /// Получает вложение сообщения
        /// </summary>
        private async Task<MessageAttachment> GetAttachmentAsync(int messageId, SqlConnection connection)
        {
            try
            {
                string sql = @"
                    SELECT ФайлID, Тип, Файл
                    FROM МедиаФайлы
                    WHERE СообщениеID = @messageId";
                
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@messageId", messageId);
                    
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            AttachmentType type = AttachmentType.Other;
                            if (Enum.TryParse<AttachmentType>(reader.GetString(1), out AttachmentType parsedType))
                            {
                                type = parsedType;
                            }
                            
                            MessageAttachment attachment = new MessageAttachment
                            {
                                AttachmentId = reader.GetInt32(0),
                                MessageId = messageId,
                                Type = type,
                                FileName = $"file_{messageId}{GetFileExtensionByType(type)}",
                                Content = (byte[])reader.GetValue(2)
                            };
                            
                            // Для изображений создаем миниатюру
                            if (attachment.Type == AttachmentType.Image)
                            {
                                attachment.CreateThumbnail();
                            }
                            
                            return attachment;
                        }
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении вложения: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Возвращает расширение файла в зависимости от типа
        /// </summary>
        private string GetFileExtensionByType(AttachmentType type)
        {
            switch (type)
            {
                case AttachmentType.Image:
                    return ".jpg";
                case AttachmentType.Document:
                    return ".pdf";
                case AttachmentType.Audio:
                    return ".mp3";
                case AttachmentType.Video:
                    return ".mp4";
                default:
                    return ".bin";
            }
        }

        // Улучшенный метод для определения позиции вставки сообщения
        private int FindMessageInsertPosition(DateTime messageTime)
        {
            // Проверка пограничных случаев
            if (messagesPanel.Children.Count == 0)
                return 0;
            
            // Если сообщение новее последнего в списке, добавляем в конец
            UIElement lastElement = messagesPanel.Children[messagesPanel.Children.Count - 1];
            DateTime? lastElementDate = GetMessageDateTime(lastElement);
            if (lastElementDate.HasValue && messageTime > lastElementDate.Value)
                return messagesPanel.Children.Count;
            
            // Проходим по списку элементов и ищем правильную позицию на основе временной метки
            for (int i = 0; i < messagesPanel.Children.Count; i++)
            {
                UIElement element = messagesPanel.Children[i];
                DateTime? elementDate = GetMessageDateTime(element);
                
                if (elementDate.HasValue && messageTime < elementDate.Value)
                {
                    // Нашли сообщение, которое новее текущего - вставляем перед ним
                    return i;
                }
            }
            
            // По умолчанию добавляем в конец
            return messagesPanel.Children.Count;
        }

        // Получение даты из элемента сообщения или разделителя
        private DateTime? GetMessageDateTime(UIElement element)
        {
            if (element is StackPanel panel)
            {
                // Проверяем, есть ли у элемента Tag с датой/временем
                var tagValue = panel.Tag;
                if (tagValue is DateTime dateTime)
                {
                    return dateTime;
                }
                
                // Если элемент это разделитель даты
                if (panel.Tag is string tagString && DateTime.TryParse(tagString, out DateTime dateValue))
                {
                    return dateValue;
                }
            }
            else if (element is Border border)
            {
                // Проверяем, является ли это датой-разделителем
                if (border.Tag is string dateTag && DateTime.TryParse(dateTag, out DateTime dateValue))
                {
                    return dateValue;
                }
            }
            
            return null;
        }
    }
} 