// ThemeManager.cs
using System;
using System.Linq;
using System.Windows;
using Управление_складом.Themes;

namespace УправлениеСкладом
{
	public static class ThemeManager
	{
		private static bool _isDarkTheme = false;

		public static void ToggleTheme()
		{
			// Определяем, какая тема сейчас активна
			string themeSource = _isDarkTheme ? "Themes/LightTheme.xaml" : "Themes/DarkTheme.xaml";

			// Получаем коллекцию объединённых словарей ресурсов
			var mergedDictionaries = Application.Current.Resources.MergedDictionaries;

			// Ищем текущий словарь темы
			var themeDictionary = mergedDictionaries.FirstOrDefault(d => d.Source != null &&
				(d.Source.OriginalString.EndsWith("LightTheme.xaml") || d.Source.OriginalString.EndsWith("DarkTheme.xaml")));

			if (themeDictionary != null)
			{
				// Удаляем текущий словарь темы
				mergedDictionaries.Remove(themeDictionary);
			}

			// Добавляем новый словарь темы
			mergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(themeSource, UriKind.Relative) });

			// Переключаем флаг темы
			_isDarkTheme = !_isDarkTheme;

			// Обновляем иконку темы во всех окнах
			UpdateThemeIcons();
		}

		private static void UpdateThemeIcons()
		{
			foreach (Window window in Application.Current.Windows)
			{
				if (window is IThemeable themeableWindow)
				{
					themeableWindow.UpdateThemeIcon();
				}
			}
		}

		public static bool IsDarkTheme => _isDarkTheme;
	}
}
