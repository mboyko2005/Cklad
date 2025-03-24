/*************************************************************
 *  ManageInventory.js (пример с camelCase-свойствами)
*************************************************************/
let inventoryData = [];
let selectedPositionId = null;
let isEditMode = false;

document.addEventListener("DOMContentLoaded", () => {
  checkAuthorization();
  loadWarehouses();
  loadInventory();
  initializeEventListeners();
  // Получаем имя пользователя из localStorage
  const username = localStorage.getItem("username") || "";
  // Формируем ключ для темы конкретного пользователя
  const themeKey = `appTheme-${username}`;
  // Считываем тему (если нет, по умолчанию "light")
  const savedTheme = localStorage.getItem(themeKey) || "light";
  // Применяем тему на странице
  document.documentElement.setAttribute("data-theme", savedTheme);
});

function checkAuthorization() {
  const isAuth = localStorage.getItem("auth");
  const role = localStorage.getItem("role");
  if (isAuth !== "true" || role !== "Менеджер") {
    window.location.href = "../../Login.html";
  }
}

function initializeEventListeners() {
  // Кнопка "Назад" в шапке
  document.getElementById("backButton").addEventListener("click", () => {
    // Возвращаемся на Менеджера страницу
    window.location.href = "../Manager.html";
  });

  // Кнопка "Выход" в меню пользователя
  document.getElementById("exitBtn").addEventListener("click", () => {
    localStorage.removeItem("auth");
    localStorage.removeItem("role");
    window.location.href = "../../Login.html";
  });

  // Кнопки CRUD
  document.getElementById("addInventoryBtn").addEventListener("click", () => {
    isEditMode = false;
    selectedPositionId = null;
    openInventoryModal("Добавить запись");
  });

  document.getElementById("editInventoryBtn").addEventListener("click", handleEditInventory);
  document.getElementById("deleteInventoryBtn").addEventListener("click", handleDeleteInventory);
  document.getElementById("showQrBtn").addEventListener("click", handleShowQr);

  // Закрытие модалки (крестик) и кнопка "Отмена"
  document.getElementById("closeModal").addEventListener("click", () => {
    document.getElementById("inventoryModal").style.display = "none";
  });
  document.getElementById("cancelBtn").addEventListener("click", () => {
    document.getElementById("inventoryModal").style.display = "none";
  });

  // Сохранение
  document.getElementById("saveInventoryBtn").addEventListener("click", handleSaveInventory);

  // Поиск
  document.getElementById("searchInput").addEventListener("input", () => {
    const query = document.getElementById("searchInput").value.toLowerCase();
    const filtered = inventoryData.filter(item => {
      const nameMatch = (item.productName || "").toLowerCase().includes(query);
      const catMatch = (item.category || "").toLowerCase().includes(query);
      const locMatch = (item.warehouseName || "").toLowerCase().includes(query);
      return nameMatch || catMatch || locMatch;
    });
    renderInventoryTable(filtered);
  });

  // Закрытие модалок при клике вне
  window.addEventListener("click", (evt) => {
    const invModal = document.getElementById("inventoryModal");
    const confirmModal = document.getElementById("confirmModal");
    const qrModal = document.getElementById("qrModal");
    if (evt.target === invModal) invModal.style.display = "none";
    if (evt.target === confirmModal) confirmModal.style.display = "none";
    if (evt.target === qrModal) qrModal.style.display = "none";
  });

  // Модальное окно подтверждения
  document.getElementById("confirmYesBtn").addEventListener("click", () => {
    if (typeof confirmCallback === "function") confirmCallback(true);
    closeConfirmModal();
  });
  document.getElementById("confirmNoBtn").addEventListener("click", () => {
    if (typeof confirmCallback === "function") confirmCallback(false);
    closeConfirmModal();
  });

  // Модалка с QR-кодом
  document.getElementById("closeQrModal").addEventListener("click", () => {
    document.getElementById("qrModal").style.display = "none";
  });
  document.getElementById("downloadQrBtn").addEventListener("click", downloadQrCode);
  document.getElementById("printQrBtn").addEventListener("click", printQrCode);

  // Закрытие уведомления
  document.getElementById("notificationClose").addEventListener("click", hideNotification);
}

/** Загрузка списка складов */
function loadWarehouses() {
  fetch("/api/manageinventory/warehouses")
    .then(resp => resp.json())
    .then(data => {
      populateWarehouseSelect(data);
    })
    .catch(err => {
      showNotification("Ошибка загрузки складов", "error");
      console.error(err);
    });
}

function populateWarehouseSelect(warehouses) {
  const select = document.getElementById("warehouseSelect");
  select.innerHTML = "";
  warehouses.forEach(w => {
    const opt = document.createElement("option");
    opt.value = w.id;
    opt.textContent = w.name;
    select.appendChild(opt);
  });
}

/** Загрузка всех позиций на складе */
function loadInventory() {
  fetch("/api/manageinventory")
    .then(resp => resp.json())
    .then(data => {
      inventoryData = data;
      renderInventoryTable(inventoryData);
    })
    .catch(err => {
      showNotification("Ошибка загрузки данных", "error");
      console.error(err);
    });
}

/** Отрисовка таблицы */
function renderInventoryTable(items) {
  const tbody = document.querySelector("#inventoryTable tbody");
  tbody.innerHTML = "";

  items.forEach(item => {
    // Если positionID = 0, используем productId
    const rowId = item.positionID !== 0 ? item.positionID : item.productId;
    const name = item.productName || "";
    const cat = item.category || "";
    const price = item.price ? parseFloat(item.price).toFixed(2) : "0.00";
    const qty = item.quantity || 0;
    const loc = item.warehouseName || "Нет на складе";

    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${rowId}</td>
      <td>${name}</td>
      <td>${cat}</td>
      <td>${price}</td>
      <td>${qty}</td>
      <td>${loc}</td>
    `;

    tr.addEventListener("click", () => {
      document.querySelectorAll("#inventoryTable tbody tr").forEach(row => row.classList.remove("selected"));
      tr.classList.add("selected");
      selectedPositionId = rowId;
    });

    tbody.appendChild(tr);
  });
}

/** Открыть модальное окно (добавление/редактирование) */
function openInventoryModal(title) {
  document.getElementById("inventoryModal").style.display = "flex";
  document.getElementById("modalTitle").textContent = title;

  // Сброс полей формы
  document.getElementById("productInput").value = "";
  document.getElementById("categoryInput").value = "";
  document.getElementById("priceInput").value = "";
  document.getElementById("quantityInput").value = "";
  document.getElementById("supplierInput").value = "";
  document.getElementById("warehouseSelect").selectedIndex = 0;

  if (isEditMode && selectedPositionId !== null) {
    // Находим объект в массиве
    const item = inventoryData.find(i => {
      const rowId = i.positionID !== 0 ? i.positionID : i.productId;
      return rowId === selectedPositionId;
    });
    if (item) {
      document.getElementById("productInput").value = item.productName || "";
      document.getElementById("categoryInput").value = item.category || "";
      document.getElementById("priceInput").value = item.price || "";
      document.getElementById("quantityInput").value = item.quantity || "";
      document.getElementById("supplierInput").value = item.supplierName || "";
      if (item.warehouseId) {
        document.getElementById("warehouseSelect").value = item.warehouseId;
      }
    }
  }
}

/** Редактирование выбранной позиции */
function handleEditInventory() {
  if (!selectedPositionId) {
    showNotification("Сначала выберите позицию для редактирования", "error");
    return;
  }
  isEditMode = true;
  openInventoryModal("Редактировать запись");
}

/** Удаление выбранной позиции */
function handleDeleteInventory() {
  if (!selectedPositionId) {
    showNotification("Сначала выберите позицию", "error");
    return;
  }
  openConfirmModal("Удаление", "Вы действительно хотите удалить эту позицию?", (confirmed) => {
    if (!confirmed) return;
    fetch(`/api/manageinventory/${selectedPositionId}`, {
      method: "DELETE"
    })
      .then(resp => resp.json())
      .then(data => {
        showNotification(data.message || "Удалено", "success");
        loadInventory();
        selectedPositionId = null;
      })
      .catch(err => {
        console.error("Ошибка при удалении позиции:", err);
        showNotification("Ошибка при удалении позиции", "error");
      });
  });
}

/** Сохранение (Добавить или Обновить) */
function handleSaveInventory() {
  const productName = document.getElementById("productInput").value.trim();
  const category = document.getElementById("categoryInput").value.trim();
  const price = parseFloat(document.getElementById("priceInput").value);
  const quantity = parseInt(document.getElementById("quantityInput").value, 10);
  const supplierName = document.getElementById("supplierInput").value.trim();
  const warehouseId = parseInt(document.getElementById("warehouseSelect").value, 10);

  if (!productName || !category || isNaN(price) || price < 0 ||
      isNaN(quantity) || quantity < 0 || !warehouseId || !supplierName) {
    showNotification("Заполните все поля корректно", "error");
    return;
  }

  const payload = {
    productName,
    category,
    price,
    quantity,
    supplierName,
    warehouseId
  };

  if (isEditMode && selectedPositionId !== null) {
    // PUT — обновление
    fetch(`/api/manageinventory/${selectedPositionId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    })
      .then(resp => resp.json())
      .then(data => {
        showNotification(data.message || "Запись обновлена", "success");
        document.getElementById("inventoryModal").style.display = "none";
        loadInventory();
      })
      .catch(err => {
        console.error("Ошибка при обновлении:", err);
        showNotification("Ошибка при обновлении", "error");
      });
  } else {
    // POST — создание
    fetch("/api/manageinventory", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    })
      .then(resp => resp.json())
      .then(data => {
        showNotification(data.message || "Запись добавлена", "success");
        document.getElementById("inventoryModal").style.display = "none";
        loadInventory();
      })
      .catch(err => {
        console.error("Ошибка при добавлении:", err);
        showNotification("Ошибка при добавлении", "error");
      });
  }
}

/** Показать QR-код выбранной позиции */
function handleShowQr() {
  if (!selectedPositionId) {
    showNotification("Сначала выберите позицию", "error");
    return;
  }
  const item = inventoryData.find(i => {
    const rowId = i.positionID !== 0 ? i.positionID : i.productId;
    return rowId === selectedPositionId;
  });
  if (!item) {
    showNotification("Не найдена позиция для QR", "error");
    return;
  }
  const productId = item.productId;
  if (!productId) {
    showNotification("У записи не указан productId", "error");
    return;
  }

  fetch(`/api/qrcode/product/${productId}`)
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

/* Модальное окно подтверждения */
let confirmCallback = null;
function openConfirmModal(title, text, callback) {
  confirmCallback = callback;
  document.getElementById("confirmTitle").textContent = title;
  document.getElementById("confirmText").textContent = text;
  document.getElementById("confirmModal").style.display = "flex";
}

function closeConfirmModal() {
  document.getElementById("confirmModal").style.display = "none";
  confirmCallback = null;
}

/* Уведомления (toast) */
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
