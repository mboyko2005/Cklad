// ReportsWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MahApps.Metro.IconPacks;
using LiveCharts;
using LiveCharts.Wpf;
using ClosedXML.Excel;
using Xceed.Words.NET;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Управление_складом.Themes;

namespace УправлениеСкладом
{
	public partial class ReportsWindow : Window, IThemeable, INotifyPropertyChanged
	{
		// Замените строку подключения на вашу
		private string _connectionString = "Server=DESKTOP-Q11QP9V\\SQLEXPRESS;Database=УправлениеСкладом;Trusted_Connection=True;";

		public ReportsWindow()
		{
			InitializeComponent();
			DataContext = this;
			ReportTypeComboBox.SelectionChanged += ReportTypeComboBox_SelectionChanged;
			ChartTypeComboBox.SelectionChanged += ChartTypeComboBox_SelectionChanged;
			UpdateThemeIcon();
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		private void ReportTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			// Дополнительная логика при изменении типа отчёта, если необходимо
		}

		private void ChartTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			// Дополнительная логика при изменении типа диаграммы, если необходимо
		}

		private void GenerateReport_Click(object sender, RoutedEventArgs e)
		{
			string selectedReport = (ReportTypeComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
			string chartType = (ChartTypeComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();

			switch (selectedReport)
			{
				case "Самые продаваемые товары":
					GenerateMostSoldProductsReport(chartType);
					break;
				case "Пользователи системы":
					GenerateSystemUsersReport(chartType);
					break;
				case "Общая стоимость товаров":
					GenerateTotalCostReport(chartType);
					break;
				case "Текущие складские позиции":
					GenerateCurrentStockPositionsReport(chartType);
					break;
				default:
					MessageBox.Show("Выберите тип отчёта.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
					break;
			}
		}

		private void GenerateMostSoldProductsReport(string chartType)
		{
			try
			{
				using (SqlConnection conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = @"SELECT TOP 10 t.Наименование, SUM(dt.Количество) AS Продано 
                                     FROM ДвиженияТоваров dt
                                     INNER JOIN Товары t ON dt.ТоварID = t.ТоварID 
                                     WHERE dt.ТипДвижения = N'Приход'
                                     GROUP BY t.Наименование 
                                     ORDER BY Продано DESC";
					SqlCommand cmd = new SqlCommand(query, conn);
					SqlDataReader reader = cmd.ExecuteReader();

					List<string> productNames = new List<string>();
					List<int> quantities = new List<int>();

					while (reader.Read())
					{
						productNames.Add(reader.GetString(0));
						quantities.Add(reader.GetInt32(1));
					}

					DisplayChart(chartType, productNames, quantities.Cast<object>().ToList(), "Самые продаваемые товары", "Товар", "Количество продано");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при генерации отчёта: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void GenerateSystemUsersReport(string chartType)
		{
			try
			{
				using (SqlConnection conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = @"SELECT r.Наименование AS Роль, COUNT(u.ПользовательID) AS Количество
                                     FROM Пользователи u
                                     INNER JOIN Роли r ON u.РольID = r.РольID
                                     GROUP BY r.Наименование";
					SqlCommand cmd = new SqlCommand(query, conn);
					SqlDataReader reader = cmd.ExecuteReader();

					List<string> roles = new List<string>();
					List<int> userCounts = new List<int>();

					while (reader.Read())
					{
						roles.Add(reader.GetString(0));
						userCounts.Add(reader.GetInt32(1));
					}

					DisplayChart(chartType, roles, userCounts.Cast<object>().ToList(), "Пользователи системы", "Роль", "Количество пользователей");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при генерации отчёта: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void GenerateTotalCostReport(string chartType)
		{
			try
			{
				using (SqlConnection conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = @"SELECT t.Наименование, SUM(sp.Количество * t.Цена) AS ОбщаяСтоимость 
                                     FROM СкладскиеПозиции sp 
                                     INNER JOIN Товары t ON sp.ТоварID = t.ТоварID 
                                     GROUP BY t.Наименование";
					SqlCommand cmd = new SqlCommand(query, conn);
					SqlDataReader reader = cmd.ExecuteReader();

					List<string> productNames = new List<string>();
					List<decimal> totalCosts = new List<decimal>();

					while (reader.Read())
					{
						productNames.Add(reader.GetString(0));
						totalCosts.Add(reader.GetDecimal(1));
					}

					DisplayChart(chartType, productNames, totalCosts.Cast<object>().ToList(), "Общая стоимость товаров", "Товар", "Стоимость (руб.)");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при генерации отчёта: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void GenerateCurrentStockPositionsReport(string chartType)
		{
			try
			{
				using (SqlConnection conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = @"SELECT t.Наименование, sp.Количество 
                                     FROM СкладскиеПозиции sp 
                                     INNER JOIN Товары t ON sp.ТоварID = t.ТоварID";
					SqlCommand cmd = new SqlCommand(query, conn);
					SqlDataReader reader = cmd.ExecuteReader();

					List<string> productNames = new List<string>();
					List<int> quantities = new List<int>();

					while (reader.Read())
					{
						productNames.Add(reader.GetString(0));
						quantities.Add(reader.GetInt32(1));
					}

					DisplayChart(chartType, productNames, quantities.Cast<object>().ToList(), "Текущие складские позиции", "Товар", "Количество");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при генерации отчёта: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void DisplayChart(string chartType, List<string> labels, List<object> values, string title, string xTitle, string yTitle)
		{
			ReportChart.Series.Clear();
			ReportChart.Visibility = Visibility.Collapsed;
			PieReportChart.Series.Clear();
			PieReportChart.Visibility = Visibility.Collapsed;

			if (chartType == "Столбчатая")
			{
				ReportChart.Visibility = Visibility.Visible;
				ReportChart.Series = new SeriesCollection
				{
					new ColumnSeries
					{
						Title = yTitle,
						Values = new ChartValues<double>(values.Select(v => Convert.ToDouble(v)))
					}
				};
				ReportChart.AxisX.Clear();
				ReportChart.AxisY.Clear();
				ReportChart.AxisX.Add(new Axis
				{
					Title = xTitle,
					Labels = labels
				});
				ReportChart.AxisY.Add(new Axis
				{
					Title = yTitle
				});
				ReportChart.LegendLocation = LegendLocation.Right;
			}
			else if (chartType == "Круговая")
			{
				PieReportChart.Visibility = Visibility.Visible;
				PieReportChart.Series = new SeriesCollection();
				for (int i = 0; i < labels.Count; i++)
				{
					PieReportChart.Series.Add(new PieSeries
					{
						Title = labels[i],
						Values = new ChartValues<double> { Convert.ToDouble(values[i]) },
						DataLabels = true
					});
				}
				PieReportChart.LegendLocation = LegendLocation.Right;
			}
			else if (chartType == "Линейная")
			{
				ReportChart.Visibility = Visibility.Visible;
				ReportChart.Series = new SeriesCollection
				{
					new LineSeries
					{
						Title = yTitle,
						Values = new ChartValues<double>(values.Select(v => Convert.ToDouble(v)))
					}
				};
				ReportChart.AxisX.Clear();
				ReportChart.AxisY.Clear();
				ReportChart.AxisX.Add(new Axis
				{
					Title = xTitle,
					Labels = labels
				});
				ReportChart.AxisY.Add(new Axis
				{
					Title = yTitle
				});
				ReportChart.LegendLocation = LegendLocation.Right;
			}
			else if (chartType == "Пирамидальная")
			{
				// Пирамидальная диаграмма не поддерживается LiveCharts по умолчанию.
				// Можно использовать ColumnSeries с специальной настройкой или выбрать другой тип диаграммы.
				MessageBox.Show("Пирамидальная диаграмма не поддерживается.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			else
			{
				MessageBox.Show("Выбранный тип диаграммы не поддерживается.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ExportReport_Click(object sender, RoutedEventArgs e)
		{
			string exportFormat = (ExportFormatComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
			string selectedReport = (ReportTypeComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();

			if (selectedReport == null)
			{
				MessageBox.Show("Пожалуйста, выберите отчёт для экспорта.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (ReportChart.Visibility == Visibility.Visible || PieReportChart.Visibility == Visibility.Visible)
			{
				ExportChartToImage(exportFormat);
			}
			else
			{
				MessageBox.Show("Нет диаграммы для экспорта. Сгенерируйте отчёт сначала.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void ExportChartToImage(string exportFormat)
		{
			try
			{
				RenderTargetBitmap bitmap = null;
				FrameworkElement chartElement = null;

				if (ReportChart.Visibility == Visibility.Visible)
				{
					chartElement = ReportChart;
				}
				else if (PieReportChart.Visibility == Visibility.Visible)
				{
					chartElement = PieReportChart;
				}

				if (chartElement != null)
				{
					bitmap = new RenderTargetBitmap(
						(int)chartElement.ActualWidth, (int)chartElement.ActualHeight,
						96, 96, PixelFormats.Pbgra32);
					bitmap.Render(chartElement);
				}

				if (bitmap == null)
				{
					MessageBox.Show("Диаграмма не найдена для экспорта.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
				dlg.FileName = "Report";
				dlg.DefaultExt = exportFormat switch
				{
					"PDF" => ".pdf",
					"Excel (XLSX)" => ".xlsx",
					"Word (DOCX)" => ".docx",
					_ => ".png"
				};
				dlg.Filter = exportFormat switch
				{
					"PDF" => "PDF Documents (*.pdf)|*.pdf",
					"Excel (XLSX)" => "Excel Documents (*.xlsx)|*.xlsx",
					"Word (DOCX)" => "Word Documents (*.docx)|*.docx",
					_ => "PNG Image (*.png)|*.png"
				};

				bool? result = dlg.ShowDialog();

				if (result == true)
				{
					string filename = dlg.FileName;

					if (exportFormat == "PDF")
					{
						using (PdfDocument pdf = new PdfDocument())
						{
							PdfPage page = pdf.AddPage();
							using (XGraphics gfx = XGraphics.FromPdfPage(page))
							{
								using (MemoryStream ms = new MemoryStream())
								{
									PngBitmapEncoder encoder = new PngBitmapEncoder();
									encoder.Frames.Add(BitmapFrame.Create(bitmap));
									encoder.Save(ms);
									ms.Position = 0;
									XImage image = XImage.FromStream(ms);
									gfx.DrawImage(image, 0, 0, page.Width, page.Height);
								}
							}
							pdf.Save(filename);
						}
						MessageBox.Show("Отчёт успешно экспортирован в PDF.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
					}
					else if (exportFormat == "Excel (XLSX)")
					{
						using (var workbook = new XLWorkbook())
						{
							var worksheet = workbook.Worksheets.Add("Report");
							using (MemoryStream ms = new MemoryStream())
							{
								PngBitmapEncoder encoder = new PngBitmapEncoder();
								encoder.Frames.Add(BitmapFrame.Create(bitmap));
								encoder.Save(ms);
								ms.Position = 0;
								var picture = worksheet.AddPicture(ms)
									.MoveTo(worksheet.Cell("A1"))
									.Scale(0.5);
							}
							workbook.SaveAs(filename);
						}
						MessageBox.Show("Отчёт успешно экспортирован в Excel.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
					}
					else if (exportFormat == "Word (DOCX)")
					{
						using (var document = DocX.Create(filename))
						{
							using (MemoryStream ms = new MemoryStream())
							{
								PngBitmapEncoder encoder = new PngBitmapEncoder();
								encoder.Frames.Add(BitmapFrame.Create(bitmap));
								encoder.Save(ms);
								ms.Position = 0;
								var image = document.AddImage(ms);
								var picture = image.CreatePicture();
								document.InsertParagraph().InsertPicture(picture);
							}
							document.Save();
						}
						MessageBox.Show("Отчёт успешно экспортирован в Word.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
					}
					else // PNG
					{
						using (FileStream fs = new FileStream(filename, FileMode.Create))
						{
							PngBitmapEncoder encoder = new PngBitmapEncoder();
							encoder.Frames.Add(BitmapFrame.Create(bitmap));
							encoder.Save(fs);
						}
						MessageBox.Show("Отчёт успешно экспортирован в PNG.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при экспорте отчёта: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			ThemeManager.ToggleTheme();
			UpdateThemeIcon();
		}

		public void UpdateThemeIcon()
		{
			if (ThemeIcon != null)
			{
				ThemeIcon.Kind = ThemeManager.IsDarkTheme ? PackIconMaterialKind.WeatherNight : PackIconMaterialKind.WeatherSunny;
			}
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				this.DragMove();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			DoubleAnimation slideInAnimation = new DoubleAnimation
			{
				From = -250,
				To = 0,
				Duration = TimeSpan.FromSeconds(1),
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
			};
			LeftPanelTransform.BeginAnimation(TranslateTransform.XProperty, slideInAnimation);
		}
	}

	public class Product
	{
		public int ProductID { get; set; }
		public string Name { get; set; }
	}
}
