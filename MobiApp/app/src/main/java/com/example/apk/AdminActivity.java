package com.example.apk;

import android.os.Bundle;

import androidx.appcompat.app.AppCompatActivity;
import androidx.fragment.app.Fragment;

import com.example.apk.fragments.ChatsFragment;
import com.example.apk.fragments.ContactsFragment;
import com.example.apk.fragments.SettingsFragment;
import com.example.apk.utils.SessionManager;
import com.google.android.material.bottomnavigation.BottomNavigationView;

/**
 * Активность для роли "Администратор"
 */
public class AdminActivity extends AppCompatActivity {

    private SessionManager sessionManager;
    private BottomNavigationView bottomNavigationView;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_admin);

        // Инициализация менеджера сессий
        sessionManager = new SessionManager(this);
        
        // Проверка, вошел ли пользователь в систему
        if (!sessionManager.isLoggedIn() || !sessionManager.getRole().equals("Администратор")) {
            finish();
            return;
        }
        
        // Инициализация нижней навигации
        bottomNavigationView = findViewById(R.id.bottom_navigation);
        bottomNavigationView.setOnItemSelectedListener(item -> {
            Fragment selectedFragment = null;
            int itemId = item.getItemId();
            
            if (itemId == R.id.nav_contacts) {
                selectedFragment = new ContactsFragment();
            } else if (itemId == R.id.nav_chats) {
                selectedFragment = new ChatsFragment();
            } else if (itemId == R.id.nav_settings) {
                selectedFragment = new SettingsFragment();
            }
            
            if (selectedFragment != null) {
                getSupportFragmentManager().beginTransaction()
                        .replace(R.id.fragment_container, selectedFragment)
                        .commit();
                return true;
            }
            
            return false;
        });
        
        // Устанавливаем по умолчанию фрагмент с чатами
        getSupportFragmentManager().beginTransaction()
                .replace(R.id.fragment_container, new ChatsFragment())
                .commit();
        
        // Выбираем пункт "Чаты" в меню
        bottomNavigationView.setSelectedItemId(R.id.nav_chats);
    }
} 