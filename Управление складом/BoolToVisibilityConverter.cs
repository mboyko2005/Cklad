using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace УправлениеСкладом
{
	public class BoolToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool boolValue = (bool)value;
			bool invert = parameter != null && bool.Parse(parameter.ToString());
			if (invert)
			{
				boolValue = !boolValue;
			}
			return boolValue ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			Visibility vis = (Visibility)value;
			return vis == Visibility.Visible;
		}
	}
}
