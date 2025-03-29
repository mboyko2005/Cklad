using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using УправлениеСкладом.Class;

namespace УправлениеСкладом.Class
{
    public class SearchService
    {
        private SearchManager searchManager;
        private MessengerUIManager uiManager;
        private Dictionary<int, FrameworkElement> messageElements;
        private DispatcherTimer searchDelayTimer;
        
        private List<SearchResult> searchResults = new List<SearchResult>();
        private int currentSearchResultIndex = -1;
        private string currentSearchQuery = "";
        private int selectedContactId;
        
        public SearchService(
            SearchManager searchManager, 
            MessengerUIManager uiManager, 
            Dictionary<int, FrameworkElement> messageElements)
        {
            this.searchManager = searchManager;
            this.uiManager = uiManager;
            this.messageElements = messageElements;
            
            // Инициализируем таймер задержки поиска
            this.searchDelayTimer = new DispatcherTimer();
            this.searchDelayTimer.Interval = TimeSpan.FromMilliseconds(500);
            this.searchDelayTimer.Tick += SearchDelayTimer_Tick;
        }
        
        public void SetSelectedContactId(int contactId)
        {
            this.selectedContactId = contactId;
        }
        
        // Обработчик изменения текста в поисковом поле
        public void HandleSearchTextChanged(string searchText, Action startInactivityTimer)
        {
            if (selectedContactId <= 0)
            {
                MessageBox.Show("Сначала выберите чат для поиска сообщений.");
                return;
            }
            
            searchText = searchText.Trim();
            
            // Если строка поиска пуста, сбрасываем результаты и запускаем таймер неактивности
            if (string.IsNullOrEmpty(searchText))
            {
                ClearSearchResults();
                startInactivityTimer();
                return;
            }
            
            // Запоминаем текущий запрос
            currentSearchQuery = searchText;
            
            // Сбрасываем таймер при каждом изменении текста
            searchDelayTimer.Stop();
            searchDelayTimer.Start();
        }
        
        // Обработчик тика таймера задержки поиска
        private void SearchDelayTimer_Tick(object sender, EventArgs e)
        {
            searchDelayTimer.Stop();
            PerformSearch(currentSearchQuery);
        }
        
        // Выполнение поиска по сообщениям
        public async void PerformSearch(string searchQuery)
        {
            if (selectedContactId <= 0 || string.IsNullOrEmpty(searchQuery))
                return;
            
            // Очищаем предыдущие результаты
            ClearSearchHighlights();
            
            try
            {
                // Получаем результаты поиска
                searchResults = await searchManager.SearchMessagesAsync(selectedContactId, searchQuery);
                
                // Обновляем счетчик результатов
                uiManager.UpdateSearchResultsCount(currentSearchResultIndex, searchResults.Count);
                
                // Если есть результаты, выделяем первый
                if (searchResults.Count > 0)
                {
                    currentSearchResultIndex = 0;
                    HighlightSearchResult(currentSearchResultIndex);
                }
                else
                {
                    currentSearchResultIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Выделение текущего результата поиска
        public void HighlightSearchResult(int index)
        {
            if (index < 0 || index >= searchResults.Count)
                return;
            
            // Получаем ID сообщения для выделения
            int messageId = searchResults[index].MessageId;
            
            // Проверяем, отображается ли это сообщение
            if (messageElements.TryGetValue(messageId, out var element))
            {
                // Выделяем сообщение с небольшой задержкой, чтобы обеспечить прокрутку
                // Сначала прокручиваем к найденному сообщению
                uiManager.ScrollToElement(element);
                
                // Задержка перед анимацией для гарантированного отображения
                DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    // Выделяем сообщение после прокрутки
                    uiManager.AnimateSearchHighlight(element);
                };
                timer.Start();
                
                // Обновляем счетчик
                uiManager.UpdateSearchResultsCount(currentSearchResultIndex, searchResults.Count);
            }
        }
        
        // Очистка выделения результатов поиска
        public void ClearSearchHighlights()
        {
            // В данной реализации выделение автоматически исчезает после анимации
        }
        
        // Очистка результатов поиска
        public void ClearSearchResults()
        {
            searchResults.Clear();
            currentSearchResultIndex = -1;
            uiManager.UpdateSearchResultsCount(-1, 0);
            ClearSearchHighlights();
        }
        
        // Переход к предыдущему результату поиска
        public void NavigateToPreviousResult()
        {
            if (searchResults.Count == 0)
                return;
            
            // Переходим к предыдущему результату
            currentSearchResultIndex--;
            if (currentSearchResultIndex < 0)
                currentSearchResultIndex = searchResults.Count - 1;
            
            // Выделяем результат
            HighlightSearchResult(currentSearchResultIndex);
        }
        
        // Переход к следующему результату поиска
        public void NavigateToNextResult()
        {
            if (searchResults.Count == 0)
                return;
            
            // Переходим к следующему результату
            currentSearchResultIndex++;
            if (currentSearchResultIndex >= searchResults.Count)
                currentSearchResultIndex = 0;
            
            // Выделяем результат
            HighlightSearchResult(currentSearchResultIndex);
        }
        
        // Обработка выбора результата из списка
        public void HandleSearchResultSelection(int selectedIndex)
        {
            if (selectedIndex >= 0 && selectedIndex < searchResults.Count)
            {
                currentSearchResultIndex = selectedIndex;
                HighlightSearchResult(currentSearchResultIndex);
            }
        }
        
        // Получение текущего поискового запроса
        public string GetCurrentSearchQuery()
        {
            return currentSearchQuery;
        }
        
        // Получение списка результатов поиска
        public List<SearchResult> GetSearchResults()
        {
            return searchResults;
        }
    }
} 