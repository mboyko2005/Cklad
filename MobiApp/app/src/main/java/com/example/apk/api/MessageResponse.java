package com.example.apk.api;

import com.google.gson.annotations.SerializedName;

public class MessageResponse {
    @SerializedName("messageId")
    private int messageId;

    @SerializedName("senderId")
    private int senderId;

    @SerializedName("receiverId")
    private int receiverId;

    @SerializedName("text")
    private String text;

    @SerializedName("timestamp")
    private String timestamp;

    @SerializedName("isRead")
    private boolean isRead;

    @SerializedName("hasAttachment")
    private boolean hasAttachment;

    public int getMessageId() {
        return messageId;
    }

    public int getSenderId() {
        return senderId;
    }

    public int getReceiverId() {
        return receiverId;
    }

    public String getText() {
        return text;
    }

    public String getTimestamp() {
        return timestamp;
    }

    public boolean isRead() {
        return isRead;
    }

    public boolean hasAttachment() {
        return hasAttachment;
    }
} 