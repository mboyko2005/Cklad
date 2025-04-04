package com.example.apk.utils;

import android.content.Context;
import android.content.SharedPreferences;
import android.content.SharedPreferences.Editor;

import com.example.apk.models.User;

/**
 * Класс для управления сессией пользователя
 */
public class SessionManager {
    // Имя файла SharedPreferences
    private static final String PREF_NAME = "WarehouseSession";
    
    // Ключи для сохранения данных
    private static final String KEY_IS_LOGGED_IN = "isLoggedIn";
    private static final String KEY_USERNAME = "username";
    private static final String KEY_ROLE = "role";
    private static final String KEY_TOKEN = "token";
    
    // Префикс для настроек приложения
    private static final String SETTINGS_PREFIX = "setting_";
    
    // SharedPreferences и Editor
    private SharedPreferences pref;
    private Editor editor;
    private Context context;
    
    // Конструктор
    public SessionManager(Context context) {
        this.context = context;
        pref = context.getSharedPreferences(PREF_NAME, Context.MODE_PRIVATE);
        editor = pref.edit();
    }
    
    /**
     * Создает сессию пользователя
     */
    public void createLoginSession(User user) {
        editor.putBoolean(KEY_IS_LOGGED_IN, true);
        editor.putString(KEY_USERNAME, user.getUsername());
        editor.putString(KEY_ROLE, user.getRole());
        editor.putString(KEY_TOKEN, user.getToken());
        editor.commit();
    }
    
    /**
     * Проверяет, вошел ли пользователь в систему
     */
    public boolean isLoggedIn() {
        return pref.getBoolean(KEY_IS_LOGGED_IN, false);
    }
    
    /**
     * Возвращает сохраненные данные пользователя
     */
    public User getUserDetails() {
        if (!isLoggedIn()) {
            return null;
        }
        
        String username = pref.getString(KEY_USERNAME, "");
        String role = pref.getString(KEY_ROLE, "");
        String token = pref.getString(KEY_TOKEN, "");
        
        return new User(username, role, token);
    }
    
    /**
     * Возвращает имя пользователя
     */
    public String getUsername() {
        return pref.getString(KEY_USERNAME, "");
    }
    
    /**
     * Возвращает роль пользователя
     */
    public String getRole() {
        return pref.getString(KEY_ROLE, "");
    }
    
    /**
     * Возвращает токен пользователя
     */
    public String getToken() {
        return pref.getString(KEY_TOKEN, "");
    }
    
    /**
     * Очищает данные сессии и завершает сеанс
     */
    public void logout() {
        editor.clear();
        editor.commit();
    }
    
    /**
     * Сохранение настройки типа String
     */
    public void saveSettingString(String key, String value) {
        String fullKey = SETTINGS_PREFIX + key;
        // Получаем редактор каждый раз заново для гарантированного сохранения
        SharedPreferences.Editor editor = pref.edit();
        editor.putString(fullKey, value);
        
        // Для отладки, специально для темы
        if (key.equals("app_theme")) {
            android.util.Log.d("SessionManager", "Сохраняю тему в настройки, ключ: " + fullKey);
            android.util.Log.d("SessionManager", "Значение темы: " + value);
        }
        
        // Важно использовать commit для синхронного применения изменений
        boolean success = editor.commit();
        android.util.Log.d("SessionManager", "Сохранение настройки " + key + " успешно: " + success);
    }
    
    /**
     * Получает строковую настройку
     */
    public String getSettingString(String key, String defaultValue) {
        String fullKey = SETTINGS_PREFIX + key;
        String value = pref.getString(fullKey, defaultValue);
        
        // Для отладки, специально для темы
        if (key.equals("app_theme")) {
            android.util.Log.d("SessionManager", "Получаю тему из настроек, ключ: " + fullKey);
            android.util.Log.d("SessionManager", "Значение темы: " + value);
        }
        
        return value;
    }
    
    /**
     * Сохраняет целочисленную настройку
     */
    public void saveSettingInt(String key, int value) {
        editor.putInt(SETTINGS_PREFIX + key, value);
        editor.commit();
    }
    
    /**
     * Получает целочисленную настройку
     */
    public int getSettingInt(String key, int defaultValue) {
        return pref.getInt(SETTINGS_PREFIX + key, defaultValue);
    }
    
    /**
     * Сохраняет булевую настройку
     */
    public void saveSettingBoolean(String key, boolean value) {
        editor.putBoolean(SETTINGS_PREFIX + key, value);
        editor.commit();
    }
    
    /**
     * Получает булевую настройку
     */
    public boolean getSettingBoolean(String key, boolean defaultValue) {
        return pref.getBoolean(SETTINGS_PREFIX + key, defaultValue);
    }
    
    /**
     * Проверяет, существует ли настройка
     */
    public boolean hasSettingKey(String key) {
        return pref.contains(SETTINGS_PREFIX + key);
    }
    
    /**
     * Удаляет настройку
     */
    public void removeSetting(String key) {
        editor.remove(SETTINGS_PREFIX + key);
        editor.commit();
    }
    
    /**
     * Проверка правильности сохранения темы
     * Этот метод печатает в лог текущую тему для отладки
     */
    public void logCurrentTheme() {
        String key = "app_theme";
        String value = pref.getString(SETTINGS_PREFIX + key, null);
        android.util.Log.d("SessionManager", "Текущая тема: " + value);
        android.util.Log.d("SessionManager", "Все настройки: " + pref.getAll().toString());
    }
    
    /**
     * Получает текущую тему приложения
     * @return Название текущей темы, "Системная" по умолчанию
     */
    public String getTheme() {
        return getSettingString("app_theme", "Системная");
    }
} 