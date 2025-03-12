using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Управление_складом.Controllers
{
	[ApiController]
	[Route("api/qrcode")]
	public class QrCodeController : ControllerBase
	{
		private readonly string _connString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		/// <summary>
		/// Генерирует (или берёт из БД) QR-код для указанного товара.
		/// Если QRText не совпадает с актуальными данными, обновляет.
		/// Возвращает PNG‑байты (image/png).
		/// GET /api/qrcode/product/{productId}
		/// </summary>
		[HttpGet("product/{productId}")]
		public IActionResult GetQrForProduct(int productId)
		{
			try
			{
				using (var conn = new SqlConnection(_connString))
				{
					conn.Open();
					// Ищем последнюю позицию для этого товара
					string selectSql = @"
SELECT TOP 1
    sp.ПозицияID,
    sp.QRText,
    t.Наименование AS ProductName,
    s.Наименование AS WarehouseName
FROM СкладскиеПозиции sp
INNER JOIN Товары t ON sp.ТоварID = t.ТоварID
INNER JOIN Склады s ON sp.СкладID = s.СкладID
WHERE sp.ТоварID = @pid
ORDER BY sp.ДатаОбновления DESC";
					int positionId = 0;
					string existingQrText = null;
					string productName = null;
					string warehouseName = null;

					using (var cmd = new SqlCommand(selectSql, conn))
					{
						cmd.Parameters.AddWithValue("@pid", productId);
						using (var rdr = cmd.ExecuteReader())
						{
							if (!rdr.Read())
							{
								// Не нашли позицию
								return NotFound(new { message = $"Товар {productId} не найден в складских позициях" });
							}
							positionId = rdr.GetInt32(rdr.GetOrdinal("ПозицияID"));
							existingQrText = rdr.IsDBNull(rdr.GetOrdinal("QRText")) ? "" : rdr.GetString(rdr.GetOrdinal("QRText"));
							productName = rdr.IsDBNull(rdr.GetOrdinal("ProductName")) ? "" : rdr.GetString(rdr.GetOrdinal("ProductName"));
							warehouseName = rdr.IsDBNull(rdr.GetOrdinal("WarehouseName")) ? "" : rdr.GetString(rdr.GetOrdinal("WarehouseName"));
						}
					}

					// Формируем актуальный текст
					string freshQrText = $"ID={productId};Наименование={productName};Местоположение={warehouseName}";

					// Если QRText пустой или отличается — обновим
					if (!string.Equals(existingQrText, freshQrText, StringComparison.Ordinal))
					{
						string updateSql = @"
UPDATE СкладскиеПозиции
SET QRText = @qtext,
    ДатаОбновления = GETDATE()
WHERE ПозицияID = @posId";
						using (var cmdUp = new SqlCommand(updateSql, conn))
						{
							cmdUp.Parameters.AddWithValue("@qtext", freshQrText);
							cmdUp.Parameters.AddWithValue("@posId", positionId);
							cmdUp.ExecuteNonQuery();
						}
					}

					// Генерируем QR-код в PNG
					byte[] pngBytes = GenerateQrPngBytes(freshQrText);
					return File(pngBytes, "image/png");
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Ошибка при генерации QR", error = ex.Message });
			}
		}

		/// <summary>
		/// Генерация массива байт (PNG) QR-кода с помощью QRCoder.
		/// </summary>
		private byte[] GenerateQrPngBytes(string text)
		{
			using (var generator = new QRCodeGenerator())
			{
				var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q, forceUtf8: true);
				using (var qrCode = new QRCode(data))
				{
					using (Bitmap bitmap = qrCode.GetGraphic(20))
					{
						using (var ms = new MemoryStream())
						{
							bitmap.Save(ms, ImageFormat.Png);
							return ms.ToArray();
						}
					}
				}
			}
		}
	}
}
