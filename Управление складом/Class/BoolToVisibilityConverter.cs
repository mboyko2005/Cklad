using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace УправлениеСкладом
{
	public class BoolToVisibilityConverter : IValueConverter
	{
		// Преобразование bool в Visibility
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool boolValue = (bool)value;
			if (parameter != null && bool.TryParse(parameter.ToString(), out bool invert) && invert)
			{
				boolValue = !boolValue;
			}
			return boolValue ? Visibility.Visible : Visibility.Collapsed;
		}

		// Преобразование Visibility обратно в bool (не используется)
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			Visibility vis = (Visibility)value;
			bool result = vis == Visibility.Visible;
			if (parameter != null && bool.TryParse(parameter.ToString(), out bool invert) && invert)
			{
				result = !result;
			}
			return result;
		}
	}
}
