document.addEventListener("DOMContentLoaded", () => {
    applyTheme();
    checkAuthorization();
    initializeEventListeners();
    
    // Добавляем класс анимации к иконке
    const animatedIcon = document.querySelector('.animated-icon');
    if (animatedIcon) {
        animatedIcon.classList.add('animate-on-entry');
    }
    
    // При загрузке показываем красивую приветственную панель с анимацией
    const messagesContainer = document.getElementById("messagesContainer");
    if (messagesContainer) {
        messagesContainer.innerHTML = `
            <div class="welcome-panel">
                <div class="welcome-icon">
                    <i class="ri-chat-smile-3-line"></i>
                </div>
                <h2>Добро пожаловать в мессенджер</h2>
                <p>Выберите контакт из списка слева или начните новый чат, чтобы начать общение</p>
                <div class="welcome-features">
                    <div class="feature-item">
                        <i class="ri-attachment-2"></i>
                        <span>Делитесь файлами</span>
                    </div>
                    <div class="feature-item">
                        <i class="ri-emotion-line"></i>
                        <span>Используйте эмодзи</span>
                    </div>
                    <div class="feature-item">
                        <i class="ri-search-line"></i>
                        <span>Ищите сообщения</span>
                    </div>
                </div>
            </div>
        `;
    }
    
    // Инициализируем переменную текущего индекса поиска
    currentSearchIndex = 0;

    // Инициализируем менеджер вложений
    window.attachmentManager = new MessengerAttachment({
        attachmentButtonId: 'attachment-button',
        fileInputId: 'file-input',
        previewContainerId: 'attachment-preview',
        messageTextAreaId: 'messageTextArea'
    });
    
    // Инициализируем менеджер эмодзи
    EmojiManager.init({
        messageTextAreaId: 'messageTextArea',
        emojiButtonId: 'emojiButton'
    });
    
    // Проверяем инициализацию ImageViewer
    if (typeof window.imageViewer === 'undefined') {
        console.warn('ImageViewer не был инициализирован. Создаем новый экземпляр.');
        window.imageViewer = new ImageViewer();
    }
    
    // Запускаем процесс автоматической проверки новых сообщений
    startMessagePolling();
});

// Переменные для контроля опроса сервера
let messagePollingInterval = null;
let lastMessageTimestamp = null;
const POLLING_INTERVAL = 5000; // Интервал проверки новых сообщений (5 сек)

/**
 * Запускает периодическую проверку новых сообщений
 */
function startMessagePolling() {
    if (messagePollingInterval) {
        clearInterval(messagePollingInterval);
    }
    
    // Устанавливаем интервал для проверки новых сообщений
    messagePollingInterval = setInterval(checkForNewMessages, POLLING_INTERVAL);
    
    // Также запускаем проверку при переключении вкладки браузера
    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') {
            checkForNewMessages();
        }
    });
    
    console.log('Автоматическое обновление чата активировано');
}

/**
 * Проверяет наличие новых сообщений для текущего контакта
 */
async function checkForNewMessages() {
    try {
        const selectedContact = document.querySelector('.contact-item.active');
        if (!selectedContact) return; // Нет выбранного контакта
        
        const userId = localStorage.getItem("userId");
        const contactId = selectedContact.dataset.id;
        
        if (!userId || !contactId) return;
        
        // Получаем последнее сообщение в чате для определения времени
        const lastMessageElement = document.querySelector('.message-wrapper:last-child');
        const lastMessageId = lastMessageElement ? lastMessageElement.dataset.id : null;
        
        // Получаем сообщения через API
        const data = await MessageAPI.getConversation(userId, contactId);
        const messages = data.messages || [];
        
        if (messages.length === 0) return;
        
        // Находим новые сообщения, которых еще нет в чате
        let hasNewMessages = false;
        const messagesContainer = document.getElementById("messagesContainer");
        
        if (lastMessageId) {
            // Ищем сообщения, которые новее последнего отображаемого
            const lastMessageIndex = messages.findIndex(msg => msg.messageId.toString() === lastMessageId);
            
            if (lastMessageIndex !== -1 && lastMessageIndex < messages.length - 1) {
                // Есть новые сообщения
                const newMessages = messages.slice(lastMessageIndex + 1);
                console.log('Обнаружены новые сообщения:', newMessages.length);
                
                // Добавляем новые сообщения в чат
                const fragment = document.createDocumentFragment();
                
                let lastDate = getLastVisibleDate();
                
                newMessages.forEach((message) => {
                    const messageDate = new Date(message.timestamp);
                    const currentDate = new Date(messageDate.getFullYear(), messageDate.getMonth(), messageDate.getDate());
                    
                    // Если дата изменилась, добавляем разделитель с датой
                    if (!lastDate || lastDate.getTime() !== currentDate.getTime()) {
                        const dateSeparator = document.createElement("div");
                        dateSeparator.className = "date-separator";
                        dateSeparator.textContent = formatDate(messageDate);
                        fragment.appendChild(dateSeparator);
                        lastDate = currentDate;
                    }
                    
                    // Определяем, отправлено ли сообщение текущим пользователем
                    const isSender = message.senderId.toString() === userId;
                    
                    // Создаем элемент сообщения и добавляем в фрагмент
                    const messageElement = MessengerUI.createMessageElement(message, isSender);
                    fragment.appendChild(messageElement);
                });
                
                // Добавляем все новые сообщения в контейнер
                messagesContainer.appendChild(fragment);
                
                // Прокручиваем вниз к новым сообщениям
                messagesContainer.scrollTop = messagesContainer.scrollHeight;
                
                hasNewMessages = true;
                
                // Отмечаем сообщения как прочитанные
                await MessageAPI.markMessagesAsRead(userId, contactId);
                
                // Обновляем статус прочтения сообщений в интерфейсе
                updateReadStatus();
            }
        } else {
            // Если чат был пустой, просто загружаем все сообщения
            await loadChatHistory(contactId);
            hasNewMessages = true;
        }
        
        // Если есть новые сообщения, обновляем список контактов для отображения последних сообщений
        if (hasNewMessages) {
            // Запоминаем ID активного контакта
            const activeContactId = selectedContact.dataset.id;
            
            // Обновляем список контактов
            await loadContacts(userId);
            
            // Восстанавливаем выбор активного контакта
            const updatedContact = document.querySelector(`.contact-item[data-id="${activeContactId}"]`);
            if (updatedContact) {
                updatedContact.classList.add("active");
            }
            
            // Проигрываем звук уведомления при получении новых сообщений
            if (!document.hasFocus()) {
                playNotificationSound();
            }
        }
    } catch (error) {
        console.error('Ошибка при проверке новых сообщений:', error);
    }
}

/**
 * Возвращает дату последнего отображаемого разделителя даты
 * @returns {Date|null} Последняя дата или null
 */
function getLastVisibleDate() {
    const dateSeparators = document.querySelectorAll('.date-separator');
    if (dateSeparators.length === 0) return null;
    
    const lastDateText = dateSeparators[dateSeparators.length - 1].textContent;
    
    if (lastDateText === 'Сегодня') {
        const today = new Date();
        return new Date(today.getFullYear(), today.getMonth(), today.getDate());
    } else if (lastDateText === 'Вчера') {
        const yesterday = new Date();
        yesterday.setDate(yesterday.getDate() - 1);
        return new Date(yesterday.getFullYear(), yesterday.getMonth(), yesterday.getDate());
    } else {
        // Парсим дату в формате ДД.ММ.ГГГГ
        const parts = lastDateText.split('.');
        if (parts.length === 3) {
            return new Date(parseInt(parts[2]), parseInt(parts[1]) - 1, parseInt(parts[0]));
        }
    }
    
    return null;
}

/**
 * Обновляет статус прочтения сообщений в интерфейсе
 */
function updateReadStatus() {
    const sentMessages = document.querySelectorAll('.message-wrapper.sent');
    sentMessages.forEach(messageEl => {
        const readStatus = messageEl.querySelector('.read-status');
        if (readStatus) {
            readStatus.innerHTML = '✓✓'; // Отмечаем как прочитанное
        }
    });
}

/**
 * Воспроизводит звук уведомления о новом сообщении
 */
function playNotificationSound() {
    // Создаем элемент звука, если он еще не существует
    let notificationSound = document.getElementById('notificationSound');
    
    if (!notificationSound) {
        notificationSound = document.createElement('audio');
        notificationSound.id = 'notificationSound';
        notificationSound.src = '../../../assets/sounds/notification.mp3'; // Путь к звуковому файлу
        notificationSound.volume = 0.5;
        document.body.appendChild(notificationSound);
    }
    
    // Воспроизводим звук
    notificationSound.play().catch(err => {
        console.warn('Не удалось воспроизвести звук уведомления:', err);
    });
}

// Обработчик события pageshow (срабатывает при возврате на страницу)
window.addEventListener("pageshow", () => {
    applyTheme();
    // Проверяем новые сообщения при возврате на страницу
    checkForNewMessages();
});

// Останавливаем опрос сервера при закрытии страницы
window.addEventListener("beforeunload", () => {
    if (messagePollingInterval) {
        clearInterval(messagePollingInterval);
    }
});

// Функция применения темы
function applyTheme() {
    const username = localStorage.getItem("username") || "";
    const themeKey = `appTheme-${username}`;
    const savedTheme = localStorage.getItem(themeKey) || "light";
    document.documentElement.setAttribute("data-theme", savedTheme);
}

// Обработчик события изменения localStorage для мгновенного обновления темы
window.addEventListener("storage", (event) => {
    const username = localStorage.getItem("username") || "";
    const themeKey = `appTheme-${username}`;
    if (event.key === themeKey) {
        document.documentElement.setAttribute("data-theme", event.newValue);
    }
});

/** Проверяем, что пользователь авторизован */
function checkAuthorization() {
    const isAuth = localStorage.getItem("auth");
    const userRole = localStorage.getItem("role");
    if (isAuth !== "true" || !userRole) {
        window.location.href = "../../Login.html";
        return;
    }

    // Имя пользователя для работы мессенджера
    const username = localStorage.getItem("username");

    if (username) {
        const usernameEl = document.querySelector(".username");
        if (usernameEl) usernameEl.textContent = username;
    } else {
        console.error("Ошибка: Имя пользователя не найдено");
        showNotification("Ошибка авторизации. Пожалуйста, перезайдите в систему.");
        setTimeout(() => {
            window.location.href = "../../Login.html";
        }, 2000);
        return;
    }

    // После небольшой задержки показываем уведомление и загружаем контакты
    setTimeout(() => {
        showNotification(`Добро пожаловать в мессенджер, ${username}!`);
        loadUserIdAndMessages(username);
    }, 1000);
}

/** Загрузка списка контактов по логину */
async function loadUserIdAndMessages(username) {
    try {
        if (!username) {
            console.error("Ошибка: Имя пользователя отсутствует");
            MessengerUI.showNotification("Ошибка авторизации. Пожалуйста, перезайдите в систему.");
            setTimeout(() => {
                window.location.href = "../../Login.html";
            }, 2000);
            return;
        }

        console.log("Получение ID пользователя для:", username);

        // Получаем userId по логину через UserAPI
        const userId = await UserAPI.getUserIdByLogin(username);
        console.log("Получен ID пользователя:", userId);

        // Сохраняем ID пользователя для использования в дальнейших запросах
        localStorage.setItem("userId", userId);

        // Загружаем только контакты для пользователя, без загрузки всех сообщений
        await loadContacts(userId);
    } catch (error) {
        console.error("Ошибка при загрузке ID пользователя и контактов:", error);
        MessengerUI.showNotification("Ошибка при загрузке данных. Попробуйте обновить страницу.");
    }
}

/** Загрузка списка контактов */
async function loadContacts(userId) {
    try {
        // Получаем контакты через ContactAPI
        const contacts = await ContactAPI.getContacts(userId);
        
        // Очищаем и заполняем список контактов
        const contactsList = document.getElementById("contactsList");
        contactsList.innerHTML = "";
        
        if (contacts.length === 0) {
            const noContactsMessage = document.createElement("div");
            noContactsMessage.className = "no-contacts-message";
            noContactsMessage.textContent = "У вас пока нет контактов";
            contactsList.appendChild(noContactsMessage);
            return;
        }
        
        // Создаем элементы контактов, используя MessengerUI
        contacts.forEach(contact => {
            const contactItem = MessengerUI.createContactItem(contact, contactsList);
            
            // Добавляем обработчик клика для выбора контакта
            contactItem.addEventListener("click", () => {
                selectContact(contactItem);
            });
        });
        
        return contacts;
    } catch (error) {
        console.error("Ошибка при загрузке контактов:", error);
        MessengerUI.showNotification("Ошибка при загрузке контактов. Попробуйте обновить страницу.");
        return [];
    }
}

async function loadMessagesForUser(userId) {
    try {
        const response = await fetch(`/api/message/messages/${userId}`);
        if (!response.ok) {
            console.error("Ошибка загрузки сообщений:", await response.text());
            throw new Error("Ошибка загрузки сообщений");
        }

        const data = await response.json();
        if (!data.success || !data.messages) {
            throw new Error("Неверный формат ответа от сервера");
        }

        const messages = data.messages;
        const messagesContainer = document.getElementById("messagesContainer");
        messagesContainer.innerHTML = "";

        // Если сообщений нет, показываем уведомление
        if (messages.length === 0) {
            const emptyMessageDiv = document.createElement("div");
            emptyMessageDiv.className = "empty-messages";
            emptyMessageDiv.textContent = "У вас пока нет сообщений. Начните общение!";
            messagesContainer.appendChild(emptyMessageDiv);
            return;
        }

        // Группируем сообщения по дате
        const messagesByDate = {};
        messages.forEach(message => {
            const date = new Date(message.timestamp).toLocaleDateString();
            if (!messagesByDate[date]) {
                messagesByDate[date] = [];
            }
            messagesByDate[date].push(message);
        });

        // Отображаем сообщения, сгруппированные по датам
        Object.keys(messagesByDate).sort().forEach(date => {
            // Добавляем разделитель даты
            const dateDiv = document.createElement("div");
            dateDiv.className = "date-separator";
            dateDiv.textContent = date;
            messagesContainer.appendChild(dateDiv);

            // Добавляем сообщения за эту дату
            messagesByDate[date].forEach(message => {
                const messageItem = document.createElement("div");
                const currentUserId = localStorage.getItem("userId");
                const isOutgoing = message.senderId.toString() === currentUserId;
                
                messageItem.className = `message-wrapper ${isOutgoing ? 'sent' : 'received'}`;
                messageItem.dataset.id = message.messageId;
                
                // Создаем контейнер для сообщения
                const messageBubble = document.createElement("div");
                messageBubble.className = "message-bubble";
                
                // Добавляем текст сообщения
                const messageContent = document.createElement("div");
                messageContent.className = "message-content";
                messageContent.textContent = message.text; // Текст уже расшифрован на сервере
                
                // Добавляем время отправки
                const messageTime = document.createElement("div");
                messageTime.className = "message-time";
                const timeString = new Date(message.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
                messageTime.textContent = timeString;
                
                // Добавляем индикатор прочтения для исходящих сообщений
                if (isOutgoing) {
                    const readStatus = document.createElement("span");
                    readStatus.className = "read-status";
                    readStatus.innerHTML = message.isRead ? "✓✓" : "✓";
                    messageTime.appendChild(readStatus);
                }
                
                messageBubble.appendChild(messageContent);
                messageBubble.appendChild(messageTime);
                messageItem.appendChild(messageBubble);
                messagesContainer.appendChild(messageItem);
            });
        });

        // Прокручиваем к последнему сообщению
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    } catch (error) {
        console.error("Ошибка при загрузке сообщений:", error);
        showNotification("Ошибка при загрузке сообщений. Попробуйте обновить страницу.");
    }
}

/** Вешаем обработчики событий на элементы */
function initializeEventListeners() {
    const backBtn = document.getElementById("backBtn");
    const exitBtn = document.getElementById("exitBtn");
    const newChatBtn = document.getElementById("newChatBtn");
    const menuBtn = document.getElementById("menuBtn");
    const contactSearchInput = document.getElementById("contactSearchInput");
    const deleteBtn = document.getElementById("deleteBtn");
    const searchMessagesBtn = document.getElementById("searchMessagesBtn");
    const infoBtn = document.getElementById("infoBtn");
    const closeBtn = document.getElementById("closeBtn");
    const messageSearchInput = document.getElementById("messageSearchInput");
    const prevSearchBtn = document.getElementById("prevSearchBtn");
    const nextSearchBtn = document.getElementById("nextSearchBtn");
    const messageTextArea = document.getElementById("messageTextArea");
    const attachmentButton = document.getElementById("attachment-button");
    const emojiButton = document.getElementById("emojiButton");
    const sendButton = document.getElementById("send-button");
    const removeAttachmentBtn = document.getElementById("removeAttachmentBtn");

    if (backBtn) {
        backBtn.addEventListener("click", () => {
            window.location.href = "../Admin.html";
        });
    }

    if (exitBtn) {
        exitBtn.addEventListener("click", handleExit);
    }

    if (newChatBtn) {
        newChatBtn.addEventListener("click", handleNewChat);
    }

    if (menuBtn) {
        menuBtn.addEventListener("click", () => {
            showNotification("Меню находится в разработке");
        });
    }

    if (contactSearchInput) {
        // Удаляем старые обработчики, если есть
        contactSearchInput.removeEventListener("input", handleContactSearch);
        // Добавляем новый обработчик
        contactSearchInput.addEventListener("input", handleContactSearch);
    }

    if (deleteBtn) {
        deleteBtn.addEventListener("click", handleDeleteChat);
    }

    if (searchMessagesBtn) {
        searchMessagesBtn.addEventListener("click", toggleSearchPanel);
    }

    if (infoBtn) {
        infoBtn.addEventListener("click", () => {
            showNotification("Функция информации о контакте находится в разработке");
        });
    }

    if (closeBtn) {
        closeBtn.addEventListener("click", () => {
            window.location.href = "../Admin.html";
        });
    }

    if (messageSearchInput) {
        messageSearchInput.addEventListener("input", handleMessageSearch);
        messageSearchInput.addEventListener("input", handleSearchMessageInput);
    }

    if (prevSearchBtn) {
        prevSearchBtn.addEventListener("click", navigateToPrevSearchResult);
    }

    if (nextSearchBtn) {
        nextSearchBtn.addEventListener("click", navigateToNextSearchResult);
    }

    if (messageTextArea) {
        messageTextArea.addEventListener("input", autoResizeTextarea);
        messageTextArea.addEventListener("keydown", handleMessageKeyDown);
    }

    if (attachmentButton) {
        attachmentButton.addEventListener("click", handleAttachment);
    }

    if (emojiButton) {
        emojiButton.addEventListener("click", handleEmojiButtonClick);
    }

    if (sendButton) {
        sendButton.addEventListener("click", sendMessage);
    }

    if (removeAttachmentBtn) {
        removeAttachmentBtn.addEventListener("click", removeAttachment);
    }

    // Инициализируем переменную текущего индекса поиска
    currentSearchIndex = 0;
}

/** Обработка выбора контакта */
async function selectContact(contactEl) {
    if (!contactEl) return;

    // Удаляем активный класс со всех контактов
    const contacts = document.querySelectorAll(".contact-item");
    contacts.forEach(c => c.classList.remove("active"));
    
    // Добавляем активный класс выбранному контакту
    contactEl.classList.add("active");

    // Получаем ID и имя выбранного контакта
    const contactId = contactEl.dataset.id;
    const contactName = contactEl.querySelector(".contact-name").textContent;
    
    // Устанавливаем заголовок чата
    const chatTitle = document.getElementById("chatTitle");
    const chatStatus = document.getElementById("chatStatus");
    if (chatTitle) chatTitle.textContent = contactName;
    if (chatStatus) chatStatus.textContent = contactEl.querySelector(".contact-role")?.textContent || "";

    // Скрываем панель поиска сообщений, если она открыта
    const searchPanel = document.getElementById("searchPanel");
    if (searchPanel) searchPanel.style.display = "none";
    
    // Загружаем историю чата для выбранного контакта
    await loadChatHistory(contactId);
    
    // Загружаем шаблоны сообщений для этого контакта
    await loadMessageTemplates(contactId);

    // Сбрасываем вложения при выборе нового контакта
    if (window.attachmentManager) {
        window.attachmentManager.clearAttachments();
    }

    // Фокусируем поле ввода
    const messageTextArea = document.getElementById("messageTextArea");
    if (messageTextArea) {
            messageTextArea.focus();
    }
}

/** Загрузка истории чата */
async function loadChatHistory(contactId) {
    try {
        const userId = localStorage.getItem("userId");
        if (!userId) {
            throw new Error("Ошибка авторизации");
        }

        // Отмечаем сообщения как прочитанные
        try {
            await MessageAPI.markMessagesAsRead(userId, contactId);
        } catch (err) {
            console.warn("Не удалось обновить статус прочтения:", err);
            // Продолжаем работу даже при ошибке
        }

        // Загружаем историю переписки
        const data = await MessageAPI.getConversation(userId, contactId);
        const messages = data.messages || [];
        const messagesContainer = document.getElementById("messagesContainer");
        messagesContainer.innerHTML = "";

        // Если сообщений нет, показываем соответствующее сообщение
        if (messages.length === 0) {
            const emptyMessageDiv = document.createElement("div");
            emptyMessageDiv.className = "empty-messages";
            emptyMessageDiv.textContent = "У вас пока нет сообщений с этим контактом";
            messagesContainer.appendChild(emptyMessageDiv);
            return;
        }

        let lastDate = null;
        messages.forEach((message) => {
            const messageDate = new Date(message.timestamp);
            const currentDate = new Date(messageDate.getFullYear(), messageDate.getMonth(), messageDate.getDate());
            
            // Если дата изменилась, добавляем разделитель с датой
            if (!lastDate || lastDate.getTime() !== currentDate.getTime()) {
                const dateSeparator = document.createElement("div");
                dateSeparator.className = "date-separator";
                dateSeparator.textContent = formatDate(messageDate);
                messagesContainer.appendChild(dateSeparator);
                lastDate = currentDate;
            }
            
            // Определяем, отправлено ли сообщение текущим пользователем
            const isSender = message.senderId.toString() === userId;
            
            // Создаем элемент сообщения и добавляем в контейнер
            const messageElement = MessengerUI.createMessageElement(message, isSender);
            messagesContainer.appendChild(messageElement);
        });
        
        // Прокручиваем контейнер вниз
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    } catch (error) {
        console.error("Ошибка при загрузке сообщений:", error);
        const messagesContainer = document.getElementById("messagesContainer");
        messagesContainer.innerHTML = `
            <div class="error-message">
                <i class="ri-error-warning-line"></i>
                <p>Ошибка при загрузке сообщений. Пожалуйста, попробуйте позже.</p>
            </div>
        `;
    }
}

function createMessageElement(message, isSender) {
    const messageWrapper = document.createElement('div');
    messageWrapper.className = `message-wrapper ${isSender ? 'sent' : 'received'}`;
    messageWrapper.dataset.messageId = message.messageId;

    const messageBubble = document.createElement('div');
    messageBubble.className = 'message-bubble';

    const messageContent = document.createElement('div');
    messageContent.className = 'message-content';

    // Если есть вложение, добавляем его перед текстом
    if (message.attachment) {
        const attachmentContainer = document.createElement('div');
        attachmentContainer.className = 'message-attachment-container';
        
        if (message.attachment.type.startsWith('image/')) {
            const imgContainer = document.createElement('div');
            imgContainer.className = 'message-image-container';
            const img = document.createElement('img');
            img.src = message.attachment.url;
            img.alt = message.attachment.name;
            img.className = 'message-image';
            imgContainer.appendChild(img);
            attachmentContainer.appendChild(imgContainer);
        } else {
            const fileContainer = document.createElement('div');
            fileContainer.className = 'message-file';
            fileContainer.innerHTML = `
                <i class="ri-file-line"></i>
                <div class="file-info">
                    <span class="file-name">${message.attachment.name}</span>
                    <span class="file-size">${formatFileSize(message.attachment.size)}</span>
                </div>
            `;
            attachmentContainer.appendChild(fileContainer);
        }
        messageContent.appendChild(attachmentContainer);
    }

    // Добавляем текст сообщения
    if (message.text) {
        const textElement = document.createElement('div');
        textElement.className = 'message-text';
        textElement.textContent = message.text;
        messageContent.appendChild(textElement);
    }

    const messageTime = document.createElement('div');
    messageTime.className = 'message-time';
    
    // Проверяем валидность даты
    const timestamp = message.timestamp ? new Date(message.timestamp) : new Date();
    const isValidDate = !isNaN(timestamp.getTime());
    messageTime.textContent = isValidDate ? formatTime(timestamp) : formatTime(new Date());

    messageBubble.appendChild(messageContent);
    messageBubble.appendChild(messageTime);
    messageWrapper.appendChild(messageBubble);

    return messageWrapper;
}

async function sendMessage() {
    const messageTextArea = document.getElementById('messageTextArea');
    const text = messageTextArea.value.trim();
    
    // Получаем ID текущего пользователя
    const currentUserId = localStorage.getItem('userId');
    if (!currentUserId) {
        showNotification('Ошибка: пользователь не авторизован');
        return;
    }

    // Получаем ID получателя
    let receiverId;
    
    // Проверяем есть ли receiverId в dataset текстового поля (для новых чатов)
    if (messageTextArea.dataset.receiverId) {
        receiverId = messageTextArea.dataset.receiverId;
    } else {
        // Получаем ID из активного контакта
        const selectedContactEl = document.querySelector('.contact-item.active');
        if (!selectedContactEl) {
            showNotification('Выберите контакт для отправки сообщения');
            return;
        }
        receiverId = selectedContactEl.dataset.id;
    }

    // Проверяем наличие текста или вложения
    const attachment = MessengerAttachment.getAttachment();
    let messageText = text;

    // Если нет текста, но есть вложение - используем имя файла как текст
    if (!text && attachment) {
        messageText = attachment.name;
    }

    // Если нет ни текста, ни вложения, не отправляем сообщение
    if (!messageText && !attachment) {
        return;
    }

    // Генерируем временный ID для отслеживания
    const tempId = 'temp_' + Date.now();
    
    // Сразу очищаем поле ввода
    messageTextArea.value = '';
    
    // Сразу добавляем временное сообщение в чат для мгновенной обратной связи
    const messagesContainer = document.getElementById('messagesContainer');
    
    // Создаем временное сообщение
    const tempMessage = {
        messageId: tempId,
        senderId: currentUserId,
        text: messageText,
        timestamp: new Date().toISOString(),
        isRead: false
    };
    
    // Если есть вложение, добавляем его к временному сообщению
    if (attachment) {
        tempMessage.attachment = {
            name: attachment.name,
            type: attachment.type,
            size: attachment.size,
            url: URL.createObjectURL(attachment)
        };
    }
    
    // Добавляем временное сообщение в чат
    const tempElement = createMessageElement(tempMessage, true);
    tempElement.classList.add('sending');
    messagesContainer.appendChild(tempElement);
    
    // Прокручиваем к последнему сообщению
    messagesContainer.scrollTop = messagesContainer.scrollHeight;
    
    try {
        let message;
        // Отправляем сообщение асинхронно
        if (attachment) {
            // Очищаем предпросмотр вложения в интерфейсе
            MessengerAttachment.clearAttachments();
            
            // Отправляем сообщение с вложением
            message = await MessageAPI.sendMessageWithAttachment(currentUserId, receiverId, messageText, attachment);
            
            // После успешной отправки, удаляем временный элемент
            // чтобы избежать дублирования, поскольку API создаст новый элемент
            if (tempElement && document.body.contains(tempElement)) {
                tempElement.remove();
            }
        } else {
            // Отправляем обычное текстовое сообщение
            message = await MessageAPI.sendMessage(currentUserId, receiverId, messageText);
            
            // Заменяем временное сообщение на реальное или обновляем его
            if (tempElement && document.body.contains(tempElement)) {
                tempElement.classList.remove('sending');
                tempElement.dataset.messageId = message.messageId;
            }
        }

        // Очищаем data-attribute после успешной отправки
        if (messageTextArea.dataset.receiverId) {
            delete messageTextArea.dataset.receiverId;
        }

    } catch (error) {
        console.error('Ошибка при отправке сообщения:', error);
        
        // Помечаем сообщение как ошибочное
        if (tempElement && document.body.contains(tempElement)) {
            tempElement.classList.remove('sending');
            tempElement.classList.add('error');
            
            // Добавляем кнопку для повторной отправки
            const retryButton = document.createElement('button');
            retryButton.className = 'retry-message-btn';
            retryButton.innerHTML = '<i class="ri-restart-line"></i>';
            retryButton.title = 'Повторить отправку';
            retryButton.addEventListener('click', async () => {
                // Удаляем сообщение с ошибкой
                tempElement.remove();
                // Восстанавливаем текст в поле ввода
                messageTextArea.value = messageText;
                // Если было вложение, восстанавливаем его
                if (attachment) {
                    MessengerAttachment.showFilePreview(attachment);
                }
            });
            
            tempElement.querySelector('.message-bubble').appendChild(retryButton);
        }
        
        showNotification('Ошибка при отправке сообщения');
    }
}

/** Поиск сообщений */
async function handleMessageSearch(event) {
    const searchText = document.getElementById("messageSearchInput").value.trim();
    if (!searchText) return;

    const activeContact = document.querySelector(".contact-item.active");
    if (!activeContact) {
        showNotification("Сначала выберите контакт для поиска");
        return;
    }

        const userId = localStorage.getItem("userId");
    const contactId = activeContact.dataset.id;
    const messagesContainer = document.getElementById("messagesContainer");
    const searchResults = document.getElementById("searchResults");
    searchResults.innerHTML = "";

    try {
        const response = await fetch(`/api/message/conversation/${userId}/${contactId}`);
        if (!response.ok) {
            throw new Error(`Ошибка получения сообщений: ${response.status}`);
        }

        const data = await response.json();
        if (!data.success || !data.messages) {
            throw new Error("Некорректный формат ответа от сервера");
        }

        // Фильтруем сообщения, содержащие искомый текст
        const filteredMessages = data.messages.filter(msg => 
            msg.text.toLowerCase().includes(searchText.toLowerCase())
        );

        // Обновляем счетчик результатов
        document.getElementById("searchResultsCount").textContent = 
            `${filteredMessages.length} из ${data.messages.length}`;

        if (filteredMessages.length === 0) {
            searchResults.innerHTML = "<div class='no-results'>Ничего не найдено</div>";
            return;
        }

        // Отображаем результаты поиска
        filteredMessages.forEach((message, index) => {
            const resultItem = document.createElement("div");
            resultItem.className = "search-result-item";
            resultItem.dataset.index = index;
            resultItem.dataset.messageId = message.messageId;

            const resultDate = document.createElement("div");
            resultDate.className = "search-result-date";
            
            const dateString = new Date(message.timestamp).toLocaleTimeString([], {
                hour: '2-digit',
                minute: '2-digit',
                day: 'numeric',
                month: 'short'
            });
            
            const direction = message.senderId.toString() === userId ? "Исходящее" : "Входящее";
            resultDate.textContent = `${dateString} - ${direction}`;

            const resultText = document.createElement("div");
            resultText.className = "search-result-text";
            
            // Выделяем найденный текст
            const regex = new RegExp(`(${searchText})`, 'gi');
            resultText.innerHTML = message.text.replace(regex, '<span class="highlight">$1</span>');

            resultItem.appendChild(resultDate);
            resultItem.appendChild(resultText);

            // Добавляем обработчик клика
            resultItem.addEventListener("click", () => {
                highlightSearchResult(index, filteredMessages);
            });

            searchResults.appendChild(resultItem);
        });

        // Выделяем первый результат
        if (filteredMessages.length > 0) {
            highlightSearchResult(0, filteredMessages);
        }

    } catch (error) {
        console.error("Ошибка поиска сообщений:", error);
        searchResults.innerHTML = "<div class='error-message'>Ошибка при поиске сообщений</div>";
    }
}

/** Функция выделения найденного сообщения */
function highlightSearchResult(index, results) {
    if (!results || results.length === 0) return;

    // Обновляем текущий индекс
    currentSearchIndex = index;

    // Обновляем текст счетчика
    document.getElementById("searchResultsCount").textContent = 
        `${index + 1} из ${results.length}`;
    
    // Удаляем выделение с предыдущих результатов
    const allResultItems = document.querySelectorAll(".search-result-item");
    allResultItems.forEach(item => item.classList.remove("active"));

    // Добавляем выделение текущему результату
    const currentResultItem = document.querySelector(`.search-result-item[data-index="${index}"]`);
    if (currentResultItem) {
        currentResultItem.classList.add("active");
        currentResultItem.scrollIntoView({ behavior: "smooth", block: "nearest" });
    }

    // Находим сообщение в основном контейнере
    const messageId = results[index].messageId;
    const messageElement = document.querySelector(`.message-wrapper[data-id="${messageId}"]`);
    
    if (messageElement) {
        // Удаляем предыдущие выделения
        const previousHighlighted = document.querySelectorAll(".message-wrapper.highlight");
        previousHighlighted.forEach(el => el.classList.remove("highlight"));
        
        // Добавляем выделение текущему сообщению
    messageElement.classList.add("highlight");
        
        // Прокручиваем к сообщению
        messageElement.scrollIntoView({ behavior: "smooth", block: "center" });
    }
}

/** Переход к предыдущему результату поиска */
function navigateToPrevSearchResult() {
    const searchResults = document.querySelectorAll(".search-result-item");
    if (searchResults.length === 0) return;

    const newIndex = (currentSearchIndex - 1 + searchResults.length) % searchResults.length;
    
    // Получаем массив найденных сообщений
    const messages = [];
    searchResults.forEach(item => {
        messages.push({
            messageId: item.dataset.messageId
        });
    });
    
    highlightSearchResult(newIndex, messages);
}

/** Переход к следующему результату поиска */
function navigateToNextSearchResult() {
    const searchResults = document.querySelectorAll(".search-result-item");
    if (searchResults.length === 0) return;

    const newIndex = (currentSearchIndex + 1) % searchResults.length;
    
    // Получаем массив найденных сообщений
    const messages = [];
    searchResults.forEach(item => {
        messages.push({
            messageId: item.dataset.messageId
        });
    });
    
    highlightSearchResult(newIndex, messages);
}

/** Показать/скрыть панель поиска */
function toggleSearchPanel() {
    const searchPanel = document.getElementById("searchPanel");
    if (!searchPanel) return;
    
    // Если панель уже активна, скрываем ее
    if (searchPanel.classList.contains("active")) {
        searchPanel.classList.remove("active");
        searchPanel.style.display = "none";
        return;
    }
    
    // Показываем панель поиска
    searchPanel.classList.add("active");
    searchPanel.style.display = "block";
    
    // Очищаем предыдущие результаты
    document.getElementById("searchResults").innerHTML = "";
    document.getElementById("searchResultsCount").textContent = "0 из 0";
    document.getElementById("messageSearchInput").value = "";
    
    // Фокусируем на поле ввода
    document.getElementById("messageSearchInput").focus();
}

/** Удаление сообщения */
async function deleteMessage(messageId) {
    try {
        const userId = localStorage.getItem("userId");
        const response = await fetch(`/api/messenger/message/${messageId}?userId=${userId}`, {
            method: "DELETE"
        });

        if (!response.ok) {
            const errorData = await response.json();
            console.error("Ошибка API:", errorData);
            throw new Error("Ошибка удаления сообщения: " + response.status);
        }

        const messageElement = document.querySelector(`.message-wrapper[data-id="${messageId}"]`);
        if (messageElement) {
            messageElement.remove();
        }

    } catch (error) {
        console.error("Ошибка при удалении сообщения:", error);
        showNotification("Ошибка при удалении сообщения");
    }
}

/** Удаляем данные авторизации и перенаправляем на страницу входа */
function handleExit() {
    localStorage.removeItem("auth");
    localStorage.removeItem("role");
    localStorage.removeItem("username");
    localStorage.removeItem("userId");
    window.location.href = "../../Login.html";
}

/** Показать уведомление (toast) */
function showNotification(message) {
    MessengerUI.showNotification(message);
}

/** Скрыть уведомление */
function hideNotification() {
    MessengerUI.hideNotification();
}

// CSS для эффекта выделения при поиске
document.addEventListener("DOMContentLoaded", () => {
    const style = document.createElement("style");
    style.textContent = `
    .message-wrapper.highlight .message-bubble {
      animation: highlightPulse 2s ease;
    }
    @keyframes highlightPulse {
      0%, 100% { box-shadow: none; }
      50% { box-shadow: 0 0 15px rgba(58, 123, 213, 0.5); }
    }
  `;
    document.head.appendChild(style);
});

/** Загрузка шаблонов сообщений */
async function loadMessageTemplates(contactId) {
    try {
        const userId = localStorage.getItem("userId");
        if (!userId) return;

        const suggestionsContainer = document.getElementById("suggestionsContainer");
        if (!suggestionsContainer) return;

        suggestionsContainer.innerHTML = "";

        try {
            // Получаем шаблоны через MessageAPI
            const templates = await MessageAPI.getMessageTemplates(userId, contactId);
            
                if (templates && templates.length > 0) {
                    console.log("Загружено шаблонов:", templates.length);
                    templates.forEach(template => {
                        const templateBtn = document.createElement("button");
                        templateBtn.className = "suggestion-chip";
                        templateBtn.textContent = template.Text || template.text;
                        templateBtn.addEventListener("click", () => {
                            const messageTextArea = document.getElementById("messageTextArea");
                            if (messageTextArea) {
                                messageTextArea.value = templateBtn.textContent;
                                messageTextArea.focus();
                                messageTextArea.style.height = "auto";
                                messageTextArea.style.height = Math.min(messageTextArea.scrollHeight, 120) + "px";
                            }
                        });
                        suggestionsContainer.appendChild(templateBtn);
                    });
                    return;
                }
        } catch (err) {
            console.warn("Ошибка загрузки шаблонов сообщений:", err);
        }

        // Если шаблонов нет или произошла ошибка, добавляем стандартные шаблоны
        const defaultTemplates = [
            "Добрый день!",
            "Спасибо за информацию.",
            "Хорошего дня!",
            "С уважением."
        ];

        defaultTemplates.forEach(text => {
            const templateBtn = document.createElement("button");
            templateBtn.className = "suggestion-chip";
            templateBtn.textContent = text;
            templateBtn.addEventListener("click", () => {
                const messageTextArea = document.getElementById("messageTextArea");
                if (messageTextArea) {
                    messageTextArea.value = templateBtn.textContent;
                    messageTextArea.focus();
                    messageTextArea.style.height = "auto";
                    messageTextArea.style.height = Math.min(messageTextArea.scrollHeight, 120) + "px";
                }
            });
            suggestionsContainer.appendChild(templateBtn);
        });
    } catch (error) {
        console.error("Ошибка при загрузке шаблонов сообщений:", error);
    }
}

/** Поиск контактов */
async function handleContactSearch(event) {
    try {
        const searchQuery = event.target.value.trim();
        const userId = localStorage.getItem("userId");
        const contactsContainer = document.getElementById("contactsList");
        
        if (!userId) {
            MessengerUI.showNotification("Ошибка авторизации. Пожалуйста, перезайдите в систему.");
            return;
        }
        
        contactsContainer.innerHTML = "<div class='loading-message'>Поиск...</div>";
        
        if (!searchQuery) {
            // Если поле поиска пустое, возвращаем все контакты
            await loadContacts(userId);
            return;
        }
        
        try {
            // Сначала ищем среди существующих контактов
            const contacts = await ContactAPI.getContacts(userId, searchQuery);
            contactsContainer.innerHTML = "";
            
            if (contacts && contacts.length > 0) {
                contacts.forEach(contact => {
                    const contactItem = MessengerUI.createContactItem(contact, contactsContainer);
                    contactItem.addEventListener("click", () => {
                        selectContact(contactItem);
                    });
                });
                return;
            }
            
            // Если контакты не найдены, ищем среди всех пользователей
            const users = await UserAPI.searchUsers(searchQuery);
            
            if (!users || users.length === 0) {
                contactsContainer.innerHTML = "<div class='no-results'>Ничего не найдено</div>";
                return;
            }
            
            // Фильтруем пользователей, исключая текущего пользователя
            const filteredUsers = users.filter(user => user.id.toString() !== userId);
            
            if (filteredUsers.length === 0) {
                contactsContainer.innerHTML = "<div class='no-results'>Ничего не найдено</div>";
                return;
            }
            
            // Отображаем найденных пользователей
            filteredUsers.forEach(user => {
                const contactItem = document.createElement("div");
                contactItem.className = "contact-item search-result";
                contactItem.dataset.id = user.id;

                const contactAvatar = document.createElement("div");
                contactAvatar.className = "contact-avatar";
                const avatarIcon = document.createElement("i");
                avatarIcon.className = "ri-user-line";
                contactAvatar.appendChild(avatarIcon);

                const contactInfo = document.createElement("div");
                contactInfo.className = "contact-info";

                const contactName = document.createElement("div");
                contactName.className = "contact-name";
                contactName.textContent = user.login;

                const contactRole = document.createElement("div");
                contactRole.className = "contact-role";
                contactRole.textContent = user.role;

                contactInfo.appendChild(contactName);
                contactInfo.appendChild(contactRole);

                contactItem.appendChild(contactAvatar);
                contactItem.appendChild(contactInfo);

                // Добавляем обработчик клика для создания нового чата с этим пользователем
                contactItem.addEventListener("click", () => {
                    startNewChat(user.id, user.login);
                });

                contactsContainer.appendChild(contactItem);
            });
            
        } catch (error) {
            console.error("Ошибка поиска:", error);
            contactsContainer.innerHTML = "<div class='error-message'>Ошибка поиска</div>";
            
            // Если произошла ошибка, пробуем восстановить список контактов
            setTimeout(() => {
                loadContacts(userId);
            }, 3000);
        }
    } catch (error) {
        console.error("Ошибка в функции поиска контактов:", error);
    }
}

/** Обработка прикрепления файла */
function handleAttachment() {
    const fileInput = document.getElementById('file-input');
    const file = fileInput.files[0];
    
    if (file) {
        // Сразу показываем предпросмотр
        showFilePreview(file);
        
        // Если это изображение, сразу добавляем его в чат
        if (file.type.startsWith('image/')) {
            const reader = new FileReader();
            reader.onload = function(e) {
                const messagesContainer = document.getElementById('messagesContainer');
                const messageElement = document.createElement('div');
                messageElement.className = 'message-wrapper sent';
                
                const currentTime = new Date();
                const formattedTime = formatTime(currentTime);
                
                messageElement.innerHTML = `
                    <div class="message-bubble">
                        <div class="message-content">
                            <div class="message-image-container">
                                <img src="${e.target.result}" alt="${file.name}" class="message-image">
                            </div>
                        </div>
                        <div class="message-time">${formattedTime}</div>
                    </div>
                `;
                messagesContainer.appendChild(messageElement);
                messagesContainer.scrollTop = messagesContainer.scrollHeight;
            };
            reader.readAsDataURL(file);
        }
    }
}

/**
 * Показывает предпросмотр выбранного файла
 * @param {File} file - Файл для предпросмотра
 */
function showFilePreview(file) {
    if (!file) return;
    
    // Используем метод из класса MessengerAttachment
    MessengerAttachment.showFilePreview(file);
}

/** Удаление вложения */
function removeAttachment() {
    if (window.attachmentManager) {
        window.attachmentManager.clearAttachments();
    }
}

/** Обработчик нажатия на кнопку эмодзи */
function handleEmojiButtonClick() {
    // Используем метод из класса EmojiManager
    EmojiManager.toggleEmojiPicker();
}

/** Автоматически изменяет высоту textarea при вводе текста */
function autoResizeTextarea(event) {
    const textarea = event.target;
    textarea.style.height = 'auto';
    textarea.style.height = Math.min(textarea.scrollHeight, 120) + 'px';
}

/**
 * Обработчик события нажатия клавиш в поле ввода сообщения
 */
async function handleMessageKeyDown(event) {
    // Отправка сообщения по Enter (без Shift)
    if (event.key === 'Enter' && !event.shiftKey) {
        event.preventDefault();
        await sendMessage();
    }
}

/**
 * Обработка редактирования сообщения
 * @param {HTMLTextAreaElement} messageTextArea - Текстовое поле с сообщением
 */
async function handleEditMessage(messageTextArea) {
    try {
        const messageId = messageTextArea.dataset.editingMessageId;
        const newText = messageTextArea.value.trim();
        const inputContainer = document.querySelector('.input-container');
        
        if (!messageId || !newText) {
            return;
        }
        
        // Отправляем запрос на редактирование сообщения
        const result = await MessageAPI.editMessage(messageId, newText);
        
        if (result && result.success) {
            // Обновляем текст сообщения в DOM
            const messageElement = document.querySelector(`.message-wrapper[data-id="${messageId}"]`);
            if (messageElement) {
                const messageContent = messageElement.querySelector('.message-content');
                if (messageContent) {
                    messageContent.textContent = newText;
                    
                    // Анимация обновления
                    messageElement.classList.add('message-updated');
                    setTimeout(() => {
                        messageElement.classList.remove('message-updated');
                    }, 2000);
                }
            }
            
            // Очищаем поле ввода и сбрасываем режим редактирования
            messageTextArea.value = '';
            messageTextArea.style.height = '40px';
            delete messageTextArea.dataset.editingMessageId;
            
            // Сбрасываем стили редактирования
            if (inputContainer) {
                inputContainer.classList.remove('editing');
            }
            
            // Возвращаем иконку отправки
            const sendButton = document.getElementById('sendButton');
            if (sendButton) {
                const icon = sendButton.querySelector('i');
                if (icon) {
                    icon.className = 'ri-send-plane-fill';
                }
            }
            
            // Уведомляем об успехе
            MessengerUI.showNotification('Сообщение успешно изменено');
        } else {
            throw new Error(result?.message || 'Не удалось изменить сообщение');
        }
    } catch (error) {
        console.error('Ошибка при редактировании сообщения:', error);
        MessengerUI.showNotification('Не удалось отредактировать сообщение: ' + error.message);
    }
}

/** Обработчик клика по кнопке "Новый чат" */
async function handleNewChat() {
    try {
        const userId = localStorage.getItem("userId");
        if (!userId) {
            showNotification("Ошибка авторизации. Пожалуйста, перезайдите в систему.");
            return;
        }

        // Показываем модальное окно с выбором пользователей
        const modalHtml = `
            <div class="modal-overlay" id="newChatModal">
                <div class="modal-content">
                    <div class="modal-header">
                        <h3>Начать новый чат</h3>
                        <button class="modal-close" id="closeNewChatModal">
                            <i class="ri-close-line"></i>
                        </button>
                    </div>
                    <div class="modal-body">
                        <div class="search-container">
                            <i class="ri-search-line search-icon"></i>
                            <input type="text" id="newChatUserSearch" class="search-input" placeholder="Поиск пользователя...">
                        </div>
                        <div class="users-list" id="usersList">
                            <div class="loading-message">Загрузка пользователей...</div>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Добавляем HTML модального окна в документ
        const modalContainer = document.createElement('div');
        modalContainer.innerHTML = modalHtml;
        document.body.appendChild(modalContainer.firstElementChild);

        // Добавляем обработчики событий на модальное окно
        document.getElementById('closeNewChatModal').addEventListener('click', () => {
            const modal = document.getElementById('newChatModal');
            if (modal) modal.remove();
        });

        document.getElementById('newChatUserSearch').addEventListener('input', async (e) => {
            await loadUsers(e.target.value);
        });

        // Загружаем список пользователей
        await loadUsers();

    } catch (error) {
        console.error("Ошибка при создании нового чата:", error);
        showNotification("Ошибка при создании нового чата");
    }
}

/** Загрузка списка всех пользователей для создания нового чата */
async function loadUsers(searchQuery = "") {
    try {
        const userId = localStorage.getItem("userId");
        if (!userId) return;

        const usersList = document.getElementById("usersList");
        if (!usersList) return;

        // Формируем URL с параметрами поиска, если они есть
        let url = `/api/user/list`;
        if (searchQuery) {
            url += `?search=${encodeURIComponent(searchQuery)}`;
        }

        const response = await fetch(url);
        if (!response.ok) {
            throw new Error(`Ошибка при загрузке пользователей: ${response.status}`);
        }

        const data = await response.json();
        if (!data.success || !data.users) {
            throw new Error("Неверный формат ответа от сервера");
        }

        // Фильтруем пользователей, исключая текущего пользователя
        const users = data.users.filter(user => user.id.toString() !== userId);

        usersList.innerHTML = "";

        if (users.length === 0) {
            usersList.innerHTML = "<div class='no-users-message'>Пользователей не найдено</div>";
            return;
        }

        // Отображаем список пользователей
        users.forEach(user => {
            const userItem = document.createElement("div");
            userItem.className = "user-item";
            userItem.dataset.id = user.id;

            const userAvatar = document.createElement("div");
            userAvatar.className = "user-avatar";
            const avatarIcon = document.createElement("i");
            avatarIcon.className = "ri-user-line";
            userAvatar.appendChild(avatarIcon);

            const userInfo = document.createElement("div");
            userInfo.className = "user-info";

            const userName = document.createElement("div");
            userName.className = "user-name";
            userName.textContent = user.login;

            const userRole = document.createElement("div");
            userRole.className = "user-role";
            userRole.textContent = user.role;

            userInfo.appendChild(userName);
            userInfo.appendChild(userRole);

            userItem.appendChild(userAvatar);
            userItem.appendChild(userInfo);

            // Добавляем обработчик клика для выбора пользователя
            userItem.addEventListener("click", () => {
                startNewChat(user.id, user.login);
            });

            usersList.appendChild(userItem);
        });

    } catch (error) {
        console.error("Ошибка при загрузке списка пользователей:", error);
        const usersList = document.getElementById("usersList");
        if (usersList) {
            usersList.innerHTML = "<div class='error-message'>Ошибка при загрузке пользователей</div>";
        }
    }
}

/** Начало нового чата с выбранным пользователем */
async function startNewChat(selectedUserId, selectedUserName) {
    try {
        const userId = localStorage.getItem("userId");
        if (!userId || !selectedUserId) {
            showNotification("Ошибка выбора пользователя");
            return;
        }

        console.log("Начинаем новый чат с:", selectedUserName, "ID:", selectedUserId);

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
            console.log("Найден существующий контакт, активируем его");
            existingContact.classList.add("active");
            
            // Загружаем историю чата
            await loadChatHistory(selectedUserId);
        } else {
            console.log("Создаем временный контакт");
            // Если контакта нет, создаем временный элемент и делаем его активным
            const contactsList = document.getElementById("contactsList");
            if (contactsList) {
                // Создаем новый элемент контакта
                const tempContact = document.createElement("div");
                tempContact.className = "contact-item active";
                tempContact.dataset.id = selectedUserId;
                
                const contactAvatar = document.createElement("div");
                contactAvatar.className = "contact-avatar";
                contactAvatar.innerHTML = '<i class="ri-user-line"></i>';
                
                const contactInfo = document.createElement("div");
                contactInfo.className = "contact-info";
                contactInfo.innerHTML = `
                    <div class="contact-name">${selectedUserName}</div>
                    <div class="contact-role">Новый чат</div>
                `;
                
                tempContact.appendChild(contactAvatar);
                tempContact.appendChild(contactInfo);
                
                // Добавляем обработчик клика
                tempContact.addEventListener("click", () => {
                    selectContact(tempContact);
                });
                
                // Добавляем временный контакт в начало списка
                if (contactsList.firstChild) {
                    contactsList.insertBefore(tempContact, contactsList.firstChild);
                } else {
                    contactsList.appendChild(tempContact);
                }
            }

            // Показываем пустой чат
            const messagesContainer = document.getElementById("messagesContainer");
            if (messagesContainer) {
                messagesContainer.innerHTML = `
                    <div class="empty-messages">
                        <p>У вас пока нет сообщений с этим пользователем</p>
                        <p>Отправьте сообщение, чтобы начать общение</p>
                    </div>
                `;
            }
        }

        // Фокусируем поле ввода сообщения
        const messageTextArea = document.getElementById("messageTextArea");
        if (messageTextArea) {
            // Сохраняем ID получателя как атрибут данных для отправки сообщения
            messageTextArea.dataset.receiverId = selectedUserId;
            // Устанавливаем фокус
            messageTextArea.focus();
            console.log("Установлен dataset.receiverId:", selectedUserId);
        }

        // Показываем уведомление
        showNotification(`Чат с ${selectedUserName} создан`);

    } catch (error) {
        console.error("Ошибка при создании нового чата:", error);
        showNotification("Не удалось создать новый чат");
    }
}

// Объявляем переменные в глобальной области видимости
let currentSearchIndex = 0;

async function handleDeleteChat() {
    const selectedContact = document.querySelector('.contact-item.active');
    if (!selectedContact) {
        MessengerUI.showNotification("Выберите чат для удаления");
        return;
    }

    const contactId = selectedContact.dataset.id;
    const contactName = selectedContact.querySelector('.contact-name').textContent;

    if (confirm(`Вы действительно хотите удалить переписку с ${contactName}?`)) {
        try {
            const userId = localStorage.getItem("userId");
            if (!userId) {
                throw new Error("Ошибка авторизации");
            }

            // Удаляем переписку через MessageAPI
            await MessageAPI.deleteConversation(userId, contactId);

            // Удаляем чат из списка
            selectedContact.remove();
            
            // Очищаем область сообщений
            const messagesContainer = document.getElementById('messagesContainer');
            if (messagesContainer) {
                messagesContainer.innerHTML = '';
            }

            // Сбрасываем заголовок чата
            const chatTitle = document.getElementById('chatTitle');
            const chatStatus = document.getElementById('chatStatus');
            if (chatTitle) chatTitle.textContent = "Выберите контакт";
            if (chatStatus) chatStatus.textContent = "";

            MessengerUI.showNotification("Переписка успешно удалена");
        } catch (error) {
            console.error("Ошибка при удалении переписки:", error);
            MessengerUI.showNotification("Не удалось удалить переписку");
        }
    }
}

let searchHideTimer;

function handleSearchMessageInput(event) {
    const searchPanel = document.getElementById('searchPanel');
    const searchInput = event.target;
    
    // Очищаем предыдущий таймер
    if (searchHideTimer) {
        clearTimeout(searchHideTimer);
    }

    // Если поле пустое, запускаем таймер на скрытие
    if (!searchInput.value.trim()) {
        searchHideTimer = setTimeout(() => {
            searchPanel.style.display = 'none';
            searchInput.value = '';
            const searchResultsCount = document.getElementById('searchResultsCount');
            if (searchResultsCount) {
                searchResultsCount.textContent = '0 из 0';
            }
        }, 3000);
    }
}

function showMessageContextMenu(event, messageId, messageText, isSender) {
        event.preventDefault();

    // Удаляем предыдущее контекстное меню, если оно есть
    const oldMenu = document.querySelector('.context-menu');
    if (oldMenu) {
        oldMenu.remove();
    }

    // Создаем контекстное меню
    const contextMenu = document.createElement('div');
    contextMenu.className = 'context-menu';
    contextMenu.style.position = 'fixed';
    contextMenu.style.left = `${event.clientX}px`;
    contextMenu.style.top = `${event.clientY}px`;

    // Добавляем пункты меню
    if (isSender) {
        const editButton = document.createElement('button');
        editButton.textContent = 'Изменить';
        editButton.onclick = () => {
            editMessage(messageId, messageText);
            contextMenu.remove();
        };
        contextMenu.appendChild(editButton);
    }

    const deleteForMeButton = document.createElement('button');
    deleteForMeButton.textContent = 'Удалить для себя';
    deleteForMeButton.onclick = () => {
        deleteMessageForMe(messageId);
        contextMenu.remove();
    };
    contextMenu.appendChild(deleteForMeButton);

    if (isSender) {
        const deleteForAllButton = document.createElement('button');
        deleteForAllButton.textContent = 'Удалить для всех';
        deleteForAllButton.onclick = () => {
            deleteMessageForAll(messageId);
            contextMenu.remove();
        };
        contextMenu.appendChild(deleteForAllButton);
    }

    // Добавляем меню в документ
    document.body.appendChild(contextMenu);

    // Закрываем меню при клике вне его
    document.addEventListener('click', function closeMenu(e) {
        if (!contextMenu.contains(e.target)) {
            contextMenu.remove();
            document.removeEventListener('click', closeMenu);
        }
    });
}

// Функции для работы с сообщениями
async function editMessage(messageId, messageText) {
    try {
        const messageInput = document.getElementById('messageTextArea');
        const inputContainer = document.querySelector('.input-container');
        
        if (messageInput) {
            // Устанавливаем текст сообщения в поле ввода
            messageInput.value = messageText;
            messageInput.dataset.editingMessageId = messageId;
            messageInput.focus();
            
            // Авторесайз поля ввода
            messageInput.style.height = "auto";
            messageInput.style.height = Math.min(messageInput.scrollHeight, 120) + "px";
            
            // Добавляем индикатор редактирования
            if (inputContainer) {
                inputContainer.classList.add('editing');
            }
            
            // Показываем уведомление
            MessengerUI.showNotification('Редактирование сообщения');
            
            // Меняем внешний вид кнопки "Отправить"
            const sendButton = document.getElementById('sendButton');
            if (sendButton) {
                const icon = sendButton.querySelector('i');
                if (icon) {
                    icon.className = 'ri-pencil-line'; // Меняем иконку на карандаш
                }
            }
        }
    } catch (error) {
        console.error('Ошибка при редактировании сообщения:', error);
        MessengerUI.showNotification('Не удалось отредактировать сообщение');
    }
}

async function deleteMessageForMe(messageId) {
    try {
        if (!messageId) {
            MessengerUI.showNotification('Ошибка: ID сообщения не указан');
            return;
        }

        // Удаляем сообщение для текущего пользователя через API
        const result = await MessageAPI.deleteMessageForMe(messageId);
        
        if (result && result.success) {
            // Удаляем элемент сообщения из интерфейса
            const messageElement = document.querySelector(`.message-wrapper[data-id="${messageId}"]`);
            if (messageElement) {
                messageElement.remove();
            }
            MessengerUI.showNotification('Сообщение удалено');
        } else {
            throw new Error(result?.message || 'Неизвестная ошибка при удалении сообщения');
        }
    } catch (error) {
        console.error('Ошибка при удалении сообщения:', error);
        MessengerUI.showNotification('Не удалось удалить сообщение: ' + error.message);
    }
}

async function deleteMessageForAll(messageId) {
    try {
        if (!messageId) {
            MessengerUI.showNotification('Ошибка: ID сообщения не указан');
            return;
        }

        // Запрос подтверждения перед удалением для всех
        if (!confirm('Вы уверены, что хотите удалить сообщение для всех участников?')) {
            return;
        }

        // Удаляем сообщение для всех пользователей через API
        const result = await MessageAPI.deleteMessageForAll(messageId);
        
        if (result && result.success) {
            // Удаляем элемент сообщения из интерфейса
            const messageElement = document.querySelector(`.message-wrapper[data-id="${messageId}"]`);
            if (messageElement) {
                messageElement.remove();
            }
            MessengerUI.showNotification('Сообщение удалено для всех');
        } else {
            throw new Error(result?.message || 'Неизвестная ошибка при удалении сообщения');
        }
    } catch (error) {
        console.error('Ошибка при удалении сообщения:', error);
        MessengerUI.showNotification('Не удалось удалить сообщение: ' + error.message);
    }
}

// В функции createMessageElement добавляем обработчик контекстного меню:
messageElement.addEventListener('contextmenu', (e) => {
    showMessageContextMenu(e, messageId, messageText, isSender);
});

// Форматирование даты
function formatDate(date) {
    // Убедимся, что мы работаем с объектом Date
    if (!(date instanceof Date)) {
        date = new Date(date);
    }
    
    // Проверяем валидность даты
    if (isNaN(date.getTime())) {
        console.error("Неверный формат даты:", date);
        return "Неизвестная дата";
    }
    
    const today = new Date();
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);
    
    // Преобразуем к началу дня для корректного сравнения
    const todayDate = new Date(today.getFullYear(), today.getMonth(), today.getDate());
    const yesterdayDate = new Date(yesterday.getFullYear(), yesterday.getMonth(), yesterday.getDate());
    const messageDate = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    
    // Отладочная информация
    console.log("Сравнение дат:", {
        "Оригинальная дата": date.toISOString(),
        "Дата сообщения": messageDate.toISOString(),
        "Сегодня": todayDate.toISOString(),
        "Вчера": yesterdayDate.toISOString(),
        "Совпадение с сегодня": messageDate.getTime() === todayDate.getTime(),
        "Совпадение с вчера": messageDate.getTime() === yesterdayDate.getTime()
    });
    
    if (messageDate.getTime() === todayDate.getTime()) {
        return "Сегодня";
    } else if (messageDate.getTime() === yesterdayDate.getTime()) {
        return "Вчера";
    } else {
        // Форматируем дату в российском формате: ДД.ММ.ГГГГ
        return date.toLocaleDateString('ru-RU', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        });
    }
}

// Форматирование времени
function formatTime(date) {
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

/**
 * Создает и добавляет вложение к сообщению
 * @param {Object} message - Объект сообщения
 * @param {HTMLElement} messageContent - Элемент с контентом сообщения
 */
function createMessageAttachment(message, messageContent) {
    if (!message.hasAttachment) return;
    
    // Используем класс MessengerAttachment для загрузки и отображения вложения
    MessengerAttachment.loadMessageAttachment(message.messageId, messageContent);
}

/**
 * Добавляет индикатор загрузки вложения в UI
 * @param {string} id - Уникальный идентификатор вложения
 * @param {string} fileName - Имя файла
 */
function addAttachmentLoadingIndicator(id, fileName) {
    const messagesContainer = document.getElementById("messagesContainer");
    const lastMessage = messagesContainer.lastElementChild;
    
    const attachmentContainer = document.createElement("div");
    attachmentContainer.className = "message-attachment loading";
    attachmentContainer.id = id;
    
    const fileNameElement = document.createElement("div");
    fileNameElement.className = "file-name";
    fileNameElement.textContent = fileName;
    
    const progressContainer = document.createElement("div");
    progressContainer.className = "progress-container";
    
    const progressBar = document.createElement("div");
    progressBar.className = "progress-bar";
    progressBar.style.width = "0%";
    
    const progressText = document.createElement("div");
    progressText.className = "progress-text";
    progressText.textContent = "0%";
    
    progressContainer.appendChild(progressBar);
    progressContainer.appendChild(progressText);
    
    attachmentContainer.appendChild(fileNameElement);
    attachmentContainer.appendChild(progressContainer);
    
    if (lastMessage && lastMessage.classList.contains("message-outgoing")) {
        const attachmentsContainer = lastMessage.querySelector(".message-attachments");
        if (attachmentsContainer) {
            attachmentsContainer.appendChild(attachmentContainer);
        } else {
            const newAttachmentsContainer = document.createElement("div");
            newAttachmentsContainer.className = "message-attachments";
            newAttachmentsContainer.appendChild(attachmentContainer);
            lastMessage.appendChild(newAttachmentsContainer);
        }
    } else {
        // Создаем новое сообщение для отображения загрузки
        const newMessage = document.createElement("div");
        newMessage.className = "message message-outgoing";
        
        const newAttachmentsContainer = document.createElement("div");
        newAttachmentsContainer.className = "message-attachments";
        newAttachmentsContainer.appendChild(attachmentContainer);
        
        newMessage.appendChild(newAttachmentsContainer);
        messagesContainer.appendChild(newMessage);
        scrollToBottom();
    }
}

/**
 * Обновляет прогресс загрузки вложения
 * @param {string} id - Идентификатор вложения
 * @param {number} progress - Процент загрузки (0-100)
 */
function updateAttachmentProgress(id, progress) {
    const attachmentContainer = document.getElementById(id);
    if (attachmentContainer) {
        const progressBar = attachmentContainer.querySelector(".progress-bar");
        const progressText = attachmentContainer.querySelector(".progress-text");
        
        if (progressBar) {
            progressBar.style.width = `${progress}%`;
        }
        
        if (progressText) {
            progressText.textContent = `${Math.round(progress)}%`;
        }
    }
}

/**
 * Удаляет индикатор загрузки вложения
 * @param {string} id - Идентификатор вложения
 */
function removeAttachmentLoadingIndicator(id) {
    const attachmentContainer = document.getElementById(id);
    if (attachmentContainer) {
        attachmentContainer.remove();
    }
}

/**
 * Отображает ошибку загрузки вложения
 * @param {string} id - Идентификатор вложения
 * @param {string} errorMessage - Сообщение об ошибке
 */
function updateAttachmentError(id, errorMessage) {
    const attachmentContainer = document.getElementById(id);
    if (attachmentContainer) {
        attachmentContainer.classList.remove("loading");
        attachmentContainer.classList.add("error");
        
        const progressContainer = attachmentContainer.querySelector(".progress-container");
        if (progressContainer) {
            progressContainer.innerHTML = `
                <div class="error-message">${errorMessage}</div>
                <button class="retry-button">Повторить</button>
            `;
            
            const retryButton = progressContainer.querySelector(".retry-button");
            if (retryButton) {
                retryButton.addEventListener("click", () => {
                    // Логика повторной отправки будет реализована позже
                    showNotification("Функция повторной отправки в разработке");
                });
            }
        }
    }
}

// Инициализация элементов для вложений
document.addEventListener("DOMContentLoaded", function() {
    // Инициализация fileInput
    fileInput = document.getElementById("messageAttachment");
    if (fileInput) {
        fileInput.addEventListener("change", handleFileSelection);
    }
    
    // Инициализация контейнера предпросмотра вложений
    attachmentPreviewContainer = document.getElementById("attachmentPreviewContainer");
    
    // Добавление обработчика события drop для области чата
    const chatArea = document.querySelector(".chat-messages");
    if (chatArea) {
        chatArea.addEventListener("dragover", function(e) {
            e.preventDefault();
            chatArea.classList.add("drag-over");
        });
        
        chatArea.addEventListener("dragleave", function() {
            chatArea.classList.remove("drag-over");
        });
        
        chatArea.addEventListener("drop", function(e) {
            e.preventDefault();
            chatArea.classList.remove("drag-over");
            
            if (e.dataTransfer.files.length > 0) {
                fileInput.files = e.dataTransfer.files;
                handleFileSelection();
            }
        });
    }
});

// Обработка выбора файла
function handleFileSelection() {
    if (!fileInput.files.length) return;
    
    // Показать контейнер предпросмотра
    attachmentPreviewContainer.style.display = "block";
    attachmentPreviewContainer.innerHTML = "";
    
    // Создать предпросмотр для каждого файла
    Array.from(fileInput.files).forEach((file, index) => {
        const fileSize = formatFileSize(file.size);
        const fileExtension = file.name.split('.').pop().toLowerCase();
        
        const previewElement = document.createElement("div");
        previewElement.className = "attachment-preview";
        previewElement.innerHTML = `
            <div class="attachment-preview-icon">
                <i class="fas ${getFileIcon(fileExtension)}"></i>
            </div>
            <div class="attachment-preview-name">${file.name}</div>
            <div class="attachment-preview-size">${fileSize}</div>
            <div class="attachment-preview-remove" data-index="${index}">
                <i class="fas fa-times"></i>
            </div>
        `;
        
        attachmentPreviewContainer.appendChild(previewElement);
        
        // Добавить обработчик события для удаления файла
        const removeButton = previewElement.querySelector(".attachment-preview-remove");
        removeButton.addEventListener("click", function() {
            removeFileFromInput(parseInt(this.dataset.index));
        });
    });
}

// Удаление файла из input
function removeFileFromInput(index) {
    const dt = new DataTransfer();
    const files = fileInput.files;
    
    for (let i = 0; i < files.length; i++) {
        if (i !== index) {
            dt.items.add(files[i]);
        }
    }
    
    fileInput.files = dt.files;
    handleFileSelection();
    
    // Скрыть контейнер предпросмотра, если файлов не осталось
    if (fileInput.files.length === 0) {
        attachmentPreviewContainer.style.display = "none";
    }
}

// Определение иконки файла по расширению
function getFileIcon(extension) {
    const imageExtensions = ["jpg", "jpeg", "png", "gif", "bmp", "svg"];
    const documentExtensions = ["doc", "docx", "pdf", "txt", "rtf", "xlsx", "xls", "ppt", "pptx"];
    const archiveExtensions = ["zip", "rar", "7z", "tar", "gz"];
    
    if (imageExtensions.includes(extension)) {
        return "fa-file-image";
    } else if (documentExtensions.includes(extension)) {
        return "fa-file-alt";
    } else if (archiveExtensions.includes(extension)) {
        return "fa-file-archive";
    } else {
        return "fa-file";
    }
}

// Форматирование размера файла
function formatFileSize(bytes) {
    if (bytes === 0) return "0 Bytes";
    
    const k = 1024;
    const sizes = ["Bytes", "KB", "MB", "GB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i];
}

// Функция для создания HTML элемента загрузки вложения
function createAttachmentLoadingElement(file, messageId) {
    const attachmentElement = document.createElement("div");
    attachmentElement.className = "message-attachment loading";
    attachmentElement.id = `attachment-${messageId}`;
    
    const fileExtension = file.name.split('.').pop().toLowerCase();
    const fileSize = formatFileSize(file.size);
    
    attachmentElement.innerHTML = `
        <div class="file-name">
            <i class="fas ${getFileIcon(fileExtension)}"></i> ${file.name}
        </div>
        <div class="file-size">${fileSize}</div>
        <div class="progress-container">
            <div class="progress-bar" style="width: 0%"></div>
            <div class="progress-text">0%</div>
        </div>
    `;
    
    return attachmentElement;
}

// Функция для обновления прогресса загрузки
function updateAttachmentProgress(messageId, progress) {
    const attachmentElement = document.getElementById(`attachment-${messageId}`);
    if (!attachmentElement) return;
    
    const progressBar = attachmentElement.querySelector(".progress-bar");
    const progressText = attachmentElement.querySelector(".progress-text");
    
    progressBar.style.width = `${progress}%`;
    progressText.textContent = `${progress}%`;
}

// Функция для установки ошибки загрузки вложения
function setAttachmentError(messageId, errorMessage) {
    const attachmentElement = document.getElementById(`attachment-${messageId}`);
    if (!attachmentElement) return;
    
    attachmentElement.classList.add("error");
    attachmentElement.classList.remove("loading");
    
    // Заменяем контейнер прогресса на сообщение об ошибке
    const progressContainer = attachmentElement.querySelector(".progress-container");
    if (progressContainer) {
        progressContainer.innerHTML = `
            <div class="error-message">${errorMessage}</div>
            <button class="retry-button" data-message-id="${messageId}">Повторить</button>
        `;
        
        const retryButton = attachmentElement.querySelector(".retry-button");
        retryButton.addEventListener("click", function() {
            // Добавить логику повторной загрузки, если необходимо
            console.log("Retry upload for message ID:", this.dataset.messageId);
        });
    }
}

// Функция для создания сообщения с вложением после успешной загрузки
function createMessageWithAttachment(messageData) {
    const attachmentElement = document.getElementById(`attachment-${messageData.id}`);
    if (!attachmentElement) return;
    
    // Убираем класс loading и добавляем финальные стили
    attachmentElement.classList.remove("loading");
    attachmentElement.classList.remove("error");
    
    // Заменяем содержимое на финальное представление вложения
    const fileExtension = messageData.attachment.fileName.split('.').pop().toLowerCase();
    
    attachmentElement.innerHTML = `
        <div class="file-name">
            <i class="fas ${getFileIcon(fileExtension)}"></i> ${messageData.attachment.fileName}
        </div>
        <div class="file-size">${formatFileSize(messageData.attachment.fileSize)}</div>
        <a href="${messageData.attachment.fileUrl}" class="download-link" target="_blank">
            <i class="fas fa-download"></i> Скачать
        </a>
    `;
}

function uploadAttachment(file, messageId) {
    return new Promise((resolve, reject) => {
        const formData = new FormData();
        formData.append("file", file);
        formData.append("messageId", messageId);
        
        const xhr = new XMLHttpRequest();
        
        xhr.open("POST", `${apiUrl}/api/messages/upload-attachment`, true);
        
        // Добавляем токен аутентификации
        const token = localStorage.getItem("authToken");
        if (token) {
            xhr.setRequestHeader("Authorization", `Bearer ${token}`);
        }
        
        // Отслеживание прогресса загрузки
        xhr.upload.addEventListener("progress", function(e) {
            if (e.lengthComputable) {
                const progressPercentage = Math.round((e.loaded / e.total) * 100);
                updateAttachmentProgress(messageId, progressPercentage);
            }
        });
        
        xhr.onload = function() {
            if (xhr.status >= 200 && xhr.status < 300) {
                try {
                    const response = JSON.parse(xhr.responseText);
                    resolve(response);
                } catch (error) {
                    reject(new Error("Ошибка при разборе ответа сервера"));
                }
            } else {
                reject(new Error(`Ошибка загрузки: ${xhr.status} ${xhr.statusText}`));
            }
        };
        
        xhr.onerror = function() {
            reject(new Error("Ошибка сети при загрузке файла"));
        };
        
        xhr.timeout = 60000; // 60 секунд таймаут
        xhr.ontimeout = function() {
            reject(new Error("Время ожидания загрузки истекло"));
        };
        
        xhr.send(formData);
    });
}

// Функция для создания панели просмотра изображений
function createImageViewer() {
    // Глобальный экземпляр imageViewer уже создан в ImageViewer.js
    // Этот метод теперь просто проверяет существование экземпляра
    if (!window.imageViewer) {
        console.error("ImageViewer не инициализирован. Проверьте подключение ImageViewer.js");
        return null;
    }
    return window.imageViewer;
}

// Функция для открытия изображения в просмотрщике
function openImageViewer(imageSrc) {
    // Проверяем, существует ли экземпляр просмотрщика
    if (!window.imageViewer) {
        // Создаем контейнер для просмотрщика
        const viewerContainer = document.createElement('div');
        viewerContainer.id = 'imageViewerContainer';
        viewerContainer.className = 'image-viewer';
        document.body.appendChild(viewerContainer);
        
        // Создаем элемент изображения
        const imgElement = document.createElement('img');
        imgElement.className = 'image-viewer-img';
        
        // Инициализируем просмотрщик
        window.imageViewer = new ImageViewer(viewerContainer, imgElement);
    }
    
    // Открываем просмотрщик с указанным изображением
    window.imageViewer.open(imageSrc, (editedImageData) => {
        // Callback для обработки отредактированного изображения (если нужно)
        console.log('Изображение отредактировано:', editedImageData && editedImageData.substring(0, 30) + '...');
    });
    
    // Предотвращаем всплытие события, чтобы не сработали другие обработчики
    return false;
}

// Обновляем обработчик клика по изображениям в чате
document.addEventListener('click', (e) => {
    if (e.target.classList.contains('message-image')) {
        openImageViewer(e.target.src);
    }
});

/**
 * Открывает просмотрщик изображений для выбранного изображения в сообщении
 * @param {Event} e - Событие клика
 * @param {string} imageUrl - URL изображения
 */
function openImageViewer(e, imageUrl) {
    e.preventDefault();
    e.stopPropagation(); // Предотвращаем всплытие события
    
    // Проверяем инициализирован ли глобальный экземпляр imageViewer
    if (!window.imageViewer) {
        // Создаем контейнер для просмотрщика, если его еще нет
        let container = document.getElementById('imageViewerContainer');
        if (!container) {
            container = document.createElement('div');
            container.id = 'imageViewerContainer';
            container.className = 'image-viewer';
            document.body.appendChild(container);
        }
        
        // Создаем элемент изображения
        const imageElement = document.createElement('img');
        imageElement.className = 'image-viewer-img';
        
        // Инициализируем просмотрщик изображений
        window.imageViewer = new ImageViewer(container, imageElement);
    }
    
    // Открываем просмотрщик с выбранным изображением
    window.imageViewer.open(imageUrl, null, function(editedImageData) {
        // Callback для подтверждения редактирования
        // Добавляем отредактированное изображение к сообщению для отправки
        appendImageToMessage(editedImageData);
        
        // Показываем предпросмотр изображения
        const previewContainer = document.querySelector('.message-attachments-preview');
        if (previewContainer) {
            // Очищаем предыдущие предпросмотры
            previewContainer.innerHTML = '';
            
            // Создаем предпросмотр изображения
            const imgPreview = document.createElement('div');
            imgPreview.className = 'image-preview';
            imgPreview.innerHTML = `
                <img src="${editedImageData}" alt="Отредактированное изображение">
                <button class="remove-attachment" onclick="removeAttachment(this)">
                    <i class="ri-close-line"></i>
                </button>
            `;
            
            previewContainer.appendChild(imgPreview);
            previewContainer.style.display = 'flex';
        }
    });
}

/**
 * Добавляет изображение к сообщению для отправки
 * @param {string} imageData - URL или Data URL изображения
 */
function appendImageToMessage(imageData) {
    // Проверяем, инициализирован ли массив прикрепленных файлов
    if (!window.attachments) {
        window.attachments = [];
    }
    
    // Преобразуем Data URL в Blob для отправки
    if (imageData.startsWith('data:')) {
        const byteString = atob(imageData.split(',')[1]);
        const mimeType = imageData.split(',')[0].split(':')[1].split(';')[0];
        
        const ab = new ArrayBuffer(byteString.length);
        const ia = new Uint8Array(ab);
        
        for (let i = 0; i < byteString.length; i++) {
            ia[i] = byteString.charCodeAt(i);
        }
        
        const blob = new Blob([ab], { type: mimeType });
        const file = new File([blob], "edited_image.png", { type: "image/png" });
        
        // Добавляем файл в список прикрепленных
        window.attachments.push({
            file: file,
            type: 'image',
            dataUrl: imageData
        });
    } else {
        // Если это обычный URL, загружаем изображение и преобразуем в Blob
        fetch(imageData)
            .then(response => response.blob())
            .then(blob => {
                const file = new File([blob], "image.png", { type: "image/png" });
                
                // Добавляем файл в список прикрепленных
                window.attachments.push({
                    file: file,
                    type: 'image',
                    url: imageData
                });
            });
    }
    
    // Обновляем UI для отображения прикрепленных файлов
    updateAttachmentsUI();
}

/**
 * Удаляет прикрепленный файл
 * @param {HTMLElement} element - Кнопка удаления
 */
function removeAttachment(element) {
    const preview = element.closest('.image-preview');
    if (preview) {
        const index = Array.from(preview.parentNode.children).indexOf(preview);
        
        // Удаляем файл из массива
        if (window.attachments && window.attachments.length > index) {
            window.attachments.splice(index, 1);
        }
        
        // Удаляем предпросмотр
        preview.remove();
        
        // Скрываем контейнер, если больше нет прикрепленных файлов
        const previewContainer = document.querySelector('.message-attachments-preview');
        if (previewContainer && previewContainer.children.length === 0) {
            previewContainer.style.display = 'none';
        }
        
        // Обновляем UI
        updateAttachmentsUI();
    }
}

/**
 * Обновляет интерфейс прикрепленных файлов
 */
function updateAttachmentsUI() {
    const attachBtn = document.querySelector('.attach-btn');
    const previewContainer = document.querySelector('.message-attachments-preview');
    
    if (window.attachments && window.attachments.length > 0) {
        // Есть прикрепленные файлы - изменяем стиль кнопки
        if (attachBtn) {
            attachBtn.classList.add('has-attachments');
        }
        
        // Показываем контейнер с предпросмотром
        if (previewContainer) {
            previewContainer.style.display = 'flex';
        }
    } else {
        // Нет прикрепленных файлов
        if (attachBtn) {
            attachBtn.classList.remove('has-attachments');
        }
        
        // Скрываем контейнер с предпросмотром
        if (previewContainer) {
            previewContainer.style.display = 'none';
        }
    }
}
