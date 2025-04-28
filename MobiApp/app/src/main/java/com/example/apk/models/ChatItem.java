package com.example.apk.models;

import androidx.annotation.NonNull;
import androidx.room.Entity;
import androidx.room.PrimaryKey;

/**
 * Модель чата
 */
@Entity(tableName = "chats")
public class ChatItem {
    @PrimaryKey
    @NonNull
    private String chatId;
    private String chatName;
    private String lastMessage;
    private long lastMessageTimestamp;
    private boolean isNew;
    private int unreadCount;

    public ChatItem(String chatId, String chatName, String lastMessage) {
        this.chatId = chatId;
        this.chatName = chatName;
        this.lastMessage = lastMessage;
        this.lastMessageTimestamp = System.currentTimeMillis();
        this.isNew = false;
        this.unreadCount = 0;
    }

    @NonNull
    public String getChatId() {
        return chatId;
    }

    public void setChatId(@NonNull String chatId) {
        this.chatId = chatId;
    }

    public String getChatName() {
        return chatName;
    }

    public void setChatName(String chatName) {
        this.chatName = chatName;
    }

    public String getLastMessage() {
        return lastMessage;
    }

    public void setLastMessage(String lastMessage) {
        this.lastMessage = lastMessage;
    }
    
    public long getLastMessageTimestamp() {
        return lastMessageTimestamp;
    }
    
    public void setLastMessageTimestamp(long lastMessageTimestamp) {
        this.lastMessageTimestamp = lastMessageTimestamp;
    }
    
    public boolean isNew() {
        return isNew;
    }
    
    public void setNew(boolean isNew) {
        this.isNew = isNew;
    }
    
    public int getUnreadCount() {
        return unreadCount;
    }
    
    public void setUnreadCount(int unreadCount) {
        this.unreadCount = unreadCount;
    }
    
    @Override
    public boolean equals(Object obj) {
        if (this == obj) return true;
        if (obj == null || getClass() != obj.getClass()) return false;
        
        ChatItem chatItem = (ChatItem) obj;
        return chatId.equals(chatItem.chatId);
    }
    
    @Override
    public int hashCode() {
        return chatId.hashCode();
    }
}
