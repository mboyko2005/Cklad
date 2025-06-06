document.addEventListener("DOMContentLoaded", () => {
  applyTheme();
  checkAuthorization();
  initializeEventListeners();
});

// Обработчик события pageshow (срабатывает при возврате на страницу)
window.addEventListener('pageshow', () => {
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

/** Проверяем, что пользователь авторизован как Администратор */
function checkAuthorization() {
  const isAuth = localStorage.getItem("auth");
  const userRole = localStorage.getItem("role");
  if (isAuth !== "true" || userRole !== "Администратор") {
    window.location.href = "../Login.html";
    return;
  }
  // Если имя пользователя сохранено – отображаем его
  const username = localStorage.getItem("username");
  if (username) {
    const usernameEl = document.querySelector(".username");
    if (usernameEl) usernameEl.textContent = username;
  }
  
  setTimeout(() => {
    showNotification(`Добро пожаловать в панель управления, ${username || 'Администратор'}!`);
  }, 1500);
}

/** Вешаем обработчики на карточки и кнопку выхода */
function initializeEventListeners() {
  const manageUsersCard = document.getElementById("manageUsersCard");
  const manageInventoryCard = document.getElementById("manageInventoryCard");
  const reportsCard = document.getElementById("reportsCard");
  const settingsCard = document.getElementById("settingsCard");
  const botCard = document.getElementById("botCard");
  const messengerCard = document.getElementById("messengerCard");

  // Переходы на нужные HTML-страницы:
  if (manageUsersCard) {
    manageUsersCard.addEventListener("click", () => {
      window.location.href = "ManageUsers/ManageUsers.html";
    });
  }
  if (manageInventoryCard) {
    manageInventoryCard.addEventListener("click", () => {
      window.location.href = "ManageInventory/ManageInventory.html";
    });
  }
  if (reportsCard) {
    reportsCard.addEventListener("click", () => {
      window.location.href = "Reports/Reports.html";
    });
  }
  if (settingsCard) {
    settingsCard.addEventListener("click", () => {
      window.location.href = "Settings/Settings.html";
    });
  }
  if (botCard) {
    botCard.addEventListener("click", () => {
      window.location.href = "ManageBot/ManageBot.html";
    });
  }
  if (messengerCard) {
    messengerCard.addEventListener("click", () => {
      window.location.href = "Messenger/Messenger.html";
    });
  }

  // Обработчик кнопки выхода
  const exitBtn = document.getElementById("exitBtn");
  if (exitBtn) {
    exitBtn.addEventListener("click", handleExit);
  }
}

/** Удаляем данные авторизации и перенаправляем на страницу входа */
function handleExit() {
  localStorage.removeItem("auth");
  localStorage.removeItem("role");
  localStorage.removeItem("username");
  window.location.href = "../Login.html";
}

/** Показать уведомление (toast) */
function showNotification(message) {
  const notification = document.getElementById("notification");
  if (!notification) return;

  const notificationMessage = notification.querySelector(".notification-message");
  if (notificationMessage) notificationMessage.textContent = message;
  
  notification.classList.add("show");

  // Скрываем через 3 секунды
  setTimeout(() => {
    hideNotification();
  }, 3000);

  // Закрытие по нажатию на крестик
  const closeBtn = notification.querySelector(".notification-close");
  if (closeBtn) {
    closeBtn.onclick = hideNotification;
  }
}

/** Скрыть уведомление */
function hideNotification() {
  const notification = document.getElementById("notification");
  if (!notification) return;
  notification.classList.remove("show");
}
