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
		// Обновлённая строка подключения с TrustServerCertificate=True
		private readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True;TrustServerCertificate=True";

		/// <summary>
		/// Проверка работоспособности сервера.
		/// GET: /api/auth/ping
		/// </summary>
		[HttpGet("ping")]
		public IActionResult Ping()
		{
			return Ok("pong");
		}

		/// <summary>
		/// Обработка POST-запроса авторизации.
		/// POST: /api/auth/login
		/// Ожидается JSON: { "username": "user1", "password": "pass1" }
		/// Возвращает { success = true, role = "Роль", username = "ИмяПользователя" } или { success = false, message = "..." }.
		/// </summary>
		[HttpPost("login")]
		public IActionResult Login([FromBody] LoginRequest request)
		{
			if (request == null ||
				string.IsNullOrWhiteSpace(request.Username) ||
				string.IsNullOrWhiteSpace(request.Password))
			{
				return BadRequest(new { success = false, message = "Некорректные данные." });
			}

			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string sql = @"
                        SELECT u.ПользовательID, u.ИмяПользователя, r.Наименование AS Роль
                        FROM Пользователи u
                        JOIN Роли r ON u.РольID = r.РольID
                        WHERE u.ИмяПользователя = @username
                          AND u.Пароль = @password";
					using (var cmd = new SqlCommand(sql, conn))
					{
						cmd.Parameters.AddWithValue("@username", request.Username);
						cmd.Parameters.AddWithValue("@password", request.Password);
						using (var reader = cmd.ExecuteReader())
						{
							if (reader.Read())
							{
								string username = reader["ИмяПользователя"].ToString();
								string role = reader["Роль"].ToString();

								// Попытка сохранить имя пользователя в сессии.
								try
								{
									HttpContext.Session?.SetString("CurrentUsername", username);
								}
								catch (Exception sessEx)
								{
									// Если сессия не настроена, просто записываем в лог (или игнорируем)
									Console.WriteLine("Ошибка при работе с сессией: " + sessEx.Message);
								}

								return Ok(new { success = true, role = role, username = username });
							}
							else
							{
								return Ok(new { success = false, message = "Неверный логин или пароль." });
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				// Возвращаем сообщение об ошибке с кодом 500
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
