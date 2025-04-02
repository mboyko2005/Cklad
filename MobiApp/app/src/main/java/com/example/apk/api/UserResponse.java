package com.example.apk.api;

import com.google.gson.annotations.SerializedName;

/**
 * Ответ с данными пользователя
 */
public class UserResponse {
    
    @SerializedName("userID")
    private int userId;
    
    @SerializedName("username")
    private String username;
    
    @SerializedName("roleID")
    private int roleId;
    
    @SerializedName("roleName")
    private String roleName;
    
    public UserResponse(int userId, String username, int roleId, String roleName) {
        this.userId = userId;
        this.username = username;
        this.roleId = roleId;
        this.roleName = roleName;
    }
    
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
    
    public int getRoleId() {
        return roleId;
    }
    
    public void setRoleId(int roleId) {
        this.roleId = roleId;
    }
    
    public String getRoleName() {
        return roleName;
    }
    
    public void setRoleName(String roleName) {
        this.roleName = roleName;
    }
} 