using MahApps.Metro.IconPacks;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace УправлениеСкладом.Class
{
    /// <summary>
    /// Класс для управления отображением превью вложений в мессенджере
    /// </summary>
    public class AttachmentPreviewManager
    {
        private readonly ContentPresenter previewContent;
        private readonly Grid previewPanel;
        private MessageAttachment currentAttachment;

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="previewContent">ContentPresenter для отображения превью</param>
        /// <param name="previewPanel">Панель, содержащая превью</param>
        public AttachmentPreviewManager(ContentPresenter previewContent, Grid previewPanel)
        {
            this.previewContent = previewContent;
            this.previewPanel = previewPanel;
        }

        /// <summary>
        /// Показать превью вложения
        /// </summary>
        /// <param name="attachment">Вложение для отображения</param>
        public void ShowPreview(MessageAttachment attachment)
        {
            if (attachment == null) return;
            
            currentAttachment = attachment;

            // Создаем превью в компактном стиле ChatGPT
            if (previewContent != null)
            {
                StackPanel previewPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    MaxWidth = 280
                };

                if (attachment.Type == AttachmentType.Image)
                {
                    // Компактное отображение изображения в стиле ChatGPT
                    Border imageBorder = new Border
                    {
                        CornerRadius = new CornerRadius(6),
                        ClipToBounds = true,
                        MaxWidth = 220,
                        MaxHeight = 120, // Более компактная высота
                        Margin = new Thickness(0, 0, 0, 4),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };

                    Image previewImage = new Image
                    {
                        Stretch = Stretch.UniformToFill,
                        MaxWidth = 220,
                        MaxHeight = 120
                    };

                    try
                    {
                        // Загружаем изображение из вложения
                        BitmapImage bitmapImage = new BitmapImage();
                        using (var ms = new MemoryStream(attachment.Thumbnail ?? attachment.Content))
                        {
                            ms.Position = 0;
                            bitmapImage.BeginInit();
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.StreamSource = ms;
                            bitmapImage.EndInit();
                            bitmapImage.Freeze(); // Оптимизация
                        }
                        
                        previewImage.Source = bitmapImage;
                        imageBorder.Child = previewImage;
                        previewPanel.Children.Add(imageBorder);
                        
                        // Добавляем имя файла под изображением в компактном виде
                        TextBlock fileNameText = new TextBlock
                        {
                            Text = attachment.FileName,
                            FontSize = 12,
                            Foreground = new SolidColorBrush(Colors.Gray),
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            Margin = new Thickness(4, 0, 0, 0),
                            MaxWidth = 220
                        };
                        previewPanel.Children.Add(fileNameText);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка при создании превью: {ex.Message}");
                        // В случае ошибки, показываем текстовую информацию
                        AddCompactFileInfoToPreviewPanel(previewPanel);
                    }
                }
                else
                {
                    // Для других типов файлов отображаем информацию о файле в компактном виде
                    AddCompactFileInfoToPreviewPanel(previewPanel);
                }

                previewContent.Content = previewPanel;
            }

            // Показываем панель превью
            this.previewPanel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Скрыть превью вложения
        /// </summary>
        public void HidePreview()
        {
            currentAttachment = null;
            
            if (previewContent != null)
                previewContent.Content = null;
                
            if (previewPanel != null)
                previewPanel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Получить текущее вложение
        /// </summary>
        public MessageAttachment GetCurrentAttachment()
        {
            return currentAttachment;
        }

        /// <summary>
        /// Добавить информацию о файле в панель превью в компактном стиле ChatGPT
        /// </summary>
        private void AddCompactFileInfoToPreviewPanel(StackPanel previewPanel)
        {
            if (currentAttachment == null) return;
            
            // Создаем компактный контейнер для информации о файле
            StackPanel fileInfoPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(4, 4, 4, 4)
            };

            // Создаем круглую подложку для иконки меньшего размера
            Border iconBackground = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(14),
                Background = GetFileIconBackground(currentAttachment.Type),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Иконка файла
            var fileIcon = new PackIconMaterial
            {
                Kind = GetFileIcon(currentAttachment.Type),
                Width = 16,
                Height = 16,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            iconBackground.Child = fileIcon;
            fileInfoPanel.Children.Add(iconBackground);

            // Компактная информация о файле
            var fileInfo = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 220
            };

            var fileName = new TextBlock
            {
                Text = currentAttachment.FileName,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 2)
            };
            fileInfo.Children.Add(fileName);

            var fileSize = new TextBlock
            {
                Text = FormatFileSize(currentAttachment.FileSize),
                FontSize = 11,
                Foreground = new SolidColorBrush(Colors.Gray)
            };
            fileInfo.Children.Add(fileSize);

            fileInfoPanel.Children.Add(fileInfo);
            previewPanel.Children.Add(fileInfoPanel);
        }

        /// <summary>
        /// Получить цвет фона для иконки типа файла
        /// </summary>
        private SolidColorBrush GetFileIconBackground(AttachmentType type)
        {
            switch (type)
            {
                case AttachmentType.Image:
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Зеленый
                case AttachmentType.Video:
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Красный
                case AttachmentType.Audio:
                    return new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Синий
                case AttachmentType.Document:
                    return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Оранжевый
                default:
                    return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Серый
            }
        }

        /// <summary>
        /// Получить иконку для типа файла
        /// </summary>
        private PackIconMaterialKind GetFileIcon(AttachmentType type)
        {
            switch (type)
            {
                case AttachmentType.Image:
                    return PackIconMaterialKind.FileImage;
                case AttachmentType.Video:
                    return PackIconMaterialKind.FileVideo;
                case AttachmentType.Audio:
                    return PackIconMaterialKind.FileMusic;
                case AttachmentType.Document:
                    return PackIconMaterialKind.FileDocument;
                default:
                    return PackIconMaterialKind.File;
            }
        }

        /// <summary>
        /// Форматировать размер файла для отображения
        /// </summary>
        private string FormatFileSize(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0)
                return "0" + suf[0];
            
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + " " + suf[place];
        }
    }
} 