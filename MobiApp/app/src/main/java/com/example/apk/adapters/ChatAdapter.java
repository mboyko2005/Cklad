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
import com.example.apk.models.ChatItem;

import java.util.ArrayList;
import java.util.List;
import java.util.Random;

public class ChatAdapter extends RecyclerView.Adapter<ChatAdapter.ChatViewHolder> {
    private final Context context;
    private List<ChatItem> chatList;
    private List<ChatItem> fullChatList; // Полный список для фильтрации
    private final Random random = new Random();
    private OnChatClickListener clickListener;

    public ChatAdapter(Context context, List<ChatItem> chatList) {
        this.context = context;
        this.chatList = new ArrayList<>(chatList);
        this.fullChatList = new ArrayList<>(chatList);
    }

    /**
     * Устанавливает слушатель клика по чату
     */
    public void setOnChatClickListener(OnChatClickListener listener) {
        this.clickListener = listener;
    }

    @NonNull
    @Override
    public ChatViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View view = LayoutInflater.from(context).inflate(R.layout.item_chat, parent, false);
        return new ChatViewHolder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull ChatViewHolder holder, int position) {
        ChatItem item = chatList.get(position);
        holder.chatName.setText(item.getChatName());
        holder.lastMessage.setText(item.getLastMessage());

        String name = item.getChatName();
        String first = (name != null && !name.isEmpty())
                ? name.substring(0, 1).toUpperCase()
                : "?";
        holder.avatarText.setText(first);

        if (holder.avatarBackground.getBackground() instanceof AnimationDrawable) {
            AnimationDrawable anim = (AnimationDrawable) holder.avatarBackground.getBackground();
            if (!anim.isRunning()) {
                holder.avatarBackground.postDelayed(() -> {
                    anim.setEnterFadeDuration(1000);
                    anim.setExitFadeDuration(1000);
                    anim.start();
                }, position * 100 % 500);
            }
        }

        // Отображение счетчика непрочитанных сообщений
        if (item.getUnreadCount() > 0) {
            holder.unreadCountTextView.setVisibility(View.VISIBLE);
            holder.unreadCountTextView.setText(String.valueOf(item.getUnreadCount()));
        } else {
            holder.unreadCountTextView.setVisibility(View.GONE);
        }

        // Обработка клика по элементу
        holder.itemView.setOnClickListener(v -> {
            if (clickListener != null) {
                try {
                    int partnerId = Integer.parseInt(item.getChatId());
                    clickListener.onChatClick(partnerId, item.getChatName());
                } catch (NumberFormatException e) {
                    // Неверный формат ID
                }
            }
        });
    }

    @Override
    public int getItemCount() {
        return chatList.size();
    }

    /**
     * Обновление списка чатов
     */
    public void updateChats(List<ChatItem> chats) {
        this.chatList = new ArrayList<>(chats);
        this.fullChatList = new ArrayList<>(chats);
        notifyDataSetChanged();
    }

    /**
     * Фильтрация чатов по поисковому запросу
     * @param query запрос для поиска
     */
    public void filter(String query) {
        if (query == null || query.isEmpty()) {
            // Если запрос пустой, восстанавливаем полный список
            chatList = new ArrayList<>(fullChatList);
        } else {
            String lowerCaseQuery = query.toLowerCase();
            chatList = new ArrayList<>();
            
            for (ChatItem item : fullChatList) {
                // Ищем по имени чата и последнему сообщению
                if (item.getChatName().toLowerCase().contains(lowerCaseQuery) || 
                    item.getLastMessage().toLowerCase().contains(lowerCaseQuery)) {
                    chatList.add(item);
                }
            }
        }
        notifyDataSetChanged();
    }

    static class ChatViewHolder extends RecyclerView.ViewHolder {
        TextView chatName;
        TextView lastMessage;
        ImageView avatarBackground;
        TextView avatarText;
        TextView unreadCountTextView;

        ChatViewHolder(@NonNull View itemView) {
            super(itemView);
            chatName = itemView.findViewById(R.id.chat_name);
            lastMessage = itemView.findViewById(R.id.last_message);
            avatarBackground = itemView.findViewById(R.id.avatarBackground);
            avatarText = itemView.findViewById(R.id.avatarText);
            unreadCountTextView = itemView.findViewById(R.id.unreadCountTextView);
        }
    }

    /**
     * Интерфейс слушателя клика по чату
     */
    public interface OnChatClickListener {
        void onChatClick(int partnerId, String chatName);
    }
} 