using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace MoveItemsController.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class MoveItemsController : ControllerBase
	{
		private readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		// DTO для вывода списка товаров
		public class ItemDto
		{
			public int ProductId { get; set; }
			public string ProductName { get; set; }
			public string Category { get; set; }
			public int Quantity { get; set; }
			public int WarehouseId { get; set; }
			public string WarehouseName { get; set; }
		}

		// DTO для вывода складов
		public class WarehouseDto
		{
			public int Id { get; set; }
			public string Name { get; set; }
			// Необязательное поле для исходных складов (чтобы возвращать остаток)
			public int Quantity { get; set; }
		}

		// DTO для перемещения
		public class MoveItemsRequest
		{
			public int ProductId { get; set; }
			public int SourceWarehouseId { get; set; }
			public int TargetWarehouseId { get; set; }
			public int Quantity { get; set; }
			public int UserId { get; set; }
		}

		/// <summary>
		/// GET /api/moveitems/items
		/// Возвращает список товаров с разбивкой по складам.
		/// </summary>
		[HttpGet("items")]
		public IActionResult GetItems()
		{
			try
			{
				var result = new List<ItemDto>();
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = @"
SELECT 
    t.ТоварID,
    t.Наименование,
    t.Категория,
    sp.Количество,
    s.СкладID,
    s.Наименование AS СкладНаименование
FROM Товары t
JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
JOIN Склады s ON sp.СкладID = s.СкладID
WHERE sp.Количество > 0
ORDER BY t.ТоварID, s.СкладID;";

					using (var cmd = new SqlCommand(query, conn))
					using (var rdr = cmd.ExecuteReader())
					{
						while (rdr.Read())
						{
							result.Add(new ItemDto
							{
								ProductId = rdr.GetInt32(0),
								ProductName = rdr.IsDBNull(1) ? "" : rdr.GetString(1),
								Category = rdr.IsDBNull(2) ? "" : rdr.GetString(2),
								Quantity = rdr.GetInt32(3),
								WarehouseId = rdr.GetInt32(4),
								WarehouseName = rdr.GetString(5)
							});
						}
					}
				}
				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при получении списка товаров", error = ex.Message });
			}
		}

		/// <summary>
		/// GET /api/moveitems/warehouses
		/// Возвращает все склады (Id, Name).
		/// </summary>
		[HttpGet("warehouses")]
		public IActionResult GetAllWarehouses()
		{
			try
			{
				var list = new List<WarehouseDto>();
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string sql = "SELECT СкладID, Наименование FROM Склады ORDER BY СкладID;";
					using (var cmd = new SqlCommand(sql, conn))
					using (var rdr = cmd.ExecuteReader())
					{
						while (rdr.Read())
						{
							list.Add(new WarehouseDto
							{
								Id = rdr.GetInt32(0),
								Name = rdr.GetString(1)
							});
						}
					}
				}
				return Ok(list);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при получении складов", error = ex.Message });
			}
		}

		/// <summary>
		/// GET /api/moveitems/warehouses/{itemId}
		/// Возвращает склады, где есть указанный товар (количество > 0).
		/// </summary>
		[HttpGet("warehouses/{itemId}")]
		public IActionResult GetWarehousesForItem(int itemId)
		{
			try
			{
				var list = new List<WarehouseDto>();
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string q = @"
SELECT s.СкладID, s.Наименование, sp.Количество
FROM СкладскиеПозиции sp
JOIN Склады s ON sp.СкладID = s.СкладID
WHERE sp.ТоварID = @tid AND sp.Количество > 0
ORDER BY s.СкладID;";
					using (var cmd = new SqlCommand(q, conn))
					{
						cmd.Parameters.AddWithValue("@tid", itemId);
						using (var rdr = cmd.ExecuteReader())
						{
							while (rdr.Read())
							{
								list.Add(new WarehouseDto
								{
									Id = rdr.GetInt32(0),
									Name = rdr.GetString(1),
									Quantity = rdr.GetInt32(2)
								});
							}
						}
					}
				}
				return Ok(list);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при получении складов товара", error = ex.Message });
			}
		}

		/// <summary>
		/// POST /api/moveitems
		/// Тело: MoveItemsRequest { ProductId, SourceWarehouseId, TargetWarehouseId, Quantity, UserId }
		/// Логика перемещения товара.
		/// </summary>
		[HttpPost]
		public IActionResult MoveItem([FromBody] MoveItemsRequest request)
		{
			if (request == null || request.ProductId <= 0 ||
				request.SourceWarehouseId <= 0 || request.TargetWarehouseId <= 0 ||
				request.Quantity <= 0)
			{
				return BadRequest(new { message = "Некорректные данные для перемещения" });
			}

			using (var conn = new SqlConnection(_connectionString))
			{
				conn.Open();
				using (var tran = conn.BeginTransaction())
				{
					try
					{
						// 1) Проверяем наличие нужного количества на исходном складе
						string checkQ = @"SELECT COUNT(*) FROM СкладскиеПозиции WHERE ТоварID=@tid AND СкладID=@sid AND Количество>=@q";
						using (var cmd = new SqlCommand(checkQ, conn, tran))
						{
							cmd.Parameters.AddWithValue("@tid", request.ProductId);
							cmd.Parameters.AddWithValue("@sid", request.SourceWarehouseId);
							cmd.Parameters.AddWithValue("@q", request.Quantity);
							int count = (int)cmd.ExecuteScalar();
							if (count == 0)
							{
								tran.Rollback();
								return BadRequest(new { message = "Недостаточно товара на исходном складе" });
							}
						}

						// 2) Получаем текущее количество на исходном складе
						string getQtyQuery = @"SELECT Количество FROM СкладскиеПозиции WHERE ТоварID=@tid AND СкладID=@sid";
						int currentQty;
						using (var cmd = new SqlCommand(getQtyQuery, conn, tran))
						{
							cmd.Parameters.AddWithValue("@tid", request.ProductId);
							cmd.Parameters.AddWithValue("@sid", request.SourceWarehouseId);
							currentQty = (int)cmd.ExecuteScalar();
						}

						// 3) Если перемещаем все количество, удаляем запись
						if (currentQty == request.Quantity)
						{
							string delSrc = @"DELETE FROM СкладскиеПозиции WHERE ТоварID=@tid AND СкладID=@sid";
							using (var cmd = new SqlCommand(delSrc, conn, tran))
							{
								cmd.Parameters.AddWithValue("@tid", request.ProductId);
								cmd.Parameters.AddWithValue("@sid", request.SourceWarehouseId);
								cmd.ExecuteNonQuery();
							}
						}
						else
						{
							// Иначе уменьшаем количество
							string updSrc = @"UPDATE СкладскиеПозиции SET Количество = Количество - @q, ДатаОбновления=GETDATE() WHERE ТоварID=@tid AND СкладID=@sid";
							using (var cmd = new SqlCommand(updSrc, conn, tran))
							{
								cmd.Parameters.AddWithValue("@q", request.Quantity);
								cmd.Parameters.AddWithValue("@tid", request.ProductId);
								cmd.Parameters.AddWithValue("@sid", request.SourceWarehouseId);
								cmd.ExecuteNonQuery();
							}
						}

						// 4) Проверяем наличие товара на целевом складе
						string checkDst = @"SELECT COUNT(*) FROM СкладскиеПозиции WHERE ТоварID=@tid AND СкладID=@sid";
						int dstCount;
						using (var cmd = new SqlCommand(checkDst, conn, tran))
						{
							cmd.Parameters.AddWithValue("@tid", request.ProductId);
							cmd.Parameters.AddWithValue("@sid", request.TargetWarehouseId);
							dstCount = (int)cmd.ExecuteScalar();
						}

						if (dstCount > 0)
						{
							// Обновляем существующую позицию
							string updDst = @"UPDATE СкладскиеПозиции SET Количество = Количество + @q, ДатаОбновления=GETDATE() WHERE ТоварID=@tid AND СкладID=@sid";
							using (var cmd = new SqlCommand(updDst, conn, tran))
							{
								cmd.Parameters.AddWithValue("@q", request.Quantity);
								cmd.Parameters.AddWithValue("@tid", request.ProductId);
								cmd.Parameters.AddWithValue("@sid", request.TargetWarehouseId);
								cmd.ExecuteNonQuery();
							}
						}
						else
						{
							// Создаем новую позицию
							string insDst = @"INSERT INTO СкладскиеПозиции(ТоварID, СкладID, Количество, ДатаОбновления) VALUES(@tid, @sid, @q, GETDATE())";
							using (var cmd = new SqlCommand(insDst, conn, tran))
							{
								cmd.Parameters.AddWithValue("@q", request.Quantity);
								cmd.Parameters.AddWithValue("@tid", request.ProductId);
								cmd.Parameters.AddWithValue("@sid", request.TargetWarehouseId);
								cmd.ExecuteNonQuery();
							}
						}

						// 5) Записываем движения товара
						string insMov = @"INSERT INTO ДвиженияТоваров(ТоварID, СкладID, Количество, ТипДвижения, ПользовательID) VALUES(@tid, @sid, @cnt, @type, @uid)";
						using (var cmd = new SqlCommand(insMov, conn, tran))
						{
							cmd.Parameters.AddWithValue("@tid", request.ProductId);
							cmd.Parameters.AddWithValue("@sid", request.SourceWarehouseId);
							cmd.Parameters.AddWithValue("@cnt", -request.Quantity);
							cmd.Parameters.AddWithValue("@type", "Перемещение: Расход");
							cmd.Parameters.AddWithValue("@uid", request.UserId);
							cmd.ExecuteNonQuery();

							cmd.Parameters["@sid"].Value = request.TargetWarehouseId;
							cmd.Parameters["@cnt"].Value = request.Quantity;
							cmd.Parameters["@type"].Value = "Перемещение: Приход";
							cmd.ExecuteNonQuery();
						}

						tran.Commit();
						return Ok(new { message = "Товар успешно перемещён" });
					}
					catch (Exception ex)
					{
						tran.Rollback();
						return StatusCode(500, new { message = "Ошибка при перемещении товара", error = ex.Message });
					}
				}
			}
		}
	}
}
