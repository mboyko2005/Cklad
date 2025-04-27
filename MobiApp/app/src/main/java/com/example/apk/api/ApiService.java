package com.example.apk.api;

import java.util.List;

import okhttp3.ResponseBody;
import retrofit2.Call;
import retrofit2.http.Body;
import retrofit2.http.DELETE;
import retrofit2.http.GET;
import retrofit2.http.Header;
import retrofit2.http.POST;
import retrofit2.http.PUT;
import retrofit2.http.Path;
import retrofit2.http.Query;
import retrofit2.http.Url;

import com.example.apk.models.UserIdResponse;
import com.example.apk.api.MessageResponse;
import com.example.apk.api.MessagesResponse;

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
    
    /**
     * Получение списка всех складских позиций
     */
    @GET("api/manageinventory")
    Call<List<InventoryResponse>> getInventory(@Header("Authorization") String token);
    
    /**
     * Получение списка складов
     */
    @GET("api/manageinventory/warehouses")
    Call<List<WarehouseResponse>> getWarehouses(@Header("Authorization") String token);
    
    /**
     * Создание новой складской позиции
     */
    @POST("api/manageinventory")
    Call<InventoryResponse> createInventoryItem(@Header("Authorization") String token, @Body InventoryRequest request);
    
    /**
     * Обновление складской позиции
     */
    @PUT("api/manageinventory/{id}")
    Call<InventoryResponse> updateInventoryItem(@Header("Authorization") String token, @Path("id") int positionId, @Body InventoryRequest request);
    
    /**
     * Удаление складской позиции
     */
    @DELETE("api/manageinventory/{id}")
    Call<Void> deleteInventoryItem(@Header("Authorization") String token, @Path("id") int positionId);
    
    /**
     * Получение отчета по самым продаваемым товарам
     */
    @GET("api/reports/mostSoldProducts")
    Call<ReportResponse> getMostSoldProducts(@Header("Authorization") String token);
    
    /**
     * Получение отчета по пользователям системы
     */
    @GET("api/reports/systemUsers")
    Call<ReportResponse> getSystemUsers(@Header("Authorization") String token);
    
    /**
     * Получение отчета по общей стоимости товаров
     */
    @GET("api/reports/totalCost")
    Call<ReportResponse> getTotalCost(@Header("Authorization") String token);
    
    /**
     * Получение отчета по текущим складским позициям
     */
    @GET("api/reports/currentStock")
    Call<ReportResponse> getCurrentStock(@Header("Authorization") String token);
    
    /**
     * Получение пользователей бота Telegram
     */
    @GET("api/managebot")
    Call<List<BotUserResponse>> getBotUsers(@Header("Authorization") String token);
    
    /**
     * Создание нового пользователя бота
     */
    @POST("api/managebot")
    Call<BotUserResponse> createBotUser(@Header("Authorization") String token, @Body BotUserRequest request);
    
    /**
     * Обновление пользователя бота
     */
    @PUT("api/managebot/{id}")
    Call<BotUserResponse> updateBotUser(@Header("Authorization") String token, @Path("id") long id, @Body BotUserRequest request);
    
    /**
     * Удаление пользователя бота
     */
    @DELETE("api/managebot/{id}")
    Call<Void> deleteBotUser(@Header("Authorization") String token, @Path("id") long id);
    
    /**
     * Изменение пароля пользователя
     */
    @POST("api/settings/changepassword")
    Call<SettingsResponse> changePassword(@Header("Authorization") String token, @Body ChangePasswordRequest request);
    
    /**
     * Сохранение темы пользователя
     */
    @POST("api/settings/theme")
    Call<SettingsResponse> saveTheme(@Header("Authorization") String token, @Body ThemeRequest request);

    /**
     * Получение ID пользователя по логину
     */
    @GET("api/User/getUserIdByLogin/{login}")
    Call<UserIdResponse> getUserIdByLogin(@Path("login") String login);

    /**
     * Получение всех сообщений пользователя
     */
    @GET("api/Message/messages/{userId}")
    Call<MessagesResponse> getMessages(@Path("userId") int userId);

    /**
     * Получение переписки между двумя пользователями
     */
    @GET("api/Message/conversation/{userId}/{contactId}")
    Call<MessagesResponse> getConversation(
        @Path("userId") int userId,
        @Path("contactId") int contactId
    );

    /**
     * Отправить новое сообщение
     */
    @POST("api/Message/send")
    Call<SendMessageResponse> sendMessage(@Body SendMessageRequest request);

    /**
     * Скачивает файл по указанному URL
     * @param fileUrl URL файла для скачивания
     * @return ResponseBody содержащий тело ответа с файлом
     */
    @GET
    Call<ResponseBody> downloadFile(@Url String fileUrl);
}