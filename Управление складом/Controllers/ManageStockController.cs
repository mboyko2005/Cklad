using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Управление_складом.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ManageStockController : ControllerBase
	{
		private readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		/// <summary>
		/// Возвращает список товаров с суммой количества на складе.
		/// Если showAll=false, то возвращаются только те товары, у которых количество=0.
		/// Если showAll=true, то возвращаются все товары.
		/// </summary>
		/// <param name="showAll">Показывать все товары (true) или только с количеством 0 (false)</param>
		[HttpGet]
		public IActionResult Get([FromQuery] bool showAll = false)
		{
			try
			{
				var items = new List<StockItemDto>();

				using (SqlConnection connection = new SqlConnection(_connectionString))
				{
					connection.Open();

					// Запрос в зависимости от флага showAll
					string query = showAll
						? @"SELECT 
	                            t.ТоварID AS ProductId,
	                            t.Наименование AS ProductName,
	                            t.Категория AS Category,
	                            ISNULL(SUM(sp.Количество), 0) AS Quantity,
	                            ISNULL(t.Цена, 0) AS Price
                            FROM Товары t
                            LEFT JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
                            GROUP BY t.ТоварID, t.Наименование, t.Категория, t.Цена
                            ORDER BY t.ТоварID"
						: @"SELECT 
	                            t.ТоварID AS ProductId,
	                            t.Наименование AS ProductName,
	                            t.Категория AS Category,
	                            ISNULL(SUM(sp.Количество), 0) AS Quantity,
	                            ISNULL(t.Цена, 0) AS Price
                            FROM Товары t
                            LEFT JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
                            GROUP BY t.ТоварID, t.Наименование, t.Категория, t.Цена
                            HAVING ISNULL(SUM(sp.Количество), 0) = 0
                            ORDER BY t.ТоварID";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								items.Add(new StockItemDto
								{
									ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
									ProductName = reader.IsDBNull(reader.GetOrdinal("ProductName"))
										? ""
										: reader.GetString(reader.GetOrdinal("ProductName")),
									Category = reader.IsDBNull(reader.GetOrdinal("Category"))
										? ""
										: reader.GetString(reader.GetOrdinal("Category")),
									Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
									Price = reader.GetDecimal(reader.GetOrdinal("Price"))
								});
							}
						}
					}
				}

				return Ok(items);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при получении списка товаров", error = ex.Message });
			}
		}
	}

	/// <summary>
	/// DTO для представления товара с суммой количества на складе.
	/// </summary>
	public class StockItemDto
	{
		public int ProductId { get; set; }
		public string ProductName { get; set; }
		public string Category { get; set; }
		public int Quantity { get; set; }
		public decimal Price { get; set; }
	}
}
