package com.example.apk.fragments;

import android.os.Bundle;
import android.text.Editable;
import android.text.TextWatcher;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.view.ViewStub;
import android.widget.EditText;
import android.widget.ImageView;
import android.widget.ProgressBar;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.fragment.app.Fragment;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.example.apk.R;
import com.example.apk.adapters.ContactAdapter;
import com.example.apk.api.ApiClient;
import com.example.apk.api.ApiService;
import com.example.apk.api.UserResponse;
import com.example.apk.models.UserData;
import com.example.apk.utils.SessionManager;

import java.util.ArrayList;
import java.util.List;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

/**
 * Фрагмент для отображения контактов
 */
public class ContactsFragment extends Fragment {
    private static final String TAG = "ContactsFragment";
    
    private RecyclerView contactsRecyclerView;
    private ContactAdapter contactAdapter;
    private EditText searchInput;
    private ProgressBar progressBar;
    private TextView emptyView;
    private TextView sortButton;
    private ViewStub sortPanelStub;
    private View sortPanelView;
    private boolean isSortPanelVisible = false;
    
    private ApiService apiService;
    private SessionManager sessionManager;
    
    private List<UserData> contacts = new ArrayList<>();

    // Хранение вызовов Retrofit для возможности их отмены
    private Call<List<UserResponse>> contactsCall;

    public ContactsFragment() {
        // Пустой конструктор
    }

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container, 
                             @Nullable Bundle savedInstanceState) {
        // Загружаем макет фрагмента
        View rootView = inflater.inflate(R.layout.fragment_contacts, container, false);
        
        // Инициализация API и менеджера сессий
        apiService = ApiClient.getClient().create(ApiService.class);
        sessionManager = new SessionManager(requireContext());
        
        // Инициализация UI
        initViews(rootView);
        
        // Загрузка контактов
        loadContacts();
        
        return rootView;
    }
    
    private void initViews(View view) {
        // Настройка RecyclerView
        contactsRecyclerView = view.findViewById(R.id.contactsRecyclerView);
        contactsRecyclerView.setLayoutManager(new LinearLayoutManager(requireContext()));
        
        // Инициализация адаптера
        contactAdapter = new ContactAdapter(requireContext(), contacts);
        contactsRecyclerView.setAdapter(contactAdapter);
        
        // Инициализация кнопки сортировки
        sortButton = view.findViewById(R.id.sortButton);
        sortButton.setOnClickListener(v -> toggleSortPanel());
        
        // Инициализация панели сортировки (отложенная загрузка)
        sortPanelStub = view.findViewById(R.id.sortPanelStub);
        
        // Поиск
        searchInput = view.findViewById(R.id.searchInput);
        searchInput.addTextChangedListener(new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence s, int start, int count, int after) {}

            @Override
            public void onTextChanged(CharSequence s, int start, int before, int count) {}

            @Override
            public void afterTextChanged(Editable s) {
                contactAdapter.filter(s.toString());
            }
        });
        
        // Индикатор загрузки и пустой список
        progressBar = view.findViewById(R.id.progressBar);
        emptyView = view.findViewById(R.id.emptyView);
    }
    
    @Override
    public void onDestroy() {
        super.onDestroy();
        // Отменяем запросы при уничтожении фрагмента
        if (contactsCall != null) {
            contactsCall.cancel();
        }
    }

    /**
     * Загрузка списка контактов (пользователей) с сервера
     */
    private void loadContacts() {
        showLoading(true);
        
        String token = "Bearer " + sessionManager.getToken();
        Log.d(TAG, "Загрузка контактов, токен: " + token);
        
        // Отменяем предыдущий запрос, если он был
        if (contactsCall != null) {
            contactsCall.cancel();
        }
        
        contactsCall = apiService.getUsers(token);
        contactsCall.enqueue(new Callback<List<UserResponse>>() {
            @Override
            public void onResponse(Call<List<UserResponse>> call, Response<List<UserResponse>> response) {
                // Проверяем, прикреплен ли еще фрагмент
                if (!isAdded()) {
                    return;
                }
                
                showLoading(false);
                
                if (response.isSuccessful() && response.body() != null) {
                    contacts.clear();
                    
                    for (UserResponse userResponse : response.body()) {
                        UserData userData = new UserData(
                            userResponse.getUserId(),
                            userResponse.getUsername(),
                            userResponse.getRoleName(),
                            userResponse.getRoleId()
                        );
                        contacts.add(userData);
                    }
                    
                    contactAdapter.updateContacts(contacts);
                    updateEmptyView();
                    
                    Log.d(TAG, "Загружено контактов: " + contacts.size());
                } else {
                    String errorMessage = "Ошибка загрузки контактов: " + 
                        (response.code() == 401 ? "Необходима авторизация" : 
                         response.code() == 403 ? "Доступ запрещен" : 
                         "Код ответа: " + response.code());
                    Log.e(TAG, errorMessage);
                    
                    if (isAdded()) {
                        Toast.makeText(requireContext(), errorMessage, Toast.LENGTH_SHORT).show();
                    }
                    updateEmptyView();
                }
            }

            @Override
            public void onFailure(Call<List<UserResponse>> call, Throwable t) {
                // Проверяем, прикреплен ли еще фрагмент
                if (!isAdded()) {
                    return;
                }
                
                showLoading(false);
                
                // Не показываем ошибку, если запрос был отменен
                if (call.isCanceled()) {
                    return;
                }
                
                String errorMessage = "Ошибка сети: " + t.getMessage();
                Log.e(TAG, errorMessage, t);
                Toast.makeText(requireContext(), errorMessage, Toast.LENGTH_SHORT).show();
                updateEmptyView();
            }
        });
    }
    
    private void showLoading(boolean isLoading) {
        // Проверяем, прикреплен ли фрагмент к контексту
        if (!isAdded()) {
            return;
        }
        
        if (isLoading) {
            progressBar.setVisibility(View.VISIBLE);
            contactsRecyclerView.setVisibility(View.GONE);
            emptyView.setVisibility(View.GONE);
        } else {
            progressBar.setVisibility(View.GONE);
            // Применяем плавную анимацию появления
            if (contacts.isEmpty()) {
                contactsRecyclerView.setVisibility(View.GONE);
                emptyView.setVisibility(View.VISIBLE);
                emptyView.startAnimation(android.view.animation.AnimationUtils.loadAnimation(
                    requireContext(), R.anim.slow_fade_in));
            } else {
                emptyView.setVisibility(View.GONE);
                contactsRecyclerView.setVisibility(View.VISIBLE);
                // Принудительно запускаем анимацию макета списка при его появлении
                contactsRecyclerView.scheduleLayoutAnimation();
            }
        }
    }
    
    private void updateEmptyView() {
        // Проверяем, прикреплен ли фрагмент к контексту
        if (!isAdded()) {
            return;
        }
        
        if (contacts.isEmpty()) {
            emptyView.setVisibility(View.VISIBLE);
            contactsRecyclerView.setVisibility(View.GONE);
            // Анимация для пустого экрана
            emptyView.startAnimation(android.view.animation.AnimationUtils.loadAnimation(
                requireContext(), R.anim.slow_fade_in));
        } else {
            emptyView.setVisibility(View.GONE);
            contactsRecyclerView.setVisibility(View.VISIBLE);
        }
    }

    @Override
    public void onResume() {
        super.onResume();
        // Обновляем анимацию при возвращении к фрагменту
        if (contactsRecyclerView != null) {
            refreshAvatarAnimations();
        }
    }
    
    /**
     * Обновляет анимацию аватаров для видимых элементов
     */
    private void refreshAvatarAnimations() {
        if (contactsRecyclerView.getAdapter() == null) return;
        
        // Обновляем только видимые элементы
        int count = contactsRecyclerView.getChildCount();
        for (int i = 0; i < count; i++) {
            View view = contactsRecyclerView.getChildAt(i);
            if (view != null) {
                ImageView avatarBackground = view.findViewById(R.id.avatarBackground);
                if (avatarBackground != null && 
                    avatarBackground.getBackground() instanceof android.graphics.drawable.AnimationDrawable) {
                    
                    // Создаем финальную копию для безопасного использования
                    final android.graphics.drawable.AnimationDrawable animDrawable = 
                        (android.graphics.drawable.AnimationDrawable) avatarBackground.getBackground();
                    
                    // Перезапускаем анимацию с эффектом перехода
                    animDrawable.setEnterFadeDuration(1000);
                    animDrawable.setExitFadeDuration(1000);
                    animDrawable.stop();
                    animDrawable.start();
                }
            }
        }
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);
        
        // Настройка обработчика клика по всему экрану для скрытия панели сортировки
        view.setOnClickListener(v -> {
            if (isSortPanelVisible) {
                hideSortPanel();
            }
        });
        
        // Предотвращаем перехват кликов внутри элементов управления
        sortButton.setOnTouchListener((v, event) -> {
            v.onTouchEvent(event);
            return true;
        });
        
        contactsRecyclerView.setOnTouchListener((v, event) -> {
            if (isSortPanelVisible) {
                hideSortPanel();
            }
            return false;
        });
    }

    /**
     * Переключение видимости панели сортировки
     */
    private void toggleSortPanel() {
        if (isSortPanelVisible) {
            hideSortPanel();
        } else {
            showSortPanel();
        }
    }
    
    /**
     * Показ панели сортировки с анимацией
     */
    private void showSortPanel() {
        if (sortPanelView == null) {
            // Первая инициализация панели
            sortPanelView = sortPanelStub.inflate();
            
            // Настройка обработчиков кликов
            View sortByName = sortPanelView.findViewById(R.id.sortByName);
            View sortByRole = sortPanelView.findViewById(R.id.sortByRole);
            
            sortByName.setOnClickListener(v -> {
                contactAdapter.setSortType(ContactAdapter.SORT_BY_NAME);
                hideSortPanel();
            });
            
            sortByRole.setOnClickListener(v -> {
                contactAdapter.setSortType(ContactAdapter.SORT_BY_ROLE);
                hideSortPanel();
            });
        }
        
        // Показываем панель с анимацией
        sortPanelView.setVisibility(View.VISIBLE);
        sortPanelView.startAnimation(android.view.animation.AnimationUtils.loadAnimation(
            requireContext(), R.anim.slide_in));
        
        isSortPanelVisible = true;
    }
    
    /**
     * Скрытие панели сортировки с анимацией
     */
    private void hideSortPanel() {
        if (sortPanelView != null && sortPanelView.getVisibility() == View.VISIBLE) {
            android.view.animation.Animation anim = android.view.animation.AnimationUtils.loadAnimation(
                requireContext(), R.anim.slide_out);
            
            anim.setAnimationListener(new android.view.animation.Animation.AnimationListener() {
                @Override
                public void onAnimationStart(android.view.animation.Animation animation) {}
                
                @Override
                public void onAnimationEnd(android.view.animation.Animation animation) {
                    sortPanelView.setVisibility(View.GONE);
                }
                
                @Override
                public void onAnimationRepeat(android.view.animation.Animation animation) {}
            });
            
            sortPanelView.startAnimation(anim);
            isSortPanelVisible = false;
        }
    }
} 