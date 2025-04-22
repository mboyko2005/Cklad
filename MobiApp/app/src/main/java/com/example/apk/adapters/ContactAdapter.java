package com.example.apk.adapters;

import android.content.Context;
import android.graphics.drawable.AnimationDrawable;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageView;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.example.apk.R;
import com.example.apk.models.UserData;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.Locale;
import java.util.Random;

/**
 * Адаптер для отображения списка контактов в стиле Telegram
 */
public class ContactAdapter extends RecyclerView.Adapter<ContactAdapter.ContactViewHolder> {

    // Константы для типов сортировки
    public static final int SORT_BY_NAME = 1;
    public static final int SORT_BY_ROLE = 2;
    
    private final Context context;
    private List<UserData> contacts;
    private List<UserData> filteredContacts;
    private OnContactClickListener listener;
    private int currentSortType = SORT_BY_NAME; // По умолчанию сортировка по имени

    // Массив цветов для аватаров (в стиле Telegram)
    private final int[] avatarColors = new int[] {
        R.color.telegram_blue,
        R.color.telegram_red,
        R.color.telegram_green,
        R.color.telegram_orange,
        R.color.telegram_pink,
        R.color.telegram_purple
    };
    
    private final Random random = new Random();

    public ContactAdapter(Context context, List<UserData> contacts) {
        this.context = context;
        this.contacts = new ArrayList<>(contacts);
        this.filteredContacts = new ArrayList<>(contacts);
    }

    @NonNull
    @Override
    public ContactViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View view = LayoutInflater.from(context).inflate(R.layout.item_contact, parent, false);
        return new ContactViewHolder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull ContactViewHolder holder, int position) {
        UserData contact = filteredContacts.get(position);
        
        // Установка имени пользователя и его роли
        holder.usernameTextView.setText(contact.getUsername());
        holder.roleTextView.setText(contact.getRoleName());
        
        // Установка первой буквы имени в аватар
        String username = contact.getUsername();
        if (username != null && !username.isEmpty()) {
            holder.avatarTextView.setText(username.substring(0, 1).toUpperCase());
        } else {
            holder.avatarTextView.setText("?");
        }
        
        // Улучшенная обработка анимации градиента аватара
        if (holder.avatarBackground != null) {
            // Проверяем, запущена ли уже анимация
            if (holder.avatarBackground.getBackground() instanceof AnimationDrawable) {
                // Создаем final копию для использования в лямбда-выражении
                final AnimationDrawable animDrawable = (AnimationDrawable) holder.avatarBackground.getBackground();
                
                // Запускаем анимацию только если она не запущена
                if (!animDrawable.isRunning()) {
                    // Добавляем небольшую задержку для создания эффекта смещения анимации 
                    // между разными аватарами
                    holder.avatarBackground.postDelayed(() -> {
                        animDrawable.setEnterFadeDuration(1000);
                        animDrawable.setExitFadeDuration(1000);
                        animDrawable.start();
                    }, position * 100 % 500); // Разные задержки для разных позиций
                }
            }
        }
        
        // Обработка клика на элемент
        holder.itemView.setOnClickListener(v -> {
            if (listener != null) {
                listener.onContactClick(contact, position);
            }
        });
    }

    @Override
    public int getItemCount() {
        return filteredContacts.size();
    }
    
    /**
     * Обновление списка контактов с указанием типа сортировки
     */
    public void updateContacts(List<UserData> contacts, int sortType) {
        this.contacts = new ArrayList<>(contacts);
        this.currentSortType = sortType;
        sortContacts();
        this.filteredContacts = new ArrayList<>(this.contacts);
        notifyDataSetChanged();
    }
    
    /**
     * Обновление списка контактов (сохраняет текущую сортировку)
     */
    public void updateContacts(List<UserData> contacts) {
        updateContacts(contacts, currentSortType);
    }
    
    /**
     * Применение сортировки к списку контактов
     */
    public void sortContacts() {
        if (currentSortType == SORT_BY_NAME) {
            // Сортировка по имени (по алфавиту)
            Collections.sort(contacts, (o1, o2) -> 
                o1.getUsername().compareToIgnoreCase(o2.getUsername()));
        } else if (currentSortType == SORT_BY_ROLE) {
            // Сортировка по роли
            Collections.sort(contacts, (o1, o2) -> {
                int roleCompare = o1.getRoleName().compareToIgnoreCase(o2.getRoleName());
                if (roleCompare == 0) {
                    // Если роли одинаковые, сортируем по имени
                    return o1.getUsername().compareToIgnoreCase(o2.getUsername());
                }
                return roleCompare;
            });
        }
    }
    
    /**
     * Изменение типа сортировки
     */
    public void setSortType(int sortType) {
        if (this.currentSortType != sortType) {
            this.currentSortType = sortType;
            sortContacts();
            
            // Переприменяем фильтр, если он активен
            String currentFilter = "";
            if (filteredContacts.size() != contacts.size()) {
                // Есть активный фильтр
                if (filteredContacts.size() > 0) {
                    currentFilter = filteredContacts.get(0).getUsername().toLowerCase(Locale.getDefault());
                }
            }
            
            filteredContacts = new ArrayList<>(contacts);
            if (!currentFilter.isEmpty()) {
                filter(currentFilter);
            }
            
            notifyDataSetChanged();
        }
    }
    
    /**
     * Получение текущего типа сортировки
     */
    public int getCurrentSortType() {
        return currentSortType;
    }

    /**
     * Фильтрация контактов по запросу
     */
    public void filter(String query) {
        filteredContacts.clear();

        if (query.isEmpty()) {
            filteredContacts.addAll(contacts);
        } else {
            String lowerCaseQuery = query.toLowerCase(Locale.getDefault());

            for (UserData contact : contacts) {
                if (contact.getUsername().toLowerCase(Locale.getDefault()).contains(lowerCaseQuery) ||
                        contact.getRoleName().toLowerCase(Locale.getDefault()).contains(lowerCaseQuery)) {
                    filteredContacts.add(contact);
                }
            }
        }

        notifyDataSetChanged();
    }

    /**
     * Установка слушателя кликов
     */
    public void setOnContactClickListener(OnContactClickListener listener) {
        this.listener = listener;
    }

    /**
     * Интерфейс для обратного вызова при клике на контакт
     */
    public interface OnContactClickListener {
        void onContactClick(UserData contact, int position);
    }

    /**
     * ViewHolder для элемента контакта
     */
    public static class ContactViewHolder extends RecyclerView.ViewHolder {
        TextView usernameTextView;
        TextView roleTextView;
        TextView avatarTextView;
        ImageView avatarBackground;

        public ContactViewHolder(@NonNull View itemView) {
            super(itemView);
            usernameTextView = itemView.findViewById(R.id.contactName);
            roleTextView = itemView.findViewById(R.id.contactRole);
            avatarTextView = itemView.findViewById(R.id.avatarText);
            avatarBackground = itemView.findViewById(R.id.avatarBackground);
        }
    }
} 