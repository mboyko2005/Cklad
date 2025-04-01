package com.example.apk.api;

import java.util.List;

import retrofit2.Call;
import retrofit2.http.Body;
import retrofit2.http.DELETE;
import retrofit2.http.GET;
import retrofit2.http.Header;
import retrofit2.http.POST;
import retrofit2.http.PUT;
import retrofit2.http.Path;

/**
 * Интерфейс для API запросов
 */
public interface ApiService {
    
    /**
     * Авторизация пользователя
     */
    @POST("api/auth/login")
    Call<AuthResponse> login(@Body AuthRequest authRequest);
    
    /**
     * Получение списка пользователей
     */
    @GET("api/manageusers")
    Call<List<UserResponse>> getUsers(@Header("Authorization") String token);
    
    /**
     * Получение списка ролей
     */
    @GET("api/manageusers/roles")
    Call<List<RoleResponse>> getRoles(@Header("Authorization") String token);
    
    /**
     * Создание нового пользователя
     */
    @POST("api/manageusers")
    Call<UserResponse> createUser(@Header("Authorization") String token, @Body UserRequest request);
    
    /**
     * Обновление пользователя
     */
    @PUT("api/manageusers/{userId}")
    Call<UserResponse> updateUser(@Header("Authorization") String token, @Path("userId") int userId, @Body UserRequest request);
    
    /**
     * Удаление пользователя
     */
    @DELETE("api/manageusers/{userId}")
    Call<Void> deleteUser(@Header("Authorization") String token, @Path("userId") int userId);
}