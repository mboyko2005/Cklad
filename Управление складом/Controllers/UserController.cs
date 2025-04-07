using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace УправлениеСкладом.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly string _connectionString =
            @"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True;TrustServerCertificate=True";

        [HttpGet("userinfo/{userId}")]
        public IActionResult GetUserInfo(int userId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT ИмяПользователя, Роль FROM Пользователи WHERE ПользовательID = @userId";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string username = reader["ИмяПользователя"].ToString();
                                string role = reader["Роль"].ToString();
                                return Ok(new { success = true, username, role });
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "Пользователь не найден." });
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

        [HttpGet("getUserIdByLogin/{login}")]
        public IActionResult GetUserIdByLogin(string login)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(login))
                {
                    return BadRequest(new { success = false, message = "Логин не может быть пустым" });
                }

                // Декодируем логин, т.к. он может содержать спецсимволы
                login = Uri.UnescapeDataString(login);

                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT ПользовательID FROM Пользователи WHERE ИмяПользователя = @login";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        var result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            int userId = Convert.ToInt32(result);
                            return Ok(new { success = true, userId });
                        }
                        else
                        {
                            return NotFound(new { success = false, message = "Пользователь не найден" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("list")]
        public IActionResult GetUsers([FromQuery] string search = "")
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = @"
                        SELECT p.ПользовательID, p.ИмяПользователя, r.Наименование AS Роль 
                        FROM Пользователи p
                        INNER JOIN Роли r ON p.РольID = r.РольID";
                        
                    // Если задан поисковый запрос, добавляем условие для поиска
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        sql += " WHERE p.ИмяПользователя LIKE @search OR r.Наименование LIKE @search";
                    }
                    
                    sql += " ORDER BY p.ИмяПользователя";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(search))
                        {
                            cmd.Parameters.AddWithValue("@search", $"%{search}%");
                        }
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            var users = new List<object>();
                            while (reader.Read())
                            {
                                users.Add(new
                                {
                                    id = reader.GetInt32(reader.GetOrdinal("ПользовательID")),
                                    login = reader.GetString(reader.GetOrdinal("ИмяПользователя")),
                                    role = reader.GetString(reader.GetOrdinal("Роль"))
                                });
                            }
                            return Ok(new { success = true, users });
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
} 