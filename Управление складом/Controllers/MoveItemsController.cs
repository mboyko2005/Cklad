using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace YourAppNamespace.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class MoveItemsController : ControllerBase
	{
		private readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		// DTO для вывода списка товаров (суммарное кол-во)
		public class ItemDto
		{
			public int ProductId { get; set; }
			public string ProductName { get; set; }
			public string Category { get; set; }
			public int Quantity { get; set; } // Суммарное по всем складам
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
		/// Возвращает список товаров (суммарное кол-во по всем складам).
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
SELECT t.ТоварID, t.Наименование, t.Категория, ISNULL(SUM(sp.Количество), 0) AS Количество
FROM Товары t
LEFT JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
GROUP BY t.ТоварID, t.Наименование, t.Категория
ORDER BY t.ТоварID;";

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
								Quantity = rdr.GetInt32(3)
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
						// 1) Проверяем, есть ли нужное количество на исходном складе
						string checkQ = @"
SELECT COUNT(*) 
FROM СкладскиеПозиции 
WHERE ТоварID = @tid AND СкладID = @sid AND Количество >= @q;";
						using (var cmdCheck = new SqlCommand(checkQ, conn, tran))
						{
							cmdCheck.Parameters.AddWithValue("@tid", request.ProductId);
							cmdCheck.Parameters.AddWithValue("@sid", request.SourceWarehouseId);
							cmdCheck.Parameters.AddWithValue("@q", request.Quantity);
							int count = (int)cmdCheck.ExecuteScalar();
							if (count == 0)
							{
								tran.Rollback();
								return BadRequest(new { message = "Недостаточно товара на исходном складе" });
							}
						}

						// 2) Вычитаем количество из исходного склада
						string updSrc = @"
UPDATE СкладскиеПозиции
SET Количество = Количество - @q,
    ДатаОбновления = GETDATE()
WHERE ТоварID = @tid AND СкладID = @sid;";
						using (var cmdSrc = new SqlCommand(updSrc, conn, tran))
						{
							cmdSrc.Parameters.AddWithValue("@q", request.Quantity);
							cmdSrc.Parameters.AddWithValue("@tid", request.ProductId);
							cmdSrc.Parameters.AddWithValue("@sid", request.SourceWarehouseId);
							cmdSrc.ExecuteNonQuery();
						}

						// 3) Прибавляем к целевому складу
						//    Проверим, есть ли запись в СкладскиеПозиции для (ProductId, TargetWarehouseId)
						string checkDst = @"
SELECT COUNT(*) 
FROM СкладскиеПозиции 
WHERE ТоварID = @tid AND СкладID = @sid;";
						int dstCount;
						using (var cmdDstCheck = new SqlCommand(checkDst, conn, tran))
						{
							cmdDstCheck.Parameters.AddWithValue("@tid", request.ProductId);
							cmdDstCheck.Parameters.AddWithValue("@sid", request.TargetWarehouseId);
							dstCount = (int)cmdDstCheck.ExecuteScalar();
						}
						if (dstCount > 0)
						{
							// Обновляем
							string updDst = @"
UPDATE СкладскиеПозиции
SET Количество = Количество + @q,
    ДатаОбновления = GETDATE()
WHERE ТоварID = @tid AND СкладID = @sid;";
							using (var cmdDst = new SqlCommand(updDst, conn, tran))
							{
								cmdDst.Parameters.AddWithValue("@q", request.Quantity);
								cmdDst.Parameters.AddWithValue("@tid", request.ProductId);
								cmdDst.Parameters.AddWithValue("@sid", request.TargetWarehouseId);
								cmdDst.ExecuteNonQuery();
							}
						}
						else
						{
							// Вставляем новую позицию
							string insDst = @"
INSERT INTO СкладскиеПозиции (ТоварID, СкладID, Количество, ДатаОбновления)
VALUES (@tid, @sid, @q, GETDATE());";
							using (var cmdIns = new SqlCommand(insDst, conn, tran))
							{
								cmdIns.Parameters.AddWithValue("@q", request.Quantity);
								cmdIns.Parameters.AddWithValue("@tid", request.ProductId);
								cmdIns.Parameters.AddWithValue("@sid", request.TargetWarehouseId);
								cmdIns.ExecuteNonQuery();
							}
						}

						// 4) Записываем движение: минус на исходном, плюс на целевом
						//    Таблица: ДвиженияТоваров(ТоварID, СкладID, Количество, ТипДвижения, ПользовательID, ДатаВремя, ...)
						string insMov = @"
INSERT INTO ДвиженияТоваров (ТоварID, СкладID, Количество, ТипДвижения, ПользовательID, ДатаВремя)
VALUES (@tid, @sid, @cnt, @type, @uid, GETDATE());";
						using (var cmdMov = new SqlCommand(insMov, conn, tran))
						{
							cmdMov.Parameters.AddWithValue("@tid", request.ProductId);
							cmdMov.Parameters.AddWithValue("@sid", request.SourceWarehouseId);
							cmdMov.Parameters.AddWithValue("@cnt", -request.Quantity);
							cmdMov.Parameters.AddWithValue("@type", "Перемещение: Расход");
							cmdMov.Parameters.AddWithValue("@uid", request.UserId);
							cmdMov.ExecuteNonQuery();

							cmdMov.Parameters["@sid"].Value = request.TargetWarehouseId;
							cmdMov.Parameters["@cnt"].Value = request.Quantity;
							cmdMov.Parameters["@type"].Value = "Перемещение: Приход";
							cmdMov.ExecuteNonQuery();
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
