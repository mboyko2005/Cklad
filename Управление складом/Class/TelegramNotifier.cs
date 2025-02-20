using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
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
		private static readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True;TrustServerCertificate=True";

		private static string BotToken;
		// Для примера: храним ChatID администратора и менеджера.
		private static long EmployeeChatId; // Админ
		private static long ManagerChatId;  // Менеджер

		private static TelegramBotClient _botClient;

		/// <summary>
		/// Формирует главное меню в зависимости от роли пользователя.
		/// Общая кнопка «QR сканер» добавляется в конце, чтобы не было дублирования.
		/// </summary>
		private static InlineKeyboardMarkup GetMainMenuForRole(string role)
		{
			string roleLower = role.ToLower().Trim();
			var rows = new List<InlineKeyboardButton[]>();

			if (roleLower == "администратор")
			{
				rows.Add(new[]
				{
					InlineKeyboardButton.WithCallbackData("Все товары", "view_products"),
					InlineKeyboardButton.WithCallbackData("Все сотрудники", "view_employees")
				});
				rows.Add(new[]
				{
					InlineKeyboardButton.WithCallbackData("Количество товаров", "view_total_products")
				});
			}
			else if (roleLower == "менеджер")
			{
				rows.Add(new[]
				{
					InlineKeyboardButton.WithCallbackData("Все товары", "view_products")
				});
			}
			else if (roleLower == "сотрудник склада")
			{
				// Для сотрудников склада никаких дополнительных кнопок не требуется
			}
			else
			{
				rows.Add(new[]
				{
					InlineKeyboardButton.WithCallbackData("Нет доступных команд", "none_cmd")
				});
				return new InlineKeyboardMarkup(rows);
			}
			// Добавляем общую кнопку «QR сканер» (которая есть у всех)
			rows.Add(new[]
			{
				InlineKeyboardButton.WithCallbackData("QR сканер", "qr_scan")
			});
			return new InlineKeyboardMarkup(rows);
		}

		/// <summary>
		/// Статический конструктор: инициализация бота и загрузка настроек из БД.
		/// </summary>
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

					// Получаем ChatID для администратора и менеджера (пример)
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
		/// Отправляет текстовое уведомление (например, о событиях на складе).
		/// Если toManager=true, отправляем менеджеру, иначе администратору.
		/// </summary>
		public static async Task SendNotificationAsync(string message, bool toManager = false)
		{
			long chatId = toManager ? ManagerChatId : EmployeeChatId;
			try
			{
				await _botClient.SendTextMessageAsync(chatId, message);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Ошибка отправки уведомления: " + ex.Message);
			}
		}

		/// <summary>
		/// Запускает бота на приём обновлений.
		/// </summary>
		public static void StartBot()
		{
			_botClient.StartReceiving(
				updateHandler: HandleUpdateAsync,
				errorHandler: HandleErrorAsync,
				cancellationToken: CancellationToken.None);

			Console.WriteLine("Бот запущен и получает обновления...");
		}

		private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
		{
			Console.WriteLine($"Ошибка бота: {exception.Message}");
			return Task.CompletedTask;
		}

		/// <summary>
		/// Главный обработчик входящих обновлений.
		/// </summary>
		private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken token)
		{
			try
			{
				long userId = update.Message?.From?.Id ?? update.CallbackQuery?.From?.Id ?? 0;
				if (userId == 0)
					return;

				// Определяем роль пользователя
				string role = GetUserRole(userId);
				if (role == "неизвестно")
				{
					long chatId = (update.Message != null)
						? update.Message.Chat.Id
						: update.CallbackQuery.Message.Chat.Id;
					await botClient.SendTextMessageAsync(chatId, "Ошибка авторизации. Пользователь не найден в БД.");
					return;
				}

				if (update.Type == UpdateType.Message && update.Message != null)
				{
					if (!string.IsNullOrEmpty(update.Message.Text))
					{
						await HandleTextMessage(botClient, update.Message, token, role);
					}
					else if (update.Message.Photo != null)
					{
						await HandlePhotoMessage(botClient, update.Message, token, role);
					}
				}
				else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
				{
					await HandleCallbackQuery(botClient, update.CallbackQuery, token, role);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Ошибка обработки обновления: " + ex.Message);
			}
		}

		/// <summary>
		/// Возвращает роль пользователя по его TelegramUserID.
		/// </summary>
		private static string GetUserRole(long telegramUserId)
		{
			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					var cmd = new SqlCommand("SELECT TOP 1 Роль FROM TelegramUsers WHERE TelegramUserID=@tid", conn);
					cmd.Parameters.AddWithValue("@tid", telegramUserId);
					var result = cmd.ExecuteScalar();
					if (result != null)
						return result.ToString().Trim();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Ошибка получения роли: " + ex.Message);
			}
			return "неизвестно";
		}

		#region Обработка текстовых сообщений
		private static async Task HandleTextMessage(ITelegramBotClient botClient, Message message, CancellationToken token, string role)
		{
			string text = message.Text.Trim();
			if (text.Equals("/start", StringComparison.OrdinalIgnoreCase) ||
				text.Equals("/menu", StringComparison.OrdinalIgnoreCase))
			{
				// Отправляем главное меню без упоминания роли
				var menu = GetMainMenuForRole(role);
				await botClient.SendTextMessageAsync(
					message.Chat.Id,
					"Главное меню:",
					replyMarkup: menu,
					cancellationToken: token);

				// Удаляем команду /start или /menu
				try
				{
					await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId, token);
				}
				catch { }
			}
		}
		#endregion

		#region Обработка фото (для сканирования QR)
		private static async Task HandlePhotoMessage(ITelegramBotClient botClient, Message message, CancellationToken token, string role)
		{
			var photo = message.Photo[^1]; // Берём фото с максимальным разрешением
										   // Используем асинхронные методы для получения файла и его загрузки
			var file = await _botClient.GetFile(photo.FileId, token);

			using (var ms = new MemoryStream())
			{
				await _botClient.DownloadFile(file.FilePath, ms, token);
				byte[] imageBytes = ms.ToArray();

				// Распознаём QR-код
				string qrData = DecodeQrCode(imageBytes);
				if (!string.IsNullOrEmpty(qrData))
				{
					string productInfo = GetProductInfoByQr(qrData);
					// Формируем callback data – запоминаем идентификатор исходного сообщения с фото
					var backButtonData = "back_qr:" + message.MessageId;
					var replyKeyboard = new InlineKeyboardMarkup(new[]
					{
						new [] { InlineKeyboardButton.WithCallbackData("Назад", backButtonData) }
					});

					if (!string.IsNullOrEmpty(productInfo))
						await botClient.SendTextMessageAsync(message.Chat.Id, productInfo, replyMarkup: replyKeyboard, cancellationToken: token);
					else
						await botClient.SendTextMessageAsync(message.Chat.Id, "Товар с данным QR-кодом не найден.", replyMarkup: replyKeyboard, cancellationToken: token);
				}
				else
				{
					await botClient.SendTextMessageAsync(message.Chat.Id, "Не удалось распознать QR-код.", cancellationToken: token);
				}
			}
		}
		#endregion

		#region Обработка inline-кнопок (callback)
		private static async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken token, string role)
		{
			// Если нажата кнопка "Назад" после QR-сканирования, удаляем как сообщение с информацией, так и исходное фото
			if (callbackQuery.Data.StartsWith("back_qr:"))
			{
				string idPart = callbackQuery.Data.Substring("back_qr:".Length);
				if (int.TryParse(idPart, out int photoMsgId))
				{
					try { await botClient.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, token); } catch { }
					try { await botClient.DeleteMessageAsync(callbackQuery.Message.Chat.Id, photoMsgId, token); } catch { }
				}
				await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: token);
				return;
			}

			string responseText = "";
			InlineKeyboardMarkup replyKeyboard = null;

			switch (callbackQuery.Data)
			{
				case "view_products":
					{
						if (role.Equals("администратор", StringComparison.OrdinalIgnoreCase) ||
							role.Equals("менеджер", StringComparison.OrdinalIgnoreCase))
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
						else
						{
							responseText = "У вас нет доступа к просмотру всех товаров.";
							replyKeyboard = new InlineKeyboardMarkup(new[]
							{
								new [] { InlineKeyboardButton.WithCallbackData("Назад", "back_main") }
							});
						}
						break;
					}

				case "view_employees":
					{
						if (role.Equals("администратор", StringComparison.OrdinalIgnoreCase))
						{
							responseText = GetEmployeesList();
							replyKeyboard = new InlineKeyboardMarkup(new[]
							{
								new [] { InlineKeyboardButton.WithCallbackData("Назад", "back_main") }
							});
						}
						else
						{
							responseText = "У вас нет доступа к списку сотрудников.";
							replyKeyboard = new InlineKeyboardMarkup(new[]
							{
								new [] { InlineKeyboardButton.WithCallbackData("Назад", "back_main") }
							});
						}
						break;
					}

				case "view_total_products":
					{
						if (role.Equals("администратор", StringComparison.OrdinalIgnoreCase))
						{
							responseText = GetTotalProducts();
							replyKeyboard = new InlineKeyboardMarkup(new[]
							{
								new [] { InlineKeyboardButton.WithCallbackData("Назад", "back_main") }
							});
						}
						else
						{
							responseText = "У вас нет доступа к этой информации.";
							replyKeyboard = new InlineKeyboardMarkup(new[]
							{
								new [] { InlineKeyboardButton.WithCallbackData("Назад", "back_main") }
							});
						}
						break;
					}

				case "filter_by_warehouse":
					{
						if (role.Equals("администратор", StringComparison.OrdinalIgnoreCase))
						{
							responseText = "Выберите склад для фильтрации товаров:";
							replyKeyboard = GetWarehouseKeyboard(withBackButton: true);
						}
						else
						{
							responseText = "У вас нет доступа к фильтрации товаров по складам.";
							replyKeyboard = new InlineKeyboardMarkup(new[]
							{
								new [] { InlineKeyboardButton.WithCallbackData("Назад", "back_main") }
							});
						}
						break;
					}

				case "qr_scan":
					{
						responseText = "Отправьте фото QR кода товара.";
						replyKeyboard = new InlineKeyboardMarkup(new[]
						{
							new [] { InlineKeyboardButton.WithCallbackData("Назад", "back_main") }
						});
						break;
					}

				case "back_main":
					{
						replyKeyboard = GetMainMenuForRole(role);
						responseText = "Главное меню:";
						break;
					}

				case "none_cmd":
					{
						responseText = "Нет доступных команд (неизвестная роль).";
						replyKeyboard = new InlineKeyboardMarkup(new[]
						{
							new [] { InlineKeyboardButton.WithCallbackData("Назад", "back_main") }
						});
						break;
					}

				default:
					{
						if (callbackQuery.Data.StartsWith("warehouse_"))
						{
							if (role.Equals("администратор", StringComparison.OrdinalIgnoreCase))
							{
								string idStr = callbackQuery.Data.Substring("warehouse_".Length);
								if (int.TryParse(idStr, out int warehouseId))
								{
									responseText = GetProductsByWarehouse(warehouseId);
									replyKeyboard = new InlineKeyboardMarkup(new[]
									{
										new [] { InlineKeyboardButton.WithCallbackData("Назад", "filter_by_warehouse") }
									});
								}
								else
								{
									responseText = "Невалидный идентификатор склада.";
									replyKeyboard = new InlineKeyboardMarkup(new[]
									{
										new [] { InlineKeyboardButton.WithCallbackData("Назад", "back_main") }
									});
								}
							}
							else
							{
								responseText = "У вас нет доступа к фильтру по складам.";
								replyKeyboard = new InlineKeyboardMarkup(new[]
								{
									new [] { InlineKeyboardButton.WithCallbackData("Назад", "back_main") }
								});
							}
						}
						break;
					}
			}

			if (!string.IsNullOrEmpty(responseText))
			{
				await botClient.SendTextMessageAsync(
					callbackQuery.Message.Chat.Id,
					responseText,
					replyMarkup: replyKeyboard,
					cancellationToken: token);

				try
				{
					await botClient.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, token);
				}
				catch { }
			}

			await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: token);
		}
		#endregion

		#region Вспомогательные методы (SQL-запросы, обработка QR и т.д.)
		/// <summary>
		/// Сканирует изображение и распознаёт QR-код с помощью ZXing.
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
					var binaryBitmap = new BinaryBitmap(binarizer);
					var reader = new QRCodeReader();
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
		/// Ищет товар в БД по значению QR (поле QRText).
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
		/// Формирует список всех товаров с суммарным количеством по всем складам.
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
			catch (Exception ex)
			{
				return "Ошибка получения списка товаров: " + ex.Message;
			}
			return sb.ToString();
		}

		/// <summary>
		/// Формирует список сотрудников.
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
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							int id = reader.GetInt32(0);
							string username = reader.GetString(1);
							string roleName = reader.GetString(2);
							sb.AppendLine(string.Format("{0,-4} | {1,-20} | {2,-15}", id, username, roleName));
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
		/// Общая стоимость товаров и их количество по всем складам.
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
			catch (Exception ex)
			{
				return "Ошибка получения данных: " + ex.Message;
			}
			return "Данные не найдены.";
		}

		/// <summary>
		/// Формирует клавиатуру для выбора склада (с опциональной кнопкой "Назад").
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
		/// Получает список товаров для конкретного склада.
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

			return sb.ToString();
		}
		#endregion
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
