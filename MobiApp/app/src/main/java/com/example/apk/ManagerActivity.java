package com.example.apk;

import android.os.Bundle;
import android.widget.TextView;

import androidx.appcompat.app.AppCompatActivity;

import com.example.apk.utils.SessionManager;

/**
 * Активность для роли "Менеджер"
 */
public class ManagerActivity extends AppCompatActivity {

    private SessionManager sessionManager;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_manager);

        // Инициализация менеджера сессий
        sessionManager = new SessionManager(this);
        
        // Проверка, вошел ли пользователь в систему
        if (!sessionManager.isLoggedIn() || !sessionManager.getRole().equals("Менеджер")) {
            finish();
            return;
        }

        // Отображение имени пользователя
        TextView usernameTitleTextView = findViewById(R.id.usernameTitleTextView);
        usernameTitleTextView.setText("Здравствуйте, " + sessionManager.getUsername() + "!");
    }
} 