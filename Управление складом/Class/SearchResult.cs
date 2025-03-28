using System;

namespace УправлениеСкладом.Class
{
    /// <summary>
    /// Класс для представления результата поиска сообщений
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// ID сообщения
        /// </summary>
        public int MessageId { get; set; }
        
        /// <summary>
        /// ID отправителя
        /// </summary>
        public int SenderId { get; set; }
        
        /// <summary>
        /// Текст сообщения
        /// </summary>
        public string Text { get; set; }
        
        /// <summary>
        /// Дата отправки
        /// </summary>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// Признак исходящего сообщения
        /// </summary>
        public bool IsOutgoing { get; set; }
        
        /// <summary>
        /// Отформатированный текст с выделенным поисковым запросом
        /// </summary>
        public string HighlightedText { get; set; }
        
        /// <summary>
        /// Форматирует текст с выделением поискового запроса
        /// </summary>
        /// <param name="searchQuery">Поисковый запрос</param>
        public void HighlightQuery(string searchQuery)
        {
            if (string.IsNullOrEmpty(Text) || string.IsNullOrEmpty(searchQuery))
            {
                HighlightedText = Text;
                return;
            }
            
            int index = Text.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                // Простое форматирование для выделения текста
                HighlightedText = Text.Substring(0, index) + 
                                 "[HIGHLIGHT]" + Text.Substring(index, searchQuery.Length) + "[/HIGHLIGHT]" + 
                                 Text.Substring(index + searchQuery.Length);
            }
            else
            {
                HighlightedText = Text;
            }
        }
    }
} 