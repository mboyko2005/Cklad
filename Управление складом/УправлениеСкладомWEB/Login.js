document.addEventListener("DOMContentLoaded", function () {
    // Если используете библиотеку AOS (для анимаций при прокрутке)
    // то инициализируем её, иначе можно удалить этот блок
    if (typeof AOS !== "undefined") {
        AOS.init({ once: true });
    } else {
        console.warn("AOS library is not loaded. Please include it in your HTML if needed.");
    }

    // Получаем необходимые элементы DOM
    const loginForm = document.getElementById("loginForm");       // Форма авторизации
    const loginMessage = document.getElementById("loginMessage"); // Блок для сообщений (ошибки/успех)
    const togglePassword = document.getElementById("togglePassword"); // Кнопка-глазик для показа/скрытия пароля
    const passwordInput = document.getElementById("password");    // Поле "Пароль"
    const usernameInput = document.getElementById("username");    // Поле "Логин"
    const rememberMeCheckbox = document.getElementById("rememberMe"); // Чекбокс "Запомнить меня"
    const preloader = document.getElementById("preloader");       // Прелоадер (если используется)

    /**
     * Функция показывающая прелоадер (если нужен).
     */
    function showPreloader() {
        if (!preloader) return;
        preloader.style.visibility = "visible";
        preloader.style.opacity = "1";
    }

    /**
     * Функция скрывающая прелоадер.
     */
    function hidePreloader() {
        if (!preloader) return;
        preloader.style.opacity = "0";
        setTimeout(() => {
            preloader.style.visibility = "hidden";
        }, 500);
    }

    /**
     * Скрываем прелоадер после загрузки страницы
     */
    window.addEventListener("load", function () {
        setTimeout(hidePreloader, 500);
    });

    /**
     * Логика переключения видимости пароля по клику на глазик
     */
    if (togglePassword && passwordInput) {
        togglePassword.addEventListener("click", function () {
            const type = passwordInput.getAttribute("type") === "password" ? "text" : "password";
            passwordInput.setAttribute("type", type);

            // Меняем иконки (fa-eye <-> fa-eye-slash), если используете FontAwesome
            const icon = this.querySelector("i");
            if (icon) {
                icon.classList.toggle("fa-eye");
                icon.classList.toggle("fa-eye-slash");

                // Добавим короткую анимацию
                icon.classList.add("fa-beat");
                setTimeout(() => {
                    icon.classList.remove("fa-beat");
                }, 300);
            }
        });
    }

    /**
     * Функция проверяет, есть ли сохранённый логин (Remember Me)
     */
    function checkSavedCredentials() {
        const savedUsername = localStorage.getItem("savedUsername");
        if (savedUsername && usernameInput) {
            usernameInput.value = savedUsername;
            rememberMeCheckbox.checked = true;
            // Чтобы триггернуть визуальное обновление (если нужно)
            usernameInput.dispatchEvent(new Event("input"));
        }
    }

    /**
     * Сохранение логина при включенном "Запомнить меня"
     */
    function saveUserCredentials() {
        if (!usernameInput) return;
        if (rememberMeCheckbox && rememberMeCheckbox.checked) {
            localStorage.setItem("savedUsername", usernameInput.value);
        } else {
            localStorage.removeItem("savedUsername");
        }
    }

    /**
     * Добавим возможность кликать на иконку внутри .input-icon, чтобы фокусироваться на поле
     */
    document.querySelectorAll('.input-icon').forEach(icon => {
        icon.addEventListener('click', function () {
            const input = this.parentElement.querySelector('input');
            if (input) input.focus();
        });
    });

    /**
     * Слушаем событие отправки формы
     */
    if (loginForm) {
        loginForm.addEventListener("submit", async function (event) {
            event.preventDefault();

            // Валидация полей
            const isValid = validateForm();
            if (!isValid) return;

            // Сохраняем логин, если чекбокс отмечен
            saveUserCredentials();

            // Собираем данные из полей
            const username = usernameInput.value.trim();
            const password = passwordInput.value.trim();

            // Очищаем сообщение перед запросом
            setLoginMessage("", "");

            // Показываем прелоадер
            showPreloader();

            try {
                // Отправляем запрос на авторизацию
                // ВАЖНО: используем относительный путь "/api/auth/login"
                // чтобы запрос шел на тот же домен, откуда загружен сайт
                const response = await fetch("/api/auth/login", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ username, password })
                });

                // Если сервер вернул статус не 200-299, бросаем ошибку
                if (!response.ok) {
                    throw new Error("Ошибка при авторизации");
                }

                // Разбираем JSON-ответ
                const data = await response.json();

                // Проверяем, что вернул сервер
                if (data.success) {
                    // Успешная авторизация
                    setLoginMessage("Успешный вход! Перенаправление...", "success");
                    localStorage.setItem("auth", "true");
                    localStorage.setItem("role", data.role);
                    localStorage.setItem("username", data.username);

                    // Запускаем анимацию успеха
                    animateSuccess();

                    // Через секунду перенаправляем по роли
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
                    // Ошибка авторизации (неверный логин/пароль и т.д.)
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
    }

    /**
     * Функция валидации формы (простая проверка на пустые поля)
     */
    function validateForm() {
        let isValid = true;

        if (usernameInput && usernameInput.value.trim() === "") {
            setLoginMessage("Пожалуйста, введите логин", "error");
            animateField(usernameInput);
            isValid = false;
        }

        if (passwordInput && passwordInput.value.trim() === "") {
            // Если уже выдана ошибка на логин, то не затираем её,
            // но всё равно анимируем поле пароля
            if (isValid) {
                setLoginMessage("Пожалуйста, введите пароль", "error");
            }
            animateField(passwordInput);
            isValid = false;
        }

        return isValid;
    }

    /**
     * Анимация поля при ошибке
     */
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

    /**
     * Установка сообщения об ошибке/успехе
     */
    function setLoginMessage(message, type) {
        if (!loginMessage) return;
        loginMessage.textContent = message;
        loginMessage.classList.remove("error-message", "success-message");
        if (type === "error") {
            loginMessage.classList.add("error-message");
        } else if (type === "success") {
            loginMessage.classList.add("success-message");
        }
    }

    /**
     * Анимация успеха (можно подцепить CSS-классы для эффекта)
     */
    function animateSuccess() {
        const loginCard = document.querySelector(".login-card");
        if (!loginCard) return;
        loginCard.classList.remove("error-animation");
        loginCard.classList.add("success-animation");
    }

    /**
     * Анимация ошибки (CSS-класс, который трясёт блок)
     */
    function animateError() {
        const loginCard = document.querySelector(".login-card");
        if (!loginCard) return;
        loginCard.classList.remove("success-animation");
        loginCard.classList.add("error-animation");
        setTimeout(() => {
            loginCard.classList.remove("error-animation");
        }, 500);
    }

    /**
     * При загрузке проверяем, не сохранился ли логин
     */
    checkSavedCredentials();

    /**
     * Вставим дополнительные стили (если нужно) для анимации "shake" и т.п.
     * Если у вас уже есть CSS с этими классами, можно не вставлять.
     */
    const styleSheet = document.createElement("style");
    styleSheet.textContent = `
      .error-shake {
        animation: shake 0.5s cubic-bezier(0.36, 0.07, 0.19, 0.97) both;
      }
      .input-error input {
        border-color: var(--error-color, #E53E3E) !important;
        box-shadow: 0 0 0 3px rgba(229, 62, 62, 0.2) !important;
      }
      @keyframes shake {
        10%, 90% { transform: translate3d(-1px, 0, 0); }
        20%, 80% { transform: translate3d(2px, 0, 0); }
        30%, 50%, 70% { transform: translate3d(-4px, 0, 0); }
        40%, 60% { transform: translate3d(4px, 0, 0); }
      }
    `;
    document.head.appendChild(styleSheet);
});
