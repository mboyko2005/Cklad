// ManageInventoryWindow.xaml.cs
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;

namespace УправлениеСкладом
{
	public partial class ManageInventoryWindow : Window, IThemeable
	{
		// Пример модели складской позиции
		public class InventoryItem
		{
			public int Id { get; set; }
			public string Name { get; set; }
			public int Quantity { get; set; }
			public string Location { get; set; }
			// Добавьте другие свойства по необходимости
		}

		// Пример списка складских позиций
		private List<InventoryItem> InventoryItems;

		public ManageInventoryWindow()
		{
			InitializeComponent();
			LoadInventory();
		}

		private void LoadInventory()
		{
			// Здесь должна быть логика загрузки складских позиций из базы данных
			// Для примера используем статический список
			InventoryItems = new List<InventoryItem>
			{
				new InventoryItem { Id = 1, Name = "Товар А", Quantity = 100, Location = "Склад 1" },
				new InventoryItem { Id = 2, Name = "Товар Б", Quantity = 50, Location = "Склад 2" },
				new InventoryItem { Id = 3, Name = "Товар В", Quantity = 200, Location = "Склад 1" }
			};

			InventoryDataGrid.ItemsSource = InventoryItems;
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void AddInventory_Click(object sender, RoutedEventArgs e)
		{
			// Логика добавления складской позиции
			// Например, открытие диалогового окна для ввода данных новой позиции
			MessageBox.Show("Функция добавления складской позиции ещё не реализована.", "Добавить позицию", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void EditInventory_Click(object sender, RoutedEventArgs e)
		{
			// Логика редактирования выбранной складской позиции
			if (InventoryDataGrid.SelectedItem is InventoryItem selectedItem)
			{
				MessageBox.Show($"Редактирование позиции: {selectedItem.Name}", "Редактировать позицию", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите позицию для редактирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void DeleteInventory_Click(object sender, RoutedEventArgs e)
		{
			// Логика удаления выбранной складской позиции
			if (InventoryDataGrid.SelectedItem is InventoryItem selectedItem)
			{
				MessageBox.Show($"Удаление позиции: {selectedItem.Name}", "Удалить позицию", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			else
			{
				MessageBox.Show("Пожалуйста, выберите позицию для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			ThemeManager.ToggleTheme();
			UpdateThemeIcon();
		}

		public void UpdateThemeIcon()
		{
			if (ThemeIcon != null)
			{
				ThemeIcon.Kind = ThemeManager.IsDarkTheme ? PackIconMaterialKind.WeatherNight : PackIconMaterialKind.WeatherSunny;
			}
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}
	}
}
