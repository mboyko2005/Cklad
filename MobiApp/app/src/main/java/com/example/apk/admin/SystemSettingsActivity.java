package com.example.apk.admin;

import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.content.Context;
import android.os.Build;
import android.os.Bundle;
import android.view.View;
import android.widget.Button;
import android.widget.ImageButton;
import android.widget.ProgressBar;
import android.widget.Spinner;
import android.widget.Switch;
import android.widget.Toast;

import androidx.appcompat.app.AlertDialog;
import androidx.appcompat.app.AppCompatDelegate;
import androidx.cardview.widget.CardView;
import androidx.core.app.NotificationCompat;
import androidx.core.app.NotificationManagerCompat;

import com.example.apk.R;
import com.example.apk.WarehouseApplication;
import com.example.apk.api.ApiClient;
import com.example.apk.api.ApiService;
import com.example.apk.api.ChangePasswordRequest;
import com.example.apk.api.SettingsResponse;
import com.example.apk.api.ThemeRequest;
import com.example.apk.utils.BaseActivity;
import com.example.apk.utils.SessionManager;
import com.google.android.material.textfield.TextInputEditText;

import java.io.File;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

/**
 * Активность настроек системы
 */
public class SystemSettingsActivity extends BaseActivity {

    private static final String CHANNEL_ID = "test_notification_channel";
    private static final int NOTIFICATION_ID = 1;

    private SessionManager sessionManager;
    private ApiService apiService;
    private String authToken;
    private String username;
    
    // UI компоненты
    private CardView notificationSettingsCard;
    private Switch enableNotificationsSwitch;
    private Switch pushNotificationsSwitch;
    private Button testNotificationButton;
    private Button saveNotificationSettingsButton;
    
    private CardView cacheSettingsCard;
    private Switch enableCachingSwitch;
    private Button clearCacheButton;
    private ProgressBar clearCacheProgress;
    
    private CardView themeSettingsCard;
    private Spinner themeSpinner;
    private Button applyThemeButton;
    
    private CardView securitySettingsCard;
    private TextInputEditText newPasswordEditText;
    private TextInputEditText confirmPasswordEditText;
    private Button changePasswordButton;
    
    private boolean isLoading = false;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_system_settings);
        
        // Инициализация сессии и API сервиса
        sessionManager = new SessionManager(this);
        apiService = ApiClient.getApiService();
        authToken = sessionManager.getToken();
        username = sessionManager.getUsername();
        
        // Проверка авторизации
        if (!sessionManager.isLoggedIn() || !sessionManager.getRole().equals("Администратор")) {
            Toast.makeText(this, "Доступ запрещен", Toast.LENGTH_SHORT).show();
            finish();
            return;
        }
        
        // Создаем канал уведомлений (для Android 8.0 и выше)
        createNotificationChannel();
        
        // Инициализация UI компонентов
        initViews();
        
        // Загрузка локальных настроек
        loadLocalSettings();
        
        // Настройка обработчиков событий
        setupListeners();
    }
    
    /**
     * Создание канала уведомлений для Android 8.0 и выше
     */
    private void createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            CharSequence name = "Тестовые уведомления";
            String description = "Канал для тестовых уведомлений";
            int importance = NotificationManager.IMPORTANCE_DEFAULT;
            NotificationChannel channel = new NotificationChannel(CHANNEL_ID, name, importance);
            channel.setDescription(description);
            
            NotificationManager notificationManager = getSystemService(NotificationManager.class);
            notificationManager.createNotificationChannel(channel);
        }
    }
    
    /**
     * Инициализация UI компонентов
     */
    private void initViews() {
        // Кнопка назад
        ImageButton backButton = findViewById(R.id.backButton);
        backButton.setOnClickListener(v -> finish());
        
        // Карточка настроек уведомлений
        notificationSettingsCard = findViewById(R.id.notificationSettingsCard);
        enableNotificationsSwitch = findViewById(R.id.enableNotificationsSwitch);
        pushNotificationsSwitch = findViewById(R.id.pushNotificationsSwitch);
        testNotificationButton = findViewById(R.id.testNotificationButton);
        saveNotificationSettingsButton = findViewById(R.id.saveNotificationSettingsButton);
        
        // Карточка настроек кэша
        cacheSettingsCard = findViewById(R.id.cacheSettingsCard);
        enableCachingSwitch = findViewById(R.id.enableCachingSwitch);
        clearCacheButton = findViewById(R.id.clearCacheButton);
        clearCacheProgress = findViewById(R.id.clearCacheProgress);
        
        // Карточка настроек темы
        themeSettingsCard = findViewById(R.id.themeSettingsCard);
        themeSpinner = findViewById(R.id.themeSpinner);
        applyThemeButton = findViewById(R.id.applyThemeButton);
        
        // Карточка настроек безопасности
        securitySettingsCard = findViewById(R.id.securitySettingsCard);
        newPasswordEditText = findViewById(R.id.newPasswordEditText);
        confirmPasswordEditText = findViewById(R.id.confirmPasswordEditText);
        changePasswordButton = findViewById(R.id.changePasswordButton);
    }
    
    /**
     * Настройка обработчиков событий
     */
    private void setupListeners() {
        // Настройка состояния переключателей уведомлений
        enableNotificationsSwitch.setOnCheckedChangeListener((buttonView, isChecked) -> {
            pushNotificationsSwitch.setEnabled(isChecked);
            testNotificationButton.setEnabled(isChecked);
        });
        
        // Тестовое уведомление
        testNotificationButton.setOnClickListener(v -> {
            sendTestNotification();
        });
        
        // Сохранение настроек уведомлений
        saveNotificationSettingsButton.setOnClickListener(v -> saveNotificationSettings());
        
        // Управление кэшем
        enableCachingSwitch.setOnCheckedChangeListener((buttonView, isChecked) -> {
            // Сохраняем настройку кэша локально
            sessionManager.saveSettingBoolean("enable_caching", isChecked);
            Toast.makeText(this, "Настройки кэша сохранены", Toast.LENGTH_SHORT).show();
        });
        
        // Очистка кэша
        clearCacheButton.setOnClickListener(v -> {
            showClearCacheConfirmationDialog();
        });
        
        // Применение темы
        applyThemeButton.setOnClickListener(v -> {
            // Получаем выбранную тему
            String selectedTheme = themeSpinner.getSelectedItem().toString();
            
            // Сохраняем тему на сервере и локально
            saveTheme(selectedTheme);
        });
        
        // Изменение пароля
        changePasswordButton.setOnClickListener(v -> {
            if (validatePasswordChange()) {
                changePassword();
            }
        });
    }
    
    /**
     * Отправка тестового уведомления
     */
    private void sendTestNotification() {
        // Проверка разрешений для Android 13+
        if (Build.VERSION.SDK_INT >= 33) {
            if (!NotificationManagerCompat.from(this).areNotificationsEnabled()) {
                Toast.makeText(this, "Разрешите уведомления в настройках приложения", Toast.LENGTH_LONG).show();
                return;
            }
        }
        
        NotificationCompat.Builder builder = new NotificationCompat.Builder(this, CHANNEL_ID)
                .setSmallIcon(R.drawable.logo_icon) // Используем иконку проекта
                .setContentTitle("Тестовое уведомление")
                .setContentText("Это тестовое уведомление от приложения Управление складом")
                .setPriority(NotificationCompat.PRIORITY_DEFAULT)
                .setAutoCancel(true);
        
        NotificationManagerCompat notificationManager = NotificationManagerCompat.from(this);
        
        try {
            notificationManager.notify(NOTIFICATION_ID, builder.build());
            Toast.makeText(this, "Тестовое уведомление отправлено", Toast.LENGTH_SHORT).show();
        } catch (SecurityException e) {
            Toast.makeText(this, "Нет разрешения на отправку уведомлений", Toast.LENGTH_SHORT).show();
        }
    }
    
    /**
     * Загрузка локальных настроек
     */
    private void loadLocalSettings() {
        // Настройки уведомлений
        boolean enableNotifications = sessionManager.getSettingBoolean("enable_notifications", true);
        enableNotificationsSwitch.setChecked(enableNotifications);
        pushNotificationsSwitch.setChecked(sessionManager.getSettingBoolean("push_notifications", true));
        
        // Состояние переключателей уведомлений
        pushNotificationsSwitch.setEnabled(enableNotifications);
        testNotificationButton.setEnabled(enableNotifications);
        
        // Настройки кэша
        enableCachingSwitch.setChecked(sessionManager.getSettingBoolean("enable_caching", true));
        
        // Настройки темы - по умолчанию "Системная"
        String theme = sessionManager.getSettingString(WarehouseApplication.THEME_PREF_KEY, WarehouseApplication.THEME_SYSTEM);
        for (int i = 0; i < themeSpinner.getAdapter().getCount(); i++) {
            if (themeSpinner.getAdapter().getItem(i).toString().equals(theme)) {
                themeSpinner.setSelection(i);
                break;
            }
        }
    }
    
    /**
     * Сохранение настроек уведомлений
     */
    private void saveNotificationSettings() {
        // Сохраняем настройки только локально
        boolean enableNotifications = enableNotificationsSwitch.isChecked();
        boolean pushNotifications = pushNotificationsSwitch.isChecked();
        
        sessionManager.saveSettingBoolean("enable_notifications", enableNotifications);
        sessionManager.saveSettingBoolean("push_notifications", pushNotifications);
        
        Toast.makeText(this, "Настройки уведомлений сохранены", Toast.LENGTH_SHORT).show();
    }
    
    /**
     * Сохранение темы на сервере и локально
     */
    private void saveTheme(String theme) {
        setLoading(true);
        
        // Логируем действие
        android.util.Log.d("SystemSettingsActivity", "Сохраняю тему: " + theme);
        
        // Сначала сохраняем локально
        // ВАЖНО: без префикса "setting_", он добавляется внутри метода saveSettingString
        sessionManager.saveSettingString(WarehouseApplication.THEME_PREF_KEY, theme);
        
        // Логируем для отладки состояние после сохранения
        sessionManager.logCurrentTheme();
        
        // Применяем тему немедленно для всего приложения
        AppCompatDelegate.setDefaultNightMode(getAppCompatDelegateMode(theme));
        
        // Затем сохраняем на сервере для пользователя
        String token = "Bearer " + sessionManager.getToken();
        ThemeRequest request = new ThemeRequest(theme);
        
        apiService.saveTheme(token, request).enqueue(new Callback<SettingsResponse>() {
            @Override
            public void onResponse(Call<SettingsResponse> call, Response<SettingsResponse> response) {
                setLoading(false);
                
                if (response.isSuccessful() && response.body() != null && response.body().isSuccess()) {
                    Toast.makeText(SystemSettingsActivity.this, 
                            "Тема успешно сохранена для пользователя " + username, 
                            Toast.LENGTH_SHORT).show();
                } else {
                    Toast.makeText(SystemSettingsActivity.this, 
                            "Тема сохранена только локально", 
                            Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<SettingsResponse> call, Throwable t) {
                setLoading(false);
                Toast.makeText(SystemSettingsActivity.this, 
                        "Ошибка соединения. Тема сохранена только локально", 
                        Toast.LENGTH_SHORT).show();
            }
        });
    }
    
    /**
     * Применяет новую тему
     */
    private void applyNewTheme(String theme) {
        switch (theme) {
            case WarehouseApplication.THEME_DARK:
                AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_YES);
                break;
            case WarehouseApplication.THEME_LIGHT:
                AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_NO);
                break;
            case WarehouseApplication.THEME_SYSTEM:
            default:
                AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_FOLLOW_SYSTEM);
                break;
        }
    }
    
    /**
     * Проверка корректности ввода при изменении пароля
     */
    private boolean validatePasswordChange() {
        String newPassword = newPasswordEditText.getText().toString().trim();
        String confirmPassword = confirmPasswordEditText.getText().toString().trim();
        
        if (newPassword.isEmpty()) {
            newPasswordEditText.setError("Введите новый пароль");
            return false;
        }
        
        if (confirmPassword.isEmpty()) {
            confirmPasswordEditText.setError("Подтвердите пароль");
            return false;
        }
        
        if (!newPassword.equals(confirmPassword)) {
            confirmPasswordEditText.setError("Пароли не совпадают");
            return false;
        }
        
        return true;
    }
    
    /**
     * Изменение пароля пользователя
     */
    private void changePassword() {
        setLoading(true);
        
        String newPassword = newPasswordEditText.getText().toString().trim();
        String confirmPassword = confirmPasswordEditText.getText().toString().trim();
        
        // Создаем запрос на изменение пароля
        ChangePasswordRequest request = new ChangePasswordRequest(username, newPassword, confirmPassword);
        
        // Отправляем запрос на сервер
        String token = "Bearer " + sessionManager.getToken();
        
        apiService.changePassword(token, request).enqueue(new Callback<SettingsResponse>() {
            @Override
            public void onResponse(Call<SettingsResponse> call, Response<SettingsResponse> response) {
                setLoading(false);
                
                if (response.isSuccessful() && response.body() != null && response.body().isSuccess()) {
                    Toast.makeText(SystemSettingsActivity.this, 
                            "Пароль успешно изменен", 
                            Toast.LENGTH_SHORT).show();
                    
                    // Очищаем поля ввода
                    newPasswordEditText.setText("");
                    confirmPasswordEditText.setText("");
                } else {
                    String errorMessage = "Не удалось изменить пароль";
                    if (response.body() != null) {
                        errorMessage = response.body().getMessage();
                    }
                    
                    Toast.makeText(SystemSettingsActivity.this, 
                            errorMessage, 
                            Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<SettingsResponse> call, Throwable t) {
                setLoading(false);
                Toast.makeText(SystemSettingsActivity.this, 
                        "Ошибка соединения: " + t.getMessage(), 
                        Toast.LENGTH_SHORT).show();
            }
        });
    }
    
    /**
     * Очистка кэша приложения
     */
    private void clearCache() {
        clearCacheProgress.setVisibility(View.VISIBLE);
        clearCacheButton.setEnabled(false);
        
        // Имитация очистки кэша
        new Thread(() -> {
            try {
                // Имитация задержки при очистке кэша
                Thread.sleep(1500);
                
                // Удаляем все файлы из внутреннего кэша
                try {
                    File[] files = getCacheDir().listFiles();
                    if (files != null) {
                        for (File file : files) {
                            file.delete();
                        }
                    }
                } catch (Exception e) {
                    e.printStackTrace();
                }
                
                runOnUiThread(() -> {
                    clearCacheProgress.setVisibility(View.GONE);
                    clearCacheButton.setEnabled(true);
                    Toast.makeText(SystemSettingsActivity.this, 
                            "Кэш успешно очищен", 
                            Toast.LENGTH_SHORT).show();
                });
            } catch (InterruptedException e) {
                e.printStackTrace();
                runOnUiThread(() -> {
                    clearCacheProgress.setVisibility(View.GONE);
                    clearCacheButton.setEnabled(true);
                    Toast.makeText(SystemSettingsActivity.this, 
                            "Ошибка при очистке кэша", 
                            Toast.LENGTH_SHORT).show();
                });
            }
        }).start();
    }
    
    /**
     * Отображение диалога подтверждения очистки кэша
     */
    private void showClearCacheConfirmationDialog() {
        new AlertDialog.Builder(this)
                .setTitle("Очистка кэша")
                .setMessage("Вы уверены, что хотите очистить кэш приложения? Это может привести к временному снижению производительности.")
                .setPositiveButton("Очистить", (dialog, which) -> clearCache())
                .setNegativeButton("Отмена", null)
                .show();
    }
    
    /**
     * Управление состоянием загрузки
     */
    private void setLoading(boolean loading) {
        isLoading = loading;
        
        // Блокировка кнопок во время загрузки
        saveNotificationSettingsButton.setEnabled(!loading);
        clearCacheButton.setEnabled(!loading);
        applyThemeButton.setEnabled(!loading);
        changePasswordButton.setEnabled(!loading);
        testNotificationButton.setEnabled(!loading && enableNotificationsSwitch.isChecked());
    }

    private int getAppCompatDelegateMode(String theme) {
        switch (theme) {
            case WarehouseApplication.THEME_DARK:
                return AppCompatDelegate.MODE_NIGHT_YES;
            case WarehouseApplication.THEME_LIGHT:
                return AppCompatDelegate.MODE_NIGHT_NO;
            case WarehouseApplication.THEME_SYSTEM:
            default:
                return AppCompatDelegate.MODE_NIGHT_FOLLOW_SYSTEM;
        }
    }
} 