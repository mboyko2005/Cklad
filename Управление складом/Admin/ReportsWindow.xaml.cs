using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ClosedXML.Excel;
using MahApps.Metro.IconPacks;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using Управление_складом.Themes;
using Xceed.Document.NET;
using Xceed.Words.NET;
using OxyCategoryAxis = OxyPlot.Axes.CategoryAxis;

namespace УправлениеСкладом
{
	public partial class ReportsWindow : Window, IThemeable, INotifyPropertyChanged
	{
		private string _connectionString = @"Server=DESKTOP-Q11QP9V\SQLEXPRESS;Database=УправлениеСкладом;Trusted_Connection=True;";

		private PlotModel _plotModel;
		public PlotModel PlotModel
		{
			get => _plotModel;
			set
			{
				_plotModel = value;
				OnPropertyChanged(nameof(PlotModel));
			}
		}

		public ReportsWindow()
		{
			InitializeComponent();
			DataContext = this;
			ReportTypeComboBox.SelectionChanged += ReportTypeComboBox_SelectionChanged;
			ChartTypeComboBox.SelectionChanged += ChartTypeComboBox_SelectionChanged;
			UpdateThemeIcon();
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		private void ReportTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			// Дополнительная логика при смене типа отчёта (опционально)
		}

		private void ChartTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			// Дополнительная логика при смене типа диаграммы (опционально)
		}

		private void GenerateReport_Click(object sender, RoutedEventArgs e)
		{
			string selectedReport = (ReportTypeComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
			string chartType = (ChartTypeComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();

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
					MessageBox.Show("Выберите тип отчёта.", "Предупреждение",
									MessageBoxButton.OK, MessageBoxImage.Warning);
					break;
			}
		}

		#region Генерация данных отчетов
		private void GenerateMostSoldProductsReport(string chartType)
		{
			try
			{
				using (SqlConnection conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = @"
                        SELECT TOP 10 t.Наименование, SUM(ABS(dt.Количество)) AS Продано 
                        FROM ДвиженияТоваров dt
                        INNER JOIN Товары t ON dt.ТоварID = t.ТоварID 
                        WHERE dt.ТипДвижения = N'Расход'
                        GROUP BY t.Наименование 
                        ORDER BY Продано DESC";

					SqlCommand cmd = new SqlCommand(query, conn);
					SqlDataReader reader = cmd.ExecuteReader();

					List<string> productNames = new List<string>();
					List<double> quantities = new List<double>();

					while (reader.Read())
					{
						productNames.Add(reader.GetString(0));
						quantities.Add(reader.GetInt32(1));
					}

					CreateChart(chartType, productNames, quantities,
								"Самые продаваемые товары",
								"Количество продано", "Товары");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при генерации отчёта: " + ex.Message,
								"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void GenerateSystemUsersReport(string chartType)
		{
			try
			{
				using (SqlConnection conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = @"
                        SELECT r.Наименование AS Роль, COUNT(u.ПользовательID) AS Количество
                        FROM Пользователи u
                        INNER JOIN Роли r ON u.РольID = r.РольID
                        GROUP BY r.Наименование";

					SqlCommand cmd = new SqlCommand(query, conn);
					SqlDataReader reader = cmd.ExecuteReader();

					List<string> roles = new List<string>();
					List<double> userCounts = new List<double>();

					while (reader.Read())
					{
						roles.Add(reader.GetString(0));
						userCounts.Add(reader.GetInt32(1));
					}

					CreateChart(chartType, roles, userCounts,
								"Пользователи системы",
								"Количество пользователей", "Роль");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при генерации отчёта: " + ex.Message,
								"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void GenerateTotalCostReport(string chartType)
		{
			try
			{
				using (SqlConnection conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = @"
                        SELECT t.Наименование,
                               SUM(sp.Количество * t.Цена) AS ОбщаяСтоимость 
                        FROM СкладскиеПозиции sp 
                        INNER JOIN Товары t ON sp.ТоварID = t.ТоварID 
                        GROUP BY t.Наименование";

					SqlCommand cmd = new SqlCommand(query, conn);
					SqlDataReader reader = cmd.ExecuteReader();

					List<string> productNames = new List<string>();
					List<double> totalCosts = new List<double>();

					while (reader.Read())
					{
						productNames.Add(reader.GetString(0));
						totalCosts.Add(Convert.ToDouble(reader.GetDecimal(1)));
					}

					CreateChart(chartType, productNames, totalCosts,
								"Общая стоимость товаров",
								"Стоимость (руб.)", "Товары");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при генерации отчёта: " + ex.Message,
								"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void GenerateCurrentStockPositionsReport(string chartType)
		{
			try
			{
				using (SqlConnection conn = new SqlConnection(_connectionString))
				{
					conn.Open();
					string query = @"
                        SELECT t.Наименование, sp.Количество 
                        FROM СкладскиеПозиции sp 
                        INNER JOIN Товары t ON sp.ТоварID = t.ТоварID";

					SqlCommand cmd = new SqlCommand(query, conn);
					SqlDataReader reader = cmd.ExecuteReader();

					List<string> productNames = new List<string>();
					List<double> quantities = new List<double>();

					while (reader.Read())
					{
						productNames.Add(reader.GetString(0));
						quantities.Add(reader.GetInt32(1));
					}

					CreateChart(chartType, productNames, quantities,
								"Текущие складские позиции",
								"Количество", "Товары");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка при генерации отчёта: " + ex.Message,
								"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		#endregion

		/// <summary>
		/// Универсальный метод построения диаграммы
		/// </summary>
		private void CreateChart(string chartType, List<string> labels, List<double> values,
								 string title, string xTitle, string yTitle)
		{
			// Создаём модель диаграммы
			PlotModel = new PlotModel { Title = title };
			PlotModel.Background = OxyColors.White;

			if (chartType == "Гистограмма")
			{
				// Горизонтальная гистограмма
				var categoryAxis = new OxyCategoryAxis
				{
					Position = AxisPosition.Left,
					Title = yTitle
				};
				categoryAxis.Labels.AddRange(labels);

				// Ось значений (горизонтальная), без научной нотации
				var valueAxis = new LinearAxis
				{
					Position = AxisPosition.Bottom,
					Title = xTitle,
					StringFormat = "N0"
				};

				var barSeries = new BarSeries
				{
					Title = xTitle,
					FillColor = OxyColor.Parse("#1F77B4"),
					ItemsSource = values.Select(v => new BarItem { Value = v }).ToList(),
					// Выносим метку наружу, чтобы при значении 0 она не сливалась
					LabelPlacement = LabelPlacement.Outside,
					LabelMargin = 8,
					LabelFormatString = "{0}" // отобразит 0, 100, 200 и т.д.
				};

				PlotModel.Series.Add(barSeries);
				PlotModel.Axes.Add(categoryAxis);
				PlotModel.Axes.Add(valueAxis);
			}
			else if (chartType == "Круговая")
			{
				var pieSeries = new PieSeries
				{
					StartAngle = 0,
					AngleSpan = 360,
					InnerDiameter = 0.0,
					StrokeThickness = 1.0,
					InsideLabelPosition = 0.5,
					OutsideLabelFormat = null,
					InsideLabelFormat = "{1}: {0}",
					TextColor = OxyColors.White,
					FontSize = 14
				};

				for (int i = 0; i < labels.Count; i++)
				{
					pieSeries.Slices.Add(new PieSlice(labels[i], values[i]));
				}

				PlotModel.Series.Add(pieSeries);
			}
			else if (chartType == "Линейная")
			{
				var categoryAxis = new OxyCategoryAxis
				{
					Position = AxisPosition.Bottom,
					Title = xTitle
				};
				categoryAxis.Labels.AddRange(labels);

				var valueAxis = new LinearAxis
				{
					Position = AxisPosition.Left,
					Title = yTitle,
					StringFormat = "N0"
				};

				var lineSeries = new LineSeries
				{
					Title = yTitle,
					MarkerType = MarkerType.Circle,
					MarkerSize = 6,
					MarkerStroke = OxyColors.White,
					MarkerFill = OxyColors.SkyBlue,
					StrokeThickness = 2,
					LineStyle = LineStyle.Solid
				};

				for (int i = 0; i < values.Count; i++)
				{
					lineSeries.Points.Add(new DataPoint(i, values[i]));
				}

				PlotModel.Series.Add(lineSeries);
				PlotModel.Axes.Add(categoryAxis);
				PlotModel.Axes.Add(valueAxis);
			}
			else
			{
				MessageBox.Show("Выбранный тип диаграммы не поддерживается.",
								"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// Обновляем отображение диаграммы
			PlotView.InvalidatePlot(true);
		}

		#region Экспорт отчётов (PNG/PDF/Excel/Word)
		private void ExportReport_Click(object sender, RoutedEventArgs e)
		{
			string exportFormat = (ExportFormatComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();

			if (PlotModel == null)
			{
				MessageBox.Show("Нет диаграммы для экспорта. Сгенерируйте отчёт сначала.",
								"Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			var dlg = new Microsoft.Win32.SaveFileDialog
			{
				FileName = "Report",
				DefaultExt = exportFormat switch
				{
					"PNG" => ".png",
					"PDF" => ".pdf",
					"Excel (XLSX)" => ".xlsx",
					"Word (DOCX)" => ".docx",
					_ => ".png"
				},
				Filter = exportFormat switch
				{
					"PNG" => "PNG Image (*.png)|*.png",
					"PDF" => "PDF Documents (*.pdf)|*.pdf",
					"Excel (XLSX)" => "Excel Documents (*.xlsx)|*.xlsx",
					"Word (DOCX)" => "Word Documents (*.docx)|*.docx",
					_ => "PNG Image (*.png)|*.png"
				}
			};

			bool? result = dlg.ShowDialog();
			if (result == true)
			{
				string filename = dlg.FileName;

				try
				{
					switch (exportFormat)
					{
						case "PNG":
							ExportToPng(filename);
							break;
						case "PDF":
							ExportToPdf(filename);
							break;
						case "Excel (XLSX)":
							ExportToExcel(filename);
							break;
						case "Word (DOCX)":
							ExportToWord(filename);
							break;
						default:
							MessageBox.Show("Неподдерживаемый формат экспорта.",
											"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
							break;
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show("Ошибка при экспорте отчёта: " + ex.Message,
									"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void ExportToPng(string filename)
		{
			var pngExporter = new PngExporter { Width = 800, Height = 600 };
			pngExporter.ExportToFile(PlotModel, filename);
			MessageBox.Show("Отчёт успешно экспортирован в PNG.",
							"Успех", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void ExportToPdf(string filename)
		{
			var pdfExporter = new PdfExporter { Width = 800, Height = 600 };
			pdfExporter.ExportToFile(PlotModel, filename);
			MessageBox.Show("Отчёт успешно экспортирован в PDF.",
							"Успех", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void ExportToExcel(string filename)
		{
			var pngExporter = new PngExporter { Width = 800, Height = 600 };
			using (var stream = new MemoryStream())
			{
				// 1) Экспортируем диаграмму в PNG (память)
				pngExporter.Export(PlotModel, stream);
				stream.Position = 0;

				// 2) Создаём Excel-книгу, добавляем лист и вставляем картинку
				var workbook = new XLWorkbook();
				var worksheet = workbook.Worksheets.Add("Отчёт");

				worksheet.Cell(1, 1).Value = "Отчёт: " + PlotModel.Title;
				worksheet.Cell(2, 1).Value = "Дата создания: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

				// 3) Добавляем картинку
				worksheet.AddPicture(stream, "Chart")
						 .MoveTo(worksheet.Cell(4, 1))
						 .Scale(1.0);

				worksheet.Columns().AdjustToContents();
				workbook.SaveAs(filename);
			}
			MessageBox.Show("Отчёт успешно экспортирован в Excel.",
							"Успех", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void ExportToWord(string filename)
		{
			var pngExporter = new PngExporter { Width = 800, Height = 600 };
			using (var stream = new MemoryStream())
			{
				// 1) Экспортируем диаграмму в PNG (память)
				pngExporter.Export(PlotModel, stream);
				stream.Position = 0;

				// 2) Создаём Word-документ
				var document = DocX.Create(filename);

				// Заголовок
				var titleParagraph = document.InsertParagraph("Отчёт: " + PlotModel.Title)
											 .FontSize(18)
											 .Bold();
				// Убедитесь, что Alignment.center/Alignment.centre/Alignment.Center 
				// доступен именно в вашей версии DocX:
				titleParagraph.Alignment = Alignment.center;

				// Дата
				var dateParagraph = document.InsertParagraph("Дата создания: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
				dateParagraph.Alignment = Alignment.center;

				// Отступ
				document.InsertParagraph().SpacingAfter(20);

				// 3) Создаём картинку c фиксированными размерами, 
				//    чтобы не обрезалось в Word
				var image = document.AddImage(stream);
				// Можно задать размеры (width, height) напрямую:
				var picture = image.CreatePicture(800, 600);

				var pictureParagraph = document.InsertParagraph()
											   .AppendPicture(picture);
				pictureParagraph.Alignment = Alignment.center;

				// 4) Сохраняем документ
				document.Save();
			}
			MessageBox.Show("Отчёт успешно экспортирован в Word.",
							"Успех", MessageBoxButton.OK, MessageBoxImage.Information);
		}
		#endregion

		private void ToggleTheme_Click(object sender, RoutedEventArgs e)
		{
			ThemeManager.ToggleTheme();
			UpdateThemeIcon();
		}

		public void UpdateThemeIcon()
		{
			if (ThemeIcon != null)
			{
				ThemeIcon.Kind = ThemeManager.IsDarkTheme
					? PackIconMaterialKind.WeatherNight
					: PackIconMaterialKind.WeatherSunny;
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
}