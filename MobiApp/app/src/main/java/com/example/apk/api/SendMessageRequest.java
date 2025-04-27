package com.example.apk.api;

public class SendMessageRequest {
    private int senderId;
    private int receiverId;
    private String text;

    public SendMessageRequest(int senderId, int receiverId, String text) {
        this.senderId = senderId;
        this.receiverId = receiverId;
        this.text = text;
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
} 