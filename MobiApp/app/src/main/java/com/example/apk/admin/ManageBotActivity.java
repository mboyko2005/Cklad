package com.example.apk.admin;

import androidx.appcompat.app.AppCompatActivity;
import androidx.cardview.widget.CardView;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout;

import android.app.AlertDialog;
import android.content.DialogInterface;
import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ImageButton;
import android.widget.ProgressBar;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;

import com.example.apk.R;
import com.example.apk.adapters.BotUserAdapter;
import com.example.apk.api.ApiClient;
import com.example.apk.api.ApiService;
import com.example.apk.api.BotUserRequest;
import com.example.apk.api.BotUserResponse;
import com.example.apk.utils.SessionManager;

import java.util.ArrayList;
import java.util.List;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

public class ManageBotActivity extends AppCompatActivity implements BotUserAdapter.BotUserItemClickListener {

    private ImageButton backButton;
    private CardView addBotUserCard;
    private EditText telegramIdEditText;
    private Spinner roleSpinner;
    private Button addBotUserButton;
    private Button clearFormButton;
    private RecyclerView botUsersRecyclerView;
    private ProgressBar loadingProgressBar;
    private SwipeRefreshLayout swipeRefreshLayout;
    private TextView noDataTextView;

    private ApiService apiService;
    private String authToken;
    private SessionManager sessionManager;

    private BotUserAdapter botUserAdapter;
    private List<BotUserResponse> botUsers = new ArrayList<>();
    private List<String> roles = new ArrayList<>();
    private Long selectedBotUserId = null;
    private boolean isEditing = false;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_manage_bot);

        // Инициализируем API и токен авторизации
        apiService = ApiClient.getClient().create(ApiService.class);
        sessionManager = new SessionManager(this);
        authToken = "Bearer " + sessionManager.getToken();

        // Инициализируем компоненты UI
        initUI();

        // Настраиваем слушатели событий
        setupListeners();
    }
    
    @Override
    protected void onResume() {
        super.onResume();
        // Загружаем данные после того, как активность полностью восстановлена
        loadBotUsers();
    }

    private void initUI() {
        backButton = findViewById(R.id.backButton);
        addBotUserCard = findViewById(R.id.addBotUserCard);
        telegramIdEditText = findViewById(R.id.telegramIdEditText);
        roleSpinner = findViewById(R.id.roleSpinner);
        addBotUserButton = findViewById(R.id.addBotUserButton);
        clearFormButton = findViewById(R.id.clearFormButton);
        botUsersRecyclerView = findViewById(R.id.botUsersRecyclerView);
        loadingProgressBar = findViewById(R.id.loadingProgressBar);
        swipeRefreshLayout = findViewById(R.id.swipeRefreshLayout);
        noDataTextView = findViewById(R.id.noDataTextView);

        // Настраиваем RecyclerView
        botUserAdapter = new BotUserAdapter(this, botUsers);
        botUsersRecyclerView.setLayoutManager(new LinearLayoutManager(this));
        botUsersRecyclerView.setAdapter(botUserAdapter);

        // Заполняем спиннер ролями
        roles.add("Администратор");
        roles.add("Менеджер");
        roles.add("Сотрудник");
        
        ArrayAdapter<String> roleAdapter = new ArrayAdapter<>(
                this, android.R.layout.simple_spinner_item, roles);
        roleAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        roleSpinner.setAdapter(roleAdapter);
    }

    private void setupListeners() {
        // Кнопка назад
        backButton.setOnClickListener(v -> finish());

        // Обновление списка при свайпе вниз
        swipeRefreshLayout.setOnRefreshListener(this::loadBotUsers);

        // Кнопка добавления/обновления пользователя бота
        addBotUserButton.setOnClickListener(v -> {
            if (validateForm()) {
                if (isEditing && selectedBotUserId != null) {
                    updateBotUser();
                } else {
                    createBotUser();
                }
            }
        });

        // Кнопка очистки формы
        clearFormButton.setOnClickListener(v -> clearForm());
    }

    private void loadBotUsers() {
        try {
            showLoading(true);
            setUiEnabled(false);
            
            Call<List<BotUserResponse>> call = apiService.getBotUsers(authToken);
            call.enqueue(new Callback<List<BotUserResponse>>() {
                @Override
                public void onResponse(Call<List<BotUserResponse>> call, Response<List<BotUserResponse>> response) {
                    showLoading(false);
                    setUiEnabled(true);
                    
                    if (response.isSuccessful() && response.body() != null) {
                        botUsers.clear();
                        botUsers.addAll(response.body());
                        botUserAdapter.notifyDataSetChanged();
                        
                        // Отображаем сообщение, если нет данных
                        if (botUsers.isEmpty()) {
                            noDataTextView.setVisibility(View.VISIBLE);
                        } else {
                            noDataTextView.setVisibility(View.GONE);
                        }
                    } else {
                        if (response.code() == 401) {
                            // Токен устарел, перенаправляем на экран входа
                            sessionManager.logout();
                            Toast.makeText(ManageBotActivity.this, 
                                    "Сессия истекла. Пожалуйста, войдите снова", 
                                    Toast.LENGTH_SHORT).show();
                            startActivity(new Intent(ManageBotActivity.this, com.example.apk.LoginActivity.class));
                            finish();
                        } else {
                            Toast.makeText(ManageBotActivity.this, 
                                    "Ошибка при загрузке пользователей: " + response.message(), 
                                    Toast.LENGTH_SHORT).show();
                        }
                    }
                }

                @Override
                public void onFailure(Call<List<BotUserResponse>> call, Throwable t) {
                    showLoading(false);
                    setUiEnabled(true);
                    Toast.makeText(ManageBotActivity.this, 
                            "Ошибка соединения: " + t.getMessage(), 
                            Toast.LENGTH_SHORT).show();
                }
            });
        } catch (Exception e) {
            showLoading(false);
            setUiEnabled(true);
            Toast.makeText(ManageBotActivity.this, 
                    "Произошла ошибка: " + e.getMessage(), 
                    Toast.LENGTH_SHORT).show();
        }
    }

    private void createBotUser() {
        try {
            long telegramId = Long.parseLong(telegramIdEditText.getText().toString());
            String role = roleSpinner.getSelectedItem().toString();
            
            BotUserRequest request = new BotUserRequest(telegramId, role);
            
            showLoading(true);
            setUiEnabled(false);
            Call<BotUserResponse> call = apiService.createBotUser(authToken, request);
            call.enqueue(new Callback<BotUserResponse>() {
                @Override
                public void onResponse(Call<BotUserResponse> call, Response<BotUserResponse> response) {
                    showLoading(false);
                    setUiEnabled(true);
                    
                    if (response.isSuccessful()) {
                        Toast.makeText(ManageBotActivity.this, 
                                "Пользователь успешно добавлен", 
                                Toast.LENGTH_SHORT).show();
                        clearForm();
                        loadBotUsers();
                    } else {
                        if (response.code() == 401) {
                            // Токен устарел, перенаправляем на экран входа
                            sessionManager.logout();
                            Toast.makeText(ManageBotActivity.this, 
                                    "Сессия истекла. Пожалуйста, войдите снова", 
                                    Toast.LENGTH_SHORT).show();
                            startActivity(new Intent(ManageBotActivity.this, com.example.apk.LoginActivity.class));
                            finish();
                        } else {
                            Toast.makeText(ManageBotActivity.this, 
                                    "Ошибка при добавлении пользователя: " + response.message(), 
                                    Toast.LENGTH_SHORT).show();
                        }
                    }
                }

                @Override
                public void onFailure(Call<BotUserResponse> call, Throwable t) {
                    showLoading(false);
                    setUiEnabled(true);
                    Toast.makeText(ManageBotActivity.this, 
                            "Ошибка соединения: " + t.getMessage(), 
                            Toast.LENGTH_SHORT).show();
                }
            });
        } catch (Exception e) {
            showLoading(false);
            setUiEnabled(true);
            Toast.makeText(ManageBotActivity.this, 
                    "Произошла ошибка: " + e.getMessage(), 
                    Toast.LENGTH_SHORT).show();
        }
    }

    private void updateBotUser() {
        try {
            long telegramId = Long.parseLong(telegramIdEditText.getText().toString());
            String role = roleSpinner.getSelectedItem().toString();
            
            BotUserRequest request = new BotUserRequest(telegramId, role);
            
            showLoading(true);
            setUiEnabled(false);
            Call<BotUserResponse> call = apiService.updateBotUser(authToken, selectedBotUserId, request);
            call.enqueue(new Callback<BotUserResponse>() {
                @Override
                public void onResponse(Call<BotUserResponse> call, Response<BotUserResponse> response) {
                    showLoading(false);
                    setUiEnabled(true);
                    
                    if (response.isSuccessful()) {
                        Toast.makeText(ManageBotActivity.this, 
                                "Пользователь успешно обновлен", 
                                Toast.LENGTH_SHORT).show();
                        clearForm();
                        loadBotUsers();
                    } else {
                        if (response.code() == 401) {
                            // Токен устарел, перенаправляем на экран входа
                            sessionManager.logout();
                            Toast.makeText(ManageBotActivity.this, 
                                    "Сессия истекла. Пожалуйста, войдите снова", 
                                    Toast.LENGTH_SHORT).show();
                            startActivity(new Intent(ManageBotActivity.this, com.example.apk.LoginActivity.class));
                            finish();
                        } else {
                            Toast.makeText(ManageBotActivity.this, 
                                    "Ошибка при обновлении пользователя: " + response.message(), 
                                    Toast.LENGTH_SHORT).show();
                        }
                    }
                }

                @Override
                public void onFailure(Call<BotUserResponse> call, Throwable t) {
                    showLoading(false);
                    setUiEnabled(true);
                    Toast.makeText(ManageBotActivity.this, 
                            "Ошибка соединения: " + t.getMessage(), 
                            Toast.LENGTH_SHORT).show();
                }
            });
        } catch (Exception e) {
            showLoading(false);
            setUiEnabled(true);
            Toast.makeText(ManageBotActivity.this, 
                    "Произошла ошибка: " + e.getMessage(), 
                    Toast.LENGTH_SHORT).show();
        }
    }

    private void deleteBotUser(long id) {
        try {
            showLoading(true);
            setUiEnabled(false);
            Call<Void> call = apiService.deleteBotUser(authToken, id);
            call.enqueue(new Callback<Void>() {
                @Override
                public void onResponse(Call<Void> call, Response<Void> response) {
                    showLoading(false);
                    setUiEnabled(true);
                    
                    if (response.isSuccessful()) {
                        Toast.makeText(ManageBotActivity.this, 
                                "Пользователь успешно удален", 
                                Toast.LENGTH_SHORT).show();
                        clearForm();
                        loadBotUsers();
                    } else {
                        if (response.code() == 401) {
                            // Токен устарел, перенаправляем на экран входа
                            sessionManager.logout();
                            Toast.makeText(ManageBotActivity.this, 
                                    "Сессия истекла. Пожалуйста, войдите снова", 
                                    Toast.LENGTH_SHORT).show();
                            startActivity(new Intent(ManageBotActivity.this, com.example.apk.LoginActivity.class));
                            finish();
                        } else {
                            Toast.makeText(ManageBotActivity.this, 
                                    "Ошибка при удалении пользователя: " + response.message(), 
                                    Toast.LENGTH_SHORT).show();
                        }
                    }
                }

                @Override
                public void onFailure(Call<Void> call, Throwable t) {
                    showLoading(false);
                    setUiEnabled(true);
                    Toast.makeText(ManageBotActivity.this, 
                            "Ошибка соединения: " + t.getMessage(), 
                            Toast.LENGTH_SHORT).show();
                }
            });
        } catch (Exception e) {
            showLoading(false);
            setUiEnabled(true);
            Toast.makeText(ManageBotActivity.this, 
                    "Произошла ошибка: " + e.getMessage(), 
                    Toast.LENGTH_SHORT).show();
        }
    }

    private boolean validateForm() {
        boolean isValid = true;
        
        if (telegramIdEditText.getText().toString().trim().isEmpty()) {
            telegramIdEditText.setError("Введите Telegram ID");
            isValid = false;
        } else {
            try {
                Long.parseLong(telegramIdEditText.getText().toString().trim());
            } catch (NumberFormatException e) {
                telegramIdEditText.setError("Telegram ID должен быть числом");
                isValid = false;
            }
        }
        
        if (roleSpinner.getSelectedItem() == null) {
            Toast.makeText(this, "Выберите роль", Toast.LENGTH_SHORT).show();
            isValid = false;
        }
        
        return isValid;
    }

    private void clearForm() {
        telegramIdEditText.setText("");
        roleSpinner.setSelection(0);
        selectedBotUserId = null;
        isEditing = false;
        addBotUserButton.setText("Добавить");
    }

    private void showLoading(boolean isLoading) {
        loadingProgressBar.setVisibility(isLoading ? View.VISIBLE : View.GONE);
        swipeRefreshLayout.setRefreshing(isLoading && swipeRefreshLayout.isRefreshing());
    }
    
    private void setUiEnabled(boolean enabled) {
        addBotUserButton.setEnabled(enabled);
        clearFormButton.setEnabled(enabled);
        telegramIdEditText.setEnabled(enabled);
        roleSpinner.setEnabled(enabled);
        backButton.setEnabled(enabled);
    }

    @Override
    public void onEditClick(BotUserResponse botUser) {
        // Заполняем форму данными выбранного пользователя
        selectedBotUserId = botUser.getId();
        telegramIdEditText.setText(String.valueOf(botUser.getTelegramId()));
        
        // Выбираем соответствующую роль в спиннере
        for (int i = 0; i < roles.size(); i++) {
            if (roles.get(i).equals(botUser.getRole())) {
                roleSpinner.setSelection(i);
                break;
            }
        }
        
        isEditing = true;
        addBotUserButton.setText("Обновить");
    }

    @Override
    public void onDeleteClick(BotUserResponse botUser) {
        // Показываем диалог подтверждения удаления
        new AlertDialog.Builder(this)
                .setTitle("Удаление пользователя бота")
                .setMessage("Вы действительно хотите удалить пользователя с Telegram ID " + botUser.getTelegramId() + "?")
                .setPositiveButton("Удалить", (dialog, which) -> deleteBotUser(botUser.getId()))
                .setNegativeButton("Отмена", null)
                .show();
    }
} 
 

import androidx.appcompat.app.AppCompatActivity;
import androidx.cardview.widget.CardView;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout;

import android.app.AlertDialog;
import android.content.DialogInterface;
import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ImageButton;
import android.widget.ProgressBar;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;

import com.example.apk.R;
import com.example.apk.adapters.BotUserAdapter;
import com.example.apk.api.ApiClient;
import com.example.apk.api.ApiService;
import com.example.apk.api.BotUserRequest;
import com.example.apk.api.BotUserResponse;
import com.example.apk.utils.SessionManager;

import java.util.ArrayList;
import java.util.List;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

public class ManageBotActivity extends AppCompatActivity implements BotUserAdapter.BotUserItemClickListener {

    private ImageButton backButton;
    private CardView addBotUserCard;
    private EditText telegramIdEditText;
    private Spinner roleSpinner;
    private Button addBotUserButton;
    private Button clearFormButton;
    private RecyclerView botUsersRecyclerView;
    private ProgressBar loadingProgressBar;
    private SwipeRefreshLayout swipeRefreshLayout;
    private TextView noDataTextView;

    private ApiService apiService;
    private String authToken;
    private SessionManager sessionManager;

    private BotUserAdapter botUserAdapter;
    private List<BotUserResponse> botUsers = new ArrayList<>();
    private List<String> roles = new ArrayList<>();
    private Long selectedBotUserId = null;
    private boolean isEditing = false;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_manage_bot);

        // Инициализируем API и токен авторизации
        apiService = ApiClient.getClient().create(ApiService.class);
        sessionManager = new SessionManager(this);
        authToken = "Bearer " + sessionManager.getToken();

        // Инициализируем компоненты UI
        initUI();

        // Настраиваем слушатели событий
        setupListeners();
    }
    
    @Override
    protected void onResume() {
        super.onResume();
        // Загружаем данные после того, как активность полностью восстановлена
        loadBotUsers();
    }

    private void initUI() {
        backButton = findViewById(R.id.backButton);
        addBotUserCard = findViewById(R.id.addBotUserCard);
        telegramIdEditText = findViewById(R.id.telegramIdEditText);
        roleSpinner = findViewById(R.id.roleSpinner);
        addBotUserButton = findViewById(R.id.addBotUserButton);
        clearFormButton = findViewById(R.id.clearFormButton);
        botUsersRecyclerView = findViewById(R.id.botUsersRecyclerView);
        loadingProgressBar = findViewById(R.id.loadingProgressBar);
        swipeRefreshLayout = findViewById(R.id.swipeRefreshLayout);
        noDataTextView = findViewById(R.id.noDataTextView);

        // Настраиваем RecyclerView
        botUserAdapter = new BotUserAdapter(this, botUsers);
        botUsersRecyclerView.setLayoutManager(new LinearLayoutManager(this));
        botUsersRecyclerView.setAdapter(botUserAdapter);

        // Заполняем спиннер ролями
        roles.add("Администратор");
        roles.add("Менеджер");
        roles.add("Сотрудник");
        
        ArrayAdapter<String> roleAdapter = new ArrayAdapter<>(
                this, android.R.layout.simple_spinner_item, roles);
        roleAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        roleSpinner.setAdapter(roleAdapter);
    }

    private void setupListeners() {
        // Кнопка назад
        backButton.setOnClickListener(v -> finish());

        // Обновление списка при свайпе вниз
        swipeRefreshLayout.setOnRefreshListener(this::loadBotUsers);

        // Кнопка добавления/обновления пользователя бота
        addBotUserButton.setOnClickListener(v -> {
            if (validateForm()) {
                if (isEditing && selectedBotUserId != null) {
                    updateBotUser();
                } else {
                    createBotUser();
                }
            }
        });

        // Кнопка очистки формы
        clearFormButton.setOnClickListener(v -> clearForm());
    }

    private void loadBotUsers() {
        try {
            showLoading(true);
            setUiEnabled(false);
            
            Call<List<BotUserResponse>> call = apiService.getBotUsers(authToken);
            call.enqueue(new Callback<List<BotUserResponse>>() {
                @Override
                public void onResponse(Call<List<BotUserResponse>> call, Response<List<BotUserResponse>> response) {
                    showLoading(false);
                    setUiEnabled(true);
                    
                    if (response.isSuccessful() && response.body() != null) {
                        botUsers.clear();
                        botUsers.addAll(response.body());
                        botUserAdapter.notifyDataSetChanged();
                        
                        // Отображаем сообщение, если нет данных
                        if (botUsers.isEmpty()) {
                            noDataTextView.setVisibility(View.VISIBLE);
                        } else {
                            noDataTextView.setVisibility(View.GONE);
                        }
                    } else {
                        if (response.code() == 401) {
                            // Токен устарел, перенаправляем на экран входа
                            sessionManager.logout();
                            Toast.makeText(ManageBotActivity.this, 
                                    "Сессия истекла. Пожалуйста, войдите снова", 
                                    Toast.LENGTH_SHORT).show();
                            startActivity(new Intent(ManageBotActivity.this, com.example.apk.LoginActivity.class));
                            finish();
                        } else {
                            Toast.makeText(ManageBotActivity.this, 
                                    "Ошибка при загрузке пользователей: " + response.message(), 
                                    Toast.LENGTH_SHORT).show();
                        }
                    }
                }

                @Override
                public void onFailure(Call<List<BotUserResponse>> call, Throwable t) {
                    showLoading(false);
                    setUiEnabled(true);
                    Toast.makeText(ManageBotActivity.this, 
                            "Ошибка соединения: " + t.getMessage(), 
                            Toast.LENGTH_SHORT).show();
                }
            });
        } catch (Exception e) {
            showLoading(false);
            setUiEnabled(true);
            Toast.makeText(ManageBotActivity.this, 
                    "Произошла ошибка: " + e.getMessage(), 
                    Toast.LENGTH_SHORT).show();
        }
    }

    private void createBotUser() {
        try {
            long telegramId = Long.parseLong(telegramIdEditText.getText().toString());
            String role = roleSpinner.getSelectedItem().toString();
            
            BotUserRequest request = new BotUserRequest(telegramId, role);
            
            showLoading(true);
            setUiEnabled(false);
            Call<BotUserResponse> call = apiService.createBotUser(authToken, request);
            call.enqueue(new Callback<BotUserResponse>() {
                @Override
                public void onResponse(Call<BotUserResponse> call, Response<BotUserResponse> response) {
                    showLoading(false);
                    setUiEnabled(true);
                    
                    if (response.isSuccessful()) {
                        Toast.makeText(ManageBotActivity.this, 
                                "Пользователь успешно добавлен", 
                                Toast.LENGTH_SHORT).show();
                        clearForm();
                        loadBotUsers();
                    } else {
                        if (response.code() == 401) {
                            // Токен устарел, перенаправляем на экран входа
                            sessionManager.logout();
                            Toast.makeText(ManageBotActivity.this, 
                                    "Сессия истекла. Пожалуйста, войдите снова", 
                                    Toast.LENGTH_SHORT).show();
                            startActivity(new Intent(ManageBotActivity.this, com.example.apk.LoginActivity.class));
                            finish();
                        } else {
                            Toast.makeText(ManageBotActivity.this, 
                                    "Ошибка при добавлении пользователя: " + response.message(), 
                                    Toast.LENGTH_SHORT).show();
                        }
                    }
                }

                @Override
                public void onFailure(Call<BotUserResponse> call, Throwable t) {
                    showLoading(false);
                    setUiEnabled(true);
                    Toast.makeText(ManageBotActivity.this, 
                            "Ошибка соединения: " + t.getMessage(), 
                            Toast.LENGTH_SHORT).show();
                }
            });
        } catch (Exception e) {
            showLoading(false);
            setUiEnabled(true);
            Toast.makeText(ManageBotActivity.this, 
                    "Произошла ошибка: " + e.getMessage(), 
                    Toast.LENGTH_SHORT).show();
        }
    }

    private void updateBotUser() {
        try {
            long telegramId = Long.parseLong(telegramIdEditText.getText().toString());
            String role = roleSpinner.getSelectedItem().toString();
            
            BotUserRequest request = new BotUserRequest(telegramId, role);
            
            showLoading(true);
            setUiEnabled(false);
            Call<BotUserResponse> call = apiService.updateBotUser(authToken, selectedBotUserId, request);
            call.enqueue(new Callback<BotUserResponse>() {
                @Override
                public void onResponse(Call<BotUserResponse> call, Response<BotUserResponse> response) {
                    showLoading(false);
                    setUiEnabled(true);
                    
                    if (response.isSuccessful()) {
                        Toast.makeText(ManageBotActivity.this, 
                                "Пользователь успешно обновлен", 
                                Toast.LENGTH_SHORT).show();
                        clearForm();
                        loadBotUsers();
                    } else {
                        if (response.code() == 401) {
                            // Токен устарел, перенаправляем на экран входа
                            sessionManager.logout();
                            Toast.makeText(ManageBotActivity.this, 
                                    "Сессия истекла. Пожалуйста, войдите снова", 
                                    Toast.LENGTH_SHORT).show();
                            startActivity(new Intent(ManageBotActivity.this, com.example.apk.LoginActivity.class));
                            finish();
                        } else {
                            Toast.makeText(ManageBotActivity.this, 
                                    "Ошибка при обновлении пользователя: " + response.message(), 
                                    Toast.LENGTH_SHORT).show();
                        }
                    }
                }

                @Override
                public void onFailure(Call<BotUserResponse> call, Throwable t) {
                    showLoading(false);
                    setUiEnabled(true);
                    Toast.makeText(ManageBotActivity.this, 
                            "Ошибка соединения: " + t.getMessage(), 
                            Toast.LENGTH_SHORT).show();
                }
            });
        } catch (Exception e) {
            showLoading(false);
            setUiEnabled(true);
            Toast.makeText(ManageBotActivity.this, 
                    "Произошла ошибка: " + e.getMessage(), 
                    Toast.LENGTH_SHORT).show();
        }
    }

    private void deleteBotUser(long id) {
        try {
            showLoading(true);
            setUiEnabled(false);
            Call<Void> call = apiService.deleteBotUser(authToken, id);
            call.enqueue(new Callback<Void>() {
                @Override
                public void onResponse(Call<Void> call, Response<Void> response) {
                    showLoading(false);
                    setUiEnabled(true);
                    
                    if (response.isSuccessful()) {
                        Toast.makeText(ManageBotActivity.this, 
                                "Пользователь успешно удален", 
                                Toast.LENGTH_SHORT).show();
                        clearForm();
                        loadBotUsers();
                    } else {
                        if (response.code() == 401) {
                            // Токен устарел, перенаправляем на экран входа
                            sessionManager.logout();
                            Toast.makeText(ManageBotActivity.this, 
                                    "Сессия истекла. Пожалуйста, войдите снова", 
                                    Toast.LENGTH_SHORT).show();
                            startActivity(new Intent(ManageBotActivity.this, com.example.apk.LoginActivity.class));
                            finish();
                        } else {
                            Toast.makeText(ManageBotActivity.this, 
                                    "Ошибка при удалении пользователя: " + response.message(), 
                                    Toast.LENGTH_SHORT).show();
                        }
                    }
                }

                @Override
                public void onFailure(Call<Void> call, Throwable t) {
                    showLoading(false);
                    setUiEnabled(true);
                    Toast.makeText(ManageBotActivity.this, 
                            "Ошибка соединения: " + t.getMessage(), 
                            Toast.LENGTH_SHORT).show();
                }
            });
        } catch (Exception e) {
            showLoading(false);
            setUiEnabled(true);
            Toast.makeText(ManageBotActivity.this, 
                    "Произошла ошибка: " + e.getMessage(), 
                    Toast.LENGTH_SHORT).show();
        }
    }

    private boolean validateForm() {
        boolean isValid = true;
        
        if (telegramIdEditText.getText().toString().trim().isEmpty()) {
            telegramIdEditText.setError("Введите Telegram ID");
            isValid = false;
        } else {
            try {
                Long.parseLong(telegramIdEditText.getText().toString().trim());
            } catch (NumberFormatException e) {
                telegramIdEditText.setError("Telegram ID должен быть числом");
                isValid = false;
            }
        }
        
        if (roleSpinner.getSelectedItem() == null) {
            Toast.makeText(this, "Выберите роль", Toast.LENGTH_SHORT).show();
            isValid = false;
        }
        
        return isValid;
    }

    private void clearForm() {
        telegramIdEditText.setText("");
        roleSpinner.setSelection(0);
        selectedBotUserId = null;
        isEditing = false;
        addBotUserButton.setText("Добавить");
    }

    private void showLoading(boolean isLoading) {
        loadingProgressBar.setVisibility(isLoading ? View.VISIBLE : View.GONE);
        swipeRefreshLayout.setRefreshing(isLoading && swipeRefreshLayout.isRefreshing());
    }
    
    private void setUiEnabled(boolean enabled) {
        addBotUserButton.setEnabled(enabled);
        clearFormButton.setEnabled(enabled);
        telegramIdEditText.setEnabled(enabled);
        roleSpinner.setEnabled(enabled);
        backButton.setEnabled(enabled);
    }

    @Override
    public void onEditClick(BotUserResponse botUser) {
        // Заполняем форму данными выбранного пользователя
        selectedBotUserId = botUser.getId();
        telegramIdEditText.setText(String.valueOf(botUser.getTelegramId()));
        
        // Выбираем соответствующую роль в спиннере
        for (int i = 0; i < roles.size(); i++) {
            if (roles.get(i).equals(botUser.getRole())) {
                roleSpinner.setSelection(i);
                break;
            }
        }
        
        isEditing = true;
        addBotUserButton.setText("Обновить");
    }

    @Override
    public void onDeleteClick(BotUserResponse botUser) {
        // Показываем диалог подтверждения удаления
        new AlertDialog.Builder(this)
                .setTitle("Удаление пользователя бота")
                .setMessage("Вы действительно хотите удалить пользователя с Telegram ID " + botUser.getTelegramId() + "?")
                .setPositiveButton("Удалить", (dialog, which) -> deleteBotUser(botUser.getId()))
                .setNegativeButton("Отмена", null)
                .show();
    }
} 