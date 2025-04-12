document.addEventListener("DOMContentLoaded", () => {
  checkAuthorization();
  initializeEventListeners();
  applyTheme();
  
  // Show loading indicators
  showLoadingIndicators();
  
  // Load all data in parallel with caching
  Promise.all([
    loadInventoryData(),
    loadExpectedDeliveriesData(),
    loadMissingProductsList(),
    loadUnreadMessages()
  ]).finally(() => {
    hideLoadingIndicators();
    
    // Запускаем проверку непрочитанных сообщений каждые 30 секунд
    setInterval(loadUnreadMessages, 30000);
  });
});

// Cache for API responses
const apiCache = {
  data: {},
  timestamps: {},
  maxAge: 5 * 60 * 1000, // 5 minutes
};

// Show loading indicators
function showLoadingIndicators() {
  const counters = document.querySelectorAll('.analytic-value');
  counters.forEach(counter => {
    counter.textContent = '...';
  });
}

// Hide loading indicators
function hideLoadingIndicators() {
  const loadingElements = document.querySelectorAll('.loading');
  loadingElements.forEach(el => el.remove());
}

// Optimized theme polling with requestAnimationFrame
let lastThemeCheck = 0;
function checkThemeChange(timestamp) {
  if (timestamp - lastThemeCheck >= 1000) { // Check every second instead of 500ms
    const username = localStorage.getItem("username") || "";
    const themeKey = `appTheme-${username}`;
    const newTheme = localStorage.getItem(themeKey) || "light";
    const currentTheme = document.documentElement.getAttribute("data-theme");
    
    if (newTheme !== currentTheme) {
      document.documentElement.setAttribute("data-theme", newTheme);
    }
    
    lastThemeCheck = timestamp;
  }
  requestAnimationFrame(checkThemeChange);
}

// Start theme polling
requestAnimationFrame(checkThemeChange);

// При возвращении на страницу (например, через bfcache) повторно применяем тему
window.addEventListener("pageshow", () => {
  applyTheme();
});

// Обработчик события изменения localStorage для мгновенного обновления темы
window.addEventListener("storage", (event) => {
  const username = localStorage.getItem("username") || "";
  const themeKey = `appTheme-${username}`;
  if (event.key === themeKey) {
    document.documentElement.setAttribute("data-theme", event.newValue);
  }
});

// Функция установки темы из localStorage для текущего пользователя
function applyTheme() {
  const username = localStorage.getItem("username") || "";
  const themeKey = `appTheme-${username}`;
  const savedTheme = localStorage.getItem(themeKey) || "light";
  document.documentElement.setAttribute("data-theme", savedTheme);
}

/** Проверяем, что пользователь авторизован как Сотрудник склада */
function checkAuthorization() {
  const isAuth = localStorage.getItem("auth");
  const userRole = localStorage.getItem("role");
  if (isAuth !== "true" || userRole !== "Сотрудник склада") {
    window.location.href = "../Login.html";
    return;
  }

  // Если имя пользователя сохранено – отображаем его
  const username = localStorage.getItem("username");
  if (username) {
    const usernameEl = document.querySelector(".username");
    if (usernameEl) usernameEl.textContent = username;
  }
}

/** Инициализируем обработчики событий */
function initializeEventListeners() {
  // Карточки
  const viewGoodsCard = document.getElementById("viewGoodsCard");
  const manageStocksCard = document.getElementById("manageStocksCard");
  const moveGoodsCard = document.getElementById("moveGoodsCard");
  const inoutAccountingCard = document.getElementById("inoutAccountingCard");
  const messengerAnalytic = document.getElementById("messengerAnalytic");

  if (viewGoodsCard) {
    viewGoodsCard.addEventListener("click", () => {
      window.location.href = "ViewItems/ViewItems.html";
    });
  }
  if (manageStocksCard) {
    manageStocksCard.addEventListener("click", () => {
      window.location.href = "ManageStock/ManageStock.html";
    });
  }
  if (moveGoodsCard) {
    moveGoodsCard.addEventListener("click", () => {
      window.location.href = "MoveItems/MoveItems.html";
    });
  }
  if (inoutAccountingCard) {
    inoutAccountingCard.addEventListener("click", () => {
      window.location.href = "InventoryLog/InventoryLog.html";
    });
  }
  if (messengerAnalytic) {
    messengerAnalytic.addEventListener("click", () => {
      window.location.href = "Messenger/Messenger.html";
    });
  }

  // Меню пользователя: при наведении (hover) .user-info / .user-menu – открывается (CSS)

  // Кнопка "Выход" – открывает модальное окно
  const exitBtn = document.getElementById("exitBtn");
  if (exitBtn) {
    exitBtn.addEventListener("click", () => {
      const modal = document.getElementById("logoutModal");
      if (modal) {
        modal.style.display = "flex";
      }
    });
  }

  // Кнопка "Настройки"
  const settingsMenuItem = document.getElementById("settingsMenuItem");
  if (settingsMenuItem) {
    settingsMenuItem.addEventListener("click", () => {
      window.location.href = "Settings/Settings.html";
    });
  }

  // Модальное окно подтверждения выхода
  const confirmLogout = document.getElementById("confirmLogout");
  const cancelLogout = document.getElementById("cancelLogout");
  const modalClose = document.querySelector(".modal-close");

  if (confirmLogout) {
    confirmLogout.addEventListener("click", () => {
      handleExit();
    });
  }

  if (cancelLogout) {
    cancelLogout.addEventListener("click", () => {
      const modal = document.getElementById("logoutModal");
      if (modal) {
        modal.style.display = "none";
      }
    });
  }

  if (modalClose) {
    modalClose.addEventListener("click", () => {
      const modal = document.getElementById("logoutModal");
      if (modal) {
        modal.style.display = "none";
      }
    });
  }
}

/** Функция выхода из системы */
function handleExit() {
  localStorage.removeItem("auth");
  localStorage.removeItem("role");
  localStorage.removeItem("username");
  window.location.href = "../Login.html";
}

/** Загрузка данных о товарах (общее количество) из API */
async function loadInventoryData() {
  const cacheKey = 'inventory';
  const cachedData = getCachedData(cacheKey);
  
  if (cachedData) {
    updateInventoryDisplay(cachedData);
    return;
  }

  try {
    const response = await fetch("/api/manageinventory/totalquantity");
    if (!response.ok) {
      throw new Error("Ошибка сети или API недоступен");
    }
    const data = await response.json();
    cacheData(cacheKey, data);
    updateInventoryDisplay(data);
  } catch (error) {
    console.error("Ошибка загрузки данных о товарах:", error);
    updateInventoryDisplay({ totalQuantity: 0 }, true);
  }
}

/** Загрузка данных об ожидаемых поставках */
async function loadExpectedDeliveriesData() {
  const cacheKey = 'deliveries';
  const cachedData = getCachedData(cacheKey);
  
  if (cachedData) {
    updateDeliveriesDisplay(cachedData);
    return;
  }

  try {
    const response = await fetch("/api/manageinventory/missing");
    if (!response.ok) {
      throw new Error("Ошибка сети или API недоступен");
    }
    const data = await response.json();
    cacheData(cacheKey, data);
    updateDeliveriesDisplay(data);
  } catch (error) {
    console.error("Ошибка загрузки данных по отсутствующим товарам:", error);
    updateDeliveriesDisplay({ missingCount: 0 }, true);
  }
}

/** Загрузка списка отсутствующих товаров */
async function loadMissingProductsList() {
  const cacheKey = 'missingProducts';
  const cachedData = getCachedData(cacheKey);
  
  if (cachedData) {
    updateMissingProductsDisplay(cachedData);
    return;
  }

  try {
    const response = await fetch("/api/manageinventory/missingproducts");
    if (!response.ok) {
      throw new Error("Ошибка сети или API недоступен");
    }
    const data = await response.json();
    cacheData(cacheKey, data);
    updateMissingProductsDisplay(data);
  } catch (error) {
    console.error("Ошибка загрузки списка отсутствующих товаров:", error);
    updateMissingProductsDisplay([], true);
  }
}

/** Загрузка количества непрочитанных сообщений */
async function loadUnreadMessages() {
  const cacheKey = 'unreadMessages';
  
  try {
    // Используем MessengerAPI для получения реальных данных
    const data = await MessengerAPI.getUnreadMessagesCount();
    
    if (data.success) {
      // Сохраняем в кеш
      cacheData(cacheKey, data);
      
      // Обновляем интерфейс
      updateUnreadMessagesDisplay(data);
      updateNotificationMessages(data);
    } else {
      throw new Error("Ошибка получения данных о непрочитанных сообщениях");
    }
  } catch (error) {
    console.error("Ошибка загрузки непрочитанных сообщений:", error);
    
    // В случае ошибки проверяем кеш
    const cachedData = getCachedData(cacheKey);
    
    if (cachedData) {
      // Используем кешированные данные
      updateUnreadMessagesDisplay(cachedData);
      updateNotificationMessages(cachedData);
    } else {
      // Вместо отображения ошибки используем данные эмуляции
      const emulationData = MessengerAPI.getUnreadMessagesEmulation();
      cacheData(cacheKey, emulationData);
      updateUnreadMessagesDisplay(emulationData);
      updateNotificationMessages(emulationData);
    }
  }
}

/** Обновляет счетчик непрочитанных сообщений */
function updateUnreadMessagesDisplay(data, isError = false) {
  const messageCounter = document.querySelector("#messengerAnalytic .analytic-value");
  if (messageCounter) {
    // Если ошибка, показываем "0" вместо "Ошибка"
    if (isError) {
      messageCounter.textContent = "0";
      messageCounter.classList.remove("has-messages");
      return;
    }
    
    // Обновляем текст счетчика
    const count = data.unreadCount || 0;
    messageCounter.textContent = count.toString();
    
    // Добавляем/удаляем класс для анимации
    if (count > 0) {
      messageCounter.classList.add("has-messages");
    } else {
      messageCounter.classList.remove("has-messages");
    }
  }
}

/** Обновляет уведомления о новых сообщениях */
function updateNotificationMessages(data, isError = false) {
  const notificationsBadge = document.querySelector(".notifications .badge");
  if (notificationsBadge) {
    // Добавляем к счетчику уведомлений количество непрочитанных сообщений
    const currentCount = parseInt(notificationsBadge.textContent) || 0;
    const missingCount = currentCount;
    const totalCount = isError ? missingCount : (missingCount + data.unreadCount);
    notificationsBadge.textContent = totalCount.toString();
  }

  const notificationsListEl = document.querySelector(".notifications-list");
  if (!notificationsListEl) return;

  // Находим заголовок уведомлений и меняем его
  const notificationsHeader = document.querySelector(".notifications-header");
  if (notificationsHeader) {
    notificationsHeader.textContent = "Уведомления";
  }

  // Если были отображены какие-то уведомления о товарах, сохраняем их
  const existingNotifications = notificationsListEl.innerHTML;

  // Теперь добавляем сообщения от пользователей
  if (isError || !data.messages || data.messages.length === 0) {
    // Если есть ошибки или нет сообщений, оставляем только существующие уведомления
    return;
  }
  
  // Добавляем разделитель если есть существующие уведомления
  let messagesHtml = "";
  if (existingNotifications && existingNotifications.trim() !== "") {
    messagesHtml += `<div class="notification-divider">Новые сообщения</div>`;
  }

  // Добавляем HTML для сообщений
  messagesHtml += data.messages.map(msg => {
    return `
      <div class="notification-item message" onclick="window.location.href='Messenger/Messenger.html'">
        <div class="notification-title">${msg.from}</div>
        <div class="notification-text">${msg.subject}</div>
        <div class="notification-time">${msg.time}</div>
      </div>
    `;
  }).join("");

  // Добавляем HTML к существующим уведомлениям
  notificationsListEl.innerHTML += messagesHtml;
}

// Cache management functions
function getCachedData(key) {
  const cached = apiCache.data[key];
  const timestamp = apiCache.timestamps[key];
  
  if (cached && timestamp && Date.now() - timestamp < apiCache.maxAge) {
    return cached;
  }
  return null;
}

function cacheData(key, data) {
  apiCache.data[key] = data;
  apiCache.timestamps[key] = Date.now();
}

// Display update functions
function updateInventoryDisplay(data, isError = false) {
  const goodsCounter = document.querySelector(".analytic-item:nth-child(1) .analytic-value");
  if (goodsCounter) {
    goodsCounter.textContent = isError ? "Ошибка" : data.totalQuantity.toLocaleString();
  }
}

function updateDeliveriesDisplay(data, isError = false) {
  const missingCounter = document.querySelector(".analytic-item:nth-child(2) .analytic-value");
  if (missingCounter) {
    missingCounter.textContent = isError ? "Ошибка" : data.missingCount.toLocaleString();
  }
}

function updateMissingProductsDisplay(missingList, isError = false) {
  const notificationsBadge = document.querySelector(".notifications .badge");
  if (notificationsBadge) {
    notificationsBadge.textContent = isError ? "!" : missingList.length.toString();
  }

  const notificationsListEl = document.querySelector(".notifications-list");
  if (!notificationsListEl) return;

  if (isError) {
    notificationsListEl.innerHTML = "<div class='notification-item'>Ошибка загрузки</div>";
  } else if (missingList.length === 0) {
    notificationsListEl.innerHTML = "<div class='notification-item'>Все товары в наличии</div>";
  } else {
    const itemsHtml = missingList.map(item => {
      const name = item.productName || "Без названия";
      return `<div class="notification-item">${name}</div>`;
    }).join("");
    notificationsListEl.innerHTML = itemsHtml;
  }
}
