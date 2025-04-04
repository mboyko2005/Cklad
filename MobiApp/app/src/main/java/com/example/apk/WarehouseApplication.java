package com.example.apk;

import android.app.Application;
import android.content.Context;
import android.content.SharedPreferences;

import androidx.appcompat.app.AppCompatDelegate;

/**
 * Основной класс приложения
 */
public class WarehouseApplication extends Application {

    // Ключ для хранения настройки темы
    public static final String THEME_PREF_KEY = "app_theme";
    
    // Возможные значения темы
    public static final String THEME_LIGHT = "Светлая";
    public static final String THEME_DARK = "Темная";
    public static final String THEME_SYSTEM = "Системная";
    
    @Override
    public void onCreate() {
        super.onCreate();
        
        // Применение темы при старте приложения
        applyAppTheme();
    }
    
    /**
     * Применение выбранной темы из настроек
     */
    private void applyAppTheme() {
        // Используем тот же формат Shared Preferences что и в SessionManager
        SharedPreferences prefs = getSharedPreferences("user_session", Context.MODE_PRIVATE);
        String settingPrefix = "setting_";
        String fullKey = settingPrefix + THEME_PREF_KEY;
        String theme = prefs.getString(fullKey, THEME_SYSTEM);
        
        // Логирование для отладки
        android.util.Log.d("WarehouseApplication", "Применяю глобальную тему: " + theme);
        android.util.Log.d("WarehouseApplication", "Ключ настройки: " + fullKey);
        android.util.Log.d("WarehouseApplication", "Все настройки: " + prefs.getAll().toString());
        
        // Устанавливаем режим темы для всего приложения
        switch (theme) {
            case THEME_DARK:
                // Принудительно устанавливаем темную тему
                android.util.Log.d("WarehouseApplication", "Устанавливаю темную тему");
                AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_YES);
                break;
                
            case THEME_LIGHT:
                // Принудительно устанавливаем светлую тему
                android.util.Log.d("WarehouseApplication", "Устанавливаю светлую тему");
                AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_NO);
                break;
                
            case THEME_SYSTEM:
            default:
                // Следуем системным настройкам
                android.util.Log.d("WarehouseApplication", "Устанавливаю системную тему");
                AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_FOLLOW_SYSTEM);
                break;
        }
    }
} 