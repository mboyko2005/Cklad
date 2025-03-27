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
using System.Windows.Media;
using System.Windows.Media.Animation;

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
            // Основной контейнер
            StackPanel messageContainer = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = isOutgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Margin = new Thickness(isOutgoing ? 50 : 5, 5, isOutgoing ? 5 : 50, 5),
                Tag = messageId
            };

            // Grid для сообщения, который позволит нам разместить статус справа внизу
            Grid messageGrid = new Grid();
            messageGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            messageGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Получаем цвета из ресурсов приложения
            Brush outgoingBackground, incomingBackground, borderBrush, messageForeground;
            
            if (ThemeManager.IsDarkTheme)
            {
                // Темная тема с цветами в стиле приложения
                outgoingBackground = (SolidColorBrush)Application.Current.FindResource("PrimaryBrush");
                incomingBackground = new SolidColorBrush(Color.FromRgb(52, 57, 62)); // Темно-серый для входящих
                borderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                messageForeground = new SolidColorBrush(Colors.White);
            }
            else
            {
                // Светлая тема
                outgoingBackground = new SolidColorBrush(Color.FromRgb(187, 222, 251)); // Светло-синий для исходящих
                incomingBackground = new SolidColorBrush(Colors.White);
                borderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230));
                messageForeground = new SolidColorBrush(Colors.Black);
            }
            
            // Основной пузырек сообщения
            Border messageBubble = new Border
            {
                Background = isOutgoing ? outgoingBackground : incomingBackground,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 8, 12, 8),
                MinWidth = 80
            };
            
            // Контейнер для текста и метаданных
            StackPanel contentPanel = new StackPanel();
            
            // Текст сообщения
            TextBlock messageTextBlock = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                Foreground = isOutgoing && ThemeManager.IsDarkTheme ? new SolidColorBrush(Colors.White) : messageForeground,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 14,
                MaxWidth = 500
            };
            contentPanel.Children.Add(messageTextBlock);
            
            // Метаданные сообщения (время, статус)
            StackPanel metadataPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 5, 0, 0)
            };
            
            // Метка "изменено"
            if (isEdited)
            {
                TextBlock editedLabel = new TextBlock
                {
                    Text = "изм. ",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                metadataPanel.Children.Add(editedLabel);
            }
            
            // Время сообщения
            TextBlock timeBlock = new TextBlock
            {
                Text = messageTime.ToString("HH:mm"),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            metadataPanel.Children.Add(timeBlock);
            
            // Индикатор статуса сообщения (только для исходящих)
            if (isOutgoing)
            {
                PackIconMaterial statusIcon = new PackIconMaterial
                {
                    Kind = isRead ? PackIconMaterialKind.CheckAll : PackIconMaterialKind.Check,
                    Width = 12,
                    Height = 12,
                    Foreground = isRead ? 
                        new SolidColorBrush(Color.FromRgb(79, 195, 247)) : // Голубой если прочитано
                        new SolidColorBrush(Color.FromRgb(189, 189, 189))  // Серый если не прочитано
                };
                metadataPanel.Children.Add(statusIcon);
            }
            
            contentPanel.Children.Add(metadataPanel);
            messageBubble.Child = contentPanel;
            
            Grid.SetRow(messageBubble, 0);
            messageGrid.Children.Add(messageBubble);
            
            // Добавляем аватар для входящих сообщений
            if (!isOutgoing && showAvatar)
            {
                Border avatarBorder = new Border
                {
                    Width = 30,
                    Height = 30,
                    CornerRadius = new CornerRadius(15),
                    Background = new SolidColorBrush(Color.FromRgb(224, 224, 224)), // Светло-серый
                    Margin = new Thickness(5, 0, 5, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                PackIconMaterial avatarIcon = new PackIconMaterial
                {
                    Kind = PackIconMaterialKind.AccountCircle,
                    Width = 30,
                    Height = 30,
                    Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)) // Серый
                };

                avatarBorder.Child = avatarIcon;
                Grid.SetRow(avatarBorder, 0);
                
                // Создаем контейнер для сообщения и аватара
                Grid messageWithAvatar = new Grid();
                messageWithAvatar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
                messageWithAvatar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                
                Grid.SetColumn(avatarBorder, 0);
                Grid.SetColumn(messageGrid, 1);
                
                messageWithAvatar.Children.Add(avatarBorder);
                messageWithAvatar.Children.Add(messageGrid);
                
                messageContainer.Children.Add(messageWithAvatar);
            }
            else
            {
                messageContainer.Children.Add(messageGrid);
            }
            
            // Добавляем контекстное меню для сообщения
            var contextMenu = CreateMessageContextMenu();
            if (contextMenu != null)
            {
                contextMenu.Tag = messageId; // Сохраняем ID сообщения в тег меню
                messageContainer.ContextMenu = contextMenu;
            }
            
            // Добавляем анимацию появления
            AnimationHelper.MessageAppearAnimation(messageContainer, isOutgoing);
            
            return messageContainer;
        }

        /// <summary>
        /// Создание контекстного меню для сообщения
        /// </summary>
        private static ContextMenu CreateMessageContextMenu()
        {
            ContextMenu menu = new ContextMenu();
            
            // Копировать
            MenuItem copyItem = new MenuItem { Header = "Копировать" };
            copyItem.Icon = new PackIconMaterial { Kind = PackIconMaterialKind.ContentCopy, Width = 16, Height = 16 };
            copyItem.Click += (s, e) => 
            {
                if (s is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu && 
                    contextMenu.PlacementTarget is FrameworkElement target)
                {
                    var window = Window.GetWindow(target);
                    if (window is MessengerWindow messengerWindow)
                    {
                        // Программно вызываем метод в окне мессенджера
                        var methodInfo = messengerWindow.GetType().GetMethod("CopyMessage_Click", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        
                        if (methodInfo != null)
                            methodInfo.Invoke(messengerWindow, new object[] { s, e });
                    }
                }
            };
            
            // Редактировать
            MenuItem editItem = new MenuItem { Header = "Редактировать" };
            editItem.Icon = new PackIconMaterial { Kind = PackIconMaterialKind.PencilOutline, Width = 16, Height = 16 };
            editItem.Click += (s, e) => 
            {
                if (s is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu &&
                    contextMenu.PlacementTarget is FrameworkElement target)
                {
                    var window = Window.GetWindow(target);
                    if (window is MessengerWindow messengerWindow)
                    {
                        var methodInfo = messengerWindow.GetType().GetMethod("EditMessage_Click",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            
                        if (methodInfo != null)
                            methodInfo.Invoke(messengerWindow, new object[] { s, e });
                    }
                }
            };
            
            // Ответить
            MenuItem replyItem = new MenuItem { Header = "Ответить" };
            replyItem.Icon = new PackIconMaterial { Kind = PackIconMaterialKind.Reply, Width = 16, Height = 16 };
            replyItem.Click += (s, e) => 
            {
                if (s is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu &&
                    contextMenu.PlacementTarget is FrameworkElement target)
                {
                    var window = Window.GetWindow(target);
                    if (window is MessengerWindow messengerWindow)
                    {
                        var methodInfo = messengerWindow.GetType().GetMethod("ReplyMessage_Click",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            
                        if (methodInfo != null)
                            methodInfo.Invoke(messengerWindow, new object[] { s, e });
                    }
                }
            };
            
            // Удалить для себя
            MenuItem deleteForMeItem = new MenuItem { Header = "Удалить для себя" };
            var deleteIcon = new PackIconMaterial { Kind = PackIconMaterialKind.DeleteOutline, Width = 16, Height = 16 };
            deleteIcon.Foreground = new SolidColorBrush(Colors.Red);
            deleteForMeItem.Icon = deleteIcon;
            deleteForMeItem.Click += (s, e) => 
            {
                if (s is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu &&
                    contextMenu.PlacementTarget is FrameworkElement target)
                {
                    var window = Window.GetWindow(target);
                    if (window is MessengerWindow messengerWindow)
                    {
                        var methodInfo = messengerWindow.GetType().GetMethod("DeleteMessageForMe_Click",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            
                        if (methodInfo != null)
                            methodInfo.Invoke(messengerWindow, new object[] { s, e });
                    }
                }
            };
            
            // Удалить для всех
            MenuItem deleteForAllItem = new MenuItem { Header = "Удалить для всех" };
            var deleteAllIcon = new PackIconMaterial { Kind = PackIconMaterialKind.DeleteForever, Width = 16, Height = 16 };
            deleteAllIcon.Foreground = new SolidColorBrush(Colors.Red);
            deleteForAllItem.Icon = deleteAllIcon;
            deleteForAllItem.Click += (s, e) => 
            {
                if (s is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu &&
                    contextMenu.PlacementTarget is FrameworkElement target)
                {
                    var window = Window.GetWindow(target);
                    if (window is MessengerWindow messengerWindow)
                    {
                        var methodInfo = messengerWindow.GetType().GetMethod("DeleteMessageForAll_Click",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            
                        if (methodInfo != null)
                            methodInfo.Invoke(messengerWindow, new object[] { s, e });
                    }
                }
            };
            
            // Добавляем пункты в меню
            menu.Items.Add(copyItem);
            menu.Items.Add(editItem);
            menu.Items.Add(replyItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(deleteForMeItem);
            menu.Items.Add(deleteForAllItem);
            
            return menu;
        }

        /// <summary>
        /// Создание разделителя с датой
        /// </summary>
        public static Border CreateDateSeparator(DateTime date)
        {
            // Форматируем дату
            string dateText;
            if (date.Date == DateTime.Today)
                dateText = "Сегодня";
            else if (date.Date == DateTime.Today.AddDays(-1))
                dateText = "Вчера";
            else
                dateText = date.ToString("d MMMM yyyy");
            
            // Создаем элемент даты
            Border dateSeparator = new Border
            {
                Background = (Brush)Application.Current.FindResource("ButtonBackgroundBrush"),
                CornerRadius = new CornerRadius(15),
                Margin = new Thickness(0, 10, 0, 10),
                Padding = new Thickness(10, 5, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            TextBlock dateTextBlock = new TextBlock
            {
                Text = dateText,
                FontSize = 12,
                Foreground = (Brush)Application.Current.FindResource("ForegroundBrush"),
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeights.SemiBold
            };
            
            dateSeparator.Child = dateTextBlock;
            return dateSeparator;
        }

        /// <summary>
        /// Вспомогательный метод для поиска всех дочерних элементов указанного типа
        /// </summary>
        public static List<T> FindAllChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            List<T> results = new List<T>();
            
            if (parent == null)
                return results;
                
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is T typedChild)
                    results.Add(typedChild);
                    
                // Рекурсивно ищем в дочерних элементах
                results.AddRange(FindAllChildren<T>(child));
            }
            
            return results;
        }
        
        /// <summary>
        /// Вспомогательный метод для поиска элемента Border в дереве элементов
        /// </summary>
        public static Border FindFirstBorder(DependencyObject parent)
        {
            if (parent is Border border)
                return border;
                
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = FindFirstBorder(child);
                if (result != null)
                    return result;
            }
            
            return null;
        }
        
        /// <summary>
        /// Вспомогательный метод для поиска элемента TextBlock в дереве элементов
        /// </summary>
        public static TextBlock FindTextBlock(DependencyObject parent)
        {
            if (parent is TextBlock textBlock)
                return textBlock;
                
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = FindTextBlock(child);
                if (result != null)
                    return result;
            }
            
            return null;
        }
    }
} 