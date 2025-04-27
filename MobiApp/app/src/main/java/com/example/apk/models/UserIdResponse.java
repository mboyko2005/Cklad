package com.example.apk.models;

import com.google.gson.annotations.SerializedName;

public class UserIdResponse {
    @SerializedName("success")
    private boolean success;

    @SerializedName("userId")
    private int userId;

    @SerializedName("message")
    private String message; // На случай ошибки от сервера

    public boolean isSuccess() {
        return success;
    }

    public int getUserId() {
        return userId;
    }

    public String getMessage() {
        return message;
    }
} 