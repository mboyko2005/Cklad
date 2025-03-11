document.addEventListener("DOMContentLoaded", () => {
  checkAuthorization();
  initializeEventListeners();
  initPulseEffect(); // Опциональный "пульс"
});

/** Проверяем, что пользователь авторизован как Администратор */
function checkAuthorization() {
  const isAuth = localStorage.getItem("auth");
  const userRole = localStorage.getItem("role");
  if (isAuth !== "true" || userRole !== "Администратор") {
    window.location.href = "../Login.html";
    return;
  }
  // Если хотим приветствовать по имени:
  const username = localStorage.getItem("username");
  if (username) {
    const usernameEl = document.querySelector(".username");
    if (usernameEl) usernameEl.textContent = username;
  }
  
  setTimeout(() => {
    showNotification(`Добро пожаловать в панель управления, ${username || 'Администратор'}!`);
  }, 1500);
}

/** Вешаем обработчики на карточки */
function initializeEventListeners() {
  const manageUsersCard = document.getElementById("manageUsersCard");
  const manageInventoryCard = document.getElementById("manageInventoryCard");
  const reportsCard = document.getElementById("reportsCard");
  const settingsCard = document.getElementById("settingsCard");
  const botCard = document.getElementById("botCard");

  // Вместо showNotification — переход на нужные HTML-страницы:
  if (manageUsersCard) {
    manageUsersCard.addEventListener("click", () => {
      // Открываем страницу ManageUsers/ManageUsers.html
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

  // Выход из системы через меню (если есть кнопка exitBtn)
  const exitBtn = document.getElementById("exitBtn");
  if (exitBtn) {
    exitBtn.addEventListener("click", handleExit);
  }
}

/** Удаляем данные авторизации и уходим на Login */
function handleExit() {
  localStorage.removeItem("auth");
  localStorage.removeItem("role");
  localStorage.removeItem("username");
  window.location.href = "../Login.html";
}

/** Опциональный "пульс" карточек */
function initPulseEffect() {
  const cards = document.querySelectorAll(".admin-card");
  cards.forEach((card, index) => {
    setTimeout(() => {
      card.style.boxShadow = "0 5px 20px rgba(58, 123, 213, 0.15)";
      setTimeout(() => {
        card.style.boxShadow = "";
      }, 800);
    }, 2000 + (index * 300));
  });
}
