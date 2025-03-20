using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace YourAppNamespace.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class InventoryLogController : ControllerBase
	{
		private readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		// DTO для журнала
		public class InventoryLogDto
		{
			public int MovementId { get; set; }
			public int ProductId { get; set; }
			public int WarehouseId { get; set; }
			public DateTime Date { get; set; }
			public string Type { get; set; } // Приход / Расход
			public string ItemName { get; set; }
			public int Quantity { get; set; }
		}

		// DTO для отсутствующих товаров
		public class OutOfStockItemDto
		{
			public int ProductId { get; set; }
			public string Name { get; set; }
			public int Quantity { get; set; } // обычно 0, но вдруг бывает отрицательное
		}

		// DTO для POST (добавления движения)
		public class AddMovementRequest
		{
			public int ProductId { get; set; }
			public int WarehouseId { get; set; }
			public int Quantity { get; set; }
			public string Type { get; set; }  // "Приход" или "Расход"
			public int UserId { get; set; }
		}

		/// <summary>
		/// GET /api/inventorylog/logs
		/// Возвращает список движений (журнала).
		/// </summary>
		[HttpGet("logs")]
		public IActionResult GetLogs()
		{
			try
			{
				var result = new List<InventoryLogDto>();
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = @"
SELECT dt.ДвижениеID,
       dt.ТоварID,
       dt.СкладID,
       dt.Дата,
       dt.ТипДвижения,
       t.Наименование AS ItemName,
       dt.Количество
FROM ДвиженияТоваров dt
JOIN Товары t ON dt.ТоварID = t.ТоварID
ORDER BY dt.Дата DESC;";

					using (var cmd = new SqlCommand(query, conn))
					using (var rdr = cmd.ExecuteReader())
					{
						while (rdr.Read())
						{
							result.Add(new InventoryLogDto
							{
								MovementId = rdr.GetInt32(rdr.GetOrdinal("ДвижениеID")),
								ProductId = rdr.GetInt32(rdr.GetOrdinal("ТоварID")),
								WarehouseId = rdr.GetInt32(rdr.GetOrdinal("СкладID")),
								Date = rdr.GetDateTime(rdr.GetOrdinal("Дата")),
								Type = rdr.GetString(rdr.GetOrdinal("ТипДвижения")),
								ItemName = rdr.GetString(rdr.GetOrdinal("ItemName")),
								Quantity = rdr.GetInt32(rdr.GetOrdinal("Количество"))
							});
						}
					}
				}
				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при получении журнала", error = ex.Message });
			}
		}

		/// <summary>
		/// GET /api/inventorylog/outofstock
		/// Возвращает товары, у которых количество <= 0.
		/// </summary>
		[HttpGet("outofstock")]
		public IActionResult GetOutOfStockItems()
		{
			try
			{
				var list = new List<OutOfStockItemDto>();
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = @"
SELECT t.ТоварID, t.Наименование, ISNULL(sp.Количество,0) AS Количество
FROM Товары t
LEFT JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
WHERE ISNULL(sp.Количество, 0) <= 0
ORDER BY t.Наименование;";
					using (var cmd = new SqlCommand(query, conn))
					using (var rdr = cmd.ExecuteReader())
					{
						while (rdr.Read())
						{
							list.Add(new OutOfStockItemDto
							{
								ProductId = rdr.GetInt32(rdr.GetOrdinal("ТоварID")),
								Name = rdr.GetString(rdr.GetOrdinal("Наименование")),
								Quantity = rdr.GetInt32(rdr.GetOrdinal("Количество"))
							});
						}
					}
				}
				return Ok(list);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при получении отсутствующих товаров", error = ex.Message });
			}
		}

		/// <summary>
		/// POST /api/inventorylog
		/// Добавляет движение (приход/расход).
		/// Тело: { productId, warehouseId, quantity, type, userId }
		/// </summary>
		[HttpPost]
		public IActionResult AddMovement([FromBody] AddMovementRequest request)
		{
			if (request == null || request.ProductId <= 0 ||
				request.WarehouseId <= 0 || request.Quantity <= 0 ||
				string.IsNullOrWhiteSpace(request.Type))
			{
				return BadRequest(new { message = "Некорректные данные для движения" });
			}

			if (request.Type != "Приход" && request.Type != "Расход")
			{
				return BadRequest(new { message = "Тип движения должен быть 'Приход' или 'Расход'" });
			}

			using (var conn = new SqlConnection(_connectionString))
			{
				conn.Open();
				using (var tran = conn.BeginTransaction())
				{
					try
					{
						// Количество для записи в таблицу ДвиженияТоваров:
						// Если Расход, то в некоторых БД хранится как отрицательное значение, 
						// но у вас может быть иначе (вы храните +N, просто ТипДвижения='Расход').
						// Допустим, оставим как есть: вставляем именно request.Quantity, 
						// а тип подскажет, что это расход.
						// Но если нужно "минус" — тогда:
						// if (request.Type == "Расход") request.Quantity = -request.Quantity;

						// 1) Добавляем запись в ДвиженияТоваров
						string insQuery = @"
INSERT INTO ДвиженияТоваров (ТоварID, СкладID, Количество, ТипДвижения, ПользовательID, Дата)
VALUES (@p, @w, @q, @t, @u, GETDATE());";
						using (var cmdIns = new SqlCommand(insQuery, conn, tran))
						{
							cmdIns.Parameters.AddWithValue("@p", request.ProductId);
							cmdIns.Parameters.AddWithValue("@w", request.WarehouseId);
							cmdIns.Parameters.AddWithValue("@q", request.Quantity);
							cmdIns.Parameters.AddWithValue("@t", request.Type);
							cmdIns.Parameters.AddWithValue("@u", request.UserId);
							cmdIns.ExecuteNonQuery();
						}

						// 2) Пересчитываем остаток (сумма по всем движениям)
						UpdateStockPosition(conn, tran, request.ProductId, request.WarehouseId);

						tran.Commit();
						return Ok(new { message = "Движение успешно добавлено" });
					}
					catch (Exception ex)
					{
						tran.Rollback();
						return StatusCode(500, new { message = "Ошибка при добавлении движения", error = ex.Message });
					}
				}
			}
		}

		/// <summary>
		/// DELETE /api/inventorylog/{movementId}
		/// Удаление записи журнала (движения).
		/// После удаления пересчитываем остаток.
		/// </summary>
		[HttpDelete("{movementId}")]
		public IActionResult DeleteMovement(int movementId)
		{
			if (movementId <= 0)
				return BadRequest(new { message = "Некорректный ID записи" });

			using (var conn = new SqlConnection(_connectionString))
			{
				conn.Open();
				using (var tran = conn.BeginTransaction())
				{
					try
					{
						// Сначала получим ProductId и WarehouseId из этой записи
						int productId = 0;
						int warehouseId = 0;
						string selQ = @"
SELECT ТоварID, СкладID 
FROM ДвиженияТоваров
WHERE ДвижениеID=@id;";
						using (var cmdSel = new SqlCommand(selQ, conn, tran))
						{
							cmdSel.Parameters.AddWithValue("@id", movementId);
							using (var rdr = cmdSel.ExecuteReader())
							{
								if (rdr.Read())
								{
									productId = rdr.GetInt32(0);
									warehouseId = rdr.GetInt32(1);
								}
								else
								{
									tran.Rollback();
									return NotFound(new { message = "Запись не найдена" });
								}
							}
						}

						// Удаляем движение
						string delQ = "DELETE FROM ДвиженияТоваров WHERE ДвижениеID=@id;";
						using (var cmdDel = new SqlCommand(delQ, conn, tran))
						{
							cmdDel.Parameters.AddWithValue("@id", movementId);
							cmdDel.ExecuteNonQuery();
						}

						// Пересчитываем остаток
						UpdateStockPosition(conn, tran, productId, warehouseId);

						tran.Commit();
						return Ok(new { message = "Запись удалена" });
					}
					catch (Exception ex)
					{
						tran.Rollback();
						return StatusCode(500, new { message = "Ошибка при удалении записи", error = ex.Message });
					}
				}
			}
		}

		/// <summary>
		/// Пересчитывает итоговый остаток в СкладскиеПозиции (сумма по ДвиженияТоваров).
		/// Если остаток 0, удаляем позицию; если > 0, обновляем или вставляем.
		/// </summary>
		private void UpdateStockPosition(SqlConnection conn, SqlTransaction tran, int productId, int warehouseId)
		{
			// 1) Узнаём сумму по всем движениям
			int totalQty = 0;
			string sumQ = @"
SELECT ISNULL(SUM(Количество), 0) 
FROM ДвиженияТоваров
WHERE ТоварID=@p AND СкладID=@w;";
			using (var cmdSum = new SqlCommand(sumQ, conn, tran))
			{
				cmdSum.Parameters.AddWithValue("@p", productId);
				cmdSum.Parameters.AddWithValue("@w", warehouseId);
				object obj = cmdSum.ExecuteScalar();
				if (obj != null) totalQty = Convert.ToInt32(obj);
			}

			// 2) Проверяем, есть ли запись в СкладскиеПозиции
			string checkQ = @"
SELECT COUNT(*) 
FROM СкладскиеПозиции
WHERE ТоварID=@p AND СкладID=@w;";
			int posCount = 0;
			using (var cmdCheck = new SqlCommand(checkQ, conn, tran))
			{
				cmdCheck.Parameters.AddWithValue("@p", productId);
				cmdCheck.Parameters.AddWithValue("@w", warehouseId);
				posCount = (int)cmdCheck.ExecuteScalar();
			}

			if (totalQty > 0)
			{
				if (posCount > 0)
				{
					// Обновляем
					string updQ = @"
UPDATE СкладскиеПозиции
SET Количество=@q,
    ДатаОбновления=GETDATE()
WHERE ТоварID=@p AND СкладID=@w;";
					using (var cmdUpd = new SqlCommand(updQ, conn, tran))
					{
						cmdUpd.Parameters.AddWithValue("@q", totalQty);
						cmdUpd.Parameters.AddWithValue("@p", productId);
						cmdUpd.Parameters.AddWithValue("@w", warehouseId);
						cmdUpd.ExecuteNonQuery();
					}
				}
				else
				{
					// Вставляем
					string insQ = @"
INSERT INTO СкладскиеПозиции (ТоварID, СкладID, Количество, ДатаОбновления)
VALUES (@p, @w, @q, GETDATE());";
					using (var cmdIns = new SqlCommand(insQ, conn, tran))
					{
						cmdIns.Parameters.AddWithValue("@p", productId);
						cmdIns.Parameters.AddWithValue("@w", warehouseId);
						cmdIns.Parameters.AddWithValue("@q", totalQty);
						cmdIns.ExecuteNonQuery();
					}
				}
			}
			else
			{
				// Если totalQty <= 0, то удаляем позицию, если она есть
				if (posCount > 0)
				{
					string delPosQ = @"
DELETE FROM СкладскиеПозиции
WHERE ТоварID=@p AND СкладID=@w;";
					using (var cmdDelPos = new SqlCommand(delPosQ, conn, tran))
					{
						cmdDelPos.Parameters.AddWithValue("@p", productId);
						cmdDelPos.Parameters.AddWithValue("@w", warehouseId);
						cmdDelPos.ExecuteNonQuery();
					}
				}
			}
		}
	}
}
