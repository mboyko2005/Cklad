using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace УправлениеСкладом.Class
{
    public class ClipboardManager
    {
        private MessageManager messageManager;
        private TextBox messageTextBox;
        private int selectedContactId;
        
        public ClipboardManager(MessageManager messageManager, TextBox messageTextBox)
        {
            this.messageManager = messageManager;
            this.messageTextBox = messageTextBox;
        }
        
        public void SetSelectedContactId(int contactId)
        {
            this.selectedContactId = contactId;
        }
        
        // Обработчик вставки содержимого из буфера обмена
        public void HandlePaste()
        {
            if (Clipboard.ContainsImage())
            {
                HandleImagePaste();
            }
            else if (Clipboard.ContainsText())
            {
                HandleTextPaste();
            }
        }
        
        // Обработка вставки изображения из буфера обмена
        private void HandleImagePaste()
        {
            // Получаем изображение из буфера обмена
            BitmapSource bitmapSource = Clipboard.GetImage();
            
            if (bitmapSource != null)
            {
                // Если выбран контакт для отправки
                if (selectedContactId <= 0)
                {
                    MessageBox.Show("Пожалуйста, выберите контакт для отправки изображения.");
                    return;
                }
                
                try
                {
                    // Преобразуем BitmapSource в массив байтов
                    byte[] imageBytes = ConvertBitmapSourceToBytes(bitmapSource);
                    
                    // Создаем объект вложения
                    MessageAttachment attachment = new MessageAttachment
                    {
                        Type = УправлениеСкладом.Class.AttachmentType.Image,
                        FileName = $"clip_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.png",
                        FileSize = imageBytes.Length,
                        Content = imageBytes,
                        Thumbnail = CreateThumbnail(bitmapSource, 100, 100)
                    };
                    
                    // Отправляем сообщение с вложением
                    SendMessageWithAttachment(attachment);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при вставке изображения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        // Обработка вставки текста из буфера обмена
        private void HandleTextPaste()
        {
            // Если в буфере текст, вставляем его в поле ввода
            string clipboardText = Clipboard.GetText();
            int caretIndex = messageTextBox.CaretIndex;
            string text = messageTextBox.Text;
            
            messageTextBox.Text = text.Insert(caretIndex, clipboardText);
            messageTextBox.CaretIndex = caretIndex + clipboardText.Length;
        }
        
        // Преобразование BitmapSource в массив байтов
        private byte[] ConvertBitmapSourceToBytes(BitmapSource bitmapSource)
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = 80; // Качество сжатия
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray();
            }
        }
        
        // Создание миниатюры для изображения
        private byte[] CreateThumbnail(BitmapSource source, int maxWidth, int maxHeight)
        {
            // Вычисляем новые размеры с сохранением соотношения сторон
            double scale = Math.Min((double)maxWidth / source.PixelWidth, (double)maxHeight / source.PixelHeight);
            int newWidth = (int)(source.PixelWidth * scale);
            int newHeight = (int)(source.PixelHeight * scale);
            
            // Создаем миниатюру
            TransformedBitmap thumbnail = new TransformedBitmap(source, new ScaleTransform(scale, scale));
            
            // Сохраняем в формате JPEG
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = 70; // Качество миниатюры
            encoder.Frames.Add(BitmapFrame.Create(thumbnail));
            
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray();
            }
        }
        
        // Отправка сообщения с вложением
        private async void SendMessageWithAttachment(MessageAttachment attachment)
        {
            try
            {
                // Получаем текст сообщения (если есть)
                string messageText = messageTextBox.Text.Trim();
                
                // Отправляем сообщение с вложением
                int messageId = await messageManager.SendMessageWithAttachmentAsync(selectedContactId, messageText, attachment);
                
                if (messageId > 0)
                {
                    // Очищаем поле ввода
                    messageTextBox.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке сообщения с вложением: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 