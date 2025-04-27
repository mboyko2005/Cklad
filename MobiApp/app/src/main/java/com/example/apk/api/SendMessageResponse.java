package com.example.apk.api;

import com.google.gson.annotations.SerializedName;

public class SendMessageResponse {
    @SerializedName("success")
    private boolean success;

    @SerializedName("message")
    private MessageResponse message;

    public boolean isSuccess() {
        return success;
    }

    public MessageResponse getMessage() {
        return message;
    }
} 