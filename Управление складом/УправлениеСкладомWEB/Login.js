document.addEventListener("DOMContentLoaded", function() {
    // Инициализация AOS (если библиотека загружена)
    if (typeof AOS !== "undefined") {
        AOS.init({ once: true });
    } else {
        console.warn("AOS library is not loaded. Please include it in your HTML.");
    }
    
    // Элементы DOM
    const loginForm = document.getElementById("loginForm");
    const loginMessage = document.getElementById("loginMessage");
    const togglePassword = document.getElementById("togglePassword");
    const passwordInput = document.getElementById("password");
    const usernameInput = document.getElementById("username");
    const rememberMeCheckbox = document.getElementById("rememberMe");
    const preloader = document.getElementById("preloader");

    // Функция отображения прелоадера
    function showPreloader() {
        preloader.style.visibility = "visible";
        preloader.style.opacity = "1";
    }

    // Функция скрытия прелоадера
    function hidePreloader() {
        preloader.style.opacity = "0";
        setTimeout(() => {
            preloader.style.visibility = "hidden";
        }, 500);
    }

    // Изначально скрыть прелоадер после загрузки страницы
    window.addEventListener("load", function() {
        setTimeout(hidePreloader, 500);
    });

    // Обработка клика на переключателе видимости пароля
    togglePassword.addEventListener("click", function() {
        const type = passwordInput.getAttribute("type") === "password" ? "text" : "password";
        passwordInput.setAttribute("type", type);

        // Изменение иконки глаза
        const icon = this.querySelector("i");
        icon.classList.toggle("fa-eye");
        icon.classList.toggle("fa-eye-slash");

        // Анимация иконки
        icon.classList.add("fa-beat");
        setTimeout(() => {
            icon.classList.remove("fa-beat");
        }, 300);
    });

    // Проверка и заполнение сохраненных данных (Remember Me)
    function checkSavedCredentials() {
        const savedUsername = localStorage.getItem("savedUsername");
        if (savedUsername) {
            usernameInput.value = savedUsername;
            rememberMeCheckbox.checked = true;
            usernameInput.dispatchEvent(new Event("input"));
        }
    }

    // Сохранение логина при включенном "Запомнить меня"
    function saveUserCredentials() {
        if (rememberMeCheckbox.checked) {
            localStorage.setItem("savedUsername", usernameInput.value);
        } else {
            localStorage.removeItem("savedUsername");
        }
    }

    // Добавление эффекта фокуса при нажатии на иконку
    document.querySelectorAll('.input-icon').forEach(icon => {
        icon.addEventListener('click', function() {
            this.parentElement.querySelector('input').focus();
        });
    });

    // Отправка формы (авторизация)
    loginForm.addEventListener("submit", async function(event) {
        event.preventDefault();

        // Валидация перед отправкой
        const isValid = validateForm();
        if (!isValid) return;

        // Сохраняем данные, если отмечен чекбокс
        saveUserCredentials();

        // Получаем значения из формы
        const username = usernameInput.value.trim();
        const password = passwordInput.value.trim();

        // Очищаем сообщение
        setLoginMessage("", "");

        // Показываем прелоадер
        showPreloader();

        try {
            // Отправляем запрос на авторизацию с полным URL для localhost
            const response = await fetch("http://localhost:8080/api/auth/login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ username, password })
            });

            if (!response.ok) {
                throw new Error("Ошибка при авторизации");
            }

            const data = await response.json();

            if (data.success) {
                // Авторизация прошла – сохраняем данные
                setLoginMessage("Успешный вход! Перенаправление...", "success");
                localStorage.setItem("auth", "true");
                localStorage.setItem("role", data.role);
                localStorage.setItem("username", data.username);
                animateSuccess();
                // Перенаправляем в зависимости от роли
                setTimeout(() => {
                    switch (data.role) {
                        case "Администратор":
                            window.location.href = "Admin/Admin.html";
                            break;
                        case "Менеджер":
                            window.location.href = "manager/Manager.html";
                            break;
                        case "Сотрудник склада":
                            window.location.href = "staff/Staff.html";
                            break;
                        default:
                            setLoginMessage("Неизвестная роль пользователя.", "error");
                            animateError();
                    }
                }, 1000);
            } else {
                setLoginMessage(data.message || "Неправильный логин или пароль!", "error");
                animateError();
            }
        } catch (err) {
            console.error("Ошибка авторизации:", err);
            setLoginMessage("Ошибка соединения с сервером.", "error");
            animateError();
        } finally {
            hidePreloader();
        }
    });

    // Валидация формы
    function validateForm() {
        let isValid = true;

        if (usernameInput.value.trim() === "") {
            setLoginMessage("Пожалуйста, введите логин", "error");
            animateField(usernameInput);
            isValid = false;
        }

        if (passwordInput.value.trim() === "") {
            if (isValid) {
                setLoginMessage("Пожалуйста, введите пароль", "error");
            }
            animateField(passwordInput);
            isValid = false;
        }

        return isValid;
    }

    // Анимация поля с ошибкой
    function animateField(field) {
        field.classList.add("error-shake");
        field.parentElement.classList.add("input-error");

        setTimeout(() => {
            field.classList.remove("error-shake");
        }, 500);

        field.addEventListener("input", function removeErrorClass() {
            field.parentElement.classList.remove("input-error");
            field.removeEventListener("input", removeErrorClass);
        });
    }

    // Установка сообщения (ошибка / успех)
    function setLoginMessage(message, type) {
        loginMessage.textContent = message;
        loginMessage.classList.remove("error-message", "success-message");
        if (type === "error") {
            loginMessage.classList.add("error-message");
        } else if (type === "success") {
            loginMessage.classList.add("success-message");
        }
    }

    // Анимация успеха
    function animateSuccess() {
        const loginCard = document.querySelector(".login-card");
        loginCard.classList.remove("error-animation");
        loginCard.classList.add("success-animation");
    }

    // Анимация ошибки
    function animateError() {
        const loginCard = document.querySelector(".login-card");
        loginCard.classList.remove("success-animation");
        loginCard.classList.add("error-animation");

        setTimeout(() => {
            loginCard.classList.remove("error-animation");
        }, 500);
    }

    // Проверяем, не сохранён ли логин
    checkSavedCredentials();

    // Дополнительные стили анимации ошибки
    const styleSheet = document.createElement("style");
    styleSheet.textContent = `
      .error-shake {
        animation: shake 0.5s cubic-bezier(0.36, 0.07, 0.19, 0.97) both;
      }
      .input-error input {
        border-color: var(--error-color) !important;
        box-shadow: 0 0 0 3px rgba(229, 62, 62, 0.2) !important;
      }
    `;
    document.head.appendChild(styleSheet);
});
