package com.example.apk.api;

import com.google.gson.annotations.SerializedName;

/**
 * Запрос на изменение пароля пользователя
 */
public class ChangePasswordRequest {
    
    @SerializedName("username")
    private String username;
    
    @SerializedName("newPassword")
    private String newPassword;
    
    @SerializedName("confirmPassword")
    private String confirmPassword;
    
    public ChangePasswordRequest(String username, String newPassword, String confirmPassword) {
        this.username = username;
        this.newPassword = newPassword;
        this.confirmPassword = confirmPassword;
    }
    
    // Геттеры и сеттеры
    
    public String getUsername() {
        return username;
    }
    
    public void setUsername(String username) {
        this.username = username;
    }
    
    public String getNewPassword() {
        return newPassword;
    }
    
    public void setNewPassword(String newPassword) {
        this.newPassword = newPassword;
    }
    
    public String getConfirmPassword() {
        return confirmPassword;
    }
    
    public void setConfirmPassword(String confirmPassword) {
        this.confirmPassword = confirmPassword;
    }
} 