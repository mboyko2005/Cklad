package com.example.apk.api;

import com.google.gson.annotations.SerializedName;
import java.util.List;

public class MessagesResponse {
    @SerializedName("success")
    private boolean success;

    @SerializedName("messages")
    private List<MessageResponse> messages;

    public boolean isSuccess() {
        return success;
    }

    public List<MessageResponse> getMessages() {
        return messages;
    }
} 