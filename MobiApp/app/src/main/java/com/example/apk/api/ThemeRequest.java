package com.example.apk.api;

import com.google.gson.annotations.SerializedName;

/**
 * Запрос на сохранение темы пользователя
 */
public class ThemeRequest {
    
    @SerializedName("theme")
    private String theme;
    
    public ThemeRequest(String theme) {
        this.theme = theme;
    }
    
    // Геттеры и сеттеры
    
    public String getTheme() {
        return theme;
    }
    
    public void setTheme(String theme) {
        this.theme = theme;
    }
} 