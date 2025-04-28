package com.example.apk.viewmodels;

import android.app.Application;

import androidx.lifecycle.AndroidViewModel;
import androidx.lifecycle.LiveData;
import androidx.lifecycle.MutableLiveData;

import com.example.apk.data.MessageRepository;
import com.example.apk.models.Message;

import java.util.List;

public class MessageViewModel extends AndroidViewModel {
    private final MessageRepository repository;
    private LiveData<List<Message>> chatMessages;
    private String currentChatId;
    
    // LiveData для отслеживания состояния загрузки
    private final MutableLiveData<Boolean> isRefreshing = new MutableLiveData<>(false);
    private final MutableLiveData<String> errorMessage = new MutableLiveData<>();
    private final MutableLiveData<Message> sentMessage = new MutableLiveData<>();
    
    // Флаг для отслеживания, выполняется ли в данный момент обновление
    private boolean isPerformingRefresh = false;
    
    public MessageViewModel(Application application) {
        super(application);
        repository = new MessageRepository(application);
    }
    
    // Установка текущего чата
    public void setChatId(String chatId) {
        this.currentChatId = chatId;
        this.chatMessages = repository.getMessagesByChatId(chatId);
    }
    
    // Получение сообщений для текущего чата
    public LiveData<List<Message>> getChatMessages() {
        return chatMessages;
    }
    
    // Получение количества непрочитанных сообщений
    public LiveData<Integer> getUnreadMessagesCount(int userId) {
        return repository.getUnreadMessagesCount(userId);
    }
    
    // Отметка сообщений как прочитанных
    public void markMessagesAsRead(int currentUserId) {
        if (currentChatId != null) {
            repository.markMessagesAsRead(currentChatId, currentUserId);
        }
    }
    
    // Отправка нового сообщения
    public void sendMessage(int senderId, int receiverId, String text) {
        repository.sendMessage(senderId, receiverId, text, new MessageRepository.OnMessageSentListener() {
            @Override
            public void onMessageSent(Message message) {
                sentMessage.postValue(message);
            }
            
            @Override
            public void onMessageSendFailed(String errorMsg) {
                errorMessage.postValue(errorMsg);
            }
        });
    }
    
    // Обновление сообщений для текущего чата
    public void refreshMessages(int userId) {
        if (currentChatId == null || isPerformingRefresh) {
            return;
        }
        
        isPerformingRefresh = true;
        isRefreshing.setValue(true);
        
        repository.refreshMessages(currentChatId, userId, new MessageRepository.OnMessageRefreshListener() {
            @Override
            public void onMessagesRefreshing() {
                isRefreshing.postValue(true);
            }
            
            @Override
            public void onMessagesRefreshed(List<Message> messages) {
                isRefreshing.postValue(false);
                isPerformingRefresh = false;
            }
            
            @Override
            public void onMessageRefreshFailed(String error) {
                isRefreshing.postValue(false);
                errorMessage.postValue(error);
                isPerformingRefresh = false;
            }
        });
    }
    
    // Получение состояния обновления
    public LiveData<Boolean> isRefreshing() {
        return isRefreshing;
    }
    
    // Получение сообщения об ошибке
    public LiveData<String> getErrorMessage() {
        return errorMessage;
    }
    
    // Получение отправленного сообщения
    public LiveData<Message> getSentMessage() {
        return sentMessage;
    }
} 