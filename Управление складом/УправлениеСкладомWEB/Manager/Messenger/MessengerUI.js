class MessengerUI {
    static async loadMessageAttachment(messageId, messageElement) {
        try {
            if (!messageId || !messageElement) {
                console.error('Отсутствуют необходимые параметры: messageId или messageElement');
                return;
            }
            
            // Проверяем, нет ли уже контейнера вложения в сообщении с загруженным содержимым
            const existingAttachment = messageElement.querySelector('.message-image, .file-attachment');
            if (existingAttachment) {
                console.log('Вложение уже загружено для сообщения:', messageId);
                return;
            }
            
            // Получаем или создаем контейнер вложения
            let attachmentContainer = messageElement.querySelector('.message-content-attachment');
            
            if (!attachmentContainer) {
                // Создаем контейнер, если его еще нет
                const messageContent = messageElement.querySelector('.message-content');
                if (!messageContent) {
                    console.error('Не найден контейнер message-content для message ID:', messageId);
                    return;
                }
                
                attachmentContainer = document.createElement('div');
                attachmentContainer.className = 'message-content-attachment';
                
                // Вставляем контейнер в начало содержимого сообщения
                if (messageContent.firstChild) {
                    messageContent.insertBefore(attachmentContainer, messageContent.firstChild);
                } else {
                    messageContent.appendChild(attachmentContainer);
                }
            }

            // Показываем индикатор загрузки
            attachmentContainer.innerHTML = `
                <div class="attachment-loading">
                    <div class="spinner"></div>
                    <span>Загрузка вложения...</span>
                </div>
            `;

            // Получаем информацию о вложении
            const response = await fetch(`/api/message/attachment/${messageId}`);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const attachment = await response.json();
            if (!attachment.success) {
                throw new Error(attachment.message || 'Ошибка получения информации о вложении');
            }

            // Создаем элемент вложения в зависимости от типа
            let attachmentElement = '';
            if (attachment.type.startsWith('image/')) {
                attachmentElement = `
                    <div class="image-attachment">
                        <img src="/api/message/media/${messageId}" alt="Изображение" 
                             onclick="MessengerUI.showImagePreview(this.src)">
                    </div>
                `;
            } else {
                attachmentElement = `
                    <div class="file-attachment">
                        <i class="ri-file-line"></i>
                        <div class="file-details">
                            <a href="/api/message/media/${messageId}" target="_blank" download="${attachment.name}" style="color: white; text-decoration: none;">
                                <span class="file-name">${attachment.name}</span>
                                <i class="ri-download-line"></i>
                            </a>
                            <span class="file-size">${this.formatFileSize(attachment.size)}</span>
                        </div>
                    </div>
                `;
            }

            attachmentContainer.innerHTML = attachmentElement;
        } catch (error) {
            console.error('Ошибка при загрузке вложения:', error);
            const attachmentContainer = messageElement.querySelector('.message-content-attachment');
            if (attachmentContainer) {
                attachmentContainer.innerHTML = `
                    <div class="attachment-error">
                        <i class="ri-error-warning-line"></i>
                        Ошибка загрузки вложения
                    </div>
                `;
            }
        }
    }

    static showImagePreview(src) {
        const modal = document.createElement('div');
        modal.className = 'image-preview-modal';
        modal.innerHTML = `
            <div class="modal-content">
                <span class="close-button">&times;</span>
                <img src="${src}" alt="Предпросмотр">
            </div>
        `;

        modal.querySelector('.close-button').onclick = () => {
            document.body.removeChild(modal);
        };

        modal.onclick = (e) => {
            if (e.target === modal) {
                document.body.removeChild(modal);
            }
        };

        document.body.appendChild(modal);
    }

    static formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    static createMessageElement(message, isSender) {
        const messageWrapper = document.createElement('div');
        messageWrapper.className = `message-wrapper ${isSender ? 'sent' : 'received'}`;
        messageWrapper.dataset.id = message.messageId;

        // Create message bubble
        const messageBubble = document.createElement('div');
        messageBubble.className = 'message-bubble';
        
        // Create message content container
        const messageContent = document.createElement('div');
        messageContent.className = 'message-content';
        
        // Проверяем наличие вложения
        if (message.attachment) {
            // Создаем контейнер для вложения
            const attachmentContainer = document.createElement('div');
            attachmentContainer.className = 'message-content-attachment';
            
            // Определяем, какой тип вложения
            if (message.attachment.type.startsWith('image/')) {
                // Отображаем изображение
                attachmentContainer.innerHTML = `
                    <div class="image-attachment">
                        <img src="${message.attachment.url}" alt="Изображение" 
                             onclick="MessengerUI.showImagePreview(this.src)">
                    </div>
                `;
            } else {
                // Отображаем файл
                attachmentContainer.innerHTML = `
                    <div class="file-attachment">
                        <i class="ri-file-line"></i>
                        <div class="file-details">
                            <span class="file-name">${message.attachment.name}</span>
                            <span class="file-size">${this.formatFileSize(message.attachment.size)}</span>
                        </div>
                    </div>
                `;
            }
            messageContent.appendChild(attachmentContainer);
        } else if (message.hasAttachment) {
            // Если есть флаг вложения, но нет локального объекта, создаем пустой контейнер
            // который будет заполнен позже через loadMessageAttachment
            const attachmentContainer = document.createElement('div');
            attachmentContainer.className = 'message-content-attachment';
            messageContent.appendChild(attachmentContainer);
        }
        
        // Добавляем текст сообщения
        if (message.text) {
            const textElement = document.createElement('div');
            textElement.className = 'message-text';
            textElement.textContent = message.text;
            messageContent.appendChild(textElement);
        }
        
        // Добавляем метаданные (время и статус)
        const metaElement = document.createElement('div');
        metaElement.className = 'message-meta';
        
        const timestamp = new Date(message.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        
        metaElement.innerHTML = `
            <span class="message-time">${timestamp}</span>
            ${isSender ? `<span class="message-status ${message.isRead ? 'read' : 'sent'}"></span>` : ''}
        `;
        
        messageContent.appendChild(metaElement);
        messageBubble.appendChild(messageContent);
        messageWrapper.appendChild(messageBubble);

        return messageWrapper;
    }
}

// Добавляем стили для новых элементов
const style = document.createElement('style');
style.textContent = `
    .image-preview-modal {
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background-color: rgba(0, 0, 0, 0.8);
        display: flex;
        justify-content: center;
        align-items: center;
        z-index: 1000;
    }

    .image-preview-modal .modal-content {
        position: relative;
        max-width: 90%;
        max-height: 90%;
    }

    .image-preview-modal img {
        max-width: 100%;
        max-height: 90vh;
        object-fit: contain;
    }

    .image-preview-modal .close-button {
        position: absolute;
        top: -30px;
        right: -30px;
        color: white;
        font-size: 24px;
        cursor: pointer;
    }

    .attachment-loading {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 8px;
        background: rgba(0, 0, 0, 0.05);
        border-radius: 4px;
    }

    .attachment-loading .spinner {
        width: 16px;
        height: 16px;
        border: 2px solid #f3f3f3;
        border-top: 2px solid #3498db;
        border-radius: 50%;
        animation: spin 1s linear infinite;
    }

    @keyframes spin {
        0% { transform: rotate(0deg); }
        100% { transform: rotate(360deg); }
    }

    .attachment-error {
        color: #e74c3c;
        padding: 8px;
        background: rgba(231, 76, 60, 0.1);
        border-radius: 4px;
        display: flex;
        align-items: center;
        gap: 8px;
    }

    .file-attachment {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 10px;
        background: var(--message-sent-bg, #007aff);
        border-radius: 4px;
        width: 100%;
    }

    .file-attachment i {
        font-size: 24px;
        color: white;
        flex-shrink: 0;
    }
    
    .file-details {
        flex-grow: 1;
        overflow: hidden;
    }

    .file-attachment a {
        color: white;
        text-decoration: none;
        font-weight: 500;
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 4px;
    }

    .file-name {
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
        max-width: 180px;
        display: inline-block;
    }

    .file-attachment a i.ri-download-line {
        font-size: 16px;
        margin-left: 8px;
    }

    .file-attachment .file-size {
        color: rgba(255, 255, 255, 0.8);
        font-size: 11px;
        display: block;
    }

    .image-attachment img {
        max-width: 200px;
        max-height: 200px;
        border-radius: 4px;
        cursor: pointer;
        transition: transform 0.2s;
    }

    .image-attachment img:hover {
        transform: scale(1.05);
    }
`;

document.head.appendChild(style); 