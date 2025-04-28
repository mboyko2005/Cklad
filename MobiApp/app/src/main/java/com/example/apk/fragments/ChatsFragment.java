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

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

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
    private static final long CHECK_INTERVAL = 2000; // 2 секунды вместо 15
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
        
        // Настройка поиска
        setupSearchView(view);
        
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
        
        // Добавляем обновление списка чатов при возвращении к фрагменту
        if (!isFirstLoad && userId > 0) {
            // Обновляем список чатов
            chatViewModel.refreshChats(userId);
        }
        isFirstLoad = false;
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
            isFirstLoad = false;
            
            // Принудительно запускаем обновление с сервера
            chatViewModel.refreshChats(userId);
            
            // Добавляем таймаут на случай, если обновление зависнет
            handler.postDelayed(() -> {
                if (isAdded() && swipeRefreshLayout.isRefreshing()) {
                    swipeRefreshLayout.setRefreshing(false);
                    updateTitleVisibility(false);
                    
                    // Проверяем, есть ли уже данные
                    chatViewModel.getAllChats().observe(getViewLifecycleOwner(), chats -> {
                        if (chats == null || chats.isEmpty()) {
                            // Если после таймаута данных всё еще нет, показываем сообщение
                            Toast.makeText(requireContext(), "Не удалось загрузить чаты. Попробуйте снова.", 
                                           Toast.LENGTH_SHORT).show();
                        }
                    });
                }
            }, 10000); // 10 секунд таймаут
        }
    }

    /**
     * Настраивает SearchView
     */
    private void setupSearchView(View view) {
        com.example.apk.views.CenteredSearchView searchView = view.findViewById(R.id.search_view);
        if (searchView != null) {
            // Настройка внешнего вида
            searchView.setIconifiedByDefault(false);
            searchView.setSubmitButtonEnabled(false);
            
            // Прозрачный фон для текстового поля
            int searchPlateId = searchView.getContext().getResources().getIdentifier(
                    "android:id/search_plate", null, null);
            View searchPlate = searchView.findViewById(searchPlateId);
            if (searchPlate != null) {
                searchPlate.setBackgroundColor(android.graphics.Color.TRANSPARENT);
            }
            
            // Обработчик поиска
            searchView.setOnQueryTextListener(new androidx.appcompat.widget.SearchView.OnQueryTextListener() {
                @Override
                public boolean onQueryTextSubmit(String query) {
                    // Обрабатываем поиск при нажатии кнопки поиска
                    return true;
                }

                @Override
                public boolean onQueryTextChange(String newText) {
                    // Фильтруем список чатов при вводе текста
                    if (chatAdapter != null) {
                        chatAdapter.filter(newText);
                    }
                    return true;
                }
            });
        }
    }
    
    /**
     * Фильтрует список чатов по поисковому запросу
     * @param query текст для поиска
     */
    private void filterChats(String query) {
        if (query == null || query.isEmpty()) {
            // Если запрос пустой, возвращаем полный список чатов
            chatViewModel.getAllChats().observe(getViewLifecycleOwner(), chatItems -> {
                if (chatItems != null && !chatItems.isEmpty()) {
                    chatAdapter.updateChats(chatItems);
                }
            });
        } else {
            // Если есть запрос, фильтруем список
            chatViewModel.getAllChats().observe(getViewLifecycleOwner(), chatItems -> {
                if (chatItems != null && !chatItems.isEmpty()) {
                    List<ChatItem> filteredList = new ArrayList<>();
                    String lowerCaseQuery = query.toLowerCase();
                    
                    for (ChatItem item : chatItems) {
                        // Проверяем совпадение по имени чата или тексту последнего сообщения
                        if (item.getChatName().toLowerCase().contains(lowerCaseQuery) || 
                            item.getLastMessage().toLowerCase().contains(lowerCaseQuery)) {
                            filteredList.add(item);
                        }
                    }
                    
                    chatAdapter.updateChats(filteredList);
                }
            });
        }
    }
} 