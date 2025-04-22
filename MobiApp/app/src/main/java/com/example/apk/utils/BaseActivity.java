package com.example.apk.utils;

import android.os.Bundle;

import androidx.appcompat.app.AppCompatActivity;
import androidx.appcompat.app.AppCompatDelegate;

import com.example.apk.WarehouseApplication;

/**
 * Базовая активность, которая применяет выбранную тему
 * Все активности должны наследоваться от этого класса
 */
public abstract class BaseActivity extends AppCompatActivity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        // Применяем тему до вызова super.onCreate() и setContentView()
        applyTheme();
        
        // Включаем полноэкранный режим для всех активностей
        getWindow().setFlags(
            android.view.WindowManager.LayoutParams.FLAG_FULLSCREEN,
            android.view.WindowManager.LayoutParams.FLAG_FULLSCREEN
        );
        
        super.onCreate(savedInstanceState);
    }

    /**
     * Применяет выбранную тему из настроек
     */
    private void applyTheme() {
        SessionManager sessionManager = new SessionManager(this);
        String theme = sessionManager.getSettingString(
                WarehouseApplication.THEME_PREF_KEY, 
                WarehouseApplication.THEME_SYSTEM);
        
        // Логирование для отладки
        android.util.Log.d("BaseActivity", "Применяю тему: " + theme + " для " + getClass().getSimpleName());
        sessionManager.logCurrentTheme();
        
        int currentNightMode = getResources().getConfiguration().uiMode & 
                android.content.res.Configuration.UI_MODE_NIGHT_MASK;
        
        boolean shouldRecreate = false;
        
        switch (theme) {
            case WarehouseApplication.THEME_DARK:
                if (currentNightMode != android.content.res.Configuration.UI_MODE_NIGHT_YES) {
                    AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_YES);
                    shouldRecreate = true;
                }
                break;
                
            case WarehouseApplication.THEME_LIGHT:
                if (currentNightMode != android.content.res.Configuration.UI_MODE_NIGHT_NO) {
                    AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_NO);
                    shouldRecreate = true;
                }
                break;
                
            case WarehouseApplication.THEME_SYSTEM:
            default:
                AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_FOLLOW_SYSTEM);
                break;
        }
    }
} 