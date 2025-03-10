document.addEventListener("DOMContentLoaded", () => {
    // Проверка localStorage
    const isAuth = localStorage.getItem("auth");
    const userRole = localStorage.getItem("role");
  
    // Если не авторизован или роль не "Менеджер", переходим на Login.html
    if (isAuth !== "true" || userRole !== "Менеджер") {
      window.location.href = "../Login.html";
      return;
    }
  
    // Инициализируем элементы
    const manageGoodsCard = document.getElementById("manageGoodsCard");
    const manageStaffCard = document.getElementById("manageStaffCard");
    const reportsCard = document.getElementById("reportsCard");
    const settingsCard = document.getElementById("settingsCard");
    const exitCard = document.getElementById("exitCard");
  
    manageGoodsCard.addEventListener("click", () => {
      alert("Управление Товаром — ваша логика здесь.");
    });
  
    manageStaffCard.addEventListener("click", () => {
      alert("Управление сотрудниками — ваша логика здесь.");
    });
  
    reportsCard.addEventListener("click", () => {
      alert("Просмотр отчётов — ваша логика здесь.");
    });
  
    settingsCard.addEventListener("click", () => {
      alert("Настройки — ваша логика здесь.");
    });
  
    // Кнопка "Выход"
    exitCard.addEventListener("click", () => {
      localStorage.removeItem("auth");
      localStorage.removeItem("role");
      window.location.href = "../Login.html";
    });
  });
  