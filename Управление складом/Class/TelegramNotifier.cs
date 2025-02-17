using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Telegram.Bot;

namespace УправлениеСкладом.Class
{
	public static class TelegramNotifier
	{
		// Строка подключения к БД (замените на вашу, если необходимо)
		private static readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		private static string BotToken;
		private static long EmployeeChatId;
		private static long ManagerChatId;
		private static TelegramBotClient _botClient;

		// Статический конструктор для загрузки настроек из БД
		static TelegramNotifier()
		{
			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();

					// Получаем API токен бота из таблицы TelegramBotSettings
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

					// Получаем ChatID для пользователей из таблицы TelegramUsers
					using (var cmdUsers = new SqlCommand("SELECT TelegramUserID, Роль FROM TelegramUsers", conn))
					{
						using (var reader = cmdUsers.ExecuteReader())
						{
							while (reader.Read())
							{
								long telegramUserId = reader.GetInt64(0);
								string role = reader.GetString(1);

								// Если роль "Администратор" — назначаем для уведомлений о поступлении товара
								if (string.Equals(role, "Администратор", StringComparison.OrdinalIgnoreCase))
								{
									EmployeeChatId = telegramUserId;
								}
								// Если роль "Менеджер" — назначаем для уведомлений о товаре, который закончился
								else if (string.Equals(role, "Менеджер", StringComparison.OrdinalIgnoreCase))
								{
									ManagerChatId = telegramUserId;
								}
							}
						}
					}
				}

				// Инициализируем клиента Telegram с полученным токеном
				_botClient = new TelegramBotClient(BotToken);
			}
			catch (Exception ex)
			{
				// Можно добавить логирование или дополнительную обработку ошибок
				Console.WriteLine("Ошибка инициализации TelegramNotifier: " + ex.Message);
				throw;
			}
		}

		/// <summary>
		/// Отправляет уведомление в Telegram.
		/// </summary>
		/// <param name="message">Сообщение для отправки</param>
		/// <param name="toManager">
		/// Если true — уведомление менеджеру (ManagerChatId),
		/// иначе — администратору (EmployeeChatId)
		/// </param>
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
	}
}
