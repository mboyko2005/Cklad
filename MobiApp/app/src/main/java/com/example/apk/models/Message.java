package com.example.apk.models;

import androidx.annotation.NonNull;
import androidx.room.Entity;
import androidx.room.PrimaryKey;

/**
 * Модель сообщения для хранения в базе данных Room
 */
@Entity(tableName = "messages")
public class Message {
    @PrimaryKey
    private int messageId;
    
    private int senderId;
    private int receiverId;
    private String text;
    private String timestamp;
    private boolean isRead;
    private boolean hasAttachment;
    private String chatId; // Для связи с ChatItem
    
    public Message(int messageId, int senderId, int receiverId, String text, String timestamp, boolean isRead, boolean hasAttachment, String chatId) {
        this.messageId = messageId;
        this.senderId = senderId;
        this.receiverId = receiverId;
        this.text = text;
        this.timestamp = timestamp;
        this.isRead = isRead;
        this.hasAttachment = hasAttachment;
        this.chatId = chatId;
    }
    
    public int getMessageId() {
        return messageId;
    }
    
    public void setMessageId(int messageId) {
        this.messageId = messageId;
    }
    
    public int getSenderId() {
        return senderId;
    }
    
    public void setSenderId(int senderId) {
        this.senderId = senderId;
    }
    
    public int getReceiverId() {
        return receiverId;
    }
    
    public void setReceiverId(int receiverId) {
        this.receiverId = receiverId;
    }
    
    public String getText() {
        return text;
    }
    
    public void setText(String text) {
        this.text = text;
    }
    
    public String getTimestamp() {
        return timestamp;
    }
    
    public void setTimestamp(String timestamp) {
        this.timestamp = timestamp;
    }
    
    public boolean isRead() {
        return isRead;
    }
    
    public void setRead(boolean isRead) {
        this.isRead = isRead;
    }
    
    public boolean hasAttachment() {
        return hasAttachment;
    }
    
    public void setHasAttachment(boolean hasAttachment) {
        this.hasAttachment = hasAttachment;
    }
    
    public String getChatId() {
        return chatId;
    }
    
    public void setChatId(String chatId) {
        this.chatId = chatId;
    }
    
    @Override
    public boolean equals(Object obj) {
        if (this == obj) return true;
        if (obj == null || getClass() != obj.getClass()) return false;
        
        Message message = (Message) obj;
        return messageId == message.messageId;
    }
    
    @Override
    public int hashCode() {
        return Integer.hashCode(messageId);
    }
} 