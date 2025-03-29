using MahApps.Metro.IconPacks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using УправлениеСкладом;

namespace УправлениеСкладом.Class
{
    /// <summary>
    /// Класс для создания элементов интерфейса сообщений
    /// </summary>
    public class UIMessageFactory
    {
        /// <summary>
        /// Создание элемента сообщения в стиле Telegram
        /// </summary>
        public static StackPanel CreateMessageBubble(int messageId, int senderId, string text, DateTime messageTime, bool isOutgoing, bool isEdited, bool isRead, bool showAvatar, int currentUserId)
        {
            var mainPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5),
                HorizontalAlignment = isOutgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Tag = messageId
            };

            // Убираем отображение аватаров для всех сообщений
            // Отключены аватары

            // Контейнер сообщения
            var messageContainer = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(5, 0, 5, 0)
            };

            // Пузырь сообщения
            var messageBubble = new Border
            {
                Background = isOutgoing ? 
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3F2FD")) : 
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5")),
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(10),
                MaxWidth = 400
            };

            var contentPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            // Добавляем текст сообщения
            if (!string.IsNullOrEmpty(text))
            {
                var textBlock = new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14
                };
                
                // Привязка к ресурсу цвета текста
                Binding foregroundBinding = new Binding("MessageTextBrush")
                {
                    Source = Application.Current.Resources
                };
                textBlock.SetBinding(TextBlock.ForegroundProperty, foregroundBinding);
                
                contentPanel.Children.Add(textBlock);
            }

            messageBubble.Child = contentPanel;
            messageContainer.Children.Add(messageBubble);

            // Информация о сообщении
            var infoPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = isOutgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Margin = new Thickness(5, 2, 5, 0)
            };

            // Время
            var timeText = new TextBlock
            {
                Text = messageTime.ToString("HH:mm"),
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.Gray)
            };
            infoPanel.Children.Add(timeText);

            // Статус прочтения
            if (isOutgoing)
            {
                var readIcon = new PackIconMaterial
                {
                    Kind = isRead ? PackIconMaterialKind.CheckAll : PackIconMaterialKind.Check,
                    Width = 16,
                    Height = 16,
                    Foreground = isRead ? new SolidColorBrush(Colors.Blue) : new SolidColorBrush(Colors.Gray),
                    Margin = new Thickness(5, 0, 0, 0)
                };
                infoPanel.Children.Add(readIcon);
            }

            // Статус редактирования
            if (isEdited)
            {
                var editText = new TextBlock
                {
                    Text = "изменено",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    Margin = new Thickness(5, 0, 0, 0)
                };
                infoPanel.Children.Add(editText);
            }

            messageContainer.Children.Add(infoPanel);
            mainPanel.Children.Add(messageContainer);

            // Добавляем контекстное меню
            var contextMenu = CreateContextMenu(messageId, senderId, currentUserId);
            mainPanel.ContextMenu = contextMenu;

            return mainPanel;
        }

        public static StackPanel CreateMessageWithAttachmentBubble(int messageId, int senderId, string text, DateTime messageTime, bool isOutgoing, bool isEdited, bool isRead, bool showAvatar, int currentUserId, MessageAttachment attachment)
        {
            var mainPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5),
                HorizontalAlignment = isOutgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Tag = messageId
            };

            // Убираем отображение аватаров для всех сообщений
            // Отключены аватары

            // Контейнер сообщения
            var messageContainer = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(5, 0, 5, 0)
            };

            // Пузырь сообщения
            var messageBubble = new Border
            {
                Background = isOutgoing ? 
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3F2FD")) : 
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5")),
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(10),
                MaxWidth = 400
            };

            var contentPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            // Добавляем вложение
            if (attachment != null)
            {
                if (attachment.Type == AttachmentType.Image)
                {
                    try
                    {
                        // Для изображений создаем более стильное отображение с закругленными углами
                        Border imageBorder = new Border
                        {
                            CornerRadius = new CornerRadius(10),
                            Margin = new Thickness(0, 0, 0, 8),
                            MaxWidth = 300,
                            MaxHeight = 300,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            ClipToBounds = true // Обрезаем изображение по границам Border
                        };
                        
                        // Создаем изображение
                        Image image = new Image
                        {
                            Stretch = Stretch.UniformToFill,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        
                        // Загружаем изображение из байтов
                        var imageSource = new BitmapImage();
                        using (var ms = new System.IO.MemoryStream(attachment.Content))
                        {
                            imageSource.BeginInit();
                            imageSource.CacheOption = BitmapCacheOption.OnLoad;
                            imageSource.StreamSource = ms;
                            imageSource.EndInit();
                            imageSource.Freeze(); // Оптимизация для UI потока
                        }
                        
                        image.Source = imageSource;
                        
                        // Добавляем изображение в Border
                        imageBorder.Child = image;
                        
                        // Добавляем Border в контент
                        contentPanel.Children.Add(imageBorder);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка отображения изображения: {ex.Message}");
                        
                        // Если не удалось отобразить изображение, показываем улучшенную информацию о файле
                        AddStyledFileInfoToContentPanel(attachment, contentPanel);
                    }
                }
                else
                {
                    // Для других типов файлов показываем улучшенную информацию
                    AddStyledFileInfoToContentPanel(attachment, contentPanel);
                }
            }

            // Добавляем текст сообщения, если он есть
            if (!string.IsNullOrEmpty(text))
            {
                var textBlock = new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                
                // Привязка к ресурсу цвета текста
                Binding foregroundBinding = new Binding("MessageTextBrush")
                {
                    Source = Application.Current.Resources
                };
                textBlock.SetBinding(TextBlock.ForegroundProperty, foregroundBinding);
                
                contentPanel.Children.Add(textBlock);
            }

            messageBubble.Child = contentPanel;
            messageContainer.Children.Add(messageBubble);

            // Панель с информацией (время, статус)
            var infoPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = isOutgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Margin = new Thickness(5, 2, 5, 0)
            };

            // Время
            var timeText = new TextBlock
            {
                Text = messageTime.ToString("HH:mm"),
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.Gray)
            };
            infoPanel.Children.Add(timeText);

            // Статус прочтения
            if (isOutgoing)
            {
                var readIcon = new PackIconMaterial
                {
                    Kind = isRead ? PackIconMaterialKind.CheckAll : PackIconMaterialKind.Check,
                    Width = 16,
                    Height = 16,
                    Foreground = isRead ? new SolidColorBrush(Colors.Blue) : new SolidColorBrush(Colors.Gray),
                    Margin = new Thickness(5, 0, 0, 0)
                };
                infoPanel.Children.Add(readIcon);
            }

            // Статус редактирования
            if (isEdited)
            {
                var editText = new TextBlock
                {
                    Text = "изменено",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    Margin = new Thickness(5, 0, 0, 0)
                };
                infoPanel.Children.Add(editText);
            }

            messageContainer.Children.Add(infoPanel);
            mainPanel.Children.Add(messageContainer);

            // Добавляем контекстное меню
            var contextMenu = CreateContextMenu(messageId, senderId, currentUserId);
            mainPanel.ContextMenu = contextMenu;

            return mainPanel;
        }

        // Вспомогательный метод для добавления стилизованной информации о файле
        private static void AddStyledFileInfoToContentPanel(MessageAttachment attachment, StackPanel contentPanel)
        {
            // Создаем стильную панель для файла
            Border fileContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0)), // Полупрозрачный цвет
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };

            Grid fileGrid = new Grid();
            fileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            fileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Создаем круглую подложку для иконки
            Border iconBackground = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                Background = GetFileIconBackground(attachment.Type),
                Margin = new Thickness(0, 0, 12, 0)
            };

            // Иконка типа файла
            var fileIcon = new PackIconMaterial
            {
                Kind = GetFileIcon(attachment.Type),
                Width = 20,
                Height = 20,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            iconBackground.Child = fileIcon;
            Grid.SetColumn(iconBackground, 0);
            fileGrid.Children.Add(iconBackground);

            // Информация о файле
            var fileInfo = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center
            };

            var fileName = new TextBlock
            {
                Text = attachment.FileName,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 2)
            };
            fileInfo.Children.Add(fileName);

            var fileSize = new TextBlock
            {
                Text = attachment.FormattedFileSize,
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.Gray)
            };
            fileInfo.Children.Add(fileSize);

            Grid.SetColumn(fileInfo, 1);
            fileGrid.Children.Add(fileInfo);

            fileContainer.Child = fileGrid;
            contentPanel.Children.Add(fileContainer);
        }

        // Получить цвет фона для иконки типа файла
        private static SolidColorBrush GetFileIconBackground(AttachmentType type)
        {
            switch (type)
            {
                case AttachmentType.Image:
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Зеленый
                case AttachmentType.Video:
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Красный
                case AttachmentType.Audio:
                    return new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Синий
                case AttachmentType.Document:
                    return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Оранжевый
                default:
                    return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Серый
            }
        }

        private static PackIconMaterialKind GetFileIcon(AttachmentType type)
        {
            switch (type)
            {
                case AttachmentType.Image:
                    return PackIconMaterialKind.Image;
                case AttachmentType.Video:
                    return PackIconMaterialKind.Video;
                case AttachmentType.Audio:
                    return PackIconMaterialKind.Music;
                case AttachmentType.Document:
                    return PackIconMaterialKind.FileDocumentOutline;
                default:
                    return PackIconMaterialKind.File;
            }
        }

        private static Border CreateAvatar(int userId)
        {
            // Создаем круглый аватар со стилем как в поиске
            var avatar = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Голубой цвет аватара
                Margin = new Thickness(0, 0, 5, 0)
            };

            // Добавляем иконку пользователя
            var icon = new PackIconMaterial
            {
                Kind = PackIconMaterialKind.Account,
                Width = 24,
                Height = 24,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            avatar.Child = icon;
            return avatar;
        }

        private static ContextMenu CreateContextMenu(int messageId, int senderId, int currentUserId)
        {
            var contextMenu = new ContextMenu();
            
            // Копировать
            var copyItem = new MenuItem
            {
                Header = "Копировать",
                Icon = new PackIconMaterial { Kind = PackIconMaterialKind.ContentCopy },
                Tag = messageId
            };
            copyItem.Click += (s, e) => CopyMessage_Click(s, e);
            contextMenu.Items.Add(copyItem);

            // Редактировать (только для своих сообщений)
            if (senderId == currentUserId)
            {
                var editItem = new MenuItem
                {
                    Header = "Редактировать",
                    Icon = new PackIconMaterial { Kind = PackIconMaterialKind.Pencil },
                    Tag = messageId
                };
                editItem.Click += (s, e) => EditMessage_Click(s, e);
                contextMenu.Items.Add(editItem);
            }

            // Удалить для меня
            var deleteForMeItem = new MenuItem
            {
                Header = "Удалить для меня",
                Icon = new PackIconMaterial { Kind = PackIconMaterialKind.Delete },
                Tag = messageId
            };
            deleteForMeItem.Click += (s, e) => DeleteMessageForMe_Click(s, e);
            contextMenu.Items.Add(deleteForMeItem);

            // Удалить для всех (только для своих сообщений)
            if (senderId == currentUserId)
            {
                var deleteForAllItem = new MenuItem
                {
                    Header = "Удалить для всех",
                    Icon = new PackIconMaterial { Kind = PackIconMaterialKind.DeleteForever },
                    Tag = messageId
                };
                deleteForAllItem.Click += (s, e) => DeleteMessageForAll_Click(s, e);
                contextMenu.Items.Add(deleteForAllItem);
            }

            return contextMenu;
        }

        // Метод, который будет вызван при нажатии на пункт "Копировать"
        private static void CopyMessage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is int messageId)
            {
                // Находим элемент сообщения по ID
                var window = FindMessengerWindow();
                if (window != null)
                {
                    window.CopyMessage(messageId);
                }
            }
        }

        // Метод, который будет вызван при нажатии на пункт "Редактировать"
        private static void EditMessage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is int messageId)
            {
                // Находим элемент сообщения по ID
                var window = FindMessengerWindow();
                if (window != null)
                {
                    window.EditMessage(messageId);
                }
            }
        }

        // Метод, который будет вызван при нажатии на пункт "Удалить для меня"
        private static void DeleteMessageForMe_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is int messageId)
            {
                // Находим окно мессенджера и вызываем метод удаления
                var window = FindMessengerWindow();
                if (window != null)
                {
                    window.DeleteMessageForMe(messageId);
                }
            }
        }

        // Метод, который будет вызван при нажатии на пункт "Удалить для всех"
        private static void DeleteMessageForAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is int messageId)
            {
                // Находим окно мессенджера и вызываем метод удаления
                var window = FindMessengerWindow();
                if (window != null)
                {
                    window.DeleteMessageForAll(messageId);
                }
            }
        }

        // Метод для поиска окна мессенджера
        private static MessengerWindow FindMessengerWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType().Name == "MessengerWindow")
                {
                    return window as MessengerWindow;
                }
            }
            return null;
        }

        public static FrameworkElement FindFirstBorder(FrameworkElement element)
        {
            if (element is Border border)
                return border;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
                if (child != null)
                {
                    var result = FindFirstBorder(child);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        public static TextBlock FindTextBlock(FrameworkElement element)
        {
            if (element is TextBlock textBlock)
                return textBlock;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
                if (child != null)
                {
                    var result = FindTextBlock(child);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Находит первый Border в элементе сообщения или его дочерних элементах
        /// </summary>
        public static Border FindFirstBorderExplicit(FrameworkElement element)
        {
            // Если элемент сам по себе Border, возвращаем его
            if (element is Border border)
                return border;
        
            // Если элемент контейнер (StackPanel, Grid и т.д.)
            if (element is Panel panel)
            {
                // Для StackPanel (обычно это mainPanel для сообщений)
                if (panel is StackPanel stackPanel)
                {
                    // Обычная структура сообщения: StackPanel -> StackPanel -> Border
                    foreach (var child in stackPanel.Children)
                    {
                        if (child is StackPanel messageContainer)
                        {
                            foreach (var containerChild in messageContainer.Children)
                            {
                                if (containerChild is Border messageBubble)
                                    return messageBubble;
                            }
                        }
                    }
                }
                // Для Grid (может быть другая структура)
                else if (panel is Grid grid)
                {
                    // Проходим по всем дочерним элементам Grid
                    for (int i = 0; i < grid.Children.Count; i++)
                    {
                        var child = grid.Children[i];
                        
                        // Если нашли Border напрямую
                        if (child is Border foundBorder)
                            return foundBorder;
                            
                        // Если нашли промежуточный контейнер
                        if (child is Panel childPanel)
                        {
                            // Рекурсивно ищем в дочернем контейнере
                            var nestedBorder = FindFirstBorderExplicit(childPanel as FrameworkElement);
                            if (nestedBorder != null)
                                return nestedBorder;
                        }
                    }
                }
            }
            
            // Если это другой контейнер, используем VisualTreeHelper для поиска
            int childCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
                if (child != null)
                {
                    var result = FindFirstBorderExplicit(child);
                    if (result != null)
                        return result;
                }
            }
            
            return null;
        }

        // Метод форматирования даты сообщения
        private static string FormatMessageDate(DateTime date)
        {
            DateTime today = DateTime.Today;
            DateTime yesterday = today.AddDays(-1);

            if (date.Date == today)
            {
                return "Сегодня";
            }
            else if (date.Date == yesterday)
            {
                return "Вчера";
            }
            else
            {
                // Формат даты: 27 марта
                string dateText = date.ToString("d MMMM");
                
                // Если год не текущий, добавляем год
                if (date.Year != today.Year)
                {
                    dateText += " " + date.Year;
                }
                
                return dateText;
            }
        }

        /// <summary>
        /// Найти все дочерние элементы указанного типа
        /// </summary>
        public static List<T> FindAllChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            var list = new List<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    list.Add(typedChild);
                }

                list.AddRange(FindAllChildren<T>(child));
            }
            return list;
        }

        /// <summary>
        /// Создает разделитель с датой для группировки сообщений в стиле Telegram
        /// </summary>
        public static FrameworkElement CreateDateSeparator(DateTime messageDate)
        {
            // Создаем облачко-разделитель с датой сообщения
            Border dateContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 200, 200, 200)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(0, 10, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Текст с датой
            TextBlock dateText = new TextBlock
            {
                Text = FormatMessageDate(messageDate),
                FontSize = 12,
                TextAlignment = TextAlignment.Center
            };
            
            // Привязка к цвету текста из ресурсов
            Binding foregroundBinding = new Binding("MessageTextBrush")
            {
                Source = Application.Current.Resources
            };
            dateText.SetBinding(TextBlock.ForegroundProperty, foregroundBinding);

            dateContainer.Child = dateText;
            
            // Добавляем тег с датой для идентификации сепаратора
            dateContainer.Tag = messageDate.Date.ToString("yyyy-MM-dd");
            
            return dateContainer;
        }

        // Метод для добавления сообщения на панель с проверкой необходимости добавления сепаратора даты
        public static void AddMessageWithDateSeparator(Panel messagesPanel, FrameworkElement messageElement, DateTime messageDate)
        {
            // Проверяем, нужно ли добавлять сепаратор даты
            bool needDateSeparator = true;
            string currentDateString = messageDate.Date.ToString("yyyy-MM-dd");
            
            // Ищем сепаратор с такой же датой - если он уже есть, не добавляем новый
            foreach (UIElement element in messagesPanel.Children)
            {
                if (element is Border border && border.Tag != null && border.Tag is string tagString)
                {
                    // Если это сепаратор даты, проверяем, совпадает ли дата
                    if (tagString == messageDate.Date.ToString("yyyy-MM-dd"))
                    {
                        // Если дата совпадает с существующим сепаратором, не нужно добавлять новый
                        needDateSeparator = false;
                        break;
                    }
                }
            }
            
            // Если нужен сепаратор даты, добавляем его
            if (needDateSeparator)
            {
                FrameworkElement dateSeparator = CreateDateSeparator(messageDate);
                messagesPanel.Children.Add(dateSeparator);
            }
            
            // Добавляем само сообщение
            messagesPanel.Children.Add(messageElement);
        }
    }
} 