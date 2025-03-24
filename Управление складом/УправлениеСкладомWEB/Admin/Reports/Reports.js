document.addEventListener("DOMContentLoaded", () => {
  // Получаем имя пользователя из localStorage
  const username = localStorage.getItem("username") || "";
  // Формируем ключ для темы конкретного пользователя
  const themeKey = `appTheme-${username}`;
  // Считываем тему (если нет, по умолчанию "light")
  const savedTheme = localStorage.getItem(themeKey) || "light";
  // Применяем тему на странице
  document.documentElement.setAttribute("data-theme", savedTheme);

  const reportTypeSelect = document.getElementById("reportType");
  const chartTypeSelect = document.getElementById("chartType");
  const generateReportBtn = document.getElementById("generateReportBtn");
  const exportReportBtn = document.getElementById("exportReportBtn");
  const exportFormatSelect = document.getElementById("exportFormat");
  const chartCanvas = document.getElementById("reportChart");
  const chartTitleElement = document.getElementById("chartTitle");
  const fullscreenBtn = document.getElementById("fullscreenBtn");
  const chartContainer = document.getElementById("chartContainer");

  // Добавляем CORS-атрибут к canvas для Safari
  chartCanvas.setAttribute("crossorigin", "anonymous");

  let myChart = null;

  // Функция уведомления (toast)
  function showNotification(message, type = "info") {
    const notification = document.getElementById("notification");
    const notificationIcon = document.getElementById("notificationIcon");
    const notificationMessage = document.getElementById("notificationMessage");

    if (!notification || !notificationMessage) return;
    notificationMessage.textContent = message;
    if (notificationIcon) {
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
    }
    notification.classList.add("show");
    setTimeout(() => {
      notification.classList.remove("show");
    }, 3000);
  }

  // Функция для получения данных отчёта с сервера через fetch
  async function fetchReportData(reportType) {
    let endpoint = "/api/reports/";
    switch (reportType) {
      case "mostSold":
        endpoint += "mostSoldProducts";
        break;
      case "systemUsers":
        endpoint += "systemUsers";
        break;
      case "totalCost":
        endpoint += "totalCost";
        break;
      case "currentStock":
        endpoint += "currentStock";
        break;
      default:
        throw new Error("Неверный тип отчёта");
    }
    const response = await fetch(endpoint);
    if (!response.ok) {
      throw new Error("Ошибка загрузки данных");
    }
    return response.json();
  }

  // Генерация диаграммы
  async function generateChart() {
    const reportType = reportTypeSelect.value;
    const chartType = chartTypeSelect.value;
    try {
      const reportData = await fetchReportData(reportType);

      // Ожидаемый формат ответа от сервера:
      // {
      //   "labels": [...],
      //   "data": [...],
      //   "title": "Отчёт",
      //   "xTitle": "X",
      //   "yTitle": "Y"
      // }
      const labels = reportData.labels;
      const data = reportData.data;
      const title = reportData.title || "Отчёт";
      const xTitle = reportData.xTitle || "";
      const yTitle = reportData.yTitle || "";

      chartTitleElement.textContent = title;

      // Если диаграмма уже создана – уничтожаем
      if (myChart) {
        myChart.destroy();
      }
      const ctx = chartCanvas.getContext("2d");

      let chartConfigType;
      let additionalOptions = {};
      switch (chartType) {
        case "bar":
          chartConfigType = "bar";
          break;
        case "pie":
          chartConfigType = "pie";
          break;
        case "line":
          chartConfigType = "line";
          break;
        case "doughnut":
          chartConfigType = "doughnut";
          break;
        case "radar":
          chartConfigType = "radar";
          break;
        case "polarArea":
          chartConfigType = "polarArea";
          break;
        case "horizontalBar":
          chartConfigType = "bar";
          additionalOptions.indexAxis = "y";
          break;
        case "area":
          chartConfigType = "line";
          additionalOptions.fill = true;
          break;
        default:
          chartConfigType = "bar";
      }

      const datasetFill = (chartType === "line") ? false : true;
      const datasetTension = (chartType === "line" || chartType === "area") ? 0.1 : 0;

      const colorSet = [
        "rgba(58, 123, 213, 0.5)",
        "rgba(0, 210, 255, 0.5)",
        "rgba(255, 159, 64, 0.5)",
        "rgba(75, 192, 192, 0.5)",
        "rgba(153, 102, 255, 0.5)"
      ];
      const borderColorSet = [
        "rgba(58, 123, 213, 1)",
        "rgba(0, 210, 255, 1)",
        "rgba(255, 159, 64, 1)",
        "rgba(75, 192, 192, 1)",
        "rgba(153, 102, 255, 1)"
      ];

      const isCircularType = ["pie", "doughnut", "radar", "polarArea"].includes(chartConfigType);
      const datasetBackground = isCircularType ? colorSet : "rgba(58, 123, 213, 0.5)";
      const datasetBorder = isCircularType ? borderColorSet : "rgba(58, 123, 213, 1)";

      const options = {
        responsive: true,
        scales: isCircularType ? {} : {
          y: {
            beginAtZero: true,
            title: { display: Boolean(yTitle), text: yTitle }
          },
          x: {
            title: { display: Boolean(xTitle), text: xTitle }
          }
        }
      };
      Object.assign(options, additionalOptions);

      const config = {
        type: chartConfigType,
        data: {
          labels: labels,
          datasets: [{
            label: title,
            data: data,
            backgroundColor: datasetBackground,
            borderColor: datasetBorder,
            borderWidth: 1,
            fill: datasetFill,
            tension: datasetTension
          }]
        },
        options: options
      };

      myChart = new Chart(ctx, config);
    } catch (error) {
      showNotification(error.message, "error");
    }
  }

  generateReportBtn.addEventListener("click", generateChart);

  // Функции экспорта отчёта
  function exportToPng() {
    if (!myChart) {
      showNotification("Сначала сгенерируйте отчёт!", "error");
      return;
    }
    const url = myChart.toBase64Image();
    const link = document.createElement("a");
    link.href = url;
    link.download = "report.png";
    link.click();
  }

  async function exportToPdf() {
    if (!myChart) {
      showNotification("Сначала сгенерируйте отчёт!", "error");
      return;
    }
    const { jsPDF } = window.jspdf;
    const imgData = myChart.toBase64Image();
    const pdf = new jsPDF("landscape", "pt", "a4");
    pdf.addImage(imgData, "PNG", 30, 30, 800, 600);
    pdf.save("report.pdf");
  }

  async function exportToExcel() {
    if (!myChart) {
      showNotification("Сначала сгенерируйте отчёт!", "error");
      return;
    }
    const imgData = myChart.toBase64Image();
    const workbook = new ExcelJS.Workbook();
    const worksheet = workbook.addWorksheet("Report");
    worksheet.getCell("A1").value = "Отчёт: " + chartTitleElement.textContent;
    worksheet.getCell("A2").value = "Дата создания: " + new Date().toLocaleString();
    const imageId = workbook.addImage({ base64: imgData, extension: "png" });
    worksheet.addImage(imageId, "A4:F20");
    const buffer = await workbook.xlsx.writeBuffer();
    saveAs(new Blob([buffer]), "report.xlsx");
  }

  async function exportToWord() {
    if (!myChart) {
      showNotification("Сначала сгенерируйте отчёт!", "error");
      return;
    }
    const base64Image = myChart.toBase64Image().replace(/^data:image\/png;base64,/, "");
    const doc = new docx.Document({
      sections: [{
        properties: {},
        children: [
          new docx.Paragraph({
            text: "Отчёт: " + chartTitleElement.textContent,
            heading: docx.HeadingLevel.HEADING_1,
            alignment: docx.AlignmentType.CENTER
          }),
          new docx.Paragraph({
            text: "Дата создания: " + new Date().toLocaleString(),
            alignment: docx.AlignmentType.CENTER
          }),
          new docx.Paragraph({ text: "", spacing: { after: 300 } }),
          new docx.Paragraph({
            children: [
              new docx.ImageRun({
                data: base64Image,
                transformation: { width: 800, height: 600 }
              })
            ],
            alignment: docx.AlignmentType.CENTER
          })
        ]
      }]
    });
    docx.Packer.toBlob(doc).then((blob) => {
      saveAs(blob, "report.docx");
    });
  }

  exportReportBtn.addEventListener("click", () => {
    const format = exportFormatSelect.value;
    switch (format) {
      case "png": exportToPng(); break;
      case "pdf": exportToPdf(); break;
      case "excel": exportToExcel(); break;
      case "word": exportToWord(); break;
      default: showNotification("Неподдерживаемый формат экспорта.", "error");
    }
  });

  // Кнопка "Назад"
  document.getElementById("backButton").addEventListener("click", () => {
    window.history.back();
  });

  // Кнопка "Выход из системы"
  document.getElementById("logoutBtn").addEventListener("click", () => {
    localStorage.removeItem("auth");
    localStorage.removeItem("role");
    window.location.href = "/Login.html";
  });

  // Полноэкранный режим с учетом префиксов для Safari
  fullscreenBtn.addEventListener("click", () => {
    if (!document.fullscreenElement && !document.webkitFullscreenElement) {
      if (chartContainer.requestFullscreen) {
        chartContainer.requestFullscreen().catch(handleFullscreenError);
      } else if (chartContainer.webkitRequestFullscreen) {
        chartContainer.webkitRequestFullscreen();
      }
    } else {
      if (document.exitFullscreen) {
        document.exitFullscreen();
      } else if (document.webkitExitFullscreen) {
        document.webkitExitFullscreen();
      }
    }
  });
  function handleFullscreenError(err) {
    showNotification(`Ошибка перехода в полноэкранный режим: ${err.message}`, "error");
  }

  // Отслеживаем изменение полноэкранного режима для обновления размера диаграммы
  function handleFullscreenChange() {
    if (myChart) {
      myChart.resize();
    }
  }
  document.addEventListener("fullscreenchange", handleFullscreenChange);
  document.addEventListener("webkitfullscreenchange", handleFullscreenChange);
  document.addEventListener("mozfullscreenchange", handleFullscreenChange);
  document.addEventListener("MSFullscreenChange", handleFullscreenChange);
});
