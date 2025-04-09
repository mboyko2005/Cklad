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
});

// Обработчик события pageshow (срабатывает при возврате на страницу)
window.addEventListener("pageshow", () => {
    applyTheme();
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

/** Отправка сообщения */
async function sendMessage() {
    const messageTextArea = document.getElementById('messageTextArea');
    const messageText = messageTextArea.value.trim();
    const messageInputContainer = document.querySelector('.message-input-container');
    
    // Получаем ID текущего пользователя и выбранного контакта
    const currentUserId = localStorage.getItem("userId");
    const selectedContactEl = document.querySelector(".contact-item.active");
    
    if (!selectedContactEl) {
        MessengerUI.showNotification('Пожалуйста, выберите контакт для отправки сообщения', 'error');
        return;
    }
    
    const selectedContactId = selectedContactEl.dataset.id;
    
    if (!messageText && !window.attachmentManager?.hasAttachment()) {
        return;
    }
    
    // Показываем индикатор отправки сообщения
    const sendButton = document.getElementById('send-button');
    if (sendButton) {
        const originalIcon = sendButton.innerHTML;
        sendButton.innerHTML = '<i class="ri-loader-4-line" style="animation: spin 1s linear infinite;"></i>';
        sendButton.disabled = true;
    }
    
    try {
        // Проверяем, есть ли вложение
        if (window.attachmentManager && window.attachmentManager.hasAttachment()) {
            // Отправляем сообщение с вложением
            await MessageAPI.sendMessageWithAttachment(
                currentUserId, 
                selectedContactId, 
                messageText, 
                window.attachmentManager.getAttachment()
            );
            
            // Очищаем поле ввода и вложение
            messageTextArea.value = '';
            window.attachmentManager.clearAttachments();
            
            // Фокусируемся на поле ввода
            messageTextArea.focus();
            
            // Обновляем чат для отображения нового сообщения
            await loadChatHistory(selectedContactId);
        } else if (messageText) {
            // Отправляем текстовое сообщение без вложения
            await MessageAPI.sendMessage(currentUserId, selectedContactId, messageText);
            
            // Очищаем поле ввода
            messageTextArea.value = '';
            
            // Фокусируемся на поле ввода
            messageTextArea.focus();
            
            // Обновляем чат для отображения нового сообщения
            await loadChatHistory(selectedContactId);
        }
    } catch (error) {
        console.error('Ошибка при отправке сообщения:', error);
        MessengerUI.showNotification('Ошибка при отправке сообщения: ' + error.message, 'error');
    } finally {
        // Восстанавливаем кнопку отправки
        if (sendButton) {
            sendButton.innerHTML = '<i class="ri-send-plane-fill"></i>';
            sendButton.disabled = false;
        }
        
        // Сбрасываем высоту текстового поля
        if (messageTextArea) {
            messageTextArea.style.height = 'auto';
        }
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
    if (window.attachmentManager) {
        // Используем метод класса MessengerAttachment для открытия диалога выбора файла
        const fileInput = document.getElementById('file-input');
        if (fileInput) {
            fileInput.click();
        }
    } else {
        console.error('Менеджер вложений не инициализирован');
        MessengerUI.showNotification('Ошибка при инициализации менеджера вложений', 'error');
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
