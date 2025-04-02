package com.example.apk.adapters;

import android.content.Context;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageButton;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.example.apk.R;
import com.example.apk.api.BotUserResponse;

import java.util.List;

public class BotUserAdapter extends RecyclerView.Adapter<BotUserAdapter.BotUserViewHolder> {

    private final Context context;
    private final List<BotUserResponse> botUsers;
    private final BotUserItemClickListener listener;

    public BotUserAdapter(Context context, List<BotUserResponse> botUsers) {
        this.context = context;
        this.botUsers = botUsers;
        this.listener = (BotUserItemClickListener) context;
    }

    @NonNull
    @Override
    public BotUserViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View view = LayoutInflater.from(context).inflate(R.layout.item_bot_user, parent, false);
        return new BotUserViewHolder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull BotUserViewHolder holder, int position) {
        BotUserResponse botUser = botUsers.get(position);
        
        holder.telegramIdTextView.setText(String.format("Telegram ID: %d", botUser.getTelegramId()));
        holder.roleTextView.setText(String.format("Роль: %s", botUser.getRole()));
        
        holder.editButton.setOnClickListener(v -> listener.onEditClick(botUser));
        holder.deleteButton.setOnClickListener(v -> listener.onDeleteClick(botUser));
    }

    @Override
    public int getItemCount() {
        return botUsers.size();
    }

    public static class BotUserViewHolder extends RecyclerView.ViewHolder {
        TextView telegramIdTextView;
        TextView roleTextView;
        ImageButton editButton;
        ImageButton deleteButton;

        public BotUserViewHolder(@NonNull View itemView) {
            super(itemView);
            telegramIdTextView = itemView.findViewById(R.id.telegramIdTextView);
            roleTextView = itemView.findViewById(R.id.roleTextView);
            editButton = itemView.findViewById(R.id.editButton);
            deleteButton = itemView.findViewById(R.id.deleteButton);
        }
    }

    public interface BotUserItemClickListener {
        void onEditClick(BotUserResponse botUser);
        void onDeleteClick(BotUserResponse botUser);
    }
<<<<<<< HEAD
=======
} 
 

import android.content.Context;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageButton;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.example.apk.R;
import com.example.apk.api.BotUserResponse;

import java.util.List;

public class BotUserAdapter extends RecyclerView.Adapter<BotUserAdapter.BotUserViewHolder> {

    private final Context context;
    private final List<BotUserResponse> botUsers;
    private final BotUserItemClickListener listener;

    public BotUserAdapter(Context context, List<BotUserResponse> botUsers) {
        this.context = context;
        this.botUsers = botUsers;
        this.listener = (BotUserItemClickListener) context;
    }

    @NonNull
    @Override
    public BotUserViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View view = LayoutInflater.from(context).inflate(R.layout.item_bot_user, parent, false);
        return new BotUserViewHolder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull BotUserViewHolder holder, int position) {
        BotUserResponse botUser = botUsers.get(position);
        
        holder.telegramIdTextView.setText(String.format("Telegram ID: %d", botUser.getTelegramId()));
        holder.roleTextView.setText(String.format("Роль: %s", botUser.getRole()));
        
        holder.editButton.setOnClickListener(v -> listener.onEditClick(botUser));
        holder.deleteButton.setOnClickListener(v -> listener.onDeleteClick(botUser));
    }

    @Override
    public int getItemCount() {
        return botUsers.size();
    }

    public static class BotUserViewHolder extends RecyclerView.ViewHolder {
        TextView telegramIdTextView;
        TextView roleTextView;
        ImageButton editButton;
        ImageButton deleteButton;

        public BotUserViewHolder(@NonNull View itemView) {
            super(itemView);
            telegramIdTextView = itemView.findViewById(R.id.telegramIdTextView);
            roleTextView = itemView.findViewById(R.id.roleTextView);
            editButton = itemView.findViewById(R.id.editButton);
            deleteButton = itemView.findViewById(R.id.deleteButton);
        }
    }

    public interface BotUserItemClickListener {
        void onEditClick(BotUserResponse botUser);
        void onDeleteClick(BotUserResponse botUser);
    }
>>>>>>> 1dea6d5621f4f889dffd0814aeeb98a9d2d0ba87
} 