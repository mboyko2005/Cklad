package com.example.apk;

import android.app.Application;
import android.content.Context;
import android.content.SharedPreferences;

import androidx.appcompat.app.AppCompatDelegate;

import com.example.apk.api.ApiClient;
import com.bumptech.glide.Glide;
import com.bumptech.glide.GlideBuilder;
import com.bumptech.glide.load.DecodeFormat;
import com.bumptech.glide.load.engine.DiskCacheStrategy;
import com.bumptech.glide.load.engine.cache.InternalCacheDiskCacheFactory;
import com.bumptech.glide.request.RequestOptions;

/**
 * Основной класс приложения
 */
public class WarehouseApplication extends Application {

    // Ключ для хранения настройки темы
    public static final String THEME_PREF_KEY = "app_theme";
    
    // Возможные значения темы
    public static final String THEME_LIGHT = "Светлая";
    public static final String THEME_DARK = "Темная";
    public static final String THEME_SYSTEM = "Системная";
    
    // Размер кеша для изображений Glide
    private static final int GLIDE_DISK_CACHE_SIZE = 30 * 1024 * 1024; // 30MB
    
    @Override
    public void onCreate() {
        super.onCreate();
        
        // Инициализация ApiClient с контекстом приложения
        ApiClient.init(this);
        
        // Настройка Glide для оптимальной загрузки изображений
        configureGlide();
        
        // Применение темы при старте приложения
        applyAppTheme();
    }
    
    /**
     * Настройка Glide для оптимальной загрузки изображений
     */
    private void configureGlide() {
        GlideBuilder builder = new GlideBuilder();
        
        // Увеличиваем размер кеша на диске для изображений (50МБ)
        builder.setDiskCache(new InternalCacheDiskCacheFactory(this, "glideDiskCache", 50 * 1024 * 1024));
        
        // Максимально увеличиваем размер кеша в памяти
        builder.setMemorySizeCalculator(new com.bumptech.glide.load.engine.cache.MemorySizeCalculator.Builder(this)
                .setMemoryCacheScreens(5) // Сохраняем в памяти больше экранов
                .setBitmapPoolScreens(3) // Увеличиваем пул для повторного использования bitmap
                .build());
        
        // Настройка исполнителя потоков для приоритизации загрузки
        builder.setDefaultRequestOptions(new RequestOptions().onlyRetrieveFromCache(false));
        
        // Глобальные настройки для всех запросов Glide
        RequestOptions requestOptions = new RequestOptions()
                .format(DecodeFormat.PREFER_RGB_565)  // Уменьшает использование памяти (16 бит вместо 32)
                .diskCacheStrategy(DiskCacheStrategy.ALL) // Кешируем все изображения (оригиналы и трансформации)
                .skipMemoryCache(false) // Используем кеш в памяти
                .placeholder(R.drawable.placeholder_image) // Заглушка на время загрузки
                .priority(com.bumptech.glide.Priority.IMMEDIATE) // Наивысший приоритет загрузки 
                .encodeQuality(80) // Снижаем качество для ускорения
                .override(600, 600) // Ограничиваем максимальный размер для ускорения декодирования
                .dontAnimate() // Отключаем анимацию для ускорения
                .dontTransform(); // Отключаем трансформации для ускорения
        
        builder.setDefaultRequestOptions(requestOptions);

        // Настройка пула потоков для экстремальной скорости загрузки
        int threadPoolSize = Math.max(1, Runtime.getRuntime().availableProcessors());
        com.bumptech.glide.load.engine.executor.GlideExecutor diskExecutor = 
            com.bumptech.glide.load.engine.executor.GlideExecutor.newDiskCacheExecutor(
                threadPoolSize, 
                "GlideDisk", 
                com.bumptech.glide.load.engine.executor.GlideExecutor.UncaughtThrowableStrategy.DEFAULT);
        
        com.bumptech.glide.load.engine.executor.GlideExecutor sourceExecutor = 
            com.bumptech.glide.load.engine.executor.GlideExecutor.newSourceExecutor(
                threadPoolSize * 2, // Используем больше потоков для загрузки
                "GlideSource", 
                com.bumptech.glide.load.engine.executor.GlideExecutor.UncaughtThrowableStrategy.DEFAULT);
        
        builder.setDiskCacheExecutor(diskExecutor)
               .setSourceExecutor(sourceExecutor);
        
        // Применяем настройки
        Glide.init(this, builder);
        
        // Прогрев кеша - загружаем часто используемые ресурсы заранее
        preloadImages();
    }
    
    /**
     * Настраивает предварительную загрузку изображений
     */
    private void preloadImages() {
        // Создаем поток для асинхронной предзагрузки
        new Thread(() -> {
            try {
                // Даем системе загрузиться
                Thread.sleep(1000);
                
                // Предзагрузка плейсхолдера
                Glide.with(getApplicationContext())
                    .load(R.drawable.placeholder_image)
                    .diskCacheStrategy(DiskCacheStrategy.RESOURCE)
                    .preload();
                
                // Здесь можно добавить загрузку других часто используемых изображений
                
            } catch (Exception e) {
                // Логируем ошибки
                android.util.Log.e("Glide", "Error preloading images", e);
            }
        }).start();
    }
    
    /**
     * Применение выбранной темы из настроек
     */
    private void applyAppTheme() {
        // Используем тот же формат Shared Preferences что и в SessionManager
        SharedPreferences prefs = getSharedPreferences("user_session", Context.MODE_PRIVATE);
        String settingPrefix = "setting_";
        String fullKey = settingPrefix + THEME_PREF_KEY;
        String theme = prefs.getString(fullKey, THEME_SYSTEM);
        
        // Логирование для отладки
        android.util.Log.d("WarehouseApplication", "Применяю глобальную тему: " + theme);
        android.util.Log.d("WarehouseApplication", "Ключ настройки: " + fullKey);
        android.util.Log.d("WarehouseApplication", "Все настройки: " + prefs.getAll().toString());
        
        // Устанавливаем режим темы для всего приложения
        switch (theme) {
            case THEME_DARK:
                // Принудительно устанавливаем темную тему
                android.util.Log.d("WarehouseApplication", "Устанавливаю темную тему");
                AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_YES);
                break;
                
            case THEME_LIGHT:
                // Принудительно устанавливаем светлую тему
                android.util.Log.d("WarehouseApplication", "Устанавливаю светлую тему");
                AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_NO);
                break;
                
            case THEME_SYSTEM:
            default:
                // Следуем системным настройкам
                android.util.Log.d("WarehouseApplication", "Устанавливаю системную тему");
                AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_FOLLOW_SYSTEM);
                break;
        }
    }
} 