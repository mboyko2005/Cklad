class MessengerUI {
    static async loadMessageAttachment(messageId, messageElement) {
        try {
            const attachmentContainer = messageElement.querySelector('.message-content-attachment');
            if (!attachmentContainer) return;

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
                        <a href="/api/message/media/${messageId}" target="_blank" download="${attachment.name}">
                            ${attachment.name}
                        </a>
                        <span class="file-size">${this.formatFileSize(attachment.size)}</span>
                    </div>
                `;
            }

            attachmentContainer.innerHTML = attachmentElement;
        } catch (error) {
            console.error('Ошибка при загрузке вложения:', error);
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
        messageWrapper.dataset.messageId = message.messageId;

        const timestamp = new Date(message.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

        messageWrapper.innerHTML = `
            <div class="message">
                <div class="message-content">
                    ${message.text ? `<div class="message-text">${message.text}</div>` : ''}
                    ${message.hasAttachment ? '<div class="message-content-attachment"></div>' : ''}
                    <div class="message-meta">
                        <span class="message-time">${timestamp}</span>
                        ${isSender ? `<span class="message-status ${message.isRead ? 'read' : 'sent'}"></span>` : ''}
                    </div>
                </div>
            </div>
        `;

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
        gap: 8px;
        padding: 8px;
        background: rgba(0, 0, 0, 0.05);
        border-radius: 4px;
    }

    .file-attachment i {
        font-size: 24px;
        color: #3498db;
    }

    .file-attachment .file-size {
        color: #666;
        font-size: 12px;
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