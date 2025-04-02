package com.example.apk.api;

import com.google.gson.annotations.SerializedName;

/**
 * Класс для создания или обновления складской позиции
 */
public class InventoryRequest {
    
    @SerializedName("productName")
    private String productName;
    
    @SerializedName("supplierName")
    private String supplierName;
    
    @SerializedName("category")
    private String category;
    
    @SerializedName("price")
    private double price;
    
    @SerializedName("warehouseId")
    private int warehouseId;
    
    @SerializedName("quantity")
    private int quantity;
    
    public InventoryRequest(String productName, String supplierName, String category, 
                           double price, int warehouseId, int quantity) {
        this.productName = productName;
        this.supplierName = supplierName;
        this.category = category;
        this.price = price;
        this.warehouseId = warehouseId;
        this.quantity = quantity;
    }
    
    public String getProductName() {
        return productName;
    }
    
    public void setProductName(String productName) {
        this.productName = productName;
    }
    
    public String getSupplierName() {
        return supplierName;
    }
    
    public void setSupplierName(String supplierName) {
        this.supplierName = supplierName;
    }
    
    public String getCategory() {
        return category;
    }
    
    public void setCategory(String category) {
        this.category = category;
    }
    
    public double getPrice() {
        return price;
    }
    
    public void setPrice(double price) {
        this.price = price;
    }
    
    public int getWarehouseId() {
        return warehouseId;
    }
    
    public void setWarehouseId(int warehouseId) {
        this.warehouseId = warehouseId;
    }
    
    public int getQuantity() {
        return quantity;
    }
    
    public void setQuantity(int quantity) {
        this.quantity = quantity;
    }
} 
 
<<<<<<< HEAD
<<<<<<< HEAD
=======
 
 
>>>>>>> 1dea6d5621f4f889dffd0814aeeb98a9d2d0ba87
=======
 
 
>>>>>>> 1dea6d5621f4f889dffd0814aeeb98a9d2d0ba87
 