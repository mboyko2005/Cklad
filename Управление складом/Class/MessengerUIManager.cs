using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using УправлениеСкладом.Class;

namespace УправлениеСкладом.Class
{
    public class MessengerUIManager
    {
        private Window parentWindow;
        private StackPanel messagesPanel;
        private ScrollViewer messagesScrollViewer;
        private Border searchPanelBorder;
        private TextBox searchMessageTextBox;
        private TextBlock searchResultsCountText;
        private ListBox searchResultsList;
        private WrapPanel suggestedMessagesPanel;
        private TextBox messageTextBox;
        private TextBlock chatTitle;
        private TextBlock chatUserLogin;
        private DispatcherTimer searchInactivityTimer;
        private bool isSearchActive = false;
        
        public MessengerUIManager(
            Window parentWindow,
            StackPanel messagesPanel,
            ScrollViewer messagesScrollViewer,
            Border searchPanelBorder,
            TextBox searchMessageTextBox,
            TextBlock searchResultsCountText,
            ListBox searchResultsList,
            WrapPanel suggestedMessagesPanel,
            TextBox messageTextBox,
            TextBlock chatTitle,
            TextBlock chatUserLogin)
        {
            this.parentWindow = parentWindow;
            this.messagesPanel = messagesPanel;
            this.messagesScrollViewer = messagesScrollViewer;
            this.searchPanelBorder = searchPanelBorder;
            this.searchMessageTextBox = searchMessageTextBox;
            this.searchResultsCountText = searchResultsCountText;
            this.searchResultsList = searchResultsList;
            this.suggestedMessagesPanel = suggestedMessagesPanel;
            this.messageTextBox = messageTextBox;
            this.chatTitle = chatTitle;
            this.chatUserLogin = chatUserLogin;
            
            // Инициализация таймера неактивности поиска
            searchInactivityTimer = new DispatcherTimer();
            searchInactivityTimer.Interval = TimeSpan.FromSeconds(5);
            searchInactivityTimer.Tick += (s, e) =>
            {
                // Если поисковое поле пустое, скрываем панель
                if (string.IsNullOrWhiteSpace(searchMessageTextBox.Text))
                {
                    ToggleSearchPanel(false);
                    searchInactivityTimer.Stop();
                }
            };
        }
        
        // Настройка предложенных сообщений
        public void SetupSuggestedMessages()
        {
            // Примеры предложенных сообщений
            var suggestedMessages = new[]
            {
                "Добрый день!",
                "Как обстоят дела с заказом?",
                "Спасибо за информацию",
                "Подтверждаю получение",
                "Требуется дополнительная информация"
            };
            
            // Очищаем панель предложенных сообщений
            suggestedMessagesPanel.Children.Clear();
            
            // Добавляем предложенные сообщения в панель
            foreach (var message in suggestedMessages)
            {
                Button button = new Button
                {
                    Content = message,
                    Style = (Style)parentWindow.FindResource("SuggestedMessageButtonStyle")
                };
                
                button.Click += (s, e) =>
                {
                    messageTextBox.Text = message;
                    messageTextBox.Focus();
                    messageTextBox.SelectionStart = messageTextBox.Text.Length;
                };
                
                suggestedMessagesPanel.Children.Add(button);
            }
        }
        
        // Показать/скрыть панель поиска
        public void ToggleSearchPanel(bool show)
        {
            if (show)
            {
                searchPanelBorder.Visibility = Visibility.Visible;
                isSearchActive = true;
                searchMessageTextBox.Focus();
                
                // Запускаем таймер неактивности
                if (string.IsNullOrWhiteSpace(searchMessageTextBox.Text))
                {
                    searchInactivityTimer.Start();
                }
            }
            else
            {
                searchPanelBorder.Visibility = Visibility.Collapsed;
                isSearchActive = false;
                
                // Останавливаем таймер неактивности
                searchInactivityTimer.Stop();
            }
        }
        
        // Прокрутка к последнему сообщению
        public void ScrollToBottom()
        {
            try
            {
                if (messagesScrollViewer != null)
                    messagesScrollViewer.ScrollToEnd();
            }
            catch (Exception ex)
            {
                // Игнорируем ошибку, если элемент не найден
                System.Diagnostics.Debug.WriteLine("Ошибка при прокрутке: " + ex.Message);
            }
        }
        
        // Прокрутка к указанному сообщению
        public void ScrollToMessage(FrameworkElement message)
        {
            if (message != null && messagesScrollViewer != null)
            {
                // Прокручиваем к сообщению с учетом его положения
                message.BringIntoView();
            }
        }
        
        // Анимация обновления сообщения
        public void AnimateMessageUpdate(FrameworkElement message)
        {
            // Сохраняем начальный цвет фона
            Brush originalBackground = null;
            if (message is Border border)
            {
                originalBackground = border.Background;
            }
            
            // Создаем анимацию фона
            ColorAnimation colorAnimation = null;
            if (originalBackground is SolidColorBrush brush)
            {
                // Светло-желтый цвет для подсветки изменения
                Color highlightColor = Color.FromRgb(255, 255, 200);
                Color originalColor = brush.Color;
                
                colorAnimation = new ColorAnimation
                {
                    From = highlightColor,
                    To = originalColor,
                    Duration = TimeSpan.FromMilliseconds(1500),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                
                // Создаем кисть для анимации
                SolidColorBrush animationBrush = new SolidColorBrush(highlightColor);
                if (message is Border borderElement)
                {
                    borderElement.Background = animationBrush;
                }
                
                // Запускаем анимацию цвета
                animationBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            }
        }
        
        // Обновление счетчика результатов поиска
        public void UpdateSearchResultsCount(int currentIndex, int total)
        {
            if (total > 0)
            {
                int displayIndex = currentIndex + 1; // +1 для отображения с 1, а не с 0
                searchResultsCountText.Text = $"{displayIndex} из {total}";
            }
            else
            {
                searchResultsCountText.Text = "0 из 0";
            }
        }
        
        // Анимация выделения найденного сообщения
        public void AnimateSearchHighlight(FrameworkElement message)
        {
            try
            {
                Border messageBubble = null;
                TextBlock messageTextBlock = null;
                
                // Находим контейнеры сообщений и их содержимое в зависимости от типа
                if (message is StackPanel outerPanel)
                {
                    // Для исходящих сообщений - находим непосредственно пузырек в StackPanel
                    messageBubble = UIMessageFactory.FindFirstBorder(outerPanel);
                    
                    if (messageBubble != null && messageBubble.Child is StackPanel contentPanel)
                    {
                        messageTextBlock = UIMessageFactory.FindTextBlock(contentPanel);
                    }
                }
                else if (message is Grid outerGrid)
                {
                    // Для входящих сообщений - более сложная структура
                    // Нам нужно найти Grid внутри Grid, который содержит пузырек сообщения
                    // Перебираем дочерние элементы и ищем нужный Border
                    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(outerGrid); i++)
                    {
                        var child = VisualTreeHelper.GetChild(outerGrid, i);
                        
                        // Если это Grid с сообщением
                        if (child is Grid messageGrid)
                        {
                            messageBubble = UIMessageFactory.FindFirstBorder(messageGrid);
                            if (messageBubble != null)
                            {
                                // Нашли пузырек сообщения
                                if (messageBubble.Child is StackPanel contentPanel)
                                {
                                    messageTextBlock = UIMessageFactory.FindTextBlock(contentPanel);
                                }
                                break;
                            }
                        }
                        // Проверяем, не нашли ли мы Border непосредственно
                        else if (child is Border childBorder)
                        {
                            messageBubble = childBorder;
                            if (messageBubble.Child is StackPanel contentPanel)
                            {
                                messageTextBlock = UIMessageFactory.FindTextBlock(contentPanel);
                            }
                            break;
                        }
                    }
                }
                
                // Если нашли пузырек сообщения
                if (messageBubble != null)
                {
                    // Запоминаем оригинальный фон
                    Brush originalBackground = messageBubble.Background;
                    
                    // Цвет для выделения сообщения
                    Color highlightColor = Color.FromRgb(255, 240, 70); // Яркий желтый
                    
                    // Анимация для фона сообщения
                    if (originalBackground is SolidColorBrush backgroundBrush)
                    {
                        Color originalColor = backgroundBrush.Color;
                        
                        // Создаем анимацию фона
                        var backgroundAnimation = new ColorAnimation
                        {
                            From = highlightColor,
                            To = originalColor,
                            Duration = TimeSpan.FromMilliseconds(2000),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };
                        
                        // Применяем новый цвет с анимацией
                        SolidColorBrush animationBrush = new SolidColorBrush(highlightColor);
                        messageBubble.Background = animationBrush;
                        animationBrush.BeginAnimation(SolidColorBrush.ColorProperty, backgroundAnimation);
                    }
                    
                    // Если нашли текстовый блок, выделяем и его
                    if (messageTextBlock != null)
                    {
                        // Запоминаем оригинальный цвет текста
                        Brush originalTextForeground = messageTextBlock.Foreground;
                        
                        // Анимация для текста
                        if (originalTextForeground is SolidColorBrush textBrush)
                        {
                            Color originalTextColor = textBrush.Color;
                            Color highlightTextColor = Color.FromRgb(0, 0, 0); // Черный
                            
                            var textAnimation = new ColorAnimation
                            {
                                From = highlightTextColor,
                                To = originalTextColor,
                                Duration = TimeSpan.FromMilliseconds(2000),
                                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                            };
                            
                            // Применяем новый цвет с анимацией
                            SolidColorBrush textAnimationBrush = new SolidColorBrush(highlightTextColor);
                            messageTextBlock.Foreground = textAnimationBrush;
                            textAnimationBrush.BeginAnimation(SolidColorBrush.ColorProperty, textAnimation);
                        }
                    }
                    
                    // Мигаем границей для привлечения внимания
                    Brush originalBorderBrush = messageBubble.BorderBrush;
                    double originalBorderThickness = messageBubble.BorderThickness.Left;
                    
                    // Устанавливаем более толстую и яркую границу
                    messageBubble.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // Оранжевый
                    messageBubble.BorderThickness = new Thickness(2);
                    
                    // Анимация возврата границы к исходной
                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromMilliseconds(2000);
                    timer.Tick += (s, e) => 
                    {
                        messageBubble.BorderBrush = originalBorderBrush;
                        messageBubble.BorderThickness = new Thickness(originalBorderThickness);
                        timer.Stop();
                    };
                    timer.Start();
                }
                
                // Прокрутка к сообщению
                ScrollToMessage(message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при выделении сообщения: {ex.Message}");
            }
        }
        
        // Запуск таймера неактивности поиска
        public void StartSearchInactivityTimer()
        {
            searchInactivityTimer.Start();
        }
    }
} 