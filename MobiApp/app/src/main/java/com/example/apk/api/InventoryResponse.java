package com.example.apk.api;

import com.google.gson.annotations.SerializedName;

/**
 * Класс для хранения данных о складской позиции, полученных от API
 */
public class InventoryResponse {
    
    @SerializedName("positionID")
    private int positionId;
    
    @SerializedName("productId")
    private int productId;
    
    @SerializedName("productName")
    private String productName;
    
    @SerializedName("category")
    private String category;
    
    @SerializedName("price")
    private double price;
    
    @SerializedName("quantity")
    private int quantity;
    
    @SerializedName("warehouseName")
    private String warehouseName;
    
    @SerializedName("warehouseId")
    private int warehouseId;
    
    @SerializedName("supplierName")
    private String supplierName;
    
    public InventoryResponse() {
    }

    public InventoryResponse(int positionId, int productId, String productName, String category, 
                             double price, int quantity, String warehouseName, int warehouseId, 
                             String supplierName) {
        this.positionId = positionId;
        this.productId = productId;
        this.productName = productName;
        this.category = category;
        this.price = price;
        this.quantity = quantity;
        this.warehouseName = warehouseName;
        this.warehouseId = warehouseId;
        this.supplierName = supplierName;
    }

    public int getPositionId() {
        return positionId;
    }

    public void setPositionId(int positionId) {
        this.positionId = positionId;
    }

    public int getProductId() {
        return productId;
    }

    public void setProductId(int productId) {
        this.productId = productId;
    }

    public String getProductName() {
        return productName;
    }

    public void setProductName(String productName) {
        this.productName = productName;
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

    public int getQuantity() {
        return quantity;
    }

    public void setQuantity(int quantity) {
        this.quantity = quantity;
    }

    public String getWarehouseName() {
        return warehouseName;
    }

    public void setWarehouseName(String warehouseName) {
        this.warehouseName = warehouseName;
    }

    public int getWarehouseId() {
        return warehouseId;
    }

    public void setWarehouseId(int warehouseId) {
        this.warehouseId = warehouseId;
    }

    public String getSupplierName() {
        return supplierName;
    }

    public void setSupplierName(String supplierName) {
        this.supplierName = supplierName;
    }
} 
 
<<<<<<< HEAD
<<<<<<< HEAD
=======
 
 
>>>>>>> 1dea6d5621f4f889dffd0814aeeb98a9d2d0ba87
=======
 
 
>>>>>>> 1dea6d5621f4f889dffd0814aeeb98a9d2d0ba87
 