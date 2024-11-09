using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.IconPacks;
using System.Windows.Threading;
using Управление_складом.Themes;

namespace УправлениеСкладом
{
	public partial class MainWindow : Window, INotifyPropertyChanged, IThemeable
	{
		private string _password = string.Empty; // Инициализируем пустой строкой
		private bool _isPasswordVisible;
		private PackIconMaterialKind _eyeIcon;
		private DispatcherTimer _passwordVisibilityTimer;

		public string Password
		{
			get => _password;
			set
			{
				_password = value;
				OnPropertyChanged();
			}
		}

		public bool IsPasswordVisible
		{
			get => _isPasswordVisible;
			set
			{
				_isPasswordVisible = value;
				OnPropertyChanged();
				UpdateEyeIcon();

				// Запускаем или останавливаем таймер в зависимости от состояния
				if (_isPasswordVisible)
				{
					_passwordVisibilityTimer.Start();
				}
				else
				{
					_passwordVisibilityTimer.Stop();
				}
			}
		}

		public PackIconMaterialKind EyeIcon
		{
			get => _eyeIcon;
			set
			{
				_eyeIcon = value;
				OnPropertyChanged();
			}
		}

		public MainWindow()
		{
			InitializeComponent();
			UsernameTextBox.Focus();
			DataContext = this;

			// Инициализация таймера для автоматического скрытия пароля
			_passwordVisibilityTimer = new DispatcherTimer();
			_passwordVisibilityTimer.Interval = TimeSpan.FromSeconds(20);
			_passwordVisibilityTimer.Tick += (s, e) =>
			{
				IsPasswordVisible = false;
				_passwordVisibilityTimer.Stop();
			};

			IsPasswordVisible = false;
			UpdateEyeIcon();

			// Обновляем иконку темы при загрузке
			UpdateThemeIcon();
		}

		private void UpdateEyeIcon()
		{
			// Иконка глаза открыта, когда пароль видим
			EyeIcon = IsPasswordVisible ? PackIconMaterialKind.EyeOff : PackIconMaterialKind.Eye;
		}

		private void TogglePasswordVisibility(object sender, RoutedEventArgs e)
		{
			IsPasswordVisible = !IsPasswordVisible;
		}

		private void LoginButton_Click(object sender, RoutedEventArgs e)
		{
			string username = UsernameTextBox.Text;
			string password = Password;

			User user = DatabaseHelper.AuthenticateUser(username, password);

			if (user != null)
			{
				// Определение роли по RoleID
				string roleName = GetRoleName(user.RoleID);

				IRoleWindow roleWindow = RoleWindowFactory.CreateWindow(roleName);
				if (roleWindow != null)
				{
					roleWindow.ShowWindow();
					Close(); // Закрываем окно авторизации
				}
				else
				{
					MessageBox.Show("Неизвестная роль пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			else
			{
				MessageBox.Show("Неверный логин или пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private string GetRoleName(int roleID)
		{
			switch (roleID)
			{
				case 1:
					return "Администратор";
				case 2:
					return "Менеджер";
				case 3:
					return "Сотрудник склада";
				default:
					return string.Empty;
			}
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}

		private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				LoginButton_Click(sender, e);
			}
		}

		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			// Используем ThemeManager для переключения темы
			ThemeManager.ToggleTheme();

			// Обновляем иконку темы в текущем окне
			UpdateThemeIcon();
		}

		public void UpdateThemeIcon()
		{
			if (ThemeIcon != null)
			{
				ThemeIcon.Kind = ThemeManager.IsDarkTheme ? PackIconMaterialKind.WeatherNight : PackIconMaterialKind.WeatherSunny;
			}
		}

		// Обработчик события для ссылки "Забыли пароль?"
		private void ForgotPassword_Click(object sender, RoutedEventArgs e)
		{
			// Укажите вашу ссылку в Telegram здесь
			string telegramLink = "https://t.me/HolyRider17"; // Замените на вашу ссылку

			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = telegramLink,
					UseShellExecute = true
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show("Не удалось открыть ссылку: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
