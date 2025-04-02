package com.example.apk.models;

/**
 * Расширенная модель данных пользователя для управления пользователями
 */
public class UserData {
    private int userId;
    private String username;
    private String roleName;
    private int roleId;

    public UserData(int userId, String username, String roleName, int roleId) {
        this.userId = userId;
        this.username = username;
        this.roleName = roleName;
        this.roleId = roleId;
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

    public String getRoleName() {
        return roleName;
    }

    public void setRoleName(String roleName) {
        this.roleName = roleName;
    }

    public int getRoleId() {
        return roleId;
    }

    public void setRoleId(int roleId) {
        this.roleId = roleId;
    }
} 