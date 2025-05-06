package com.example.apk.fragments;

import android.Manifest;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.media.MediaPlayer;
import android.media.Ringtone;
import android.media.RingtoneManager;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.os.Environment;
import android.os.Handler;
import android.os.Looper;
import android.os.VibrationEffect;
import android.os.Vibrator;
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
import java.util.ArrayList;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.IOException;
import okhttp3.MediaType;
import okhttp3.MultipartBody;
import okhttp3.RequestBody;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;

import android.view.LayoutInflater;
import android.view.WindowManager;
import android.widget.EditText;
import android.widget.FrameLayout;
import android.content.ContentResolver;
import android.database.Cursor;
import android.provider.OpenableColumns;

import com.bumptech.glide.Glide;

import java.text.DecimalFormat;
import android.util.Log;

// <<< Добавляем импорты для фоновой работы >>>
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

/**
 * Фрагмент для отображения переписки между пользователями
 */
public class ConversationFragment extends Fragment {
    private static final String ARG_PARTNER_ID = "partnerId";
    private static final String ARG_CHAT_NAME = "chatName";
    private static final int PERMISSION_REQUEST_CODE = 1001;
    private static final int MANAGE_STORAGE_PERMISSION_REQUEST = 1002;
    // Интервал обновления чата (5 секунд вместо 1 секунды)
    private static final long MESSAGE_UPDATE_INTERVAL = 5000;

    private int partnerId;
    private String chatName;
    private int userId;

    private SessionManager sessionManager;
    private RecyclerView recyclerView;
    private ConversationAdapter adapter;
    
    // Для периодического обновления сообщений
    private Handler handler;
    private Runnable updateRunnable;
    private boolean isAutoUpdateEnabled = true;

    // <<< Добавляем переменные для выбора изображения >>>
    private Uri selectedImageUri = null;
    private ActivityResultLauncher<String> requestPermissionLauncher;
    private ActivityResultLauncher<Intent> pickImageLauncher;
    
    // <<< Добавляем переменные для предпросмотра изображения >>>
    private View attachmentPreviewView;
    private String imageFileName;
    private long imageFileSize;
    
    // <<< Добавляем ExecutorService >>>
    private final ExecutorService backgroundExecutor = Executors.newSingleThreadExecutor();

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
        // checkStoragePermissions(); // <-- Пока закомментируем старую проверку

        // <<< Инициализация ActivityResultLauncher'ов >>>
        initializeLaunchers();
    }

    // <<< Новый метод для инициализации лаунчеров >>>
    private void initializeLaunchers() {
        // Лаунчер для запроса разрешений
        requestPermissionLauncher = registerForActivityResult(
                new ActivityResultContracts.RequestPermission(), isGranted -> {
            if (isGranted) {
                // Разрешение получено, запускаем выбор изображения
                launchImagePicker();
            } else {
                // Разрешение не получено
                Toast.makeText(requireContext(), "Разрешение на доступ к галерее не предоставлено", Toast.LENGTH_SHORT).show();
            }
        });

        // Лаунчер для выбора изображения из галереи
        pickImageLauncher = registerForActivityResult(
                new ActivityResultContracts.StartActivityForResult(), result -> {
            if (result.getResultCode() == getActivity().RESULT_OK && result.getData() != null) {
                selectedImageUri = result.getData().getData();
                if (selectedImageUri != null) {
                    // Получаем информацию о выбранном файле
                    getFileInfoFromUri(selectedImageUri);
                    
                    // Показываем превью вложения
                    showAttachmentPreview();
                }
            } else {
                selectedImageUri = null; // Сбрасываем, если выбор отменен
            }
        });
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
        
        // Настройка периодического обновления
        setupMessageUpdates();

        // Обработчик кнопки отправки
        ImageView sendButton = view.findViewById(R.id.send_button);
        TextInputEditText messageInput = view.findViewById(R.id.message_input);

        // Показ/скрытие send_button в зависимости от текста
        messageInput.addTextChangedListener(new TextWatcher() {
            @Override public void beforeTextChanged(CharSequence s, int start, int count, int after) {}
            @Override public void onTextChanged(CharSequence s, int start, int before, int count) {
                // Показываем кнопку отправки, если есть текст или прикреплено изображение
                boolean showButton = s.length() > 0 || selectedImageUri != null;
                sendButton.setVisibility(showButton ? View.VISIBLE : View.GONE);
            }
            @Override public void afterTextChanged(Editable s) {}
        });

        sendButton.setOnClickListener(v -> {
            String text = messageInput.getText().toString().trim();
            // Если нет ни текста, ни изображения - ничего не делаем
            if (text.isEmpty() && selectedImageUri == null) return;
            
            // Очищаем поле ввода
            messageInput.setText("");
            
            // Если нет прикрепленного изображения - отправляем только текст
            if (selectedImageUri == null) {
                // Отправляем обычное текстовое сообщение
            SendMessageRequest req = new SendMessageRequest(userId, partnerId, text);
            ApiClient.getApiService().sendMessage(req)
                .enqueue(new Callback<SendMessageResponse>() {
                    @Override
                    public void onResponse(Call<SendMessageResponse> call,
                                           Response<SendMessageResponse> response) {
                        if (response.isSuccessful() && response.body() != null && response.body().isSuccess()) {
                            MessageResponse newMsg = response.body().getMessage();
                                // Добавляем текстовое сообщение в adapter
                                List<MessageResponse> current = new ArrayList<>(adapter.getMessages());
                                current.add(newMsg);
                                adapter.setMessages(current);
                                recyclerView.scrollToPosition(current.size() - 1);
                            } else {
                                // Обработка ошибки отправки текстового сообщения
                                Toast.makeText(requireContext(), "Ошибка отправки сообщения", Toast.LENGTH_SHORT).show();
                            }
                        }

                        @Override
                        public void onFailure(Call<SendMessageResponse> call, Throwable t) {
                            Toast.makeText(requireContext(), "Ошибка отправки: " + t.getMessage(), Toast.LENGTH_SHORT).show();
                        }
                    });
            } else {
                // Отправляем сообщение с вложением
                // Если текст пустой, используем имя файла в качестве текста
                String messageText = text.isEmpty() ? imageFileName : text;
                
                // Создаем запрос для отправки сообщения
                SendMessageRequest req = new SendMessageRequest(userId, partnerId, messageText);
                
                // Сохраняем URI изображения для использования в колбэке
                final Uri imageUri = selectedImageUri;
                
                // Отправляем сообщение
                ApiClient.getApiService().sendMessage(req)
                    .enqueue(new Callback<SendMessageResponse>() {
                        @Override
                        public void onResponse(Call<SendMessageResponse> call,
                                               Response<SendMessageResponse> response) {
                            if (response.isSuccessful() && response.body() != null && response.body().isSuccess()) {
                                MessageResponse newMsg = response.body().getMessage();
                                
                                // Устанавливаем флаг вложения и локальный URI
                                newMsg.setHasAttachment(true);
                                newMsg.setLocalAttachmentUri(imageUri);
                                
                                // Добавляем сообщение в адаптер
                                List<MessageResponse> current = new ArrayList<>(adapter.getMessages());
                                current.add(newMsg);
                                adapter.setMessages(current);
                                recyclerView.scrollToPosition(current.size() - 1);
                                
                                // Загружаем изображение в фоне
                                uploadImage(newMsg.getMessageId(), imageUri);
                                
                                // Скрываем превью вложения и сбрасываем выбранное изображение
                                hideAttachmentPreview();
                                selectedImageUri = null;
                            } else {
                                Toast.makeText(requireContext(), "Ошибка отправки сообщения", Toast.LENGTH_SHORT).show();
                            }
                        }

                        @Override
                        public void onFailure(Call<SendMessageResponse> call, Throwable t) {
                            Toast.makeText(requireContext(), "Ошибка отправки: " + t.getMessage(), Toast.LENGTH_SHORT).show();
                        }
                    });
            }
        });
        
        // Настройка кнопки прикрепления файлов
        ImageView attachButton = view.findViewById(R.id.attach);
        if (attachButton != null) {
            attachButton.setOnClickListener(v -> {
                // <<< Логика для прикрепления файлов >>>
                checkGalleryPermissionAndPickImage();
            });
        }
    }
    
    /**
     * Настройка периодического обновления сообщений
     */
    private void setupMessageUpdates() {
        handler = new Handler(Looper.getMainLooper());
        updateRunnable = new Runnable() {
            @Override
            public void run() {
                if (isAdded() && isAutoUpdateEnabled) {
                    loadConversation();
                    // Планируем следующее обновление
                    handler.postDelayed(this, MESSAGE_UPDATE_INTERVAL);
                }
            }
        };
    }
    
    @Override
    public void onResume() {
        super.onResume();
        // Запускаем периодическое обновление
        isAutoUpdateEnabled = true;
        handler.postDelayed(updateRunnable, MESSAGE_UPDATE_INTERVAL);
        
        // Обновляем переписку при возврате к фрагменту
        loadConversation();
        
        // Отмечаем сообщения как прочитанные
        markMessagesAsRead();
    }
    
    @Override
    public void onPause() {
        super.onPause();
        // Останавливаем периодическое обновление
        isAutoUpdateEnabled = false;
        handler.removeCallbacks(updateRunnable);
    }
    
    /**
     * Отмечает сообщения как прочитанные
     */
    private void markMessagesAsRead() {
        ApiClient.getApiService()
                .markMessagesAsRead(userId, partnerId)
                .enqueue(new Callback<Object>() {
                    @Override
                    public void onResponse(Call<Object> call, Response<Object> response) {
                        // Обновляем список сообщений после отметки
                        loadConversation();
                    }

                    @Override
                    public void onFailure(Call<Object> call, Throwable t) {
                        // Игнорируем ошибку
                    }
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
                            List<MessageResponse> currentMsgs = adapter.getMessages();
                            
                            // Проверяем, изменились ли сообщения
                            boolean shouldUpdate = currentMsgs.size() != msgs.size();
                            
                            // Если количество одинаковое, проверяем ID последнего сообщения
                            if (!shouldUpdate && !msgs.isEmpty() && !currentMsgs.isEmpty()) {
                                MessageResponse lastNew = msgs.get(msgs.size() - 1);
                                MessageResponse lastCurrent = currentMsgs.get(currentMsgs.size() - 1);
                                shouldUpdate = lastNew.getMessageId() != lastCurrent.getMessageId();
                            }
                            
                            if (shouldUpdate) {
                                // Если добавлены новые сообщения - воспроизводим звук уведомления
                                if (currentMsgs.size() > 0 && msgs.size() > currentMsgs.size()) {
                                    playNotificationSound();
                                }
                                
                                adapter.setMessages(msgs);
                                // Прокрутка в конец
                                if (!msgs.isEmpty()) {
                                    recyclerView.scrollToPosition(msgs.size() - 1);
                                }
                                
                                // Отмечаем сообщения как прочитанные
                                markMessagesAsRead();
                            }
                        }
                    }

                    @Override
                    public void onFailure(Call<MessagesResponse> call, Throwable t) {
                        Toast.makeText(requireContext(), "Ошибка загрузки сообщений", Toast.LENGTH_SHORT).show();
                    }
                });
    }
    
    /**
     * Воспроизводит звук уведомления о новом сообщении
     */
    private void playNotificationSound() {
        try {
            // Используем стандартный звук уведомления
            Uri notificationSound = RingtoneManager.getDefaultUri(RingtoneManager.TYPE_NOTIFICATION);
            Ringtone r = RingtoneManager.getRingtone(requireContext(), notificationSound);
            r.play();
            
            // Также добавляем вибрацию для лучшего оповещения
            Vibrator vibrator = (Vibrator) requireContext().getSystemService(Context.VIBRATOR_SERVICE);
            if (vibrator != null && vibrator.hasVibrator()) {
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                    vibrator.vibrate(VibrationEffect.createOneShot(100, VibrationEffect.DEFAULT_AMPLITUDE));
                } else {
                    vibrator.vibrate(100);
                }
            }
        } catch (Exception e) {
            // Игнорируем ошибки воспроизведения звука
        }
    }

    // <<< Новый метод для проверки разрешений и запуска выбора >>>
    private void checkGalleryPermissionAndPickImage() {
        String permission;
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            // Android 13+
            permission = Manifest.permission.READ_MEDIA_IMAGES;
        } else {
            // Android < 13
            permission = Manifest.permission.READ_EXTERNAL_STORAGE;
        }

        if (ContextCompat.checkSelfPermission(requireContext(), permission) == PackageManager.PERMISSION_GRANTED) {
            // Разрешение уже есть, запускаем выбор
            launchImagePicker();
        } else {
            // Запрашиваем разрешение
            requestPermissionLauncher.launch(permission);
        }
    }

    // <<< Новый метод для запуска системного пикера >>>
    private void launchImagePicker() {
        Intent intent = new Intent(Intent.ACTION_PICK);
        intent.setType("image/*");
        pickImageLauncher.launch(intent);
    }

    // <<< Метод для загрузки изображения (с фоновой подготовкой) >>>
    private void uploadImage(int messageId, Uri imageUri) {
        // Запускаем подготовку и загрузку в фоновом потоке
        backgroundExecutor.execute(() -> {
            File imageFile = getFileFromUri(requireContext(), imageUri);
            if (imageFile == null) {
                // Показываем ошибку в основном потоке
                new Handler(Looper.getMainLooper()).post(() -> 
                    Toast.makeText(requireContext(), "Не удалось получить файл изображения", Toast.LENGTH_SHORT).show());
                return;
            }

            try {
                // Создаем RequestBody для messageId
                RequestBody messageIdBody = RequestBody.create(MediaType.parse("text/plain"), String.valueOf(messageId));

                // Создаем RequestBody для файла
                String mimeType = requireContext().getContentResolver().getType(imageUri);
                if (mimeType == null) {
                    mimeType = "image/*"; // Тип по умолчанию
                }
                RequestBody requestFile = RequestBody.create(MediaType.parse(mimeType), imageFile);
                MultipartBody.Part body = MultipartBody.Part.createFormData("file", imageFile.getName(), requestFile);

                // Выполняем запрос на загрузку (уже в фоновом потоке)
                ApiClient.getApiService().uploadMessageMedia(userId, messageIdBody, body)
                    .enqueue(new Callback<MessageResponse>() {
                        @Override
                        public void onResponse(Call<MessageResponse> call, Response<MessageResponse> response) {
                            // Обработка ответа (выполняется в потоке OkHttp/Retrofit)
                             File tempFile = imageFile; // Сохраняем ссылку для удаления
                            if (response.isSuccessful() && response.body() != null) {
                                // Можно показать Toast об успехе (в основном потоке)
                                new Handler(Looper.getMainLooper()).post(() -> 
                                    Toast.makeText(requireContext(), "Изображение успешно загружено", Toast.LENGTH_SHORT).show());
                            } else {
                                // Показываем ошибку (в основном потоке)
                                int code = response.code();
                                new Handler(Looper.getMainLooper()).post(() -> 
                                    Toast.makeText(requireContext(), "Ошибка загрузки изображения: " + code, Toast.LENGTH_SHORT).show());
                                // Если загрузка не удалась, обновляем чат (в основном потоке)
                                new Handler(Looper.getMainLooper()).post(ConversationFragment.this::loadConversation);
                            }
                            // Удаляем временный файл
                            tempFile.delete();
                        }

                        @Override
                        public void onFailure(Call<MessageResponse> call, Throwable t) {
                            // Обработка ошибки сети (выполняется в потоке OkHttp/Retrofit)
                            File tempFile = imageFile; // Сохраняем ссылку для удаления
                             String errorMessage = t.getMessage();
                            // Показываем ошибку (в основном потоке)
                            new Handler(Looper.getMainLooper()).post(() ->
                                Toast.makeText(requireContext(), "Ошибка сети при загрузке: " + errorMessage, Toast.LENGTH_SHORT).show());
                            // Если загрузка не удалась, обновляем чат (в основном потоке)
                            new Handler(Looper.getMainLooper()).post(ConversationFragment.this::loadConversation);
                            // Удаляем временный файл
                            tempFile.delete();
                        }
                    });
            } catch (Exception e) {
                // Обработка ошибок при подготовке запроса
                File tempFile = imageFile; // Сохраняем ссылку для удаления
                String errorMsg = e.getMessage();
                 new Handler(Looper.getMainLooper()).post(() ->
                    Toast.makeText(requireContext(), "Ошибка подготовки файла: " + errorMsg, Toast.LENGTH_SHORT).show());
                 if (tempFile != null) tempFile.delete();
            }
        });
    }

    // <<< Вспомогательный метод для получения File из Uri >>>
    private File getFileFromUri(Context context, Uri uri) {
        File file = null;
        InputStream inputStream = null;
        OutputStream outputStream = null;
        try {
            String fileExtension = getFileExtension(context, uri);
            // Создаем временный файл в кеше приложения
            file = File.createTempFile("upload_", "." + fileExtension, context.getCacheDir());
            inputStream = context.getContentResolver().openInputStream(uri);
            outputStream = new FileOutputStream(file);
            if (inputStream != null) {
                byte[] buffer = new byte[1024];
                int length;
                while ((length = inputStream.read(buffer)) > 0) {
                    outputStream.write(buffer, 0, length);
                }
            }
        } catch (IOException e) {
            e.printStackTrace();
            return null; // Возвращаем null в случае ошибки
        } finally {
            try {
                if (inputStream != null) inputStream.close();
                if (outputStream != null) outputStream.close();
            } catch (IOException e) {
                e.printStackTrace();
            }
        }
        return file;
    }

    // <<< Вспомогательный метод для получения расширения файла >>>
    private String getFileExtension(Context context, Uri uri) {
        android.content.ContentResolver cR = context.getContentResolver();
        android.webkit.MimeTypeMap mime = android.webkit.MimeTypeMap.getSingleton();
        String type = mime.getExtensionFromMimeType(cR.getType(uri));
        if (type == null) {
            // Пытаемся получить из пути, если MIME тип не определен
            String path = uri.getPath();
            if (path != null) {
                int cut = path.lastIndexOf('.');
                if (cut != -1) {
                    return path.substring(cut + 1);
                }
            }
            return "tmp"; // Возвращаем временное расширение, если ничего не нашли
        }
        return type;
    }

    // <<< Новый метод для получения информации о файле >>>
    private void getFileInfoFromUri(Uri uri) {
        ContentResolver contentResolver = requireContext().getContentResolver();
        Cursor cursor = contentResolver.query(uri, null, null, null, null);
        
        // Имя файла и размер по умолчанию
        imageFileName = "image.jpg";
        imageFileSize = 0;
        
        if (cursor != null && cursor.moveToFirst()) {
            // Получаем имя файла из колонки DISPLAY_NAME
            int nameIndex = cursor.getColumnIndex(OpenableColumns.DISPLAY_NAME);
            if (nameIndex != -1) {
                imageFileName = cursor.getString(nameIndex);
            }
            
            // Получаем размер файла из колонки SIZE
            int sizeIndex = cursor.getColumnIndex(OpenableColumns.SIZE);
            if (sizeIndex != -1) {
                imageFileSize = cursor.getLong(sizeIndex);
            }
            
            cursor.close();
        }
    }
    
    // <<< Метод для показа превью вложения >>>
    private void showAttachmentPreview() {
        // Находим контейнер для превью
        FrameLayout previewContainer = requireView().findViewById(R.id.attachment_preview_container);
        
        // Если превью еще не создано - создаем его
        if (attachmentPreviewView == null) {
            LayoutInflater inflater = LayoutInflater.from(requireContext());
            attachmentPreviewView = inflater.inflate(R.layout.layout_attachment_preview, previewContainer, false);
            previewContainer.addView(attachmentPreviewView);
            
            // Находим элементы UI в превью
            ImageView attachmentImage = attachmentPreviewView.findViewById(R.id.attachment_image);
            TextView attachmentInfo = attachmentPreviewView.findViewById(R.id.attachment_info);
            ImageView removeButton = attachmentPreviewView.findViewById(R.id.attachment_remove);
            
            // Обработчик кнопки удаления вложения
            removeButton.setOnClickListener(v -> {
                hideAttachmentPreview();
                selectedImageUri = null;
            });
        }
        
        // Обновляем содержимое превью
        ImageView attachmentImage = attachmentPreviewView.findViewById(R.id.attachment_image);
        TextView attachmentInfo = attachmentPreviewView.findViewById(R.id.attachment_info);
        
        // Загружаем изображение
        Glide.with(requireContext())
                .load(selectedImageUri)
                .centerCrop()
                .into(attachmentImage);
        
        // Обновляем информацию о файле
        String formattedSize = formatFileSize(imageFileSize);
        attachmentInfo.setText(imageFileName + " • " + formattedSize);
        
        // Показываем контейнер
        previewContainer.setVisibility(View.VISIBLE);
        
        // Прокручиваем список сообщений к концу, чтобы было видно поле ввода с превью
        recyclerView.scrollToPosition(adapter.getItemCount() - 1);
        
        // Показываем кнопку отправки, даже если поле ввода пустое
        ImageView sendButton = requireView().findViewById(R.id.send_button);
        sendButton.setVisibility(View.VISIBLE);
    }
    
    // <<< Метод для скрытия превью вложения >>>
    private void hideAttachmentPreview() {
        FrameLayout previewContainer = requireView().findViewById(R.id.attachment_preview_container);
        previewContainer.setVisibility(View.GONE);
        
        // Проверяем, нужно ли скрыть кнопку отправки (если поле ввода пустое)
        TextInputEditText messageInput = requireView().findViewById(R.id.message_input);
        ImageView sendButton = requireView().findViewById(R.id.send_button);
        if (messageInput.getText().toString().trim().isEmpty()) {
            sendButton.setVisibility(View.GONE);
        }
    }
    
    // <<< Новый метод для форматирования размера файла >>>
    private String formatFileSize(long size) {
        if (size <= 0) {
            return "0 B";
        }
        
        final String[] units = new String[] { "B", "KB", "MB", "GB", "TB" };
        int digitGroups = (int) (Math.log10(size) / Math.log10(1024));
        
        return new DecimalFormat("#,##0.#").format(size / Math.pow(1024, digitGroups)) 
                + " " + units[digitGroups];
    }

    @Override
    public void onDestroy() {
        super.onDestroy();
        // Останавливаем ExecutorService при уничтожении фрагмента
        backgroundExecutor.shutdown();
    }
} 