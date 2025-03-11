using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Управление_складом.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ManageUsersController : ControllerBase
	{
		// Строка подключения к базе
		private readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		/// <summary>
		/// Возвращает список пользователей (LEFT JOIN, чтобы загружались все записи).
		/// GET /api/manageusers
		/// </summary>
		[HttpGet]
		public IActionResult GetUsers()
		{
			try
			{
				var result = new List<UserDto>();

				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string sql = @"
                        SELECT p.ПользовательID AS UserID,
                               p.ИмяПользователя AS Username,
                               ISNULL(r.Наименование, 'Нет роли') AS RoleName,
                               p.РольID AS RoleID
                        FROM Пользователи p
                        LEFT JOIN Роли r ON p.РольID = r.РольID
                        ORDER BY p.ПользовательID";
					using (var cmd = new SqlCommand(sql, conn))
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							result.Add(new UserDto
							{
								UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
								Username = reader.GetString(reader.GetOrdinal("Username")),
								RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
								RoleID = reader.GetInt32(reader.GetOrdinal("RoleID"))
							});
						}
					}
				}

				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при получении пользователей", error = ex.Message });
			}
		}

		/// <summary>
		/// Возвращает список ролей.
		/// GET /api/manageusers/roles
		/// </summary>
		[HttpGet("roles")]
		public IActionResult GetRoles()
		{
			try
			{
				var roles = new List<RoleDto>();
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string sql = "SELECT РольID, Наименование FROM Роли";
					using (var cmd = new SqlCommand(sql, conn))
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							roles.Add(new RoleDto
							{
								Id = reader.GetInt32(reader.GetOrdinal("РольID")),
								Name = reader.GetString(reader.GetOrdinal("Наименование"))
							});
						}
					}
				}
				return Ok(roles);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при получении ролей", error = ex.Message });
			}
		}

		/// <summary>
		/// Добавить пользователя.
		/// POST /api/manageusers
		/// Body: { username, password, roleId }
		/// </summary>
		[HttpPost]
		public IActionResult CreateUser([FromBody] UserCreateDto dto)
		{
			if (dto == null || string.IsNullOrWhiteSpace(dto.Username) ||
				string.IsNullOrWhiteSpace(dto.Password) || dto.RoleId <= 0)
			{
				return BadRequest(new { message = "Некорректные данные" });
			}

			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					// Проверяем, нет ли уже пользователя с таким именем
					string checkSql = "SELECT COUNT(*) FROM Пользователи WHERE ИмяПользователя = @username";
					using (var cmdCheck = new SqlCommand(checkSql, conn))
					{
						cmdCheck.Parameters.AddWithValue("@username", dto.Username);
						int count = (int)cmdCheck.ExecuteScalar();
						if (count > 0)
						{
							return BadRequest(new { message = "Пользователь с таким именем уже существует" });
						}
					}
					// Добавляем
					string insertSql = @"
                        INSERT INTO Пользователи (ИмяПользователя, Пароль, РольID)
                        VALUES (@username, @password, @roleId);
                        SELECT SCOPE_IDENTITY();";
					using (var cmdIns = new SqlCommand(insertSql, conn))
					{
						cmdIns.Parameters.AddWithValue("@username", dto.Username);
						cmdIns.Parameters.AddWithValue("@password", dto.Password);
						cmdIns.Parameters.AddWithValue("@roleId", dto.RoleId);

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
		/// Обновить пользователя.
		/// PUT /api/manageusers/{id}
		/// Body: { username, password, roleId }
		/// </summary>
		[HttpPut("{id}")]
		public IActionResult UpdateUser(int id, [FromBody] UserCreateDto dto)
		{
			if (dto == null || string.IsNullOrWhiteSpace(dto.Username) ||
				string.IsNullOrWhiteSpace(dto.Password) || dto.RoleId <= 0)
			{
				return BadRequest(new { message = "Некорректные данные" });
			}

			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					// Проверяем, нет ли другого пользователя с таким же именем
					string checkSql = @"
                        SELECT COUNT(*) FROM Пользователи
                        WHERE ИмяПользователя = @username AND ПользовательID <> @id";
					using (var cmdCheck = new SqlCommand(checkSql, conn))
					{
						cmdCheck.Parameters.AddWithValue("@username", dto.Username);
						cmdCheck.Parameters.AddWithValue("@id", id);
						int count = (int)cmdCheck.ExecuteScalar();
						if (count > 0)
						{
							return BadRequest(new { message = "Другой пользователь с таким именем уже существует" });
						}
					}
					// Обновляем
					string updateSql = @"
                        UPDATE Пользователи
                        SET ИмяПользователя = @username,
                            Пароль = @password,
                            РольID = @roleId
                        WHERE ПользовательID = @id";
					using (var cmdUp = new SqlCommand(updateSql, conn))
					{
						cmdUp.Parameters.AddWithValue("@username", dto.Username);
						cmdUp.Parameters.AddWithValue("@password", dto.Password);
						cmdUp.Parameters.AddWithValue("@roleId", dto.RoleId);
						cmdUp.Parameters.AddWithValue("@id", id);

						int rows = cmdUp.ExecuteNonQuery();
						if (rows > 0)
							return Ok(new { message = "Пользователь обновлён" });
						else
							return NotFound(new { message = $"Пользователь {id} не найден" });
					}
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при обновлении пользователя", error = ex.Message });
			}
		}

		/// <summary>
		/// Удалить пользователя.
		/// DELETE /api/manageusers/{id}
		/// </summary>
		[HttpDelete("{id}")]
		public IActionResult DeleteUser(int id)
		{
			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					// Удаляем связанные записи в ДвиженияТоваров
					string deleteRelated = "DELETE FROM ДвиженияТоваров WHERE ПользовательID = @uid";
					using (var cmdRel = new SqlCommand(deleteRelated, conn))
					{
						cmdRel.Parameters.AddWithValue("@uid", id);
						cmdRel.ExecuteNonQuery();
					}

					// Удаляем пользователя
					string deleteSql = "DELETE FROM Пользователи WHERE ПользовательID = @uid";
					using (var cmdDel = new SqlCommand(deleteSql, conn))
					{
						cmdDel.Parameters.AddWithValue("@uid", id);
						int rows = cmdDel.ExecuteNonQuery();
						if (rows > 0)
							return Ok(new { message = $"Пользователь {id} удалён" });
						else
							return NotFound(new { message = $"Пользователь {id} не найден" });
					}
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при удалении пользователя", error = ex.Message });
			}
		}
	}

	// DTO-классы
	public class UserDto
	{
		public int UserID { get; set; }
		public string Username { get; set; }
		public string RoleName { get; set; }
		public int RoleID { get; set; }
	}

	public class RoleDto
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	public class UserCreateDto
	{
		public string Username { get; set; }
		public string Password { get; set; }
		public int RoleId { get; set; }
	}
}
