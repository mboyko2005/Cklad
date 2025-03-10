document.addEventListener("DOMContentLoaded", function() {
    const loginForm = document.getElementById("loginForm");
    const loginMessage = document.getElementById("loginMessage");

    // При сабмите формы отправляем логин/пароль на локальный сервер
    loginForm.addEventListener("submit", async function (event) {
        event.preventDefault(); // чтобы не перезагружать страницу

        const username = document.getElementById("username").value.trim();
        const password = document.getElementById("password").value.trim();

        loginMessage.textContent = "";

        try {
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
                // Сохраняем данные авторизации в localStorage
                localStorage.setItem("auth", "true");      // Флаг, что пользователь авторизован
                localStorage.setItem("role", data.role);   // Сохраняем роль

                // Перенаправляем на страницу в зависимости от роли
                if (data.role === "Администратор") {
                    window.location.href = "Admin/Admin.html";
                } else if (data.role === "Менеджер") {
                    window.location.href = "manager/Manager.html";
                } else if (data.role === "Сотрудник склада") {
                    window.location.href = "staff/Staff.html";
                } else {
                    loginMessage.textContent = "Неизвестная роль пользователя.";
                }
            } else {
                loginMessage.textContent = "Неправильный логин или пароль!";
            }

        } catch (err) {
            console.error(err);
            loginMessage.textContent = "Ошибка соединения с сервером.";
        }
    });
});
