package com.example.apk.admin;

import android.graphics.Color;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.example.apk.R;
import com.example.apk.models.InventoryItem;

import java.util.ArrayList;
import java.util.List;
import java.util.Locale;

/**
 * Адаптер для отображения списка складских позиций
 */
public class InventoryAdapter extends RecyclerView.Adapter<InventoryAdapter.InventoryViewHolder> {

    private List<InventoryItem> items;
    private List<InventoryItem> filteredItems;
    private OnInventoryItemClickListener listener;
    private InventoryItem selectedItem;

    public InventoryAdapter() {
        this.items = new ArrayList<>();
        this.filteredItems = new ArrayList<>();
    }

    @NonNull
    @Override
    public InventoryViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View view = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_inventory, parent, false);
        return new InventoryViewHolder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull InventoryViewHolder holder, int position) {
        InventoryItem item = filteredItems.get(position);
        holder.bind(item);
    }

    @Override
    public int getItemCount() {
        return filteredItems.size();
    }

    /**
     * Обновляет список элементов в адаптере
     */
    public void updateItems(List<InventoryItem> items) {
        this.items = items;
        this.filteredItems = new ArrayList<>(items);
        notifyDataSetChanged();
    }

    /**
     * Фильтрует список товаров по поисковому запросу
     */
    public void filter(String query) {
        filteredItems.clear();

        if (query.isEmpty()) {
            filteredItems.addAll(items);
        } else {
            String lowerCaseQuery = query.toLowerCase(Locale.getDefault());

            for (InventoryItem item : items) {
                if ((item.getProductName() != null && item.getProductName().toLowerCase(Locale.getDefault()).contains(lowerCaseQuery)) ||
                    (item.getCategory() != null && item.getCategory().toLowerCase(Locale.getDefault()).contains(lowerCaseQuery)) ||
                    (item.getWarehouseName() != null && item.getWarehouseName().toLowerCase(Locale.getDefault()).contains(lowerCaseQuery)) ||
                    (item.getSupplierName() != null && item.getSupplierName().toLowerCase(Locale.getDefault()).contains(lowerCaseQuery))) {
                    filteredItems.add(item);
                }
            }
        }

        notifyDataSetChanged();
    }

    /**
     * Получает элемент по позиции
     */
    public InventoryItem getItemAt(int position) {
        if (position >= 0 && position < filteredItems.size()) {
            return filteredItems.get(position);
        }
        return null;
    }

    /**
     * Устанавливает слушатель для клика по элементу
     */
    public void setOnInventoryItemClickListener(OnInventoryItemClickListener listener) {
        this.listener = listener;
    }

    /**
     * Устанавливает выбранный элемент
     */
    public void setSelectedItem(InventoryItem item) {
        this.selectedItem = item;
        notifyDataSetChanged();
    }

    /**
     * Получает цвет для индикатора категории
     */
    private int getCategoryColor(String category) {
        if (category == null || category.isEmpty()) {
            return Color.GRAY;
        }
        
        // Создаем цвет на основе хеша категории
        int hash = category.hashCode();
        int r = (hash & 0xFF0000) >> 16;
        int g = (hash & 0x00FF00) >> 8;
        int b = hash & 0x0000FF;
        
        // Убедимся, что цвет не слишком темный или светлый
        r = Math.max(r, 100);
        g = Math.max(g, 100);
        b = Math.max(b, 100);
        
        return Color.rgb(r, g, b);
    }

    /**
     * Интерфейс для обработки кликов
     */
    public interface OnInventoryItemClickListener {
        void onInventoryItemClick(InventoryItem item, int position);
    }

    /**
     * ViewHolder для элемента списка
     */
    public class InventoryViewHolder extends RecyclerView.ViewHolder {
        private View categoryIndicator;
        private TextView productNameTextView;
        private TextView categoryTextView;
        private TextView supplierTextView;
        private TextView quantityTextView;
        private TextView priceTextView;
        private TextView warehouseTextView;

        public InventoryViewHolder(@NonNull View itemView) {
            super(itemView);
            categoryIndicator = itemView.findViewById(R.id.categoryIndicator);
            productNameTextView = itemView.findViewById(R.id.productNameTextView);
            categoryTextView = itemView.findViewById(R.id.categoryTextView);
            supplierTextView = itemView.findViewById(R.id.supplierTextView);
            quantityTextView = itemView.findViewById(R.id.quantityTextView);
            priceTextView = itemView.findViewById(R.id.priceTextView);
            warehouseTextView = itemView.findViewById(R.id.warehouseTextView);

            itemView.setOnClickListener(v -> {
                int position = getAdapterPosition();
                if (position != RecyclerView.NO_POSITION && listener != null) {
                    listener.onInventoryItemClick(filteredItems.get(position), position);
                }
            });
        }

        public void bind(InventoryItem item) {
            productNameTextView.setText(item.getProductName());
            categoryTextView.setText(item.getCategory());
            supplierTextView.setText(item.getSupplierName());
            quantityTextView.setText(String.format(Locale.getDefault(), "%d шт.", item.getQuantity()));
            priceTextView.setText(String.format(Locale.getDefault(), "%.2f ₽", item.getPrice()));
            warehouseTextView.setText(item.getWarehouseName());
            
            // Устанавливаем цвет индикатора категории
            categoryIndicator.setBackgroundColor(getCategoryColor(item.getCategory()));
            
            // Выделяем элемент, если он выбран
            itemView.setSelected(item.equals(selectedItem));
            
            // Если товар отсутствует на складе, выделяем это
            if (item.getQuantity() <= 0) {
                quantityTextView.setTextColor(Color.RED);
            } else {
                quantityTextView.setTextColor(Color.BLACK);
            }
        }
    }
} 
 
<<<<<<< HEAD
=======
 
 
>>>>>>> 1dea6d5621f4f889dffd0814aeeb98a9d2d0ba87
 