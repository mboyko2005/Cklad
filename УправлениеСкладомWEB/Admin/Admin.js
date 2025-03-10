document.addEventListener("DOMContentLoaded", () => {
    // Проверяем localStorage
    const isAuth = localStorage.getItem("auth");
    const userRole = localStorage.getItem("role");
  
    // Если не авторизован или роль не "Администратор", отправляем на Login.html
    if (isAuth !== "true" || userRole !== "Администратор") {
      window.location.href = "../Login.html";
      return;
    }
  
    // Если проверку прошли, значит пользователь - Администратор
    const manageUsersCard = document.getElementById("manageUsersCard");
    const manageInventoryCard = document.getElementById("manageInventoryCard");
    const reportsCard = document.getElementById("reportsCard");
    const settingsCard = document.getElementById("settingsCard");
    const checkApiCard = document.getElementById("checkApiCard");
    const botCard = document.getElementById("botCard");
    const exitCard = document.getElementById("exitCard");
  
    manageUsersCard.addEventListener("click", () => {
      alert("Управление пользователями — ваша логика здесь.");
    });
  
    manageInventoryCard.addEventListener("click", () => {
      alert("Управление складскими позициями — ваша логика здесь.");
    });
  
    reportsCard.addEventListener("click", () => {
      alert("Отчёты — ваша логика здесь.");
    });
  
    settingsCard.addEventListener("click", () => {
      alert("Настройки — ваша логика здесь.");
    });
  
    checkApiCard.addEventListener("click", () => {
      alert("Проверка API — ваша логика здесь.");
    });
  
    botCard.addEventListener("click", () => {
      alert("Управление ботом — ваша логика здесь.");
    });
  
    // При нажатии на "Выход" удаляем из localStorage данные и переходим на Login.html
    exitCard.addEventListener("click", () => {
      localStorage.removeItem("auth");
      localStorage.removeItem("role");
      window.location.href = "../Login.html";
    });
  });
  