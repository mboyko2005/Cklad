package com.example.apk.admin;

import android.app.Dialog;
import android.content.DialogInterface;
import android.content.pm.ActivityInfo;
import android.content.res.Configuration;
import android.graphics.Color;
import android.os.Bundle;
import android.view.View;
import android.view.Window;
import android.view.WindowManager;
import android.widget.Button;
import android.widget.FrameLayout;
import android.widget.ImageButton;
import android.widget.PopupMenu;
import android.widget.ProgressBar;
import android.widget.TextView;
import android.widget.Toast;

import androidx.appcompat.app.AppCompatActivity;
import androidx.cardview.widget.CardView;

import com.example.apk.R;
import com.example.apk.api.ApiClient;
import com.example.apk.api.ApiService;
import com.example.apk.api.ReportResponse;
import com.example.apk.utils.SessionManager;
import com.github.mikephil.charting.charts.BarChart;
import com.github.mikephil.charting.charts.LineChart;
import com.github.mikephil.charting.charts.PieChart;
import com.github.mikephil.charting.components.Description;
import com.github.mikephil.charting.components.XAxis;
import com.github.mikephil.charting.components.YAxis;
import com.github.mikephil.charting.data.BarData;
import com.github.mikephil.charting.data.BarDataSet;
import com.github.mikephil.charting.data.BarEntry;
import com.github.mikephil.charting.data.Entry;
import com.github.mikephil.charting.data.LineData;
import com.github.mikephil.charting.data.LineDataSet;
import com.github.mikephil.charting.data.PieData;
import com.github.mikephil.charting.data.PieDataSet;
import com.github.mikephil.charting.data.PieEntry;
import com.github.mikephil.charting.formatter.IndexAxisValueFormatter;
import com.github.mikephil.charting.formatter.PercentFormatter;
import com.github.mikephil.charting.utils.ColorTemplate;

import java.util.ArrayList;
import java.util.List;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

public class ReportsActivity extends AppCompatActivity {

    private ApiService apiService;
    private String authToken;
    private SessionManager sessionManager;

    // UI компоненты
    private ImageButton backButton;
    
    // Типы диаграмм
    public enum ChartType {
        BAR_CHART,
        LINE_CHART,
        PIE_CHART
    }
    
    // Компоненты для отчета по самым продаваемым товарам
    private CardView mostSoldProductsCard;
    private Button mostSoldProductsButton;
    private ImageButton mostSoldProductsChartTypeButton;
    private ImageButton mostSoldProductsFullscreenButton;
    private ProgressBar mostSoldProductsProgress;
    private FrameLayout mostSoldProductsChartContainer;
    private BarChart mostSoldProductsChart;
    private ChartType mostSoldProductsChartType = ChartType.BAR_CHART;
    private ReportResponse mostSoldProductsData;
    
    // Компоненты для отчета по пользователям системы
    private CardView systemUsersCard;
    private Button systemUsersButton;
    private ImageButton systemUsersChartTypeButton;
    private ImageButton systemUsersFullscreenButton;
    private ProgressBar systemUsersProgress;
    private FrameLayout systemUsersChartContainer;
    private BarChart systemUsersChart;
    private ChartType systemUsersChartType = ChartType.BAR_CHART;
    private ReportResponse systemUsersData;
    
    // Компоненты для отчета по общей стоимости товаров
    private CardView totalCostCard;
    private Button totalCostButton;
    private ImageButton totalCostChartTypeButton;
    private ImageButton totalCostFullscreenButton;
    private ProgressBar totalCostProgress;
    private FrameLayout totalCostChartContainer;
    private BarChart totalCostChart;
    private ChartType totalCostChartType = ChartType.BAR_CHART;
    private ReportResponse totalCostData;
    
    // Компоненты для отчета по текущим складским позициям
    private CardView currentStockCard;
    private Button currentStockButton;
    private ImageButton currentStockChartTypeButton;
    private ImageButton currentStockFullscreenButton;
    private ProgressBar currentStockProgress;
    private FrameLayout currentStockChartContainer;
    private BarChart currentStockChart;
    private ChartType currentStockChartType = ChartType.BAR_CHART;
    private ReportResponse currentStockData;
    
    // Флаги состояния масштабирования для каждого графика
    private boolean isMostSoldProductsZoomed = false;
    private boolean isSystemUsersZoomed = false;
    private boolean isTotalCostZoomed = false;
    private boolean isCurrentStockZoomed = false;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_reports);
        
        // Инициализация API сервиса
        apiService = ApiClient.getClient().create(ApiService.class);
        sessionManager = new SessionManager(this);
        authToken = sessionManager.getToken();
        
        // Инициализация UI компонентов
        initUI();
        
        // Настройка обработчиков нажатий
        setupClickListeners();
        
        // Автоматическая загрузка всех отчетов при открытии экрана
        loadAllReports();
    }
    
    private void initUI() {
        // Общие компоненты
        backButton = findViewById(R.id.backButton);
        
        // Отчет по самым продаваемым товарам
        mostSoldProductsCard = findViewById(R.id.mostSoldProductsCard);
        mostSoldProductsButton = findViewById(R.id.mostSoldProductsButton);
        mostSoldProductsChartTypeButton = findViewById(R.id.mostSoldProductsChartTypeButton);
        mostSoldProductsFullscreenButton = findViewById(R.id.mostSoldProductsFullscreenButton);
        mostSoldProductsProgress = findViewById(R.id.mostSoldProductsProgress);
        mostSoldProductsChartContainer = findViewById(R.id.mostSoldProductsChartContainer);
        
        // Отчет по пользователям системы
        systemUsersCard = findViewById(R.id.systemUsersCard);
        systemUsersButton = findViewById(R.id.systemUsersButton);
        systemUsersChartTypeButton = findViewById(R.id.systemUsersChartTypeButton);
        systemUsersFullscreenButton = findViewById(R.id.systemUsersFullscreenButton);
        systemUsersProgress = findViewById(R.id.systemUsersProgress);
        systemUsersChartContainer = findViewById(R.id.systemUsersChartContainer);
        
        // Отчет по общей стоимости товаров
        totalCostCard = findViewById(R.id.totalCostCard);
        totalCostButton = findViewById(R.id.totalCostButton);
        totalCostChartTypeButton = findViewById(R.id.totalCostChartTypeButton);
        totalCostFullscreenButton = findViewById(R.id.totalCostFullscreenButton);
        totalCostProgress = findViewById(R.id.totalCostProgress);
        totalCostChartContainer = findViewById(R.id.totalCostChartContainer);
        
        // Отчет по текущим складским позициям
        currentStockCard = findViewById(R.id.currentStockCard);
        currentStockButton = findViewById(R.id.currentStockButton);
        currentStockChartTypeButton = findViewById(R.id.currentStockChartTypeButton);
        currentStockFullscreenButton = findViewById(R.id.currentStockFullscreenButton);
        currentStockProgress = findViewById(R.id.currentStockProgress);
        currentStockChartContainer = findViewById(R.id.currentStockChartContainer);
    }
    
    private void setupClickListeners() {
        // Кнопка назад
        backButton.setOnClickListener(v -> finish());
        
        // Кнопки для масштабирования отчетов и иконки полноэкранного режима
        mostSoldProductsButton.setText("Открыть на весь экран");
        mostSoldProductsButton.setOnClickListener(v -> toggleFullScreenChart(mostSoldProductsCard, mostSoldProductsChartContainer));
        mostSoldProductsFullscreenButton.setOnClickListener(v -> toggleFullScreenChart(mostSoldProductsCard, mostSoldProductsChartContainer));
        
        systemUsersButton.setText("Открыть на весь экран");
        systemUsersButton.setOnClickListener(v -> toggleFullScreenChart(systemUsersCard, systemUsersChartContainer));
        systemUsersFullscreenButton.setOnClickListener(v -> toggleFullScreenChart(systemUsersCard, systemUsersChartContainer));
        
        totalCostButton.setText("Открыть на весь экран");
        totalCostButton.setOnClickListener(v -> toggleFullScreenChart(totalCostCard, totalCostChartContainer));
        totalCostFullscreenButton.setOnClickListener(v -> toggleFullScreenChart(totalCostCard, totalCostChartContainer));
        
        currentStockButton.setText("Открыть на весь экран");
        currentStockButton.setOnClickListener(v -> toggleFullScreenChart(currentStockCard, currentStockChartContainer));
        currentStockFullscreenButton.setOnClickListener(v -> toggleFullScreenChart(currentStockCard, currentStockChartContainer));
        
        // Кнопки для выбора типа диаграмм
        mostSoldProductsChartTypeButton.setOnClickListener(v -> showChartTypeMenu(v, mostSoldProductsData, mostSoldProductsChartContainer, ChartType.BAR_CHART, chartType -> {
            mostSoldProductsChartType = chartType;
            updateChart(mostSoldProductsData, mostSoldProductsChartContainer, mostSoldProductsChartType, Color.parseColor("#2196F3"));
        }));
        
        systemUsersChartTypeButton.setOnClickListener(v -> showChartTypeMenu(v, systemUsersData, systemUsersChartContainer, ChartType.BAR_CHART, chartType -> {
            systemUsersChartType = chartType;
            updateChart(systemUsersData, systemUsersChartContainer, systemUsersChartType, Color.parseColor("#FF4081"));
        }));
        
        totalCostChartTypeButton.setOnClickListener(v -> showChartTypeMenu(v, totalCostData, totalCostChartContainer, ChartType.BAR_CHART, chartType -> {
            totalCostChartType = chartType;
            updateChart(totalCostData, totalCostChartContainer, totalCostChartType, Color.parseColor("#4CAF50"));
        }));
        
        currentStockChartTypeButton.setOnClickListener(v -> showChartTypeMenu(v, currentStockData, currentStockChartContainer, ChartType.BAR_CHART, chartType -> {
            currentStockChartType = chartType;
            updateChart(currentStockData, currentStockChartContainer, currentStockChartType, Color.parseColor("#3F51B5"));
        }));
    }
    
    // Метод для загрузки всех отчетов при открытии экрана
    private void loadAllReports() {
        loadMostSoldProductsReport();
        loadSystemUsersReport();
        loadTotalCostReport();
        loadCurrentStockReport();
    }
    
    // Метод для переключения масштаба графика
    private void toggleChartZoom(CardView card, FrameLayout container, boolean isZoomed) {
        if (isZoomed) {
            // Возвращаем к обычному размеру
            card.setLayoutParams(new androidx.constraintlayout.widget.ConstraintLayout.LayoutParams(
                    androidx.constraintlayout.widget.ConstraintLayout.LayoutParams.MATCH_PARENT,
                    getResources().getDimensionPixelSize(R.dimen.card_height)
            ));
            
            // Обновляем флаг масштабирования
            if (card == mostSoldProductsCard) {
                isMostSoldProductsZoomed = false;
                mostSoldProductsButton.setText("Показать в полном размере");
            } else if (card == systemUsersCard) {
                isSystemUsersZoomed = false;
                systemUsersButton.setText("Показать в полном размере");
            } else if (card == totalCostCard) {
                isTotalCostZoomed = false;
                totalCostButton.setText("Показать в полном размере");
            } else if (card == currentStockCard) {
                isCurrentStockZoomed = false;
                currentStockButton.setText("Показать в полном размере");
            }
        } else {
            // Устанавливаем увеличенный размер
            card.setLayoutParams(new androidx.constraintlayout.widget.ConstraintLayout.LayoutParams(
                    androidx.constraintlayout.widget.ConstraintLayout.LayoutParams.MATCH_PARENT,
                    getResources().getDimensionPixelSize(R.dimen.card_height_zoomed)
            ));
            
            // Обновляем флаг масштабирования
            if (card == mostSoldProductsCard) {
                isMostSoldProductsZoomed = true;
                mostSoldProductsButton.setText("Вернуть обычный размер");
            } else if (card == systemUsersCard) {
                isSystemUsersZoomed = true;
                systemUsersButton.setText("Вернуть обычный размер");
            } else if (card == totalCostCard) {
                isTotalCostZoomed = true;
                totalCostButton.setText("Вернуть обычный размер");
            } else if (card == currentStockCard) {
                isCurrentStockZoomed = true;
                currentStockButton.setText("Вернуть обычный размер");
            }
        }
        
        // Перерисовываем график для корректного отображения в новом размере
        BarChart chart = (BarChart) container.getChildAt(0);
        if (chart != null) {
            chart.notifyDataSetChanged();
            chart.invalidate();
        }
    }
    
    // Загрузка отчета по самым продаваемым товарам
    private void loadMostSoldProductsReport() {
        // Активируем кнопки сразу
        mostSoldProductsButton.setEnabled(true);
        mostSoldProductsChartTypeButton.setVisibility(View.VISIBLE);
        
        // Показываем прогресс
        mostSoldProductsProgress.setVisibility(View.VISIBLE);
        
        Call<ReportResponse> call = apiService.getMostSoldProducts("Bearer " + authToken);
        call.enqueue(new Callback<ReportResponse>() {
            @Override
            public void onResponse(Call<ReportResponse> call, Response<ReportResponse> response) {
                // Скрываем прогресс после загрузки
                mostSoldProductsProgress.setVisibility(View.GONE);
                
                if (response.isSuccessful() && response.body() != null) {
                    // Сохраняем данные для возможности изменения типа диаграммы
                    mostSoldProductsData = response.body();
                    
                    // Отображаем диаграмму выбранного типа
                    updateChart(mostSoldProductsData, 
                            mostSoldProductsChartContainer, 
                            mostSoldProductsChartType, 
                            Color.parseColor("#2196F3"));
                } else {
                    Toast.makeText(ReportsActivity.this, "Ошибка при загрузке отчета", Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<ReportResponse> call, Throwable t) {
                // Скрываем прогресс при ошибке
                mostSoldProductsProgress.setVisibility(View.GONE);
                Toast.makeText(ReportsActivity.this, "Ошибка соединения: " + t.getMessage(), Toast.LENGTH_SHORT).show();
            }
        });
    }
    
    // Загрузка отчета по пользователям системы
    private void loadSystemUsersReport() {
        // Активируем кнопки сразу
        systemUsersButton.setEnabled(true);
        systemUsersChartTypeButton.setVisibility(View.VISIBLE);
        
        // Показываем прогресс
        systemUsersProgress.setVisibility(View.VISIBLE);
        
        Call<ReportResponse> call = apiService.getSystemUsers("Bearer " + authToken);
        call.enqueue(new Callback<ReportResponse>() {
            @Override
            public void onResponse(Call<ReportResponse> call, Response<ReportResponse> response) {
                // Скрываем прогресс после загрузки
                systemUsersProgress.setVisibility(View.GONE);
                
                if (response.isSuccessful() && response.body() != null) {
                    // Сохраняем данные для возможности изменения типа диаграммы
                    systemUsersData = response.body();
                    
                    // Отображаем диаграмму выбранного типа
                    updateChart(systemUsersData, 
                            systemUsersChartContainer, 
                            systemUsersChartType, 
                            Color.parseColor("#FF4081"));
                } else {
                    Toast.makeText(ReportsActivity.this, "Ошибка при загрузке отчета", Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<ReportResponse> call, Throwable t) {
                // Скрываем прогресс при ошибке
                systemUsersProgress.setVisibility(View.GONE);
                Toast.makeText(ReportsActivity.this, "Ошибка соединения: " + t.getMessage(), Toast.LENGTH_SHORT).show();
            }
        });
    }
    
    // Загрузка отчета по общей стоимости товаров
    private void loadTotalCostReport() {
        // Активируем кнопки сразу
        totalCostButton.setEnabled(true);
        totalCostChartTypeButton.setVisibility(View.VISIBLE);
        
        // Показываем прогресс
        totalCostProgress.setVisibility(View.VISIBLE);
        
        Call<ReportResponse> call = apiService.getTotalCost("Bearer " + authToken);
        call.enqueue(new Callback<ReportResponse>() {
            @Override
            public void onResponse(Call<ReportResponse> call, Response<ReportResponse> response) {
                // Скрываем прогресс после загрузки
                totalCostProgress.setVisibility(View.GONE);
                
                if (response.isSuccessful() && response.body() != null) {
                    // Сохраняем данные для возможности изменения типа диаграммы
                    totalCostData = response.body();
                    
                    // Отображаем диаграмму выбранного типа
                    updateChart(totalCostData, 
                            totalCostChartContainer, 
                            totalCostChartType, 
                            Color.parseColor("#4CAF50"));
                } else {
                    Toast.makeText(ReportsActivity.this, "Ошибка при загрузке отчета", Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<ReportResponse> call, Throwable t) {
                // Скрываем прогресс при ошибке
                totalCostProgress.setVisibility(View.GONE);
                Toast.makeText(ReportsActivity.this, "Ошибка соединения: " + t.getMessage(), Toast.LENGTH_SHORT).show();
            }
        });
    }
    
    // Загрузка отчета по текущим складским позициям
    private void loadCurrentStockReport() {
        // Активируем кнопки сразу
        currentStockButton.setEnabled(true);
        currentStockChartTypeButton.setVisibility(View.VISIBLE);
        
        // Показываем прогресс
        currentStockProgress.setVisibility(View.VISIBLE);
        
        Call<ReportResponse> call = apiService.getCurrentStock("Bearer " + authToken);
        call.enqueue(new Callback<ReportResponse>() {
            @Override
            public void onResponse(Call<ReportResponse> call, Response<ReportResponse> response) {
                // Скрываем прогресс после загрузки
                currentStockProgress.setVisibility(View.GONE);
                
                if (response.isSuccessful() && response.body() != null) {
                    // Сохраняем данные для возможности изменения типа диаграммы
                    currentStockData = response.body();
                    
                    // Отображаем диаграмму выбранного типа
                    updateChart(currentStockData, 
                            currentStockChartContainer, 
                            currentStockChartType, 
                            Color.parseColor("#3F51B5"));
                } else {
                    Toast.makeText(ReportsActivity.this, "Ошибка при загрузке отчета", Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<ReportResponse> call, Throwable t) {
                // Скрываем прогресс при ошибке
                currentStockProgress.setVisibility(View.GONE);
                Toast.makeText(ReportsActivity.this, "Ошибка соединения: " + t.getMessage(), Toast.LENGTH_SHORT).show();
            }
        });
    }
    
    // Метод для обновления графика с выбранным типом
    private void updateChart(ReportResponse report, FrameLayout container, ChartType chartType, int color) {
        if (report == null || container == null) {
            return;
        }
        
        container.removeAllViews();
        
        switch (chartType) {
            case BAR_CHART:
                createBarChart(report, container, color);
                break;
            case LINE_CHART:
                createLineChart(report, container, color);
                break;
            case PIE_CHART:
                createPieChart(report, container, color);
                break;
        }
    }
    
    // Создание столбчатой диаграммы
    private void createBarChart(ReportResponse report, FrameLayout container, int color) {
        List<String> labels = report.getLabels();
        List<Double> data = report.getData();
        
        if (labels == null || data == null || labels.isEmpty() || data.isEmpty()) {
            return;
        }
        
        // Создание столбчатой диаграммы
        BarChart barChart = new BarChart(this);
        
        // Проверяем, является ли контейнер полноэкранным диалогом
        boolean isFullscreen = container.getId() == R.id.dialogChartContainer;
        
        // Настраиваем параметры графика в зависимости от режима отображения
        configureBarChart(barChart, report, labels, data, color, isFullscreen);
        
        // Добавляем график в контейнер
        container.addView(barChart, new FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT, 
                FrameLayout.LayoutParams.MATCH_PARENT));
    }
    
    // Метод для настройки столбчатой диаграммы
    private void configureBarChart(BarChart barChart, ReportResponse report, List<String> labels, List<Double> data, int color, boolean isFullscreen) {
        // Создание наборов данных
        ArrayList<BarEntry> entries = new ArrayList<>();
        for (int i = 0; i < data.size(); i++) {
            entries.add(new BarEntry(i, data.get(i).floatValue()));
        }
        
        BarDataSet dataSet = new BarDataSet(entries, report.getTitle());
        dataSet.setColor(color);
        dataSet.setValueTextColor(Color.BLACK);
        dataSet.setValueTextSize(isFullscreen ? 14f : 12f); // Увеличиваем текст в полноэкранном режиме
        
        BarData barData = new BarData(dataSet);
        barData.setBarWidth(0.7f);
        barChart.setData(barData);
        
        // Настройка оси X
        XAxis xAxis = barChart.getXAxis();
        xAxis.setValueFormatter(new IndexAxisValueFormatter(labels));
        xAxis.setPosition(XAxis.XAxisPosition.BOTTOM);
        xAxis.setGranularity(1f);
        xAxis.setLabelRotationAngle(45f);
        xAxis.setTextSize(isFullscreen ? 13f : 11f);
        
        // Настройка осей Y
        YAxis leftAxis = barChart.getAxisLeft();
        leftAxis.setTextSize(isFullscreen ? 13f : 11f);
        
        YAxis rightAxis = barChart.getAxisRight();
        rightAxis.setEnabled(false);
        
        // Описание и форматирование
        Description description = new Description();
        description.setText(report.getTitle());
        description.setTextSize(isFullscreen ? 16f : 14f);
        barChart.setDescription(description);
        
        // Взаимодействие с графиком
        barChart.setPinchZoom(true);
        barChart.setDoubleTapToZoomEnabled(true);
        barChart.setHighlightPerTapEnabled(true);
        barChart.setHighlightPerDragEnabled(true);
        barChart.setDragEnabled(true);
        barChart.setScaleEnabled(true);
        barChart.setScaleXEnabled(true);
        barChart.setScaleYEnabled(true);
        
        // Отступы для лучшей видимости (разные в зависимости от режима)
        barChart.setExtraBottomOffset(isFullscreen ? 15f : 10f);
        barChart.setExtraLeftOffset(isFullscreen ? 15f : 10f);
        barChart.setExtraRightOffset(isFullscreen ? 15f : 10f);
        barChart.setExtraTopOffset(isFullscreen ? 15f : 5f);
        
        // Анимация
        barChart.animateY(1000);
        
        // Обновление диаграммы
        barChart.invalidate();
    }
    
    // Создание линейной диаграммы
    private void createLineChart(ReportResponse report, FrameLayout container, int color) {
        List<String> labels = report.getLabels();
        List<Double> data = report.getData();
        
        if (labels == null || data == null || labels.isEmpty() || data.isEmpty()) {
            return;
        }
        
        // Создание линейной диаграммы
        LineChart lineChart = new LineChart(this);
        
        // Проверяем, является ли контейнер полноэкранным диалогом
        boolean isFullscreen = container.getId() == R.id.dialogChartContainer;
        
        // Настраиваем параметры графика в зависимости от режима отображения
        configureLineChart(lineChart, report, labels, data, color, isFullscreen);
        
        // Добавляем график в контейнер
        container.addView(lineChart, new FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT, 
                FrameLayout.LayoutParams.MATCH_PARENT));
    }
    
    // Метод для настройки линейной диаграммы
    private void configureLineChart(LineChart lineChart, ReportResponse report, List<String> labels, List<Double> data, int color, boolean isFullscreen) {
        // Создание наборов данных
        ArrayList<Entry> entries = new ArrayList<>();
        for (int i = 0; i < data.size(); i++) {
            entries.add(new Entry(i, data.get(i).floatValue()));
        }
        
        LineDataSet dataSet = new LineDataSet(entries, report.getTitle());
        dataSet.setColor(color);
        dataSet.setValueTextColor(Color.BLACK);
        dataSet.setValueTextSize(isFullscreen ? 14f : 12f);
        dataSet.setLineWidth(isFullscreen ? 3f : 2.5f);
        dataSet.setCircleColor(color);
        dataSet.setCircleRadius(isFullscreen ? 6f : 5f);
        dataSet.setDrawCircleHole(true);
        dataSet.setCircleHoleRadius(isFullscreen ? 3f : 2.5f);
        dataSet.setDrawFilled(true);
        dataSet.setFillColor(Color.argb(80, Color.red(color), Color.green(color), Color.blue(color)));
        
        LineData lineData = new LineData(dataSet);
        lineChart.setData(lineData);
        
        // Настройка оси X
        XAxis xAxis = lineChart.getXAxis();
        xAxis.setValueFormatter(new IndexAxisValueFormatter(labels));
        xAxis.setPosition(XAxis.XAxisPosition.BOTTOM);
        xAxis.setGranularity(1f);
        xAxis.setLabelRotationAngle(45f);
        xAxis.setTextSize(isFullscreen ? 13f : 11f);
        
        // Настройка осей Y
        YAxis leftAxis = lineChart.getAxisLeft();
        leftAxis.setTextSize(isFullscreen ? 13f : 11f);
        
        YAxis rightAxis = lineChart.getAxisRight();
        rightAxis.setEnabled(false);
        
        // Описание и форматирование
        Description description = new Description();
        description.setText(report.getTitle());
        description.setTextSize(isFullscreen ? 16f : 14f);
        lineChart.setDescription(description);
        
        // Взаимодействие с графиком
        lineChart.setPinchZoom(true);
        lineChart.setDoubleTapToZoomEnabled(true);
        lineChart.setDragEnabled(true);
        lineChart.setScaleEnabled(true);
        lineChart.setScaleXEnabled(true);
        lineChart.setScaleYEnabled(true);
        
        // Отступы для лучшей видимости (разные в зависимости от режима)
        lineChart.setExtraBottomOffset(isFullscreen ? 15f : 10f);
        lineChart.setExtraLeftOffset(isFullscreen ? 15f : 10f);
        lineChart.setExtraRightOffset(isFullscreen ? 15f : 10f);
        lineChart.setExtraTopOffset(isFullscreen ? 15f : 5f);
        
        // Анимация
        lineChart.animateY(1200);
        
        // Обновление диаграммы
        lineChart.invalidate();
    }
    
    // Создание круговой диаграммы
    private void createPieChart(ReportResponse report, FrameLayout container, int color) {
        List<String> labels = report.getLabels();
        List<Double> data = report.getData();
        
        if (labels == null || data == null || labels.isEmpty() || data.isEmpty()) {
            return;
        }
        
        // Создание круговой диаграммы
        PieChart pieChart = new PieChart(this);
        
        // Проверяем, является ли контейнер полноэкранным диалогом
        boolean isFullscreen = container.getId() == R.id.dialogChartContainer;
        
        // Настраиваем параметры графика в зависимости от режима отображения
        configurePieChart(pieChart, report, labels, data, color, isFullscreen);
        
        // Добавляем график в контейнер
        container.addView(pieChart, new FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT, 
                FrameLayout.LayoutParams.MATCH_PARENT));
    }
    
    // Метод для настройки круговой диаграммы
    private void configurePieChart(PieChart pieChart, ReportResponse report, List<String> labels, List<Double> data, int color, boolean isFullscreen) {
        // Создание наборов данных
        ArrayList<PieEntry> entries = new ArrayList<>();
        for (int i = 0; i < data.size(); i++) {
            entries.add(new PieEntry(data.get(i).floatValue(), labels.get(i)));
        }
        
        PieDataSet dataSet = new PieDataSet(entries, report.getTitle());
        
        // Создаем разные цвета для сегментов с улучшенной палитрой
        ArrayList<Integer> colors = new ArrayList<>();
        for (int c : ColorTemplate.MATERIAL_COLORS) {
            colors.add(c);
        }
        for (int c : ColorTemplate.VORDIPLOM_COLORS) {
            colors.add(c);
        }
        dataSet.setColors(colors);
        
        // Настройка текста
        dataSet.setValueTextColor(Color.WHITE);
        dataSet.setValueTextSize(isFullscreen ? 16f : 14f);
        dataSet.setXValuePosition(PieDataSet.ValuePosition.OUTSIDE_SLICE);
        dataSet.setYValuePosition(PieDataSet.ValuePosition.INSIDE_SLICE);
        dataSet.setSliceSpace(3f);
        dataSet.setSelectionShift(isFullscreen ? 12f : 10f);
        
        PieData pieData = new PieData(dataSet);
        pieData.setValueFormatter(new PercentFormatter(pieChart));
        pieChart.setData(pieData);
        
        // Настройки диаграммы
        pieChart.setUsePercentValues(true);
        pieChart.setDrawHoleEnabled(true);
        pieChart.setHoleColor(Color.WHITE);
        pieChart.setTransparentCircleRadius(61f);
        pieChart.setHoleRadius(isFullscreen ? 35f : 40f); // В полноэкранном режиме меньше дырка в центре
        pieChart.setTransparentCircleAlpha(110);
        
        // Настройки вращения и взаимодействия
        pieChart.setRotationEnabled(true);
        pieChart.setRotationAngle(0);
        pieChart.setHighlightPerTapEnabled(true);
        pieChart.setDragDecelerationEnabled(true);
        pieChart.setDragDecelerationFrictionCoef(0.95f);
        
        // Описание и форматирование
        Description description = new Description();
        description.setText(report.getTitle());
        description.setTextSize(isFullscreen ? 16f : 14f);
        pieChart.setDescription(description);
        
        // Размещение легенды
        pieChart.setDrawEntryLabels(true);
        pieChart.setEntryLabelTextSize(isFullscreen ? 14f : 12f);
        pieChart.setEntryLabelColor(Color.BLACK);
        
        // Отступы для лучшей видимости
        pieChart.setExtraBottomOffset(isFullscreen ? 15f : 10f);
        pieChart.setExtraLeftOffset(isFullscreen ? 15f : 10f);
        pieChart.setExtraRightOffset(isFullscreen ? 15f : 10f);
        pieChart.setExtraTopOffset(isFullscreen ? 15f : 5f);
        
        // Анимация
        pieChart.animateY(1400);
        
        // Обновление диаграммы
        pieChart.invalidate();
    }
    
    // Управление отображением прогресса и кнопки
    private void showProgress(ProgressBar progressBar, Button button, boolean show) {
        if (show) {
            progressBar.setVisibility(View.VISIBLE);
            button.setEnabled(false);
        } else {
            progressBar.setVisibility(View.GONE);
            button.setEnabled(true);
        }
    }

    // Интерфейс для обратного вызова при выборе типа диаграммы
    interface ChartTypeSelectionListener {
        void onChartTypeSelected(ChartType chartType);
    }
    
    // Метод для отображения меню выбора типа диаграммы
    private void showChartTypeMenu(View anchor, ReportResponse reportData, FrameLayout container, ChartType defaultType, ChartTypeSelectionListener listener) {
        if (reportData == null) {
            Toast.makeText(this, "Данные отчета еще не загружены", Toast.LENGTH_SHORT).show();
            return;
        }
        
        PopupMenu popupMenu = new PopupMenu(this, anchor);
        popupMenu.getMenu().add(0, 1, 0, "Столбчатая диаграмма");
        popupMenu.getMenu().add(0, 2, 0, "Линейная диаграмма");
        popupMenu.getMenu().add(0, 3, 0, "Круговая диаграмма");
        
        popupMenu.setOnMenuItemClickListener(item -> {
            ChartType selectedType;
            switch (item.getItemId()) {
                case 1:
                    selectedType = ChartType.BAR_CHART;
                    break;
                case 2:
                    selectedType = ChartType.LINE_CHART;
                    break;
                case 3:
                    selectedType = ChartType.PIE_CHART;
                    break;
                default:
                    selectedType = defaultType;
                    break;
            }
            
            if (listener != null) {
                listener.onChartTypeSelected(selectedType);
            }
            
            return true;
        });
        
        popupMenu.show();
    }
    
    // Метод для переключения карточки в полноэкранный режим
    private void toggleFullScreenChart(CardView card, FrameLayout chartContainer) {
        // Определяем, какая карточка обрабатывается
        boolean isZoomed = false;
        String title = "";
        ReportResponse data = null;
        ChartType chartType = ChartType.BAR_CHART;
        int color = Color.BLUE;
        
        if (card == mostSoldProductsCard) {
            isZoomed = isMostSoldProductsZoomed;
            title = "Самые продаваемые товары";
            data = mostSoldProductsData;
            chartType = mostSoldProductsChartType;
            color = Color.parseColor("#2196F3");
        } else if (card == systemUsersCard) {
            isZoomed = isSystemUsersZoomed;
            title = "Пользователи системы";
            data = systemUsersData;
            chartType = systemUsersChartType;
            color = Color.parseColor("#FF4081");
        } else if (card == totalCostCard) {
            isZoomed = isTotalCostZoomed;
            title = "Общая стоимость товаров";
            data = totalCostData;
            chartType = totalCostChartType;
            color = Color.parseColor("#4CAF50");
        } else if (card == currentStockCard) {
            isZoomed = isCurrentStockZoomed;
            title = "Текущие складские позиции";
            data = currentStockData;
            chartType = currentStockChartType;
            color = Color.parseColor("#3F51B5");
        }
        
        if (isZoomed) {
            // Восстанавливаем обычный режим
            restoreAllCards();
        } else {
            // Показываем диалог с графиком в полноэкранном режиме
            showFullscreenChartDialog(title, data, chartType, color);
            
            // Обновляем флаг зума для конкретной карточки
            updateZoomState(card, true);
        }
    }
    
    // Метод для отображения диалога с графиком в полноэкранном режиме
    private void showFullscreenChartDialog(String title, ReportResponse data, ChartType chartType, int color) {
        if (data == null) {
            Toast.makeText(this, "Данные отчета еще не загружены", Toast.LENGTH_SHORT).show();
            return;
        }
        
        // Используем Dialog без переворачивания экрана
        final Dialog fullscreenDialog = new Dialog(this, android.R.style.Theme_DeviceDefault_Light_NoActionBar_Fullscreen);
        
        // Создаем View для диалога с графиком и кнопками
        View dialogView = getLayoutInflater().inflate(R.layout.dialog_fullscreen_chart, null);
        
        // Получаем контейнер для графика
        FrameLayout chartContainer = dialogView.findViewById(R.id.dialogChartContainer);
        ImageButton chartTypeButton = dialogView.findViewById(R.id.dialogChartTypeButton);
        ImageButton closeButton = dialogView.findViewById(R.id.dialogChartCloseButton);
        
        // Задаем заголовок
        TextView titleView = dialogView.findViewById(R.id.dialogChartTitle);
        titleView.setText(title);
        
        // Устанавливаем макет диалога
        fullscreenDialog.setContentView(dialogView);
        
        // Настройка параметров окна диалога для полноэкранного режима
        Window window = fullscreenDialog.getWindow();
        if (window != null) {
            // Устанавливаем размер на весь экран
            WindowManager.LayoutParams params = window.getAttributes();
            params.width = WindowManager.LayoutParams.MATCH_PARENT;
            params.height = WindowManager.LayoutParams.MATCH_PARENT;
            
            // Устанавливаем флаги для полноэкранного режима
            window.setFlags(
                WindowManager.LayoutParams.FLAG_FULLSCREEN,
                WindowManager.LayoutParams.FLAG_FULLSCREEN
            );
            
            // Применяем параметры
            window.setAttributes(params);
        }
        
        // Создаем график в зависимости от типа
        updateChart(data, chartContainer, chartType, color);
        
        // Устанавливаем обработчик кнопки смены типа графика
        final ChartType[] currentType = {chartType};
        chartTypeButton.setOnClickListener(v -> {
            PopupMenu popupMenu = new PopupMenu(this, v);
            popupMenu.getMenu().add(0, 1, 0, "Столбчатая диаграмма");
            popupMenu.getMenu().add(0, 2, 0, "Линейная диаграмма");
            popupMenu.getMenu().add(0, 3, 0, "Круговая диаграмма");
            
            popupMenu.setOnMenuItemClickListener(item -> {
                ChartType selectedType;
                switch (item.getItemId()) {
                    case 1:
                        selectedType = ChartType.BAR_CHART;
                        break;
                    case 2:
                        selectedType = ChartType.LINE_CHART;
                        break;
                    case 3:
                        selectedType = ChartType.PIE_CHART;
                        break;
                    default:
                        selectedType = currentType[0];
                        break;
                }
                
                currentType[0] = selectedType;
                chartContainer.removeAllViews();
                updateChart(data, chartContainer, selectedType, color);
                return true;
            });
            
            popupMenu.show();
        });
        
        // Обработчик для кнопки закрытия
        closeButton.setOnClickListener(v -> fullscreenDialog.dismiss());
        
        // Отображаем диалог
        fullscreenDialog.show();
    }
    
    // Метод для обновления состояния зума карточки
    private void updateZoomState(CardView card, boolean isZoomed) {
        if (card == mostSoldProductsCard) {
            isMostSoldProductsZoomed = isZoomed;
        } else if (card == systemUsersCard) {
            isSystemUsersZoomed = isZoomed;
        } else if (card == totalCostCard) {
            isTotalCostZoomed = isZoomed;
        } else if (card == currentStockCard) {
            isCurrentStockZoomed = isZoomed;
        }
    }
    
    // Метод для восстановления отображения всех карточек
    private void restoreAllCards() {
        // Сбрасываем флаги масштабирования
        isMostSoldProductsZoomed = false;
        isSystemUsersZoomed = false;
        isTotalCostZoomed = false;
        isCurrentStockZoomed = false;
    }
    
    // Метод для обновления иконки кнопки полноэкранного режима
    private void updateFullscreenButtonIcon(CardView card, boolean isFullscreen) {
        ImageButton fullscreenButton = null;
        
        if (card == mostSoldProductsCard) {
            fullscreenButton = mostSoldProductsFullscreenButton;
        } else if (card == systemUsersCard) {
            fullscreenButton = systemUsersFullscreenButton;
        } else if (card == totalCostCard) {
            fullscreenButton = totalCostFullscreenButton;
        } else if (card == currentStockCard) {
            fullscreenButton = currentStockFullscreenButton;
        }
        
        if (fullscreenButton != null) {
            if (isFullscreen) {
                fullscreenButton.setImageResource(R.drawable.ic_fullscreen_exit);
                fullscreenButton.setContentDescription("Выйти из полноэкранного режима");
            } else {
                fullscreenButton.setImageResource(R.drawable.ic_fullscreen);
                fullscreenButton.setContentDescription("Открыть в полный экран");
            }
        }
    }
} 