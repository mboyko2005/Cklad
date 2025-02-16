using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;              // Для Bitmap
using System.Drawing.Printing;
using System.Drawing.Imaging;      // Для ImageFormat
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using QRCoder;                     // Библиотека QRCode
using ZXing;                      // Для декодирования QR-кодов
using ZXing.Common;               // Для BinaryBitmap, DecodingOptions, MultiFormatReader и т.п.

namespace УправлениеСкладом
{
	public partial class ViewQrWindow : Window
	{
		private int _positionId;
		private byte[] _qrBytes;

		// Строка подключения к БД
		private readonly string _connectionString =
			@"Data Source=DESKTOP-Q11QP9V\SQLEXPRESS;Initial Catalog=УправлениеСкладом;Integrated Security=True";

		public ViewQrWindow(int positionId)
		{
			InitializeComponent();
			_positionId = positionId;
		}

		protected override void OnContentRendered(EventArgs e)
		{
			base.OnContentRendered(e);
			LoadQrFromDatabase(_positionId);
		}

		/// <summary>
		/// Загружает QR-код для указанной позиции из БД.
		/// Если QR-кода нет – генерирует новый, сохраняет и отображает.
		/// Если QR-код есть, декодирует его и сравнивает с актуальными данными.
		/// Если данные не совпадают или декодирование не удалось – удаляет старый QR и генерирует новый.
		/// </summary>
		private void LoadQrFromDatabase(int positionId)
		{
			// Выполняем JOIN, чтобы получить: sp.QRCode, t.Наименование (как Товар) и s.Наименование (как Склад)
			const string selectQuery = @"
                SELECT
                    sp.QRCode,
                    t.Наименование AS [Товар],
                    s.Наименование AS [Склад]
                FROM СкладскиеПозиции sp
                INNER JOIN Товары t ON sp.ТоварID = t.ТоварID
                INNER JOIN Склады s ON sp.СкладID = s.СкладID
                WHERE sp.ПозицияID = @PosId";

			try
			{
				using (var conn = new SqlConnection(_connectionString))
				{
					conn.Open();

					byte[] existingQr = null;
					string productName = "";
					string warehouseName = "";

					using (var selectCmd = new SqlCommand(selectQuery, conn))
					{
						selectCmd.Parameters.AddWithValue("@PosId", positionId);
						using (var reader = selectCmd.ExecuteReader())
						{
							if (reader.Read())
							{
								if (reader["QRCode"] != DBNull.Value)
									existingQr = (byte[])reader["QRCode"];
								productName = reader["Товар"]?.ToString() ?? "";
								warehouseName = reader["Склад"]?.ToString() ?? "";
							}
							else
							{
								MessageBox.Show("Позиция не найдена в базе данных.",
									"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
								this.Close();
								return;
							}
						}
					}

					// Формируем строку для QR-кода в одну строку (без переводов строк)
					// Это улучшает совместимость с iPhone-сканерами
					string freshQrText = $"ID={positionId};Наименование={productName};Местоположение={warehouseName}";

					// Если QR-кода нет – генерируем новый
					if (existingQr == null || existingQr.Length == 0)
					{
						byte[] newQr = GenerateQrCode(freshQrText);
						SaveQrToDatabase(conn, positionId, newQr);
						_qrBytes = newQr;
						QrImage.Source = BytesToBitmapImage(_qrBytes);
					}
					else
					{
						// Если QR-код уже есть – декодируем его и сравниваем с актуальными данными
						string decodedText = DecodeQrBytes(existingQr);
						if (decodedText == null || !decodedText.Equals(freshQrText, StringComparison.Ordinal))
						{
							// Если данные не совпадают или декодирование не удалось, удаляем старый QR и генерируем новый
							RemoveQrFromDatabase(conn, positionId);
							byte[] newQr = GenerateQrCode(freshQrText);
							SaveQrToDatabase(conn, positionId, newQr);
							_qrBytes = newQr;
							QrImage.Source = BytesToBitmapImage(_qrBytes);
						}
						else
						{
							_qrBytes = existingQr;
							QrImage.Source = BytesToBitmapImage(_qrBytes);
						}
					}
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
		/// Генерирует PNG (массив байтов) с QR-кодом, используя QRCoder.
		/// Принудительно использует кодировку UTF-8 для поддержки кириллицы.
		/// Для повышения совместимости с iPhone-сканерами QR-код формируется в одну строку.
		/// </summary>
		private byte[] GenerateQrCode(string content)
		{
			using (var generator = new QRCodeGenerator())
			{
				// Используем принудительно UTF-8 для поддержки кириллицы
				var data = generator.CreateQrCode(
					content,
					QRCodeGenerator.ECCLevel.Q,
					forceUtf8: true,
					utf8BOM: false,
					eciMode: QRCodeGenerator.EciMode.Utf8);

				using (var qrCode = new QRCode(data))
				{
					// Используем масштаб 20, явно указываем рисование quiet zone (true)
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
		/// Декодирует массив байтов (PNG) с помощью ZXing и нашего BitmapLuminanceSource.
		/// Если декодирование не удалось, возвращает null.
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
		/// "Удаляет" старый QR-код (устанавливает поле QRCode в NULL) для заданной позиции.
		/// </summary>
		private void RemoveQrFromDatabase(SqlConnection conn, int positionId)
		{
			const string removeQuery = @"
                UPDATE СкладскиеПозиции
                SET QRCode = NULL
                WHERE ПозицияID = @PosId";
			using (var cmd = new SqlCommand(removeQuery, conn))
			{
				cmd.Parameters.AddWithValue("@PosId", positionId);
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Сохраняет новый QR-код (массив байтов) в поле QRCode для заданной позиции.
		/// </summary>
		private void SaveQrToDatabase(SqlConnection conn, int positionId, byte[] newQr)
		{
			const string updateQuery = @"
                UPDATE СкладскиеПозиции
                SET QRCode = @QrCode
                WHERE ПозицияID = @PosId";
			using (var cmd = new SqlCommand(updateQuery, conn))
			{
				cmd.Parameters.AddWithValue("@QrCode", newQr);
				cmd.Parameters.AddWithValue("@PosId", positionId);
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Преобразует массив байтов (PNG) в BitmapImage для отображения в WPF.
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

		// --- Обработчики кнопок ---

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
		/// Кнопка "Сохранить" — сохраняет текущий QR-код (_qrBytes) в PNG-файл.
		/// </summary>
		private void SaveQrButton_Click(object sender, RoutedEventArgs e)
		{
			if (_qrBytes == null || _qrBytes.Length == 0)
			{
				MessageBox.Show("Нет QR-кода для сохранения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			var sfd = new SaveFileDialog
			{
				Filter = "PNG Image|*.png",
				FileName = $"QrPosition_{_positionId}.png"
			};
			if (sfd.ShowDialog() == true)
			{
				try
				{
					File.WriteAllBytes(sfd.FileName, _qrBytes);
					MessageBox.Show("QR-код успешно сохранён!",
						"Информация", MessageBoxButton.OK, MessageBoxImage.Information);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка сохранения: {ex.Message}",
						"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		/// <summary>
		/// Кнопка "Печать" — печатает текущий QR-код (_qrBytes) через PrintDocument.
		/// </summary>
		private void PrintQrButton_Click(object sender, RoutedEventArgs e)
		{
			if (_qrBytes == null || _qrBytes.Length == 0)
			{
				MessageBox.Show("Нет QR-кода для печати.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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
				MessageBox.Show($"Ошибка при печати: {ex.Message}",
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}

	/// <summary>
	/// Класс для преобразования Bitmap в массив оттенков серого (luminance)
	/// с использованием взвешенного среднего (0.299*R + 0.587*G + 0.114*B).
	/// Требуется для декодирования QR-кода с помощью ZXing.
	/// </summary>
	public class BitmapLuminanceSource : LuminanceSource
	{
		private readonly byte[] luminances;

		public BitmapLuminanceSource(Bitmap bitmap) : base(bitmap.Width, bitmap.Height)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			var rect = new Rectangle(0, 0, width, height);
			var bmpData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
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
					// Взвешенное среднее: 0.299*R + 0.587*G + 0.114*B
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
