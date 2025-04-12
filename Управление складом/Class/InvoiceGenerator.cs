using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Printing;
using System.IO;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;

namespace Управление_складом.Class
{
    public class InvoiceGenerator
    {
        private string connectionString;
        private int productId;
        private string sourceWarehouse;
        private string targetWarehouse;
        private int quantity;
        private int userId;

        public InvoiceGenerator(string connectionString, int productId, string sourceWarehouse, string targetWarehouse, int quantity, int userId)
        {
            this.connectionString = connectionString;
            this.productId = productId;
            this.sourceWarehouse = sourceWarehouse;
            this.targetWarehouse = targetWarehouse;
            this.quantity = quantity;
            this.userId = userId;
        }

        public void GenerateAndPrintInvoice()
        {
            try
            {
                // Создание документа
                FlowDocument document = CreateInvoiceDocument();
                
                // Печать документа
                PrintDocument(document);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании накладной: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument CreateInvoiceDocument()
        {
            // Получение данных о товаре и пользователях
            string productName = string.Empty;
            decimal productPrice = 0;
            string senderName = "________________";  // Пробелы для заполнения вручную
            string receiverName = string.Empty;
            string senderPosition = "Мастер";
            string receiverPosition = "Кладовщик";
            
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                
                // Получаем информацию о товаре
                string productQuery = @"SELECT t.Наименование, ISNULL(t.Цена, 0) AS Цена 
                                     FROM Товары t 
                                     WHERE t.ТоварID = @productId";
                
                using (SqlCommand cmd = new SqlCommand(productQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@productId", productId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            productName = reader.GetString(0);
                            productPrice = reader.GetDecimal(1);
                        }
                    }
                }
                
                // Получаем логин пользователя (кладовщика)
                try
                {
                    // Пробуем получить логин из настроек, если он там сохранен
                    // Если в вашей системе есть статический класс для доступа к логину, используйте его
                    string userQuery = @"SELECT Логин 
                                        FROM Пользователи 
                                        WHERE ПользовательID = @userId";
                    using (SqlCommand cmd = new SqlCommand(userQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        object result = cmd.ExecuteScalar();
                        
                        if (result != null && result != DBNull.Value)
                        {
                            receiverName = result.ToString();
                        }
                        else
                        {
                            // Если логин не найден, оставляем пустое место для заполнения вручную
                            receiverName = "________________";
                        }
                    }
                }
                catch (Exception)
                {
                    // Если возникла ошибка, оставляем пустое место для заполнения вручную
                    receiverName = "________________";
                }
            }
            
            // Создаем документ
            FlowDocument document = new FlowDocument();
            document.PagePadding = new Thickness(50);
            document.ColumnWidth = 700;
            // Установка шрифта для всего документа
            document.FontFamily = new FontFamily("Times New Roman");
            document.FontSize = 11;
            
            // Шапка документа - верхняя часть с "Унифицированная форма..."
            Paragraph unifiedFormPara = new Paragraph();
            unifiedFormPara.TextAlignment = TextAlignment.Right;
            unifiedFormPara.Margin = new Thickness(0, 0, 0, 5);
            Run unifiedFormRun = new Run("Унифицированная форма № ТОРГ-13");
            unifiedFormRun.FontSize = 9;
            unifiedFormPara.Inlines.Add(unifiedFormRun);
            document.Blocks.Add(unifiedFormPara);
            
            Paragraph approvedPara = new Paragraph();
            approvedPara.TextAlignment = TextAlignment.Right;
            approvedPara.Margin = new Thickness(0, 0, 0, 15);
            Run approvedRun = new Run("Утверждена постановлением Госкомстата России от 25.12.98 № 132");
            approvedRun.FontSize = 9;
            approvedPara.Inlines.Add(approvedRun);
            document.Blocks.Add(approvedPara);
            
            // Шапка документа
            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(100) });
            
            // Код
            TextBlock codeLabel = new TextBlock();
            codeLabel.Text = "Код";
            codeLabel.HorizontalAlignment = HorizontalAlignment.Right;
            codeLabel.VerticalAlignment = VerticalAlignment.Bottom;
            codeLabel.Margin = new Thickness(0, 0, 0, 3);
            Grid.SetColumn(codeLabel, 1);
            headerGrid.Children.Add(codeLabel);
            
            Border codeBorder = new Border();
            codeBorder.BorderBrush = Brushes.Black;
            codeBorder.BorderThickness = new Thickness(1);
            codeBorder.Margin = new Thickness(0, 3, 0, 0);
            TextBlock codeText = new TextBlock();
            codeText.Text = "0330213";
            codeText.HorizontalAlignment = HorizontalAlignment.Center;
            codeText.VerticalAlignment = VerticalAlignment.Center;
            codeText.Padding = new Thickness(5, 2, 5, 2);
            codeBorder.Child = codeText;
            Grid.SetColumn(codeBorder, 1);
            headerGrid.Children.Add(codeBorder);
            
            Section headerSection = new Section();
            headerSection.Blocks.Add(new BlockUIContainer(headerGrid));
            
            // Добавляем headerSection в документ
            document.Blocks.Add(headerSection);
            
            // ОКПО
            StackPanel okpoPanel = new StackPanel();
            okpoPanel.HorizontalAlignment = HorizontalAlignment.Right;
            okpoPanel.Margin = new Thickness(0, 10, 0, 0);
            
            TextBlock okpoLabel = new TextBlock();
            okpoLabel.Text = "по ОКПО";
            okpoLabel.HorizontalAlignment = HorizontalAlignment.Right;
            okpoLabel.Margin = new Thickness(0, 0, 0, 3);
            okpoPanel.Children.Add(okpoLabel);
            
            Border okpoBorder = new Border();
            okpoBorder.BorderBrush = Brushes.Black;
            okpoBorder.BorderThickness = new Thickness(1);
            okpoBorder.Width = 100;
            TextBlock okpoText = new TextBlock();
            okpoText.Text = "37682759";
            okpoText.HorizontalAlignment = HorizontalAlignment.Center;
            okpoText.VerticalAlignment = VerticalAlignment.Center;
            okpoText.Padding = new Thickness(5, 2, 5, 2);
            okpoBorder.Child = okpoText;
            okpoPanel.Children.Add(okpoBorder);
            
            BlockUIContainer okpoContainer = new BlockUIContainer(okpoPanel);
            document.Blocks.Add(okpoContainer);
            
            // Название организации
            Paragraph companyPara = new Paragraph(new Run("ООО «Фирма ОренКлип»"));
            companyPara.TextAlignment = TextAlignment.Center;
            companyPara.FontWeight = FontWeights.Bold;
            companyPara.Margin = new Thickness(0, 10, 0, 0);
            document.Blocks.Add(companyPara);
            
            Paragraph organizationPara = new Paragraph(new Run("(организация)"));
            organizationPara.TextAlignment = TextAlignment.Center;
            organizationPara.FontSize = 9;
            organizationPara.Margin = new Thickness(0, 0, 0, 10);
            document.Blocks.Add(organizationPara);
            
            // Номер и дата накладной
            Table numberDateTable = new Table();
            numberDateTable.CellSpacing = 0;
            numberDateTable.BorderBrush = Brushes.Black;
            numberDateTable.BorderThickness = new Thickness(1);
            numberDateTable.Margin = new Thickness(0, 0, 0, 10);
            
            numberDateTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            numberDateTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            
            TableRowGroup numberDateRowGroup = new TableRowGroup();
            
            // Заголовок таблицы
            TableRow headerNumberDateRow = new TableRow();
            Paragraph numberHeaderPara = new Paragraph(new Run("Номер"));
            numberHeaderPara.TextAlignment = TextAlignment.Center;
            numberHeaderPara.FontWeight = FontWeights.Bold;
            headerNumberDateRow.Cells.Add(new TableCell(numberHeaderPara));
            
            Paragraph dateHeaderPara = new Paragraph(new Run("Дата"));
            dateHeaderPara.TextAlignment = TextAlignment.Center;
            dateHeaderPara.FontWeight = FontWeights.Bold;
            headerNumberDateRow.Cells.Add(new TableCell(dateHeaderPara));
            
            numberDateRowGroup.Rows.Add(headerNumberDateRow);
            
            // Значения
            TableRow valueNumberDateRow = new TableRow();
            // Генерируем случайный номер накладной
            Random rnd = new Random();
            int invoiceNumber = rnd.Next(100, 999);
            
            Paragraph numberValuePara = new Paragraph(new Run(invoiceNumber.ToString()));
            numberValuePara.TextAlignment = TextAlignment.Center;
            valueNumberDateRow.Cells.Add(new TableCell(numberValuePara));
            
            Paragraph dateValuePara = new Paragraph(new Run(DateTime.Now.ToString("dd.MM.yyyy")));
            dateValuePara.TextAlignment = TextAlignment.Center;
            valueNumberDateRow.Cells.Add(new TableCell(dateValuePara));
            
            numberDateRowGroup.Rows.Add(valueNumberDateRow);
            
            numberDateTable.RowGroups.Add(numberDateRowGroup);
            document.Blocks.Add(numberDateTable);
            
            // Заголовок накладной
            Paragraph titlePara = new Paragraph(new Run("НАКЛАДНАЯ"));
            titlePara.TextAlignment = TextAlignment.Center;
            titlePara.FontWeight = FontWeights.Bold;
            titlePara.FontSize = 16;
            titlePara.Margin = new Thickness(0, 5, 0, 5);
            document.Blocks.Add(titlePara);
            
            Paragraph subtitlePara = new Paragraph(new Run("НА ВНУТРЕННЕЕ ПЕРЕМЕЩЕНИЕ, ПЕРЕДАЧУ ТОВАРОВ, ТАРЫ"));
            subtitlePara.TextAlignment = TextAlignment.Center;
            subtitlePara.FontWeight = FontWeights.Bold;
            subtitlePara.FontSize = 14;
            subtitlePara.Margin = new Thickness(0, 0, 0, 10);
            document.Blocks.Add(subtitlePara);
            
            // Таблица с отправителем и получателем
            Table mainTable = new Table();
            mainTable.CellSpacing = 0;
            mainTable.BorderBrush = Brushes.Black;
            mainTable.BorderThickness = new Thickness(1);
            mainTable.Margin = new Thickness(0, 0, 0, 10);
            
            mainTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            mainTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            mainTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            
            TableRowGroup mainRowGroup = new TableRowGroup();
            
            // Заголовок отправитель/получатель
            TableRow headerRow = new TableRow();
            
            Paragraph senderHeaderPara = new Paragraph(new Run("Отправитель"));
            senderHeaderPara.TextAlignment = TextAlignment.Center;
            senderHeaderPara.FontWeight = FontWeights.Bold;
            TableCell senderCell = new TableCell(senderHeaderPara);
            headerRow.Cells.Add(senderCell);
            
            Paragraph receiverHeaderPara = new Paragraph(new Run("Получатель"));
            receiverHeaderPara.TextAlignment = TextAlignment.Center;
            receiverHeaderPara.FontWeight = FontWeights.Bold;
            TableCell receiverCell = new TableCell(receiverHeaderPara);
            headerRow.Cells.Add(receiverCell);
            
            Paragraph accountHeaderPara = new Paragraph(new Run("Корреспондирующий счет"));
            accountHeaderPara.TextAlignment = TextAlignment.Center;
            accountHeaderPara.FontWeight = FontWeights.Bold;
            TableCell accountCell = new TableCell(accountHeaderPara);
            headerRow.Cells.Add(accountCell);
            
            mainRowGroup.Rows.Add(headerRow);
            
            // Подзаголовки
            TableRow subHeaderRow = new TableRow();
            
            Paragraph senderSubheaderPara = new Paragraph(new Run("структурное подразделение"));
            senderSubheaderPara.TextAlignment = TextAlignment.Center;
            senderSubheaderPara.FontStyle = FontStyles.Italic;
            senderSubheaderPara.FontSize = 9;
            TableCell senderDeptCell = new TableCell(senderSubheaderPara);
            subHeaderRow.Cells.Add(senderDeptCell);
            
            Paragraph receiverSubheaderPara = new Paragraph(new Run("структурное подразделение"));
            receiverSubheaderPara.TextAlignment = TextAlignment.Center;
            receiverSubheaderPara.FontStyle = FontStyles.Italic;
            receiverSubheaderPara.FontSize = 9;
            TableCell receiverDeptCell = new TableCell(receiverSubheaderPara);
            subHeaderRow.Cells.Add(receiverDeptCell);
            
            // Подзаголовки для счета в одной ячейке
            TableCell accountSubCell = new TableCell();
            
            Table accountSubTable = new Table();
            accountSubTable.CellSpacing = 0;
            accountSubTable.BorderBrush = Brushes.Black;
            accountSubTable.BorderThickness = new Thickness(1, 1, 0, 0);
            
            accountSubTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            accountSubTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            
            TableRowGroup accountSubRowGroup = new TableRowGroup();
            TableRow accountSubRow = new TableRow();
            
            Paragraph accountSubheader1 = new Paragraph(new Run("счет, субсчет"));
            accountSubheader1.TextAlignment = TextAlignment.Center;
            accountSubheader1.FontStyle = FontStyles.Italic;
            accountSubheader1.FontSize = 9;
            accountSubRow.Cells.Add(new TableCell(accountSubheader1));
            
            Paragraph accountSubheader2 = new Paragraph(new Run("код аналитического учета"));
            accountSubheader2.TextAlignment = TextAlignment.Center;
            accountSubheader2.FontStyle = FontStyles.Italic;
            accountSubheader2.FontSize = 9;
            accountSubRow.Cells.Add(new TableCell(accountSubheader2));
            
            accountSubRowGroup.Rows.Add(accountSubRow);
            accountSubTable.RowGroups.Add(accountSubRowGroup);
            
            // Создаем двойной текст для имитации таблицы
            Paragraph accountSubParagraph = new Paragraph();
            accountSubParagraph.TextAlignment = TextAlignment.Center;
            accountSubParagraph.FontSize = 9;
            accountSubParagraph.Inlines.Add(new Run("счет, субсчет"));
            accountSubParagraph.Inlines.Add(new Run("    |    "));
            accountSubParagraph.Inlines.Add(new Run("код аналитического учета"));
            accountSubCell.Blocks.Add(accountSubParagraph);
            
            subHeaderRow.Cells.Add(accountSubCell);
            
            mainRowGroup.Rows.Add(subHeaderRow);
            
            // Данные отправителя/получателя
            TableRow dataRow = new TableRow();
            
            Paragraph senderNamePara = new Paragraph(new Run("Главный склад"));
            senderNamePara.TextAlignment = TextAlignment.Center;
            senderNamePara.FontWeight = FontWeights.Bold;
            dataRow.Cells.Add(new TableCell(senderNamePara));
            
            Paragraph receiverNamePara = new Paragraph(new Run("Дополнительный склад"));
            receiverNamePara.TextAlignment = TextAlignment.Center;
            receiverNamePara.FontWeight = FontWeights.Bold;
            dataRow.Cells.Add(new TableCell(receiverNamePara));
            
            // Данные счета
            TableCell accountDataCell = new TableCell();
            Table accountDataTable = new Table();
            accountDataTable.CellSpacing = 0;
            accountDataTable.BorderBrush = Brushes.Black;
            accountDataTable.BorderThickness = new Thickness(1, 1, 0, 0);
            
            accountDataTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            accountDataTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            
            TableRowGroup accountDataRowGroup = new TableRowGroup();
            TableRow accountDataRow = new TableRow();
            
            Paragraph accountDataPara = new Paragraph(new Run("20"));
            accountDataPara.TextAlignment = TextAlignment.Center;
            accountDataPara.FontWeight = FontWeights.Bold;
            accountDataRow.Cells.Add(new TableCell(accountDataPara));
            
            accountDataRow.Cells.Add(new TableCell(new Paragraph()));
            
            accountDataRowGroup.Rows.Add(accountDataRow);
            accountDataTable.RowGroups.Add(accountDataRowGroup);
            
            // Снова, поскольку нельзя встраивать таблицы, упрощаем
            Paragraph accountDataSimplePara = new Paragraph(new Run("20"));
            accountDataSimplePara.TextAlignment = TextAlignment.Center;
            accountDataSimplePara.FontWeight = FontWeights.Bold;
            accountDataCell.Blocks.Add(accountDataSimplePara);
            
            dataRow.Cells.Add(accountDataCell);
            
            mainRowGroup.Rows.Add(dataRow);
            
            // Деятельность
            TableRow activityRow = new TableRow();
            
            Paragraph senderActivityPara = new Paragraph(new Run("Основное производство"));
            senderActivityPara.TextAlignment = TextAlignment.Center;
            senderActivityPara.FontStyle = FontStyles.Italic;
            senderActivityPara.Foreground = Brushes.DarkBlue;
            activityRow.Cells.Add(new TableCell(senderActivityPara));
            
            Paragraph receiverActivityPara = new Paragraph(new Run("хранение"));
            receiverActivityPara.TextAlignment = TextAlignment.Center;
            receiverActivityPara.FontStyle = FontStyles.Italic;
            receiverActivityPara.Foreground = Brushes.DarkBlue;
            activityRow.Cells.Add(new TableCell(receiverActivityPara));
            
            activityRow.Cells.Add(new TableCell(new Paragraph()));
            
            mainRowGroup.Rows.Add(activityRow);
            
            mainTable.RowGroups.Add(mainRowGroup);
            document.Blocks.Add(mainTable);
            
            // Создаем таблицу товаров с такой же структурой, как на образце
            Table productsTable = new Table();
            productsTable.CellSpacing = 0;
            productsTable.BorderBrush = Brushes.Black;
            productsTable.BorderThickness = new Thickness(1);
            productsTable.Margin = new Thickness(0, 0, 0, 10);
            
            // Определяем колонки для товарной таблицы
            productsTable.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) }); // Наименование
            productsTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) }); // Единица измерения  
            productsTable.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) }); // Количество
            productsTable.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) }); // Цена
            
            TableRowGroup productsRowGroup = new TableRowGroup();
            
            // Заголовки для товаров
            TableRow productHeaderRow = new TableRow();
            
            Paragraph productHeaderPara = new Paragraph(new Run("Товар, тара"));
            productHeaderPara.TextAlignment = TextAlignment.Center;
            productHeaderPara.FontWeight = FontWeights.Bold;
            productHeaderRow.Cells.Add(new TableCell(productHeaderPara));
            
            Paragraph unitHeaderPara = new Paragraph(new Run("Ед. изм."));
            unitHeaderPara.TextAlignment = TextAlignment.Center;
            unitHeaderPara.FontWeight = FontWeights.Bold;
            productHeaderRow.Cells.Add(new TableCell(unitHeaderPara));
            
            Paragraph qtyHeaderPara = new Paragraph(new Run("Отпущено"));
            qtyHeaderPara.TextAlignment = TextAlignment.Center;
            qtyHeaderPara.FontWeight = FontWeights.Bold;
            productHeaderRow.Cells.Add(new TableCell(qtyHeaderPara));
            
            Paragraph priceHeaderPara = new Paragraph(new Run("По учетным ценам"));
            priceHeaderPara.TextAlignment = TextAlignment.Center;
            priceHeaderPara.FontWeight = FontWeights.Bold;
            productHeaderRow.Cells.Add(new TableCell(priceHeaderPara));
            
            productsRowGroup.Rows.Add(productHeaderRow);
            
            // Подзаголовки товарной таблицы
            TableRow productSubHeaderRow = new TableRow();
            
            Paragraph nameSubHeaderPara = new Paragraph(new Run("наименование, характеристика"));
            nameSubHeaderPara.TextAlignment = TextAlignment.Center;
            nameSubHeaderPara.FontStyle = FontStyles.Italic;
            nameSubHeaderPara.FontSize = 9;
            productSubHeaderRow.Cells.Add(new TableCell(nameSubHeaderPara));
            
            Paragraph unitSubHeaderPara = new Paragraph();
            productSubHeaderRow.Cells.Add(new TableCell(unitSubHeaderPara));
            
            Paragraph qtySubHeaderPara = new Paragraph(new Run("количество"));
            qtySubHeaderPara.TextAlignment = TextAlignment.Center;
            qtySubHeaderPara.FontStyle = FontStyles.Italic;
            qtySubHeaderPara.FontSize = 9;
            productSubHeaderRow.Cells.Add(new TableCell(qtySubHeaderPara));
            
            Paragraph priceSubHeaderPara = new Paragraph();
            priceSubHeaderPara.TextAlignment = TextAlignment.Center;
            priceSubHeaderPara.FontStyle = FontStyles.Italic;
            priceSubHeaderPara.FontSize = 9;
            Run priceRunLeft = new Run("цена, ");
            Run priceRunRight = new Run("сумма, ");
            priceSubHeaderPara.Inlines.Add(priceRunLeft);
            priceSubHeaderPara.Inlines.Add(new LineBreak());
            priceSubHeaderPara.Inlines.Add(priceRunRight);
            priceSubHeaderPara.Inlines.Add(new LineBreak());
            priceSubHeaderPara.Inlines.Add(new Run("руб. коп."));
            productSubHeaderRow.Cells.Add(new TableCell(priceSubHeaderPara));
            
            productsRowGroup.Rows.Add(productSubHeaderRow);
            
            // Номера колонок
            TableRow productIndexRow = new TableRow();
            productIndexRow.Background = Brushes.LightGray;
            
            Paragraph indexPara1 = new Paragraph(new Run("1"));
            indexPara1.TextAlignment = TextAlignment.Center;
            indexPara1.FontSize = 9;
            productIndexRow.Cells.Add(new TableCell(indexPara1));
            
            Paragraph indexPara2 = new Paragraph(new Run("2"));
            indexPara2.TextAlignment = TextAlignment.Center;
            indexPara2.FontSize = 9;
            productIndexRow.Cells.Add(new TableCell(indexPara2));
            
            Paragraph indexPara3 = new Paragraph(new Run("3"));
            indexPara3.TextAlignment = TextAlignment.Center;
            indexPara3.FontSize = 9;
            productIndexRow.Cells.Add(new TableCell(indexPara3));
            
            Paragraph indexPara4 = new Paragraph(new Run("4"));
            indexPara4.TextAlignment = TextAlignment.Center;
            indexPara4.FontSize = 9;
            productIndexRow.Cells.Add(new TableCell(indexPara4));
            
            productsRowGroup.Rows.Add(productIndexRow);
            
            // Данные о товаре
            TableRow productDataRow = new TableRow();
            
            // Наименование товара
            Paragraph productNamePara = new Paragraph(new Run(productName));
            productNamePara.TextAlignment = TextAlignment.Left;
            productNamePara.Margin = new Thickness(5, 0, 0, 0);
            productDataRow.Cells.Add(new TableCell(productNamePara));
            
            // Единица измерения
            Paragraph productUnitPara = new Paragraph(new Run("шт"));
            productUnitPara.TextAlignment = TextAlignment.Center;
            productDataRow.Cells.Add(new TableCell(productUnitPara));
            
            // Количество
            Paragraph productQuantityPara = new Paragraph(new Run(quantity.ToString()));
            productQuantityPara.TextAlignment = TextAlignment.Center;
            productDataRow.Cells.Add(new TableCell(productQuantityPara));
            
            // Цена и сумма
            Paragraph productPricePara = new Paragraph();
            decimal totalSum = productPrice * quantity;
            
            productPricePara.TextAlignment = TextAlignment.Right;
            productPricePara.Margin = new Thickness(0, 0, 5, 0);
            
            Run priceRun = new Run(productPrice.ToString("0"));
            Run totalRun = new Run(totalSum.ToString("0"));
            
            productPricePara.Inlines.Add(priceRun);
            productPricePara.Inlines.Add(new LineBreak());
            productPricePara.Inlines.Add(totalRun);
            
            productDataRow.Cells.Add(new TableCell(productPricePara));
            
            productsRowGroup.Rows.Add(productDataRow);
            
            // Итоговая строка
            TableRow totalRow = new TableRow();
            
            Paragraph totalLabelPara = new Paragraph(new Run("Итого"));
            totalLabelPara.TextAlignment = TextAlignment.Right;
            totalLabelPara.FontWeight = FontWeights.Bold;
            totalLabelPara.Margin = new Thickness(0, 0, 10, 0);
            TableCell totalLabelCell = new TableCell(totalLabelPara);
            totalLabelCell.ColumnSpan = 2;
            totalRow.Cells.Add(totalLabelCell);
            
            Paragraph totalQtyPara = new Paragraph(new Run(quantity.ToString()));
            totalQtyPara.TextAlignment = TextAlignment.Center;
            totalQtyPara.FontWeight = FontWeights.Bold;
            totalRow.Cells.Add(new TableCell(totalQtyPara));
            
            Paragraph totalPricePara = new Paragraph(new Run(totalSum.ToString("0")));
            totalPricePara.TextAlignment = TextAlignment.Right;
            totalPricePara.FontWeight = FontWeights.Bold;
            totalPricePara.Margin = new Thickness(0, 0, 5, 0);
            totalRow.Cells.Add(new TableCell(totalPricePara));
            
            productsRowGroup.Rows.Add(totalRow);
            
            // Всего по накладной
            TableRow grandTotalRow = new TableRow();
            
            Paragraph grandTotalLabelPara = new Paragraph(new Run("Всего по накладной"));
            grandTotalLabelPara.TextAlignment = TextAlignment.Right;
            grandTotalLabelPara.FontWeight = FontWeights.Bold;
            grandTotalLabelPara.Margin = new Thickness(0, 0, 10, 0);
            TableCell grandTotalLabelCell = new TableCell(grandTotalLabelPara);
            grandTotalLabelCell.ColumnSpan = 2;
            grandTotalRow.Cells.Add(grandTotalLabelCell);
            
            Paragraph grandTotalQtyPara = new Paragraph(new Run(quantity.ToString()));
            grandTotalQtyPara.TextAlignment = TextAlignment.Center;
            grandTotalQtyPara.FontWeight = FontWeights.Bold;
            grandTotalRow.Cells.Add(new TableCell(grandTotalQtyPara));
            
            Paragraph grandTotalPricePara = new Paragraph(new Run(totalSum.ToString("0")));
            grandTotalPricePara.TextAlignment = TextAlignment.Right;
            grandTotalPricePara.FontWeight = FontWeights.Bold;
            grandTotalPricePara.Margin = new Thickness(0, 0, 5, 0);
            grandTotalRow.Cells.Add(new TableCell(grandTotalPricePara));
            
            productsRowGroup.Rows.Add(grandTotalRow);
            
            productsTable.RowGroups.Add(productsRowGroup);
            document.Blocks.Add(productsTable);
            
            // Подписи должностных лиц
            // Отпустил
            Paragraph senderSigPara = new Paragraph();
            senderSigPara.Margin = new Thickness(0, 0, 0, 10);
            
            Run sentByRun = new Run("Отпустил   ");
            sentByRun.FontWeight = FontWeights.Bold;
            senderSigPara.Inlines.Add(sentByRun);
            
            Run sentPositionRun = new Run(senderPosition);
            sentPositionRun.TextDecorations = TextDecorations.Underline;
            senderSigPara.Inlines.Add(sentPositionRun);
            
            senderSigPara.Inlines.Add(new Run("               "));
            
            Run signatureRun = new Run("Подпись");
            signatureRun.TextDecorations = TextDecorations.Underline;
            senderSigPara.Inlines.Add(signatureRun);
            
            senderSigPara.Inlines.Add(new Run("               "));
            
            Run senderNameRun = new Run(senderName);
            senderNameRun.TextDecorations = null;
            senderSigPara.Inlines.Add(senderNameRun);
            
            document.Blocks.Add(senderSigPara);
            
            // Сумма прописью
            Paragraph amountTextPara = new Paragraph();
            amountTextPara.Margin = new Thickness(0, 0, 0, 5);
            
            Run goodsQualityRun = new Run("товар и тару по количеству и надлежащему качеству на");
            amountTextPara.Inlines.Add(goodsQualityRun);
            
            Run sumRun = new Run(" сумму");
            sumRun.FontWeight = FontWeights.Bold;
            amountTextPara.Inlines.Add(sumRun);
            
            document.Blocks.Add(amountTextPara);
            
            // Сумма прописью
            Paragraph amountPara = new Paragraph();
            amountPara.Margin = new Thickness(0, 0, 0, 15);
            
            // Преобразуем сумму в прописью
            string amountInWords = NumberToText(totalSum);
            
            Run amountWordsRun = new Run(amountInWords);
            amountWordsRun.TextDecorations = TextDecorations.Underline;
            amountPara.Inlines.Add(amountWordsRun);
            
            amountPara.Inlines.Add(new Run("                                          "));
            
            Run rubRun = new Run("руб.");
            rubRun.FontWeight = FontWeights.Bold;
            amountPara.Inlines.Add(rubRun);
            
            amountPara.Inlines.Add(new Run("    "));
            
            Run kopRun = new Run("00");
            kopRun.TextDecorations = TextDecorations.Underline;
            amountPara.Inlines.Add(kopRun);
            
            amountPara.Inlines.Add(new Run("    "));
            
            Run kopTextRun = new Run("коп");
            kopTextRun.FontWeight = FontWeights.Bold;
            amountPara.Inlines.Add(kopTextRun);
            
            document.Blocks.Add(amountPara);
            
            // Получил
            Paragraph receiverSigPara = new Paragraph();
            
            Run receivedByRun = new Run("Получил   ");
            receivedByRun.FontWeight = FontWeights.Bold;
            receiverSigPara.Inlines.Add(receivedByRun);
            
            Run receivedPositionRun = new Run(receiverPosition);
            receivedPositionRun.TextDecorations = TextDecorations.Underline;
            receiverSigPara.Inlines.Add(receivedPositionRun);
            
            receiverSigPara.Inlines.Add(new Run("               "));
            
            Run receiverSignatureRun = new Run("Подпись");
            receiverSignatureRun.TextDecorations = TextDecorations.Underline;
            receiverSigPara.Inlines.Add(receiverSignatureRun);
            
            receiverSigPara.Inlines.Add(new Run("               "));
            
            Run receiverNameRun = new Run(receiverName);
            if (!receiverName.Contains("_"))
            {
                receiverNameRun.TextDecorations = TextDecorations.Underline;
            }
            receiverSigPara.Inlines.Add(receiverNameRun);
            
            document.Blocks.Add(receiverSigPara);
            
            return document;
        }
        
        private void PrintDocument(FlowDocument document)
        {
            try
            {
                // Создаем временный файл XPS
                string tempFile = Path.GetTempFileName() + ".xps";
                
                // Настраиваем макет страницы документа
                document.PagePadding = new Thickness(40);
                document.ColumnWidth = double.PositiveInfinity;
                document.PageWidth = 210 * 96 / 25.4;  // 210мм в пикселях (A4)
                document.PageHeight = 297 * 96 / 25.4; // 297мм в пикселях (A4)
                
                // Создаем XPS документ
                XpsDocument xpsDoc = new XpsDocument(tempFile, FileAccess.ReadWrite);
                XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(xpsDoc);
                writer.Write(((IDocumentPaginatorSource)document).DocumentPaginator);
                
                // Показываем диалог печати
                PrintDialog printDialog = new PrintDialog();
                
                // Настраиваем параметры печати по умолчанию
                printDialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Portrait;
                printDialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);
                
                if (printDialog.ShowDialog() == true)
                {
                    DocumentPaginator paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
                    printDialog.PrintDocument(paginator, "Накладная на перемещение");
                }
                
                // Закрываем XPS документ
                xpsDoc.Close();
                
                // Удаляем временный файл
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при печати: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Преобразование числа в текст прописью
        private string NumberToText(decimal number)
        {
            string[] units = { "", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" };
            string[] teens = { "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать" };
            string[] tens = { "", "", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто" };
            string[] hundreds = { "", "сто", "двести", "триста", "четыреста", "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот" };
            string[] thousands = { "", "тысяча", "тысячи", "тысяч" };
            
            string result = "";
            
            int intNumber = (int)number;
            
            if (intNumber == 0)
                return "ноль";
                
            // Обработка тысяч
            int thousands_part = intNumber / 1000;
            if (thousands_part > 0)
            {
                if (thousands_part == 1)
                    result += "одна тысяча ";
                else if (thousands_part == 2)
                    result += "две тысячи ";
                else if (thousands_part >= 3 && thousands_part <= 4)
                    result += NumberToText(thousands_part) + " тысячи ";
                else
                    result += NumberToText(thousands_part) + " тысяч ";
                
                intNumber %= 1000;
            }
            
            // Обработка сотен
            int hundreds_part = intNumber / 100;
            if (hundreds_part > 0)
            {
                result += hundreds[hundreds_part] + " ";
                intNumber %= 100;
            }
            
            // Обработка десятков и единиц
            if (intNumber > 0)
            {
                if (intNumber < 10)
                    result += units[intNumber] + " ";
                else if (intNumber < 20)
                    result += teens[intNumber - 10] + " ";
                else
                {
                    result += tens[intNumber / 10] + " ";
                    int unit = intNumber % 10;
                    if (unit > 0)
                        result += units[unit] + " ";
                }
            }
            
            return result.Trim();
        }
    }
} 