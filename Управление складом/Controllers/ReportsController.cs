using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;

namespace УправлениеСкладом.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ReportsController : ControllerBase
	{
		private readonly string _connectionString = @"Server=DESKTOP-Q11QP9V\SQLEXPRESS;Database=УправлениеСкладом;Trusted_Connection=True;";

		[HttpGet("mostSoldProducts")]
		public IActionResult GetMostSoldProducts()
		{
			try
			{
				var productNames = new List<string>();
				var quantities = new List<double>();

				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = @"
                        SELECT TOP 10 t.Наименование, SUM(ABS(dt.Количество)) AS Продано 
                        FROM ДвиженияТоваров dt
                        INNER JOIN Товары t ON dt.ТоварID = t.ТоварID 
                        WHERE dt.ТипДвижения = N'Расход'
                        GROUP BY t.Наименование 
                        ORDER BY Продано DESC";
					using (var cmd = new SqlCommand(query, conn))
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							productNames.Add(reader.GetString(0));
							double val = reader.IsDBNull(1) ? 0 : Convert.ToDouble(reader[1]);
							quantities.Add(val);
						}
					}
				}
				return Ok(new
				{
					labels = productNames,
					data = quantities,
					title = "Самые продаваемые товары",
					xTitle = "Количество продано",
					yTitle = "Товары"
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { error = ex.Message });
			}
		}

		[HttpGet("systemUsers")]
		public IActionResult GetSystemUsers()
		{
			try
			{
				var roles = new List<string>();
				var userCounts = new List<double>();

				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = @"
                        SELECT r.Наименование AS Роль, COUNT(u.ПользовательID) AS Количество
                        FROM Пользователи u
                        INNER JOIN Роли r ON u.РольID = r.РольID
                        GROUP BY r.Наименование";
					using (var cmd = new SqlCommand(query, conn))
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							roles.Add(reader.GetString(0));
							double cnt = reader.IsDBNull(1) ? 0 : Convert.ToDouble(reader[1]);
							userCounts.Add(cnt);
						}
					}
				}
				return Ok(new
				{
					labels = roles,
					data = userCounts,
					title = "Пользователи системы",
					xTitle = "Количество пользователей",
					yTitle = "Роль"
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { error = ex.Message });
			}
		}

		[HttpGet("totalCost")]
		public IActionResult GetTotalCost()
		{
			try
			{
				var productNames = new List<string>();
				var totalCosts = new List<double>();

				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = @"
                        SELECT t.Наименование,
                               SUM(sp.Количество * t.Цена) AS ОбщаяСтоимость 
                        FROM СкладскиеПозиции sp 
                        INNER JOIN Товары t ON sp.ТоварID = t.ТоварID 
                        GROUP BY t.Наименование";
					using (var cmd = new SqlCommand(query, conn))
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							productNames.Add(reader.GetString(0));
							double cost = reader.IsDBNull(1) ? 0 : Convert.ToDouble(reader.GetDecimal(1));
							totalCosts.Add(cost);
						}
					}
				}
				return Ok(new
				{
					labels = productNames,
					data = totalCosts,
					title = "Общая стоимость товаров",
					xTitle = "Стоимость (руб.)",
					yTitle = "Товары"
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { error = ex.Message });
			}
		}

		[HttpGet("currentStock")]
		public IActionResult GetCurrentStock()
		{
			try
			{
				var productNames = new List<string>();
				var quantities = new List<double>();

				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = @"
                        SELECT t.Наименование, sp.Количество 
                        FROM СкладскиеПозиции sp 
                        INNER JOIN Товары t ON sp.ТоварID = t.ТоварID";
					using (var cmd = new SqlCommand(query, conn))
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							productNames.Add(reader.GetString(0));
							double qty = reader.IsDBNull(1) ? 0 : Convert.ToDouble(reader[1]);
							quantities.Add(qty);
						}
					}
				}
				return Ok(new
				{
					labels = productNames,
					data = quantities,
					title = "Текущие складские позиции",
					xTitle = "Количество",
					yTitle = "Товары"
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { error = ex.Message });
			}
		}
	}
}
