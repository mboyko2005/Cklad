package com.example.apk.data;

import android.app.Application;
import android.os.AsyncTask;

import androidx.lifecycle.LiveData;

import com.example.apk.api.ApiClient;
import com.example.apk.api.MessageResponse;
import com.example.apk.api.MessagesResponse;
import com.example.apk.api.UserResponse;
import com.example.apk.models.ChatItem;
import com.example.apk.utils.SessionManager;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

public class ChatRepository {
    private final ChatDao chatDao;
    private final LiveData<List<ChatItem>> allChats;
    private final ExecutorService executor;
    private final SessionManager sessionManager;
    private final Application application;

    public ChatRepository(Application application) {
        this.application = application;
        AppDatabase db = AppDatabase.getDatabase(application);
        chatDao = db.chatDao();
        allChats = chatDao.getAllChats();
        executor = Executors.newSingleThreadExecutor();
        sessionManager = new SessionManager(application);
    }

    // Получение всех чатов из кэша
    public LiveData<List<ChatItem>> getAllChats() {
        return allChats;
    }

    // Обновление чатов с сервера
    public void refreshChats(int userId, OnChatRefreshListener listener) {
        executor.execute(() -> {
            // Запускаем загрузку чатов с сервера
            loadChatsFromServer(userId, listener);
        });
    }

    // Проверка наличия новых чатов
    public void checkForNewChats(int userId, OnChatCheckListener listener) {
        executor.execute(() -> {
            // Получаем список чатов из локальной базы данных
            List<ChatItem> cachedChats = chatDao.getAllChatsSync();
            
            // Если локальный кэш пуст, загружаем с сервера
            if (cachedChats.isEmpty()) {
                loadChatsFromServer(userId, new OnChatRefreshListener() {
                    @Override
                    public void onChatsRefreshing() {
                        listener.onCheckStarted();
                    }

                    @Override
                    public void onChatsRefreshed(List<ChatItem> chats) {
                        listener.onCheckCompleted(true, chats.size());
                    }

                    @Override
                    public void onChatRefreshFailed(String error) {
                        listener.onCheckFailed(error);
                    }
                });
                return;
            }
            
            // Запускаем проверку на сервере
            listener.onCheckStarted();
            
            String token = "Bearer " + sessionManager.getToken();
            
            // Получаем список пользователей с сервера
            ApiClient.getApiService().getUsers(token).enqueue(new Callback<List<UserResponse>>() {
                @Override
                public void onResponse(Call<List<UserResponse>> call, Response<List<UserResponse>> response) {
                    if (response.isSuccessful() && response.body() != null) {
                        // Создаем карту userId -> username
                        Map<Integer, String> userMap = new LinkedHashMap<>();
                        for (UserResponse u : response.body()) {
                            userMap.put(u.getUserId(), u.getUsername());
                        }

                        // Получаем сообщения для текущего пользователя
                        ApiClient.getApiService().getMessages(userId).enqueue(new Callback<MessagesResponse>() {
                            @Override
                            public void onResponse(Call<MessagesResponse> call, Response<MessagesResponse> response) {
                                if (response.isSuccessful() && response.body() != null && response.body().isSuccess()) {
                                    List<MessageResponse> messages = response.body().getMessages();

                                    // Группируем сообщения по собеседнику и берем первые (последние по дате)
                                    Map<Integer, MessageResponse> latestMap = new LinkedHashMap<>();
                                    for (MessageResponse msg : messages) {
                                        int partnerId = msg.getSenderId() == userId ? msg.getReceiverId() : msg.getSenderId();
                                        if (!latestMap.containsKey(partnerId) || 
                                            msg.getTimestamp().compareTo(latestMap.get(partnerId).getTimestamp()) > 0) {
                                            latestMap.put(partnerId, msg);
                                        }
                                    }

                                    // Формируем список ChatItem
                                    List<ChatItem> serverChats = new ArrayList<>();
                                    for (Map.Entry<Integer, MessageResponse> entry : latestMap.entrySet()) {
                                        int partnerId = entry.getKey();
                                        MessageResponse msg = entry.getValue();
                                        String chatName = userMap.containsKey(partnerId) ? 
                                            userMap.get(partnerId) : String.valueOf(partnerId);
                                        
                                        ChatItem chat = new ChatItem(
                                            String.valueOf(partnerId),
                                            chatName,
                                            msg.getText()
                                        );
                                        // Устанавливаем timestamp из сообщения
                                        long timestamp = parseTimestamp(msg.getTimestamp());
                                        chat.setLastMessageTimestamp(timestamp);
                                        
                                        // Подсчитываем количество непрочитанных сообщений
                                        int unreadCount = 0;
                                        for (MessageResponse message : messages) {
                                            if (message.getSenderId() == partnerId && 
                                                message.getReceiverId() == userId && 
                                                !message.isRead()) {
                                                unreadCount++;
                                            }
                                        }
                                        chat.setUnreadCount(unreadCount);
                                        
                                        serverChats.add(chat);
                                    }

                                    // Сравниваем с локальным кэшем
                                    executor.execute(() -> {
                                        boolean hasNewChats = serverChats.size() > cachedChats.size();
                                        boolean hasChanges = false;
                                        
                                        // Проверяем изменения в существующих чатах
                                        for (ChatItem serverChat : serverChats) {
                                            String chatId = serverChat.getChatId();
                                            ChatItem cachedChat = null;
                                            
                                            // Ищем чат в кэше
                                            for (ChatItem chat : cachedChats) {
                                                if (chat.getChatId().equals(chatId)) {
                                                    cachedChat = chat;
                                                    break;
                                                }
                                            }
                                            
                                            // Если чат новый или сообщение изменилось
                                            if (cachedChat == null) {
                                                serverChat.setNew(true);
                                                hasChanges = true;
                                            } else if (!cachedChat.getLastMessage().equals(serverChat.getLastMessage())) {
                                                // Обновляем только последнее сообщение
                                                cachedChat.setLastMessage(serverChat.getLastMessage());
                                                cachedChat.setLastMessageTimestamp(serverChat.getLastMessageTimestamp());
                                                chatDao.updateChat(cachedChat);
                                                hasChanges = true;
                                            }
                                        }
                                        
                                        // Если есть новые чаты, обновляем весь список
                                        if (hasNewChats) {
                                            chatDao.deleteAllChats();
                                            chatDao.insertChats(serverChats);
                                        }
                                        
                                        // Отправляем результат
                                        listener.onCheckCompleted(hasChanges, serverChats.size());
                                    });
                                } else {
                                    listener.onCheckFailed("Ошибка получения сообщений");
                                }
                            }

                            @Override
                            public void onFailure(Call<MessagesResponse> call, Throwable t) {
                                listener.onCheckFailed(t.getMessage());
                            }
                        });
                    } else {
                        listener.onCheckFailed("Ошибка получения пользователей");
                    }
                }

                @Override
                public void onFailure(Call<List<UserResponse>> call, Throwable t) {
                    listener.onCheckFailed(t.getMessage());
                }
            });
        });
    }

    // Загрузка чатов с сервера
    private void loadChatsFromServer(int userId, OnChatRefreshListener listener) {
        if (listener != null) {
            listener.onChatsRefreshing();
        }
        
        String token = "Bearer " + sessionManager.getToken();
        
        // Получаем список пользователей с сервера
        ApiClient.getApiService().getUsers(token).enqueue(new Callback<List<UserResponse>>() {
            @Override
            public void onResponse(Call<List<UserResponse>> call, Response<List<UserResponse>> response) {
                if (response.isSuccessful() && response.body() != null) {
                    // Создаем карту userId -> username
                    Map<Integer, String> userMap = new LinkedHashMap<>();
                    for (UserResponse u : response.body()) {
                        userMap.put(u.getUserId(), u.getUsername());
                    }

                    // Получаем сообщения для текущего пользователя
                    ApiClient.getApiService().getMessages(userId).enqueue(new Callback<MessagesResponse>() {
                        @Override
                        public void onResponse(Call<MessagesResponse> call, Response<MessagesResponse> response) {
                            if (response.isSuccessful() && response.body() != null && response.body().isSuccess()) {
                                List<MessageResponse> messages = response.body().getMessages();

                                // Группируем сообщения по собеседнику и берем первые (последние по дате)
                                Map<Integer, MessageResponse> latestMap = new LinkedHashMap<>();
                                for (MessageResponse msg : messages) {
                                    int partnerId = msg.getSenderId() == userId ? msg.getReceiverId() : msg.getSenderId();
                                    if (!latestMap.containsKey(partnerId) || 
                                        msg.getTimestamp().compareTo(latestMap.get(partnerId).getTimestamp()) > 0) {
                                        latestMap.put(partnerId, msg);
                                    }
                                }

                                // Формируем список ChatItem
                                List<ChatItem> chatItems = new ArrayList<>();
                                for (Map.Entry<Integer, MessageResponse> entry : latestMap.entrySet()) {
                                    int partnerId = entry.getKey();
                                    MessageResponse msg = entry.getValue();
                                    String chatName = userMap.containsKey(partnerId) ? 
                                        userMap.get(partnerId) : String.valueOf(partnerId);
                                    
                                    ChatItem chat = new ChatItem(
                                        String.valueOf(partnerId),
                                        chatName,
                                        msg.getText()
                                    );
                                    // Устанавливаем timestamp из сообщения
                                    long timestamp = parseTimestamp(msg.getTimestamp());
                                    chat.setLastMessageTimestamp(timestamp);
                                    
                                    // Подсчитываем количество непрочитанных сообщений
                                    int unreadCount = 0;
                                    for (MessageResponse message : messages) {
                                        if (message.getSenderId() == partnerId && 
                                            message.getReceiverId() == userId && 
                                            !message.isRead()) {
                                            unreadCount++;
                                        }
                                    }
                                    chat.setUnreadCount(unreadCount);
                                    
                                    chatItems.add(chat);
                                }

                                // Сохраняем в локальную базу данных
                                executor.execute(() -> {
                                    chatDao.deleteAllChats();
                                    chatDao.insertChats(chatItems);
                                    
                                    if (listener != null) {
                                        listener.onChatsRefreshed(chatItems);
                                    }
                                });
                            } else {
                                if (listener != null) {
                                    listener.onChatRefreshFailed("Ошибка получения сообщений");
                                }
                            }
                        }

                        @Override
                        public void onFailure(Call<MessagesResponse> call, Throwable t) {
                            if (listener != null) {
                                listener.onChatRefreshFailed(t.getMessage());
                            }
                        }
                    });
                } else {
                    if (listener != null) {
                        listener.onChatRefreshFailed("Ошибка получения пользователей");
                    }
                }
            }

            @Override
            public void onFailure(Call<List<UserResponse>> call, Throwable t) {
                if (listener != null) {
                    listener.onChatRefreshFailed(t.getMessage());
                }
            }
        });
    }
    
    // Преобразование строки времени в миллисекунды
    private long parseTimestamp(String timestamp) {
        try {
            // Пример: 2023-04-25T14:32:15
            String[] parts = timestamp.split("T");
            String[] dateParts = parts[0].split("-");
            String[] timeParts = parts[1].split(":");
            
            int year = Integer.parseInt(dateParts[0]);
            int month = Integer.parseInt(dateParts[1]) - 1; // месяцы с 0
            int day = Integer.parseInt(dateParts[2]);
            
            int hour = Integer.parseInt(timeParts[0]);
            int minute = Integer.parseInt(timeParts[1]);
            int second = Integer.parseInt(timeParts[2]);
            
            java.util.Calendar calendar = java.util.Calendar.getInstance();
            calendar.set(year, month, day, hour, minute, second);
            
            return calendar.getTimeInMillis();
        } catch (Exception e) {
            return System.currentTimeMillis();
        }
    }
    
    // Интерфейс для обратных вызовов при обновлении чатов
    public interface OnChatRefreshListener {
        void onChatsRefreshing();
        void onChatsRefreshed(List<ChatItem> chats);
        void onChatRefreshFailed(String error);
    }
    
    // Интерфейс для обратных вызовов при проверке новых чатов
    public interface OnChatCheckListener {
        void onCheckStarted();
        void onCheckCompleted(boolean hasChanges, int totalChats);
        void onCheckFailed(String error);
    }
} 