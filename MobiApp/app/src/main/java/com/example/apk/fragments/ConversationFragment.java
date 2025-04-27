package com.example.apk.fragments;

import android.Manifest;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.os.Environment;
import android.provider.Settings;
import android.text.Editable;
import android.text.TextWatcher;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageView;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.appcompat.widget.Toolbar;
import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;
import androidx.fragment.app.Fragment;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.example.apk.R;
import com.example.apk.api.ApiClient;
import com.example.apk.api.MessagesResponse;
import com.example.apk.api.MessageResponse;
import com.example.apk.adapters.ConversationAdapter;
import com.example.apk.api.SendMessageRequest;
import com.example.apk.api.SendMessageResponse;
import com.example.apk.utils.SessionManager;
import com.google.android.material.textfield.TextInputEditText;

import java.util.List;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

/**
 * Фрагмент для отображения переписки между пользователями
 */
public class ConversationFragment extends Fragment {
    private static final String ARG_PARTNER_ID = "partnerId";
    private static final String ARG_CHAT_NAME = "chatName";
    private static final int PERMISSION_REQUEST_CODE = 1001;
    private static final int MANAGE_STORAGE_PERMISSION_REQUEST = 1002;

    private int partnerId;
    private String chatName;
    private int userId;

    private SessionManager sessionManager;
    private RecyclerView recyclerView;
    private ConversationAdapter adapter;

    public static ConversationFragment newInstance(int partnerId, String chatName) {
        ConversationFragment fragment = new ConversationFragment();
        Bundle args = new Bundle();
        args.putInt(ARG_PARTNER_ID, partnerId);
        args.putString(ARG_CHAT_NAME, chatName);
        fragment.setArguments(args);
        return fragment;
    }

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (getArguments() != null) {
            partnerId = getArguments().getInt(ARG_PARTNER_ID);
            chatName = getArguments().getString(ARG_CHAT_NAME);
        }
        sessionManager = new SessionManager(requireContext());
        SharedPreferences prefs = requireContext()
                .getSharedPreferences("UserPrefs", Context.MODE_PRIVATE);
        userId = prefs.getInt("userId", -1);
        
        // Запрашиваем разрешения на работу с файлами
        checkStoragePermissions();
    }

    // Проверка и запрос разрешений для работы с хранилищем
    private void checkStoragePermissions() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
            // Android 11 (API 30) и выше
            if (!Environment.isExternalStorageManager()) {
                try {
                    Intent intent = new Intent(Settings.ACTION_MANAGE_APP_ALL_FILES_ACCESS_PERMISSION);
                    Uri uri = Uri.fromParts("package", requireActivity().getPackageName(), null);
                    intent.setData(uri);
                    startActivityForResult(intent, MANAGE_STORAGE_PERMISSION_REQUEST);
                } catch (Exception e) {
                    Intent intent = new Intent();
                    intent.setAction(Settings.ACTION_MANAGE_ALL_FILES_ACCESS_PERMISSION);
                    startActivityForResult(intent, MANAGE_STORAGE_PERMISSION_REQUEST);
                }
            }
        } else {
            // Android 10 (API 29) и ниже
            int writePermission = ContextCompat.checkSelfPermission(requireContext(), 
                    Manifest.permission.WRITE_EXTERNAL_STORAGE);
            int readPermission = ContextCompat.checkSelfPermission(requireContext(),
                    Manifest.permission.READ_EXTERNAL_STORAGE);
            
            if (writePermission != PackageManager.PERMISSION_GRANTED || 
                readPermission != PackageManager.PERMISSION_GRANTED) {
                ActivityCompat.requestPermissions(requireActivity(),
                    new String[]{
                        Manifest.permission.WRITE_EXTERNAL_STORAGE,
                        Manifest.permission.READ_EXTERNAL_STORAGE
                    }, PERMISSION_REQUEST_CODE);
            }
        }
    }

    @Override
    public void onRequestPermissionsResult(int requestCode, @NonNull String[] permissions, @NonNull int[] grantResults) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults);
        if (requestCode == PERMISSION_REQUEST_CODE) {
            if (grantResults.length > 0 && grantResults[0] == PackageManager.PERMISSION_GRANTED) {
                // Разрешения получены
                Toast.makeText(requireContext(), "Разрешения на работу с файлами получены", Toast.LENGTH_SHORT).show();
            } else {
                // Разрешения не получены
                Toast.makeText(requireContext(), "Для скачивания файлов необходимы разрешения", Toast.LENGTH_LONG).show();
            }
        }
    }

    @Override
    public void onActivityResult(int requestCode, int resultCode, @Nullable Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode == MANAGE_STORAGE_PERMISSION_REQUEST) {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
                if (Environment.isExternalStorageManager()) {
                    // Разрешение получено
                    Toast.makeText(requireContext(), "Разрешения на работу с файлами получены", Toast.LENGTH_SHORT).show();
                } else {
                    // Разрешение не получено
                    Toast.makeText(requireContext(), "Для скачивания файлов необходимы разрешения", Toast.LENGTH_LONG).show();
                }
            }
        }
    }

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater,
                             @Nullable ViewGroup container,
                             @Nullable Bundle savedInstanceState) {
        return inflater.inflate(R.layout.fragment_conversation, container, false);
    }

    @Override
    public void onViewCreated(@NonNull View view,
                              @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);

        // Настраиваем кнопку назад через navigationIcon
        Toolbar toolbar = view.findViewById(R.id.toolbar);
        toolbar.setNavigationOnClickListener(v -> requireActivity().onBackPressed());

        // Устанавливаем заголовок и подзаголовок
        TextView titleView = view.findViewById(R.id.toolbar_title);
        TextView subtitleView = view.findViewById(R.id.toolbar_subtitle);
        titleView.setText(chatName);
        subtitleView.setText("был(а) недавно");

        // Устанавливаем первую букву в аватар
        TextView avatarText = view.findViewById(R.id.avatarText);
        String first = (chatName != null && !chatName.isEmpty())
                ? chatName.substring(0, 1).toUpperCase()
                : "?";
        avatarText.setText(first);

        recyclerView = view.findViewById(R.id.recycler_view_messages);
        recyclerView.setLayoutManager(new LinearLayoutManager(requireContext()));
        adapter = new ConversationAdapter(userId);
        recyclerView.setAdapter(adapter);

        loadConversation();

        // Обработчик кнопки отправки
        ImageView sendButton = view.findViewById(R.id.send_button);
        TextInputEditText messageInput = view.findViewById(R.id.message_input);

        // Показ/скрытие send_button в зависимости от текста
        messageInput.addTextChangedListener(new TextWatcher() {
            @Override public void beforeTextChanged(CharSequence s, int start, int count, int after) {}
            @Override public void onTextChanged(CharSequence s, int start, int before, int count) {
                sendButton.setVisibility(s.length() > 0 ? View.VISIBLE : View.GONE);
            }
            @Override public void afterTextChanged(Editable s) {}
        });

        sendButton.setOnClickListener(v -> {
            String text = messageInput.getText().toString().trim();
            if (text.isEmpty()) return;
            // Очищаем поле ввода
            messageInput.setText("");
            // Формируем запрос
            SendMessageRequest req = new SendMessageRequest(userId, partnerId, text);
            ApiClient.getApiService().sendMessage(req)
                .enqueue(new Callback<SendMessageResponse>() {
                    @Override
                    public void onResponse(Call<SendMessageResponse> call,
                                           Response<SendMessageResponse> response) {
                        if (response.isSuccessful() && response.body() != null && response.body().isSuccess()) {
                            MessageResponse newMsg = response.body().getMessage();
                            // Добавляем сообщение в adapter и скроллим
                            List<MessageResponse> current = adapter.getMessages();
                            current.add(newMsg);
                            adapter.setMessages(current);
                            recyclerView.scrollToPosition(current.size() - 1);
                        }
                    }

                    @Override
                    public void onFailure(Call<SendMessageResponse> call, Throwable t) {
                        // TODO: показать ошибку
                    }
                });
        });
    }

    /**
     * Загружает переписку между пользователями
     */
    private void loadConversation() {
        ApiClient.getApiService()
                .getConversation(userId, partnerId)
                .enqueue(new Callback<MessagesResponse>() {
                    @Override
                    public void onResponse(Call<MessagesResponse> call,
                                           Response<MessagesResponse> response) {
                        if (response.isSuccessful() && response.body() != null
                                && response.body().isSuccess()) {
                            List<MessageResponse> msgs = response.body().getMessages();
                            adapter.setMessages(msgs);
                            // Прокрутка в конец
                            if (!msgs.isEmpty()) {
                                recyclerView.scrollToPosition(msgs.size() - 1);
                            }
                        }
                    }

                    @Override
                    public void onFailure(Call<MessagesResponse> call, Throwable t) {
                        // TODO: обработка ошибки
                    }
                });
    }
} 