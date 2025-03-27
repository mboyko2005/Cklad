using System;
using System.IO;
using System.Windows.Media.Imaging;

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