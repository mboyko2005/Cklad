package com.example.apk.admin;

import android.app.AlertDialog;
import android.os.Bundle;
import android.text.Editable;
import android.text.TextWatcher;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ImageButton;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;

import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.example.apk.R;
import com.example.apk.api.ApiClient;
import com.example.apk.api.ApiService;
import com.example.apk.api.InventoryRequest;
import com.example.apk.api.InventoryResponse;
import com.example.apk.api.WarehouseResponse;
import com.example.apk.models.InventoryItem;
import com.example.apk.utils.SessionManager;
import com.google.android.material.floatingactionbutton.FloatingActionButton;

import java.util.ArrayList;
import java.util.List;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

/**
 * Активность для управления складскими позициями
 */
public class ManageInventoryActivity extends AppCompatActivity {
    private static final String TAG = "ManageInventoryActivity";

    private ApiService apiService;
    private SessionManager sessionManager;
    private InventoryAdapter inventoryAdapter;
    private List<WarehouseResponse> warehouses = new ArrayList<>();
    
    private RecyclerView inventoryRecyclerView;
    private EditText searchInput;
    private FloatingActionButton addInventoryButton;
    private FloatingActionButton editInventoryButton;
    private FloatingActionButton deleteInventoryButton;
    
    private InventoryItem selectedItem = null;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_manage_inventory);
        
        // Инициализация API и менеджера сессий
        apiService = ApiClient.getClient().create(ApiService.class);
        sessionManager = new SessionManager(this);
        
        // Проверка авторизации
        if (!sessionManager.isLoggedIn() || !sessionManager.getRole().equals("Администратор")) {
            Toast.makeText(this, "Доступ запрещен", Toast.LENGTH_SHORT).show();
            finish();
            return;
        }
        
        // Инициализация UI
        initViews();
        
        // Загрузка данных
        loadWarehouses();
        loadInventory();
    }
    
    private void initViews() {
        // Настройка кнопки "Назад"
        ImageButton backButton = findViewById(R.id.backButton);
        backButton.setOnClickListener(v -> finish());
        
        // Настройка RecyclerView
        inventoryRecyclerView = findViewById(R.id.inventoryRecyclerView);
        inventoryRecyclerView.setLayoutManager(new LinearLayoutManager(this));
        inventoryAdapter = new InventoryAdapter();
        inventoryRecyclerView.setAdapter(inventoryAdapter);
        
        // Настройка выбора позиции
        inventoryAdapter.setOnInventoryItemClickListener((item, position) -> {
            selectedItem = item;
            inventoryAdapter.setSelectedItem(item);
            editInventoryButton.setEnabled(true);
            deleteInventoryButton.setEnabled(true);
        });
        
        // Поиск
        searchInput = findViewById(R.id.searchInput);
        searchInput.addTextChangedListener(new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence s, int start, int count, int after) {}

            @Override
            public void onTextChanged(CharSequence s, int start, int before, int count) {}

            @Override
            public void afterTextChanged(Editable s) {
                inventoryAdapter.filter(s.toString());
            }
        });
        
        // Кнопки действий
        addInventoryButton = findViewById(R.id.addInventoryButton);
        editInventoryButton = findViewById(R.id.editInventoryButton);
        deleteInventoryButton = findViewById(R.id.deleteInventoryButton);
        
        addInventoryButton.setOnClickListener(v -> showInventoryDialog(false));
        editInventoryButton.setOnClickListener(v -> {
            if (selectedItem != null) {
                showInventoryDialog(true);
            } else {
                Toast.makeText(this, "Сначала выберите позицию", Toast.LENGTH_SHORT).show();
            }
        });
        deleteInventoryButton.setOnClickListener(v -> {
            if (selectedItem != null) {
                showDeleteConfirmDialog();
            } else {
                Toast.makeText(this, "Сначала выберите позицию", Toast.LENGTH_SHORT).show();
            }
        });
        
        // Изначально кнопки редактирования и удаления неактивны
        editInventoryButton.setEnabled(false);
        deleteInventoryButton.setEnabled(false);
    }
    
    /**
     * Загрузка списка складов
     */
    private void loadWarehouses() {
        String token = "Bearer " + sessionManager.getToken();
        Log.d(TAG, "Загрузка складов, токен: " + token);
        
        apiService.getWarehouses(token).enqueue(new Callback<List<WarehouseResponse>>() {
            @Override
            public void onResponse(Call<List<WarehouseResponse>> call, Response<List<WarehouseResponse>> response) {
                if (response.isSuccessful() && response.body() != null) {
                    warehouses.clear();
                    warehouses.addAll(response.body());
                    Log.d(TAG, "Загружено складов: " + warehouses.size());
                } else {
                    String errorMessage = "Ошибка загрузки списка складов: " + 
                        (response.code() == 401 ? "Необходима авторизация" : 
                         response.code() == 403 ? "Доступ запрещен" : 
                         "Код ответа: " + response.code());
                    Log.e(TAG, errorMessage);
                    Toast.makeText(ManageInventoryActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<List<WarehouseResponse>> call, Throwable t) {
                String errorMessage = "Ошибка сети: " + t.getMessage();
                Log.e(TAG, errorMessage, t);
                Toast.makeText(ManageInventoryActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
            }
        });
    }
    
    /**
     * Загрузка списка складских позиций
     */
    private void loadInventory() {
        String token = "Bearer " + sessionManager.getToken();
        Log.d(TAG, "Загрузка складских позиций, токен: " + token);
        
        apiService.getInventory(token).enqueue(new Callback<List<InventoryResponse>>() {
            @Override
            public void onResponse(Call<List<InventoryResponse>> call, Response<List<InventoryResponse>> response) {
                if (response.isSuccessful() && response.body() != null) {
                    Log.d(TAG, "Получен ответ от сервера, позиций: " + response.body().size());
                    
                    List<InventoryItem> items = new ArrayList<>();
                    for (InventoryResponse item : response.body()) {
                        items.add(new InventoryItem(
                            item.getPositionId(),
                            item.getProductId(),
                            item.getProductName(),
                            item.getCategory(),
                            item.getPrice(),
                            item.getQuantity(),
                            item.getWarehouseName(),
                            item.getWarehouseId(),
                            item.getSupplierName()
                        ));
                    }
                    
                    inventoryAdapter.updateItems(items);
                    
                    // Сбрасываем выбор
                    selectedItem = null;
                    editInventoryButton.setEnabled(false);
                    deleteInventoryButton.setEnabled(false);
                    
                    Log.d(TAG, "Загружено позиций: " + items.size());
                } else {
                    String errorMessage = "Ошибка загрузки складских позиций: " + 
                        (response.code() == 401 ? "Необходима авторизация" : 
                         response.code() == 403 ? "Доступ запрещен" : 
                         "Код ответа: " + response.code());
                    Log.e(TAG, errorMessage);
                    Toast.makeText(ManageInventoryActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<List<InventoryResponse>> call, Throwable t) {
                String errorMessage = "Ошибка сети: " + t.getMessage();
                Log.e(TAG, errorMessage, t);
                Toast.makeText(ManageInventoryActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
            }
        });
    }
    
    /**
     * Отображение диалога добавления/редактирования позиции
     */
    private void showInventoryDialog(boolean isEdit) {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        View dialogView = LayoutInflater.from(this).inflate(R.layout.dialog_inventory_form, null);
        builder.setView(dialogView);
        
        // Находим элементы в диалоге
        TextView dialogTitle = dialogView.findViewById(R.id.dialogTitle);
        EditText productNameInput = dialogView.findViewById(R.id.productNameInput);
        EditText categoryInput = dialogView.findViewById(R.id.categoryInput);
        EditText supplierInput = dialogView.findViewById(R.id.supplierInput);
        EditText priceInput = dialogView.findViewById(R.id.priceInput);
        EditText quantityInput = dialogView.findViewById(R.id.quantityInput);
        Spinner warehouseSpinner = dialogView.findViewById(R.id.warehouseSpinner);
        Button cancelButton = dialogView.findViewById(R.id.cancelButton);
        Button saveButton = dialogView.findViewById(R.id.saveButton);
        
        // Устанавливаем заголовок
        dialogTitle.setText(isEdit ? "Редактировать позицию" : "Добавить позицию");
        
        // Заполняем спиннер складов
        ArrayAdapter<WarehouseResponse> warehouseAdapter = new ArrayAdapter<>(this, 
                android.R.layout.simple_spinner_dropdown_item, warehouses);
        warehouseSpinner.setAdapter(warehouseAdapter);
        
        // Если редактирование, заполняем поля
        if (isEdit && selectedItem != null) {
            productNameInput.setText(selectedItem.getProductName());
            categoryInput.setText(selectedItem.getCategory());
            supplierInput.setText(selectedItem.getSupplierName());
            priceInput.setText(String.valueOf(selectedItem.getPrice()));
            quantityInput.setText(String.valueOf(selectedItem.getQuantity()));
            
            // Находим индекс выбранного склада
            for (int i = 0; i < warehouses.size(); i++) {
                if (warehouses.get(i).getId() == selectedItem.getWarehouseId()) {
                    warehouseSpinner.setSelection(i);
                    break;
                }
            }
        }
        
        AlertDialog dialog = builder.create();
        
        // Обработчики кнопок
        cancelButton.setOnClickListener(v -> dialog.dismiss());
        
        saveButton.setOnClickListener(v -> {
            // Получаем введенные данные
            String productName = productNameInput.getText().toString().trim();
            String category = categoryInput.getText().toString().trim();
            String supplier = supplierInput.getText().toString().trim();
            
            // Проверка обязательных полей
            if (productName.isEmpty() || category.isEmpty() || supplier.isEmpty()) {
                Toast.makeText(this, "Заполните все текстовые поля", Toast.LENGTH_SHORT).show();
                return;
            }
            
            // Преобразуем числовые поля
            double price;
            int quantity;
            try {
                price = Double.parseDouble(priceInput.getText().toString().trim());
                quantity = Integer.parseInt(quantityInput.getText().toString().trim());
            } catch (NumberFormatException e) {
                Toast.makeText(this, "Введите корректные значения для цены и количества", Toast.LENGTH_SHORT).show();
                return;
            }
            
            // Проверяем допустимые значения
            if (price < 0 || quantity < 0) {
                Toast.makeText(this, "Цена и количество не могут быть отрицательными", Toast.LENGTH_SHORT).show();
                return;
            }
            
            // Получаем выбранный склад
            if (warehouseSpinner.getSelectedItem() == null) {
                Toast.makeText(this, "Выберите склад", Toast.LENGTH_SHORT).show();
                return;
            }
            WarehouseResponse selectedWarehouse = (WarehouseResponse) warehouseSpinner.getSelectedItem();
            
            // Создаем запрос
            InventoryRequest request = new InventoryRequest(
                productName, supplier, category, price, selectedWarehouse.getId(), quantity
            );
            
            if (isEdit) {
                // Редактирование
                updateInventoryItem(selectedItem.getPositionId(), request);
            } else {
                // Создание
                createInventoryItem(request);
            }
            
            dialog.dismiss();
        });
        
        dialog.show();
    }
    
    /**
     * Создание новой складской позиции
     */
    private void createInventoryItem(InventoryRequest request) {
        String token = "Bearer " + sessionManager.getToken();
        Log.d(TAG, "Создание складской позиции: " + request.getProductName());
        
        apiService.createInventoryItem(token, request).enqueue(new Callback<InventoryResponse>() {
            @Override
            public void onResponse(Call<InventoryResponse> call, Response<InventoryResponse> response) {
                if (response.isSuccessful()) {
                    Toast.makeText(ManageInventoryActivity.this, 
                            "Позиция успешно создана", Toast.LENGTH_SHORT).show();
                    Log.d(TAG, "Позиция успешно создана");
                    loadInventory(); // Перезагружаем список
                } else {
                    String errorMessage = "Ошибка создания позиции: " + 
                        (response.code() == 400 ? "Некорректные данные" :
                         response.code() == 401 ? "Необходима авторизация" : 
                         response.code() == 403 ? "Доступ запрещен" : 
                         "Код ответа: " + response.code());
                    Log.e(TAG, errorMessage);
                    Toast.makeText(ManageInventoryActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
                    
                    // Дополнительное логирование ошибки
                    try {
                        if (response.errorBody() != null) {
                            String errorBody = response.errorBody().string();
                            Log.e(TAG, "Тело ошибки: " + errorBody);
                        }
                    } catch (Exception e) {
                        Log.e(TAG, "Не удалось прочитать тело ошибки", e);
                    }
                }
            }

            @Override
            public void onFailure(Call<InventoryResponse> call, Throwable t) {
                String errorMessage = "Ошибка сети: " + t.getMessage();
                Log.e(TAG, errorMessage, t);
                Toast.makeText(ManageInventoryActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
            }
        });
    }
    
    /**
     * Обновление складской позиции
     */
    private void updateInventoryItem(int positionId, InventoryRequest request) {
        String token = "Bearer " + sessionManager.getToken();
        Log.d(TAG, "Обновление складской позиции ID: " + positionId);
        
        apiService.updateInventoryItem(token, positionId, request).enqueue(new Callback<InventoryResponse>() {
            @Override
            public void onResponse(Call<InventoryResponse> call, Response<InventoryResponse> response) {
                if (response.isSuccessful()) {
                    Toast.makeText(ManageInventoryActivity.this, 
                            "Позиция успешно обновлена", Toast.LENGTH_SHORT).show();
                    Log.d(TAG, "Позиция успешно обновлена");
                    loadInventory(); // Перезагружаем список
                } else {
                    String errorMessage = "Ошибка обновления позиции: " + 
                        (response.code() == 400 ? "Некорректные данные" :
                         response.code() == 401 ? "Необходима авторизация" : 
                         response.code() == 403 ? "Доступ запрещен" : 
                         response.code() == 404 ? "Позиция не найдена" :
                         "Код ответа: " + response.code());
                    Log.e(TAG, errorMessage);
                    Toast.makeText(ManageInventoryActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
                    
                    // Дополнительное логирование ошибки
                    try {
                        if (response.errorBody() != null) {
                            String errorBody = response.errorBody().string();
                            Log.e(TAG, "Тело ошибки: " + errorBody);
                        }
                    } catch (Exception e) {
                        Log.e(TAG, "Не удалось прочитать тело ошибки", e);
                    }
                }
            }

            @Override
            public void onFailure(Call<InventoryResponse> call, Throwable t) {
                String errorMessage = "Ошибка сети: " + t.getMessage();
                Log.e(TAG, errorMessage, t);
                Toast.makeText(ManageInventoryActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
            }
        });
    }
    
    /**
     * Показывает диалог подтверждения удаления
     */
    private void showDeleteConfirmDialog() {
        AlertDialog.Builder builder = new AlertDialog.Builder(this);
        View dialogView = LayoutInflater.from(this).inflate(R.layout.dialog_confirm, null);
        builder.setView(dialogView);
        
        TextView confirmTitle = dialogView.findViewById(R.id.confirmTitle);
        TextView confirmMessage = dialogView.findViewById(R.id.confirmMessage);
        Button noButton = dialogView.findViewById(R.id.noButton);
        Button yesButton = dialogView.findViewById(R.id.yesButton);
        
        confirmTitle.setText("Удаление позиции");
        confirmMessage.setText("Вы действительно хотите удалить позицию " + 
                selectedItem.getProductName() + "?");
        
        AlertDialog dialog = builder.create();
        
        noButton.setOnClickListener(v -> dialog.dismiss());
        
        yesButton.setOnClickListener(v -> {
            deleteInventoryItem(selectedItem.getPositionId());
            dialog.dismiss();
        });
        
        dialog.show();
    }
    
    /**
     * Удаление складской позиции
     */
    private void deleteInventoryItem(int positionId) {
        String token = "Bearer " + sessionManager.getToken();
        Log.d(TAG, "Удаление складской позиции ID: " + positionId);
        
        apiService.deleteInventoryItem(token, positionId).enqueue(new Callback<Void>() {
            @Override
            public void onResponse(Call<Void> call, Response<Void> response) {
                if (response.isSuccessful()) {
                    Toast.makeText(ManageInventoryActivity.this, 
                            "Позиция успешно удалена", Toast.LENGTH_SHORT).show();
                    Log.d(TAG, "Позиция успешно удалена");
                    loadInventory(); // Перезагружаем список
                } else {
                    String errorMessage = "Ошибка удаления позиции: " + 
                        (response.code() == 401 ? "Необходима авторизация" : 
                         response.code() == 403 ? "Доступ запрещен" : 
                         response.code() == 404 ? "Позиция не найдена" :
                         "Код ответа: " + response.code());
                    Log.e(TAG, errorMessage);
                    Toast.makeText(ManageInventoryActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
                    
                    // Дополнительное логирование ошибки
                    try {
                        if (response.errorBody() != null) {
                            String errorBody = response.errorBody().string();
                            Log.e(TAG, "Тело ошибки: " + errorBody);
                        }
                    } catch (Exception e) {
                        Log.e(TAG, "Не удалось прочитать тело ошибки", e);
                    }
                }
            }

            @Override
            public void onFailure(Call<Void> call, Throwable t) {
                String errorMessage = "Ошибка сети: " + t.getMessage();
                Log.e(TAG, errorMessage, t);
                Toast.makeText(ManageInventoryActivity.this, errorMessage, Toast.LENGTH_SHORT).show();
            }
        });
    }
} 
 
 
 
 