using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Управление_складом.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ManageInventoryController : ControllerBase
	{
		private readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		// GET: api/manageinventory
		[HttpGet]
		public IActionResult GetAll()
		{
			try
			{
				var result = new List<InventoryDto>();
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string sql = @"
SELECT
    t.ТоварID AS ProductId,
    t.Наименование AS ProductName,
    t.Цена AS Price,
    t.Категория AS Category,
    ISNULL(sp.ПозицияID, 0) AS PositionID,
    ISNULL(sp.Количество, 0) AS Quantity,
    ISNULL(s.Наименование, N'Нет на складе') AS WarehouseName,
    ISNULL(sp.СкладID, 0) AS WarehouseId,
    p.Наименование AS SupplierName
FROM Товары t
LEFT JOIN СкладскиеПозиции sp ON t.ТоварID = sp.ТоварID
LEFT JOIN Склады s ON sp.СкладID = s.СкладID
LEFT JOIN Поставщики p ON t.ПоставщикID = p.ПоставщикID
ORDER BY t.ТоварID;";
					using (var cmd = new SqlCommand(sql, conn))
					using (var rdr = cmd.ExecuteReader())
					{
						while (rdr.Read())
						{
							result.Add(new InventoryDto
							{
								ProductId = rdr.GetInt32(rdr.GetOrdinal("ProductId")),
								ProductName = rdr.IsDBNull(rdr.GetOrdinal("ProductName"))
									? ""
									: rdr.GetString(rdr.GetOrdinal("ProductName")),
								Category = rdr.IsDBNull(rdr.GetOrdinal("Category"))
									? ""
									: rdr.GetString(rdr.GetOrdinal("Category")),
								Price = rdr.IsDBNull(rdr.GetOrdinal("Price"))
									? 0
									: rdr.GetDecimal(rdr.GetOrdinal("Price")),
								PositionID = rdr.GetInt32(rdr.GetOrdinal("PositionID")),
								Quantity = rdr.GetInt32(rdr.GetOrdinal("Quantity")),
								WarehouseName = rdr.GetString(rdr.GetOrdinal("WarehouseName")),
								WarehouseId = rdr.GetInt32(rdr.GetOrdinal("WarehouseId")),
								SupplierName = rdr.IsDBNull(rdr.GetOrdinal("SupplierName"))
									? ""
									: rdr.GetString(rdr.GetOrdinal("SupplierName"))
							});
						}
					}
				}
				return Ok(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при получении данных", error = ex.Message });
			}
		}

		// GET: api/manageinventory/totalquantity
		[HttpGet("totalquantity")]
		public IActionResult GetTotalQuantity()
		{
			try
			{
				int totalQuantity = 0;
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string sql = "SELECT SUM(Количество) AS TotalQuantity FROM СкладскиеПозиции";
					using (var cmd = new SqlCommand(sql, conn))
					{
						var result = cmd.ExecuteScalar();
						totalQuantity = result == DBNull.Value ? 0 : Convert.ToInt32(result);
					}
				}
				return Ok(new { totalQuantity });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при получении общего количества", error = ex.Message });
			}
		}

		// GET: api/manageinventory/warehouses
		[HttpGet("warehouses")]
		public IActionResult GetWarehouses()
		{
			try
			{
				var list = new List<WarehouseDto>();
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string sql = "SELECT СкладID, Наименование FROM Склады";
					using (var cmd = new SqlCommand(sql, conn))
					using (var rdr = cmd.ExecuteReader())
					{
						while (rdr.Read())
						{
							list.Add(new WarehouseDto
							{
								Id = rdr.GetInt32(rdr.GetOrdinal("СкладID")),
								Name = rdr.GetString(rdr.GetOrdinal("Наименование"))
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

		// POST: api/manageinventory
		[HttpPost]
		public IActionResult Create([FromBody] InventoryCreateDto dto)
		{
			if (dto == null || string.IsNullOrWhiteSpace(dto.ProductName) ||
				string.IsNullOrWhiteSpace(dto.SupplierName) || dto.Price < 0 ||
				dto.WarehouseId <= 0 || dto.Quantity < 0)
			{
				return BadRequest(new { message = "Некорректные данные" });
			}

			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					using (var tran = conn.BeginTransaction())
					{
						try
						{
							int supplierId = EnsureSupplier(dto.SupplierName, conn, tran);
							int productId = EnsureProduct(dto.ProductName, dto.Category, dto.Price, supplierId, conn, tran);

							string sqlPos = @"
INSERT INTO СкладскиеПозиции (ТоварID, СкладID, Количество, ДатаОбновления)
VALUES (@p, @w, @q, GETDATE());
SELECT SCOPE_IDENTITY();";
							using (var cmdPos = new SqlCommand(sqlPos, conn, tran))
							{
								cmdPos.Parameters.AddWithValue("@p", productId);
								cmdPos.Parameters.AddWithValue("@w", dto.WarehouseId);
								cmdPos.Parameters.AddWithValue("@q", dto.Quantity);

								decimal newPosId = (decimal)cmdPos.ExecuteScalar();
								tran.Commit();
								return Ok(new { message = "Запись добавлена", newPosId });
							}
						}
						catch (Exception ex2)
						{
							tran.Rollback();
							return StatusCode(500, new { message = "Ошибка при добавлении", error = ex2.Message });
						}
					}
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при добавлении", error = ex.Message });
			}
		}

		// PUT: api/manageinventory/{id}
		[HttpPut("{id}")]
		public IActionResult Update(int id, [FromBody] InventoryCreateDto dto)
		{
			if (dto == null || string.IsNullOrWhiteSpace(dto.ProductName) ||
				string.IsNullOrWhiteSpace(dto.SupplierName) || dto.Price < 0 ||
				dto.WarehouseId <= 0 || dto.Quantity < 0)
			{
				return BadRequest(new { message = "Некорректные данные" });
			}

			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					using (var tran = conn.BeginTransaction())
					{
						try
						{
							int supplierId = EnsureSupplier(dto.SupplierName, conn, tran);
							int productId = EnsureProduct(dto.ProductName, dto.Category, dto.Price, supplierId, conn, tran);

							if (id == 0)
							{
								string sqlPos = @"
INSERT INTO СкладскиеПозиции (ТоварID, СкладID, Количество, ДатаОбновления)
VALUES (@p, @w, @q, GETDATE());";
								using (var cmdPos = new SqlCommand(sqlPos, conn, tran))
								{
									cmdPos.Parameters.AddWithValue("@p", productId);
									cmdPos.Parameters.AddWithValue("@w", dto.WarehouseId);
									cmdPos.Parameters.AddWithValue("@q", dto.Quantity);
									cmdPos.ExecuteNonQuery();
								}
							}
							else
							{
								string sqlUp = @"
UPDATE СкладскиеПозиции
SET ТоварID = @p,
    СкладID = @w,
    Количество = @q,
    ДатаОбновления = GETDATE()
WHERE ПозицияID = @id;";
								using (var cmdUp = new SqlCommand(sqlUp, conn, tran))
								{
									cmdUp.Parameters.AddWithValue("@p", productId);
									cmdUp.Parameters.AddWithValue("@w", dto.WarehouseId);
									cmdUp.Parameters.AddWithValue("@q", dto.Quantity);
									cmdUp.Parameters.AddWithValue("@id", id);
									int rows = cmdUp.ExecuteNonQuery();
									if (rows == 0)
									{
										tran.Rollback();
										return NotFound(new { message = $"Позиция {id} не найдена" });
									}
								}
							}
							tran.Commit();
							return Ok(new { message = "Запись обновлена" });
						}
						catch (Exception ex2)
						{
							tran.Rollback();
							return StatusCode(500, new { message = "Ошибка при обновлении", error = ex2.Message });
						}
					}
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при обновлении", error = ex.Message });
			}
		}

		// DELETE: api/manageinventory/{id}
		[HttpDelete("{id}")]
		public IActionResult Delete(int id)
		{
			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					using (var tran = conn.BeginTransaction())
					{
						try
						{
							string sqlGet = "SELECT ТоварID FROM СкладскиеПозиции WHERE ПозицияID=@id";
							object objProduct;
							using (var cmdGet = new SqlCommand(sqlGet, conn, tran))
							{
								cmdGet.Parameters.AddWithValue("@id", id);
								objProduct = cmdGet.ExecuteScalar();
								if (objProduct == null)
								{
									tran.Rollback();
									return NotFound(new { message = $"Позиция {id} не найдена" });
								}
							}
							int productId = Convert.ToInt32(objProduct);

							string sqlMov = "DELETE FROM ДвиженияТоваров WHERE ТоварID=@p";
							using (var cmdMov = new SqlCommand(sqlMov, conn, tran))
							{
								cmdMov.Parameters.AddWithValue("@p", productId);
								cmdMov.ExecuteNonQuery();
							}

							string sqlPos = "DELETE FROM СкладскиеПозиции WHERE ПозицияID=@pid";
							using (var cmdPos = new SqlCommand(sqlPos, conn, tran))
							{
								cmdPos.Parameters.AddWithValue("@pid", id);
								cmdPos.ExecuteNonQuery();
							}

							string sqlCheck = "SELECT COUNT(*) FROM СкладскиеПозиции WHERE ТоварID=@p";
							int countProd;
							using (var cmdC = new SqlCommand(sqlCheck, conn, tran))
							{
								cmdC.Parameters.AddWithValue("@p", productId);
								countProd = (int)cmdC.ExecuteScalar();
							}
							if (countProd == 0)
							{
								int supplierId = 0;
								string sqlSup = "SELECT ПоставщикID FROM Товары WHERE ТоварID=@p";
								using (var cmdSup = new SqlCommand(sqlSup, conn, tran))
								{
									cmdSup.Parameters.AddWithValue("@p", productId);
									object supObj = cmdSup.ExecuteScalar();
									if (supObj != null) supplierId = Convert.ToInt32(supObj);
								}
								string sqlDelProd = "DELETE FROM Товары WHERE ТоварID=@p";
								using (var cmdDP = new SqlCommand(sqlDelProd, conn, tran))
								{
									cmdDP.Parameters.AddWithValue("@p", productId);
									cmdDP.ExecuteNonQuery();
								}
								if (supplierId > 0)
								{
									string sqlCheckSup = "SELECT COUNT(*) FROM Товары WHERE ПоставщикID=@s";
									int supCount;
									using (var cmdCS = new SqlCommand(sqlCheckSup, conn, tran))
									{
										cmdCS.Parameters.AddWithValue("@s", supplierId);
										supCount = (int)cmdCS.ExecuteScalar();
									}
									if (supCount == 0)
									{
										string sqlDelSup = "DELETE FROM Поставщики WHERE ПоставщикID=@s";
										using (var cmdDS = new SqlCommand(sqlDelSup, conn, tran))
										{
											cmdDS.Parameters.AddWithValue("@s", supplierId);
											cmdDS.ExecuteNonQuery();
										}
									}
								}
							}

							tran.Commit();
							return Ok(new { message = $"Позиция {id} удалена." });
						}
						catch (Exception ex2)
						{
							tran.Rollback();
							return StatusCode(500, new { message = "Ошибка при удалении", error = ex2.Message });
						}
					}
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при удалении", error = ex.Message });
			}
		}

		private int EnsureSupplier(string supplierName, SqlConnection conn, SqlTransaction tran)
		{
			string sqlFind = "SELECT ПоставщикID FROM Поставщики WHERE Наименование=@n";
			using (var cmdF = new SqlCommand(sqlFind, conn, tran))
			{
				cmdF.Parameters.AddWithValue("@n", supplierName);
				object obj = cmdF.ExecuteScalar();
				if (obj != null) return Convert.ToInt32(obj);
			}
			string sqlIns = @"
INSERT INTO Поставщики (Наименование)
VALUES (@n);
SELECT SCOPE_IDENTITY();";
			using (var cmdI = new SqlCommand(sqlIns, conn, tran))
			{
				cmdI.Parameters.AddWithValue("@n", supplierName);
				decimal newId = (decimal)cmdI.ExecuteScalar();
				return (int)newId;
			}
		}

		private int EnsureProduct(string productName, string category, decimal price, int supplierId, SqlConnection conn, SqlTransaction tran)
		{
			string sqlFind = "SELECT ТоварID FROM Товары WHERE Наименование=@pn";
			int productId = 0;
			using (var cmdF = new SqlCommand(sqlFind, conn, tran))
			{
				cmdF.Parameters.AddWithValue("@pn", productName);
				object obj = cmdF.ExecuteScalar();
				if (obj != null) productId = Convert.ToInt32(obj);
			}

			if (productId == 0)
			{
				string sqlIns = @"
INSERT INTO Товары (Наименование, Цена, ПоставщикID, Категория)
VALUES (@n, @pr, @s, @c);
SELECT SCOPE_IDENTITY();";
				using (var cmdI = new SqlCommand(sqlIns, conn, tran))
				{
					cmdI.Parameters.AddWithValue("@n", productName);
					cmdI.Parameters.AddWithValue("@pr", price);
					cmdI.Parameters.AddWithValue("@s", supplierId);
					cmdI.Parameters.AddWithValue("@c", category);
					decimal newPid = (decimal)cmdI.ExecuteScalar();
					return (int)newPid;
				}
			}
			else
			{
				string sqlUp = @"
UPDATE Товары
SET Цена=@pr,
    Категория=@c,
    ПоставщикID=@s
WHERE ТоварID=@pid;";
				using (var cmdU = new SqlCommand(sqlUp, conn, tran))
				{
					cmdU.Parameters.AddWithValue("@pr", price);
					cmdU.Parameters.AddWithValue("@c", category);
					cmdU.Parameters.AddWithValue("@s", supplierId);
					cmdU.Parameters.AddWithValue("@pid", productId);
					cmdU.ExecuteNonQuery();
				}
				return productId;
			}
		}
	}

	public class InventoryDto
	{
		public int PositionID { get; set; }
		public int ProductId { get; set; }
		public string ProductName { get; set; }
		public string Category { get; set; }
		public decimal Price { get; set; }
		public int Quantity { get; set; }
		public string WarehouseName { get; set; }
		public int WarehouseId { get; set; }
		public string SupplierName { get; set; }
	}

	public class InventoryCreateDto
	{
		public string ProductName { get; set; }
		public string SupplierName { get; set; }
		public string Category { get; set; }
		public decimal Price { get; set; }
		public int WarehouseId { get; set; }
		public int Quantity { get; set; }
	}

	public class WarehouseDto
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}
}