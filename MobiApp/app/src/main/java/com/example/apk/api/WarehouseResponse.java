package com.example.apk.api;

import com.google.gson.annotations.SerializedName;

/**
 * Класс для хранения данных о складе, полученных от API
 */
public class WarehouseResponse {
    
    @SerializedName("id")
    private int id;
    
    @SerializedName("name")
    private String name;
    
    public WarehouseResponse(int id, String name) {
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
    
    @Override
    public String toString() {
        return name;
    }
} 
 
 
 
 