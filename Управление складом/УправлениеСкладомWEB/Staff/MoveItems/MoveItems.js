let allItems = [];               // Список товаров (с разбивкой по складам)
let displayedItems = [];         // Текущий отфильтрованный список
let categories = ["Все"];        // Список категорий
let warehouses = [];             // Все склады
let sourceWarehousesForItem = [];// Склады, где есть выбранный товар
let selectedItem = null;         // Выбранный товар (объект)
let recognition = null;          // Для голосового поиска (Web Speech API)
let isRecognizing = false;       // Флаг запущенного распознавания

document.addEventListener("DOMContentLoaded", () => {
  checkAuthorization();
  loadData();
  initEventListeners();
  initVoiceRecognition();
  
  // Применяем тему при первой загрузке
  applyTheme();
});

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

// Fallback-механизм: опрос localStorage каждые 500 мс для проверки изменений темы
(function pollThemeChange() {
  const username = localStorage.getItem("username") || "";
  const themeKey = `appTheme-${username}`;
  let currentTheme = localStorage.getItem(themeKey) || "light";
  setInterval(() => {
    const newTheme = localStorage.getItem(themeKey) || "light";
    if (newTheme !== currentTheme) {
      currentTheme = newTheme;
      document.documentElement.setAttribute("data-theme", newTheme);
    }
  }, 500);
})();

// Функция установки темы из localStorage для текущего пользователя
function applyTheme() {
  const username = localStorage.getItem("username") || "";
  const themeKey = `appTheme-${username}`;
  const savedTheme = localStorage.getItem(themeKey) || "light";
  document.documentElement.setAttribute("data-theme", savedTheme);
}

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

/** Загрузка начальных данных (товары, категории, склады) */
async function loadData() {
  try {
    // 1) Загружаем список товаров (с их суммарным количеством)
    //    GET /api/moveitems/items
    const itemsResp = await fetch("/api/moveitems/items");
    if (!itemsResp.ok) throw new Error("Ошибка при загрузке товаров");
    allItems = await itemsResp.json();

    // 2) Извлекаем категории
    categories = ["Все"];
    allItems.forEach(item => {
      if (item.category && !categories.includes(item.category)) {
        categories.push(item.category);
      }
    });

    // 3) Загружаем все склады
    //    GET /api/moveitems/warehouses
    const whResp = await fetch("/api/moveitems/warehouses");
    if (!whResp.ok) throw new Error("Ошибка при загрузке складов");
    warehouses = await whResp.json();

    // Заполняем селект категорий
    fillCategorySelect();
    // Применяем фильтр (отобразим все товары)
    applyFilters();
    // Заполним список целевого местоположения
    fillTargetLocationSelect(warehouses);

    // Если был выбран товар, обновим список исходных складов
    if (selectedItem) {
      const sourceWarehouses = await loadSourceWarehouses(selectedItem.productId);
      fillSourceLocationSelect(sourceWarehouses);
    }
  } catch (error) {
    console.error(error);
    showNotification("Не удалось загрузить данные", "error");
  }
}

/** Инициализация обработчиков */
function initEventListeners() {
  const backButton = document.getElementById("backButton");
  if (backButton) {
    backButton.addEventListener("click", () => {
      // Возвращаемся на панель сотрудника
      window.location.href = "../Staff.html";
    });
  }

  const exitBtn = document.getElementById("exitBtn");
  if (exitBtn) {
    exitBtn.addEventListener("click", handleExit);
  }

  const searchInput = document.getElementById("searchInput");
  if (searchInput) {
    searchInput.addEventListener("input", applyFilters);
  }

  const categorySelect = document.getElementById("categorySelect");
  if (categorySelect) {
    categorySelect.addEventListener("change", applyFilters);
  }

  const itemsTable = document.getElementById("itemsTable");
  if (itemsTable) {
    itemsTable.addEventListener("click", handleItemSelection);
  }

  const voiceSearchBtn = document.getElementById("voiceSearchBtn");
  if (voiceSearchBtn) {
    voiceSearchBtn.addEventListener("click", toggleVoiceRecognition);
  }

  const moveButton = document.getElementById("moveButton");
  if (moveButton) {
    moveButton.addEventListener("click", handleMove);
  }

  const notificationClose = document.getElementById("notificationClose");
  if (notificationClose) {
    notificationClose.addEventListener("click", hideNotification);
  }
}

/** Обработка выхода из системы */
function handleExit() {
  localStorage.removeItem("auth");
  localStorage.removeItem("role");
  localStorage.removeItem("username");
  window.location.href = "../Login.html";
}

/** Применение фильтров */
function applyFilters() {
  const searchValue = (document.getElementById("searchInput").value || "").toLowerCase();
  const selectedCategory = document.getElementById("categorySelect").value || "Все";

  displayedItems = allItems.filter(item => {
    const nameMatch = item.productName.toLowerCase().includes(searchValue);
    const catMatch = (item.category || '').toLowerCase().includes(searchValue);
    const warehouseMatch = item.warehouseName.toLowerCase().includes(searchValue);
    const categoryFilter = (selectedCategory === "Все" || item.category === selectedCategory);
    return (nameMatch || catMatch || warehouseMatch) && categoryFilter;
  });

  renderItemsTable(displayedItems);
  // Сбрасываем выбранный товар
  selectedItem = null;
  clearSourceWarehouses();
}

/** Отрисовка таблицы товаров */
function renderItemsTable(items) {
  const tbody = document.querySelector("#itemsTable tbody");
  if (!tbody) return;
  tbody.innerHTML = "";

  items.forEach(item => {
    const tr = document.createElement("tr");
    tr.dataset.itemId = item.productId;
    tr.dataset.warehouseId = item.warehouseId;
    if (selectedItem && 
        selectedItem.productId === item.productId && 
        selectedItem.warehouseId === item.warehouseId) {
      tr.classList.add("selected");
    }
    tr.innerHTML = `
      <td>${item.productId}</td>
      <td>${item.productName}</td>
      <td>${item.category || ''}</td>
      <td>${item.quantity}</td>
      <td>${item.warehouseName}</td>
    `;
    tbody.appendChild(tr);
  });
}

/** Обработка клика по таблице товаров (выбор товара) */
async function handleItemSelection(e) {
  const row = e.target.closest("tr");
  if (!row) return;

  // Снимаем выделение со всех строк
  document.querySelectorAll("#itemsTable tbody tr").forEach(tr => {
    tr.classList.remove("selected");
  });

  // Выделяем выбранную строку
  row.classList.add("selected");

  const itemId = parseInt(row.dataset.itemId);
  const warehouseId = parseInt(row.dataset.warehouseId);
  selectedItem = allItems.find(item => 
    item.productId === itemId && 
    item.warehouseId === warehouseId
  );
  
  if (selectedItem) {
    // Загружаем склады для выбранного товара
    const sourceWarehouses = await loadSourceWarehouses(selectedItem.productId);
    fillSourceLocationSelect(sourceWarehouses);
    
    // Устанавливаем выбранный склад
    const sourceSelect = document.getElementById("sourceLocationSelect");
    if (sourceSelect) {
      sourceSelect.value = selectedItem.warehouseId;
    }
    
    // Очищаем поле количества
    document.getElementById("moveQuantityInput").value = "";
  }
}

/** Заполнение выпадающего списка исходного склада */
function fillSourceLocationSelect(whList) {
  const sourceSelect = document.getElementById("sourceLocationSelect");
  if (!sourceSelect) return;

  sourceSelect.innerHTML = "";
  if (!whList || whList.length === 0) {
    // Если нет складов, где есть товар
    sourceSelect.disabled = true;
    return;
  }
  // Если всего 1 склад — тоже заполним, но сделаем доступным
  whList.forEach(wh => {
    const option = document.createElement("option");
    option.value = wh.id;
    option.textContent = `${wh.name} (остаток: ${wh.quantity})`;
    sourceSelect.appendChild(option);
  });
  sourceSelect.disabled = false;
}

/** Очистка списка исходного склада */
function clearSourceWarehouses() {
  const sourceSelect = document.getElementById("sourceLocationSelect");
  if (sourceSelect) {
    sourceSelect.innerHTML = "";
    sourceSelect.disabled = true;
  }
}

/** Заполнение выпадающего списка целевого склада */
function fillTargetLocationSelect(whList) {
  const targetSelect = document.getElementById("targetLocationSelect");
  if (!targetSelect) return;

  targetSelect.innerHTML = "";
  whList.forEach(wh => {
    const option = document.createElement("option");
    option.value = wh.id;
    option.textContent = wh.name;
    targetSelect.appendChild(option);
  });
}

/** Заполнение селекта категорий */
function fillCategorySelect() {
  const categorySelect = document.getElementById("categorySelect");
  if (!categorySelect) return;
  categorySelect.innerHTML = "";

  categories.forEach(cat => {
    const option = document.createElement("option");
    option.value = cat;
    option.textContent = cat;
    categorySelect.appendChild(option);
  });
  categorySelect.value = "Все";
}

/** Попытка переместить товар */
async function handleMove() {
  if (!selectedItem) {
    showNotification("Сначала выберите товар в таблице", "error");
    return;
  }
  const sourceSelect = document.getElementById("sourceLocationSelect");
  const targetSelect = document.getElementById("targetLocationSelect");
  const qtyInput = document.getElementById("moveQuantityInput");

  if (sourceSelect.disabled || !sourceSelect.value) {
    showNotification("Нет доступного исходного склада или не выбран", "error");
    return;
  }
  if (!targetSelect.value) {
    showNotification("Не выбран целевой склад", "error");
    return;
  }
  const sourceId = parseInt(sourceSelect.value);
  const targetId = parseInt(targetSelect.value);
  if (sourceId === targetId) {
    showNotification("Исходное и целевое местоположение должны отличаться", "error");
    return;
  }

  const moveQty = parseInt(qtyInput.value);
  if (isNaN(moveQty) || moveQty <= 0) {
    showNotification("Некорректное количество для перемещения", "error");
    return;
  }

  // Проверяем, не превышает ли количество доступное на складе
  if (moveQty > selectedItem.quantity) {
    showNotification("Недостаточно товара на исходном складе", "error");
    return;
  }

  // Делаем POST-запрос на сервер: /api/moveitems
  try {
    const userId = parseInt(localStorage.getItem("userId")) || 1;
    const response = await fetch("/api/moveitems", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        productId: selectedItem.productId,
        sourceWarehouseId: sourceId,
        targetWarehouseId: targetId,
        quantity: moveQty,
        userId: userId
      })
    });

    if (!response.ok) {
      const errData = await response.json();
      throw new Error(errData.message || "Ошибка перемещения");
    }

    const result = await response.json();
    showNotification(result.message || "Товар перемещён", "success");

    // После успеха:
    // 1. Очищаем поле количества
    qtyInput.value = "";
    
    // 2. Обновляем данные
    await loadData();

    // 3. Если это было перемещение всего количества, очищаем выбор товара
    if (moveQty === selectedItem.quantity) {
      selectedItem = null;
      clearSourceWarehouses();
    }
  } catch (error) {
    console.error(error);
    showNotification(error.message, "error");
  }
}

/** Загрузка складов для выбранного товара */
async function loadSourceWarehouses(productId) {
  try {
    const response = await fetch(`/api/moveitems/warehouses/${productId}`);
    if (!response.ok) throw new Error("Ошибка при загрузке складов товара");
    sourceWarehousesForItem = await response.json();
    return sourceWarehousesForItem;
  } catch (error) {
    console.error(error);
    showNotification("Не удалось загрузить склады товара", "error");
    return [];
  }
}

/** Инициализация голосового поиска (Web Speech API) */
function initVoiceRecognition() {
  if (!("webkitSpeechRecognition" in window) && !("SpeechRecognition" in window)) {
    console.warn("Web Speech API не поддерживается в этом браузере.");
    return;
  }
  const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
  recognition = new SpeechRecognition();
  recognition.lang = "ru-RU";
  recognition.continuous = false;
  recognition.interimResults = false;

  recognition.onstart = () => {
    isRecognizing = true;
    const voiceIcon = document.getElementById("voiceIcon");
    if (voiceIcon) {
      voiceIcon.className = "fas fa-record-vinyl";
      voiceIcon.style.color = "red";
    }
  };

  recognition.onend = () => {
    isRecognizing = false;
    const voiceIcon = document.getElementById("voiceIcon");
    if (voiceIcon) {
      voiceIcon.className = "fas fa-microphone";
      voiceIcon.style.color = "";
    }
  };

  recognition.onresult = (event) => {
    const transcript = event.results[0][0].transcript;
    document.getElementById("searchInput").value = transcript;
    applyFilters();
  };

  recognition.onerror = (event) => {
    console.error("Speech recognition error:", event.error);
    showNotification("Ошибка распознавания речи", "error");
    isRecognizing = false;
    const voiceIcon = document.getElementById("voiceIcon");
    if (voiceIcon) {
      voiceIcon.className = "fas fa-microphone";
      voiceIcon.style.color = "";
    }
  };
}

/** Включение/выключение голосового распознавания */
function toggleVoiceRecognition() {
  if (!recognition) {
    showNotification("Голосовой поиск не поддерживается", "error");
    return;
  }
  if (isRecognizing) {
    recognition.stop();
  } else {
    recognition.start();
  }
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
