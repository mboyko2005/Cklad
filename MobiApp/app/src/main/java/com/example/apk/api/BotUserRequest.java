package com.example.apk.api;

import com.google.gson.annotations.SerializedName;

/**
 * Запрос для создания/обновления пользователя бота
 */
public class BotUserRequest {
    
    @SerializedName("telegramId")
    private long telegramId;
    
    @SerializedName("role")
    private String role;
    
    public BotUserRequest(long telegramId, String role) {
        this.telegramId = telegramId;
        this.role = role;
    }
    
    public long getTelegramId() {
        return telegramId;
    }
    
    public void setTelegramId(long telegramId) {
        this.telegramId = telegramId;
    }
    
    public String getRole() {
        return role;
    }
    
    public void setRole(String role) {
        this.role = role;
    }
} 
 

import com.google.gson.annotations.SerializedName;

/**
 * Запрос для создания/обновления пользователя бота
 */
public class BotUserRequest {
    
    @SerializedName("telegramId")
    private long telegramId;
    
    @SerializedName("role")
    private String role;
    
    public BotUserRequest(long telegramId, String role) {
        this.telegramId = telegramId;
        this.role = role;
    }
    
    public long getTelegramId() {
        return telegramId;
    }
    
    public void setTelegramId(long telegramId) {
        this.telegramId = telegramId;
    }
    
    public String getRole() {
        return role;
    }
    
    public void setRole(String role) {
        this.role = role;
    }
} 