using System;
using System.Collections.Generic;
using System.Drawing;           // System.Drawing.Common
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient; // Microsoft.Data.SqlClient
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace УправлениеСкладом.Class
{
	public static class TelegramNotifier
	{
		// Подключение к БД. В таблице СкладскиеПозиции в поле QRText хранится текст QR‑кода.
		private static readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True;TrustServerCertificate=True";

		private static string BotToken;
		private static long EmployeeChatId;
		private static long ManagerChatId;
		private static TelegramBotClient _botClient;

		// Главное меню бота с кнопками для вывода товаров, сотрудников, суммы товаров и QR‑сканера.
		private static InlineKeyboardMarkup MainMenuKeyboard => new InlineKeyboardMarkup(new[]
		{
			new []
			{
				InlineKeyboardButton.WithCallbackData("Все товары", "view_products"),
				InlineKeyboardButton.WithCallbackData("Все сотрудники", "view_employees")
			},
			new []
			{
				InlineKeyboardButton.WithCallbackData("Количество товаров", "view_total_products")
			},
			new []
			{
				InlineKeyboardButton.WithCallbackData("QR сканер", "qr_scan")
			}
		});

		// Статический конструктор: загрузка настроек из БД.
		static TelegramNotifier()
		{
			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();

					// Получаем API токен бота
					using (var cmdToken = new SqlCommand("SELECT TOP 1 APIToken FROM TelegramBotSettings", conn))
					{
						object result = cmdToken.ExecuteScalar();
						if (result != null)
							BotToken = result.ToString();
						else
							throw new Exception("API токен не найден в таблице TelegramBotSettings.");
					}

					// Получаем ChatID для пользователей
					using (var cmdUsers = new SqlCommand("SELECT TelegramUserID, Роль FROM TelegramUsers", conn))
					{
						using (var reader = cmdUsers.ExecuteReader())
						{
							while (reader.Read())
							{
								long telegramUserId = reader.GetInt64(0);
								string role = reader.GetString(1);
								if (string.Equals(role, "Администратор", StringComparison.OrdinalIgnoreCase))
									EmployeeChatId = telegramUserId;
								else if (string.Equals(role, "Менеджер", StringComparison.OrdinalIgnoreCase))
									ManagerChatId = telegramUserId;
							}
						}
					}
				}
				_botClient = new TelegramBotClient(BotToken);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Ошибка инициализации TelegramNotifier: " + ex.Message);
				throw;
			}
		}

		/// <summary>
		/// Отправляет текстовое уведомление.
		/// </summary>
		public static async Task SendNotificationAsync(string message, bool toManager = false)
		{
			long chatId = toManager ? ManagerChatId : EmployeeChatId;
			try
			{
				await _botClient.SendMessage(chatId, message);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Ошибка отправки уведомления: " + ex.Message);
			}
		}

		/// <summary>
		/// Запускает бота (получение обновлений).
		/// </summary>
		public static void StartBot()
		{
			_botClient.StartReceiving(
				updateHandler: HandleUpdateAsync,
				errorHandler: HandleErrorAsync,
				cancellationToken: CancellationToken.None);
			Console.WriteLine("Бот запущен и получает обновления...");
		}

		/// <summary>
		/// Главный обработчик обновлений: текст, фото и callback‑запросы.
		/// </summary>
		private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
		{
			try
			{
				if (update.Type == UpdateType.Message && update.Message.Text != null)
				{
					await HandleTextMessage(botClient, update.Message, cancellationToken);
				}
				else if (update.Type == UpdateType.Message && update.Message.Photo != null)
				{
					await HandlePhotoMessage(botClient, update.Message, cancellationToken);
				}
				else if (update.Type == UpdateType.CallbackQuery)
				{
					await HandleCallbackQuery(botClient, update.CallbackQuery, cancellationToken);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Ошибка обработки обновления: " + ex.Message);
			}
		}

		private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
		{
			Console.WriteLine($"Ошибка бота: {exception.Message}");
			return Task.CompletedTask;
		}

		/// <summary>
		/// Обрабатывает текстовые сообщения (/start, /menu).
		/// </summary>
		private static async Task HandleTextMessage(ITelegramBotClient botClient, Message message, CancellationToken token)
		{
			if (message.From.Id == EmployeeChatId)
			{
				if (message.Text.Equals("/start", StringComparison.OrdinalIgnoreCase))
				{
					await botClient.SendMessage(message.Chat.Id,
						"Добро пожаловать! Выберите действие:",
						replyMarkup: MainMenuKeyboard,
						cancellationToken: token);
				}
				else if (message.Text.Equals("/menu", StringComparison.OrdinalIgnoreCase))
				{
					var menuKeyboard = new InlineKeyboardMarkup(new[]
					{
						new []
						{
							InlineKeyboardButton.WithCallbackData("Все товары", "view_products"),
							InlineKeyboardButton.WithCallbackData("Количество товаров", "view_total_products")
						}
					});
					await botClient.SendMessage(message.Chat.Id,
						"Выберите действие:",
						replyMarkup: menuKeyboard,
						cancellationToken: token);
				}
			}
		}

		/// <summary>
		/// Обрабатывает фото (предположительно QR‑код).
		/// Загружает изображение, декодирует QR‑код и ищет товар по полю QRText.
		/// После успешного распознавания добавляется кнопка «Назад»,
		/// при нажатии на которую удаляются сообщение с информацией о товаре и фото пользователя.
		/// </summary>
		private static async Task HandlePhotoMessage(ITelegramBotClient botClient, Message message, CancellationToken token)
		{
			if (message.From.Id != EmployeeChatId)
				return;

			var photo = message.Photo[^1]; // Наивысшее разрешение
			var file = await _botClient.GetFile(photo.FileId, token);
			using (var ms = new MemoryStream())
			{
				await _botClient.DownloadFile(file.FilePath, ms, token);
				byte[] imageBytes = ms.ToArray();
				string qrData = DecodeQrCode(imageBytes);
				if (!string.IsNullOrEmpty(qrData))
				{
					string productInfo = GetProductInfoByQr(qrData);
					// Формируем callback data, включающую ID исходного сообщения с фото
					var backButtonData = "back_qr:" + message.MessageId;
					var replyKeyboard = new InlineKeyboardMarkup(new[]
					{
						new [] { InlineKeyboardButton.WithCallbackData("Назад", backButtonData) }
					});

					if (!string.IsNullOrEmpty(productInfo))
						await botClient.SendMessage(message.Chat.Id, productInfo, replyMarkup: replyKeyboard, cancellationToken: token);
					else
						await botClient.SendMessage(message.Chat.Id, "Товар с данным QR кодом не найден.", replyMarkup: replyKeyboard, cancellationToken: token);
				}
				else
				{
					await botClient.SendMessage(message.Chat.Id, "Не удалось распознать QR код.", cancellationToken: token);
				}
			}
		}

		/// <summary>
		/// Обрабатывает callback‑запросы от inline‑кнопок.
		/// </summary>
		private static async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken token)
		{
			// Обработка нажатия кнопки "Назад" после QR-сканирования.
			if (callbackQuery.Data.StartsWith("back_qr:"))
			{
				// Извлекаем ID сообщения с фото из callback data.
				string idPart = callbackQuery.Data.Substring("back_qr:".Length);
				if (int.TryParse(idPart, out int photoMessageId))
				{
					// Удаляем сообщение с информацией о товаре (текущее сообщение с кнопкой)
					try { await botClient.DeleteMessage(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, token); } catch { }
					// Удаляем сообщение с фотографией, отправленное пользователем
					try { await botClient.DeleteMessage(callbackQuery.Message.Chat.Id, photoMessageId, token); } catch { }
				}
				await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: token);
				return; // Не отправляем новых сообщений – главное меню остаётся на экране.
			}

			string responseText = "";
			InlineKeyboardMarkup replyKeyboard = null;

			if (callbackQuery.Data == "view_products")
			{
				responseText = GetProductsList();
				replyKeyboard = new InlineKeyboardMarkup(new[]
				{
					new []
					{
						InlineKeyboardButton.WithCallbackData("Фильтр по складам", "filter_by_warehouse")
					},
					new []
					{
						InlineKeyboardButton.WithCallbackData("Назад", "back_main")
					}
				});
			}
			else if (callbackQuery.Data == "view_employees")
			{
				responseText = GetEmployeesList();
				replyKeyboard = new InlineKeyboardMarkup(new[]
				{
					new []
					{
						InlineKeyboardButton.WithCallbackData("Назад", "back_main")
					}
				});
			}
			else if (callbackQuery.Data == "view_total_products")
			{
				responseText = GetTotalProducts();
				replyKeyboard = new InlineKeyboardMarkup(new[]
				{
					new []
					{
						InlineKeyboardButton.WithCallbackData("Назад", "back_main")
					}
				});
			}
			else if (callbackQuery.Data == "filter_by_warehouse")
			{
				replyKeyboard = GetWarehouseKeyboard(withBackButton: true);
				responseText = "Выберите склад для фильтрации товаров:";
			}
			else if (callbackQuery.Data.StartsWith("warehouse_"))
			{
				if (int.TryParse(callbackQuery.Data.Substring("warehouse_".Length), out int warehouseId))
				{
					responseText = GetProductsByWarehouse(warehouseId);
					replyKeyboard = new InlineKeyboardMarkup(new[]
					{
						new []
						{
							InlineKeyboardButton.WithCallbackData("Назад", "filter_by_warehouse")
						}
					});
				}
			}
			else if (callbackQuery.Data == "qr_scan")
			{
				responseText = "Отправьте фото QR кода товара.";
				replyKeyboard = new InlineKeyboardMarkup(new[]
				{
					new []
					{
						InlineKeyboardButton.WithCallbackData("Назад", "back_main")
					}
				});
			}
			else if (callbackQuery.Data == "back_main")
			{
				replyKeyboard = MainMenuKeyboard;
				responseText = "Главное меню:";
			}

			if (!string.IsNullOrEmpty(responseText))
			{
				var sentMessage = await botClient.SendMessage(callbackQuery.Message.Chat.Id, responseText, replyMarkup: replyKeyboard, cancellationToken: token);
				try
				{
					await botClient.DeleteMessage(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, token);
				}
				catch { }
			}
			await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: token);
		}

		/// <summary>
		/// Декодирует QR‑код из PNG-изображения с помощью ZXing, используя QRCodeReader напрямую.
		/// </summary>
		private static string DecodeQrCode(byte[] imageBytes)
		{
			try
			{
				using (var ms = new MemoryStream(imageBytes))
				using (var bitmap = new Bitmap(ms))
				{
					var luminanceSource = new BitmapLuminanceSource(bitmap);
					var binarizer = new HybridBinarizer(luminanceSource);
					var binaryBitmap = new ZXing.BinaryBitmap(binarizer);
					var reader = new ZXing.QrCode.QRCodeReader();
					var result = reader.decode(binaryBitmap);
					return result?.Text;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Ошибка декодирования QR кода: " + ex.Message);
				return null;
			}
		}

		/// <summary>
		/// Ищет в БД товар по значению QR‑кода.
		/// В таблице СкладскиеПозиции поле QRText хранит текст QR‑кода.
		/// </summary>
		private static string GetProductInfoByQr(string qrData)
		{
			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					const string query = @"
                        SELECT TOP 1 t.ТоварID, t.Наименование, t.Цена, sp.Количество, s.Наименование AS Склада
                        FROM Товары t
                        INNER JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
                        INNER JOIN Склады s ON sp.СкладID = s.СкладID
                        WHERE sp.QRText = @QRData";
					using (var cmd = new SqlCommand(query, conn))
					{
						cmd.Parameters.AddWithValue("@QRData", qrData);
						using (var reader = cmd.ExecuteReader())
						{
							if (reader.Read())
							{
								int id = reader.GetInt32(0);
								string name = reader.GetString(1);
								decimal price = reader.GetDecimal(2);
								int quantity = reader.GetInt32(reader.GetOrdinal("Количество"));
								string warehouse = reader.GetString(reader.GetOrdinal("Склада"));
								return $"Информация о товаре:\nID: {id}\nНаименование: {name}\nЦена: {price:F2} руб.\nКоличество: {quantity}\nСклад: {warehouse}";
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				return "Ошибка поиска товара: " + ex.Message;
			}
			return null;
		}

		/// <summary>
		/// Возвращает список товаров (суммарное количество по всем складам) в виде форматированного текста.
		/// </summary>
		private static string GetProductsList()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Список товаров:");
			sb.AppendLine("ID   | Название             | Категория      | Цена    | Кол-во");
			sb.AppendLine(new string('-', 70));
			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					const string query = @"
                        SELECT t.ТоварID, t.Наименование, t.Категория, t.Цена, 
                               ISNULL(SUM(sp.Количество), 0) AS TotalQuantity
                        FROM Товары t
                        LEFT JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
                        GROUP BY t.ТоварID, t.Наименование, t.Категория, t.Цена";
					using (var cmd = new SqlCommand(query, conn))
					{
						using (var reader = cmd.ExecuteReader())
						{
							while (reader.Read())
							{
								int id = reader.GetInt32(0);
								string name = reader.GetString(1);
								string category = reader.IsDBNull(2) ? "Без категории" : reader.GetString(2);
								decimal price = reader.GetDecimal(3);
								int totalQuantity = reader.GetInt32(reader.GetOrdinal("TotalQuantity"));
								sb.AppendLine(string.Format("{0,-4} | {1,-20} | {2,-14} | {3,7:F2} | {4,7}",
									id, name, category, price, totalQuantity));
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				return "Ошибка получения списка товаров: " + ex.Message;
			}
			return sb.ToString();
		}

		/// <summary>
		/// Возвращает список сотрудников в виде форматированного текста.
		/// </summary>
		private static string GetEmployeesList()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Список сотрудников:");
			sb.AppendLine("ID   | Имя                 | Роль");
			sb.AppendLine(new string('-', 45));
			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					const string query = @"
                        SELECT u.ПользовательID, u.ИмяПользователя, r.Наименование AS Роль
                        FROM Пользователи u
                        INNER JOIN Роли r ON u.РольID = r.РольID
                        ORDER BY u.ПользовательID";
					using (var cmd = new SqlCommand(query, conn))
					{
						using (var reader = cmd.ExecuteReader())
						{
							while (reader.Read())
							{
								int id = reader.GetInt32(0);
								string username = reader.GetString(1);
								string role = reader.GetString(2);
								sb.AppendLine(string.Format("{0,-4} | {1,-20} | {2,-15}", id, username, role));
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				return "Ошибка получения списка сотрудников: " + ex.Message;
			}
			return sb.ToString();
		}

		/// <summary>
		/// Возвращает общую стоимость товаров и общее количество товаров со всех складов.
		/// </summary>
		private static string GetTotalProducts()
		{
			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					const string query = @"
                        SELECT ISNULL(SUM(sp.Количество * t.Цена), 0) AS TotalValue,
                               ISNULL(SUM(sp.Количество), 0) AS TotalQuantity
                        FROM СкладскиеПозиции sp
                        INNER JOIN Товары t ON sp.ТоварID = t.ТоварID";
					using (var cmd = new SqlCommand(query, conn))
					{
						using (var reader = cmd.ExecuteReader())
						{
							if (reader.Read())
							{
								decimal totalValue = reader.GetDecimal(reader.GetOrdinal("TotalValue"));
								int totalQuantity = reader.GetInt32(reader.GetOrdinal("TotalQuantity"));
								return $"Общая стоимость товаров: {totalValue:F2} руб.\nОбщее количество товаров: {totalQuantity}";
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				return "Ошибка получения данных: " + ex.Message;
			}
			return "Данные не найдены.";
		}

		/// <summary>
		/// Возвращает количество товаров по каждому складу в виде форматированного текста.
		/// </summary>
		private static string GetProductsCountByWarehouse()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Количество товаров по складам:");
			sb.AppendLine("Склад                | Количество");
			sb.AppendLine(new string('-', 35));
			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					const string query = @"
                        SELECT s.Наименование, SUM(sp.Количество) AS Количество
                        FROM СкладскиеПозиции sp
                        INNER JOIN Склады s ON sp.СкладID = s.СкладID
                        GROUP BY s.Наименование";
					using (var cmd = new SqlCommand(query, conn))
					{
						using (var reader = cmd.ExecuteReader())
						{
							while (reader.Read())
							{
								string warehouseName = reader.GetString(0);
								int quantity = reader.GetInt32(1);
								sb.AppendLine(string.Format("{0,-20} | {1,10}", warehouseName, quantity));
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				return "Ошибка получения данных по складам: " + ex.Message;
			}
			return sb.ToString();
		}

		/// <summary>
		/// Формирует клавиатуру со списком складов.
		/// </summary>
		private static InlineKeyboardMarkup GetWarehouseKeyboard(bool withBackButton = false)
		{
			var buttons = new List<InlineKeyboardButton[]>();
			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					const string query = "SELECT СкладID, Наименование FROM Склады ORDER BY Наименование";
					using (var cmd = new SqlCommand(query, conn))
					{
						using (var reader = cmd.ExecuteReader())
						{
							while (reader.Read())
							{
								int warehouseId = reader.GetInt32(0);
								string name = reader.GetString(1);
								var button = InlineKeyboardButton.WithCallbackData(name, $"warehouse_{warehouseId}");
								buttons.Add(new[] { button });
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Ошибка получения складов: " + ex.Message);
			}
			if (withBackButton)
			{
				buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Назад", "back_main") });
			}
			return new InlineKeyboardMarkup(buttons);
		}

		/// <summary>
		/// Возвращает список товаров для заданного склада в виде форматированного текста.
		/// </summary>
		private static string GetProductsByWarehouse(int warehouseId)
		{
			StringBuilder sb = new StringBuilder();
			string warehouseName = "";
			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					using (var cmdWarehouse = new SqlCommand("SELECT Наименование FROM Склады WHERE СкладID = @WarehouseId", conn))
					{
						cmdWarehouse.Parameters.AddWithValue("@WarehouseId", warehouseId);
						object result = cmdWarehouse.ExecuteScalar();
						warehouseName = result != null ? result.ToString() : $"ID {warehouseId}";
					}

					sb.AppendLine($"Товары для склада \"{warehouseName}\":");
					sb.AppendLine("ID   | Название             | Категория      | Цена    | Кол-во");
					sb.AppendLine(new string('-', 70));

					const string query = @"
                        SELECT t.ТоварID, t.Наименование, t.Категория, t.Цена, sp.Количество
                        FROM Товары t
                        INNER JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
                        WHERE sp.СкладID = @WarehouseId";
					using (var cmd = new SqlCommand(query, conn))
					{
						cmd.Parameters.AddWithValue("@WarehouseId", warehouseId);
						using (var reader = cmd.ExecuteReader())
						{
							while (reader.Read())
							{
								int id = reader.GetInt32(0);
								string name = reader.GetString(1);
								string category = reader.IsDBNull(2) ? "Без категории" : reader.GetString(2);
								decimal price = reader.GetDecimal(3);
								int quantity = reader.GetInt32(reader.GetOrdinal("Количество"));
								sb.AppendLine(string.Format("{0,-4} | {1,-20} | {2,-14} | {3,7:F2} | {4,7}",
									id, name, category, price, quantity));
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				return "Ошибка получения товаров по складу: " + ex.Message;
			}
			sb.AppendLine();
			sb.AppendLine("Для возврата в главное меню нажмите кнопку \"Назад\".");
			return sb.ToString();
		}
	}

	/// <summary>
	/// Класс для преобразования Bitmap в массив оттенков серого (luminance) для ZXing.
	/// </summary>
	public class BitmapLuminanceSource : LuminanceSource
	{
		private readonly byte[] luminances;

		public BitmapLuminanceSource(Bitmap bitmap) : base(bitmap.Width, bitmap.Height)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			var rect = new Rectangle(0, 0, width, height);
			var bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			int stride = bmpData.Stride;
			int bytes = Math.Abs(stride) * height;
			byte[] pixelData = new byte[bytes];
			System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, pixelData, 0, bytes);
			bitmap.UnlockBits(bmpData);

			luminances = new byte[width * height];
			for (int y = 0; y < height; y++)
			{
				int offset = y * stride;
				for (int x = 0; x < width; x++)
				{
					int index = offset + x * 4;
					byte b = pixelData[index];
					byte g = pixelData[index + 1];
					byte r = pixelData[index + 2];
					luminances[y * width + x] = (byte)((r * 299 + g * 587 + b * 114) / 1000);
				}
			}
		}

		public override byte[] Matrix => luminances;

		public override byte[] getRow(int y, byte[] row)
		{
			int width = Width;
			if (row == null || row.Length < width)
				row = new byte[width];
			Array.Copy(luminances, y * width, row, 0, width);
			return row;
		}
	}
}
