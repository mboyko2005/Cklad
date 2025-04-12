let goodsData = [];
let selectedProductId = null;

document.addEventListener("DOMContentLoaded", () => {
  // Проверяем авторизацию
  checkAuthorization();
  // Получаем имя пользователя из localStorage
  const username = localStorage.getItem("username") || "";
  // Формируем ключ для темы конкретного пользователя
  const themeKey = `appTheme-${username}`;
  // Считываем тему (если нет, по умолчанию "light")
  const savedTheme = localStorage.getItem(themeKey) || "light";
  // Применяем тему на странице
  document.documentElement.setAttribute("data-theme", savedTheme);
  // Загружаем список товаров
  loadGoods();

  // Вешаем обработчики
  initializeEventListeners();
});

/** Проверяем авторизацию (Сотрудник склада) */
function checkAuthorization() {
  const isAuth = localStorage.getItem("auth");
  const role = localStorage.getItem("role");
  if (isAuth !== "true" || role !== "Сотрудник склада") {
    window.location.href = "../Login.html";
  }
}

/** Применяем тему, сохранённую для конкретного пользователя */
function applyUserTheme() {
  const username = localStorage.getItem("username") || "";
  const themeKey = `appTheme-${username}`;
  const savedTheme = localStorage.getItem(themeKey) || "light";
  // Устанавливаем атрибут data-theme на html
  document.documentElement.setAttribute("data-theme", savedTheme);
}

/** Инициализация обработчиков */
function initializeEventListeners() {
  // Кнопка "Назад"
  const backButton = document.getElementById("backButton");
  if (backButton) {
    backButton.addEventListener("click", () => {
      // Возвращаемся на панель сотрудника (проверьте путь!)
      window.location.href = "../Staff.html";
    });
  }

  // Кнопка "Выход из системы"
  const exitBtn = document.getElementById("exitBtn");
  if (exitBtn) {
    exitBtn.addEventListener("click", handleExit);
  }

  // Поиск
  const searchInput = document.getElementById("searchInput");
  if (searchInput) {
    searchInput.addEventListener("input", () => {
      const query = searchInput.value.toLowerCase();
      const filtered = goodsData.filter(item => {
        const nameMatch = (item.productName || "").toLowerCase().includes(query);
        const catMatch = (item.category || "").toLowerCase().includes(query);
        const warehouseMatch = (item.warehouseName || "").toLowerCase().includes(query);
        return nameMatch || catMatch || warehouseMatch;
      });
      renderGoodsTable(filtered);
    });
  }

  // Показ QR-кода
  const showQrBtn = document.getElementById("showQrBtn");
  if (showQrBtn) {
    showQrBtn.addEventListener("click", handleShowQr);
  }

  // Закрытие модального окна QR
  const closeQrModal = document.getElementById("closeQrModal");
  if (closeQrModal) {
    closeQrModal.addEventListener("click", () => {
      document.getElementById("qrModal").style.display = "none";
    });
  }

  // Скачать и распечатать QR
  const downloadQrBtn = document.getElementById("downloadQrBtn");
  if (downloadQrBtn) {
    downloadQrBtn.addEventListener("click", downloadQrCode);
  }
  const printQrBtn = document.getElementById("printQrBtn");
  if (printQrBtn) {
    printQrBtn.addEventListener("click", printQrCode);
  }

  // Закрытие уведомления
  const notificationClose = document.getElementById("notificationClose");
  if (notificationClose) {
    notificationClose.addEventListener("click", hideNotification);
  }

  // Закрытие модалки при клике вне её области
  window.addEventListener("click", (evt) => {
    const qrModal = document.getElementById("qrModal");
    if (evt.target === qrModal) {
      qrModal.style.display = "none";
    }
  });
}

/** Загрузка списка товаров */
async function loadGoods() {
  try {
    const response = await fetch("/api/manageinventory");
    if (!response.ok) {
      throw new Error("Ошибка сети или API недоступен");
    }
    const data = await response.json();
    goodsData = data;
    renderGoodsTable(goodsData);
  } catch (error) {
    console.error("Ошибка при загрузке товаров:", error);
    showNotification("Ошибка при загрузке товаров", "error");
  }
}

/** Отрисовка таблицы */
function renderGoodsTable(items) {
  const tbody = document.querySelector("#goodsTable tbody");
  if (!tbody) return;

  tbody.innerHTML = "";

  items.forEach(item => {
    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${item.productId}</td>
      <td>${item.productName || ""}</td>
      <td>${item.category || ""}</td>
      <td>${item.quantity || 0}</td>
      <td>${item.price ? parseFloat(item.price).toFixed(2) : "0.00"}</td>
      <td>${item.warehouseName || "Нет на складе"}</td>
    `;

    tr.addEventListener("click", () => {
      // Убираем выделение со всех строк
      document.querySelectorAll("#goodsTable tbody tr").forEach(row => row.classList.remove("selected"));
      // Выделяем текущую
      tr.classList.add("selected");
      selectedProductId = item.productId;
    });

    tbody.appendChild(tr);
  });
}

/** Показ QR-кода для выбранного товара */
function handleShowQr() {
  if (!selectedProductId) {
    showNotification("Сначала выберите товар из списка", "error");
    return;
  }
  // Запрашиваем QR-код по ID
  fetch(`/api/qrcode/product/${selectedProductId}`)
    .then(resp => {
      if (!resp.ok) throw new Error("Ошибка при генерации QR: " + resp.status);
      return resp.blob();
    })
    .then(blob => {
      const url = URL.createObjectURL(blob);
      document.getElementById("qrImage").src = url;
      document.getElementById("qrModal").style.display = "flex";
    })
    .catch(err => {
      console.error("Ошибка при загрузке QR:", err);
      showNotification("Ошибка при загрузке QR", "error");
    });
}

/** Скачать QR-код */
function downloadQrCode() {
  const img = document.getElementById("qrImage");
  if (!img || !img.src) {
    showNotification("Нет QR для скачивания", "error");
    return;
  }
  const link = document.createElement("a");
  link.href = img.src;
  link.download = "QrCode.png";
  link.click();
}

/** Печать QR-кода */
function printQrCode() {
  const img = document.getElementById("qrImage");
  if (!img || !img.src) {
    showNotification("Нет QR для печати", "error");
    return;
  }
  const w = window.open("");
  w.document.write(`<img src="${img.src}" onload="window.print(); window.close();" />`);
  w.document.close();
}

/** Выход из системы */
function handleExit() {
  localStorage.removeItem("auth");
  localStorage.removeItem("role");
  localStorage.removeItem("username");
  window.location.href = "../Login.html";
}

/** Уведомления (toast) */
function showNotification(message, type = "info") {
  const notification = document.getElementById("notification");
  const icon = document.getElementById("notificationIcon");
  const msg = document.getElementById("notificationMessage");

  msg.textContent = message;

  switch (type) {
    case "success":
      icon.className = "ri-checkbox-circle-line";
      icon.style.color = "#2ecc71";
      break;
    case "error":
      icon.className = "ri-close-circle-line";
      icon.style.color = "#e53e3e";
      break;
    default:
      icon.className = "ri-information-line";
      icon.style.color = "var(--primary-color)";
      break;
  }

  notification.classList.add("show");
  setTimeout(() => {
    hideNotification();
  }, 3000);
}

function hideNotification() {
  document.getElementById("notification").classList.remove("show");
}
