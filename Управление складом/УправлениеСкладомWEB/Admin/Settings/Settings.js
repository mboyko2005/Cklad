document.addEventListener("DOMContentLoaded", () => {
  // Получаем имя пользователя из localStorage (устанавливается после авторизации)
  const username = localStorage.getItem("username") || "";
  if (!username) {
    console.warn("Не удалось определить пользователя. Используем гостевую тему.");
  }

  // Читаем тему из localStorage, используя ключ с учётом имени пользователя
  const savedTheme = localStorage.getItem(`appTheme-${username}`) || "light";
  document.documentElement.setAttribute("data-theme", savedTheme);

  // Устанавливаем значение в select (если он есть)
  const themeSelect = document.getElementById("themeSelect");
  if (themeSelect) {
    themeSelect.value = savedTheme;
  }
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

    // Проверяем длину нового пароля
    if (newPass !== "") {
      newPasswordStatus.style.display = "block";
      newPasswordStatus.className = "fa-solid " + (newPass.length >= 4 ? "fa-check-circle" : "fa-times-circle");
      newPasswordStatus.style.color = newPass.length >= 4 ? "#38a169" : "#e53e3e";
    } else {
      newPasswordStatus.style.display = "none";
    }

    // Проверяем совпадение с подтверждением
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

  // Сохранение темы (по кнопке "Сохранить настройки")
  document.getElementById("saveSettingsBtn").addEventListener("click", async () => {
    const selectedTheme = themeSelect.value;
    try {
      const response = await fetch("/api/settings/theme", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ Theme: selectedTheme })
      });
      const data = await response.json();
      if (data.success) {
        localStorage.setItem(`appTheme-${username}`, selectedTheme);
        document.documentElement.setAttribute("data-theme", selectedTheme);
        if (themeSelect) themeSelect.value = selectedTheme;
        updateThemeIcon(selectedTheme);
        showNotification("Тема сохранена", "success");
      } else {
        showNotification(data.message || "Ошибка при сохранении темы", "error");
      }
    } catch (err) {
      console.error("Ошибка при сохранении темы:", err);
      showNotification("Ошибка соединения с сервером", "error");
    }
  });

  // Переключение темы по круглой кнопке
  document.getElementById("themeToggleBtn").addEventListener("click", async () => {
    let currentTheme = localStorage.getItem(`appTheme-${username}`) || "light";
    let newTheme = currentTheme === "light" ? "dark" : "light";
    try {
      const response = await fetch("/api/settings/theme", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ Theme: newTheme })
      });
      const data = await response.json();
      if (data.success) {
        localStorage.setItem(`appTheme-${username}`, newTheme);
        document.documentElement.setAttribute("data-theme", newTheme);
        if (themeSelect) themeSelect.value = newTheme;
        updateThemeIcon(newTheme);
        showNotification("Тема переключена: " + newTheme, "success");
      } else {
        showNotification(data.message || "Ошибка при переключении темы", "error");
      }
    } catch (err) {
      console.error("Ошибка при переключении темы:", err);
      showNotification("Ошибка соединения с сервером", "error");
    }
  });

  // Смена пароля
  document.getElementById("savePasswordBtn").addEventListener("click", async () => {
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
    if (!username) {
      showNotification("Не удалось определить пользователя. Повторите вход.", "error");
      return;
    }
    try {
      const response = await fetch("/api/settings/changepassword", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ 
          Username: username,
          NewPassword: newPass, 
          ConfirmPassword: confPass 
        })
      });
      const data = await response.json();
      if (data.success) {
        showNotification("Пароль изменён", "success");
        newPasswordInput.value = "";
        confirmPasswordInput.value = "";
        newPasswordStatus.style.display = "none";
        confirmPasswordStatus.style.display = "none";
      } else {
        showNotification(data.message || "Ошибка при смене пароля", "error");
      }
    } catch (err) {
      console.error("Ошибка при смене пароля:", err);
      showNotification("Ошибка соединения с сервером", "error");
    }
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

// Обновление иконки темы (солнышко/луна)
function updateThemeIcon(theme) {
  const themeIcon = document.getElementById("themeIcon");
  if (themeIcon) {
    themeIcon.className = theme === "dark" 
      ? "fa-solid fa-moon" 
      : "fa-solid fa-sun";
  }
}
