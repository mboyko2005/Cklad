let usersData = [];      // Список пользователей
let rolesData = [];      // Список ролей
let selectedUserId = null;
let isEditMode = false;  // false = добавление, true = редактирование

document.addEventListener("DOMContentLoaded", () => {
  checkAuthorization();
  loadRoles();
  loadUsers();
  initializeEventListeners();
});

/** Проверка авторизации */
function checkAuthorization() {
  const isAuth = localStorage.getItem("auth");
  const userRole = localStorage.getItem("role");
  // Если не админ, переходим на логин
  if (isAuth !== "true" || userRole !== "Администратор") {
    window.location.href = "../../Login.html";
  }
}

/** Инициализация всех кнопок и т.п. */
function initializeEventListeners() {
  // Кнопка "Назад" (стрелка)
  const backButton = document.getElementById("backButton");
  backButton.addEventListener("click", () => {
    // Возврат на страницу Admin.html
    window.location.href = "../Admin.html";
  });

  // Кнопка "Выход"
  const exitBtn = document.getElementById("exitBtn");
  exitBtn.addEventListener("click", () => {
    // Сброс авторизации
    localStorage.removeItem("auth");
    localStorage.removeItem("role");
    window.location.href = "../../Login.html";
  });

  // Добавление пользователя
  document.getElementById("addUserBtn").addEventListener("click", () => {
    isEditMode = false;
    selectedUserId = null;
    openUserModal("Добавить пользователя");
  });

  // Редактирование пользователя
  document.getElementById("editUserBtn").addEventListener("click", handleEditUser);

  // Удаление пользователя
  document.getElementById("deleteUserBtn").addEventListener("click", handleDeleteUser);

  // Закрытие модалки (крестик)
  document.getElementById("closeModal").addEventListener("click", () => {
    document.getElementById("userModal").style.display = "none";
  });

  // Кнопка "Отмена" в модалке
  document.getElementById("cancelBtn").addEventListener("click", () => {
    document.getElementById("userModal").style.display = "none";
  });

  // Сохранение (Добавить / Редактировать)
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

  // Закрытие модального окна при клике вне контента
  window.addEventListener("click", (event) => {
    const userModal = document.getElementById("userModal");
    if (event.target === userModal) {
      userModal.style.display = "none";
    }
    const confirmModal = document.getElementById("confirmModal");
    if (event.target === confirmModal) {
      confirmModal.style.display = "none";
    }
  });

  // Обработчики кнопок подтверждения
  document.getElementById("confirmYesBtn").addEventListener("click", () => {
    // Подтвердили удаление
    if (typeof confirmCallback === "function") {
      confirmCallback(true);
    }
    closeConfirmModal();
  });
  document.getElementById("confirmNoBtn").addEventListener("click", () => {
    // Отменили
    if (typeof confirmCallback === "function") {
      confirmCallback(false);
    }
    closeConfirmModal();
  });

  // Закрытие уведомления
  document.getElementById("notificationClose").addEventListener("click", () => {
    hideNotification();
  });
}

/** Загрузка ролей */
function loadRoles() {
  // Если страница открывается на http://localhost:8080/...
  // меняем путь на полный:
  fetch("http://localhost:8080/api/manageusers/roles")
    .then(resp => resp.json())
    .then(data => {
      rolesData = data;
    })
    .catch(err => {
      showNotification("Ошибка загрузки ролей", "error");
      console.error(err);
    });
}

/** Загрузка списка пользователей */
function loadUsers() {
  fetch("http://localhost:8080/api/manageusers")
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

/** Отрисовка таблицы пользователей */
function renderUsersTable(users) {
  const tbody = document.querySelector("#usersTable tbody");
  tbody.innerHTML = "";
  users.forEach(user => {
    const tr = document.createElement("tr");
    tr.innerHTML = `
      <td>${user.userID}</td>
      <td>${user.username}</td>
      <td>${user.roleName}</td>
    `;
    // Выделение строки
    tr.addEventListener("click", () => {
      document.querySelectorAll("#usersTable tbody tr").forEach(row => row.classList.remove("selected"));
      tr.classList.add("selected");
      selectedUserId = user.userID;
    });
    tbody.appendChild(tr);
  });
}

/** Открытие модального окна (добавить/редактировать) */
function openUserModal(title) {
  document.getElementById("userModal").style.display = "flex";
  document.getElementById("modalTitle").textContent = title;

  // Сброс полей
  document.getElementById("usernameInput").value = "";
  document.getElementById("passwordInput").value = "";
  document.getElementById("confirmPasswordInput").value = "";

  // Заполнение списка ролей
  const roleSelect = document.getElementById("roleSelect");
  roleSelect.innerHTML = "";
  rolesData.forEach(role => {
    const opt = document.createElement("option");
    opt.value = role.id;
    opt.textContent = role.name;
    roleSelect.appendChild(opt);
  });

  // Если редактируем — подставляем
  if (isEditMode && selectedUserId) {
    const user = usersData.find(u => u.userID === selectedUserId);
    if (user) {
      document.getElementById("usernameInput").value = user.username;
      // Пароль не показываем
      roleSelect.value = user.roleID;
    }
  }
}

/** Обработка "Редактировать" */
function handleEditUser() {
  if (!selectedUserId) {
    showNotification("Сначала выберите пользователя", "error");
    return;
  }
  isEditMode = true;
  openUserModal("Редактировать пользователя");
}

/** Обработка "Удалить" */
function handleDeleteUser() {
  if (!selectedUserId) {
    showNotification("Сначала выберите пользователя", "error");
    return;
  }
  openConfirmModal("Удаление пользователя", "Вы действительно хотите удалить выбранного пользователя?", (confirmed) => {
    if (confirmed) {
      // Выполняем удаление
      fetch(`http://localhost:8080/api/manageusers/${selectedUserId}`, {
        method: "DELETE"
      })
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
  });
}

/** Обработка "Сохранить" (добавление/редактирование) */
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
    // Редактируем
    fetch(`http://localhost:8080/api/manageusers/${selectedUserId}`, {
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
    // Добавляем
    fetch("http://localhost:8080/api/manageusers", {
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

/* ======= НИЖЕ - функционал подтверждения (confirm modal) и уведомлений (notification) ======= */
let confirmCallback = null;

/** Открыть модалку подтверждения */
function openConfirmModal(title, text, callback) {
  confirmCallback = callback;
  document.getElementById("confirmTitle").textContent = title;
  document.getElementById("confirmText").textContent = text;
  document.getElementById("confirmModal").style.display = "flex";
}
/** Закрыть модалку подтверждения */
function closeConfirmModal() {
  document.getElementById("confirmModal").style.display = "none";
  confirmCallback = null;
}

/** Показать уведомление (type: 'success' | 'error' | 'info') */
function showNotification(message, type = "info") {
  const notification = document.getElementById("notification");
  const notificationIcon = document.getElementById("notificationIcon");
  const notificationMessage = document.getElementById("notificationMessage");

  notificationMessage.textContent = message;

  // Меняем иконку и цвет в зависимости от типа
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

  // Скрыть через 3 сек
  setTimeout(() => {
    hideNotification();
  }, 3000);
}

/** Скрыть уведомление */
function hideNotification() {
  const notification = document.getElementById("notification");
  notification.classList.remove("show");
}
