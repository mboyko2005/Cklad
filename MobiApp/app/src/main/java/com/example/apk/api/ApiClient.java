package com.example.apk.api;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import com.google.gson.FieldNamingPolicy;
import java.util.concurrent.TimeUnit;

import okhttp3.OkHttpClient;
import okhttp3.logging.HttpLoggingInterceptor;
import retrofit2.Retrofit;
import retrofit2.converter.gson.GsonConverterFactory;

/**
 * Клиент для работы с API
 */
public class ApiClient {
    
    // Базовый URL API - совпадает с tunnelURL из WPF-приложения
    // Настраивается в App.xaml.cs в WPF проекте, константа Subdomain
    private static final String BASE_URL = "https://ckladtestt.loca.lt/";
    
    private static Retrofit retrofit = null;
    private static ApiService apiService = null;
    
    /**
     * Возвращает настроенный Retrofit-клиент
     */
    public static Retrofit getClient() {
        if (retrofit == null) {
            // Добавляем логирование запросов для отладки
            HttpLoggingInterceptor interceptor = new HttpLoggingInterceptor();
            interceptor.setLevel(HttpLoggingInterceptor.Level.BODY);
            
            OkHttpClient client = new OkHttpClient.Builder()
                    .addInterceptor(interceptor)
                    .connectTimeout(30, TimeUnit.SECONDS)
                    .readTimeout(30, TimeUnit.SECONDS)
                    .writeTimeout(30, TimeUnit.SECONDS)
                    .retryOnConnectionFailure(true)
                    .build();
            
            // Настраиваем Gson для работы с ASP.NET контроллером
            Gson gson = new GsonBuilder()
                    .setLenient() // Позволяет обрабатывать некорректный JSON
                    .serializeNulls() // Сериализует null-значения
                    .setFieldNamingPolicy(FieldNamingPolicy.UPPER_CAMEL_CASE) // Для работы с ASP.NET стилем именования
                    .create();
            
            retrofit = new Retrofit.Builder()
                    .baseUrl(BASE_URL)
                    .addConverterFactory(GsonConverterFactory.create(gson))
                    .client(client)
                    .build();
        }
        return retrofit;
    }
    
    /**
     * Возвращает сервис API
     */
    public static ApiService getApiService() {
        if (apiService == null) {
            apiService = getClient().create(ApiService.class);
        }
        return apiService;
    }
} 