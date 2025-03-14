/***************************************
 *         Управление ботом JS         *
 ***************************************/
let botUsers = []; // Список Telegram-пользователей
let selectedBotUserId = null;

document.addEventListener("DOMContentLoaded", () => {
  checkAuthorization();
  loadBotUsers();
  initializeEventListeners();
  const savedTheme = localStorage.getItem("appTheme") || "light";
  document.documentElement.setAttribute("data-theme", savedTheme);

});

/** Проверка авторизации */
function checkAuthorization() {
  const isAuth = localStorage.getItem("auth");
  const userRole = localStorage.getItem("role");
  if (isAuth !== "true" || userRole !== "Администратор") {
    window.location.href = "../../Login.html";
  }
}

/** Инициализация обработчиков событий */
function initializeEventListeners() {
  // Кнопка "Назад"
  document.getElementById("backButton").addEventListener("click", () => {
    window.location.href = "../Admin.html";
  });
  
  // Кнопка "Выход"
  document.getElementById("exitBtn").addEventListener("click", () => {
    localStorage.removeItem("auth");
    localStorage.removeItem("role");
    window.location.href = "../../Login.html";
  });
  
  // Обработчики для кнопок добавления, редактирования и удаления
  document.getElementById("addBtn").addEventListener("click", handleAddBotUser);
  document.getElementById("editBtn").addEventListener("click", handleEditBotUser);
  document.getElementById("deleteBtn").addEventListener("click", handleDeleteBotUser);
  
  // Поиск
  document.getElementById("searchInput").addEventListener("input", () => {
    const query = document.getElementById("searchInput").value.toLowerCase();
    const filtered = botUsers.filter(user =>
      user.telegramId.toString().includes(query) ||
      user.role.toLowerCase().includes(query)
    );
    renderBotUsers(filtered);
  });
  
  // Закрытие уведомления (toast)
  document.getElementById("notificationClose").addEventListener("click", hideNotification);
}

/** Загрузка списка Telegram-пользователей с сервера */
function loadBotUsers() {
  fetch("http://localhost:8080/api/managebot")
    .then(resp => resp.json())
    .then(data => {
      botUsers = data;
      renderBotUsers(botUsers);
    })
    .catch(err => {
      showNotification("Ошибка загрузки списка пользователей", "error");
      console.error(err);
    });
}

/** Отрисовка таблицы пользователей */
function renderBotUsers(users) {
  const tbody = document.querySelector("#usersTable tbody");
  tbody.innerHTML = "";
  
  if (users.length === 0) {
    document.getElementById("emptyState").style.display = "block";
  } else {
    document.getElementById("emptyState").style.display = "none";
  }
  
  users.forEach(user => {
    const tr = document.createElement("tr");
    // Храним ID в data-атрибуте, но не отображаем
    tr.dataset.userId = user.id; 

    tr.innerHTML = `
      <td>${user.telegramId}</td>
      <td>${user.role}</td>
    `;
    
    tr.addEventListener("click", () => {
      document.querySelectorAll("#usersTable tbody tr").forEach(row => row.classList.remove("selected"));
      tr.classList.add("selected");
      selectedBotUserId = tr.dataset.userId; // Берём ID из data-атрибута

      // Заполняем поля формы
      document.getElementById("telegramId").value = user.telegramId;
      document.getElementById("role").value = user.role;
    });
    tbody.appendChild(tr);
  });
}

/** Добавление пользователя */
function handleAddBotUser() {
  const telegramId = document.getElementById("telegramId").value.trim();
  const role = document.getElementById("role").value;
  
  if (!telegramId || !role) {
    showNotification("Заполните все поля", "error");
    return;
  }
  
  const userData = { telegramId, role };
  
  fetch("http://localhost:8080/api/managebot", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(userData)
  })
    .then(resp => resp.json())
    .then(data => {
      showNotification(data.message || "Пользователь добавлен", "success");
      loadBotUsers();
      clearForm();
    })
    .catch(err => {
      console.error(err);
      showNotification("Ошибка при добавлении пользователя", "error");
    });
}

/** Редактирование пользователя */
function handleEditBotUser() {
  if (!selectedBotUserId) {
    showNotification("Сначала выберите пользователя", "error");
    return;
  }
  
  const telegramId = document.getElementById("telegramId").value.trim();
  const role = document.getElementById("role").value;
  
  if (!telegramId || !role) {
    showNotification("Заполните все поля", "error");
    return;
  }
  
  const userData = { telegramId, role };
  
  fetch(`http://localhost:8080/api/managebot/${selectedBotUserId}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(userData)
  })
    .then(resp => resp.json())
    .then(data => {
      showNotification(data.message || "Пользователь обновлён", "success");
      loadBotUsers();
      clearForm();
    })
    .catch(err => {
      console.error(err);
      showNotification("Ошибка при обновлении пользователя", "error");
    });
}

/** Удаление пользователя */
function handleDeleteBotUser() {
  if (!selectedBotUserId) {
    showNotification("Сначала выберите пользователя", "error");
    return;
  }
  
  fetch(`http://localhost:8080/api/managebot/${selectedBotUserId}`, {
    method: "DELETE"
  })
    .then(resp => resp.json())
    .then(data => {
      showNotification(data.message || "Пользователь удалён", "success");
      loadBotUsers();
      clearForm();
    })
    .catch(err => {
      console.error(err);
      showNotification("Ошибка при удалении пользователя", "error");
    });
}

/** Очистка формы */
function clearForm() {
  document.getElementById("telegramId").value = "";
  document.getElementById("role").value = "";
  selectedBotUserId = null;
  document.querySelectorAll("#usersTable tbody tr").forEach(row => row.classList.remove("selected"));
}

/** Показ уведомления (toast) */
function showNotification(message, type = "info") {
  const notification = document.getElementById("notification");
  const notificationIcon = document.getElementById("notificationIcon");
  const notificationMessage = document.getElementById("notificationMessage");
  
  notificationMessage.textContent = message;
  
  switch (type) {
    case "success":
      notificationIcon.innerHTML = '<i class="ri-checkbox-circle-line"></i>';
      notificationIcon.style.color = "#2ecc71";
      break;
    case "error":
      notificationIcon.innerHTML = '<i class="ri-close-circle-line"></i>';
      notificationIcon.style.color = "#e53e3e";
      break;
    default:
      notificationIcon.innerHTML = '<i class="ri-information-line"></i>';
      notificationIcon.style.color = "var(--primary-color)";
      break;
  }
  
  notification.classList.add("show");
  
  setTimeout(() => {
    hideNotification();
  }, 3000);
}

/** Скрытие уведомления */
function hideNotification() {
  const notification = document.getElementById("notification");
  notification.classList.remove("show");
}
