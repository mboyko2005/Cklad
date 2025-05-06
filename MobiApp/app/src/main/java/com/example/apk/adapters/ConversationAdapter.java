package com.example.apk.adapters;

import android.content.ActivityNotFoundException;
import android.content.Context;
import android.content.Intent;
import android.graphics.Bitmap;
import android.graphics.drawable.BitmapDrawable;
import android.graphics.drawable.Drawable;
import android.net.Uri;
import android.os.Build;
import android.os.Environment;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.webkit.MimeTypeMap;
import android.widget.Button;
import android.widget.FrameLayout;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.bumptech.glide.Glide;
import com.bumptech.glide.load.DataSource;
import com.bumptech.glide.load.engine.GlideException;
import com.bumptech.glide.request.RequestListener;
import com.bumptech.glide.request.target.Target;
import com.example.apk.R;
import com.example.apk.api.ApiClient;
import com.example.apk.api.MessageResponse;
import com.example.apk.dialogs.ImageEditorDialog;

import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.text.DecimalFormat;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.HashMap;
import java.time.LocalDate;

import okhttp3.ResponseBody;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

public class ConversationAdapter extends RecyclerView.Adapter<RecyclerView.ViewHolder> {
    private static final int VIEW_TYPE_DATE_DIVIDER = 0;
    private static final int VIEW_TYPE_SENT = 1;
    private static final int VIEW_TYPE_RECEIVED = 2;

    private List<MessageResponse> originalMessages = new ArrayList<>();
    // Смешанный список: либо String (дата), либо MessageResponse
    private List<Object> items = new ArrayList<>();
    private final int currentUserId;
    
    // Хранение информации о скачивании файлов
    private Map<Integer, FileDownloadStatus> fileDownloadStatuses = new HashMap<>();

    // Карта для названий месяцев в родительном падеже
    private static final Map<Integer, String> MONTH_MAP = new HashMap<>();
    static {
        MONTH_MAP.put(1, "января");
        MONTH_MAP.put(2, "февраля");
        MONTH_MAP.put(3, "марта");
        MONTH_MAP.put(4, "апреля");
        MONTH_MAP.put(5, "мая");
        MONTH_MAP.put(6, "июня");
        MONTH_MAP.put(7, "июля");
        MONTH_MAP.put(8, "августа");
        MONTH_MAP.put(9, "сентября");
        MONTH_MAP.put(10, "октября");
        MONTH_MAP.put(11, "ноября");
        MONTH_MAP.put(12, "декабря");
    }

    public ConversationAdapter(int currentUserId) {
        this.currentUserId = currentUserId;
    }

    // Устанавливаем список сообщений и формируем разделители дат
    public void setMessages(List<MessageResponse> msgs) {
        originalMessages = msgs != null ? msgs : new ArrayList<>();
        items.clear();
        String prevDate = "";
        for (MessageResponse msg : originalMessages) {
            String fullTimestamp = msg.getTimestamp();
            String datePart = fullTimestamp.split("T")[0];
            if (!datePart.equals(prevDate)) {
                prevDate = datePart;
                items.add(formatDateDivider(datePart));
            }
            items.add(msg);
        }
        notifyDataSetChanged();
        
        // Запускаем предварительную загрузку изображений
        preloadImages();
    }

    // Форматирует дату для разделителя
    private String formatDateDivider(String datePart) {
        LocalDate date = LocalDate.parse(datePart);
        LocalDate today = LocalDate.now();
        // Если дата — сегодня, показываем "Сегодня"
        if (date.equals(today)) {
            return "Сегодня";
        }
        int day = date.getDayOfMonth();
        String month = MONTH_MAP.get(date.getMonthValue());
        return day + " " + month;
    }

    /**
     * Возвращает список сообщений без разделителей дат
     */
    public List<MessageResponse> getMessages() {
        return new ArrayList<>(originalMessages);
    }

    @Override
    public int getItemViewType(int position) {
        Object item = items.get(position);
        if (item instanceof String) {
            return VIEW_TYPE_DATE_DIVIDER;
        } else {
            MessageResponse msg = (MessageResponse) item;
            return msg.getSenderId() == currentUserId ? VIEW_TYPE_SENT : VIEW_TYPE_RECEIVED;
        }
    }

    @NonNull
    @Override
    public RecyclerView.ViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        LayoutInflater inflater = LayoutInflater.from(parent.getContext());
        appContext = parent.getContext().getApplicationContext();
        
        if (viewType == VIEW_TYPE_DATE_DIVIDER) {
            View view = inflater.inflate(R.layout.item_date_divider, parent, false);
            convertView = view;
            return new DateDividerViewHolder(view);
        } else if (viewType == VIEW_TYPE_SENT) {
            View view = inflater.inflate(R.layout.item_message_sent, parent, false);
            convertView = view;
            return new MessageViewHolder(view);
        } else {
            View view = inflater.inflate(R.layout.item_message_received, parent, false);
            convertView = view;
            return new MessageViewHolder(view);
        }
    }

    @Override
    public void onBindViewHolder(@NonNull RecyclerView.ViewHolder holder, int position) {
        int viewType = getItemViewType(position);
        if (viewType == VIEW_TYPE_DATE_DIVIDER) {
            String dateText = (String) items.get(position);
            ((DateDividerViewHolder) holder).dateText.setText(dateText);
        } else {
            MessageResponse msg = (MessageResponse) items.get(position);
            MessageViewHolder mvh = (MessageViewHolder) holder;
            mvh.messageText.setText(msg.getText());
            // Форматируем время HH:mm
            String ts = msg.getTimestamp();
            String time = ts.contains("T") ? ts.substring(ts.indexOf('T')+1, ts.indexOf('T')+6) : ts;
            mvh.messageTime.setText(time);

            if (msg.hasAttachment()) {
                // Устанавливаем правильную видимость элементов
                mvh.messageText.setVisibility(View.VISIBLE);
                mvh.attachmentContainer.setVisibility(View.VISIBLE);
                
                // Определяем источник для загрузки
                Object loadSource;
                if (msg.getLocalAttachmentUri() != null) {
                    // Если есть локальный URI (только что отправленное сообщение), грузим из него
                    loadSource = msg.getLocalAttachmentUri();
                } else {
                    // Иначе грузим с сервера
                    loadSource = ApiClient.BASE_URL + "api/Message/media/" + msg.getMessageId();
                }
                
                // Всегда пробуем сначала загрузить как изображение
                mvh.messageImage.setVisibility(View.VISIBLE);
                mvh.fileLayout.setVisibility(View.GONE);
                mvh.imageLoadingProgress.setVisibility(View.VISIBLE);
                
                // Загружаем изображение оптимизированным способом с приоритетом IMMEDIATE
                // Сначала проверяем, есть ли изображение в кеше, чтобы показать немедленно
                Glide.with(mvh.itemView.getContext())
                        .load(loadSource)
                    .placeholder(R.drawable.placeholder_image)
                    .diskCacheStrategy(com.bumptech.glide.load.engine.DiskCacheStrategy.ALL)
                    .priority(com.bumptech.glide.Priority.IMMEDIATE)
                    .thumbnail(0.5f) // Добавляем загрузку миниатюры для быстрого показа
                    .override(600, 600) // Ограничиваем размер для ускорения
                    .dontTransform() // Отключаем трансформации
                    .format(com.bumptech.glide.load.DecodeFormat.PREFER_RGB_565) // Экономия памяти
                        .listener(new RequestListener<Drawable>() {
                            @Override
                            public boolean onLoadFailed(GlideException e, Object model, Target<Drawable> target, boolean isFirstResource) {
                            // Если загрузка не удалась, значит это не изображение - показываем как файл
                            mvh.imageLoadingProgress.setVisibility(View.GONE);
                                mvh.messageImage.setVisibility(View.GONE);
                                mvh.fileLayout.setVisibility(View.VISIBLE);
                                
                                // Находим имя файла в тексте сообщения или устанавливаем стандартное
                                String messageText = msg.getText().toLowerCase();
                                String fileName = messageText.isEmpty() ? "документ" : messageText.trim();
                                mvh.fileName.setText(fileName);
                                
                                // Устанавливаем значок файла в зависимости от расширения
                                setFileIcon(mvh.fileIcon, fileName);
                                
                                // ID сообщения для загрузки
                                int messageId = msg.getMessageId();
                                
                                // Обновляем UI в зависимости от статуса загрузки
                                updateDownloadUI(mvh, messageId, fileName);
                                
                            return false;
                            }

                            @Override
                            public boolean onResourceReady(Drawable resource, Object model, Target<Drawable> target, DataSource dataSource, boolean isFirstResource) {
                            mvh.imageLoadingProgress.setVisibility(View.GONE);
                            // Изображение успешно загружено - сохраняем тип для будущих загрузок
                            if (msg.getAttachmentType() == null) {
                                msg.setAttachmentType("image");
                            }
                            
                            // Логируем источник загрузки, чтобы отслеживать эффективность кеширования
                            android.util.Log.d("ImageLoading", "Image loaded from " + dataSource.name() + 
                                    " for message " + msg.getMessageId());
                            
                            return false;
                            }
                        })
                    .into(new com.bumptech.glide.request.target.DrawableImageViewTarget(mvh.messageImage) {
                        @Override
                        public void onLoadStarted(Drawable placeholder) {
                            super.onLoadStarted(placeholder);
                        }
                    });
            } else {
                // Если нет вложений, скрываем элементы
                mvh.messageImage.setVisibility(View.GONE);
                mvh.attachmentContainer.setVisibility(View.GONE);
            }

            mvh.messageImage.setOnClickListener(v -> {
                Context context = v.getContext();
                if (context != null) {
                    MessageResponse currentMessage = (MessageResponse) items.get(holder.getAdapterPosition());
                    if (currentMessage != null && currentMessage.hasAttachment()) {
                        // Получаем изображение из ImageView
                        if (mvh.messageImage.getDrawable() instanceof BitmapDrawable) {
                            Bitmap bitmap = ((BitmapDrawable) mvh.messageImage.getDrawable()).getBitmap();
                            if (bitmap != null) {
                                // Открываем диалог редактирования изображения
                                ImageEditorDialog dialog = new ImageEditorDialog(context, bitmap, editedBitmap -> {
                                    // Обновляем изображение в сообщении
                                    mvh.messageImage.setImageBitmap(editedBitmap);
                                    // Сохраняем изменения в сообщении
                                    currentMessage.setEditedBitmap(editedBitmap);
                                });
                                dialog.show();
                            }
                        }
                    }
                }
            });
        }
    }
    
    // Устанавливает иконку файла в зависимости от расширения
    private void setFileIcon(ImageView imageView, String fileName) {
        fileName = fileName.toLowerCase();
        if (fileName.endsWith(".pdf")) {
            imageView.setImageResource(R.drawable.ic_document);
        } else if (fileName.endsWith(".doc") || fileName.endsWith(".docx")) {
            imageView.setImageResource(R.drawable.ic_document);
        } else if (fileName.endsWith(".xls") || fileName.endsWith(".xlsx")) {
            imageView.setImageResource(R.drawable.ic_document);
        } else if (fileName.endsWith(".txt")) {
            imageView.setImageResource(R.drawable.ic_document);
        } else {
            imageView.setImageResource(R.drawable.ic_document);
        }
    }
    
    // Настраивает кнопку загрузки
    private void setupDownloadButton(MessageViewHolder holder, int messageId, String fileName) {
        holder.downloadIcon.setOnClickListener(v -> {
            // Сохраняем контекст
            Context context = v.getContext();
            
            // Создаем или обновляем статус загрузки
            final FileDownloadStatus statusFinal;
            FileDownloadStatus status = fileDownloadStatuses.get(messageId);
            if (status == null) {
                status = new FileDownloadStatus();
                fileDownloadStatuses.put(messageId, status);
            }
            statusFinal = status;
            
            // Если загрузка не в процессе, начинаем ее
            if (!statusFinal.isDownloading) {
                statusFinal.isDownloading = true;
                statusFinal.progress = 0;
                statusFinal.totalBytes = 0;
                statusFinal.downloadedBytes = 0;
                notifyItemChanged(holder.getAdapterPosition());
                
                // Начинаем загрузку
                String url = "api/Message/media/" + messageId;
                ApiClient.getApiService().downloadFile(url).enqueue(new Callback<ResponseBody>() {
                    @Override
                    public void onResponse(Call<ResponseBody> call, Response<ResponseBody> response) {
                        if (response.isSuccessful() && response.body() != null) {
                            // Запускаем сохранение файла в фоновом потоке
                            Thread thread = new Thread(() -> {
                                boolean successful = writeResponseBodyToDisk(response.body(), fileName, statusFinal, holder, messageId);
                                
                                if (successful) {
                                    // Оповещаем пользователя о успешной загрузке
                                    holder.itemView.post(() -> {
                                        FileDownloadStatus statusUpdate = fileDownloadStatuses.get(messageId);
                                        statusUpdate.isDownloading = false;
                                        statusUpdate.isComplete = true;
                                        notifyItemChanged(holder.getAdapterPosition());
                                        Toast.makeText(context, "Файл загружен: " + fileName, Toast.LENGTH_SHORT).show();
                                    });
                                } else {
                                    // Сообщаем об ошибке
                                    holder.itemView.post(() -> {
                                        FileDownloadStatus statusUpdate = fileDownloadStatuses.get(messageId);
                                        statusUpdate.isDownloading = false;
                                        statusUpdate.isComplete = false;
                                        notifyItemChanged(holder.getAdapterPosition());
                                        Toast.makeText(context, "Ошибка при загрузке файла", Toast.LENGTH_SHORT).show();
                                    });
                                }
                            });
                            thread.start();
                        } else {
                            FileDownloadStatus statusUpdate = fileDownloadStatuses.get(messageId);
                            statusUpdate.isDownloading = false;
                            notifyItemChanged(holder.getAdapterPosition());
                            Toast.makeText(context, "Ошибка загрузки файла", Toast.LENGTH_SHORT).show();
                        }
                    }

                    @Override
                    public void onFailure(Call<ResponseBody> call, Throwable t) {
                        FileDownloadStatus statusUpdate = fileDownloadStatuses.get(messageId);
                        statusUpdate.isDownloading = false;
                        notifyItemChanged(holder.getAdapterPosition());
                        Toast.makeText(context, "Ошибка сети: " + t.getMessage(), Toast.LENGTH_SHORT).show();
                    }
                });
            }
        });
    }
    
    // Обновляет UI в зависимости от статуса загрузки
    private void updateDownloadUI(MessageViewHolder holder, int messageId, String fileName) {
        FileDownloadStatus status = fileDownloadStatuses.get(messageId);
        
        // Проверяем, существует ли уже файл в директории загрузок - делаем это быстрее
        File downloadsDir = Environment.getExternalStoragePublicDirectory(Environment.DIRECTORY_DOWNLOADS);
        File outputFile = new File(downloadsDir, fileName);
        boolean fileExists = outputFile.exists();
        
        // Если файл уже существует, отмечаем его как загруженный
        if (fileExists && (status == null || !status.isComplete)) {
            if (status == null) {
                status = new FileDownloadStatus();
                fileDownloadStatuses.put(messageId, status);
            }
            status.isComplete = true;
        }
        
        // Устанавливаем видимость элементов в зависимости от состояния загрузки
        if (status != null && status.isDownloading) {
            // Файл в процессе загрузки
            holder.fileIcon.setVisibility(View.GONE);
            holder.downloadIcon.setVisibility(View.GONE);
            holder.cancelIcon.setVisibility(View.VISIBLE);
            holder.downloadProgressRing.setVisibility(View.VISIBLE);
            
            // Устанавливаем обработчик на крестик для отмены загрузки
            holder.cancelIcon.setOnClickListener(v -> {
                // Код для отмены загрузки
                cancelDownload(messageId);
                // Обновляем UI
                notifyItemChanged(holder.getAdapterPosition());
            });
            
            // Обновляем прогресс
            if (status.totalBytes > 0) {
                // Устанавливаем детерминированный прогресс
                holder.downloadProgressRing.setIndeterminate(false);
                int progressPercent = (int)((status.downloadedBytes * 100) / status.totalBytes);
                holder.downloadProgressRing.setProgress(progressPercent);
                
                // Обновляем текст размера с прогрессом
                String progressText = formatFileSize(status.downloadedBytes) + "/" + formatFileSize(status.totalBytes);
                holder.fileSize.setText(progressText);
            } else {
                // Если общий размер пока неизвестен, показываем неопределенный прогресс
                holder.downloadProgressRing.setIndeterminate(true);
                holder.fileSize.setText("Загрузка...");
            }
        } else if (status != null && status.isComplete || fileExists) {
            // Файл уже загружен
            holder.fileIcon.setVisibility(View.VISIBLE);
            holder.downloadIcon.setVisibility(View.GONE);
            holder.cancelIcon.setVisibility(View.GONE);
            holder.downloadProgressRing.setVisibility(View.GONE);
            
            // Показываем размер, если известен
            if (fileExists) {
                long fileSize = outputFile.length();
                holder.fileSize.setText(formatFileSize(fileSize));
            }
            
            // Устанавливаем обработчик на иконку файла для открытия
            holder.fileIcon.setOnClickListener(v -> openFile(v.getContext(), fileName));
            holder.fileIcon.setClickable(true);
            holder.fileIcon.setFocusable(true);
            holder.fileIcon.setBackgroundResource(R.drawable.circle_ripple_effect);
        } else {
            // Файл не загружен
            holder.fileIcon.setVisibility(View.GONE);
            holder.downloadIcon.setVisibility(View.VISIBLE);
            holder.cancelIcon.setVisibility(View.GONE);
            holder.downloadProgressRing.setVisibility(View.GONE);
            
            // Устанавливаем обработчик на иконку загрузки
            holder.downloadIcon.setOnClickListener(v -> {
                startDownload(holder, messageId, fileName);
            });
            holder.downloadIcon.setClickable(true);
            holder.downloadIcon.setFocusable(true);
            holder.downloadIcon.setBackgroundResource(R.drawable.circle_ripple_effect);
        }
    }
    
    // Записывает загруженный файл на диск
    private boolean writeResponseBodyToDisk(ResponseBody body, String fileName, FileDownloadStatus status, 
                                           MessageViewHolder holder, int messageId) {
        try {
            // Создаем директорию для загрузок, если она не существует
            File downloadsDir = Environment.getExternalStoragePublicDirectory(Environment.DIRECTORY_DOWNLOADS);
            File outputFile = new File(downloadsDir, fileName);
            
            // Открываем потоки
            InputStream inputStream = null;
            OutputStream outputStream = null;
            
            try {
                byte[] fileReader = new byte[4096];
                
                // Получаем общий размер файла
                status.totalBytes = body.contentLength();
                status.downloadedBytes = 0;
                
                // Открываем потоки
                inputStream = body.byteStream();
                outputStream = new FileOutputStream(outputFile);
                
                // Читаем данные и обновляем прогресс
                while (true) {
                    int read = inputStream.read(fileReader);
                    if (read == -1) {
                        break;
                    }
                    
                    outputStream.write(fileReader, 0, read);
                    status.downloadedBytes += read;
                    
                    // Обновляем прогресс в UI
                    int progress = (int) ((status.downloadedBytes * 100) / status.totalBytes);
                    if (progress != status.progress) {
                        status.progress = progress;
                        holder.itemView.post(() -> notifyItemChanged(holder.getAdapterPosition()));
                    }
                }
                
                // Проверяем, успешно ли завершилась операция
                return status.downloadedBytes == status.totalBytes;
                
            } catch (IOException e) {
                return false;
            } finally {
                // Закрываем потоки
                if (inputStream != null) {
                    inputStream.close();
                }
                
                if (outputStream != null) {
                    outputStream.close();
                }
            }
        } catch (IOException e) {
            return false;
        }
    }
    
    // Форматирует размер файла в читаемый вид
    private String formatFileSize(long size) {
        if (size <= 0) return "0 Б";
        
        final String[] units = new String[] { "Б", "КБ", "МБ", "ГБ", "ТБ" };
        int digitGroups = (int) (Math.log10(size) / Math.log10(1024));
        
        // Проверяем, чтобы не выйти за границы массива
        if (digitGroups >= units.length) {
            digitGroups = units.length - 1;
        }
        
        // Форматируем число с одной цифрой после запятой
        DecimalFormat df = new DecimalFormat("#.#");
        return df.format(size / Math.pow(1024, digitGroups)) + " " + units[digitGroups];
    }

    // Открывает файл с помощью подходящего приложения
    private void openFile(Context context, String fileName) {
        try {
            File downloadsDir = Environment.getExternalStoragePublicDirectory(Environment.DIRECTORY_DOWNLOADS);
            File file = new File(downloadsDir, fileName);
            
            if (!file.exists()) {
                Toast.makeText(context, "Файл не найден", Toast.LENGTH_SHORT).show();
                return;
            }
            
            Intent intent = new Intent(Intent.ACTION_VIEW);
            Uri uri;
            
            // Для Android 7.0 (API 24) и выше используем FileProvider
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                uri = androidx.core.content.FileProvider.getUriForFile(
                    context, 
                    context.getApplicationContext().getPackageName() + ".provider",
                    file
                );
                intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
            } else {
                uri = Uri.fromFile(file);
            }
            
            // Определяем MIME-тип по расширению файла
            String mimeType = getMimeType(fileName);
            intent.setDataAndType(uri, mimeType);
            intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
            
            // Пытаемся открыть файл
            try {
                context.startActivity(intent);
            } catch (ActivityNotFoundException e) {
                Toast.makeText(context, "Не найдено приложение для открытия данного типа файлов", Toast.LENGTH_LONG).show();
            }
            
        } catch (Exception e) {
            Toast.makeText(context, "Ошибка открытия файла: " + e.getMessage(), Toast.LENGTH_SHORT).show();
        }
    }

    // Определяет MIME-тип на основе расширения файла
    private String getMimeType(String fileName) {
        String fileExtension = MimeTypeMap.getFileExtensionFromUrl(fileName);
        if (fileExtension != null) {
            return MimeTypeMap.getSingleton().getMimeTypeFromExtension(fileExtension.toLowerCase());
        }
        return "application/octet-stream"; // По умолчанию для неизвестных типов
    }

    // Метод для запуска загрузки файла
    private void startDownload(MessageViewHolder holder, int messageId, String fileName) {
        // Сохраняем контекст
        Context context = holder.itemView.getContext();
        
        // Создаем или обновляем статус загрузки
        final FileDownloadStatus statusFinal;
        FileDownloadStatus status = fileDownloadStatuses.get(messageId);
        if (status == null) {
            status = new FileDownloadStatus();
            fileDownloadStatuses.put(messageId, status);
        }
        statusFinal = status;
        
        // Если загрузка не в процессе, начинаем ее
        if (!statusFinal.isDownloading) {
            statusFinal.isDownloading = true;
            statusFinal.progress = 0;
            statusFinal.totalBytes = 0;
            statusFinal.downloadedBytes = 0;
            notifyItemChanged(holder.getAdapterPosition());
            
            // Начинаем загрузку
            String url = "api/Message/media/" + messageId;
            ApiClient.getApiService().downloadFile(url).enqueue(new Callback<ResponseBody>() {
                @Override
                public void onResponse(Call<ResponseBody> call, Response<ResponseBody> response) {
                    if (response.isSuccessful() && response.body() != null) {
                        // Запускаем сохранение файла в фоновом потоке
                        Thread thread = new Thread(() -> {
                            boolean successful = writeResponseBodyToDisk(response.body(), fileName, statusFinal, holder, messageId);
                            
                            if (successful) {
                                // Оповещаем пользователя о успешной загрузке
                                holder.itemView.post(() -> {
                                    FileDownloadStatus statusUpdate = fileDownloadStatuses.get(messageId);
                                    statusUpdate.isDownloading = false;
                                    statusUpdate.isComplete = true;
                                    notifyItemChanged(holder.getAdapterPosition());
                                    Toast.makeText(context, "Файл загружен: " + fileName, Toast.LENGTH_SHORT).show();
                                });
                            } else {
                                // Сообщаем об ошибке
                                holder.itemView.post(() -> {
                                    FileDownloadStatus statusUpdate = fileDownloadStatuses.get(messageId);
                                    statusUpdate.isDownloading = false;
                                    statusUpdate.isComplete = false;
                                    notifyItemChanged(holder.getAdapterPosition());
                                    Toast.makeText(context, "Ошибка при загрузке файла", Toast.LENGTH_SHORT).show();
                                });
                            }
                        });
                        thread.start();
                    } else {
                        FileDownloadStatus statusUpdate = fileDownloadStatuses.get(messageId);
                        statusUpdate.isDownloading = false;
                        notifyItemChanged(holder.getAdapterPosition());
                        Toast.makeText(context, "Ошибка загрузки файла", Toast.LENGTH_SHORT).show();
                    }
                }

                @Override
                public void onFailure(Call<ResponseBody> call, Throwable t) {
                    FileDownloadStatus statusUpdate = fileDownloadStatuses.get(messageId);
                    statusUpdate.isDownloading = false;
                    notifyItemChanged(holder.getAdapterPosition());
                    Toast.makeText(context, "Ошибка сети: " + t.getMessage(), Toast.LENGTH_SHORT).show();
                }
            });
        }
    }

    // Метод для отмены загрузки файла
    private void cancelDownload(int messageId) {
        FileDownloadStatus status = fileDownloadStatuses.get(messageId);
        if (status != null) {
            status.isDownloading = false;
            status.isComplete = false;
        }
        // Здесь можно было бы добавить логику для реальной отмены HTTP-запроса,
        // но в данном случае мы просто обновляем UI и полагаемся на GC для очистки потоков
    }

    @Override
    public int getItemCount() {
        return items.size();
    }

    // ViewHolder для разделителя даты
    static class DateDividerViewHolder extends RecyclerView.ViewHolder {
        TextView dateText;

        DateDividerViewHolder(@NonNull View itemView) {
            super(itemView);
            dateText = itemView.findViewById(R.id.date_divider_text);
        }
    }

    // ViewHolder для сообщений
    static class MessageViewHolder extends RecyclerView.ViewHolder {
        TextView messageText;
        TextView messageTime;
        ImageView messageImage;
        ProgressBar imageLoadingProgress;
        View attachmentContainer;
        View fileLayout;
        ImageView fileIcon;
        ImageView downloadIcon;
        ImageView cancelIcon;
        ProgressBar downloadProgressRing;
        TextView fileName;
        TextView fileSize;

        MessageViewHolder(@NonNull View itemView) {
            super(itemView);
            messageText = itemView.findViewById(R.id.message_text);
            messageTime = itemView.findViewById(R.id.message_time);
            messageImage = itemView.findViewById(R.id.message_image);
            imageLoadingProgress = itemView.findViewById(R.id.image_loading_progress);
            attachmentContainer = itemView.findViewById(R.id.attachment_container);
            fileLayout = itemView.findViewById(R.id.file_layout);
            
            if (fileLayout != null) {
                fileIcon = fileLayout.findViewById(R.id.file_icon);
                downloadIcon = fileLayout.findViewById(R.id.download_icon);
                cancelIcon = fileLayout.findViewById(R.id.cancel_icon);
                downloadProgressRing = fileLayout.findViewById(R.id.download_progress_ring);
                fileName = fileLayout.findViewById(R.id.file_name);
                fileSize = fileLayout.findViewById(R.id.file_size);
            }
        }
    }
    
    // Класс для хранения информации о загрузке файла
    private static class FileDownloadStatus {
        boolean isDownloading = false;
        boolean isComplete = false;
        long totalBytes = 0;
        long downloadedBytes = 0;
        int progress = 0;
    }

    // Добавляем новый метод для определения типа вложения
    private boolean isImageAttachment(MessageResponse msg) {
        // Проверяем тип вложения из API (если он есть)
        if (msg.getAttachmentType() != null) {
            String attachmentType = msg.getAttachmentType().toLowerCase();
            return attachmentType.contains("image");
        }
        
        // Если локальный URI - скорее всего это изображение
        if (msg.getLocalAttachmentUri() != null) {
            return true;
        }
        
        // Проверяем по расширению файла в тексте
        String text = msg.getText().toLowerCase();
        if (text.endsWith(".jpg") || text.endsWith(".jpeg") || 
            text.endsWith(".png") || text.endsWith(".gif") || 
            text.endsWith(".webp") || text.endsWith(".bmp")) {
            return true;
        }
        
        // По умолчанию считаем, что это может быть изображение
        // Попробуем загрузить как изображение, и только при ошибке загрузки покажем как файл
        return true;
    }

    /**
     * Предварительно загружает изображения для сообщений, 
     * которые еще не видны на экране
     */
    private void preloadImages() {
        // Запускаем загрузку в отдельном потоке, чтобы не блокировать UI
        new Thread(() -> {
            try {
                // Небольшая задержка, чтобы дать приоритет видимым изображениям
                Thread.sleep(300);
                
                // Предзагружаем только если есть контекст и сообщения
                Context context = getContext();
                if (context == null || originalMessages.isEmpty()) {
                    return;
                }
                
                // Берем только сообщения с вложениями
                List<MessageResponse> messagesWithAttachments = new ArrayList<>();
                for (MessageResponse msg : originalMessages) {
                    if (msg.hasAttachment()) {
                        messagesWithAttachments.add(msg);
                    }
                }
                
                // Начинаем с конца (новые сообщения)
                for (int i = messagesWithAttachments.size() - 1; i >= 0; i--) {
                    MessageResponse msg = messagesWithAttachments.get(i);
                    Object loadSource;
                    
                    if (msg.getLocalAttachmentUri() != null) {
                        loadSource = msg.getLocalAttachmentUri();
                    } else {
                        loadSource = ApiClient.BASE_URL + "api/Message/media/" + msg.getMessageId();
                    }
                    
                    // Предзагрузка с миниатюрами
                    Glide.with(context)
                        .load(loadSource)
                        .diskCacheStrategy(com.bumptech.glide.load.engine.DiskCacheStrategy.ALL)
                        .priority(com.bumptech.glide.Priority.LOW) // Низкий приоритет для предзагрузки
                        .thumbnail(0.1f) // Загружаем сначала очень маленькую версию
                        .preload(300, 300); // Предзагружаем уменьшенную версию
                }
            } catch (Exception e) {
                android.util.Log.e("ConversationAdapter", "Error preloading images", e);
            }
        }).start();
    }
    
    /**
     * Получает последний доступный контекст
     */
    private Context getContext() {
        try {
            if (appContext != null) {
                return appContext;
            } else if (convertView != null) {
                appContext = convertView.getContext();
                return appContext;
            } else if (!originalMessages.isEmpty()) {
                return appContext;
            }
            return null;
        } catch (Exception e) {
            return null;
        }
    }
    
    // Храним последний контекст
    private static Context appContext;
    private View convertView;
} 