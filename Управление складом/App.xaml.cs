using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace УправлениеСкладом
{
	/// <summary>
	/// Для сжатие в единый exe файл
	/// dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:UseAppHost=true -p:PublishTrimmed=false -o publish
	/// </summary>
	public partial class App : Application
	{
		private IHost _webHost;

		/// <summary>
		/// Метод запуска приложения.
		/// Здесь конфигурируется и запускается Kestrel-сервер, который раздаёт статические файлы из папки "УправлениеСкладомWEB".
		/// По умолчанию при обращении к корню возвращается login.html.
		/// После старта сервера открывается браузер с указанным адресом.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			_webHost = Host.CreateDefaultBuilder(e.Args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					// Слушаем на http://localhost:8080/
					webBuilder.UseUrls("http://localhost:8080");

					// Регистрируем сервисы (включая контроллеры) и настраиваем CORS
					webBuilder.ConfigureServices(services =>
					{
						services.AddControllers();

						// Включаем CORS-политику "MyCors": разрешаем запросы с любого источника, любые методы и заголовки
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

					// Конфигурируем пайплайн обработки запросов
					webBuilder.Configure(app =>
					{
						// Определяем путь к папке со статическими файлами (наш сайт)
						string siteFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "УправлениеСкладомWEB");

						// Настройка для использования файла по умолчанию (login.html)
						var defaultFilesOptions = new DefaultFilesOptions
						{
							FileProvider = new PhysicalFileProvider(siteFolder)
						};
						// Очищаем стандартный список и указываем, что по умолчанию используется именно login.html
						defaultFilesOptions.DefaultFileNames.Clear();
						defaultFilesOptions.DefaultFileNames.Add("login.html");

						// Подключаем middleware для файлов по умолчанию и для статических файлов
						// Важно: UseDefaultFiles должен идти перед UseStaticFiles
						app.UseDefaultFiles(defaultFilesOptions);
						app.UseStaticFiles(new StaticFileOptions
						{
							FileProvider = new PhysicalFileProvider(siteFolder)
						});

						// Подключаем маршрутизацию
						app.UseRouting();

						// Применяем нашу CORS-политику
						app.UseCors("MyCors");

						// Мапим контроллеры (например, AuthController и т.д.)
						app.UseEndpoints(endpoints =>
						{
							endpoints.MapControllers();
						});
					});
				})
				.Build();

			// Запускаем Kestrel-сервер
			_webHost.Start();

			// Автоматически открываем браузер по адресу локального сервера
			Process.Start(new ProcessStartInfo("http://localhost:8080") { UseShellExecute = true });
		}

		/// <summary>
		/// Останавливаем и освобождаем ресурсы веб-сервера при выходе из приложения.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnExit(ExitEventArgs e)
		{
			_webHost?.StopAsync().Wait();
			_webHost?.Dispose();
			base.OnExit(e);
		}
	}
}
