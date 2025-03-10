using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace УправлениеСкладом
{
	public partial class ApiStatusWindow : Window
	{
		public ApiStatusWindow()
		{
			InitializeComponent();
			Loaded += ApiStatusWindow_Loaded;
		}

		private async void ApiStatusWindow_Loaded(object sender, RoutedEventArgs e)
		{
			await CheckSiteStatusAsync();
		}

		private async void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			await CheckSiteStatusAsync();
		}

		/// <summary>
		/// Проверяет, доступен ли локальный сайт (сервер) по адресу http://localhost:8080/api/auth/ping.
		/// Если сервер отвечает с кодом 200 – сайт запущен, иначе выводит код ошибки.
		/// </summary>
		private async Task CheckSiteStatusAsync()
		{
			try
			{
				using (HttpClient client = new HttpClient())
				{
					// Изменили URL на тот, который гарантированно существует (ping-метод контроллера)
					HttpResponseMessage response = await client.GetAsync("http://localhost:8080/api/auth/ping");

					if (response.IsSuccessStatusCode)
					{
						ApiStatusIndicator.Foreground = Brushes.Green;
						StatusTextBlock.Text = "Локальный сайт работает корректно!";
					}
					else
					{
						ApiStatusIndicator.Foreground = Brushes.Red;
						StatusTextBlock.Text = $"Сайт отвечает, но ошибка: {response.StatusCode}";
					}
				}
			}
			catch (HttpRequestException ex)
			{
				ApiStatusIndicator.Foreground = Brushes.Red;
				StatusTextBlock.Text = "Ошибка обращения к локальному сайту: " + ex.Message;
			}
			catch (Exception ex)
			{
				ApiStatusIndicator.Foreground = Brushes.Red;
				StatusTextBlock.Text = "Ошибка: " + ex.Message;
			}
		}

		// Перетаскивание окна при зажатии ЛКМ
		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}

		// Закрытие окна по нажатию на крестик
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
