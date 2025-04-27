package com.example.apk;

import android.animation.ArgbEvaluator;
import android.animation.ValueAnimator;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.view.View;
import android.view.animation.Animation;
import android.view.animation.AnimationUtils;
import android.view.inputmethod.InputMethodManager;
import android.widget.CheckBox;
import android.widget.ImageView;
import android.widget.TextView;
import android.widget.Toast;

import androidx.core.content.ContextCompat;

import com.example.apk.api.ApiClient;
import com.example.apk.api.AuthRequest;
import com.example.apk.api.AuthResponse;
import com.example.apk.models.User;
import com.example.apk.models.UserIdResponse;
import com.example.apk.utils.BaseActivity;
import com.example.apk.utils.SessionManager;
import com.google.android.material.button.MaterialButton;
import com.google.android.material.textfield.TextInputEditText;
import com.google.android.material.textfield.TextInputLayout;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

/**
 * Активность авторизации пользователя.
 * Позволяет пользователю ввести логин и пароль для входа в систему.
 */
public class LoginActivity extends BaseActivity {

    private TextInputEditText usernameEditText;
    private TextInputEditText passwordEditText;
    private CheckBox rememberMeCheckBox;
    private MaterialButton loginButton;
    private TextView errorTextView;
    private TextView subtitleTextView;
    private ImageView logoImageView;
    private ImageView logoGlow;
    private Animation shakeAnimation;
    private SessionManager sessionManager;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_login);

        // Инициализация UI компонентов
        usernameEditText = findViewById(R.id.usernameEditText);
        passwordEditText = findViewById(R.id.passwordEditText);
        rememberMeCheckBox = findViewById(R.id.rememberMeCheckBox);
        loginButton = findViewById(R.id.loginButton);
        errorTextView = findViewById(R.id.errorTextView);
        subtitleTextView = findViewById(R.id.subtitleTextView);
        logoImageView = findViewById(R.id.logoImageView);
        logoGlow = findViewById(R.id.logoGlow);
        
        // Загрузка анимации тряски для ошибки ввода
        shakeAnimation = AnimationUtils.loadAnimation(this, R.anim.shake_animation);
        
        // Инициализация менеджера сессий
        sessionManager = new SessionManager(this);

        // Настройка анимации градиента для подзаголовка
        setupSubtitleGradientAnimation();
        
        // Настройка анимации свечения для логотипа
        setupLogoGlowAnimation();

        // Проверяем сохраненные данные
        checkSavedCredentials();
        
        // Настройка скрытия клавиатуры при нажатии вне полей ввода
        setupHideKeyboardOnOutsideTouch();

        // Обработчик нажатия на кнопку входа
        loginButton.setOnClickListener(v -> {
            String username = usernameEditText.getText().toString().trim();
            String password = passwordEditText.getText().toString().trim();

            if (validateInput(username, password)) {
                loginUser(username, password);
            }
        });
    }

    /**
     * Настраивает анимацию градиента для подзаголовка
     */
    private void setupSubtitleGradientAnimation() {
        int colorFrom = ContextCompat.getColor(this, R.color.text_secondary);
        int colorTo = ContextCompat.getColor(this, R.color.primary);
        
        ValueAnimator colorAnimation = ValueAnimator.ofObject(new ArgbEvaluator(), colorFrom, colorTo, colorFrom);
        colorAnimation.setDuration(3000);
        colorAnimation.setRepeatCount(ValueAnimator.INFINITE);
        colorAnimation.addUpdateListener(animator -> 
            subtitleTextView.setTextColor((int) animator.getAnimatedValue())
        );
        colorAnimation.start();
    }
    
    /**
     * Настраивает анимацию свечения для логотипа
     */
    private void setupLogoGlowAnimation() {
        Animation pulseAnimation = AnimationUtils.loadAnimation(this, R.anim.pulse_animation);
        logoGlow.startAnimation(pulseAnimation);
    }

    /**
     * Проверяет сохраненные учетные данные и заполняет поля формы
     */
    private void checkSavedCredentials() {
        if (sessionManager.isLoggedIn()) {
            // Если пользователь уже вошел в систему, перенаправляем согласно его роли
            navigateToRoleScreen(sessionManager.getRole());
            return;
        }

        // Загружаем сохраненные учетные данные
        SharedPreferences prefs = getSharedPreferences("LoginPrefs", MODE_PRIVATE);
        if (prefs.getBoolean("rememberMe", false)) {
            String savedUsername = prefs.getString("username", "");
            String savedPassword = prefs.getString("password", "");
            boolean rememberMe = prefs.getBoolean("rememberMe", false);
            
            usernameEditText.setText(savedUsername);
            passwordEditText.setText(savedPassword);
            rememberMeCheckBox.setChecked(rememberMe);
        }
    }

    /**
     * Проверяет корректность ввода
     */
    private boolean validateInput(String username, String password) {
        if (username.isEmpty() || password.isEmpty()) {
            errorTextView.setText(getString(R.string.login_error));
            errorTextView.setVisibility(View.VISIBLE);
            errorTextView.startAnimation(shakeAnimation);
            return false;
        }
        return true;
    }

    /**
     * Авторизация пользователя через API
     */
    private void loginUser(String username, String password) {
        // Показываем индикатор загрузки
        loginButton.setEnabled(false);
        errorTextView.setVisibility(View.INVISIBLE);
        
        // Создаем объект запроса
        AuthRequest authRequest = new AuthRequest(username, password);
        
        // Выполняем запрос к API
        ApiClient.getApiService().login(authRequest).enqueue(new Callback<AuthResponse>() {
            @Override
            public void onResponse(Call<AuthResponse> call, Response<AuthResponse> response) {
                loginButton.setEnabled(true);
                
                if (response.isSuccessful() && response.body() != null && response.body().isSuccess()) {
                    // Авторизация успешна
                    AuthResponse authResponse = response.body();
                    
                    // Сохраняем данные сессии
                    User user = new User(username, authResponse.getRole(), authResponse.getToken());
                    sessionManager.createLoginSession(user);

                    // Получаем и сохраняем ID пользователя
                    fetchAndStoreUserId(username);
                    
                    // Сохраняем учетные данные, если выбрано "Запомнить меня"
                    if (rememberMeCheckBox.isChecked()) {
                        SharedPreferences.Editor editor = getSharedPreferences("LoginPrefs", MODE_PRIVATE).edit();
                        editor.putString("username", username);
                        editor.putString("password", password);
                        editor.putBoolean("rememberMe", true);
                        editor.apply();
                    } else {
                        // Очищаем сохраненные данные, если флажок не установлен
                        SharedPreferences.Editor editor = getSharedPreferences("LoginPrefs", MODE_PRIVATE).edit();
                        editor.clear();
                        editor.apply();
                    }
                    
                    // Отображаем сообщение об успешной авторизации
                    Toast.makeText(LoginActivity.this, R.string.login_success, Toast.LENGTH_SHORT).show();
                    
                    // Перенаправляем пользователя согласно его роли
                    navigateToRoleScreen(authResponse.getRole());
                    
                } else {
                    // Ошибка авторизации - неверные учетные данные
                    errorTextView.setText(R.string.login_error);
                    errorTextView.setVisibility(View.VISIBLE);
                    errorTextView.startAnimation(shakeAnimation);
                }
            }

            @Override
            public void onFailure(Call<AuthResponse> call, Throwable t) {
                loginButton.setEnabled(true);
                // Ошибка соединения с сервером
                errorTextView.setText(R.string.connection_error);
                errorTextView.setVisibility(View.VISIBLE);
                errorTextView.startAnimation(shakeAnimation);
            }
        });
    }

    /**
     * Получает и сохраняет ID пользователя по его логину.
     */
    private void fetchAndStoreUserId(String username) {
        // Выполняем запрос к API для получения ID
        // Используем ApiClient, который уже настроен
        ApiClient.getApiService().getUserIdByLogin(username).enqueue(new Callback<UserIdResponse>() {
            @Override
            public void onResponse(Call<UserIdResponse> call, Response<UserIdResponse> response) {
                if (response.isSuccessful() && response.body() != null && response.body().isSuccess()) {
                    int userId = response.body().getUserId();
                    
                    // Сохраняем userId в SharedPreferences (можно использовать отдельный файл или SessionManager)
                    SharedPreferences prefs = getSharedPreferences("UserPrefs", MODE_PRIVATE);
                    SharedPreferences.Editor editor = prefs.edit();
                    editor.putInt("userId", userId);
                    editor.apply();
                    
                    // Показываем уведомление об успешном сохранении ID
                    Toast.makeText(LoginActivity.this, "ID пользователя " + userId + " сохранен", Toast.LENGTH_SHORT).show();
                    
                } else {
                    // Ошибка получения ID от сервера
                    String errorMessage = "Не удалось получить ID пользователя.";
                    if (response != null && response.body() != null && response.body().getMessage() != null) {
                        errorMessage += " Причина: " + response.body().getMessage();
                    } else if(response != null) {
                        errorMessage += " Код ошибки: " + response.code();
                    }
                    Toast.makeText(LoginActivity.this, errorMessage, Toast.LENGTH_LONG).show();
                }
            }

            @Override
            public void onFailure(Call<UserIdResponse> call, Throwable t) {
                // Ошибка сети или другая проблема при выполнении запроса
                Toast.makeText(LoginActivity.this, "Ошибка сети при получении ID: " + t.getMessage(), Toast.LENGTH_LONG).show();
            }
        });
    }
    
    /**
     * Перенаправляет пользователя на экран, соответствующий его роли
     */
    private void navigateToRoleScreen(String role) {
        Intent intent;
        
        switch (role) {
            case "Администратор":
                intent = new Intent(LoginActivity.this, AdminActivity.class);
                break;
            case "Менеджер":
                intent = new Intent(LoginActivity.this, ManagerActivity.class);
                break;
            case "Сотрудник склада":
                intent = new Intent(LoginActivity.this, StaffActivity.class);
                break;
            default:
                // Если роль неизвестна, остаемся на экране входа
                Toast.makeText(this, "Неизвестная роль: " + role, Toast.LENGTH_LONG).show();
                return;
        }
        
        startActivity(intent);
        finish();
    }

    /**
     * Настройка скрытия клавиатуры при нажатии вне полей ввода
     */
    private void setupHideKeyboardOnOutsideTouch() {
        // Получаем корневой вид активности
        View rootView = findViewById(android.R.id.content);
        
        // Устанавливаем слушатель касаний для корневого вида
        rootView.setOnTouchListener((v, event) -> {
            // Если текущий фокус - поле ввода и произошло касание экрана
            if (getCurrentFocus() != null) {
                // Получаем координаты касания
                float x = event.getX();
                float y = event.getY();
                
                // Получаем координаты текущего поля ввода с фокусом
                int[] location = new int[2];
                getCurrentFocus().getLocationOnScreen(location);
                float focusLeft = location[0];
                float focusTop = location[1];
                float focusRight = focusLeft + getCurrentFocus().getWidth();
                float focusBottom = focusTop + getCurrentFocus().getHeight();
                
                // Если касание произошло вне поля ввода, скрываем клавиатуру
                if (!(x > focusLeft && x < focusRight && y > focusTop && y < focusBottom)) {
                    hideKeyboard();
                    getCurrentFocus().clearFocus();
                    return true;
                }
            }
            return false;
        });
    }

    /**
     * Скрытие клавиатуры
     */
    private void hideKeyboard() {
        View view = this.getCurrentFocus();
        if (view != null) {
            InputMethodManager imm = (InputMethodManager) getSystemService(Context.INPUT_METHOD_SERVICE);
            imm.hideSoftInputFromWindow(view.getWindowToken(), 0);
        }
    }
} 