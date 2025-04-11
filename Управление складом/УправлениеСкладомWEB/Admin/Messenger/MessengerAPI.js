/**
 * MessengerAPI.js - Вспомогательные классы для работы с API мессенджера
 * Содержит классы для работы с пользователями, контактами и сообщениями
 */

// Базовый класс для API запросов
class BaseAPI {
    /**
     * Обрабатывает ошибки при выполнении API запросов
     * @param {Error} error - Объект ошибки
     * @param {string} message - Сообщение об ошибке
     * @returns {Promise<never>} - Промис с ошибкой
     */
    static handleError(error, message) {
        console.error(`${message}: `, error);
        return Promise.reject(new Error(message));
    }

    /**
     * Проверяет ответ от сервера
     * @param {Response} response - Ответ от сервера
     * @param {string} errorMessage - Сообщение об ошибке
     * @returns {Promise<any>} - Промис с данными
     */
    static async checkResponse(response, errorMessage) {
        if (!response.ok) {
            const errorText = await response.text();
            console.error(`${errorMessage}: ${response.status}`, errorText);
            throw new Error(`${errorMessage}: ${response.status}`);
        }

        return response.json();
    }
}

/**
 * Класс для работы с пользователями
 */
class UserAPI extends BaseAPI {
    /**
     * Получить ID текущего пользователя из localStorage
     * @returns {string} - ID текущего пользователя
     */
    static getCurrentUserId() {
        const userId = localStorage.getItem("userId");
        if (!userId) {
            console.error("ID пользователя не найден в localStorage");
        }
        return userId;
    }

    /**
     * Получить ID пользователя по логину
     * @param {string} login - Логин пользователя
     * @returns {Promise<number>} - ID пользователя
     */
    static async getUserIdByLogin(login) {
        try {
            if (!login) {
                throw new Error("Логин пользователя не указан");
            }

            const response = await fetch(`/api/user/getUserIdByLogin/${encodeURIComponent(login)}`);
            const data = await this.checkResponse(response, "Ошибка получения ID пользователя");
            
            if (!data.userId) {
                throw new Error("ID пользователя не найден в ответе API");
            }

            return data.userId;
        } catch (error) {
            return this.handleError(error, "Ошибка получения ID пользователя");
        }
    }

    /**
     * Получить информацию о пользователе
     * @param {number} userId - ID пользователя
     * @returns {Promise<Object>} - Информация о пользователе
     */
    static async getUserInfo(userId) {
        try {
            if (!userId) {
                throw new Error("ID пользователя не указан");
            }

            const response = await fetch(`/api/user/info/${userId}`);
            return this.checkResponse(response, "Ошибка получения информации о пользователе");
        } catch (error) {
            return this.handleError(error, "Ошибка получения информации о пользователе");
        }
    }

    /**
     * Поиск пользователей
     * @param {string} searchQuery - Поисковый запрос
     * @returns {Promise<Array>} - Список пользователей
     */
    static async searchUsers(searchQuery = "") {
        try {
            let url = "/api/user/list";
            if (searchQuery) {
                url += `?search=${encodeURIComponent(searchQuery)}`;
            }

            const response = await fetch(url);
            const data = await this.checkResponse(response, "Ошибка поиска пользователей");
            
            if (!data.success || !data.users) {
                throw new Error("Неверный формат ответа от сервера");
            }

            return data.users;
        } catch (error) {
            return this.handleError(error, "Ошибка поиска пользователей");
        }
    }
}

/**
 * Класс для работы с контактами
 */
class ContactAPI extends BaseAPI {
    /**
     * Получить список контактов пользователя
     * @param {number} userId - ID пользователя
     * @param {string} search - Поисковый запрос (опционально)
     * @returns {Promise<Array>} - Список контактов
     */
    static async getContacts(userId, search = "") {
        try {
            if (!userId) {
                throw new Error("ID пользователя не указан");
            }

            let url = `/api/contact/list?userId=${userId}`;
            if (search) {
                url += `&search=${encodeURIComponent(search)}`;
            }

            const response = await fetch(url);
            return this.checkResponse(response, "Ошибка загрузки контактов");
        } catch (error) {
            return this.handleError(error, "Ошибка при загрузке контактов");
        }
    }

    /**
     * Получить информацию о контакте
     * @param {number} userId - ID пользователя
     * @param {number} contactId - ID контакта
     * @returns {Promise<Object>} - Информация о контакте
     */
    static async getContactInfo(userId, contactId) {
        try {
            if (!userId || !contactId) {
                throw new Error("ID пользователя или контакта не указаны");
            }

            const response = await fetch(`/api/contact/info/${userId}/${contactId}`);
            return this.checkResponse(response, "Ошибка получения информации о контакте");
        } catch (error) {
            return this.handleError(error, "Ошибка получения информации о контакте");
        }
    }
}

/**
 * Класс для работы с сообщениями
 */
class MessageAPI extends BaseAPI {
    static updateInterval = null;
    static currentContactId = null;

    /**
     * Получить историю переписки
     * @param {number} userId - ID пользователя
     * @param {number} contactId - ID контакта
     * @returns {Promise<Object>} - История переписки
     */
    static async getConversation(userId, contactId) {
        try {
            if (!userId || !contactId) {
                throw new Error("ID пользователя или контакта не указаны");
            }

            const response = await fetch(`/api/message/conversation/${userId}/${contactId}`);
            const data = await this.checkResponse(response, "Ошибка загрузки сообщений");
            
            if (!data.success) {
                throw new Error("Неверный формат ответа от сервера");
            }

            return data;
        } catch (error) {
            return this.handleError(error, "Ошибка при загрузке сообщений");
        }
    }

    /**
     * Отправить сообщение
     * @param {number} senderId - ID отправителя
     * @param {number} receiverId - ID получателя
     * @param {string} text - Текст сообщения
     * @returns {Promise<Object>} - Результат отправки
     */
    static async sendMessage(senderId, receiverId, text) {
        try {
            if (!senderId || !receiverId || !text) {
                throw new Error("Не все параметры для отправки сообщения указаны");
            }

            const response = await fetch('/api/message/send', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    senderId: parseInt(senderId),
                    receiverId: parseInt(receiverId),
                    text: text
                })
            });

            return this.checkResponse(response, "Ошибка при отправке сообщения");
        } catch (error) {
            return this.handleError(error, "Ошибка при отправке сообщения");
        }
    }

    /**
     * Отметить сообщения как прочитанные
     * @param {number} userId - ID пользователя
     * @param {number} contactId - ID контакта
     * @returns {Promise<Object>} - Результат операции
     */
    static async markMessagesAsRead(userId, contactId) {
        try {
            if (!userId || !contactId) {
                throw new Error("ID пользователя или контакта не указаны");
            }

            const response = await fetch(`/api/message/read/${userId}/${contactId}`, {
                method: "POST"
            });

            return this.checkResponse(response, "Ошибка обновления статуса сообщений");
        } catch (error) {
            return this.handleError(error, "Ошибка при обновлении статуса сообщений");
        }
    }

    /**
     * Удалить сообщение
     * @param {number} messageId - ID сообщения
     * @param {number} userId - ID пользователя
     * @returns {Promise<Object>} - Результат операции
     */
    static async deleteMessage(messageId, userId) {
        try {
            if (!messageId || !userId) {
                throw new Error("ID сообщения или пользователя не указаны");
            }

            const response = await fetch(`/api/messenger/message/${messageId}?userId=${userId}`, {
                method: "DELETE"
            });

            return this.checkResponse(response, "Ошибка удаления сообщения");
        } catch (error) {
            return this.handleError(error, "Ошибка при удалении сообщения");
        }
    }
    
    /**
     * Удалить переписку
     * @param {number} userId - ID пользователя
     * @param {number} contactId - ID контакта
     * @returns {Promise<Object>} - Результат операции
     */
    static async deleteConversation(userId, contactId) {
        try {
            if (!userId || !contactId) {
                throw new Error("ID пользователя или контакта не указаны");
            }

            const response = await fetch(`/api/message/delete-conversation/${userId}/${contactId}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            return this.checkResponse(response, "Ошибка при удалении переписки");
        } catch (error) {
            return this.handleError(error, "Ошибка при удалении переписки");
        }
    }
    
    /**
     * Получить шаблоны сообщений
     * @param {number} userId - ID пользователя
     * @param {number} contactId - ID контакта
     * @returns {Promise<Array>} - Список шаблонов
     */
    static async getMessageTemplates(userId, contactId) {
        try {
            if (!userId || !contactId) {
                throw new Error("ID пользователя или контакта не указаны");
            }

            const response = await fetch(`/api/messenger/templates?userId=${userId}&contactId=${contactId}`);
            return this.checkResponse(response, "Ошибка загрузки шаблонов сообщений");
        } catch (error) {
            return this.handleError(error, "Ошибка при загрузке шаблонов сообщений");
        }
    }
    
    /**
     * Удалить сообщение для себя
     * @param {number} messageId - ID сообщения
     * @returns {Promise<Object>} - Результат операции
     */
    static async deleteMessageForMe(messageId) {
        try {
            if (!messageId) {
                throw new Error("ID сообщения не указан");
            }

            // Получаем ID пользователя из localStorage для заголовка
            const userId = localStorage.getItem("userId");
            if (!userId) {
                throw new Error("Ошибка авторизации. Пожалуйста, перезайдите в систему.");
            }

            const response = await fetch(`/api/message/delete-for-me/${messageId}`, {
                method: 'DELETE',
                headers: {
                    'UserId': userId
                }
            });

            return this.checkResponse(response, "Ошибка при удалении сообщения");
        } catch (error) {
            return this.handleError(error, "Ошибка при удалении сообщения");
        }
    }
    
    /**
     * Удалить сообщение для всех
     * @param {number} messageId - ID сообщения
     * @returns {Promise<Object>} - Результат операции
     */
    static async deleteMessageForAll(messageId) {
        try {
            if (!messageId) {
                throw new Error("ID сообщения не указан");
            }

            // Получаем ID пользователя из localStorage для заголовка
            const userId = localStorage.getItem("userId");
            if (!userId) {
                throw new Error("Ошибка авторизации. Пожалуйста, перезайдите в систему.");
            }

            const response = await fetch(`/api/message/delete-for-all/${messageId}`, {
                method: 'DELETE',
                headers: {
                    'UserId': userId
                }
            });

            return this.checkResponse(response, "Ошибка при удалении сообщения");
        } catch (error) {
            return this.handleError(error, "Ошибка при удалении сообщения");
        }
    }
    
    /**
     * Редактировать сообщение
     * @param {number} messageId - ID сообщения
     * @param {string} newText - Новый текст сообщения
     * @returns {Promise<Object>} - Результат операции
     */
    static async editMessage(messageId, newText) {
        try {
            if (!messageId || !newText) {
                throw new Error("ID сообщения или новый текст не указаны");
            }

            const response = await fetch(`/api/message/edit/${messageId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    text: newText
                })
            });

            return this.checkResponse(response, "Ошибка при редактировании сообщения");
        } catch (error) {
            return this.handleError(error, "Ошибка при редактировании сообщения");
        }
    }

    /**
     * Запускает автоматическое обновление чата
     * @param {number} userId - ID текущего пользователя
     * @param {number} contactId - ID собеседника
     */
    static startAutoUpdate(userId, contactId) {
        this.currentContactId = contactId;
        
        // Останавливаем предыдущее обновление, если оно было
        if (this.updateInterval) {
            clearInterval(this.updateInterval);
        }
        
        // Запускаем новое обновление каждые 3 секунды
        this.updateInterval = setInterval(async () => {
            try {
                const response = await this.getConversation(userId, contactId);
                if (response.success && response.messages) {
                    this.updateChatMessages(response.messages);
                }
            } catch (error) {
                console.error('Ошибка при автоматическом обновлении чата:', error);
            }
        }, 3000);
    }

    /**
     * Останавливает автоматическое обновление чата
     */
    static stopAutoUpdate() {
        if (this.updateInterval) {
            clearInterval(this.updateInterval);
            this.updateInterval = null;
        }
        this.currentContactId = null;
    }

    /**
     * Обновляет сообщения в чате
     * @param {Array} messages - Новые сообщения
     */
    static updateChatMessages(messages) {
        const messagesContainer = document.querySelector('.messages-container');
        if (!messagesContainer) return;

        // Получаем текущие сообщения
        const currentMessages = Array.from(messagesContainer.querySelectorAll('.message-wrapper'))
            .map(el => ({
                messageId: parseInt(el.dataset.id),
                element: el
            }));

        // Обрабатываем новые сообщения
        messages.forEach(message => {
            const existingMessageEl = currentMessages.find(m => m.messageId === message.messageId);
            
            if (!existingMessageEl) {
                // Добавляем новое сообщение
                const isSender = message.senderId === parseInt(UserAPI.getCurrentUserId());
                const messageElement = MessengerUI.createMessageElement(message, isSender);
                messagesContainer.appendChild(messageElement);
                
                // Если есть вложение, сразу начинаем его загрузку
                if (message.hasAttachment) {
                    MessengerUI.loadMessageAttachment(message.messageId, messageElement);
                }
            } else if (message.hasAttachment) {
                // Проверяем, если у существующего сообщения есть вложение, но оно не загружено
                const hasLoadedAttachment = existingMessageEl.element.querySelector('.message-image') ||
                                           existingMessageEl.element.querySelector('.file-attachment') ||
                                           existingMessageEl.element.querySelector('.message-file');
                
                // Если вложение не загружено, но флаг hasAttachment=true, загружаем его
                if (!hasLoadedAttachment) {
                    MessengerUI.loadMessageAttachment(message.messageId, existingMessageEl.element);
                }
            }
        });

        // Прокручиваем к последнему сообщению
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    static async sendMessageWithAttachment(senderId, receiverId, text, file) {
        try {
            // Сначала отправляем сообщение
            const messageResponse = await this.sendMessage(senderId, receiverId, text);
            if (!messageResponse.success) {
                throw new Error(messageResponse.message || 'Ошибка отправки сообщения');
            }

            const messageId = messageResponse.message.messageId;

            // Создаем временный элемент для отображения загрузки
            const tempMessage = {
                messageId,
                senderId,
                receiverId,
                text,
                timestamp: new Date(),
                isRead: false,
                hasAttachment: true
            };

            // Проверяем не существует ли уже этот элемент (предотвращаем дублирование)
            let messageElement = document.querySelector(`.message-wrapper[data-id="${messageId}"]`);
            if (!messageElement) {
                // Добавляем сообщение в чат только если его еще нет
                messageElement = MessengerUI.createMessageElement(tempMessage, true);
                const messagesContainer = document.querySelector('.messages-container');
                if (messagesContainer) {
                    messagesContainer.appendChild(messageElement);
                    messagesContainer.scrollTop = messagesContainer.scrollHeight;
                }
            }

            // Загружаем файл
            const uploadResponse = await MediaFileAPI.uploadMedia(file, messageId);
            if (!uploadResponse.success) {
                throw new Error(uploadResponse.message || 'Ошибка загрузки файла');
            }

            // После успешной загрузки немедленно обновляем вложение в сообщении
            if (messageElement) {
                // Удаляем временный индикатор загрузки если он есть
                const loadingIndicator = messageElement.querySelector('.attachment-loading');
                if (loadingIndicator && loadingIndicator.parentNode) {
                    loadingIndicator.parentNode.removeChild(loadingIndicator);
                }
                
                // Проверяем наличие контейнера для вложения
                let attachmentContainer = messageElement.querySelector('.message-content-attachment');
                if (!attachmentContainer) {
                    const messageContent = messageElement.querySelector('.message-content');
                    if (messageContent) {
                        attachmentContainer = document.createElement('div');
                        attachmentContainer.className = 'message-content-attachment';
                        messageContent.insertBefore(attachmentContainer, messageContent.firstChild);
                    }
                }
                
                // Загружаем и отображаем вложение
                if (attachmentContainer) {
                    // Получаем информацию о загруженном файле
                    const mediaInfo = await MediaFileAPI.getMessageMediaInfo(messageId);
                    if (mediaInfo && mediaInfo.length > 0) {
                        const mediaFile = mediaInfo[0];
                        
                        // Проверяем наличие имени файла, если его нет - используем запасной вариант
                        const fileName = mediaFile.name || (file ? file.name : 'Файл') || 'Файл';
                        const fileSize = mediaFile.size || (file ? file.size : 0);
                        
                        if (mediaFile.type && mediaFile.type.toLowerCase() === "image") {
                            // Создаем элемент изображения
                            attachmentContainer.innerHTML = `
                                <div class="image-attachment">
                                    <img src="/api/message/media/${messageId}" alt="Изображение" 
                                         onclick="if(window.imageViewer) window.imageViewer.open('/api/message/media/${messageId}')">
                                </div>
                            `;
                        } else {
                            // Определяем иконку в зависимости от расширения файла
                            let iconClass = 'ri-file-line';
                            const fileExt = fileName.split('.').pop().toLowerCase();
                            
                            // Выбираем подходящую иконку в зависимости от типа файла
                            if (['pdf'].includes(fileExt)) {
                                iconClass = 'ri-file-pdf-line';
                            } else if (['doc', 'docx'].includes(fileExt)) {
                                iconClass = 'ri-file-word-line';
                            } else if (['xls', 'xlsx'].includes(fileExt)) {
                                iconClass = 'ri-file-excel-line';
                            } else if (['ppt', 'pptx'].includes(fileExt)) {
                                iconClass = 'ri-file-ppt-line';
                            } else if (['zip', 'rar', '7z', 'tar', 'gz'].includes(fileExt)) {
                                iconClass = 'ri-file-zip-line';
                            } else if (['jpg', 'jpeg', 'png', 'gif', 'bmp', 'svg'].includes(fileExt)) {
                                iconClass = 'ri-image-line';
                            } else if (['mp3', 'wav', 'ogg', 'aac'].includes(fileExt)) {
                                iconClass = 'ri-music-line';
                            } else if (['mp4', 'avi', 'mov', 'wmv', 'mkv'].includes(fileExt)) {
                                iconClass = 'ri-video-line';
                            } else if (['txt', 'html', 'css', 'js'].includes(fileExt)) {
                                iconClass = 'ri-file-text-line';
                            }
                            
                            // Создаем элемент файла с улучшенным дизайном
                            attachmentContainer.innerHTML = `
                                <div class="file-attachment">
                                    <div class="file-icon-container">
                                        <i class="${iconClass}"></i>
                                        <span class="file-ext">${fileExt}</span>
                                    </div>
                                    <div class="file-details">
                                        <a href="/api/message/media/${messageId}" target="_blank" download="${fileName}" class="file-download-link">
                                            <span class="file-name">${fileName}</span>
                                            <div class="file-size-row">
                                                <span class="file-size">${MessengerUI.formatFileSize(fileSize)}</span>
                                                <i class="ri-download-line download-icon"></i>
                                            </div>
                                        </a>
                                    </div>
                                </div>
                            `;
                        }
                    }
                }
            }

            return messageResponse;
        } catch (error) {
            console.error('Ошибка при отправке сообщения с вложением:', error);
            throw error;
        }
    }
}

/**
 * API для работы с медиафайлами
 */
class MediaFileAPI {
    /**
     * Получает информацию о медиафайлах для сообщения
     * @param {number} messageId - ID сообщения
     * @returns {Promise<Array>} - Информация о медиафайлах
     */
    static async getMessageMediaInfo(messageId) {
        try {
            if (!messageId) return [];
            
            const response = await fetch(`/api/message/media-info/${messageId}`);
            
            if (!response.ok) {
                throw new Error(`Ошибка HTTP: ${response.status}`);
            }
            
            const data = await response.json();
            return data.success ? data.mediaFiles : [];
        } catch (error) {
            console.error('Ошибка при получении информации о медиафайлах:', error);
            return [];
        }
    }
    
    /**
     * Загружает медиафайл на сервер
     * @param {File} file - Файл для загрузки
     * @param {number} messageId - ID сообщения, к которому относится файл
     * @returns {Promise<Object>} - Результат загрузки
     */
    static async uploadMedia(file, messageId) {
        try {
            if (!file || !messageId) {
                throw new Error('Не указан файл или ID сообщения');
            }
            
            // Проверка размера файла
            const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB - согласно ограничению на сервере
            const CHUNK_SIZE = 2 * 1024 * 1024; // 2MB chunks
            
            if (file.size > MAX_FILE_SIZE) {
                throw new Error(`Размер файла не должен превышать ${MAX_FILE_SIZE / (1024 * 1024)} MB`);
            }
            
            // Для файлов меньше 2MB используем обычную загрузку
            if (file.size <= CHUNK_SIZE) {
                return await this.uploadSmallMedia(file, messageId);
            } else {
                // Для больших файлов используем чанкированную загрузку
                return await this.uploadLargeMedia(file, messageId);
            }
        } catch (error) {
            console.error('Ошибка при загрузке медиафайла:', error);
            throw error;
        }
    }
    
    /**
     * Загружает небольшой медиафайл на сервер
     * @param {File} file - Файл для загрузки
     * @param {number} messageId - ID сообщения
     * @returns {Promise<Object>} - Результат загрузки
     */
    static async uploadSmallMedia(file, messageId) {
        try {
            const formData = new FormData();
            formData.append('file', file);
            formData.append('messageId', messageId);
            
            // Добавляем заголовок UserId
            const userId = UserAPI.getCurrentUserId();
            
            const response = await fetch('/api/message/upload-media', {
                method: 'POST',
                headers: {
                    'UserId': userId
                },
                body: formData
            });
            
            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`Ошибка HTTP: ${response.status} - ${errorText}`);
            }
            
            return await response.json();
        } catch (error) {
            console.error('Ошибка при загрузке небольшого медиафайла:', error);
            throw error;
        }
    }
    
    /**
     * Загружает большой медиафайл на сервер частями
     * @param {File} file - Файл для загрузки
     * @param {number} messageId - ID сообщения
     * @returns {Promise<Object>} - Результат загрузки
     */
    static async uploadLargeMedia(file, messageId) {
        try {
            const CHUNK_SIZE = 2 * 1024 * 1024; // 2MB chunks
            const totalChunks = Math.ceil(file.size / CHUNK_SIZE);
            const fileId = this.generateFileId();
            
            // Отображаем прогресс загрузки
            const progressCallback = (current, total) => {
                const percent = Math.round((current / total) * 100);
                
                // Найти сообщение с этим ID и обновить индикатор загрузки
                const messageElement = document.querySelector(`[data-message-id="${messageId}"]`);
                if (messageElement) {
                    const loadingIndicator = messageElement.querySelector('.attachment-loading');
                    if (loadingIndicator) {
                        loadingIndicator.innerHTML = `<i class="ri-loader-4-line"></i> Загрузка: ${percent}%`;
                    }
                }
            };
            
            // Добавляем заголовок UserId
            const userId = UserAPI.getCurrentUserId();
            
            // Загрузка чанков
            for (let chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++) {
                const start = chunkIndex * CHUNK_SIZE;
                const end = Math.min(file.size, start + CHUNK_SIZE);
                const chunk = file.slice(start, end);
                
                const formData = new FormData();
                formData.append('file', chunk);
                formData.append('messageId', messageId);
                formData.append('chunkIndex', chunkIndex);
                formData.append('totalChunks', totalChunks);
                formData.append('fileId', fileId);
                
                const response = await fetch('/api/message/upload-media-chunk', {
                    method: 'POST',
                    headers: {
                        'UserId': userId
                    },
                    body: formData
                });
                
                if (!response.ok) {
                    const errorData = await response.json();
                    throw new Error(errorData.message || `Ошибка загрузки чанка ${chunkIndex}: ${response.status}`);
                }
                
                const chunkResult = await response.json();
                
                // Проверяем, завершена ли загрузка
                if (chunkResult.success && chunkIndex === totalChunks - 1) {
                    return chunkResult;
                }
                
                // Вызываем колбэк прогресса
                progressCallback(chunkIndex + 1, totalChunks);
            }
            
            // Проверяем статус загрузки
            return await this.checkUploadStatus(fileId, userId);
        } catch (error) {
            console.error('Ошибка при загрузке большого медиафайла:', error);
            throw error;
        }
    }
    
    /**
     * Проверяет статус загрузки чанков
     * @param {string} fileId - ID файла
     * @param {number} userId - ID пользователя
     * @returns {Promise<Object>}
     */
    static async checkUploadStatus(fileId, userId) {
        try {
            const response = await fetch(`/api/message/check-upload-status?fileId=${fileId}&userId=${userId}`);
            
            if (!response.ok) {
                throw new Error(`Ошибка получения статуса загрузки: ${response.status}`);
            }
            
            return await response.json();
        } catch (error) {
            console.error('Ошибка при проверке статуса загрузки:', error);
            throw error;
        }
    }
    
    /**
     * Отменяет загрузку файла
     * @param {string} fileId - ID файла
     * @returns {Promise<Object>}
     */
    static async cancelUpload(fileId) {
        try {
            const userId = UserAPI.getCurrentUserId();
            
            const response = await fetch(`/api/message/cancel-upload?fileId=${fileId}`, {
                method: 'DELETE',
                headers: {
                    'UserId': userId
                }
            });
            
            if (!response.ok) {
                throw new Error(`Ошибка отмены загрузки: ${response.status}`);
            }
            
            return await response.json();
        } catch (error) {
            console.error('Ошибка при отмене загрузки:', error);
            throw error;
        }
    }
    
    /**
     * Генерирует уникальный идентификатор файла
     * @returns {string}
     */
    static generateFileId() {
        return 'file_' + Date.now() + '_' + Math.random().toString(36).substring(2);
    }
    
    /**
     * Получает URL для доступа к медиафайлу
     * @param {number} messageId - ID сообщения
     * @param {boolean} useStream - Использовать потоковую передачу для больших файлов
     * @returns {string} - URL для доступа к медиафайлу
     */
    static getMediaUrl(messageId, useStream = false) {
        // Создаем постоянное кэширование файлов с помощью версионирования URL
        // Используем messageId как версию для стабильности URL
        if (useStream) {
            return `/api/message/media/${messageId}/stream`;
        }
        return `/api/message/media/${messageId}`;
    }
}

/**
 * Вспомогательный класс для работы с UI
 */
class MessengerUI {
    /**
     * Показать уведомление
     * @param {string} message - Текст уведомления
     */
    static showNotification(message) {
        const notification = document.getElementById("notification");
        if (!notification) return;
        const notificationMessage = notification.querySelector(".notification-message");
        if (notificationMessage) notificationMessage.textContent = message;
        notification.classList.add("show");
        setTimeout(() => {
            MessengerUI.hideNotification();
        }, 3000);
        const closeBtn = notification.querySelector(".notification-close");
        if (closeBtn) {
            closeBtn.onclick = MessengerUI.hideNotification;
        }
    }

    /**
     * Скрыть уведомление
     */
    static hideNotification() {
        const notification = document.getElementById("notification");
        if (!notification) return;
        notification.classList.remove("show");
    }

    /**
     * Создать элемент контакта
     * @param {Object} contact - Информация о контакте
     * @param {HTMLElement} container - Контейнер для элемента
     * @returns {HTMLElement} Созданный элемент
     */
    static createContactItem(contact, container) {
        const contactItem = document.createElement("div");
        contactItem.className = "contact-item";
        contactItem.dataset.id = contact.Id || contact.id || contact.userId || contact.пользовательId;

        const contactAvatar = document.createElement("div");
        contactAvatar.className = "contact-avatar";
        const avatarIcon = document.createElement("i");
        avatarIcon.className = "ri-user-line";
        contactAvatar.appendChild(avatarIcon);

        const contactInfo = document.createElement("div");
        contactInfo.className = "contact-info";

        const contactName = document.createElement("div");
        contactName.className = "contact-name";
        contactName.textContent = contact.Login || contact.login || contact.имяПользователя || contact.userName;

        const contactRole = document.createElement("div");
        contactRole.className = "contact-role";
        contactRole.textContent = contact.Role || contact.role || contact.роль;

        contactInfo.appendChild(contactName);
        contactInfo.appendChild(contactRole);

        contactItem.appendChild(contactAvatar);
        contactItem.appendChild(contactInfo);

        if ((contact.UnreadCount || contact.unreadCount || 0) > 0) {
            const contactStatus = document.createElement("div");
            contactStatus.className = "contact-status";
            contactStatus.textContent = contact.UnreadCount || contact.unreadCount;
            contactItem.appendChild(contactStatus);
        }

        if (container) {
            container.appendChild(contactItem);
        }
        
        return contactItem;
    }

    /**
     * Создание элемента сообщения
     * @param {Object} message - Объект сообщения
     * @param {boolean} isSender - Является ли текущий пользователь отправителем сообщения
     * @returns {HTMLElement} - Элемент сообщения
     */
    static createMessageElement(message, isSender) {
        const messageWrapper = document.createElement("div");
        messageWrapper.className = `message-wrapper ${isSender ? 'sent' : 'received'}`;
        messageWrapper.dataset.id = message.messageId;
        
        // Создаем контейнер для сообщения
        const messageBubble = document.createElement("div");
        messageBubble.className = "message-bubble";
        
        // Проверяем наличие вложений
        if (message.hasAttachment) {
            // Если есть previewUrl (для быстрого отображения изображений)
            if (message.previewUrl && message.file && message.file.type.startsWith('image/')) {
                // Создаем контейнер для изображения
                const imageContainer = document.createElement("div");
                imageContainer.className = "message-image-container";
                
                // Создаем элемент изображения с предпросмотром
                const imageElement = document.createElement("img");
                imageElement.className = "message-attachment";
                imageElement.alt = "Изображение";
                imageElement.src = message.previewUrl;
                
                // Добавляем обработчик клика для открытия изображения в новом окне
                imageElement.addEventListener("click", () => {
                    const mediaUrl = MediaFileAPI.getMediaUrl(message.messageId);
                    // Используем ImageViewer вместо открытия в новой вкладке
                    if (window.imageViewer) {
                        window.imageViewer.open(mediaUrl);
                    } else {
                        // Запасной вариант, если ImageViewer не инициализирован
                        window.open(mediaUrl, "_blank");
                        console.warn('ImageViewer не инициализирован. Проверьте подключение ImageViewer.js');
                    }
                });
                
                // Добавляем обработчик ошибки загрузки
                imageElement.onerror = function() {
                    console.error(`Ошибка загрузки предпросмотра изображения для сообщения ID: ${message.messageId}`);
                    // Заменяем изображение на иконку ошибки
                    this.src = "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJub25lIiBzdHJva2U9ImN1cnJlbnRDb2xvciIgc3Ryb2tlLXdpZHRoPSIyIiBzdHJva2UtbGluZWNhcD0icm91bmQiIHN0cm9rZS1saW5lam9pbj0icm91bmQiPjxjaXJjbGUgY3g9IjEyIiBjeT0iMTIiIHI9IjEwIj48L2NpcmNsZT48bGluZSB4MT0iMTIiIHkxPSI4IiB4Mj0iMTIiIHkyPSIxMiI+PC9saW5lPjxsaW5lIHgxPSIxMiIgeTE9IjE2IiB4Mj0iMTIuMDEiIHkyPSIxNiI+PC9saW5lPjwvc3ZnPg==";
                    this.style.padding = "10px";
                    this.style.boxSizing = "border-box";
                };
                
                imageContainer.appendChild(imageElement);
                messageBubble.appendChild(imageContainer);
            } else {
                // Загружаем и отображаем вложение стандартным способом
                this.loadMessageAttachment(message.messageId, messageBubble);
            }
        }
        
        // Добавляем текст сообщения
        const messageContent = document.createElement("div");
        messageContent.className = "message-content";
        messageContent.textContent = message.text;
        messageBubble.appendChild(messageContent);
        
        // Добавляем время отправки
        const messageTime = document.createElement("div");
        messageTime.className = "message-time";
        const messageDate = new Date(message.timestamp);
        messageTime.textContent = this.formatTime(messageDate);
        
        // Добавляем индикатор прочтения для исходящих сообщений
        if (isSender) {
            const readStatus = document.createElement("span");
            readStatus.className = "read-status";
            readStatus.innerHTML = message.isRead ? "✓✓" : "✓";
            messageTime.appendChild(readStatus);
        }
        
        messageBubble.appendChild(messageTime);
        messageWrapper.appendChild(messageBubble);
        
        // Добавляем обработчик контекстного меню
        messageWrapper.addEventListener('contextmenu', (e) => {
            showMessageContextMenu(e, message.messageId, message.text, isSender);
        });
        
        return messageWrapper;
    }
    
    /**
     * Загрузка вложения сообщения
     * @param {number} messageId - ID сообщения
     * @param {HTMLElement} messageBubble - Контейнер сообщения
     */
    static async loadMessageAttachment(messageId, messageBubble) {
        try {
            // Проверяем параметры
            if (!messageId || !messageBubble) {
                console.error("Недостаточно параметров для загрузки вложения");
                return;
            }
            
            // Создаем индикатор загрузки
            const loadingIndicator = document.createElement("div");
            loadingIndicator.className = "attachment-loading";
            loadingIndicator.innerHTML = '<i class="ri-loader-4-line"></i> Загрузка...';
            messageBubble.insertBefore(loadingIndicator, messageBubble.firstChild);
            
            console.log(`Загрузка вложений для сообщения ID: ${messageId}`);
            
            // Получаем информацию о медиафайлах сообщения
            const mediaFiles = await MediaFileAPI.getMessageMediaInfo(messageId);
            
            // Удаляем индикатор загрузки
            if (loadingIndicator && loadingIndicator.parentNode) {
                loadingIndicator.parentNode.removeChild(loadingIndicator);
            }
            
            if (!mediaFiles || mediaFiles.length === 0) {
                console.info(`Вложения для сообщения ${messageId} не найдены`);
                return;
            }
            
            console.log(`Найдено вложений: ${mediaFiles.length}`, mediaFiles);
            
            // Функция для определения, нужно ли использовать потоковую загрузку
            const shouldUseStream = (fileSize) => {
                return fileSize && fileSize > 2 * 1024 * 1024; // Файлы больше 2MB
            };
            
            // Для каждого медиафайла создаем соответствующий элемент
            mediaFiles.forEach(mediaFile => {
                // Определяем, использовать ли потоковую загрузку для больших файлов
                const useStream = shouldUseStream(mediaFile.size);
                const mediaUrl = MediaFileAPI.getMediaUrl(messageId, useStream);
                
                if (mediaFile.type.toLowerCase() === "image") {
                    // Создаем контейнер для изображения
                    const imageContainer = document.createElement("div");
                    imageContainer.className = "message-image-container";
                    
                    // Добавляем контейнер в начало сообщения
                    messageBubble.insertBefore(imageContainer, messageBubble.firstChild);
                    
                    // Создаем элемент изображения с прогрессом загрузки
                    const imageElement = document.createElement("img");
                    imageElement.className = "message-attachment";
                    imageElement.alt = "Изображение";
                    imageElement.loading = "lazy"; // Ленивая загрузка для оптимизации
                    
                    // Создаем прогресс-индикатор для больших файлов
                    let progressIndicator = null;
                    if (useStream) {
                        progressIndicator = document.createElement("div");
                        progressIndicator.className = "image-load-progress";
                        progressIndicator.innerHTML = '<div class="progress-bar"></div>';
                        imageContainer.appendChild(progressIndicator);
                    }
                    
                    // Устанавливаем источник изображения
                    imageElement.src = mediaUrl;
                    
                    // Добавляем обработчик клика для открытия изображения в новом окне
                    imageElement.addEventListener("click", () => {
                        // Используем ImageViewer вместо открытия в новой вкладке
                        if (window.imageViewer) {
                            window.imageViewer.open(mediaUrl);
                        } else {
                            // Запасной вариант, если ImageViewer не инициализирован
                            window.open(mediaUrl, '_blank');
                            console.warn('ImageViewer не найден. Проверьте подключение ImageViewer.js');
                        }
                    });
                    
                    // Добавляем обработчик для показа загруженного изображения
                    imageElement.onload = function() {
                        console.log(`Изображение для сообщения ${messageId} успешно загружено`);
                        // Удаляем индикатор прогресса, если он существует
                        if (progressIndicator) {
                            imageContainer.removeChild(progressIndicator);
                        }
                    };
                    
                    // Добавляем обработчик ошибки загрузки
                    imageElement.onerror = function() {
                        console.error(`Ошибка загрузки изображения для сообщения ID: ${messageId}, URL: ${mediaUrl}`);
                        
                        // Удаляем индикатор прогресса, если он существует
                        if (progressIndicator && progressIndicator.parentNode) {
                            progressIndicator.parentNode.removeChild(progressIndicator);
                        }
                        
                        // Заменяем изображение на иконку ошибки
                        this.src = "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJub25lIiBzdHJva2U9ImN1cnJlbnRDb2xvciIgc3Ryb2tlLXdpZHRoPSIyIiBzdHJva2UtbGluZWNhcD0icm91bmQiIHN0cm9rZS1saW5lam9pbj0icm91bmQiPjxjaXJjbGUgY3g9IjEyIiBjeT0iMTIiIHI9IjEwIj48L2NpcmNsZT48bGluZSB4MT0iMTIiIHkxPSI4IiB4Mj0iMTIiIHkyPSIxMiI+PC9saW5lPjxsaW5lIHgxPSIxMiIgeTE9IjE2IiB4Mj0iMTIuMDEiIHkyPSIxNiI+PC9saW5lPjwvc3ZnPg==";
                        this.style.padding = "10px";
                        this.style.boxSizing = "border-box";
                        
                        // Добавляем текстовое сообщение об ошибке
                        const errorMsg = document.createElement("div");
                        errorMsg.className = "attachment-error-message";
                        errorMsg.textContent = "Ошибка загрузки изображения";
                        imageContainer.appendChild(errorMsg);
                        
                        // Кнопка для повторной попытки
                        const retryButton = document.createElement("button");
                        retryButton.className = "retry-button";
                        retryButton.innerHTML = '<i class="ri-refresh-line"></i> Повторить';
                        retryButton.onclick = () => {
                            // Удаляем сообщение об ошибке
                            if (errorMsg.parentNode) {
                                errorMsg.parentNode.removeChild(errorMsg);
                            }
                            
                            // Удаляем кнопку повтора
                            retryButton.parentNode.removeChild(retryButton);
                            
                            // Создаем новый индикатор прогресса
                            if (useStream) {
                                progressIndicator = document.createElement("div");
                                progressIndicator.className = "image-load-progress";
                                progressIndicator.innerHTML = '<div class="progress-bar"></div>';
                                imageContainer.appendChild(progressIndicator);
                            }
                            
                            // Пробуем загрузить изображение еще раз
                            const newSrc = mediaUrl + (mediaUrl.includes('?') ? '&' : '?') + "retry=" + new Date().getTime();
                            this.src = newSrc;
                        };
                        imageContainer.appendChild(retryButton);
                    };
                    
                    // Для больших изображений измеряем прогресс загрузки
                    if (useStream && window.fetch && "onprogress" in new XMLHttpRequest()) {
                        // Извлекаем URL для измерения прогресса загрузки
                        this.loadImageWithProgress(mediaUrl, imageElement, progressIndicator);
                    } else {
                        // Для браузеров без поддержки, просто отображаем изображение
                        imageContainer.appendChild(imageElement);
                    }
                } else if (mediaFile.type.toLowerCase() === "video") {
                    // Создаем контейнер для видео
                    const videoContainer = document.createElement("div");
                    videoContainer.className = "message-video-container";
                    
                    // Добавляем контейнер в начало сообщения
                    messageBubble.insertBefore(videoContainer, messageBubble.firstChild);
                    
                    // Создаем элемент видео с поддержкой потоковой передачи для больших файлов
                    const videoElement = document.createElement("video");
                    videoElement.className = "message-attachment";
                    videoElement.controls = true;
                    videoElement.preload = "metadata";
                    
                    // Для больших видео используем source с type для лучшей поддержки потоковой передачи
                    if (useStream) {
                        const source = document.createElement("source");
                        source.src = mediaUrl;
                        source.type = "video/mp4"; // Предполагаем, что большинство видео - MP4
                        videoElement.appendChild(source);
                    } else {
                        videoElement.src = mediaUrl;
                    }
                    
                    // Добавляем обработчик ошибки загрузки
                    videoElement.onerror = function(e) {
                        console.error(`Ошибка загрузки видео для сообщения ID: ${messageId}`, e);
                        this.onerror = null;
                        this.style.display = "none";
                        
                        const errorText = document.createElement("div");
                        errorText.className = "attachment-error";
                        errorText.innerHTML = '<i class="ri-error-warning-line"></i> Ошибка загрузки видео';
                        
                        // Кнопка для повторной попытки
                        const retryButton = document.createElement("button");
                        retryButton.className = "retry-button";
                        retryButton.innerHTML = '<i class="ri-refresh-line"></i> Повторить';
                        retryButton.onclick = () => {
                            // Удаляем сообщение об ошибке
                            if (errorText.parentNode) {
                                videoContainer.removeChild(errorText);
                            }
                            
                            // Удаляем кнопку повтора
                            retryButton.parentNode.removeChild(retryButton);
                            
                            // Пробуем загрузить видео еще раз
                            videoElement.style.display = "block";
                            const newSrc = mediaUrl + (mediaUrl.includes('?') ? '&' : '?') + "retry=" + new Date().getTime();
                            videoElement.src = newSrc;
                            videoElement.load();
                        };
                        
                        videoContainer.appendChild(errorText);
                        videoContainer.appendChild(retryButton);
                    };
                    
                    // Добавляем видео в контейнер
                    videoContainer.appendChild(videoElement);
                } else {
                    // Для других типов файлов создаем ссылку для скачивания
                    const fileContainer = document.createElement("div");
                    fileContainer.className = "message-file-container";
                    
                    const fileLink = document.createElement("a");
                    fileLink.className = "message-file";
                    fileLink.href = mediaUrl;
                    fileLink.target = "_blank";
                    fileLink.download = "";  // Для скачивания файла
                    fileLink.style.color = "white"; // Make text white 
                    
                    // Выбираем подходящую иконку в зависимости от типа файла
                    let iconClass = "ri-file-line";
                    
                    if (mediaFile.type.includes('pdf')) {
                        iconClass = "ri-file-pdf-line";
                    } else if (mediaFile.type.includes('word') || mediaFile.type.includes('doc')) {
                        iconClass = "ri-file-word-line";
                    } else if (mediaFile.type.includes('excel') || mediaFile.type.includes('sheet') || mediaFile.type.includes('xls')) {
                        iconClass = "ri-file-excel-line";
                    } else if (mediaFile.type.includes('text')) {
                        iconClass = "ri-file-text-line";
                    }
                    
                    // Создаем HTML-структуру с именем файла и иконкой загрузки
                    fileLink.innerHTML = `
                        <div style="display: flex; align-items: center;">
                            <i class="${iconClass}" style="margin-right: 8px;"></i>
                            <span style="white-space: nowrap; overflow: hidden; text-overflow: ellipsis; max-width: 150px;">${mediaFile.name || mediaFile.type}</span>
                        </div>
                        <i class="ri-download-line"></i>
                    `;
                    
                    // Если у нас есть размер файла, добавим его
                    if (mediaFile.size) {
                        const sizeFormatted = this.formatFileSize(mediaFile.size);
                        const fileSizeElement = document.createElement("div");
                        fileSizeElement.className = "file-size";
                        fileSizeElement.textContent = sizeFormatted;
                        fileContainer.appendChild(fileSizeElement);
                    }
                    
                    // Добавляем ссылку в контейнер
                    fileContainer.appendChild(fileLink);
                    
                    // Добавляем контейнер в начало сообщения
                    messageBubble.insertBefore(fileContainer, messageBubble.firstChild);
                }
            });
        } catch (error) {
            console.error(`Ошибка при загрузке вложений для сообщения ${messageId}:`, error);
            
            // Удаляем индикатор загрузки, если он существует
            const loadingIndicator = messageBubble.querySelector('.attachment-loading');
            if (loadingIndicator && loadingIndicator.parentNode) {
                loadingIndicator.parentNode.removeChild(loadingIndicator);
            }
            
            // Добавляем сообщение об ошибке
            const errorElement = document.createElement("div");
            errorElement.className = "attachment-error";
            errorElement.innerHTML = '<i class="ri-error-warning-line"></i> Не удалось загрузить вложение';
            
            // Кнопка для повторной попытки
            const retryButton = document.createElement("button");
            retryButton.className = "retry-button";
            retryButton.innerHTML = '<i class="ri-refresh-line"></i> Повторить';
            retryButton.onclick = () => {
                // Удаляем сообщение об ошибке
                if (errorElement.parentNode) {
                    errorElement.parentNode.removeChild(errorElement);
                }
                
                // Удаляем кнопку повтора
                retryButton.parentNode.removeChild(retryButton);
                
                // Пробуем загрузить вложение еще раз
                this.loadMessageAttachment(messageId, messageBubble);
            };
            
            messageBubble.insertBefore(errorElement, messageBubble.firstChild);
            messageBubble.insertBefore(retryButton, messageBubble.firstChild.nextSibling);
        }
    }
    
    /**
     * Загружает изображение с отслеживанием прогресса
     * @param {string} url - URL изображения
     * @param {HTMLImageElement} imageElement - Элемент изображения
     * @param {HTMLElement} progressIndicator - Индикатор прогресса
     */
    static loadImageWithProgress(url, imageElement, progressIndicator) {
        // Проверяем, есть ли изображение в кэше браузера
        const img = new Image();
        img.onload = function() {
            // Если изображение уже в кэше, просто используем его
            imageElement.src = url;
            // Удаляем индикатор прогресса
            if (progressIndicator && progressIndicator.parentNode) {
                progressIndicator.parentNode.removeChild(progressIndicator);
            }
        };
        img.onerror = function() {
            // Если изображение не в кэше, загружаем с отслеживанием прогресса
            const xhr = new XMLHttpRequest();
            xhr.open('GET', url, true);
            xhr.responseType = 'blob';
            
            // Устанавливаем обработчик прогресса
            xhr.onprogress = function(event) {
                if (event.lengthComputable && progressIndicator) {
                    const percent = Math.round((event.loaded / event.total) * 100);
                    const progressBar = progressIndicator.querySelector('.progress-bar');
                    if (progressBar) {
                        progressBar.style.width = percent + '%';
                    }
                }
            };
            
            // Устанавливаем обработчик завершения
            xhr.onload = function() {
                if (this.status === 200) {
                    const blob = this.response;
                    const objectURL = URL.createObjectURL(blob);
                    
                    // Устанавливаем URL для изображения
                    imageElement.src = objectURL;
                    
                    // Сохраняем blob в локальном кэше для повторного использования
                    try {
                        const urlKey = `image_${url.split('/').pop()}`;
                        localStorage.setItem(urlKey, objectURL);
                    } catch (e) {
                        console.warn('Не удалось сохранить изображение в локальном хранилище:', e);
                    }
                    
                    // Освобождаем URL при выгрузке изображения
                    imageElement.onload = function() {
                        // Удаляем индикатор прогресса
                        if (progressIndicator && progressIndicator.parentNode) {
                            progressIndicator.parentNode.removeChild(progressIndicator);
                        }
                    };
                } else {
                    // В случае ошибки устанавливаем обработчик ошибки изображения
                    imageElement.onerror();
                }
            };
            
            // Устанавливаем обработчик ошибки
            xhr.onerror = function() {
                imageElement.onerror();
            };
            
            // Запускаем запрос
            xhr.send();
        };
        
        // Проверяем, есть ли изображение в кэше
        img.src = url;
    }
    
    /**
     * Форматирует размер файла в читаемый вид
     * @param {number} bytes - Размер в байтах
     * @returns {string} Отформатированный размер
     */
    static formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }
    
    /**
     * Форматирование времени
     * @param {Date} date - Дата
     * @returns {string} Отформатированное время
     */
    static formatTime(date) {
        if (!date || !(date instanceof Date)) return "";
        return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }
}

/**
 * Класс для работы с медиафайлами и вложениями
 */
class MediaHandler {
    /**
     * Показывает предпросмотр выбранного файла
     * @param {File} file - Файл для предпросмотра
     * @param {HTMLElement} container - Контейнер для предпросмотра
     */
    static showFilePreview(file, container) {
        if (!file || !container) return;
        
        // Очищаем контейнер
        container.innerHTML = '';
        
        // Создаем элемент предпросмотра в зависимости от типа файла
        if (file.type.startsWith('image/')) {
            // Предпросмотр изображения
            const img = document.createElement('img');
            img.className = 'attachment-thumbnail';
            img.file = file;
            
            const reader = new FileReader();
            reader.onload = function(e) {
                img.src = e.target.result;
            };
            reader.readAsDataURL(file);
            
            container.appendChild(img);
        } else {
            // Предпросмотр для других типов файлов
            const icon = document.createElement('i');
            
            if (file.type.startsWith('video/')) {
                icon.className = 'ri-video-line';
            } else if (file.type.startsWith('audio/')) {
                icon.className = 'ri-file-music-line';
            } else {
                icon.className = 'ri-file-line';
            }
            
            container.appendChild(icon);
        }
        
        // Добавляем название файла
        const fileName = document.createElement('div');
        fileName.className = 'attachment-filename';
        fileName.textContent = file.name;
        container.appendChild(fileName);
    }
    
    /**
     * Создает медиаэлемент в зависимости от типа файла
     * @param {Object} attachment - Информация о вложении
     * @returns {HTMLElement} Медиаэлемент
     */
    static createMediaElement(attachment) {
        if (!attachment || !attachment.mediaType) return null;
        
        const mediaUrl = MessageAPI.getMediaUrl(attachment.mediaId);
        let mediaElement = null;
        
        if (attachment.mediaType.startsWith('image/')) {
            mediaElement = document.createElement('img');
            mediaElement.src = mediaUrl;
            mediaElement.className = 'message-image';
            mediaElement.onclick = function() {
                // Используем ImageViewer вместо открытия в новой вкладке
                if (window.imageViewer) {
                    window.imageViewer.open(mediaUrl);
                } else {
                    // Запасной вариант, если ImageViewer не инициализирован
                    window.open(mediaUrl, '_blank');
                    console.warn('ImageViewer не найден. Проверьте подключение ImageViewer.js');
                }
            };
        } else if (attachment.mediaType.startsWith('video/')) {
            mediaElement = document.createElement('video');
            mediaElement.src = mediaUrl;
            mediaElement.className = 'message-video';
            mediaElement.controls = true;
        } else if (attachment.mediaType.startsWith('audio/')) {
            mediaElement = document.createElement('audio');
            mediaElement.src = mediaUrl;
            mediaElement.className = 'message-audio';
            mediaElement.controls = true;
        } else {
            // Для других типов файлов создаем ссылку для скачивания
            mediaElement = document.createElement('a');
            mediaElement.href = mediaUrl;
            mediaElement.className = 'message-file';
            mediaElement.target = '_blank';
            mediaElement.style.color = 'white';
            mediaElement.style.display = 'flex';
            mediaElement.style.alignItems = 'center';
            mediaElement.style.justifyContent = 'space-between';
            mediaElement.style.width = '100%';
            
            // Выбираем подходящую иконку в зависимости от типа файла
            let iconClass = "ri-file-line";
            
            if (attachment.mediaType.includes('pdf')) {
                iconClass = "ri-file-pdf-line";
            } else if (attachment.mediaType.includes('word') || attachment.mediaType.includes('doc')) {
                iconClass = "ri-file-word-line";
            } else if (attachment.mediaType.includes('excel') || attachment.mediaType.includes('sheet') || attachment.mediaType.includes('xls')) {
                iconClass = "ri-file-excel-line";
            } else if (attachment.mediaType.includes('text')) {
                iconClass = "ri-file-text-line";
            }
            
            // Создаем HTML-структуру с именем файла и иконкой загрузки
            mediaElement.innerHTML = `
                <div style="display: flex; align-items: center;">
                    <i class="${iconClass}" style="margin-right: 8px;"></i>
                    <span style="white-space: nowrap; overflow: hidden; text-overflow: ellipsis; max-width: 150px;">${attachment.fileName || 'Файл'}</span>
                </div>
                <i class="ri-download-line"></i>
            `;
        }
        
        return mediaElement;
    }
    
    /**
     * Загружает вложение сообщения
     * @param {Object} attachment - Информация о вложении
     * @param {HTMLElement} container - Контейнер для вложения
     * @param {boolean} showLoader - Показывать ли индикатор загрузки
     */
    static loadMessageAttachment(attachment, container, showLoader = false) {
        if (!attachment || !attachment.mediaId || !container) return;
        
        // Очищаем контейнер
        container.innerHTML = '';
        
        // Показываем индикатор загрузки, если нужно
        if (showLoader) {
            const loader = document.createElement('div');
            loader.className = 'message-attachment-loader';
            loader.innerHTML = '<i class="ri-loader-line spinning"></i>';
            container.appendChild(loader);
        }
        
        try {
            const mediaElement = this.createMediaElement(attachment);
            if (mediaElement) {
                // Убираем индикатор загрузки
                if (showLoader) container.innerHTML = '';
                
                // Добавляем медиаэлемент
                container.appendChild(mediaElement);
                
                // Для изображений добавляем обработчик ошибок
                if (mediaElement.tagName === 'IMG') {
                    mediaElement.onerror = function() {
                        // При ошибке загрузки показываем сообщение об ошибке
                        container.innerHTML = '';
                        const errorMsg = document.createElement('div');
                        errorMsg.className = 'message-attachment-error';
                        errorMsg.innerHTML = '<i class="ri-error-warning-line"></i> Не удалось загрузить изображение';
                        container.appendChild(errorMsg);
                    };
                }
            }
        } catch (error) {
            console.error('Error loading attachment:', error);
            container.innerHTML = '';
            const errorMsg = document.createElement('div');
            errorMsg.className = 'message-attachment-error';
            errorMsg.innerHTML = '<i class="ri-error-warning-line"></i> Ошибка загрузки вложения';
            container.appendChild(errorMsg);
        }
    }
    
    /**
     * Повторяет загрузку медиафайла
     * @param {number} mediaId - ID медиафайла
     * @param {HTMLElement} container - Контейнер для вложения
     * @param {Object} attachment - Информация о вложении
     */
    static retryMediaLoad(mediaId, container, attachment) {
        if (!mediaId || !container || !attachment) return;
        
        // Показываем индикатор загрузки
        container.innerHTML = '<div class="message-attachment-loader"><i class="ri-loader-line spinning"></i></div>';
        
        // Пытаемся загрузить медиафайл еще раз
        setTimeout(() => {
            this.loadMessageAttachment(attachment, container);
        }, 1000);
    }
}

/**
 * Вспомогательный класс для обработки ошибок и уведомлений
 */
class NotificationHandler {
    /**
     * Показывает уведомление пользователю
     * @param {string} message - Текст уведомления
     * @param {string} type - Тип уведомления (success, error, warning, info)
     * @param {number} duration - Продолжительность показа в мс
     */
    static showNotification(message, type = 'info', duration = 3000) {
        const notification = document.getElementById("notification");
        if (!notification) return;
        
        // Устанавливаем класс в зависимости от типа уведомления
        notification.className = "notification show";
        notification.dataset.type = type;
        
        // Устанавливаем иконку
        const icon = notification.querySelector(".notification-icon i");
        if (icon) {
            switch (type) {
                case 'success':
                    icon.className = 'ri-check-line';
                    break;
                case 'error':
                    icon.className = 'ri-error-warning-line';
                    break;
                case 'warning':
                    icon.className = 'ri-alert-line';
                    break;
                default:
                    icon.className = 'ri-information-line';
            }
        }
        
        // Устанавливаем текст уведомления
        const notificationMessage = notification.querySelector(".notification-message");
        if (notificationMessage) {
            notificationMessage.textContent = message;
        }
        
        // Показываем уведомление
        notification.classList.add("show");
        
        // Устанавливаем таймер для скрытия
        const timeout = setTimeout(() => {
            this.hideNotification(notification);
        }, duration);
        
        // Сохраняем ссылку на таймер для возможности отмены
        notification.dataset.timeout = timeout;
        
        // Добавляем обработчик клика для закрытия уведомления
        const closeBtn = notification.querySelector(".notification-close");
        if (closeBtn) {
            closeBtn.onclick = () => {
                clearTimeout(parseInt(notification.dataset.timeout));
                this.hideNotification(notification);
            };
        }
    }
    
    /**
     * Скрывает уведомление
     * @param {HTMLElement} notification - Элемент уведомления
     */
    static hideNotification(notification = null) {
        if (!notification) {
            notification = document.getElementById("notification");
        }
        
        if (notification) {
            notification.classList.remove("show");
            
            // Очищаем таймер если он существует
            if (notification.dataset.timeout) {
                clearTimeout(parseInt(notification.dataset.timeout));
                delete notification.dataset.timeout;
            }
        }
    }
    
    /**
     * Обрабатывает ошибки HTTP запросов
     * @param {Error} error - Объект ошибки
     * @param {string} context - Контекст, в котором произошла ошибка
     * @param {boolean} showNotification - Показывать ли уведомление пользователю
     */
    static handleError(error, context, showNotification = true) {
        // Форматируем сообщение об ошибке
        let errorMessage = `Ошибка: ${error.message}`;
        
        // Добавляем контекст если он указан
        if (context) {
            console.error(`Ошибка в ${context}:`, error);
            errorMessage = `Ошибка в ${context}: ${error.message}`;
        } else {
            console.error('Ошибка:', error);
        }
        
        // Показываем уведомление если нужно
        if (showNotification) {
            this.showNotification(errorMessage, 'error');
        }
        
        return errorMessage;
    }
}

// Экспорт новых классов
window.MediaHandler = MediaHandler;
window.NotificationHandler = NotificationHandler;

// Экспорт классов
window.UserAPI = UserAPI;
window.ContactAPI = ContactAPI;
window.MessageAPI = MessageAPI;
window.MediaFileAPI = MediaFileAPI;
window.MessengerUI = MessengerUI;

/**
 * Начало нового чата с выбранным пользователем
 * @param {number} selectedUserId - ID выбранного пользователя
 * @param {string} selectedUserName - Имя выбранного пользователя
 */
async function startNewChat(selectedUserId, selectedUserName) {
    try {
        const userId = localStorage.getItem("userId");
        if (!userId || !selectedUserId) {
            showNotification("Ошибка выбора пользователя");
            return;
        }

        // Закрываем модальное окно, если оно открыто
        const modal = document.getElementById('newChatModal');
        if (modal) modal.remove();

        // Очищаем поле поиска контактов
        const searchInput = document.getElementById("contactSearchInput");
        if (searchInput) {
            searchInput.value = '';
        }

        // Устанавливаем заголовок чата
        const chatTitle = document.getElementById('chatTitle');
        const chatStatus = document.getElementById('chatStatus');
        if (chatTitle) chatTitle.textContent = selectedUserName;
        if (chatStatus) chatStatus.textContent = ""; // Можно загрузить роль пользователя при необходимости

        // Очищаем активность со всех контактов
        const contacts = document.querySelectorAll(".contact-item");
        contacts.forEach(c => c.classList.remove("active"));

        // Проверяем, есть ли этот контакт уже в списке
        const existingContact = document.querySelector(`.contact-item[data-id="${selectedUserId}"]`);
        if (existingContact) {
            // Если контакт существует, делаем его активным
            existingContact.classList.add("active");
        } else {
            // Если контакта нет, создаем временный элемент и делаем его активным
            const messagesContainer = document.getElementById("messagesContainer");
            if (messagesContainer) {
                messagesContainer.innerHTML = `
                    <div class="empty-messages">
                        <p>У вас пока нет сообщений с этим пользователем</p>
                        <p>Отправьте сообщение, чтобы начать общение</p>
                    </div>
                `;
            }
            
            // Создаем временный элемент контакта
            const tempContact = document.createElement("div");
            tempContact.className = "contact-item active";
            tempContact.dataset.id = selectedUserId;
            tempContact.style.display = "none"; // Делаем его скрытым, но активным для функционала
            
            const contactsList = document.getElementById("contactsList");
            if (contactsList) {
                contactsList.appendChild(tempContact);
            }
        }

        // Фокусируем поле ввода сообщения
        const messageTextArea = document.getElementById("messageTextArea");
        if (messageTextArea) {
            messageTextArea.focus();
            // Сохраняем ID получателя как атрибут данных для отправки сообщения
            messageTextArea.dataset.receiverId = selectedUserId;
        }

        // Обновляем список контактов
        await loadContacts(userId);

        // Делаем контакт активным после обновления списка
        setTimeout(() => {
            const updatedContact = document.querySelector(`.contact-item[data-id="${selectedUserId}"]`);
            if (updatedContact) {
                updatedContact.classList.add("active");
            }
        }, 500);

        // Показываем уведомление
        showNotification(`Чат с ${selectedUserName} создан`);

    } catch (error) {
        console.error("Ошибка при создании нового чата:", error);
        showNotification("Не удалось создать новый чат");
    }
}

/**
 * Класс MessengerAPI - фасад для работы с API мессенджера
 */
class MessengerAPI {
    constructor() {
        this.currentUserId = UserAPI.getCurrentUserId();
    }
    
    /**
     * Получить ID текущего пользователя
     * @returns {string} ID пользователя
     */
    getCurrentUserId() {
        return this.currentUserId;
    }
    
    /**
     * Прикрепляет изображение к форме ввода сообщения
     * @param {string} imageDataUrl - Данные изображения в формате base64
     * @returns {boolean} - Успешность операции
     */
    attachImage(imageDataUrl) {
        try {
            if (!imageDataUrl) {
                throw new Error('Нет данных изображения');
            }
            
            console.log('Attaching image to message input. Data length:', imageDataUrl.length);
            
            // Найти контейнер предпросмотра вложений
            const previewContainer = document.getElementById('attachment-preview');
            if (!previewContainer) {
                throw new Error('Контейнер предпросмотра не найден');
            }
            
            // Очистить предыдущие вложения
            previewContainer.innerHTML = '';
            previewContainer.classList.add('active');
            
            // Создать элемент изображения
            const img = document.createElement('img');
            img.src = imageDataUrl;
            img.className = 'attachment-preview-image';
            
            // Создать обертку для предпросмотра
            const previewWrapper = document.createElement('div');
            previewWrapper.className = 'attachment-preview-wrapper';
            previewWrapper.appendChild(img);
            
            // Добавить информацию о файле
            const fileInfo = document.createElement('div');
            fileInfo.className = 'attachment-file-info';
            fileInfo.innerHTML = `
                <div class="attachment-file-name">Изображение</div>
                <div class="attachment-file-size">Отредактированное фото</div>
            `;
            
            previewWrapper.appendChild(fileInfo);
            
            // Добавить кнопку удаления
            const removeButton = document.createElement('button');
            removeButton.className = 'remove-attachment-btn';
            removeButton.setAttribute('title', 'Удалить вложение');
            removeButton.innerHTML = '<i class="ri-close-line"></i>';
            removeButton.addEventListener('click', (e) => {
                e.stopPropagation();
                previewContainer.innerHTML = '';
                previewContainer.classList.remove('active');
            });
            
            previewWrapper.appendChild(removeButton);
            previewContainer.appendChild(previewWrapper);
            
            // Сохранить ссылку на изображение в контейнере
            previewContainer.imageData = imageDataUrl;
            
            // Изменить стиль кнопки прикрепления
            const attachmentButton = document.getElementById('attachment-button');
            if (attachmentButton) {
                attachmentButton.classList.add('attachment-button-active');
            }
            
            // Добавить класс к контейнеру ввода для стилизации
            const messageInputContainer = document.querySelector('.message-input-container');
            if (messageInputContainer) {
                messageInputContainer.classList.add('has-attachment');
            }
            
            // Изменить placeholder в текстовом поле
            const messageTextArea = document.getElementById('messageTextArea');
            if (messageTextArea) {
                const originalPlaceholder = messageTextArea.getAttribute('data-original-placeholder') || 
                                          messageTextArea.placeholder;
                
                // Сохранить оригинальный placeholder, если он еще не сохранен
                if (!messageTextArea.getAttribute('data-original-placeholder')) {
                    messageTextArea.setAttribute('data-original-placeholder', originalPlaceholder);
                }
                
                messageTextArea.placeholder = 'Добавьте подпись к изображению...';
                
                // Фокусироваться на текстовом поле для удобства пользователя
                messageTextArea.focus();
            }
            
            // Запустить событие для обновления UI
            const attachmentEvent = new CustomEvent('attachment-added', {
                detail: { type: 'image', data: imageDataUrl }
            });
            document.dispatchEvent(attachmentEvent);
            
            console.log('Image attached successfully');
            return true;
        } catch (error) {
            console.error('Ошибка при прикреплении изображения:', error);
            
            // Отображение уведомления об ошибке
            if (typeof MessengerUI !== 'undefined' && MessengerUI.showNotification) {
                MessengerUI.showNotification('Ошибка при прикреплении изображения: ' + error.message, 'error');
            } else if (typeof showNotification === 'function') {
                showNotification('Ошибка при прикреплении изображения');
            }
            
            return false;
        }
    }
} 