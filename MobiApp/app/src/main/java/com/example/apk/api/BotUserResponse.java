package com.example.apk.api;

import com.google.gson.annotations.SerializedName;

/**
 * Ответ с данными пользователя бота
 */
public class BotUserResponse {
    
    @SerializedName("id")
    private long id;
    
    @SerializedName("telegramId")
    private long telegramId;
    
    @SerializedName("role")
    private String role;
    
    public BotUserResponse(long id, long telegramId, String role) {
        this.id = id;
        this.telegramId = telegramId;
        this.role = role;
    }
    
    public long getId() {
        return id;
    }
    
    public void setId(long id) {
        this.id = id;
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