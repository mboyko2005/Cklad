using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Collections.Generic;

namespace УправлениеСкладом.Class
{
    public class SearchUIManager
    {
        private Window parentWindow;
        private SearchService searchService;
        private TextBox searchTextBox;
        private Border searchPanelBorder;
        private TextBlock searchResultsCountText;
        private ListBox searchResultsList;
        private MessengerUIManager uiManager;
        private DispatcherTimer searchInactivityTimer;

        public SearchUIManager(
            Window parentWindow,
            SearchService searchService,
            TextBox searchTextBox,
            Border searchPanelBorder,
            TextBlock searchResultsCountText,
            ListBox searchResultsList,
            MessengerUIManager uiManager)
        {
            this.parentWindow = parentWindow;
            this.searchService = searchService;
            this.searchTextBox = searchTextBox;
            this.searchPanelBorder = searchPanelBorder;
            this.searchResultsCountText = searchResultsCountText;
            this.searchResultsList = searchResultsList;
            this.uiManager = uiManager;
            
            // Инициализация таймера неактивности поиска
            InitializeSearchInactivityTimer();
        }
        
        // Инициализация таймера неактивности поиска
        private void InitializeSearchInactivityTimer()
        {
            searchInactivityTimer = new DispatcherTimer();
            searchInactivityTimer.Interval = TimeSpan.FromSeconds(5);
            searchInactivityTimer.Tick += (s, e) =>
            {
                // Если поисковое поле пустое, скрываем панель
                if (string.IsNullOrWhiteSpace(searchTextBox.Text))
                {
                    uiManager.ToggleSearchPanel(false);
                    searchInactivityTimer.Stop();
                }
            };
        }
        
        // Обработчик изменения текста в поисковом поле
        public void HandleSearchTextChanged(string searchText)
        {
            searchService.HandleSearchTextChanged(
                searchText,
                StartSearchInactivityTimer
            );
        }
        
        // Запуск таймера неактивности поиска
        public void StartSearchInactivityTimer()
        {
            searchInactivityTimer.Stop();
            searchInactivityTimer.Start();
        }
        
        // Обработчик нажатия клавиш в поисковом поле
        public void HandleSearchKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                string searchText = searchTextBox.Text.Trim();
                
                // Если поисковая строка пуста, закрываем поиск
                if (string.IsNullOrEmpty(searchText))
                {
                    uiManager.ToggleSearchPanel(false);
                    return;
                }
                
                // Если уже есть результаты, перейдем к следующему
                var results = searchService.GetSearchResults();
                if (results.Count > 0)
                {
                    if (searchText == searchService.GetCurrentSearchQuery())
                    {
                        // Если запрос не изменился, просто переходим к следующему результату
                        searchService.NavigateToNextResult();
                    }
                    else
                    {
                        // Если запрос изменился, выполняем новый поиск
                        searchService.PerformSearch(searchText);
                    }
                }
                else
                {
                    // Если результатов еще нет, выполняем поиск
                    searchService.PerformSearch(searchText);
                }
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                uiManager.ToggleSearchPanel(false);
            }
        }
        
        // Очистка текста поиска
        public void ClearSearchText()
        {
            searchTextBox.Clear();
            searchService.ClearSearchResults();
            searchTextBox.Focus();
        }
        
        // Переход к предыдущему результату поиска
        public void NavigateToPreviousResult()
        {
            if (searchService.GetSearchResults().Count > 0)
            {
                searchService.NavigateToPreviousResult();
            }
            else if (!string.IsNullOrEmpty(searchTextBox.Text.Trim()))
            {
                // Если нет результатов, но есть поисковой запрос, выполняем поиск
                searchService.PerformSearch(searchTextBox.Text.Trim());
            }
        }
        
        // Переход к следующему результату поиска
        public void NavigateToNextResult()
        {
            if (searchService.GetSearchResults().Count > 0)
            {
                searchService.NavigateToNextResult();
            }
            else if (!string.IsNullOrEmpty(searchTextBox.Text.Trim()))
            {
                // Если нет результатов, но есть поисковой запрос, выполняем поиск
                searchService.PerformSearch(searchTextBox.Text.Trim());
            }
        }
        
        // Выбор элемента в списке результатов
        public void HandleSearchResultSelection(int selectedIndex)
        {
            searchService.HandleSearchResultSelection(selectedIndex);
        }
    }
} 