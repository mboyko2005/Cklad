using System.Windows;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using Управление_складом.Themes;
using Управление_складом.Админ;

namespace УправлениеСкладом
{
	/// <summary>
	/// Окно администратора с функционалом управления пользователями, инвентарем, отчетами, настройками и ботом.
	/// </summary>
	public partial class AdministratorWindow : Window, IRoleWindow, IThemeable
	{
		/// <summary>
		/// Конструктор окна администратора.
		/// Инициализирует компоненты и обновляет иконку текущей темы.
		/// </summary>
		public AdministratorWindow()
		{
			InitializeComponent();
			UpdateThemeIcon();
		}

		/// <summary>
		/// Отображает окно администратора.
		/// </summary>
		public void ShowWindow() => Show();

		/// <summary>
		/// Обработчик нажатия кнопки закрытия.
		/// Открывает главное окно и закрывает текущее окно администратора.
		/// </summary>
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow mainWindow = new MainWindow();
			mainWindow.Show();
			this.Close();
		}

		/// <summary>
		/// Обработчик нажатия кнопки управления пользователями.
		/// Открывает окно управления пользователями в виде модального диалога.
		/// </summary>
		private void ManageUsers_Click(object sender, RoutedEventArgs e)
		{
			ShowDialogWindow(new ManageUsersWindow());
		}

		/// <summary>
		/// Обработчик нажатия кнопки управления инвентарем.
		/// Открывает окно управления инвентарем в виде модального диалога.
		/// </summary>
		private void ManageInventory_Click(object sender, RoutedEventArgs e)
		{
			ShowDialogWindow(new ManageInventoryWindow());
		}

		/// <summary>
		/// Обработчик нажатия кнопки отчетов.
		/// Открывает окно отчетов в виде модального диалога.
		/// </summary>
		private void Reports_Click(object sender, RoutedEventArgs e)
		{
			ShowDialogWindow(new ReportsWindow());
		}

		/// <summary>
		/// Обработчик нажатия кнопки настроек.
		/// Открывает окно настроек в виде модального диалога.
		/// </summary>
		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			ShowDialogWindow(new SettingsWindow());
		}

		/// <summary>
		/// Обработчик нажатия кнопки управления ботом.
		/// Открывает окно управления ботом и закрывает текущее окно администратора.
		/// </summary>
		private void ManageBot_Click(object sender, RoutedEventArgs e)
		{
			var manageBotWindow = new ManageBotWindow();
			manageBotWindow.Show();
			this.Close();
		}

		/// <summary>
		/// Обработчик нажатия кнопки переключения темы.
		/// Переключает тему приложения и обновляет соответствующую иконку.
		/// </summary>
		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			ThemeManager.ToggleTheme();
			UpdateThemeIcon();
		}

		/// <summary>
		/// Обновляет иконку темы в зависимости от текущего режима.
		/// Использует иконку "ночь" для темной темы и "солнце" для светлой.
		/// </summary>
		public void UpdateThemeIcon()
		{
			if (ThemeIcon != null)
			{
				ThemeIcon.Kind = ThemeManager.IsDarkTheme
					? PackIconMaterialKind.WeatherNight
					: PackIconMaterialKind.WeatherSunny;
			}
		}

		/// <summary>
		/// Обработчик события перемещения окна.
		/// Позволяет перетаскивать окно, если нажата левая кнопка мыши.
		/// </summary>
		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}

		#region Вспомогательные методы

		/// <summary>
		/// Устанавливает владельца для переданного окна и открывает его как модальный диалог.
		/// Позволяет избежать дублирования кода для открытия окон.
		/// </summary>
		/// <param name="window">Окно, которое необходимо открыть.</param>
		private void ShowDialogWindow(Window window)
		{
			window.Owner = this;
			window.ShowDialog();
		}

		#endregion
	}
}
