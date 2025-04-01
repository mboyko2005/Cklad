package com.example.apk.fragments;

import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.fragment.app.Fragment;

import com.example.apk.R;

/**
 * Фрагмент для отображения чатов
 */
public class ChatsFragment extends Fragment {

    public ChatsFragment() {
        // Пустой конструктор
    }

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container, 
                             @Nullable Bundle savedInstanceState) {
        // Загружаем макет фрагмента
        return inflater.inflate(R.layout.fragment_chats, container, false);
    }
} 