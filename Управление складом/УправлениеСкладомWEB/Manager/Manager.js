// Manager.js

document.addEventListener("DOMContentLoaded", () => {
  checkAuthorization();
  // Привязываем сразу события на карточки и кнопку выхода
  initializeEventListeners();
  
  // Применяем тему при первой загрузке (если пришли напрямую)
  applyTheme();
});

/**
 * При возвращении на эту страницу (например, при нажатии "Назад" из кеша bfcache),
 * событие `pageshow` срабатывает, даже если страница не была перезагружена.
 * Здесь мы ещё раз читаем тему и устанавливаем её.
 */
window.addEventListener("pageshow", () => {
  applyTheme();
});

/** Устанавливает тему из localStorage для текущего пользователя */
function applyTheme() {
  // Получаем имя пользователя из localStorage
  const username = localStorage.getItem("username") || "";
  // Формируем ключ для темы конкретного пользователя
  const themeKey = `appTheme-${username}`;
  // Считываем тему (если нет, по умолчанию "light")
  const savedTheme = localStorage.getItem(themeKey) || "light";
  // Применяем тему на странице
  document.documentElement.setAttribute("data-theme", savedTheme);
}

/** Проверяем, что пользователь авторизован как Менеджер */
function checkAuthorization() {
  const isAuth = localStorage.getItem("auth");
  const userRole = localStorage.getItem("role");
  if (isAuth !== "true" || userRole !== "Менеджер") {
    window.location.href = "../Login.html";
    return;
  }
  // Если имя пользователя сохранено – отображаем его
  const username = localStorage.getItem("username") || "Менеджер";
  const usernameEl = document.querySelector(".username");
  if (usernameEl) {
    usernameEl.textContent = username;
  }
  // Приветственное уведомление
  setTimeout(() => {
    showNotification(`Добро пожаловать в панель управления, ${username}!`);
  }, 1500);
}

/** Вешаем обработчики на карточки и кнопку выхода */
function initializeEventListeners() {
  const manageUsersCard = document.getElementById("manageUsersCard");
  const manageInventoryCard = document.getElementById("manageInventoryCard");
  const reportsCard = document.getElementById("reportsCard");
  const settingsCard = document.getElementById("settingsCard");

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
  if (notificationMessage) {
    notificationMessage.textContent = message;
  }
  
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
