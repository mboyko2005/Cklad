package com.example.apk.api;

import retrofit2.Call;
import retrofit2.http.Body;
import retrofit2.http.POST;

/**
 * Интерфейс для API запросов
 */
public interface ApiService {
    
    /**
     * Авторизация пользователя
     */
    @POST("api/auth/login")
    Call<AuthResponse> login(@Body AuthRequest authRequest);
    
} 