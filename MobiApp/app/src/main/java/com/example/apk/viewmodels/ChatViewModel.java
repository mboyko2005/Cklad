package com.example.apk.viewmodels;

import android.app.Application;

import androidx.lifecycle.AndroidViewModel;
import androidx.lifecycle.LiveData;
import androidx.lifecycle.MutableLiveData;

import com.example.apk.data.ChatRepository;
import com.example.apk.models.ChatItem;

import java.util.List;

public class ChatViewModel extends AndroidViewModel {
    private final ChatRepository repository;
    private final LiveData<List<ChatItem>> allChats;
    
    // LiveData для отслеживания состояния загрузки
    private final MutableLiveData<Boolean> isRefreshing = new MutableLiveData<>(false);
    private final MutableLiveData<Boolean> isChecking = new MutableLiveData<>(false);
    private final MutableLiveData<Boolean> hasChanges = new MutableLiveData<>(false);
    private final MutableLiveData<String> errorMessage = new MutableLiveData<>();
    
    // Флаг для отслеживания, выполняется ли в данный момент обновление
    private boolean isPerformingRefresh = false;
    
    public ChatViewModel(Application application) {
        super(application);
        repository = new ChatRepository(application);
        allChats = repository.getAllChats();
    }
    
    // Получение всех чатов
    public LiveData<List<ChatItem>> getAllChats() {
        return allChats;
    }
    
    // Обновление чатов (полное с сервера)
    public void refreshChats(int userId) {
        // Предотвращаем множественные обновления
        if (isPerformingRefresh) {
            return;
        }
        
        isPerformingRefresh = true;
        isRefreshing.setValue(true);
        
        repository.refreshChats(userId, new ChatRepository.OnChatRefreshListener() {
            @Override
            public void onChatsRefreshing() {
                isRefreshing.postValue(true);
            }
            
            @Override
            public void onChatsRefreshed(List<ChatItem> chats) {
                isRefreshing.postValue(false);
                isPerformingRefresh = false;
            }
            
            @Override
            public void onChatRefreshFailed(String error) {
                isRefreshing.postValue(false);
                errorMessage.postValue(error);
                isPerformingRefresh = false;
            }
        });
    }
    
    // Проверка наличия новых чатов
    public void checkForNewChats(int userId) {
        // Если уже выполняется обновление, не запускаем проверку
        if (isPerformingRefresh || isChecking.getValue() == Boolean.TRUE) {
            return;
        }
        
        isChecking.setValue(true);
        
        repository.checkForNewChats(userId, new ChatRepository.OnChatCheckListener() {
            @Override
            public void onCheckStarted() {
                isChecking.postValue(true);
            }
            
            @Override
            public void onCheckCompleted(boolean hasChangesDetected, int totalChats) {
                isChecking.postValue(false);
                hasChanges.postValue(hasChangesDetected);
                
                // Если есть изменения, показываем индикатор обновления,
                // но только для РЕАЛЬНЫХ изменений данных
                if (hasChangesDetected) {
                    isRefreshing.postValue(true);
                    
                    // Кратковременное обновление для отображения UI с индикатором "Обновление..."
                    // а затем автоматическое скрытие
                    new android.os.Handler().postDelayed(() -> {
                        isRefreshing.postValue(false);
                    }, 1000);
                }
            }
            
            @Override
            public void onCheckFailed(String error) {
                isChecking.postValue(false);
                errorMessage.postValue(error);
            }
        });
    }
    
    // Получение состояния обновления
    public LiveData<Boolean> isRefreshing() {
        return isRefreshing;
    }
    
    // Получение состояния проверки
    public LiveData<Boolean> isChecking() {
        return isChecking;
    }
    
    // Информация о наличии изменений
    public LiveData<Boolean> hasChanges() {
        return hasChanges;
    }
    
    // Получение сообщения об ошибке
    public LiveData<String> getErrorMessage() {
        return errorMessage;
    }
} 