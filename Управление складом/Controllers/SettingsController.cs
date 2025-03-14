using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data.SqlClient;

namespace УправлениеСкладом.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class SettingsController : ControllerBase
	{
		private readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True;TrustServerCertificate=True";

		/// <summary>
		/// Смена пароля текущего пользователя.
		/// POST: /api/settings/changepassword
		/// </summary>
		[HttpPost("changepassword")]
		public IActionResult ChangePassword([FromBody] ChangePasswordDto dto)
		{
			if (dto == null || string.IsNullOrWhiteSpace(dto.NewPassword) || string.IsNullOrWhiteSpace(dto.ConfirmPassword))
				return BadRequest(new { success = false, message = "Пароль не может быть пустым." });
			if (dto.NewPassword != dto.ConfirmPassword)
				return BadRequest(new { success = false, message = "Пароли не совпадают." });

			// Пытаемся получить имя пользователя через ISessionFeature.
			var sessionFeature = HttpContext.Features.Get<ISessionFeature>();
			// Если сессия не настроена, используем имя пользователя, переданное в DTO.
			string currentUsername = sessionFeature?.Session?.GetString("CurrentUsername") ?? dto.Username;
			if (string.IsNullOrWhiteSpace(currentUsername))
			{
				return BadRequest(new { success = false, message = "Не удалось определить пользователя. Повторите вход." });
			}

			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = @"UPDATE Пользователи SET Пароль = @Пароль WHERE ИмяПользователя = @Имя";
					using (var cmd = new SqlCommand(query, conn))
					{
						cmd.Parameters.AddWithValue("@Пароль", dto.NewPassword);
						cmd.Parameters.AddWithValue("@Имя", currentUsername);
						int rows = cmd.ExecuteNonQuery();
						if (rows > 0)
							return Ok(new { success = true, message = "Пароль успешно изменён." });
						else
							return NotFound(new { success = false, message = $"Пользователь '{currentUsername}' не найден." });
					}
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = $"Ошибка: {ex.Message}" });
			}
		}

		/// <summary>
		/// Сохранение темы для текущего пользователя.
		/// POST: /api/settings/theme
		/// </summary>
		[HttpPost("theme")]
		public IActionResult SaveTheme([FromBody] ThemeDto dto)
		{
			if (dto == null || string.IsNullOrWhiteSpace(dto.Theme))
				return BadRequest(new { success = false, message = "Некорректные данные" });

			var sessionFeature = HttpContext.Features.Get<ISessionFeature>();
			string currentUsername = sessionFeature?.Session?.GetString("CurrentUsername") ?? "TestUser";

			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string sql = @"UPDATE Пользователи SET Тема = @Theme WHERE ИмяПользователя = @User";
					using (var cmd = new SqlCommand(sql, conn))
					{
						cmd.Parameters.AddWithValue("@Theme", dto.Theme);
						cmd.Parameters.AddWithValue("@User", currentUsername);
						int rows = cmd.ExecuteNonQuery();
						if (rows > 0)
							return Ok(new { success = true, message = "Тема сохранена" });
						else
							return NotFound(new { success = false, message = "Пользователь не найден" });
					}
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}
	}

	public class ChangePasswordDto
	{
		// Если сессия не настроена, передаём имя пользователя через DTO
		public string Username { get; set; }
		public string NewPassword { get; set; }
		public string ConfirmPassword { get; set; }
	}

	public class ThemeDto
	{
		public string Theme { get; set; }
	}
}
