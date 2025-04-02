package com.example.apk.fragments;

import android.content.Intent;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.fragment.app.Fragment;

import com.example.apk.LoginActivity;
import com.example.apk.R;
import com.example.apk.admin.ManageUsersActivity;
import com.example.apk.admin.ManageInventoryActivity;
import com.example.apk.admin.ReportsActivity;
import com.example.apk.utils.SessionManager;

/**
 * Фрагмент настроек с разделами администрирования
 */
public class SettingsFragment extends Fragment {

    private SessionManager sessionManager;
    private LinearLayout manageUsersOption;
    private LinearLayout manageInventoryOption;
    private LinearLayout reportsOption;
    private LinearLayout systemSettingsOption;
    private LinearLayout botOption;
    private Button logoutButton;

    public SettingsFragment() {
        // Пустой конструктор
    }

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container, 
                             @Nullable Bundle savedInstanceState) {
        // Загружаем макет фрагмента
        View view = inflater.inflate(R.layout.fragment_settings, container, false);
        
        // Инициализация менеджера сессий
        sessionManager = new SessionManager(requireContext());
        
        // Проверка роли пользователя
        if (!sessionManager.getRole().equals("Администратор")) {
            // Если пользователь не администратор, перенаправляем на экран входа
            Toast.makeText(requireContext(), "Доступ запрещен", Toast.LENGTH_SHORT).show();
            Intent intent = new Intent(requireContext(), LoginActivity.class);
            startActivity(intent);
            requireActivity().finish();
            return view;
        }
        
        // Инициализация элементов UI
        initViews(view);
        
        // Настройка слушателей событий
        setupListeners();
        
        return view;
    }
    
    /**
     * Инициализация элементов интерфейса
     */
    private void initViews(View view) {
        manageUsersOption = view.findViewById(R.id.manage_users_option);
        manageInventoryOption = view.findViewById(R.id.manage_inventory_option);
        reportsOption = view.findViewById(R.id.reports_option);
        systemSettingsOption = view.findViewById(R.id.system_settings_option);
        botOption = view.findViewById(R.id.bot_option);
        logoutButton = view.findViewById(R.id.logout_button);
    }
    
    /**
     * Настройка слушателей событий для элементов интерфейса
     */
    private void setupListeners() {
        // Управление пользователями
        manageUsersOption.setOnClickListener(v -> {
            // Открываем экран управления пользователями
            Intent intent = new Intent(requireContext(), ManageUsersActivity.class);
            startActivity(intent);
        });
        
        // Управление складом
        manageInventoryOption.setOnClickListener(v -> {
            // Открываем экран управления складом
            Intent intent = new Intent(requireContext(), ManageInventoryActivity.class);
            startActivity(intent);
        });
        
        // Аналитика и отчеты
        reportsOption.setOnClickListener(v -> {
            // Открываем экран аналитики и отчетов
            Intent intent = new Intent(requireContext(), ReportsActivity.class);
            startActivity(intent);
        });
        
        // Настройки системы
        systemSettingsOption.setOnClickListener(v -> {
            Toast.makeText(requireContext(), "Настройки системы (в разработке)", Toast.LENGTH_SHORT).show();
        });
        
        // Управление ботом
        botOption.setOnClickListener(v -> {
            // Открываем экран управления ботом
            Intent intent = new Intent(requireContext(), com.example.apk.admin.ManageBotActivity.class);
            startActivity(intent);
        });
        
        // Выход из аккаунта
        logoutButton.setOnClickListener(v -> {
            sessionManager.logout();
            Intent intent = new Intent(requireContext(), LoginActivity.class);
            startActivity(intent);
            requireActivity().finish();
        });
    }
} 