package com.example.apk.data;

import androidx.lifecycle.LiveData;
import androidx.room.Dao;
import androidx.room.Insert;
import androidx.room.OnConflictStrategy;
import androidx.room.Query;
import androidx.room.Update;

import com.example.apk.models.Message;

import java.util.List;

@Dao
public interface MessageDao {
    
    @Query("SELECT * FROM messages WHERE chatId = :chatId ORDER BY timestamp ASC")
    LiveData<List<Message>> getMessagesByChatId(String chatId);
    
    @Query("SELECT * FROM messages WHERE chatId = :chatId ORDER BY timestamp ASC")
    List<Message> getMessagesByChatIdSync(String chatId);
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    void insertMessages(List<Message> messages);
    
    @Insert(onConflict = OnConflictStrategy.REPLACE)
    void insertMessage(Message message);
    
    @Update
    void updateMessage(Message message);
    
    @Query("DELETE FROM messages WHERE chatId = :chatId")
    void deleteMessagesByChatId(String chatId);
    
    @Query("DELETE FROM messages")
    void deleteAllMessages();
    
    @Query("SELECT * FROM messages WHERE messageId = :messageId")
    Message getMessageById(int messageId);
    
    @Query("UPDATE messages SET isRead = 1 WHERE chatId = :chatId AND receiverId = :currentUserId")
    void markMessagesAsRead(String chatId, int currentUserId);
    
    @Query("SELECT COUNT(*) FROM messages WHERE isRead = 0 AND receiverId = :userId")
    LiveData<Integer> getUnreadMessagesCount(int userId);
} 