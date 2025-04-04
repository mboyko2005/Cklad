package com.example.apk.api;

import com.google.gson.annotations.SerializedName;

/**
 * Ответ на операции с настройками пользователя
 */
public class SettingsResponse {
    
    @SerializedName("success")
    private boolean success;
    
    @SerializedName("message")
    private String message;
    
    public SettingsResponse(boolean success, String message) {
        this.success = success;
        this.message = message;
    }
    
    // Геттеры и сеттеры
    
    public boolean isSuccess() {
        return success;
    }
    
    public void setSuccess(boolean success) {
        this.success = success;
    }
    
    public String getMessage() {
        return message;
    }
    
    public void setMessage(String message) {
        this.message = message;
    }
} 