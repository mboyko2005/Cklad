document.addEventListener("DOMContentLoaded", () => {
    // Проверяем авторизацию и роль
    const isAuth = localStorage.getItem("auth");
    const userRole = localStorage.getItem("role");
  
    if (isAuth !== "true" || userRole !== "Сотрудник склада") {
      window.location.href = "../Login.html";
      return;
    }
  
    // Находим элементы-карточки
    const viewGoodsCard = document.getElementById("viewGoodsCard");
    const manageStocksCard = document.getElementById("manageStocksCard");
    const moveGoodsCard = document.getElementById("moveGoodsCard");
    const inoutAccountingCard = document.getElementById("inoutAccountingCard");
    const exitCard = document.getElementById("exitCard");
  
    // Пример обработчиков
    viewGoodsCard.addEventListener("click", () => {
      alert("Просмотр товаров — логика для сотрудника склада.");
    });
  
    manageStocksCard.addEventListener("click", () => {
      alert("Управление запасами — логика для сотрудника склада.");
    });
  
    moveGoodsCard.addEventListener("click", () => {
      alert("Перемещение товаров — логика для сотрудника склада.");
    });
  
    inoutAccountingCard.addEventListener("click", () => {
      alert("Учёт приходов/расходов — логика для сотрудника склада.");
    });
  
    // Выход: чистим localStorage и уходим на Login.html
    exitCard.addEventListener("click", () => {
      localStorage.removeItem("auth");
      localStorage.removeItem("role");
      window.location.href = "../Login.html";
    });
  });
  