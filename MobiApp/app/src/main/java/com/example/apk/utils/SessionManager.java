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
} 