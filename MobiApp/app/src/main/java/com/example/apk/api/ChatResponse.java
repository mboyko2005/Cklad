package com.example.apk.api;

import com.google.gson.annotations.SerializedName;

/**
 * Модель ответа с данными чата
 */
public class ChatResponse {
    @SerializedName("userId")
    private int userId;
    
    @SerializedName("username")
    private String username;
    
    @SerializedName("lastMessage")
    private String lastMessage;
    
    @SerializedName("timestamp")
    private String timestamp;
    
    @SerializedName("unreadCount")
    private int unreadCount;
    
    // Геттеры и сеттеры
    public int getUserId() {
        return userId;
    }
    
    public void setUserId(int userId) {
        this.userId = userId;
    }
    
    public String getUsername() {
        return username;
    }
    
    public void setUsername(String username) {
        this.username = username;
    }
    
    public String getLastMessage() {
        return lastMessage;
    }
    
    public void setLastMessage(String lastMessage) {
        this.lastMessage = lastMessage;
    }
    
    public String getTimestamp() {
        return timestamp;
    }
    
    public void setTimestamp(String timestamp) {
        this.timestamp = timestamp;
    }
    
    public int getUnreadCount() {
        return unreadCount;
    }
    
    public void setUnreadCount(int unreadCount) {
        this.unreadCount = unreadCount;
    }
} 