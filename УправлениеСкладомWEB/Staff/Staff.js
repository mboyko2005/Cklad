document.addEventListener("DOMContentLoaded", () => {
  // Проверка авторизации и роли
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

  // Кнопка выхода только в верхнем меню (карточку "Выход" убрали)
  const exitBtn = document.getElementById("exitBtn");

  // Назначаем обработчики для карточек
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

  // Обработчик для кнопки "Выход"
  exitBtn.addEventListener("click", handleExit);
});

/** Функция выхода из системы */
function handleExit() {
  localStorage.removeItem("auth");
  localStorage.removeItem("role");
  window.location.href = "../Login.html";
}
