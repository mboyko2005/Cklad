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
 
<<<<<<< HEAD
<<<<<<< HEAD
=======
 
 
>>>>>>> 1dea6d5621f4f889dffd0814aeeb98a9d2d0ba87
=======
 
 
>>>>>>> 1dea6d5621f4f889dffd0814aeeb98a9d2d0ba87
 