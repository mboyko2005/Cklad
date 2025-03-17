document.addEventListener("DOMContentLoaded", () => {
  // Устанавливаем тему из localStorage
  const username = localStorage.getItem("username") || "";
  const themeKey = `appTheme-${username}`; // Исправлена опечатка в кавычках
  const savedTheme = localStorage.getItem(themeKey) || "light";
  document.documentElement.setAttribute("data-theme", savedTheme);

  // Проверка авторизации и роли
  const isAuth = localStorage.getItem("auth");
  const userRole = localStorage.getItem("role");

  if (isAuth !== "true" || userRole !== "Сотрудник склада") {
    window.location.href = "../Login.html";
    return;
  }

  // Находим элементы-карточки
  const viewGoodsCard = document.getElementById("viewGoodsCard");
  const manageStocksCard = document.getElementById("manageStocksCard");
  const moveGoodsCard = document.getElementById("moveGoodsCard");
  const inoutAccountingCard = document.getElementById("inoutAccountingCard");
  const exitBtn = document.getElementById("exitBtn");
  const confirmLogout = document.getElementById("confirmLogout");
  const cancelLogout = document.getElementById("cancelLogout");
  const modalClose = document.querySelector(".modal-close");

  // Назначаем обработчики для карточек
  viewGoodsCard.addEventListener("click", () => {
    alert("Просмотр товаров — логика для сотрудника склада.");
  });

  manageStocksCard.addEventListener("click", () => {
    alert("Управление запасами — логика для сотрудника склада.");
  });

  moveGoodsCard.addEventListener("click", () => {
    alert("Перемещение товаров — логика для сотрудника склада.");
  });

  inoutAccountingCard.addEventListener("click", () => {
    alert("Учёт приходов/расходов — логика для сотрудника склада.");
  });

  // Обработчик для кнопки "Выход" в выпадающем меню
  if (exitBtn) {
    exitBtn.addEventListener("click", () => {
      const modal = document.getElementById("logoutModal");
      if (modal) {
        modal.style.display = "flex";
      }
    });
  }

  // Обработчики для модального окна
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

  // Загрузка данных о товарах
  loadInventoryData();
});

/** Функция выхода из системы */
function handleExit() {
  localStorage.removeItem("auth");
  localStorage.removeItem("role");
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
      goodsCounter.textContent = "Ошибка"; // Указываем ошибку для пользователя
    }
  }
}