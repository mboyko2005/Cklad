package com.example.apk.admin;

import android.app.AlertDialog;
import android.os.Bundle;
import android.text.Editable;
import android.text.TextWatcher;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ImageButton;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;

import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.example.apk.R;
import com.example.apk.api.ApiClient;
import com.example.apk.api.ApiService;
import com.example.apk.api.RoleResponse;
import com.example.apk.api.UserRequest;
import com.example.apk.api.UserResponse;
import com.example.apk.models.Role;
import com.example.apk.models.UserData;
import com.example.apk.utils.BaseActivity;
import com.example.apk.utils.SessionManager;

import java.util.ArrayList;
import java.util.List;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

/**
 * Активность для управления пользователями (аналог ManageUsers в веб-версии)
 */
public class ManageUsersActivity extends BaseActivity {
    private static final String TAG = "ManageUsersActivity";

    private ApiService apiService;
    private SessionManager sessionManager;
    private UserAdapter userAdapter;
    private List<Role> roles = new ArrayList<>();
    
    private RecyclerView usersRecyclerView;
    private EditText searchInput;
    private com.google.android.material.floatingactionbutton.FloatingActionButton addUserButton;
    private com.google.android.material.floatingactionbutton.FloatingActionButton editUserButton;
    private com.google.android.material.floatingactionbutton.FloatingActionButton deleteUserButton;
    
    private UserData selectedUser = null;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_manage_users);
        
        // Инициализация API и менеджера сессий
        apiService = ApiClient.getClient().create(ApiService.class);
        sessionManager = new SessionManager(this);
        
        // Проверка авторизации
        if (!sessionManager.isLoggedIn() || !sessionManager.getRole().equals("Администратор")) {
            Toast.makeText(this, "Доступ запрещен", Toast.LENGTH_SHORT).show();
            finish();
            return;
        }
        
        // Инициализация UI
        initViews();
        
        // Загрузка данных
        loadRoles();
        loadUsers();
    }
    
    private void initViews() {
        // Настройка кнопки "Назад"
        ImageButton backButton = findViewById(R.id.backButton);
        backButton.setOnClickListener(v -> finish());
        
        // Настройка RecyclerView
        usersRecyclerView = findViewById(R.id.usersRecyclerView);
        usersRecyclerView.setLayoutManager(new LinearLayoutManager(this));
        setupUsersAdapter();
        
        // Поиск
        searchInput = findViewById(R.id.searchInput);
        searchInput.addTextChangedListener(new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence s, int start, int count, int after) {}

            @Override
            public void onTextChanged(CharSequence s, int start, int before, int count) {}

            @Override
            public void afterTextChanged(Editable s) {
                userAdapter.filter(s.toString());
            }
        });
        
        // Кнопки действий
        addUserButton = findViewById(R.id.addUserButton);
        editUserButton = findViewById(R.id.editUserButton);
        deleteUserButton = findViewById(R.id.deleteUserButton);
        
        addUserButton.setOnClickListener(v -> showUserDialog(false));
        editUserButton.setOnClickListener(v -> {
            if (selectedUser != null) {
                showUserDialog(true);
            } else {
                Toast.makeText(this, "Сначала выберите пользователя", Toast.LENGTH_SHORT).show();
            }
        });
        deleteUserButton.setOnClickListener(v -> {
            if (selectedUser != null) {
                showDeleteConfirmDialog();
            } else {
                Toast.makeText(this, "Сначала выберите пользователя", Toast.LENGTH_SHORT).show();
            }
        });
        
        // Изначально кнопки редактирования и удаления неактивны
        editUserButton.setEnabled(false);
        deleteUserButton.setEnabled(false);
    }
    
    /**
     * Загрузка списка ролей
     */
    private void loadRoles() {
        String token = "Bearer " + sessionManager.getToken();
        Log.d(TAG, "Загрузка ролей, токен: " + token);
        
        apiService.getRoles(token).enqueue(new Callback<List<RoleResponse>>() {
            @Override
            public void onResponse(Call<List<RoleResponse>> call, Response<List<RoleResponse>> response) {
                if (response.isSuccessful() && response.body() != null) {
                    roles.clear();
                    for (RoleResponse roleResponse : response.body()) {
                        roles.add(new Role(roleResponse.getId(), roleResponse.getName()));
                    }
                    Log.d(TAG, "Загружено ролей: " + roles.size());
                } else {
                    String errorMessage = "Ошибка загрузки ролей: " + 
                        (response.code() == 401 ? "Необходима авторизация" : 
                         response.code() == 403 ? "Доступ запрещен" : 
                         "Код ответа: " + response.code());
                    Log.e(TAG, errorMessage);
                    Toast.makeText(ManageUsersActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<List<RoleResponse>> call, Throwable t) {
                String errorMessage = "Ошибка сети: " + t.getMessage();
                Log.e(TAG, errorMessage, t);
                Toast.makeText(ManageUsersActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
            }
        });
    }
    
    /**
     * Загрузка списка пользователей
     */
    private void loadUsers() {
        String token = "Bearer " + sessionManager.getToken();
        Log.d(TAG, "Загрузка пользователей, токен: " + token);
        
        apiService.getUsers(token).enqueue(new Callback<List<UserResponse>>() {
            @Override
            public void onResponse(Call<List<UserResponse>> call, Response<List<UserResponse>> response) {
                if (response.isSuccessful() && response.body() != null) {
                    List<UserData> users = new ArrayList<>();
                    
                    Log.d(TAG, "Получен ответ от сервера, пользователей: " + response.body().size());
                    
                    // Логируем каждый объект для отладки
                    for (UserResponse userResponse : response.body()) {
                        Log.d(TAG, String.format("Пользователь из API: ID=%d, Username=%s, RoleID=%d, RoleName=%s", 
                            userResponse.getUserId(), userResponse.getUsername(), 
                            userResponse.getRoleId(), userResponse.getRoleName()));
                        
                        if (userResponse.getUserId() <= 0) {
                            Log.e(TAG, "ВНИМАНИЕ: Обнаружен пользователь с некорректным ID: " + 
                                userResponse.getUserId() + ", имя: " + userResponse.getUsername());
                        }
                        
                        UserData userData = new UserData(
                            userResponse.getUserId(),
                            userResponse.getUsername(),
                            userResponse.getRoleName(),
                            userResponse.getRoleId()
                        );
                        users.add(userData);
                    }
                    
                    // Проверяем, что данные преобразованы корректно
                    for (UserData user : users) {
                        Log.d(TAG, String.format("UserData после преобразования: ID=%d, Username=%s, RoleID=%d", 
                            user.getUserId(), user.getUsername(), user.getRoleId()));
                    }
                    
                    userAdapter.updateUsers(users);
                    
                    // Сбрасываем выбор
                    selectedUser = null;
                    editUserButton.setEnabled(false);
                    deleteUserButton.setEnabled(false);
                    
                    Log.d(TAG, "Загружено пользователей: " + users.size());
                } else {
                    String errorMessage = "Ошибка загрузки пользователей: " + 
                        (response.code() == 401 ? "Необходима авторизация" : 
                         response.code() == 403 ? "Доступ запрещен" : 
                         "Код ответа: " + response.code());
                    Log.e(TAG, errorMessage);
                    
                    // Дополнительное логирование ошибки
                    try {
                        if (response.errorBody() != null) {
                            String errorBody = response.errorBody().string();
                            Log.e(TAG, "Тело ошибки: " + errorBody);
                        }
                    } catch (Exception e) {
                        Log.e(TAG, "Не удалось прочитать тело ошибки", e);
                    }
                    
                    Toast.makeText(ManageUsersActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<List<UserResponse>> call, Throwable t) {
                String errorMessage = "Ошибка сети: " + t.getMessage();
                Log.e(TAG, errorMessage, t);
                Toast.makeText(ManageUsersActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
            }
        });
    }
    
    /**
     * Отображение диалога добавления/редактирования пользователя
     */
    private void showUserDialog(boolean isEdit) {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        View dialogView = LayoutInflater.from(this).inflate(R.layout.dialog_user_form, null);
        builder.setView(dialogView);
        
        // Находим элементы
        TextView dialogTitle = dialogView.findViewById(R.id.dialogTitle);
        EditText usernameInput = dialogView.findViewById(R.id.usernameInput);
        EditText passwordInput = dialogView.findViewById(R.id.passwordInput);
        EditText confirmPasswordInput = dialogView.findViewById(R.id.confirmPasswordInput);
        Spinner roleSpinner = dialogView.findViewById(R.id.roleSpinner);
        Button cancelButton = dialogView.findViewById(R.id.cancelButton);
        Button saveButton = dialogView.findViewById(R.id.saveButton);
        
        // Устанавливаем заголовок
        dialogTitle.setText(isEdit ? "Редактировать пользователя" : "Добавить пользователя");
        
        // Настройка спиннера с ролями
        ArrayAdapter<Role> roleAdapter = new ArrayAdapter<>(this, 
                android.R.layout.simple_spinner_dropdown_item, roles);
        roleSpinner.setAdapter(roleAdapter);
        
        // Если редактирование, заполняем поля
        if (isEdit && selectedUser != null) {
            usernameInput.setText(selectedUser.getUsername());
            
            // Находим индекс выбранной роли
            for (int i = 0; i < roles.size(); i++) {
                if (roles.get(i).getId() == selectedUser.getRoleId()) {
                    roleSpinner.setSelection(i);
                    break;
                }
            }
            
            // ВНИМАНИЕ: Контроллер требует пароль даже при редактировании
            passwordInput.setHint("Введите пароль (обязательно)");
            confirmPasswordInput.setHint("Подтвердите пароль");
        }
        
        AlertDialog dialog = builder.create();
        
        // Обработчики кнопок
        cancelButton.setOnClickListener(v -> dialog.dismiss());
        
        saveButton.setOnClickListener(v -> {
            String username = usernameInput.getText().toString().trim();
            String password = passwordInput.getText().toString().trim();
            String confirmPassword = confirmPasswordInput.getText().toString().trim();
            
            if (username.isEmpty()) {
                Toast.makeText(this, "Введите имя пользователя", Toast.LENGTH_SHORT).show();
                return;
            }
            
            // Проверка паролей (всегда обязательны, даже при редактировании)
            if (password.isEmpty()) {
                Toast.makeText(this, "Введите пароль", Toast.LENGTH_SHORT).show();
                return;
            }
            
            if (!password.equals(confirmPassword)) {
                Toast.makeText(this, "Пароли не совпадают", Toast.LENGTH_SHORT).show();
                return;
            }
            
            // Проверка на выбор роли
            if (roleSpinner.getSelectedItem() == null) {
                Toast.makeText(this, "Выберите роль", Toast.LENGTH_SHORT).show();
                return;
            }
            
            // Получаем выбранную роль
            Role selectedRole = (Role) roleSpinner.getSelectedItem();
            
            if (isEdit) {
                // Проверяем, что ID пользователя корректный
                if (selectedUser == null || selectedUser.getUserId() <= 0) {
                    Toast.makeText(this, "Ошибка: некорректный ID пользователя", Toast.LENGTH_SHORT).show();
                    loadUsers(); // Перезагружаем список для обновления ID
                    return;
                }
                updateUser(selectedUser.getUserId(), username, password, selectedRole.getId());
            } else {
                createUser(username, password, selectedRole.getId());
            }
            
            dialog.dismiss();
        });
        
        dialog.show();
    }
    
    /**
     * Создание нового пользователя
     */
    private void createUser(String username, String password, int roleId) {
        String token = "Bearer " + sessionManager.getToken();
        UserRequest request = new UserRequest(username, password, roleId);
        
        Log.d(TAG, "Создание пользователя: " + username + ", roleId: " + roleId);
        
        apiService.createUser(token, request).enqueue(new Callback<UserResponse>() {
            @Override
            public void onResponse(Call<UserResponse> call, Response<UserResponse> response) {
                if (response.isSuccessful()) {
                    Toast.makeText(ManageUsersActivity.this, 
                            "Пользователь успешно создан", Toast.LENGTH_SHORT).show();
                    Log.d(TAG, "Пользователь успешно создан");
                    loadUsers(); // Перезагружаем список
                } else {
                    String errorMessage = "Ошибка создания пользователя: " + 
                        (response.code() == 401 ? "Необходима авторизация" : 
                         response.code() == 403 ? "Доступ запрещен" : 
                         response.code() == 409 ? "Пользователь с таким именем уже существует" :
                         "Код ответа: " + response.code());
                    Log.e(TAG, errorMessage);
                    Toast.makeText(ManageUsersActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<UserResponse> call, Throwable t) {
                String errorMessage = "Ошибка сети: " + t.getMessage();
                Log.e(TAG, errorMessage, t);
                Toast.makeText(ManageUsersActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
            }
        });
    }
    
    /**
     * Обновление пользователя
     */
    private void updateUser(int userId, String username, String password, int roleId) {
        // Дополнительная проверка ID перед отправкой запроса
        if (userId <= 0) {
            Toast.makeText(this, "Ошибка: некорректный ID пользователя", Toast.LENGTH_SHORT).show();
            Log.e(TAG, "Попытка обновить пользователя с некорректным ID: " + userId);
            return;
        }
        
        String token = "Bearer " + sessionManager.getToken();
        UserRequest request = new UserRequest(username, password, roleId);
        
        Log.d(TAG, "Обновление пользователя id: " + userId + ", username: " + username + ", roleId: " + roleId);
        
        apiService.updateUser(token, userId, request).enqueue(new Callback<UserResponse>() {
            @Override
            public void onResponse(Call<UserResponse> call, Response<UserResponse> response) {
                if (response.isSuccessful()) {
                    Toast.makeText(ManageUsersActivity.this, 
                            "Пользователь успешно обновлен", Toast.LENGTH_SHORT).show();
                    Log.d(TAG, "Пользователь успешно обновлен");
                    loadUsers(); // Перезагружаем список
                } else {
                    String errorMessage = "Ошибка обновления пользователя: " + 
                        (response.code() == 400 ? "Некорректные данные запроса" :
                         response.code() == 401 ? "Необходима авторизация" : 
                         response.code() == 403 ? "Доступ запрещен" : 
                         response.code() == 404 ? "Пользователь не найден" :
                         response.code() == 409 ? "Пользователь с таким именем уже существует" :
                         "Код ответа: " + response.code());
                    Log.e(TAG, errorMessage);
                    
                    // Более подробное логирование
                    try {
                        if (response.errorBody() != null) {
                            String errorBody = response.errorBody().string();
                            Log.e(TAG, "Тело ошибки: " + errorBody);
                        }
                    } catch (Exception e) {
                        Log.e(TAG, "Не удалось прочитать тело ошибки", e);
                    }
                    
                    Toast.makeText(ManageUsersActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<UserResponse> call, Throwable t) {
                String errorMessage = "Ошибка сети: " + t.getMessage();
                Log.e(TAG, errorMessage, t);
                Toast.makeText(ManageUsersActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
            }
        });
    }
    
    /**
     * Показывает диалог подтверждения удаления
     */
    private void showDeleteConfirmDialog() {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        View dialogView = LayoutInflater.from(this).inflate(R.layout.dialog_confirm, null);
        builder.setView(dialogView);
        
        TextView confirmTitle = dialogView.findViewById(R.id.confirmTitle);
        TextView confirmMessage = dialogView.findViewById(R.id.confirmMessage);
        Button noButton = dialogView.findViewById(R.id.noButton);
        Button yesButton = dialogView.findViewById(R.id.yesButton);
        
        confirmTitle.setText("Удаление пользователя");
        confirmMessage.setText("Вы действительно хотите удалить пользователя " + 
                selectedUser.getUsername() + "?");
        
        AlertDialog dialog = builder.create();
        
        noButton.setOnClickListener(v -> dialog.dismiss());
        
        yesButton.setOnClickListener(v -> {
            deleteUser(selectedUser.getUserId());
            dialog.dismiss();
        });
        
        dialog.show();
    }
    
    /**
     * Удаление пользователя
     */
    private void deleteUser(int userId) {
        String token = "Bearer " + sessionManager.getToken();
        Log.d(TAG, "Удаление пользователя id: " + userId);
        
        apiService.deleteUser(token, userId).enqueue(new Callback<Void>() {
            @Override
            public void onResponse(Call<Void> call, Response<Void> response) {
                if (response.isSuccessful()) {
                    Toast.makeText(ManageUsersActivity.this, 
                            "Пользователь успешно удален", Toast.LENGTH_SHORT).show();
                    Log.d(TAG, "Пользователь успешно удален");
                    loadUsers(); // Перезагружаем список
                } else {
                    String errorMessage = "Ошибка удаления пользователя: " + 
                        (response.code() == 401 ? "Необходима авторизация" : 
                         response.code() == 403 ? "Доступ запрещен" : 
                         response.code() == 404 ? "Пользователь не найден" :
                         "Код ответа: " + response.code());
                    Log.e(TAG, errorMessage);
                    Toast.makeText(ManageUsersActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<Void> call, Throwable t) {
                String errorMessage = "Ошибка сети: " + t.getMessage();
                Log.e(TAG, errorMessage, t);
                Toast.makeText(ManageUsersActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
            }
        });
    }

    // Настройка адаптера пользователей
    private void setupUsersAdapter() {
        userAdapter = new UserAdapter();
        usersRecyclerView.setAdapter(userAdapter);
        
        // Настройка выбора пользователя
        userAdapter.setOnUserClickListener(new UserAdapter.OnUserClickListener() {
            @Override
            public void onUserClick(UserData user, int position) {
                // Сохраняем выбранного пользователя
                selectedUser = user;
                userAdapter.setSelectedUser(user); // Обновляем выбранного пользователя в адаптере
                editUserButton.setEnabled(true);
                deleteUserButton.setEnabled(true);
            }
            
            @Override
            public void onUserEditClick(UserData user) {
                // Выбираем пользователя
                selectedUser = user;
                userAdapter.setSelectedUser(user);
                editUserButton.setEnabled(true);
                deleteUserButton.setEnabled(true);
                
                // Вызываем диалог редактирования
                showUserDialog(true);
            }
            
            @Override
            public void onUserDeleteClick(UserData user) {
                // Выбираем пользователя
                selectedUser = user;
                userAdapter.setSelectedUser(user);
                editUserButton.setEnabled(true);
                deleteUserButton.setEnabled(true);
                
                // Вызываем диалог подтверждения удаления
                showDeleteConfirmDialog();
            }
        });
    }
} 