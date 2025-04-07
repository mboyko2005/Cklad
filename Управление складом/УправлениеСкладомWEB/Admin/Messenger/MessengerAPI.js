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

            const response = await fetch(`/api/message/delete-for-me/${messageId}`, {
                method: 'DELETE'
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

            const response = await fetch(`/api/message/delete-for-all/${messageId}`, {
                method: 'DELETE'
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
     * Создать элемент сообщения
     * @param {Object} message - Информация о сообщении
     * @param {boolean} isSender - Является ли отправителем текущий пользователь
     * @returns {HTMLElement} Созданный элемент
     */
    static createMessageElement(message, isSender) {
        const messageWrapper = document.createElement("div");
        messageWrapper.className = `message-wrapper ${isSender ? "sent" : "received"}`;
        messageWrapper.dataset.id = message.messageId;
        
        const messageBubble = document.createElement("div");
        messageBubble.className = "message-bubble";
        
        // Добавление содержимого сообщения
        const messageContent = document.createElement("div");
        messageContent.className = "message-content";
        messageContent.textContent = message.text;
        
        // Добавление информации о времени
        const messageTime = document.createElement("div");
        messageTime.className = "message-time";
        
        const date = new Date(message.timestamp);
        messageTime.textContent = this.formatTime(date);
        
        // Показатель прочтения
        if (isSender && message.isRead !== undefined) {
            const readStatus = document.createElement("span");
            readStatus.className = "read-status";
            readStatus.innerHTML = message.isRead ? "✓✓" : "✓";
            messageTime.appendChild(readStatus);
        }
        
        messageBubble.appendChild(messageContent);
        messageBubble.appendChild(messageTime);
        messageWrapper.appendChild(messageBubble);
        
        // Добавляем контекстное меню
        messageWrapper.addEventListener("contextmenu", (e) => {
            e.preventDefault();
            window.showMessageContextMenu(e, message.messageId, message.text, isSender);
        });
        
        return messageWrapper;
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

// Экспорт классов
window.UserAPI = UserAPI;
window.ContactAPI = ContactAPI;
window.MessageAPI = MessageAPI;
window.MessengerUI = MessengerUI; 