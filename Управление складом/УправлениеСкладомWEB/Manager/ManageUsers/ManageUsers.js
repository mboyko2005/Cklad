/***************************************
 *      Управление пользователями
 *      (Добавление, редактирование,
 *       удаление, поиск)
 ***************************************/

// Глобальные переменные
let usersData = [];      // Список пользователей, загруженных с сервера
let rolesData = [];      // Список ролей, загруженных с сервера
let selectedUserId = null; 
let isEditMode = false;  // false: Добавление, true: Редактирование

document.addEventListener("DOMContentLoaded", () => {
  checkAuthorization();
  loadRoles();
  loadUsers();
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

/** 
 * Проверка авторизации 
 * Если пользователь не авторизован ИЛИ роль не "Менеджер", 
 * отправляем на страницу логина.
 */
function checkAuthorization() {
  const isAuth = localStorage.getItem("auth");
  const userRole = localStorage.getItem("role");
  if (isAuth !== "true" || userRole !== "Менеджер") {
    window.location.href = "../../Login.html";
  }
}

/** Инициализация всех обработчиков событий */
function initializeEventListeners() {
  // Кнопка "Назад" (стрелка)
  const backButton = document.getElementById("backButton");
  backButton.addEventListener("click", () => {
    // Переходим на страницу Менеджера.html
    window.location.href = "../Manager.html";
  });

  // Кнопка "Выход"
  const exitBtn = document.getElementById("exitBtn");
  exitBtn.addEventListener("click", () => {
    // Сбрасываем авторизацию
    localStorage.removeItem("auth");
    localStorage.removeItem("role");
    window.location.href = "../../Login.html";
  });

  // Кнопка "Добавить пользователя"
  document.getElementById("addUserBtn").addEventListener("click", () => {
    isEditMode = false;
    selectedUserId = null;
    openUserModal("Добавить пользователя");
  });

  // Кнопка "Редактировать пользователя"
  document.getElementById("editUserBtn").addEventListener("click", handleEditUser);

  // Кнопка "Удалить пользователя"
  document.getElementById("deleteUserBtn").addEventListener("click", handleDeleteUser);

  // Закрытие модалки (крестик)
  document.getElementById("closeModal").addEventListener("click", () => {
    document.getElementById("userModal").style.display = "none";
  });

  // Кнопка "Отмена" в модалке
  document.getElementById("cancelBtn").addEventListener("click", () => {
    document.getElementById("userModal").style.display = "none";
  });

  // Сохранить изменения (Добавить / Редактировать)
  document.getElementById("saveUserBtn").addEventListener("click", handleSaveUser);

  // Поиск
  document.getElementById("searchInput").addEventListener("input", () => {
    const query = document.getElementById("searchInput").value.toLowerCase();
    const filtered = usersData.filter(u =>
      u.userID.toString().includes(query) ||
      u.username.toLowerCase().includes(query) ||
      u.roleName.toLowerCase().includes(query)
    );
    renderUsersTable(filtered);
  });

  // Закрытие модальных окон при клике вне их содержимого
  window.addEventListener("click", (event) => {
    const userModal = document.getElementById("userModal");
    const confirmModal = document.getElementById("confirmModal");
    if (event.target === userModal) {
      userModal.style.display = "none";
    }
    if (event.target === confirmModal) {
      confirmModal.style.display = "none";
    }
  });

  // Кнопки подтверждения в модалке удаления
  document.getElementById("confirmYesBtn").addEventListener("click", () => {
    if (typeof confirmCallback === "function") {
      confirmCallback(true);
    }
    closeConfirmModal();
  });

  document.getElementById("confirmNoBtn").addEventListener("click", () => {
    if (typeof confirmCallback === "function") {
      confirmCallback(false);
    }
    closeConfirmModal();
  });

  // Закрытие уведомления (toast)
  document.getElementById("notificationClose").addEventListener("click", () => {
    hideNotification();
  });
}

/** Загрузка списка ролей с сервера */
function loadRoles() {
  fetch("/api/manageusers/roles")
    .then(resp => resp.json())
    .then(data => {
      rolesData = data;
    })
    .catch(err => {
      showNotification("Ошибка загрузки ролей", "error");
      console.error(err);
    });
}

/** Загрузка списка пользователей с сервера */
function loadUsers() {
  fetch("/api/manageusers")
    .then(resp => resp.json())
    .then(data => {
      usersData = data;
      renderUsersTable(usersData);
    })
    .catch(err => {
      showNotification("Ошибка загрузки списка пользователей", "error");
      console.error(err);
    });
}

/** Отрисовка таблицы пользователей (только сотрудники склада) */
function renderUsersTable(users) {
  const tbody = document.querySelector("#usersTable tbody");
  tbody.innerHTML = "";

  // Фильтруем пользователей, оставляя только тех, у кого роль "Сотрудник склада"
  const filteredUsers = users.filter(user => user.roleName === "Сотрудник склада");

  filteredUsers.forEach(user => {
    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${user.userID}</td>
      <td>${user.username}</td>
      <td>${user.roleName}</td>
    `;
    // Клик по строке = выбор пользователя
    tr.addEventListener("click", () => {
      document.querySelectorAll("#usersTable tbody tr")
        .forEach(row => row.classList.remove("selected"));
      tr.classList.add("selected");
      selectedUserId = user.userID;
    });
    tbody.appendChild(tr);
  });
}

/** Открытие модалки для добавления или редактирования пользователя */
function openUserModal(title) {
  document.getElementById("userModal").style.display = "flex";
  document.getElementById("modalTitle").textContent = title;
  // Сброс полей
  document.getElementById("usernameInput").value = "";
  document.getElementById("passwordInput").value = "";
  document.getElementById("confirmPasswordInput").value = "";
  // Заполняем список ролей (только роль "Сотрудник склада")
  const roleSelect = document.getElementById("roleSelect");
  roleSelect.innerHTML = "";
  const allowedRoles = ["Сотрудник склада"];
  rolesData.forEach(role => {
    if (allowedRoles.includes(role.name)) {
      const opt = document.createElement("option");
      opt.value = role.id;
      opt.textContent = role.name;
      roleSelect.appendChild(opt);
    }
  });
  // Если редактируем — подставляем данные пользователя
  if (isEditMode && selectedUserId) {
    const user = usersData.find(u => u.userID === selectedUserId);
    if (user) {
      document.getElementById("usernameInput").value = user.username;
      // Пароль в целях безопасности не показываем
      roleSelect.value = user.roleID;
    }
  }
}

/** Обработка нажатия "Редактировать" */
function handleEditUser() {
  if (!selectedUserId) {
    showNotification("Сначала выберите пользователя", "error");
    return;
  }
  isEditMode = true;
  openUserModal("Редактировать пользователя");
}

/** Обработка нажатия "Удалить" */
function handleDeleteUser() {
  if (!selectedUserId) {
    showNotification("Сначала выберите пользователя", "error");
    return;
  }
  openConfirmModal(
    "Удаление пользователя",
    "Вы действительно хотите удалить выбранного пользователя?",
    (confirmed) => {
      if (confirmed) {
        fetch(`/api/manageusers/${selectedUserId}`, { method: "DELETE" })
          .then(resp => resp.json())
          .then(data => {
            showNotification(data.message || "Пользователь удалён", "success");
            loadUsers();
            selectedUserId = null;
          })
          .catch(err => {
            console.error("Ошибка при удалении пользователя:", err);
            showNotification("Ошибка при удалении пользователя", "error");
          });
      }
    }
  );
}

/** Обработка нажатия "Сохранить" (добавление/редактирование) */
function handleSaveUser() {
  const username = document.getElementById("usernameInput").value.trim();
  const password = document.getElementById("passwordInput").value;
  const confirmPassword = document.getElementById("confirmPasswordInput").value;
  const roleId = parseInt(document.getElementById("roleSelect").value, 10);
  if (!username || !password || !confirmPassword || !roleId) {
    showNotification("Заполните все поля", "error");
    return;
  }
  if (password !== confirmPassword) {
    showNotification("Пароли не совпадают", "error");
    return;
  }
  const userData = { username, password, roleId };
  if (isEditMode && selectedUserId) {
    // Редактирование
    fetch(`/api/manageusers/${selectedUserId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(userData)
    })
      .then(resp => resp.json())
      .then(data => {
        showNotification(data.message || "Пользователь обновлён", "success");
        document.getElementById("userModal").style.display = "none";
        loadUsers();
      })
      .catch(err => {
        console.error("Ошибка при редактировании:", err);
        showNotification("Ошибка при редактировании пользователя", "error");
      });
  } else {
    // Добавление
    fetch("/api/manageusers", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(userData)
    })
      .then(resp => resp.json())
      .then(data => {
        showNotification(data.message || "Пользователь добавлен", "success");
        document.getElementById("userModal").style.display = "none";
        loadUsers();
      })
      .catch(err => {
        console.error("Ошибка при добавлении пользователя:", err);
        showNotification("Ошибка при добавлении пользователя", "error");
      });
  }
}

/* =========================================================
   МОДАЛЬНОЕ ОКНО ПОДТВЕРЖДЕНИЯ (confirmModal)
   ========================================================= */
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

/* =========================================================
   УВЕДОМЛЕНИЯ (TOAST)
   ========================================================= */
function showNotification(message, type = "info") {
  const notification = document.getElementById("notification");
  const notificationIcon = document.getElementById("notificationIcon");
  const notificationMessage = document.getElementById("notificationMessage");
  notificationMessage.textContent = message;
  switch (type) {
    case "success":
      notificationIcon.className = "ri-checkbox-circle-line";
      notificationIcon.style.color = "#2ecc71";
      break;
    case "error":
      notificationIcon.className = "ri-close-circle-line";
      notificationIcon.style.color = "#e53e3e";
      break;
    default:
      notificationIcon.className = "ri-information-line";
      notificationIcon.style.color = "var(--primary-color)";
      break;
  }
  notification.classList.add("show");
  setTimeout(() => {
    hideNotification();
  }, 3000);
}
function hideNotification() {
  const notification = document.getElementById("notification");
  notification.classList.remove("show");
}
