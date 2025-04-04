package com.example.apk.api;

/**
 * Класс для запроса на обновление настроек на сервере
 */
public class SettingsRequest {
    
    private String serverUrl;
    private String apiKey;
    private String apiVersion;
    private boolean enableNotifications;
    private boolean pushNotifications;
    private boolean emailNotifications;
    private boolean enableCaching;
    private String theme;
    
    public SettingsRequest() {
        // Пустой конструктор
    }
    
    // Геттеры и сеттеры
    
    public String getServerUrl() {
        return serverUrl;
    }
    
    public void setServerUrl(String serverUrl) {
        this.serverUrl = serverUrl;
    }
    
    public String getApiKey() {
        return apiKey;
    }
    
    public void setApiKey(String apiKey) {
        this.apiKey = apiKey;
    }
    
    public String getApiVersion() {
        return apiVersion;
    }
    
    public void setApiVersion(String apiVersion) {
        this.apiVersion = apiVersion;
    }
    
    public boolean isEnableNotifications() {
        return enableNotifications;
    }
    
    public void setEnableNotifications(boolean enableNotifications) {
        this.enableNotifications = enableNotifications;
    }
    
    public boolean isPushNotifications() {
        return pushNotifications;
    }
    
    public void setPushNotifications(boolean pushNotifications) {
        this.pushNotifications = pushNotifications;
    }
    
    public boolean isEmailNotifications() {
        return emailNotifications;
    }
    
    public void setEmailNotifications(boolean emailNotifications) {
        this.emailNotifications = emailNotifications;
    }
    
    public boolean isEnableCaching() {
        return enableCaching;
    }
    
    public void setEnableCaching(boolean enableCaching) {
        this.enableCaching = enableCaching;
    }
    
    public String getTheme() {
        return theme;
    }
    
    public void setTheme(String theme) {
        this.theme = theme;
    }
} 