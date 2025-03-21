using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data.SqlClient;

namespace УправлениеСкладом.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AuthController : ControllerBase
	{
		// Строка подключения к базе данных с включённым параметром TrustServerCertificate=True.
		private readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True;TrustServerCertificate=True";

		/// <summary>
		/// Метод для проверки работоспособности сервера.
		/// GET: /api/auth/ping
		/// Возвращает строку "pong".
		/// </summary>
		[HttpGet("ping")]
		public IActionResult Ping()
		{
			return Ok("pong");
		}

		/// <summary>
		/// Обрабатывает POST-запрос на авторизацию.
		/// Ожидается JSON-объект: { "username": "имя", "password": "пароль" }
		/// При успешной авторизации возвращает: { success = true, role = role, username = username }
		/// При неудаче возвращает: { success = false, message = "Сообщение об ошибке" }
		/// </summary>
		/// <param name="request">Объект с именем пользователя и паролем.</param>
		[HttpPost("login")]
		public IActionResult Login([FromBody] LoginRequest request)
		{
			// Проверяем, что получены корректные данные
			if (request == null ||
				string.IsNullOrWhiteSpace(request.Username) ||
				string.IsNullOrWhiteSpace(request.Password))
			{
				return BadRequest(new { success = false, message = "Некорректные данные." });
			}

			try
			{
				// Устанавливаем соединение с базой данных
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();

					// SQL-запрос для получения данных пользователя и его роли
					string sql = @"
                        SELECT u.ПользовательID, u.ИмяПользователя, r.Наименование AS Роль
                        FROM Пользователи u
                        JOIN Роли r ON u.РольID = r.РольID
                        WHERE u.ИмяПользователя = @username
                          AND u.Пароль = @password";

					using (var cmd = new SqlCommand(sql, conn))
					{
						// Подставляем параметры в запрос для предотвращения SQL-инъекций
						cmd.Parameters.AddWithValue("@username", request.Username);
						cmd.Parameters.AddWithValue("@password", request.Password);

						// Выполняем запрос и читаем результат
						using (var reader = cmd.ExecuteReader())
						{
							if (reader.Read())
							{
								// Извлекаем имя пользователя и роль из результата запроса
								string username = reader["ИмяПользователя"].ToString();
								string role = reader["Роль"].ToString();

								// Сохраняем имя пользователя в сессии (если сессия настроена)
								try
								{
									HttpContext.Session?.SetString("CurrentUsername", username);
								}
								catch (Exception sessEx)
								{
									Console.WriteLine("Ошибка при работе с сессией: " + sessEx.Message);
								}

								// Возвращаем успешный результат с данными пользователя
								return Ok(new { success = true, role = role, username = username });
							}
							else
							{
								// Если данные не найдены – возвращаем сообщение о неверном логине или пароле
								return Ok(new { success = false, message = "Неверный логин или пароль." });
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				// В случае ошибки соединения или выполнения запроса возвращаем код 500 и сообщение об ошибке
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}
	}

	/// <summary>
	/// Модель запроса для авторизации.
	/// Ожидается JSON: { "username": "имя", "password": "пароль" }
	/// </summary>
	public class LoginRequest
	{
		public string Username { get; set; }
		public string Password { get; set; }
	}
}
