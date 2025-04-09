/**
 * MessengerAttachment - класс для работы с вложениями в сообщениях
 */
class MessengerAttachment {
    constructor(options = {}) {
        // Элементы интерфейса
        this.attachmentButton = document.getElementById(options.attachmentButtonId || 'attachment-button');
        this.fileInput = document.getElementById(options.fileInputId || 'file-input');
        this.previewContainer = document.getElementById(options.previewContainerId || 'attachment-preview');
        this.messageTextArea = document.getElementById(options.messageTextAreaId || 'messageTextArea');
        this.messageInputContainer = document.querySelector('.message-input-container');
        
        // Максимальный размер файла (1.5GB)
        this.maxFileSize = options.maxFileSize || 1536 * 1024 * 1024;
        
        // Текущее вложение
        this.currentAttachment = null;
        
        // Инициализация обработчиков событий
        this._initEventListeners();
    }
    
    /**
     * Инициализация обработчиков событий
     * @private
     */
    _initEventListeners() {
        // Открытие диалога выбора файла
        if (this.attachmentButton) {
            this.attachmentButton.addEventListener('click', () => {
                this.fileInput.click();
            });
        }
        
        // Выбор файла
        if (this.fileInput) {
            this.fileInput.addEventListener('change', (event) => {
                const file = event.target.files[0];
                if (file) {
                    this._handleFileSelection(file);
                }
                
                // Важно: сбрасываем значение input, чтобы событие change сработало 
                // даже при повторном выборе того же файла
                this.fileInput.value = '';
            });
        }
    }
    
    /**
     * Обработка выбора файла
     * @param {File} file - выбранный файл
     * @private
     */
    _handleFileSelection(file) {
        // Проверка размера файла
        if (file.size > this.maxFileSize) {
            const maxSizeMB = this.maxFileSize / (1024 * 1024);
            MessengerUI.showNotification(`Файл слишком большой. Максимальный размер: ${maxSizeMB} МБ`, 'error');
            return;
        }
        
        // Сохраняем файл
        this.currentAttachment = file;
        
        // Показываем предпросмотр
        this._showPreview(file);
        
        // Подсвечиваем кнопку прикрепления, чтобы показать активное вложение
        if (this.attachmentButton) {
            this.attachmentButton.classList.add('attachment-button-active');
        }
        
        // Добавляем класс к контейнеру ввода для стилизации
        if (this.messageInputContainer) {
            this.messageInputContainer.classList.add('has-attachment');
        }
        
        // Изменяем placeholder в текстовом поле
        if (this.messageTextArea) {
            const originalPlaceholder = this.messageTextArea.getAttribute('data-original-placeholder') || 
                                       this.messageTextArea.placeholder;
            
            // Сохраняем оригинальный placeholder, если он еще не сохранен
            if (!this.messageTextArea.getAttribute('data-original-placeholder')) {
                this.messageTextArea.setAttribute('data-original-placeholder', originalPlaceholder);
            }
            
            // Устанавливаем новый placeholder в зависимости от типа файла
            const fileType = file.type.split('/')[0];
            if (fileType === 'image') {
                this.messageTextArea.placeholder = 'Добавьте подпись к изображению...';
            } else {
                this.messageTextArea.placeholder = 'Добавьте комментарий к файлу...';
            }
            
            // Фокусируемся на текстовом поле для удобства пользователя
            this.messageTextArea.focus();
        }
    }
    
    /**
     * Показать предпросмотр файла
     * @param {File} file - файл для предпросмотра
     * @private
     */
    _showPreview(file) {
        if (!this.previewContainer) return;
        
        // Очищаем предыдущий предпросмотр
        this.previewContainer.innerHTML = '';
        this.previewContainer.classList.add('active');
        
        // Сохраняем ссылку на файл для статического метода
        this.previewContainer.file = file;
        
        const previewWrapper = document.createElement('div');
        previewWrapper.className = 'attachment-preview-wrapper';
        
        // Определяем тип файла
        const fileType = file.type.split('/')[0];
        const fileExtension = file.name.split('.').pop().toLowerCase();
        
        // Создаем элемент предпросмотра в зависимости от типа файла
        if (fileType === 'image') {
            // Создаем контейнер для изображения
            const imgContainer = document.createElement('div');
            imgContainer.className = 'attachment-image-container';
            
            // Создаем изображение и добавляем анимацию загрузки
            const img = document.createElement('img');
            img.className = 'attachment-preview-image';
            
            // Устанавливаем обработчики для изображения
            img.onload = () => {
                // Добавляем класс для анимации появления
                img.classList.add('loaded');
            };
            
            // Устанавливаем источник изображения
            img.src = URL.createObjectURL(file);
            imgContainer.appendChild(img);
            
            // Добавляем информацию о файле
            const fileInfo = document.createElement('div');
            fileInfo.className = 'attachment-file-info';
            
            // Добавляем имя файла и его размер
            const formattedSize = this._formatFileSize(file.size);
            fileInfo.innerHTML = `
                <div class="attachment-file-name">${file.name}</div>
                <div class="attachment-file-size">${formattedSize}</div>
            `;
            
            previewWrapper.appendChild(imgContainer);
            previewWrapper.appendChild(fileInfo);
        } else {
            // Для других типов файлов создаем иконку в зависимости от типа
            const fileIcon = document.createElement('div');
            fileIcon.className = 'attachment-file-icon';
            
            // Выбираем иконку в зависимости от типа файла
            let iconClass = 'ri-file-line';
            
            // Определяем иконку по расширению или MIME-типу
            if (fileType === 'video') {
                iconClass = 'ri-video-line';
            } else if (fileType === 'audio') {
                iconClass = 'ri-music-line';
            } else if (fileType === 'application') {
                if (['pdf'].includes(fileExtension)) {
                    iconClass = 'ri-file-pdf-line';
                } else if (['doc', 'docx'].includes(fileExtension)) {
                    iconClass = 'ri-file-word-line';
                } else if (['xls', 'xlsx'].includes(fileExtension)) {
                    iconClass = 'ri-file-excel-line';
                } else if (['ppt', 'pptx'].includes(fileExtension)) {
                    iconClass = 'ri-file-ppt-line';
                } else if (['zip', 'rar', '7z', 'tar', 'gz'].includes(fileExtension)) {
                    iconClass = 'ri-file-zip-line';
                } else if (['exe', 'msi', 'bat'].includes(fileExtension)) {
                    iconClass = 'ri-software-line';
                }
            } else if (fileType === 'text') {
                iconClass = 'ri-file-text-line';
            }
            
            fileIcon.innerHTML = `<i class="${iconClass}"></i>`;
            
            // Создаем контейнер для информации о файле
            const fileInfo = document.createElement('div');
            fileInfo.className = 'attachment-file-info';
            
            // Форматируем размер файла
            const formattedSize = this._formatFileSize(file.size);
            
            // Добавляем имя файла и его размер
            fileInfo.innerHTML = `
                <div class="attachment-file-name">${file.name}</div>
                <div class="attachment-file-size">${formattedSize}</div>
            `;
            
            previewWrapper.appendChild(fileIcon);
            previewWrapper.appendChild(fileInfo);
        }
        
        // Добавляем кнопку удаления
        const removeButton = document.createElement('button');
        removeButton.className = 'remove-attachment-btn';
        removeButton.setAttribute('title', 'Удалить вложение');
        removeButton.innerHTML = '<i class="ri-close-line"></i>';
        removeButton.addEventListener('click', (e) => {
            e.stopPropagation();
            this.clearAttachments();
        });
        
        previewWrapper.appendChild(removeButton);
        this.previewContainer.appendChild(previewWrapper);
    }
    
    /**
     * Форматирует размер файла для отображения
     * @param {number} bytes - размер в байтах
     * @returns {string} форматированный размер
     * @private
     */
    _formatFileSize(bytes) {
        if (bytes === 0) return '0 Б';
        
        const sizes = ['Б', 'КБ', 'МБ', 'ГБ', 'ТБ'];
        const i = Math.floor(Math.log(bytes) / Math.log(1024));
        
        return parseFloat((bytes / Math.pow(1024, i)).toFixed(2)) + ' ' + sizes[i];
    }
    
    /**
     * Очистить все вложения
     */
    clearAttachments() {
        this.currentAttachment = null;
        if (this.fileInput) this.fileInput.value = '';
        if (this.previewContainer) {
            this.previewContainer.innerHTML = '';
            this.previewContainer.classList.remove('active');
            this.previewContainer.file = null;
        }
        
        // Убираем подсветку кнопки прикрепления
        if (this.attachmentButton) {
            this.attachmentButton.classList.remove('attachment-button-active');
        }
        
        // Убираем класс у контейнера ввода
        if (this.messageInputContainer) {
            this.messageInputContainer.classList.remove('has-attachment');
        }
        
        // Восстанавливаем исходный placeholder
        if (this.messageTextArea) {
            const originalPlaceholder = this.messageTextArea.getAttribute('data-original-placeholder');
            if (originalPlaceholder) {
                this.messageTextArea.placeholder = originalPlaceholder;
            }
        }
    }
    
    /**
     * Проверить, есть ли текущее вложение
     * @returns {boolean} true, если есть вложение
     */
    hasAttachment() {
        return !!this.currentAttachment;
    }
    
    /**
     * Получить текущее вложение
     * @returns {File|null} файл вложения или null
     */
    getAttachment() {
        return this.currentAttachment;
    }
    
    /**
     * Статические методы для работы с вложениями без создания экземпляра класса
     */
    
    /**
     * Проверить, есть ли вложение в контейнере предпросмотра
     * @returns {boolean} true, если есть вложение
     */
    static hasAttachment() {
        const previewContainer = document.getElementById('attachment-preview');
        return previewContainer && previewContainer.classList.contains('active');
    }
    
    /**
     * Получить вложение из контейнера предпросмотра
     * @returns {File|null} файл вложения или null
     */
    static getAttachment() {
        const previewContainer = document.getElementById('attachment-preview');
        return previewContainer && previewContainer.file;
    }
    
    /**
     * Очистить все вложения
     */
    static clearAttachments() {
        const previewContainer = document.getElementById('attachment-preview');
        const fileInput = document.getElementById('file-input');
        const messageTextArea = document.getElementById('messageTextArea');
        const messageInputContainer = document.querySelector('.message-input-container');
        const attachmentButton = document.getElementById('attachment-button');
        
        if (previewContainer) {
            previewContainer.innerHTML = '';
            previewContainer.classList.remove('active');
            previewContainer.file = null;
        }
        
        if (fileInput) {
            fileInput.value = '';
        }
        
        // Убираем подсветку кнопки прикрепления
        if (attachmentButton) {
            attachmentButton.classList.remove('attachment-button-active');
        }
        
        // Убираем класс у контейнера ввода
        if (messageInputContainer) {
            messageInputContainer.classList.remove('has-attachment');
        }
        
        // Восстанавливаем исходный placeholder
        if (messageTextArea) {
            const originalPlaceholder = messageTextArea.getAttribute('data-original-placeholder');
            if (originalPlaceholder) {
                messageTextArea.placeholder = originalPlaceholder;
            }
        }
    }
} 