using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace УправлениеСкладом
{
	public static class QRCodeHelper
	{
		/// <summary>
		/// Генерирует массив байт (PNG) с QR‑кодом на основе входной строки.
		/// Используется кодировка UTF‑8 для поддержки кириллицы.
		/// </summary>
		public static byte[] GenerateQrBytes(string data)
		{
			using (var generator = new QRCodeGenerator())
			{
				QRCodeData qrData = generator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q, forceUtf8: true, utf8BOM: false);
				using (var qrCode = new QRCode(qrData))
				{
					using (Bitmap bitmap = qrCode.GetGraphic(10))
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

		/// <summary>
		/// Преобразует массив байт (PNG) в BitmapImage для отображения в WPF.
		/// </summary>
		public static BitmapImage BytesToBitmapImage(byte[] bytes)
		{
			if (bytes == null || bytes.Length == 0)
				return null;

			var image = new BitmapImage();
			using (var ms = new MemoryStream(bytes))
			{
				image.BeginInit();
				image.CacheOption = BitmapCacheOption.OnLoad;
				image.StreamSource = ms;
				image.EndInit();
			}
			return image;
		}
	}
}
