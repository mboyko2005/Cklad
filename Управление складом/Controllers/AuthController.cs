using Microsoft.AspNetCore.Mvc;
using System;
using System.Data.SqlClient;

namespace УправлениеСкладом.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AuthController : ControllerBase
	{
		// Строка подключения к базе данных
		private readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		/// <summary>
		/// Метод для проверки работоспособности сервера.
		/// GET: /api/auth/ping
		/// Возвращает 200 OK с текстом "pong".
		/// </summary>
		[HttpGet("ping")]
		public IActionResult Ping()
		{
			return Ok("pong");
		}

		/// <summary>
		/// Обрабатывает POST-запрос авторизации по адресу /api/auth/login.
		/// Ожидает JSON вида: { "username": "user1", "password": "pass1" }
		/// Возвращает успешный ответ с ролью или { success = false }.
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

					// Поиск пользователя по логину/паролю
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
								var roleName = reader["Роль"].ToString();
								return Ok(new { success = true, role = roleName });
							}
							else
							{
								return Ok(new { success = false });
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
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
