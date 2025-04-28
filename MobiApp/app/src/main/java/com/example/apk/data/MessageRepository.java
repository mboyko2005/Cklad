package com.example.apk.data;

import android.app.Application;

import androidx.lifecycle.LiveData;

import com.example.apk.api.ApiClient;
import com.example.apk.api.MessageResponse;
import com.example.apk.api.MessagesResponse;
import com.example.apk.api.SendMessageRequest;
import com.example.apk.api.SendMessageResponse;
import com.example.apk.models.Message;
import com.example.apk.utils.SessionManager;

import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.Locale;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

public class MessageRepository {
    private final MessageDao messageDao;
    private final ExecutorService executor;
    private final SessionManager sessionManager;
    private final Application application;
    private final SimpleDateFormat dateFormat;

    public MessageRepository(Application application) {
        this.application = application;
        AppDatabase db = AppDatabase.getDatabase(application);
        messageDao = db.messageDao();
        executor = Executors.newSingleThreadExecutor();
        sessionManager = new SessionManager(application);
        dateFormat = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'", Locale.US);
    }

    // Получение сообщений для конкретного чата
    public LiveData<List<Message>> getMessagesByChatId(String chatId) {
        return messageDao.getMessagesByChatId(chatId);
    }

    // Получение количества непрочитанных сообщений
    public LiveData<Integer> getUnreadMessagesCount(int userId) {
        return messageDao.getUnreadMessagesCount(userId);
    }

    // Отметка сообщений как прочитанных
    public void markMessagesAsRead(String chatId, int currentUserId) {
        executor.execute(() -> {
            messageDao.markMessagesAsRead(chatId, currentUserId);
        });
    }

    // Отправка нового сообщения
    public void sendMessage(int senderId, int receiverId, String text, OnMessageSentListener listener) {
        String token = "Bearer " + sessionManager.getToken();
        
        SendMessageRequest request = new SendMessageRequest(senderId, receiverId, text);
        
        ApiClient.getApiService().sendMessage(request).enqueue(new Callback<SendMessageResponse>() {
            @Override
            public void onResponse(Call<SendMessageResponse> call, Response<SendMessageResponse> response) {
                if (response.isSuccessful() && response.body() != null && response.body().isSuccess()) {
                    MessageResponse messageResponse = response.body().getMessage();
                    Message message = convertToMessage(messageResponse, String.valueOf(receiverId));
                    
                    // Сохраняем сообщение в базу данных
                    executor.execute(() -> {
                        messageDao.insertMessage(message);
                        if (listener != null) {
                            listener.onMessageSent(message);
                        }
                    });
                } else {
                    if (listener != null) {
                        listener.onMessageSendFailed("Ошибка отправки сообщения");
                    }
                }
            }

            @Override
            public void onFailure(Call<SendMessageResponse> call, Throwable t) {
                if (listener != null) {
                    listener.onMessageSendFailed("Ошибка сети: " + t.getMessage());
                }
            }
        });
    }

    // Загрузка сообщений для конкретного чата с сервера
    public void refreshMessages(String chatId, int userId, OnMessageRefreshListener listener) {
        if (listener != null) {
            listener.onMessagesRefreshing();
        }
        
        int receiverId = Integer.parseInt(chatId);
        
        ApiClient.getApiService().getMessages(userId).enqueue(new Callback<MessagesResponse>() {
            @Override
            public void onResponse(Call<MessagesResponse> call, Response<MessagesResponse> response) {
                if (response.isSuccessful() && response.body() != null && response.body().isSuccess()) {
                    List<MessageResponse> allMessages = response.body().getMessages();
                    List<Message> chatMessages = new ArrayList<>();
                    
                    // Фильтруем сообщения только для выбранного чата
                    for (MessageResponse msgResponse : allMessages) {
                        if ((msgResponse.getSenderId() == userId && msgResponse.getReceiverId() == receiverId) ||
                            (msgResponse.getSenderId() == receiverId && msgResponse.getReceiverId() == userId)) {
                            chatMessages.add(convertToMessage(msgResponse, chatId));
                        }
                    }
                    
                    // Сохраняем сообщения в базу данных
                    executor.execute(() -> {
                        // Удаляем только если есть новые сообщения
                        if (!chatMessages.isEmpty()) {
                            messageDao.insertMessages(chatMessages);
                        }
                        
                        if (listener != null) {
                            listener.onMessagesRefreshed(chatMessages);
                        }
                    });
                } else {
                    if (listener != null) {
                        listener.onMessageRefreshFailed("Ошибка получения сообщений");
                    }
                }
            }

            @Override
            public void onFailure(Call<MessagesResponse> call, Throwable t) {
                if (listener != null) {
                    listener.onMessageRefreshFailed("Ошибка сети: " + t.getMessage());
                }
            }
        });
    }

    // Конвертация MessageResponse в Message
    private Message convertToMessage(MessageResponse response, String chatId) {
        return new Message(
            response.getMessageId(),
            response.getSenderId(),
            response.getReceiverId(),
            response.getText(),
            response.getTimestamp(),
            response.isRead(),
            response.hasAttachment(),
            chatId
        );
    }

    // Парсинг строки времени в long
    private long parseTimestamp(String timestamp) {
        try {
            Date date = dateFormat.parse(timestamp);
            return date != null ? date.getTime() : System.currentTimeMillis();
        } catch (ParseException e) {
            return System.currentTimeMillis();
        }
    }

    // Интерфейс слушателя для обновления сообщений
    public interface OnMessageRefreshListener {
        void onMessagesRefreshing();
        void onMessagesRefreshed(List<Message> messages);
        void onMessageRefreshFailed(String errorMessage);
    }

    // Интерфейс слушателя для отправки сообщений
    public interface OnMessageSentListener {
        void onMessageSent(Message message);
        void onMessageSendFailed(String errorMessage);
    }
} 