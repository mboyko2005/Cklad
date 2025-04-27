package com.example.apk.data;

import androidx.lifecycle.LiveData;
import androidx.room.Dao;
import androidx.room.Insert;
import androidx.room.OnConflictStrategy;
import androidx.room.Query;
import androidx.room.Update;

import com.example.apk.models.ChatItem;

import java.util.List;

@Dao
public interface ChatDao {
    
    @Query("SELECT * FROM chats ORDER BY lastMessageTimestamp DESC")
    LiveData<List<ChatItem>> getAllChats();
    
    @Query("SELECT * FROM chats ORDER BY lastMessageTimestamp DESC")
    List<ChatItem> getAllChatsSync();
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    void insertChats(List<ChatItem> chats);
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    void insertChat(ChatItem chat);
    
    @Update
    void updateChat(ChatItem chat);
    
    @Query("DELETE FROM chats")
    void deleteAllChats();
    
    @Query("SELECT * FROM chats WHERE chatId = :chatId")
    ChatItem getChatById(String chatId);
} 