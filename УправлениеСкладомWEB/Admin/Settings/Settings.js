document.addEventListener("DOMContentLoaded", () => {
  // Устанавливаем тему из localStorage при загрузке
  const savedTheme = localStorage.getItem("appTheme") || "light";
  document.documentElement.setAttribute("data-theme", savedTheme);
  const themeSelect = document.getElementById("themeSelect");
  themeSelect.value = savedTheme;
  updateThemeIcon(savedTheme);

  // Элементы для смены пароля и их валидация
  const newPasswordInput = document.getElementById("newPassword");
  const confirmPasswordInput = document.getElementById("confirmPassword");
  const newPasswordStatus = document.getElementById("newPasswordStatus");
  const confirmPasswordStatus = document.getElementById("confirmPasswordStatus");

  newPasswordInput.addEventListener("input", validatePasswordFields);
  confirmPasswordInput.addEventListener("input", validatePasswordFields);

  function validatePasswordFields() {
    const newPass = newPasswordInput.value.trim();
    const confPass = confirmPasswordInput.value.trim();
    if (newPass !== "") {
      newPasswordStatus.style.display = "block";
      newPasswordStatus.className = "fa-solid " + (newPass.length >= 4 ? "fa-check-circle" : "fa-times-circle");
      newPasswordStatus.style.color = newPass.length >= 4 ? "#38a169" : "#e53e3e";
    } else {
      newPasswordStatus.style.display = "none";
    }

    if (confPass !== "") {
      confirmPasswordStatus.style.display = "block";
      if (newPass === confPass && newPass !== "") {
        confirmPasswordStatus.className = "fa-solid fa-check-circle";
        confirmPasswordStatus.style.color = "#38a169";
      } else {
        confirmPasswordStatus.className = "fa-solid fa-times-circle";
        confirmPasswordStatus.style.color = "#e53e3e";
      }
    } else {
      confirmPasswordStatus.style.display = "none";
    }
  }

  // Сохранение темы
  document.getElementById("saveSettingsBtn").addEventListener("click", () => {
    const selectedTheme = themeSelect.value;
    localStorage.setItem("appTheme", selectedTheme);
    document.documentElement.setAttribute("data-theme", selectedTheme);
    showNotification("Тема сохранена", "success");
  });

  // Переключение темы по кнопке
  document.getElementById("themeToggleBtn").addEventListener("click", () => {
    let currentTheme = localStorage.getItem("appTheme") || "light";
    let newTheme = currentTheme === "light" ? "dark" : "light";
    localStorage.setItem("appTheme", newTheme);
    document.documentElement.setAttribute("data-theme", newTheme);
    themeSelect.value = newTheme;
    showNotification("Тема сохранена: " + newTheme, "success");
    updateThemeIcon(newTheme);
  });

  // Смена пароля
  document.getElementById("savePasswordBtn").addEventListener("click", () => {
    const newPass = newPasswordInput.value.trim();
    const confPass = confirmPasswordInput.value.trim();
    if (!newPass || !confPass) {
      showNotification("Пароль не может быть пустым", "error");
      return;
    }
    if (newPass !== confPass) {
      showNotification("Пароли не совпадают", "error");
      return;
    }
    // Получаем имя пользователя из localStorage – оно должно быть установлено после авторизации
    const username = localStorage.getItem("username") || "";
    if (!username) {
      showNotification("Не удалось определить пользователя. Повторите вход.", "error");
      return;
    }
    // Отправка запроса на смену пароля с полным URL для localhost
    fetch("http://localhost:8080/api/settings/changepassword", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ 
        Username: username,
        NewPassword: newPass, 
        ConfirmPassword: confPass 
      })
    })
      .then(response => response.json())
      .then(data => {
        if (data.success) {
          showNotification("Пароль изменён", "success");
          newPasswordInput.value = "";
          confirmPasswordInput.value = "";
          newPasswordStatus.style.display = "none";
          confirmPasswordStatus.style.display = "none";
        } else {
          showNotification(data.message || "Ошибка при смене пароля", "error");
        }
      })
      .catch(err => {
        console.error("Ошибка:", err);
        showNotification("Ошибка соединения с сервером", "error");
      });
  });

  // Обработчик кнопки "Назад"
  document.getElementById("backButton").addEventListener("click", () => {
    window.history.back();
  });

  // Выход из системы
  document.getElementById("exitBtn").addEventListener("click", () => {
    localStorage.removeItem("auth");
    localStorage.removeItem("role");
    localStorage.removeItem("username");
    window.location.href = "../../Login.html";
  });

  // Закрытие уведомления по клику на крестик
  document.getElementById("notificationClose").addEventListener("click", () => {
    hideNotification();
  });
});

// Функция отображения уведомления (toast)
function showNotification(message, type = "info") {
  const notification = document.getElementById("notificationToast");
  const notificationMessage = document.getElementById("notificationMessage");
  notificationMessage.textContent = message;
  notification.classList.add("show");
  setTimeout(() => {
    hideNotification();
  }, 3000);
}

// Функция скрытия уведомления
function hideNotification() {
  const notification = document.getElementById("notificationToast");
  notification.classList.remove("show");
}

// Обновление иконки темы
function updateThemeIcon(theme) {
  const themeIcon = document.getElementById("themeIcon");
  if (themeIcon) {
    themeIcon.className = theme === "dark" ? "fa-solid fa-moon" : "fa-solid fa-sun";
  }
}
