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
        
        // Добавление новых сообщений в список предложенных
        public void AddSuggestedMessages(params string[] messages)
        {
            if (messages == null || messages.Length == 0)
                return;
                
            foreach (var message in messages)
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
        
        // Очистка предложенных сообщений
        public void ClearSuggestedMessages()
        {
            suggestedMessagesPanel.Children.Clear();
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
        
        // Анимация выделения результата поиска
        public void AnimateSearchHighlight(FrameworkElement element)
        {
            try
            {
                // Находим основной Border сообщения
                Border messageBubble = UIMessageFactory.FindFirstBorderExplicit(element);
                if (messageBubble != null)
                {
                    // Сохраняем оригинальный цвет фона
                    Brush originalBrush = messageBubble.Background;
                    
                    // Создаем анимацию цвета для "вспышки" найденного сообщения
                    ColorAnimation highlightAnimation = new ColorAnimation
                    {
                        From = Colors.Yellow,
                        To = ((SolidColorBrush)originalBrush).Color,
                        Duration = TimeSpan.FromMilliseconds(1500),
                        AutoReverse = false,
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    
                    // Создаем новую кисть для анимации
                    SolidColorBrush animationBrush = new SolidColorBrush(((SolidColorBrush)originalBrush).Color);
                    messageBubble.Background = animationBrush;
                    
                    // Выполняем анимацию
                    animationBrush.BeginAnimation(SolidColorBrush.ColorProperty, highlightAnimation);
                    
                    // Также слегка увеличим и вернем к исходному размеру
                    ScaleTransform scaleTransform = new ScaleTransform(1, 1);
                    messageBubble.RenderTransform = scaleTransform;
                    
                    // Анимация увеличения
                    DoubleAnimation scaleXAnimation = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 1.05,
                        Duration = TimeSpan.FromMilliseconds(200),
                        AutoReverse = true
                    };
                    
                    DoubleAnimation scaleYAnimation = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 1.05,
                        Duration = TimeSpan.FromMilliseconds(200),
                        AutoReverse = true
                    };
                    
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при анимации выделения: {ex.Message}");
            }
        }
        
        // Запуск таймера неактивности поиска
        public void StartSearchInactivityTimer()
        {
            searchInactivityTimer.Start();
        }

        // Метод для прокрутки к конкретному элементу в чате
        public void ScrollToElement(FrameworkElement element)
        {
            if (element == null) return;
            
            try 
            {
                // Находим ScrollViewer, в котором находятся сообщения
                ScrollViewer scrollViewer = FindParentScrollViewer(messagesPanel);
                if (scrollViewer != null)
                {
                    // Прокручиваем максимально точно к найденному элементу
                    
                    // Вариант 1: используем BringIntoView для надежной прокрутки
                    element.BringIntoView();
                    
                    // Для надежности добавляем небольшую задержку и вызываем BringIntoView еще раз
                    // Это помогает в случаях, когда первый вызов не сработал из-за расчета макета
                    DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        element.BringIntoView();
                        
                        // Определяем позицию элемента и устанавливаем скролл чуть выше него
                        Point elementPosition = element.TranslatePoint(new Point(0, 0), scrollViewer);
                        // Отступаем 50 пикселей вверх для лучшей видимости
                        double targetOffset = scrollViewer.VerticalOffset + elementPosition.Y - 50;
                        scrollViewer.ScrollToVerticalOffset(targetOffset);
                    };
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при прокрутке к элементу: {ex.Message}");
            }
        }

        // Метод для нахождения родительского ScrollViewer
        private ScrollViewer FindParentScrollViewer(DependencyObject child)
        {
            // Проверяем сначала текущий элемент
            if (child is ScrollViewer scrollViewer)
                return scrollViewer;
            
            // Получаем родительский элемент
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            
            // Если родитель не найден, возвращаем null
            if (parent == null)
                return null;
            
            // Рекурсивно ищем ScrollViewer среди родителей
            return FindParentScrollViewer(parent);
        }
    }
} 