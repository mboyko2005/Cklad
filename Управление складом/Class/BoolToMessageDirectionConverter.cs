using System;
using System.Globalization;
using System.Windows.Data;

namespace УправлениеСкладом.Class
{
    /// <summary>
    /// Конвертер для преобразования булевого значения (isOutgoing) в текстовое представление направления сообщения
    /// </summary>
    public class BoolToMessageDirectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isOutgoing)
            {
                return isOutgoing ? "Отправлено" : "Получено";
            }
            return "Неизвестно";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == "Отправлено";
        }
    }
} 