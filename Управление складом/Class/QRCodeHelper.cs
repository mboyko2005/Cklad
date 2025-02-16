using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace УправлениеСкладом
{
	public static class QRCodeHelper
	{
		/// <summary>
		/// Генерирует массив байт (PNG) с QR-кодом на основе входной строки.
		/// </summary>
		/// <param name="data">Данные для кодирования.</param>
		/// <returns>Массив байт в формате PNG.</returns>
		public static byte[] GenerateQrBytes(string data)
		{
			using (var generator = new QRCodeGenerator())
			{
				QRCodeData qrData = generator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
				using (var qrCode = new QRCode(qrData))
				{
					using (Bitmap bitmap = qrCode.GetGraphic(10)) // Масштаб 10
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
		/// Преобразует массив байт QR-кода в BitmapImage (для отображения в WPF).
		/// </summary>
		public static System.Windows.Media.Imaging.BitmapImage BytesToBitmapImage(byte[] bytes)
		{
			if (bytes == null || bytes.Length == 0)
				return null;

			var image = new System.Windows.Media.Imaging.BitmapImage();
			using (var ms = new MemoryStream(bytes))
			{
				image.BeginInit();
				image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
				image.StreamSource = ms;
				image.EndInit();
			}
			return image;
		}
	}
}
