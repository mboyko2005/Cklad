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
            // Очищаем панель сообщений и словарь элементов
            messagesPanel.Children.Clear();
            messageElements.Clear();
            
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
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
                            DateTime prevDate = DateTime.MinValue;
                            DateTime currentDate;

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
                                
                                // Добавляем разделитель с датой если это первое сообщение или новый день
                                currentDate = messageDate.Date;
                                if (prevDate == DateTime.MinValue || currentDate != prevDate.Date)
                                {
                                    Border dateSeparator = UIMessageFactory.CreateDateSeparator(messageDate);
                                    messagesPanel.Children.Add(dateSeparator);
                                    prevDate = currentDate;
                                }
                                
                                // Создаем контейнер сообщения
                                StackPanel messageBubble = UIMessageFactory.CreateMessageBubble(messageId, senderId, decryptedText, messageDate, isOutgoing, isEdited, isRead, prevSenderId != senderId, currentUserId);
                                
                                // Добавляем в панель и сохраняем ссылку
                                messagesPanel.Children.Add(messageBubble);
                                messageElements[messageId] = messageBubble;
                                
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
        /// Загрузка переписки без повторной анимации для существующих сообщений
        /// </summary>
        public void LoadConversationWithoutAnimation(int contactId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
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
                            
                            // Временный список для новых сообщений
                            List<FrameworkElement> newMessages = new List<FrameworkElement>();
                            Dictionary<int, StackPanel> newMessageElements = new Dictionary<int, StackPanel>();

                            while (reader.Read())
                            {
                                int messageId = reader.GetInt32(0);
                                currentIds.Add(messageId); // Добавляем ID в текущий набор
                                
                                // Если сообщение уже отображается, просто обновляем статус
                                if (messageElements.ContainsKey(messageId))
                                {
                                    bool isReadStatus = reader.GetBoolean(5);
                                    UpdateMessageStatus(messageId, isReadStatus);
                                    continue;
                                }
                                
                                // Если это новое сообщение, создаем его
                                int senderId = reader.GetInt32(1);
                                byte[] encryptedBytes = (byte[])reader["Текст"];
                                string encryptedBase64 = Convert.ToBase64String(encryptedBytes);
                                string decryptedText = EncryptionHelper.DecryptString(encryptedBase64);
                                DateTime messageDate = reader.GetDateTime(3);
                                bool isEdited = reader.GetBoolean(4);
                                bool isRead = reader.GetBoolean(5);
                                bool isOutgoing = (senderId == currentUserId);
                                
                                // Добавляем разделитель с датой если нужно
                                currentDate = messageDate.Date;
                                if (prevDate == DateTime.MinValue || currentDate != prevDate.Date)
                                {
                                    if (newMessages.Count == 0 && messagesPanel.Children.Count > 0)
                                    {
                                        // Проверяем, нужно ли добавлять разделитель даты
                                        bool needDateSeparator = true;
                                        foreach (var child in messagesPanel.Children)
                                        {
                                            if (child is Border border && border.Child is TextBlock textBlock)
                                            {
                                                string dateText;
                                                if (currentDate == DateTime.Today)
                                                    dateText = "Сегодня";
                                                else if (currentDate == DateTime.Today.AddDays(-1))
                                                    dateText = "Вчера";
                                                else
                                                    dateText = currentDate.ToString("d MMMM yyyy");
                                                
                                                if (textBlock.Text == dateText)
                                                {
                                                    needDateSeparator = false;
                                                    break;
                                                }
                                            }
                                        }
                                        
                                        if (needDateSeparator)
                                        {
                                            Border dateSeparator = UIMessageFactory.CreateDateSeparator(messageDate);
                                            newMessages.Add(dateSeparator);
                                        }
                                    }
                                    else
                                    {
                                        Border dateSeparator = UIMessageFactory.CreateDateSeparator(messageDate);
                                        newMessages.Add(dateSeparator);
                                    }
                                    prevDate = currentDate;
                                }
                                
                                // Создаем контейнер сообщения
                                StackPanel messageBubble = UIMessageFactory.CreateMessageBubble(messageId, senderId, decryptedText, messageDate, isOutgoing, isEdited, isRead, prevSenderId != senderId, currentUserId);
                                
                                // Добавляем в временный список новых сообщений
                                newMessages.Add(messageBubble);
                                newMessageElements[messageId] = messageBubble;
                                
                                prevSenderId = senderId;
                            }
                            
                            // Добавляем новые сообщения в панель
                            foreach (var element in newMessages)
                            {
                                messagesPanel.Children.Add(element);
                            }
                            
                            // Обновляем словарь элементов сообщений
                            foreach (var kvp in newMessageElements)
                            {
                                messageElements[kvp.Key] = kvp.Value;
                            }
                            
                            // Удаляем сообщения, которых больше нет в базе
                            existingIds.ExceptWith(currentIds); // Оставляем только те, которых нет в currentIds
                            foreach (int messageId in existingIds)
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
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка обновления сообщений: " + ex.Message);
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
    }
} 