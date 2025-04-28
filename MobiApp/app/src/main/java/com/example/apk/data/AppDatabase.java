package com.example.apk.data;

import android.content.Context;

import androidx.room.Database;
import androidx.room.Room;
import androidx.room.RoomDatabase;

import com.example.apk.models.ChatItem;
import com.example.apk.models.Message;

@Database(entities = {ChatItem.class, Message.class}, version = 3, exportSchema = false)
public abstract class AppDatabase extends RoomDatabase {
    
    private static volatile AppDatabase INSTANCE;
    
    public abstract ChatDao chatDao();
    public abstract MessageDao messageDao();
    
    public static AppDatabase getDatabase(final Context context) {
        if (INSTANCE == null) {
            synchronized (AppDatabase.class) {
                if (INSTANCE == null) {
                    INSTANCE = Room.databaseBuilder(
                            context.getApplicationContext(),
                            AppDatabase.class,
                            "app_database")
                            .fallbackToDestructiveMigration() // При изменении версии схемы, перестроить базу заново
                            .build();
                }
            }
        }
        return INSTANCE;
    }
} 