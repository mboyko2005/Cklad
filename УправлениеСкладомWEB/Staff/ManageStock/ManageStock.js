let allItems = [];          // Полный список товаров, загруженных с сервера
let filteredItems = [];     // Отфильтрованный список
let showAll = false;        // Флаг "Показывать все" или только с количеством=0
let recognition = null;     // Для голосового поиска (Web Speech API)
let isRecognizing = false;  // Флаг запущенного распознавания

document.addEventListener("DOMContentLoaded", () => {
  checkAuthorization();
  applyUserTheme(); // Если хотите применять сохранённую тему
  loadItems(showAll);
  initializeEventListeners();
});

/** Проверка авторизации (роль "Сотрудник склада") */
function checkAuthorization() {
  const isAuth = localStorage.getItem("auth");
  const role = localStorage.getItem("role");
  if (isAuth !== "true" || role !== "Сотрудник склада") {
    window.location.href = "../Login.html";
  }
  // Установим имя пользователя в интерфейсе
  const username = localStorage.getItem("username") || "Сотрудник склада";
  const userNameSpan = document.getElementById("userNameSpan");
  if (userNameSpan) {
    userNameSpan.textContent = username;
  }
}

/** Считываем тему пользователя из localStorage и применяем (если нужно) */
function applyUserTheme() {
  const username = localStorage.getItem("username") || "";
  const themeKey = `appTheme-${username}`;
  const savedTheme = localStorage.getItem(themeKey) || "light";
  document.documentElement.setAttribute("data-theme", savedTheme);
}

/** Инициализация обработчиков */
function initializeEventListeners() {
  // Кнопка "Назад"
  const backButton = document.getElementById("backButton");
  if (backButton) {
    backButton.addEventListener("click", () => {
      // Переход на панель сотрудника (пример)
      window.location.href = "../Staff.html";
    });
  }

  // Кнопка "Выход"
  const exitBtn = document.getElementById("exitBtn");
  if (exitBtn) {
    exitBtn.addEventListener("click", handleExit);
  }

  // Поля фильтра
  const searchInput = document.getElementById("searchInput");
  const minQuantityInput = document.getElementById("minQuantityInput");
  const maxQuantityInput = document.getElementById("maxQuantityInput");

  if (searchInput) {
    searchInput.addEventListener("input", applyFilters);
  }
  if (minQuantityInput) {
    minQuantityInput.addEventListener("input", applyFilters);
  }
  if (maxQuantityInput) {
    maxQuantityInput.addEventListener("input", applyFilters);
  }

  // Кнопка "Показать все товары"
  const toggleShowAllBtn = document.getElementById("toggleShowAllBtn");
  if (toggleShowAllBtn) {
    toggleShowAllBtn.addEventListener("click", () => {
      showAll = !showAll;
      loadItems(showAll);
    });
  }

  // Кнопка голосового поиска
  const voiceSearchBtn = document.getElementById("voiceSearchBtn");
  if (voiceSearchBtn) {
    voiceSearchBtn.addEventListener("click", toggleVoiceRecognition);
  }

  // Закрытие уведомления
  const notificationClose = document.getElementById("notificationClose");
  if (notificationClose) {
    notificationClose.addEventListener("click", hideNotification);
  }

  // Инициализируем Web Speech API (если поддерживается)
  initVoiceRecognition();
}

/** Загрузка списка товаров с учётом флага showAll */
async function loadItems(showAllFlag) {
  try {
    // Запрашиваем GET: /api/managestock?showAll=true/false
    const response = await fetch(`http://localhost:8080/api/managestock?showAll=${showAllFlag}`);
    if (!response.ok) {
      throw new Error("Ошибка сети или API недоступен");
    }
    const data = await response.json();
    allItems = data;
    applyFilters(); // Применяем фильтры сразу после загрузки
    updateToggleShowAllIcon();
  } catch (error) {
    console.error("Ошибка при загрузке товаров:", error);
    showNotification("Ошибка при загрузке товаров", "error");
  }
}

/** Применение фильтров (поиск, мин/макс количество) */
function applyFilters() {
  const searchValue = (document.getElementById("searchInput").value || "").toLowerCase();
  const minQtyValue = parseInt(document.getElementById("minQuantityInput").value) || 0;
  let maxQtyValue = parseInt(document.getElementById("maxQuantityInput").value);
  if (isNaN(maxQtyValue)) {
    maxQtyValue = Number.MAX_SAFE_INTEGER;
  }

  filteredItems = allItems.filter(item => {
    const nameMatch = (item.productName || "").toLowerCase().includes(searchValue);
    const catMatch = (item.category || "").toLowerCase().includes(searchValue);
    const inQuantityRange = (item.quantity >= minQtyValue && item.quantity <= maxQtyValue);
    return (nameMatch || catMatch) && inQuantityRange;
  });

  renderTable(filteredItems);
}

/** Отрисовка таблицы */
function renderTable(items) {
  const tbody = document.querySelector("#stockTable tbody");
  if (!tbody) return;
  tbody.innerHTML = "";

  items.forEach(item => {
    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${item.productId}</td>
      <td>${item.productName}</td>
      <td>${item.category}</td>
      <td>${item.quantity}</td>
      <td>${parseFloat(item.price).toFixed(2)}</td>
    `;
    tbody.appendChild(tr);
  });
}

/** Обновление иконки кнопки "Показать все товары" */
function updateToggleShowAllIcon() {
  const icon = document.getElementById("toggleShowAllIcon");
  if (!icon) return;
  icon.className = showAll ? "ri-filter-off-line" : "ri-filter-line";
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
      voiceIcon.className = "ri-record-circle-line";
      voiceIcon.style.color = "red";
    }
  };

  recognition.onend = () => {
    isRecognizing = false;
    const voiceIcon = document.getElementById("voiceIcon");
    if (voiceIcon) {
      voiceIcon.className = "ri-mic-line";
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
      voiceIcon.className = "ri-mic-line";
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
