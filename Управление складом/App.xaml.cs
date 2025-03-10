using System;
using System.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace УправлениеСкладом
{
	public partial class App : Application
	{
		private IHost _webHost;

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			_webHost = Host.CreateDefaultBuilder(e.Args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					// Слушаем на http://localhost:8080/
					webBuilder.UseUrls("http://localhost:8080");

					// Регистрируем сервисы (включая контроллеры) и разрешаем CORS
					webBuilder.ConfigureServices(services =>
					{
						services.AddControllers();

						// Включаем CORS-политику "MyCors" -> AllowAnyOrigin / AnyMethod / AnyHeader
						services.AddCors(options =>
						{
							options.AddPolicy("MyCors", builder =>
							{
								builder
									.AllowAnyOrigin()
									.AllowAnyMethod()
									.AllowAnyHeader();
							});
						});
					});

					// Подключаем маршруты контроллеров и применяем CORS
					webBuilder.Configure(app =>
					{
						app.UseRouting();

						// Применяем нашу CORS-политику
						app.UseCors("MyCors");

						// Мапим контроллеры (AuthController и т.д.)
						app.UseEndpoints(endpoints =>
						{
							endpoints.MapControllers();
						});
					});
				})
				.Build();

			// Запускаем Kestrel
			_webHost.Start();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			_webHost?.StopAsync().Wait();
			_webHost?.Dispose();
			base.OnExit(e);
		}
	}
}
