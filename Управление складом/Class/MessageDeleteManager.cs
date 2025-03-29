using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace УправлениеСкладом.Class
{
    /// <summary>
    /// Класс для управления удалением сообщений в мессенджере
    /// </summary>
    public class MessageDeleteManager
    {
        private readonly MessageManager messageManager;
        private readonly Dictionary<int, FrameworkElement> messageElements;
        private readonly Panel messagesPanel;

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="messageManager">Менеджер сообщений</param>
        /// <param name="messageElements">Словарь элементов сообщений</param>
        /// <param name="messagesPanel">Панель сообщений</param>
        public MessageDeleteManager(MessageManager messageManager, Dictionary<int, FrameworkElement> messageElements, Panel messagesPanel)
        {
            this.messageManager = messageManager;
            this.messageElements = messageElements;
            this.messagesPanel = messagesPanel;
        }

        /// <summary>
        /// Удалить сообщение для текущего пользователя
        /// </summary>
        /// <param name="messageId">ID сообщения для удаления</param>
        /// <returns>Успешно ли удалено сообщение</returns>
        public async Task<bool> DeleteMessageForMeAsync(int messageId)
        {
            try
            {
                // Получаем элемент сообщения
                if (!messageElements.TryGetValue(messageId, out FrameworkElement messageElement))
                {
                    MessageBox.Show("Сообщение не найдено в интерфейсе");
                    return false;
                }

                // Вызываем метод удаления в MessageManager
                bool deleted = await messageManager.DeleteMessageForMeAsync(messageId);

                if (deleted)
                {
                    // Если удаление прошло успешно, удаляем элемент из интерфейса с анимацией
                    RemoveMessageWithAnimation(messageId, messageElement);
                    return true;
                }
                else
                {
                    MessageBox.Show("Не удалось удалить сообщение");
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении сообщения: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Удалить сообщение для всех
        /// </summary>
        /// <param name="messageId">ID сообщения для удаления</param>
        /// <returns>Успешно ли удалено сообщение</returns>
        public async Task<bool> DeleteMessageForAllAsync(int messageId)
        {
            try
            {
                // Проверяем, существует ли элемент сообщения
                if (!messageElements.TryGetValue(messageId, out FrameworkElement messageElement))
                {
                    MessageBox.Show("Сообщение не найдено в интерфейсе");
                    return false;
                }

                // Запрашиваем подтверждение
                var result = MessageBox.Show(
                    "Вы действительно хотите удалить это сообщение для всех участников?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return false;

                // Вызываем метод удаления в MessageManager
                bool deleted = await messageManager.DeleteMessageForAllAsync(messageId);

                if (deleted)
                {
                    // Удаляем элемент из интерфейса с анимацией
                    RemoveMessageWithAnimation(messageId, messageElement);
                    return true;
                }
                else
                {
                    MessageBox.Show("Не удалось удалить сообщение для всех");
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении сообщения: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Удаление сообщения с анимацией
        /// </summary>
        private void RemoveMessageWithAnimation(int messageId, FrameworkElement messageElement)
        {
            // Удаляем элемент из интерфейса
            messagesPanel.Children.Remove(messageElement);
            
            // Удаляем элемент из словаря
            messageElements.Remove(messageId);
            
            // Проверяем наличие пустых разделителей дат
            CleanupEmptyDateSeparators();
        }

        /// <summary>
        /// Очистка пустых разделителей дат
        /// </summary>
        private void CleanupEmptyDateSeparators()
        {
            // Список для хранения разделителей, которые нужно удалить
            List<UIElement> separatorsToRemove = new List<UIElement>();
            UIElement previousElement = null;
            
            // Проходим по всем элементам и ищем два соседних разделителя дат
            foreach (UIElement element in messagesPanel.Children)
            {
                // Определяем, является ли текущий элемент разделителем даты
                bool isSeparator = false;
                if (element is Border border && border.Tag != null && border.Tag is string tagString && tagString.StartsWith("yyyy-MM-dd"))
                {
                    isSeparator = true;
                }
                
                // Если текущий и предыдущий элементы - разделители, помечаем предыдущий на удаление
                if (isSeparator && previousElement != null && 
                    previousElement is Border prevBorder && prevBorder.Tag != null && 
                    prevBorder.Tag is string prevTag && prevTag.StartsWith("yyyy-MM-dd"))
                {
                    separatorsToRemove.Add(previousElement);
                }
                
                previousElement = element;
            }
            
            // Удаляем найденные лишние разделители
            foreach (var separatorToRemove in separatorsToRemove)
            {
                messagesPanel.Children.Remove(separatorToRemove);
            }
        }
    }
} 