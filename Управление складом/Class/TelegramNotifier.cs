using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace УправлениеСкладом.Class
{
	public static class TelegramNotifier
	{
		// Строка подключения к БД (при необходимости замените на вашу)
		private static readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		private static string BotToken;
		private static long EmployeeChatId;
		private static long ManagerChatId;
		private static TelegramBotClient _botClient;

		// Главная клавиатура (для главного меню)
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
			}
		});

		// Статический конструктор для загрузки настроек из БД
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
						{
							BotToken = result.ToString();
						}
						else
						{
							throw new Exception("API токен не найден в таблице TelegramBotSettings.");
						}
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
		/// Отправляет простое уведомление.
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
		/// Запускает получение обновлений (long polling).
		/// Вызывается, например, из Main().
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
		/// Обработчик обновлений.
		/// Реагирует на команды /start и /menu, а также на callback-запросы.
		/// </summary>
		private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
		{
			try
			{
				if (update.Type == UpdateType.Message && update.Message.Text != null)
				{
					var message = update.Message;
					if (message.From.Id == EmployeeChatId)
					{
						if (message.Text.Equals("/start", StringComparison.OrdinalIgnoreCase))
						{
							await botClient.SendTextMessageAsync(
								chatId: message.Chat.Id,
								text: "Добро пожаловать! Выберите действие:",
								replyMarkup: MainMenuKeyboard,
								cancellationToken: cancellationToken);
						}
						else if (message.Text.Equals("/menu", StringComparison.OrdinalIgnoreCase))
						{
							// Для /menu используем клавиатуру с кнопками "Все товары" и "Количество товаров"
							var menuKeyboard = new InlineKeyboardMarkup(new[]
							{
								new []
								{
									InlineKeyboardButton.WithCallbackData("Все товары", "view_products"),
									InlineKeyboardButton.WithCallbackData("Количество товаров", "view_total_products")
								}
							});
							await botClient.SendTextMessageAsync(
								chatId: message.Chat.Id,
								text: "Выберите действие:",
								replyMarkup: menuKeyboard,
								cancellationToken: cancellationToken);
						}
					}
				}
				else if (update.Type == UpdateType.CallbackQuery)
				{
					var callbackQuery = update.CallbackQuery;
					string responseText = "";
					InlineKeyboardMarkup replyKeyboard = null;

					if (callbackQuery.Data == "view_products")
					{
						responseText = GetProductsList();
						// Добавляем кнопку "Фильтр по складам" и "Назад"
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
							// Добавляем кнопку "Назад"
							replyKeyboard = new InlineKeyboardMarkup(new[]
							{
								new []
								{
									InlineKeyboardButton.WithCallbackData("Назад", "filter_by_warehouse")
								}
							});
						}
					}
					else if (callbackQuery.Data == "back_main")
					{
						replyKeyboard = MainMenuKeyboard;
						responseText = "Главное меню:";
					}

					if (!string.IsNullOrEmpty(responseText))
					{
						var sentMessage = await botClient.SendTextMessageAsync(
							chatId: callbackQuery.Message.Chat.Id,
							text: responseText,
							replyMarkup: replyKeyboard,
							cancellationToken: cancellationToken);
						try
						{
							await botClient.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, cancellationToken);
						}
						catch { }
					}
					await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Ошибка обработки обновления: " + ex.Message);
			}
		}

		/// <summary>
		/// Обработчик ошибок при получении обновлений.
		/// </summary>
		private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
		{
			Console.WriteLine($"Ошибка бота: {exception.Message}");
			return Task.CompletedTask;
		}

		/// <summary>
		/// Возвращает список товаров в виде форматированного текста (plain text).
		/// </summary>
		private static string GetProductsList()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Список товаров:");
			sb.AppendLine("ID   | Название             | Категория      | Цена");
			sb.AppendLine(new string('-', 55));
			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					const string query = "SELECT ТоварID, Наименование, Категория, Цена FROM Товары";
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
								sb.AppendLine(string.Format("{0,-4} | {1,-20} | {2,-14} | {3,6:F2}", id, name, category, price));
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
		/// Возвращает общую стоимость товаров в рублях и общее количество товаров со всех складов.
		/// </summary>
		private static string GetTotalProducts()
		{
			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					// Суммируем произведение количества на цену для вычисления стоимости, и общее количество товаров.
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
		/// Возвращает количество товаров по каждому складу.
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
		/// Если withBackButton == true, добавляется кнопка "Назад".
		/// </summary>
		private static InlineKeyboardMarkup GetWarehouseKeyboard(bool withBackButton = false)
		{
			var buttons = new List<InlineKeyboardButton[]>();
			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					// Используем столбец СкладID, а не СкладыID
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
		/// Возвращает список товаров для конкретного склада (по warehouseId) в виде текстового списка.
		/// </summary>
		private static string GetProductsByWarehouse(int warehouseId)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine($"Товары для склада ID {warehouseId}:");
			sb.AppendLine("ID   | Название             | Категория      | Цена");
			sb.AppendLine(new string('-', 55));
			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					const string query = @"
                        SELECT t.ТоварID, t.Наименование, t.Категория, t.Цена
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
								sb.AppendLine(string.Format("{0,-4} | {1,-20} | {2,-14} | {3,6:F2}", id, name, category, price));
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
}
