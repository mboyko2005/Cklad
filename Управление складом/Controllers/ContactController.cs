using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace УправлениеСкладом.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly string _connectionString =
            @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True;TrustServerCertificate=True";

        [HttpGet("list")]
        public IActionResult GetContacts([FromQuery] int userId, [FromQuery] string search = "")
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT DISTINCT
                            p.ПользовательID AS Id, 
                            p.ИмяПользователя AS Login, 
                            r.Наименование AS Role,
                            (SELECT COUNT(*) FROM Сообщения m 
                             WHERE m.ОтправительID = p.ПользовательID 
                             AND m.ПолучательID = @userId 
                             AND m.Прочитано = 0) AS UnreadCount
                        FROM Пользователи p
                        INNER JOIN Роли r ON p.РольID = r.РольID
                        -- Связываем с таблицей сообщений, чтобы отобрать только пользователей с перепиской
                        INNER JOIN (
                            SELECT ОтправительID AS UserId FROM Сообщения WHERE ПолучательID = @userId
                            UNION
                            SELECT ПолучательID AS UserId FROM Сообщения WHERE ОтправительID = @userId
                        ) conv ON p.ПользовательID = conv.UserId
                        WHERE p.ПользовательID <> @userId";

                    // Если задан поисковый запрос, добавляем условие для поиска
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        sql += " AND (p.ИмяПользователя LIKE @search OR r.Наименование LIKE @search)";
                    }

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        
                        if (!string.IsNullOrWhiteSpace(search))
                        {
                            cmd.Parameters.AddWithValue("@search", $"%{search}%");
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            var contacts = new List<object>();
                            while (reader.Read())
                            {
                                contacts.Add(new
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Login = reader.GetString(reader.GetOrdinal("Login")),
                                    Role = reader.GetString(reader.GetOrdinal("Role")),
                                    UnreadCount = reader.GetInt32(reader.GetOrdinal("UnreadCount"))
                                });
                            }
                            return Ok(contacts);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Ошибка при получении списка контактов", details = ex.Message });
            }
        }
    }
} 