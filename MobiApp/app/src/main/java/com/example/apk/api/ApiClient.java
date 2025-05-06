package com.example.apk.api;

import java.io.IOException;
import java.util.concurrent.TimeUnit;

import okhttp3.Cache;
import okhttp3.CacheControl;
import okhttp3.Interceptor;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.Response;
import okhttp3.logging.HttpLoggingInterceptor;
import retrofit2.Retrofit;
import retrofit2.converter.gson.GsonConverterFactory;
import android.content.Context;
import android.net.ConnectivityManager;
import android.net.NetworkInfo;
import android.os.SystemClock;
import android.util.Log;

/**
 * Клиент для работы с API
 */
public class ApiClient {
    
    // Базовый URL API - совпадает с tunnelURL из WPF-приложения
    // Настраивается в App.xaml.cs в WPF проекте, константа Subdomain
    public static final String BASE_URL = "https://ckladtestt.loca.lt/";
    
    private static Retrofit retrofit = null;
    private static ApiService apiService = null;
    private static final int CACHE_SIZE = 20 * 1024 * 1024; // 20 МБ
    private static Context appContext;
    
    /**
     * Инициализация клиента с контекстом приложения
     */
    public static void init(Context context) {
        appContext = context.getApplicationContext();
    }
    
    /**
     * Проверяет наличие сетевого подключения
     * @return true, если есть подключение к сети
     */
    private static boolean isNetworkAvailable() {
        if (appContext == null) {
            return true; // По умолчанию считаем, что есть подключение
        }
        
        ConnectivityManager connectivityManager = (ConnectivityManager) 
                appContext.getSystemService(Context.CONNECTIVITY_SERVICE);
        
        if (connectivityManager != null) {
            NetworkInfo activeNetworkInfo = connectivityManager.getActiveNetworkInfo();
            return activeNetworkInfo != null && activeNetworkInfo.isConnected();
        }
        
        return false;
    }
    
    /**
     * Возвращает настроенный Retrofit-клиент
     */
    public static Retrofit getClient() {
        if (retrofit == null) {
            // Добавляем логирование запросов для отладки
            HttpLoggingInterceptor interceptor = new HttpLoggingInterceptor();
            interceptor.setLevel(HttpLoggingInterceptor.Level.BODY);
            
            // Создаем кеш для HTTP-запросов (увеличиваем до 50 МБ)
            Cache cache = null;
            if (appContext != null) {
                cache = new Cache(appContext.getCacheDir(), 50 * 1024 * 1024);
            }
            
            // Создаем специальный перехватчик для быстрой загрузки изображений
            FastImageInterceptor fastImageInterceptor = new FastImageInterceptor();
            
            OkHttpClient.Builder clientBuilder = new OkHttpClient.Builder()
                    .addInterceptor(interceptor)
                    .addInterceptor(new RetryInterceptor(3)) // Добавляем перехватчик для повторов
                    .addInterceptor(fastImageInterceptor) // Добавляем перехватчик для быстрых изображений
                    .addNetworkInterceptor(new CacheInterceptor()) // Перехватчик для кеширования
                    .connectTimeout(60, TimeUnit.SECONDS)    // Увеличиваем таймаут соединения
                    .readTimeout(60, TimeUnit.SECONDS)       // Увеличиваем таймаут чтения
                    .writeTimeout(60, TimeUnit.SECONDS);     // Добавляем таймаут записи
                    
            // Добавляем кеш, если он был создан
            if (cache != null) {
                clientBuilder.cache(cache);
            }
            
            OkHttpClient client = clientBuilder.build();
            
            retrofit = new Retrofit.Builder()
                    .baseUrl(BASE_URL)
                    .addConverterFactory(GsonConverterFactory.create())
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

    // Перехватчик для реализации механизма повторных запросов
    private static class RetryInterceptor implements Interceptor {
        private final int maxRetry;
        private final int retryDelayMillis = 2000; // Задержка между попытками (2 секунды)

        public RetryInterceptor(int maxRetry) {
            this.maxRetry = maxRetry;
        }

        @Override
        public Response intercept(Chain chain) throws IOException {
            Request request = chain.request();
            
            // Для запросов изображений устанавливаем приоритет
            String url = request.url().toString();
            if (url.contains("/api/Message/media/")) {
                request = request.newBuilder()
                    .addHeader("X-Priority", "high")
                    .build();
            }
            
            Response response = null;
            IOException exception = null;
            int tryCount = 0;

            while (tryCount < maxRetry && (response == null || !response.isSuccessful())) {
                // Закрываем предыдущий ответ, если он был
                if (response != null) {
                    response.close();
                }

                try {
                    response = chain.proceed(request);
                    // Если ответ успешный, выходим из цикла
                    if (response.isSuccessful()) {
                        return response;
                    }
                } catch (IOException e) {
                    Log.e("RetryInterceptor", "Request failed, try " + (tryCount + 1) + "/" + maxRetry, e);
                    exception = e; // Сохраняем последнее исключение
                    // Проверяем, можно ли повторить запрос (например, проблема с сетью)
                    // Не повторяем, если проблема в самом запросе (например, 4xx ошибки)
                }

                tryCount++;

                // Если попытки не исчерпаны, ждем перед следующей
                if (tryCount < maxRetry) {
                   Log.d("RetryInterceptor", "Retrying request in " + retryDelayMillis + "ms");
                   SystemClock.sleep(retryDelayMillis);
                }
            }

            // Если после всех попыток ответ все еще null, бросаем сохраненное исключение
            if (response == null && exception != null) {
                 Log.e("RetryInterceptor", "Request failed after " + maxRetry + " retries.");
                throw exception;
            }

            // Возвращаем последний ответ (даже если он неуспешный, после всех попыток)
            return response;
        }
    }
    
    // Перехватчик для работы с кешем
    private static class CacheInterceptor implements Interceptor {
        @Override
        public Response intercept(Chain chain) throws IOException {
            Request request = chain.request();
            
            // Для изображений и файлов используем агрессивное кеширование
            String url = request.url().toString();
            if (url.contains("/api/Message/media/")) {
                // Устанавливаем кеширование на длительный период (365 дней)
                int maxAge = 60 * 60 * 24 * 365; // 365 дней
                
                // Проверяем, есть ли сетевое подключение
                boolean isNetworkAvailable = ApiClient.isNetworkAvailable();
                
                CacheControl.Builder cacheBuilder = new CacheControl.Builder()
                    .maxAge(maxAge, TimeUnit.SECONDS);
                
                // В автономном режиме используем кеш, даже если он устарел
                if (!isNetworkAvailable) {
                    cacheBuilder.maxStale(60 * 60 * 24 * 365 * 2, TimeUnit.SECONDS); // 2 года
                }
                
                request = request.newBuilder()
                    .cacheControl(cacheBuilder.build())
                    .build();
                
                // Выполняем запрос
                Response response = chain.proceed(request);
                
                // Добавляем заголовки для кеширования в ответ
                return response.newBuilder()
                    .removeHeader("Pragma")
                    .removeHeader("Cache-Control")
                    .header("Cache-Control", "public, immutable, max-age=" + maxAge)
                    .build();
            }
            
            // Для обычных запросов оставляем без изменений
            return chain.proceed(request);
        }
    }
    
    // Специальный перехватчик для ускорения загрузки изображений
    private static class FastImageInterceptor implements Interceptor {
        @Override
        public Response intercept(Chain chain) throws IOException {
            Request request = chain.request();
            String url = request.url().toString();
            
            // Проверяем, является ли запрос запросом на изображение
            if (url.contains("/api/Message/media/")) {
                // Устанавливаем повышенный приоритет загрузки
                request = request.newBuilder()
                    .addHeader("X-Priority", "high")
                    .addHeader("X-Requested-With", "XMLHttpRequest") // Помогает некоторым серверам оптимизировать ответ
                    .addHeader("Accept", "image/*") // Указываем, что ожидаем изображение
                    .build();
                
                // Выполняем запрос
                Response response = chain.proceed(request);
                
                // Если это действительно изображение, добавляем специальные заголовки для кеширования
                String contentType = response.header("Content-Type", "");
                if (contentType.startsWith("image/")) {
                    return response.newBuilder()
                        .header("Cache-Control", "public, immutable, max-age=31536000") // 1 год
                        .removeHeader("Pragma")
                        .build();
                }
                
                return response;
            }
            
            return chain.proceed(request);
        }
    }
} 