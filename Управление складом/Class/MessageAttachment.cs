using System;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.IconPacks;

namespace УправлениеСкладом.Class
{
    /// <summary>
    /// Класс для работы с вложениями в сообщениях
    /// </summary>
    public class MessageAttachment
    {
        /// <summary>
        /// Идентификатор вложения
        /// </summary>
        public int AttachmentId { get; set; }

        /// <summary>
        /// Идентификатор сообщения, к которому относится вложение
        /// </summary>
        public int MessageId { get; set; }

        /// <summary>
        /// Тип вложения (изображение, документ, аудио и т.д.)
        /// </summary>
        public AttachmentType Type { get; set; }

        /// <summary>
        /// Имя файла
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Размер файла
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Содержимое файла в бинарном формате
        /// </summary>
        public byte[] Content { get; set; }

        /// <summary>
        /// Миниатюра для изображений
        /// </summary>
        public byte[] Thumbnail { get; set; }

        /// <summary>
        /// Тип файла
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// Видимость предварительного просмотра
        /// </summary>
        public bool IsPreviewVisible { get; set; }

        /// <summary>
        /// Элемент предварительного просмотра
        /// </summary>
        public UIElement PreviewElement { get; set; }

        /// <summary>
        /// Получить размер файла в удобочитаемом формате (КБ, МБ и т.д.)
        /// </summary>
        public string FormattedFileSize 
        { 
            get
            {
                string[] sizes = { "Б", "КБ", "МБ", "ГБ" };
                double len = FileSize;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                return String.Format("{0:0.#} {1}", len, sizes[order]);
            }
        }

        /// <summary>
        /// Создать миниатюру из изображения
        /// </summary>
        public void CreateThumbnail(int maxWidth = 100, int maxHeight = 100)
        {
            if (Type != AttachmentType.Image || Content == null)
                return;

            try
            {
                using (MemoryStream stream = new MemoryStream(Content))
                {
                    // Создаем изображение из потока
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();

                    // Вычисляем новые размеры, сохраняя пропорции
                    double scale = Math.Min(maxWidth / (double)image.PixelWidth, maxHeight / (double)image.PixelHeight);
                    int newWidth = (int)(image.PixelWidth * scale);
                    int newHeight = (int)(image.PixelHeight * scale);

                    // Создаем миниатюру
                    TransformedBitmap transformedBitmap = new TransformedBitmap(image, new System.Windows.Media.ScaleTransform(scale, scale));
                    
                    // Сохраняем миниатюру в поток
                    using (MemoryStream thumbnailStream = new MemoryStream())
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(transformedBitmap));
                        encoder.Save(thumbnailStream);
                        Thumbnail = thumbnailStream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка создания миниатюры: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить расширение файла
        /// </summary>
        public string GetFileExtension()
        {
            return Path.GetExtension(FileName).ToLower();
        }

        /// <summary>
        /// Получить BitmapImage для предварительного просмотра
        /// </summary>
        public BitmapImage GetImagePreview()
        {
            if (Type != AttachmentType.Image || Content == null)
                return null;

            try
            {
                BitmapImage image = new BitmapImage();
                using (MemoryStream ms = new MemoryStream(Content))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                }
                return image;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Получить миниатюру изображения
        /// </summary>
        public BitmapImage GetThumbnail()
        {
            if (Thumbnail == null)
                return null;

            try
            {
                BitmapImage image = new BitmapImage();
                using (MemoryStream ms = new MemoryStream(Thumbnail))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                }
                return image;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки миниатюры: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Сохранить файл на диск
        /// </summary>
        public bool SaveToFile(string path)
        {
            try
            {
                File.WriteAllBytes(path, Content);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения файла: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Создать элемент предварительного просмотра
        /// </summary>
        public UIElement CreatePreviewElement()
        {
            // Создаем основной контейнер для предварительного просмотра
            Border mainBorder = new Border
            {
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Margin = new Thickness(5),
                Padding = new Thickness(8),
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240))
            };

            StackPanel mainPanel = new StackPanel
            {
                Margin = new Thickness(0)
            };

            mainBorder.Child = mainPanel;

            // Заголовок с типом файла и размером
            StackPanel headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 5)
            };

            PackIconMaterial typeIcon = new PackIconMaterial
            {
                Kind = GetFileIcon(),
                Width = 16,
                Height = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                Margin = new Thickness(0, 0, 5, 0)
            };

            TextBlock typeText = new TextBlock
            {
                Text = GetAttachmentTypeText(),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
            };

            TextBlock sizeText = new TextBlock
            {
                Text = FormattedFileSize,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 130)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0)
            };

            headerPanel.Children.Add(typeIcon);
            headerPanel.Children.Add(typeText);
            headerPanel.Children.Add(sizeText);
            mainPanel.Children.Add(headerPanel);

            // Содержимое в зависимости от типа вложения
            switch (Type)
            {
                case AttachmentType.Image:
                    // Контейнер для изображения
                    Border imageBorder = new Border
                    {
                        CornerRadius = new CornerRadius(4),
                        ClipToBounds = true,
                        MaxHeight = 300,
                        MaxWidth = 300
                    };

                    Image image = new Image
                    {
                        Source = GetImagePreview(),
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    imageBorder.Child = image;
                    mainPanel.Children.Add(imageBorder);
                    break;

                default:
                    // Для других типов файлов показываем имя файла
                    Border fileInfoBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(8),
                        Margin = new Thickness(0, 5, 0, 0)
                    };

                    StackPanel fileInfoPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal
                    };

                    PackIconMaterial fileIcon = new PackIconMaterial
                    {
                        Kind = GetFileIcon(),
                        Width = 24,
                        Height = 24,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
                        Margin = new Thickness(0, 0, 10, 0)
                    };

                    TextBlock fileNameText = new TextBlock
                    {
                        Text = FileName,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                        MaxWidth = 250
                    };

                    fileInfoPanel.Children.Add(fileIcon);
                    fileInfoPanel.Children.Add(fileNameText);
                    fileInfoBorder.Child = fileInfoPanel;
                    mainPanel.Children.Add(fileInfoBorder);
                    break;
            }

            return mainBorder;
        }

        private PackIconMaterialKind GetFileIcon()
        {
            string extension = GetFileExtension();
            switch (extension)
            {
                case ".pdf":
                    return PackIconMaterialKind.FileDocumentOutline;
                case ".doc":
                case ".docx":
                    return PackIconMaterialKind.FileDocumentOutline;
                case ".xls":
                case ".xlsx":
                    return PackIconMaterialKind.FileExcelOutline;
                case ".zip":
                case ".rar":
                    return PackIconMaterialKind.FolderZipOutline;
                case ".mp3":
                case ".wav":
                    return PackIconMaterialKind.FileMusic;
                case ".mp4":
                case ".avi":
                    return PackIconMaterialKind.FileVideo;
                default:
                    return PackIconMaterialKind.File;
            }
        }

        // Добавим метод для получения текстового представления типа вложения
        private string GetAttachmentTypeText()
        {
            switch (Type)
            {
                case AttachmentType.Image:
                    return "Изображение";
                case AttachmentType.Document:
                    return "Документ";
                case AttachmentType.Audio:
                    return "Аудио";
                case AttachmentType.Video:
                    return "Видео";
                default:
                    return "Файл";
            }
        }
    }

    /// <summary>
    /// Перечисление типов вложений
    /// </summary>
    public enum AttachmentType
    {
        Image,
        Document,
        Audio,
        Video,
        Other
    }
} 