package com.example.apk.fragments;

import android.content.Context;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.LinearLayout;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.fragment.app.Fragment;
import androidx.fragment.app.FragmentTransaction;
import androidx.lifecycle.ViewModelProvider;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout;

import com.example.apk.R;
import com.example.apk.adapters.ChatAdapter;
import com.example.apk.models.ChatItem;
import com.example.apk.utils.SessionManager;
import com.example.apk.viewmodels.ChatViewModel;

import java.util.ArrayList;
import java.util.List;

/**
 * Фрагмент для отображения чатов
 */
public class ChatsFragment extends Fragment {

    private RecyclerView recyclerViewChats;
    private ChatAdapter chatAdapter;
    private SessionManager sessionManager;
    private ChatViewModel chatViewModel;
    private SwipeRefreshLayout swipeRefreshLayout;
    private TextView chatsTitleView;
    private LinearLayout loadingLayout;
    
    // Интервал проверки новых чатов (в миллисекундах)
    private static final long CHECK_INTERVAL = 30000; // 30 секунд
    private Handler handler;
    private Runnable checkRunnable;
    private int userId;
    
    // Флаг для отслеживания первого запуска
    private boolean isFirstLoad = true;

    public ChatsFragment() {
        // Пустой конструктор
    }

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container, 
                             @Nullable Bundle savedInstanceState) {
        // Загружаем макет фрагмента
        return inflater.inflate(R.layout.fragment_chats, container, false);
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);

        // Инициализация менеджера сессии
        sessionManager = new SessionManager(requireContext());
        
        // Получаем ID пользователя
        SharedPreferences prefs = requireContext()
            .getSharedPreferences("UserPrefs", Context.MODE_PRIVATE);
        userId = prefs.getInt("userId", -1);
        
        // Настройка ViewModel
        chatViewModel = new ViewModelProvider(this).get(ChatViewModel.class);
        
        // Инициализация UI элементов
        chatsTitleView = view.findViewById(R.id.chats_title);
        loadingLayout = view.findViewById(R.id.loading_layout);
        swipeRefreshLayout = view.findViewById(R.id.swipe_refresh_layout);
        
        // Настройка RecyclerView и адаптера
        recyclerViewChats = view.findViewById(R.id.recycler_view_chats);
        recyclerViewChats.setLayoutManager(new LinearLayoutManager(requireContext()));
        chatAdapter = new ChatAdapter(requireContext(), new ArrayList<>());
        recyclerViewChats.setAdapter(chatAdapter);
        
        // Настраиваем SwipeRefreshLayout
        swipeRefreshLayout.setColorSchemeResources(android.R.color.holo_blue_light);
        swipeRefreshLayout.setOnRefreshListener(() -> {
            // Ручное обновление списка чатов по свайпу
            chatViewModel.refreshChats(userId);
        });
        
        // Настройка обработчика кликов по чатам
        chatAdapter.setOnChatClickListener((partnerId, chatName) -> {
            // Открываем фрагмент переписки
            ConversationFragment convFrag = ConversationFragment.newInstance(
                partnerId,
                chatName
            );
            FragmentTransaction ft = getParentFragmentManager().beginTransaction();
            ft.replace(R.id.fragment_container, convFrag);
            ft.addToBackStack(null);
            ft.commit();
        });
        
        // Настройка наблюдателей LiveData
        chatViewModel.getAllChats().observe(getViewLifecycleOwner(), chatItems -> {
            // Обновляем адаптер только если есть данные
            if (chatItems != null && !chatItems.isEmpty()) {
                chatAdapter.updateChats(chatItems);
            }
        });
        
        chatViewModel.isRefreshing().observe(getViewLifecycleOwner(), isRefreshing -> {
            // Отображаем/скрываем индикатор обновления
            swipeRefreshLayout.setRefreshing(isRefreshing);
            updateTitleVisibility(isRefreshing);
        });
        
        chatViewModel.isChecking().observe(getViewLifecycleOwner(), isChecking -> {
            // При проверке не показываем индикатор загрузки, чтобы не мешать пользователю
        });
        
        chatViewModel.hasChanges().observe(getViewLifecycleOwner(), hasChanges -> {
            // Обработка события наличия изменений (если нужно)
        });
        
        chatViewModel.getErrorMessage().observe(getViewLifecycleOwner(), errorMessage -> {
            if (errorMessage != null && !errorMessage.isEmpty()) {
                Toast.makeText(requireContext(), errorMessage, Toast.LENGTH_SHORT).show();
            }
        });
        
        // Настройка периодической проверки новых чатов
        handler = new Handler(Looper.getMainLooper());
        checkRunnable = () -> {
            if (userId > 0 && isAdded()) {
                // Проверяем наличие новых чатов в фоне
                chatViewModel.checkForNewChats(userId);
            }
            // Планируем следующую проверку
            handler.postDelayed(checkRunnable, CHECK_INTERVAL);
        };
        
        // Загружаем чаты при первом запуске
        loadChats();
    }
    
    @Override
    public void onResume() {
        super.onResume();
        // Запускаем периодическую проверку
        handler.postDelayed(checkRunnable, CHECK_INTERVAL);
        
        // Удаляем вызов проверки при каждом возврате к фрагменту
        // Теперь проверка будет происходить только по таймеру
    }
    
    @Override
    public void onPause() {
        super.onPause();
        // Останавливаем периодическую проверку
        handler.removeCallbacks(checkRunnable);
    }
    
    /**
     * Обновляет видимость заголовка и индикатора загрузки
     */
    private void updateTitleVisibility(boolean isLoading) {
        if (isLoading) {
            chatsTitleView.setVisibility(View.GONE);
            loadingLayout.setVisibility(View.VISIBLE);
        } else {
            chatsTitleView.setVisibility(View.VISIBLE);
            loadingLayout.setVisibility(View.GONE);
        }
    }

    /**
     * Загружает список чатов из кэша или с сервера
     */
    private void loadChats() {
        if (userId < 0) {
            return; // userId не найден
        }
        
        if (isFirstLoad) {
            // При первом запуске просто запускаем обновление с сервера
            // или используем LiveData из ViewModel, которая уже работает асинхронно
            isFirstLoad = false;
            
            // Показываем данные из кэша через LiveData (работает асинхронно)
            // Проверяем наличие данных через обсервер
            chatViewModel.getAllChats().observe(getViewLifecycleOwner(), chats -> {
                if (chats == null || chats.isEmpty()) {
                    // Если кэш пуст, запускаем обновление с сервера
                    chatViewModel.refreshChats(userId);
                }
            });
        }
        // Проверка обновлений будет происходить автоматически по таймеру
    }
} 