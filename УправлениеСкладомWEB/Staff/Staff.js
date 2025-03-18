document.addEventListener("DOMContentLoaded", () => {
  // Устанавливаем тему из localStorage
  const username = localStorage.getItem("username") || "";
  const themeKey = `appTheme-${username}`;
  const savedTheme = localStorage.getItem(themeKey) || "light";
  document.documentElement.setAttribute("data-theme", savedTheme);

  checkAuthorization();
  initializeEventListeners();

  // Загрузка данных о товарах и ожидаемых поставках
  loadInventoryData();
  loadExpectedDeliveriesData();
});

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

  if (viewGoodsCard) {
    viewGoodsCard.addEventListener("click", () => {
      // Пример пути, измените на свой
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

  // Меню пользователя: кнопка "Выход"
  const exitBtn = document.getElementById("exitBtn");
  if (exitBtn) {
    exitBtn.addEventListener("click", () => {
      const modal = document.getElementById("logoutModal");
      if (modal) {
        modal.style.display = "flex";
      }
    });
  }

  // Меню пользователя: кнопка "Настройки"
  const settingsMenuItem = document.getElementById("settingsMenuItem");
  if (settingsMenuItem) {
    settingsMenuItem.addEventListener("click", () => {
      // Пример пути, измените на свой
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

/** Загрузка данных о товарах из API */
async function loadInventoryData() {
  try {
    const response = await fetch("http://localhost:8080/api/manageinventory/totalquantity");
    if (!response.ok) {
      throw new Error("Ошибка сети или API недоступен");
    }
    const data = await response.json();
    const totalGoods = data.totalQuantity || 0; // Устанавливаем 0, если данные отсутствуют
    const goodsCounter = document.querySelector(".analytic-item:nth-child(1) .analytic-value");
    if (goodsCounter) {
      goodsCounter.textContent = totalGoods.toLocaleString();
    }
  } catch (error) {
    console.error("Ошибка загрузки данных о товарах:", error);
    const goodsCounter = document.querySelector(".analytic-item:nth-child(1) .analytic-value");
    if (goodsCounter) {
      goodsCounter.textContent = "Ошибка";
    }
  }
}

/** Загрузка данных об отсутствующих товарах (где количество = 0 или записи нет) */
async function loadExpectedDeliveriesData() {
  try {
    const response = await fetch("http://localhost:8080/api/manageinventory/missing");
    if (!response.ok) {
      throw new Error("Ошибка сети или API недоступен");
    }
    const data = await response.json();
    const missingCounter = document.querySelector(".analytic-item:nth-child(2) .analytic-value");
    if (missingCounter) {
      missingCounter.textContent = data.missingCount.toLocaleString();
    }
  } catch (error) {
    console.error("Ошибка загрузки данных по отсутствующим товарам:", error);
    const missingCounter = document.querySelector(".analytic-item:nth-child(2) .analytic-value");
    if (missingCounter) {
      missingCounter.textContent = "Ошибка";
    }
  }
}
