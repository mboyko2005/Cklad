package com.example.apk.api;

import com.google.gson.annotations.SerializedName;

/**
 * Запрос для создания/обновления пользователя
 */
public class UserRequest {
    
    @SerializedName("username")
    private String username;
    
    @SerializedName("password")
    private String password;
    
    @SerializedName("roleId")
    private int roleId;
    
    /**
     * Конструктор для создания пользователя (требуются все поля)
     */
    public UserRequest(String username, String password, int roleId) {
        this.username = username;
        this.password = password;
        this.roleId = roleId;
    }
    
    /**
     * Конструктор для обновления без пароля (пароль не меняется)
     * Внимание: не подходит для обновления на сервере, т.к. сервер требует пароль
     */
    public UserRequest(String username, int roleId) {
        this.username = username;
        // Указываем пустую строку вместо null, чтобы избежать BadRequest
        this.password = "";
        this.roleId = roleId;
    }
    
    public String getUsername() {
        return username;
    }
    
    public void setUsername(String username) {
        this.username = username;
    }
    
    public String getPassword() {
        return password;
    }
    
    public void setPassword(String password) {
        this.password = password;
    }
    
    public int getRoleId() {
        return roleId;
    }
    
    public void setRoleId(int roleId) {
        this.roleId = roleId;
    }
} 