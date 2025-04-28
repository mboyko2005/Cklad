package com.example.apk.fragments;

import android.content.Intent;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.TextView;
import android.widget.Toast;
import android.widget.FrameLayout;
import android.widget.ProgressBar;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.fragment.app.Fragment;

import com.example.apk.LoginActivity;
import com.example.apk.R;
import com.example.apk.admin.ManageUsersActivity;
import com.example.apk.admin.ManageInventoryActivity;
import com.example.apk.admin.ReportsActivity;
import com.example.apk.utils.SessionManager;

import java.io.File;
import java.text.DecimalFormat;

import pl.droidsonroids.gif.GifImageView;

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
    
    // Элементы UI для очистки кэша
    private TextView cacheSizeText;
    private Button clearCacheButton;
    private FrameLayout cacheCleaningContainer;
    private GifImageView duckAnimation;
    private ProgressBar cacheProgressBar;
    private TextView cleaningProgressText;

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
        
        // Вычисляем и отображаем размер кэша
        updateCacheSize();
        
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
        
        // Инициализация элементов для очистки кэша
        cacheSizeText = view.findViewById(R.id.cache_size_text);
        clearCacheButton = view.findViewById(R.id.clear_cache_button);
        cacheCleaningContainer = view.findViewById(R.id.cache_cleaning_container);
        duckAnimation = view.findViewById(R.id.duck_animation);
        cacheProgressBar = view.findViewById(R.id.cache_progress_bar);
        cleaningProgressText = view.findViewById(R.id.cleaning_progress_text);
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
            // Открываем экран настроек системы
            Intent intent = new Intent(requireContext(), com.example.apk.admin.SystemSettingsActivity.class);
            startActivity(intent);
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
        
        // Настройка кнопки очистки кэша
        clearCacheButton.setOnClickListener(v -> {
            startCacheCleaning();
        });
    }
    
    /**
     * Обновляет информацию о размере кэша
     */
    private void updateCacheSize() {
        new Thread(() -> {
            long size = calculateCacheSize();
            requireActivity().runOnUiThread(() -> {
                cacheSizeText.setText(formatSize(size));
            });
        }).start();
    }
    
    /**
     * Вычисляет размер кэша приложения
     */
    private long calculateCacheSize() {
        long size = 0;
        File cacheDir = requireContext().getCacheDir();
        File externalCacheDir = requireContext().getExternalCacheDir();
        
        size += getDirSize(cacheDir);
        if (externalCacheDir != null) {
            size += getDirSize(externalCacheDir);
        }
        
        return size;
    }
    
    /**
     * Рекурсивно вычисляет размер директории
     */
    private long getDirSize(File dir) {
        long size = 0;
        if (dir == null || !dir.exists()) {
            return 0;
        }
        
        File[] files = dir.listFiles();
        if (files == null) {
            return 0;
        }
        
        for (File file : files) {
            if (file.isFile()) {
                size += file.length();
            } else {
                size += getDirSize(file);
            }
        }
        
        return size;
    }
    
    /**
     * Форматирует размер в байтах в читаемый формат
     */
    private String formatSize(long size) {
        DecimalFormat df = new DecimalFormat("0.00");
        float sizeKb = size / 1024f;
        float sizeMb = sizeKb / 1024f;
        
        if (sizeMb >= 1.0f) {
            return df.format(sizeMb) + " Мб";
        } else if (sizeKb >= 1.0f) {
            return df.format(sizeKb) + " Кб";
        } else {
            return size + " байт";
        }
    }
    
    /**
     * Запускает процесс очистки кэша с анимацией
     */
    private void startCacheCleaning() {
        // Показываем контейнер с анимацией
        cacheCleaningContainer.setVisibility(View.VISIBLE);
        // Отключаем кнопку очистки кэша
        clearCacheButton.setEnabled(false);
        
        // Сбрасываем индикаторы прогресса
        cacheProgressBar.setProgress(0);
        cleaningProgressText.setText("0%");
        
        // Запускаем очистку кэша в отдельном потоке
        new Thread(() -> {
            try {
                // Имитация процесса очистки кэша
                int totalSteps = 10;
                
                for (int i = 1; i <= totalSteps; i++) {
                    // Вычисляем процент выполнения
                    final int progress = i * 100 / totalSteps;
                    
                    // Если это первый шаг, начинаем очистку внутреннего кэша
                    if (i == 1) {
                        clearInternalCache();
                    }
                    // Если на середине, очищаем внешний кэш
                    else if (i == totalSteps / 2) {
                        clearExternalCache();
                    }
                    
                    // Обновляем UI в главном потоке
                    requireActivity().runOnUiThread(() -> {
                        cacheProgressBar.setProgress(progress);
                        cleaningProgressText.setText(progress + "%");
                    });
                    
                    // Имитация задержки при выполнении операции
                    Thread.sleep(300);
                }
                
                // Обновляем размер кэша после очистки
                long newSize = calculateCacheSize();
                final String formattedSize = formatSize(newSize);
                
                // Обновляем UI в главном потоке после завершения
                requireActivity().runOnUiThread(() -> {
                    // Обновляем отображаемый размер кэша
                    cacheSizeText.setText(formattedSize);
                    
                    // Скрываем анимацию с задержкой
                    new android.os.Handler().postDelayed(() -> {
                        cacheCleaningContainer.setVisibility(View.GONE);
                        clearCacheButton.setEnabled(true);
                        Toast.makeText(requireContext(), "Кэш успешно очищен", Toast.LENGTH_SHORT).show();
                    }, 500);
                });
                
            } catch (Exception e) {
                e.printStackTrace();
                // В случае ошибки обновляем UI
                requireActivity().runOnUiThread(() -> {
                    cacheCleaningContainer.setVisibility(View.GONE);
                    clearCacheButton.setEnabled(true);
                    Toast.makeText(requireContext(), "Ошибка при очистке кэша", Toast.LENGTH_SHORT).show();
                });
            }
        }).start();
    }
    
    /**
     * Очищает внутренний кэш приложения
     */
    private void clearInternalCache() {
        File cacheDir = requireContext().getCacheDir();
        deleteDir(cacheDir);
    }
    
    /**
     * Очищает внешний кэш приложения (если доступен)
     */
    private void clearExternalCache() {
        File externalCacheDir = requireContext().getExternalCacheDir();
        if (externalCacheDir != null) {
            deleteDir(externalCacheDir);
        }
    }
    
    /**
     * Рекурсивно удаляет содержимое директории
     */
    private boolean deleteDir(File dir) {
        if (dir == null || !dir.exists() || !dir.isDirectory()) {
            return false;
        }
        
        File[] files = dir.listFiles();
        if (files != null) {
            for (File file : files) {
                if (file.isDirectory()) {
                    deleteDir(file);
                } else {
                    file.delete();
                }
            }
        }
        
        // Директорию кэша не удаляем, только очищаем содержимое
        return true;
    }
    
    @Override
    public void onResume() {
        super.onResume();
        // Обновляем размер кэша при возвращении к фрагменту
        updateCacheSize();
    }
} 