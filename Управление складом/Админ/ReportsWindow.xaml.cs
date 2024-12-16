// ReportsWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MahApps.Metro.IconPacks;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Управление_складом.Themes;
using OxyPlot.Wpf;
using System.Windows.Media;

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
            // Optional: Add additional logic when the report type changes
        }

        private void ChartTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Optional: Add additional logic when the chart type changes
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
                    string query = @"SELECT TOP 10 t.Наименование, SUM(ABS(dt.Количество)) AS Продано 
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

                    CreateChart(chartType, productNames, quantities, "Самые продаваемые товары", "Количество продано", "Товары");
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
                    List<double> userCounts = new List<double>();

                    while (reader.Read())
                    {
                        roles.Add(reader.GetString(0));
                        userCounts.Add(reader.GetInt32(1));
                    }

                    CreateChart(chartType, roles, userCounts, "Пользователи системы", "Количество пользователей", "Роль");
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
                    List<double> totalCosts = new List<double>();

                    while (reader.Read())
                    {
                        productNames.Add(reader.GetString(0));
                        totalCosts.Add(Convert.ToDouble(reader.GetDecimal(1)));
                    }

                    CreateChart(chartType, productNames, totalCosts, "Общая стоимость товаров", "Стоимость (руб.)", "Товары");
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
                    List<double> quantities = new List<double>();

                    while (reader.Read())
                    {
                        productNames.Add(reader.GetString(0));
                        quantities.Add(reader.GetInt32(1));
                    }

                    CreateChart(chartType, productNames, quantities, "Текущие складские позиции", "Количество", "Товары");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при генерации отчёта: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateChart(string chartType, List<string> labels, List<double> values, string title, string xTitle, string yTitle)
        {
            PlotModel = new PlotModel { Title = title };
            PlotModel.Background = OxyColors.White;

            if (chartType == "Гистограмма")
            {
                var categoryAxis = new CategoryAxis
                {
                    Position = AxisPosition.Left,
                    Title = yTitle
                };
                categoryAxis.Labels.AddRange(labels);

                var valueAxis = new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = xTitle
                };

                var series = new BarSeries
                {
                    Title = xTitle,
                    FillColor = OxyColor.Parse("#1F77B4"),
                    ItemsSource = values.Select(v => new BarItem { Value = v }).ToList(),
                    LabelPlacement = LabelPlacement.Inside,
                    LabelFormatString = "{0}"
                };

                PlotModel.Series.Add(series);
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
                var categoryAxis = new CategoryAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = xTitle
                };
                categoryAxis.Labels.AddRange(labels);

                var valueAxis = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = yTitle
                };

                var series = new LineSeries
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
                    series.Points.Add(new DataPoint(i, values[i]));
                }

                PlotModel.Series.Add(series);
                PlotModel.Axes.Add(categoryAxis);
                PlotModel.Axes.Add(valueAxis);
            }
            else
            {
                MessageBox.Show("Выбранный тип диаграммы не поддерживается.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Refresh the plot view
            PlotView.InvalidatePlot(true);
        }

        private void ExportReport_Click(object sender, RoutedEventArgs e)
        {
            string exportFormat = (ExportFormatComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();

            if (PlotModel == null)
            {
                MessageBox.Show("Нет диаграммы для экспорта. Сгенерируйте отчёт сначала.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
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
                            MessageBox.Show("Неподдерживаемый формат экспорта.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при экспорте отчёта: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportToPng(string filename)
        {
            var pngExporter = new PngExporter { Width = 800, Height = 600 };
            pngExporter.ExportToFile(PlotModel, filename);
            MessageBox.Show("Отчёт успешно экспортирован в PNG.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportToPdf(string filename)
        {
            var pdfExporter = new PdfExporter { Width = 800, Height = 600 };
            pdfExporter.ExportToFile(PlotModel, filename);
            MessageBox.Show("Отчёт успешно экспортирован в PDF.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportToExcel(string filename)
        {
            // Реализация экспорта в Excel (например, используя ClosedXML)
            MessageBox.Show("Экспорт в Excel не реализован.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportToWord(string filename)
        {
            // Реализация экспорта в Word (например, используя DocX)
            MessageBox.Show("Экспорт в Word не реализован.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
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
}
