using QRCoder;
using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ZXing;
using ZXing.Common;

namespace УправлениеСкладом
{
	public partial class ViewQrWindow : Window
	{
		private int _productId;
		private byte[] _qrBytes;

		// Строка подключения к БД.
		private readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		public ViewQrWindow(int productId)
		{
			InitializeComponent();
			_productId = productId;
		}

		protected override void OnContentRendered(EventArgs e)
		{
			base.OnContentRendered(e);
			LoadQrFromDatabase(_productId);
		}

		/// <summary>
		/// Загружает QR‑код для указанного товара из БД.
		/// Теперь запрос ищет запись по ТоварID и выбирает самую свежую (по дате обновления).
		/// Если запись не найдена, выводится сообщение, что товар не найден в складских позициях.
		/// </summary>
		private void LoadQrFromDatabase(int productId)
		{
			// Изменён запрос: вместо ПозицияID ищем по ТоварID и сортируем по дате обновления (самая свежая запись)
			const string selectQuery = @"
                SELECT TOP 1
                    sp.QRText,
                    t.Наименование AS [Товар],
                    s.Наименование AS [Склад]
                FROM СкладскиеПозиции sp
                INNER JOIN Товары t ON sp.ТоварID = t.ТоварID
                INNER JOIN Склады s ON sp.СкладID = s.СкладID
                WHERE sp.ТоварID = @ProductId
                ORDER BY sp.ДатаОбновления DESC";

			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();

					string existingQrText = "";
					string productName = "";
					string warehouseName = "";

					using (var selectCmd = new SqlCommand(selectQuery, conn))
					{
						selectCmd.Parameters.AddWithValue("@ProductId", productId);
						using (var reader = selectCmd.ExecuteReader())
						{
							if (reader.Read())
							{
								if (reader["QRText"] != DBNull.Value)
									existingQrText = reader["QRText"].ToString();
								productName = reader["Товар"]?.ToString() ?? "";
								warehouseName = reader["Склад"]?.ToString() ?? "";
							}
							else
							{
								MessageBox.Show("Запись о данном товаре в складских позициях не найдена.",
									"Информация", MessageBoxButton.OK, MessageBoxImage.Information);
								this.Close();
								return;
							}
						}
					}

					// Формируем актуальный текст QR‑кода
					string freshQrText = $"ID={productId};Наименование={productName};Местоположение={warehouseName}";

					if (string.IsNullOrEmpty(existingQrText) || !existingQrText.Equals(freshQrText, StringComparison.Ordinal))
					{
						// Если запись отсутствует или не соответствует актуальным данным, сохраняем новый QR‑текст
						SaveQrTextToDatabase(conn, productId, freshQrText);
						_qrBytes = GenerateQrCode(freshQrText);
					}
					else
					{
						_qrBytes = GenerateQrCode(existingQrText);
					}

					// Отображаем QR‑код в Image-контроле (например, с именем QrImage)
					QrImage.Source = BytesToBitmapImage(_qrBytes);
				}
			}
			catch (SqlException ex)
			{
				MessageBox.Show($"Ошибка при работе с БД: {ex.Message}",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				this.Close();
			}
		}

		/// <summary>
		/// Генерирует PNG-массив байтов с QR‑кодом на основе входной строки, используя QRCoder.
		/// </summary>
		private byte[] GenerateQrCode(string content)
		{
			using (var generator = new QRCodeGenerator())
			{
				var data = generator.CreateQrCode(
					content,
					QRCodeGenerator.ECCLevel.Q,
					forceUtf8: true,
					utf8BOM: false,
					eciMode: QRCodeGenerator.EciMode.Utf8);

				using (var qrCode = new QRCode(data))
				{
					using (Bitmap bitmap = qrCode.GetGraphic(20, Color.Black, Color.White, true))
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
		private BitmapImage BytesToBitmapImage(byte[] bytes)
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

		/// <summary>
		/// Декодирует QR‑код из массива байт (PNG) с помощью ZXing и класса BitmapLuminanceSource.
		/// </summary>
		private string DecodeQrBytes(byte[] qrBytes)
		{
			try
			{
				using (var ms = new MemoryStream(qrBytes))
				using (var image = System.Drawing.Image.FromStream(ms))
				using (var bmp = new Bitmap(image))
				{
					var luminanceSource = new BitmapLuminanceSource(bmp);
					var binarizer = new HybridBinarizer(luminanceSource);
					var binaryBitmap = new BinaryBitmap(binarizer);
					var reader = new MultiFormatReader();
					var hints = new Dictionary<DecodeHintType, object>
					{
						{ DecodeHintType.POSSIBLE_FORMATS, new List<BarcodeFormat> { BarcodeFormat.QR_CODE } },
						{ DecodeHintType.TRY_HARDER, true }
					};
					var result = reader.decode(binaryBitmap, hints);
					return result?.Text;
				}
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Обновляет (сохраняет) поле QRText для заданного товара в таблице СкладскиеПозиции.
		/// </summary>
		private void SaveQrTextToDatabase(SqlConnection conn, int productId, string qrText)
		{
			const string updateQuery = @"
                UPDATE СкладскиеПозиции
                SET QRText = @QrText,
                    ДатаОбновления = GETDATE()
                WHERE ТоварID = @ProductId";
			using (var cmd = new SqlCommand(updateQuery, conn))
			{
				cmd.Parameters.AddWithValue("@QrText", qrText);
				cmd.Parameters.AddWithValue("@ProductId", productId);
				cmd.ExecuteNonQuery();
			}
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
				this.DragMove();
		}

		/// <summary>
		/// Кнопка "Сохранить" – сохраняет текущий QR‑код (_qrBytes) в PNG-файл.
		/// </summary>
		private void SaveQrButton_Click(object sender, RoutedEventArgs e)
		{
			if (_qrBytes == null || _qrBytes.Length == 0)
			{
				MessageBox.Show("Нет QR‑кода для сохранения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			var sfd = new SaveFileDialog
			{
				Filter = "PNG Image|*.png",
				FileName = $"QrProduct_{_productId}.png"
			};
			if (sfd.ShowDialog() == true)
			{
				try
				{
					File.WriteAllBytes(sfd.FileName, _qrBytes);
					MessageBox.Show("QR‑код успешно сохранён!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		/// <summary>
		/// Кнопка "Печать" – печатает текущий QR‑код (_qrBytes) через PrintDocument.
		/// </summary>
		private void PrintQrButton_Click(object sender, RoutedEventArgs e)
		{
			if (_qrBytes == null || _qrBytes.Length == 0)
			{
				MessageBox.Show("Нет QR‑кода для печати.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			try
			{
				using (var ms = new MemoryStream(_qrBytes))
				using (var image = System.Drawing.Image.FromStream(ms))
				{
					PrintDocument pd = new PrintDocument();
					pd.PrintPage += (s, args) =>
					{
						var bounds = args.PageBounds;
						float scale = Math.Min((float)bounds.Width / image.Width, (float)bounds.Height / image.Height);
						int w = (int)(image.Width * scale);
						int h = (int)(image.Height * scale);
						args.Graphics.DrawImage(image, 0, 0, w, h);
					};
					PrintDialog dialog = new PrintDialog();
					if (dialog.ShowDialog() == true)
					{
						pd.PrinterSettings.PrinterName = dialog.PrintQueue.FullName;
						pd.Print();
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при печати: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}

	/// <summary>
	/// Класс для преобразования Bitmap в массив оттенков серого (luminance) для ZXing.
	/// </summary>
	public class BitmapLuminanceSource : LuminanceSource
	{
		private readonly byte[] luminances;

		public BitmapLuminanceSource(Bitmap bitmap) : base(bitmap.Width, bitmap.Height)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			var rect = new Rectangle(0, 0, width, height);
			var bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			int stride = bmpData.Stride;
			int bytes = Math.Abs(stride) * height;
			byte[] pixelData = new byte[bytes];
			System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, pixelData, 0, bytes);
			bitmap.UnlockBits(bmpData);

			luminances = new byte[width * height];
			for (int y = 0; y < height; y++)
			{
				int offset = y * stride;
				for (int x = 0; x < width; x++)
				{
					int index = offset + x * 4;
					byte b = pixelData[index];
					byte g = pixelData[index + 1];
					byte r = pixelData[index + 2];
					luminances[y * width + x] = (byte)((r * 299 + g * 587 + b * 114) / 1000);
				}
			}
		}

		public override byte[] Matrix => luminances;

		public override byte[] getRow(int y, byte[] row)
		{
			int width = Width;
			if (row == null || row.Length < width)
				row = new byte[width];
			Array.Copy(luminances, y * width, row, 0, width);
			return row;
		}
	}
}
