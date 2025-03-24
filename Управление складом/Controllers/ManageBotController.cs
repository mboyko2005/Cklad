using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Управление_складом.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ManageBotController : ControllerBase
	{
		private readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True;TrustServerCertificate=True";

		/// <summary>
		/// Получить список Telegram-пользователей.
		/// GET /api/managebot
		/// </summary>
		[HttpGet]
		public IActionResult GetBotUsers()
		{
			try
			{
				var result = new List<BotUserDto>();

				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string sql = @"
                        SELECT TelegramUserID, Роль 
                        FROM TelegramUsers
                        ORDER BY TelegramUserID";
					using (var cmd = new SqlCommand(sql, conn))
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							result.Add(new BotUserDto
							{
								// Считываем TelegramUserID как Int64 для обоих свойств
								Id = reader.GetInt64(reader.GetOrdinal("TelegramUserID")),
								TelegramId = reader.GetInt64(reader.GetOrdinal("TelegramUserID")),
								Role = reader.GetString(reader.GetOrdinal("Роль"))
							});
						}
					}
				}

				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при получении Telegram-пользователей", error = ex.Message });
			}
		}

		/// <summary>
		/// Добавить нового Telegram-пользователя.
		/// POST /api/managebot
		/// Body: { telegramId, role }
		/// </summary>
		[HttpPost]
		public IActionResult CreateBotUser([FromBody] BotUserCreateDto dto)
		{
			if (dto == null || dto.TelegramId <= 0 || string.IsNullOrWhiteSpace(dto.Role))
			{
				return BadRequest(new { message = "Некорректные данные" });
			}

			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					// Проверяем, существует ли уже пользователь с таким Telegram ID
					string checkSql = "SELECT COUNT(*) FROM TelegramUsers WHERE TelegramUserID = @telegramId";
					using (var cmdCheck = new SqlCommand(checkSql, conn))
					{
						cmdCheck.Parameters.AddWithValue("@telegramId", dto.TelegramId);
						int count = (int)cmdCheck.ExecuteScalar();
						if (count > 0)
						{
							return BadRequest(new { message = "Пользователь с таким Telegram ID уже существует" });
						}
					}
					string insertSql = @"
                        INSERT INTO TelegramUsers (TelegramUserID, Роль)
                        VALUES (@telegramId, @role);
                        SELECT SCOPE_IDENTITY();";
					using (var cmdIns = new SqlCommand(insertSql, conn))
					{
						cmdIns.Parameters.AddWithValue("@telegramId", dto.TelegramId);
						cmdIns.Parameters.AddWithValue("@role", dto.Role);
						decimal newId = (decimal)cmdIns.ExecuteScalar();
						return Ok(new { message = "Пользователь добавлен", newId = newId });
					}
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при добавлении пользователя", error = ex.Message });
			}
		}

		/// <summary>
		/// Обновить данные Telegram-пользователя.
		/// PUT /api/managebot/{id}
		/// Body: { telegramId, role }
		/// </summary>
		[HttpPut("{id}")]
		public IActionResult UpdateBotUser(long id, [FromBody] BotUserCreateDto dto)
		{
			if (dto == null || dto.TelegramId <= 0 || string.IsNullOrWhiteSpace(dto.Role))
			{
				return BadRequest(new { message = "Некорректные данные" });
			}

			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string updateSql = @"
                        UPDATE TelegramUsers
                        SET TelegramUserID = @telegramId,
                            Роль = @role
                        WHERE TelegramUserID = @id";
					using (var cmd = new SqlCommand(updateSql, conn))
					{
						cmd.Parameters.AddWithValue("@telegramId", dto.TelegramId);
						cmd.Parameters.AddWithValue("@role", dto.Role);
						cmd.Parameters.AddWithValue("@id", id);
						int rows = cmd.ExecuteNonQuery();
						if (rows > 0)
							return Ok(new { message = "Пользователь обновлён" });
						else
							return NotFound(new { message = $"Пользователь с ID {id} не найден" });
					}
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при обновлении пользователя", error = ex.Message });
			}
		}

		/// <summary>
		/// Удалить Telegram-пользователя.
		/// DELETE /api/managebot/{id}
		/// </summary>
		[HttpDelete("{id}")]
		public IActionResult DeleteBotUser(long id)
		{
			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string deleteSql = "DELETE FROM TelegramUsers WHERE TelegramUserID = @id";
					using (var cmd = new SqlCommand(deleteSql, conn))
					{
						cmd.Parameters.AddWithValue("@id", id);
						int rows = cmd.ExecuteNonQuery();
						if (rows > 0)
							return Ok(new { message = $"Пользователь с ID {id} удалён" });
						else
							return NotFound(new { message = $"Пользователь с ID {id} не найден" });
					}
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при удалении пользователя", error = ex.Message });
			}
		}
	}

	// DTO-классы для Telegram-пользователей
	public class BotUserDto
	{
		public long Id { get; set; }
		public long TelegramId { get; set; }
		public string Role { get; set; }
	}

	public class BotUserCreateDto
	{
		public long TelegramId { get; set; }
		public string Role { get; set; }
	}
}
