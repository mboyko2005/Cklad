package com.example.apk.api;

import com.google.gson.annotations.SerializedName;

/**
 * Ответ с данными роли
 */
public class RoleResponse {
    
    @SerializedName("id")
    private int id;
    
    @SerializedName("name")
    private String name;
    
    public RoleResponse(int id, String name) {
        this.id = id;
        this.name = name;
    }
    
    public int getId() {
        return id;
    }
    
    public void setId(int id) {
        this.id = id;
    }
    
    public String getName() {
        return name;
    }
    
    public void setName(String name) {
        this.name = name;
    }
} 