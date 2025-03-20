let logEntries = [];          // Список записей журнала (движения товаров)
let outOfStockItems = [];     // Список товаров, отсутствующих на складе
let selectedLogEntry = null;  // Выбранная запись журнала
let selectedOutOfStockItem = null; // Выбранный товар из "отсутствующих"

// Переменные для модальных окон
let movementType = ""; // "Приход" или "Расход"
let movementModal = null;
let deleteModal = null;

document.addEventListener("DOMContentLoaded", () => {
  checkAuthorization();
  initializeEventListeners();
  loadAllData();
});

/** Проверка авторизации (пример) */
function checkAuthorization() {
  const isAuth = localStorage.getItem("auth");
  const role = localStorage.getItem("role");
  if (isAuth !== "true" || role !== "Сотрудник склада") {
    window.location.href = "../Login.html";
  }
  // Установим имя пользователя
  const username = localStorage.getItem("username") || "Сотрудник склада";
  const userNameSpan = document.getElementById("userNameSpan");
  if (userNameSpan) {
    userNameSpan.textContent = username;
  }
}

/** Инициализация обработчиков */
function initializeEventListeners() {
  // Кнопка "Назад"
  const backButton = document.getElementById("backButton");
  if (backButton) {
    backButton.addEventListener("click", () => {
      // Пример: переход на панель сотрудника
      window.location.href = "../Staff.html";
    });
  }

  // Кнопка "Выход"
  const exitBtn = document.getElementById("exitBtn");
  if (exitBtn) {
    exitBtn.addEventListener("click", handleExit);
  }

  // Таблица журнала
  const inventoryLogTable = document.getElementById("inventoryLogTable");
  if (inventoryLogTable) {
    inventoryLogTable.addEventListener("click", (e) => {
      const row = e.target.closest("tr");
      if (!row) return;
      handleLogRowSelection(row);
    });
  }

  // Таблица отсутствующих товаров
  const outOfStockTable = document.getElementById("outOfStockTable");
  if (outOfStockTable) {
    outOfStockTable.addEventListener("click", (e) => {
      const row = e.target.closest("tr");
      if (!row) return;
      handleOutOfStockRowSelection(row);
    });
  }

  // Кнопки "Добавить приход", "Добавить расход", "Удалить запись"
  const addIncomeBtn = document.getElementById("addIncomeBtn");
  const addExpenseBtn = document.getElementById("addExpenseBtn");
  const deleteRecordBtn = document.getElementById("deleteRecordBtn");

  if (addIncomeBtn) {
    addIncomeBtn.addEventListener("click", () => openMovementModal("Приход"));
  }
  if (addExpenseBtn) {
    addExpenseBtn.addEventListener("click", () => openMovementModal("Расход"));
  }
  if (deleteRecordBtn) {
    deleteRecordBtn.addEventListener("click", openDeleteModal);
  }

  // Кнопка "Добавить на склад" для отсутствующих товаров
  const addStockBtn = document.getElementById("addStockBtn");
  if (addStockBtn) {
    addStockBtn.addEventListener("click", addStockForOutOfItem);
  }

  // Модальные окна
  movementModal = document.getElementById("movementModal");
  deleteModal = document.getElementById("deleteModal");

  // Кнопки в модальном окне movementModal
  const movementConfirmBtn = document.getElementById("movementConfirmBtn");
  const movementCancelBtn = document.getElementById("movementCancelBtn");

  if (movementConfirmBtn) {
    movementConfirmBtn.addEventListener("click", confirmMovement);
  }
  if (movementCancelBtn) {
    movementCancelBtn.addEventListener("click", closeMovementModal);
  }

  // Кнопки в модальном окне deleteModal
  const deleteConfirmBtn = document.getElementById("deleteConfirmBtn");
  const deleteCancelBtn = document.getElementById("deleteCancelBtn");

  if (deleteConfirmBtn) {
    deleteConfirmBtn.addEventListener("click", confirmDelete);
  }
  if (deleteCancelBtn) {
    deleteCancelBtn.addEventListener("click", closeDeleteModal);
  }

  // Закрыть уведомление
  const notificationClose = document.getElementById("notificationClose");
  if (notificationClose) {
    notificationClose.addEventListener("click", hideNotification);
  }
}

/** Выход из системы */
function handleExit() {
  localStorage.removeItem("auth");
  localStorage.removeItem("role");
  localStorage.removeItem("username");
  window.location.href = "../Login.html";
}

/** Загрузка данных журнала и отсутствующих товаров */
async function loadAllData() {
  try {
    // Загружаем журнал
    let resp = await fetch("http://localhost:8080/api/inventorylog/logs");
    if (!resp.ok) throw new Error("Ошибка при загрузке журнала");
    logEntries = await resp.json();

    // Загружаем список отсутствующих товаров
    resp = await fetch("http://localhost:8080/api/inventorylog/outofstock");
    if (!resp.ok) throw new Error("Ошибка при загрузке отсутствующих товаров");
    outOfStockItems = await resp.json();

    renderLogTable();
    renderOutOfStockTable();
  } catch (error) {
    console.error(error);
    showNotification(error.message, "error");
  }
}

/** Отрисовка таблицы журнала */
function renderLogTable() {
  const tbody = document.querySelector("#inventoryLogTable tbody");
  if (!tbody) return;
  tbody.innerHTML = "";

  logEntries.forEach(entry => {
    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${formatDate(entry.date)}</td>
      <td>${entry.type}</td>
      <td>${entry.itemName}</td>
      <td>${entry.quantity}</td>
      <td class="hidden-col">${entry.movementId}</td>
      <td class="hidden-col">${entry.productId}</td>
      <td class="hidden-col">${entry.warehouseId}</td>
    `;
    tbody.appendChild(tr);
  });
  selectedLogEntry = null;
}

/** Отрисовка таблицы отсутствующих товаров */
function renderOutOfStockTable() {
  const tbody = document.querySelector("#outOfStockTable tbody");
  if (!tbody) return;
  tbody.innerHTML = "";

  outOfStockItems.forEach(item => {
    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td class="hidden-col">${item.productId}</td>
      <td>${item.name}</td>
      <td>${item.quantity}</td>
    `;
    tbody.appendChild(tr);
  });
  selectedOutOfStockItem = null;
}

/** Обработка выбора строки в журнале */
function handleLogRowSelection(row) {
  // Снимаем выделение со всех
  document.querySelectorAll("#inventoryLogTable tbody tr").forEach(tr => tr.classList.remove("selected"));
  row.classList.add("selected");

  // Достаём данные
  const cells = row.querySelectorAll("td");
  selectedLogEntry = {
    date: cells[0].textContent,
    type: cells[1].textContent,
    itemName: cells[2].textContent,
    quantity: parseInt(cells[3].textContent),
    movementId: parseInt(cells[4].textContent),
    productId: parseInt(cells[5].textContent),
    warehouseId: parseInt(cells[6].textContent)
  };
}

/** Обработка выбора строки в списке отсутствующих товаров */
function handleOutOfStockRowSelection(row) {
  document.querySelectorAll("#outOfStockTable tbody tr").forEach(tr => tr.classList.remove("selected"));
  row.classList.add("selected");

  const cells = row.querySelectorAll("td");
  selectedOutOfStockItem = {
    productId: parseInt(cells[0].textContent),
    name: cells[1].textContent,
    quantity: parseInt(cells[2].textContent)
  };
}

/** Открыть модал для прихода/расхода */
function openMovementModal(type) {
  if (!selectedLogEntry) {
    showNotification("Сначала выберите запись в журнале", "error");
    return;
  }
  movementType = type; // "Приход" или "Расход"

  // Устанавливаем заголовок, подзаголовок, очищаем инпут
  const titleEl = document.getElementById("movementModalTitle");
  const subtitleEl = document.getElementById("movementModalSubtitle");
  const qtyInput = document.getElementById("movementQtyInput");

  if (type === "Приход") {
    titleEl.textContent = "Добавить приход";
  } else {
    titleEl.textContent = "Добавить расход";
  }
  subtitleEl.textContent = `Товар: ${selectedLogEntry.itemName}`;
  qtyInput.value = "";

  movementModal.classList.add("show");
}

/** Закрыть модал добавления прихода/расхода */
function closeMovementModal() {
  movementModal.classList.remove("show");
}

/** Подтвердить приход/расход */
async function confirmMovement() {
  const qtyInput = document.getElementById("movementQtyInput");
  const qty = parseInt(qtyInput.value);
  if (isNaN(qty) || qty <= 0) {
    showNotification("Некорректное количество", "error");
    return;
  }
  try {
    await addMovement(selectedLogEntry.productId, selectedLogEntry.warehouseId, qty, movementType, 3 /*userID*/);
    showNotification(`${movementType} добавлен на ${qty} единиц`, "success");
    closeMovementModal();
    await loadAllData();
  } catch (error) {
    showNotification(error.message, "error");
  }
}

/** Открыть модал удаления */
function openDeleteModal() {
  if (!selectedLogEntry) {
    showNotification("Сначала выберите запись в журнале", "error");
    return;
  }
  deleteModal.classList.add("show");
}

/** Закрыть модал удаления */
function closeDeleteModal() {
  deleteModal.classList.remove("show");
}

/** Подтвердить удаление */
async function confirmDelete() {
  try {
    const resp = await fetch(`http://localhost:8080/api/inventorylog/${selectedLogEntry.movementId}`, {
      method: "DELETE"
    });
    if (!resp.ok) {
      const errData = await resp.json();
      throw new Error(errData.message || "Ошибка при удалении записи");
    }
    const data = await resp.json();
    showNotification(data.message || "Запись удалена", "success");
    closeDeleteModal();
    await loadAllData();
  } catch (error) {
    showNotification(error.message, "error");
  }
}

/** Добавить на склад (для выбранного отсутствующего товара, склад по умолчанию = 1) */
async function addStockForOutOfItem() {
  if (!selectedOutOfStockItem) {
    showNotification("Сначала выберите товар из списка отсутствующих", "error");
    return;
  }
  const qtyStr = document.getElementById("addStockQtyInput").value;
  const qty = parseInt(qtyStr);
  if (isNaN(qty) || qty <= 0) {
    showNotification("Некорректное количество", "error");
    return;
  }
  const defaultWarehouseId = 1;
  try {
    await addMovement(selectedOutOfStockItem.productId, defaultWarehouseId, qty, "Приход", 3);
    showNotification(`Добавлено ${qty} единиц для товара ${selectedOutOfStockItem.name}`, "success");
    document.getElementById("addStockQtyInput").value = "";
    await loadAllData();
  } catch (error) {
    showNotification(error.message, "error");
  }
}

/** Общая функция для добавления движения (приход/расход) */
async function addMovement(productId, warehouseId, quantity, type, userId) {
  const body = {
    productId,
    warehouseId,
    quantity,
    type,
    userId
  };
  const resp = await fetch("http://localhost:8080/api/inventorylog", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body)
  });
  if (!resp.ok) {
    const errData = await resp.json();
    throw new Error(errData.message || "Ошибка при добавлении движения");
  }
  return await resp.json();
}

/** Форматирование даты (dd.MM.yyyy HH:mm) */
function formatDate(dateStr) {
  if (!dateStr) return "";
  const dateObj = new Date(dateStr);
  if (isNaN(dateObj.getTime())) return dateStr;
  const day = String(dateObj.getDate()).padStart(2, "0");
  const month = String(dateObj.getMonth() + 1).padStart(2, "0");
  const year = dateObj.getFullYear();
  const hours = String(dateObj.getHours()).padStart(2, "0");
  const mins = String(dateObj.getMinutes()).padStart(2, "0");
  return `${day}.${month}.${year} ${hours}:${mins}`;
}

/** Уведомления (toast) */
function showNotification(message, type = "info") {
  const notification = document.getElementById("notification");
  const icon = document.getElementById("notificationIcon");
  const msg = document.getElementById("notificationMessage");

  msg.textContent = message;
  switch (type) {
    case "success":
      icon.className = "fas fa-check-circle";
      icon.style.color = "#2ecc71";
      break;
    case "error":
      icon.className = "fas fa-times-circle";
      icon.style.color = "#e53e3e";
      break;
    default:
      icon.className = "fas fa-info-circle";
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
