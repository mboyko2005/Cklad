package com.example.apk.models;

/**
 * Модель данных для работы со складскими позициями в приложении
 */
public class InventoryItem {
    private int positionId;
    private int productId;
    private String productName;
    private String category;
    private double price;
    private int quantity;
    private String warehouseName;
    private int warehouseId;
    private String supplierName;
    
    public InventoryItem(int positionId, int productId, String productName, String category, 
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
    
    /**
     * Получает идентификатор для работы в приложении:
     * - Если positionId > 0, возвращает positionId
     * - Иначе, возвращает productId
     */
    public int getItemId() {
        return positionId > 0 ? positionId : productId;
    }
} 
 
<<<<<<< HEAD
=======
 
 
>>>>>>> 1dea6d5621f4f889dffd0814aeeb98a9d2d0ba87
 