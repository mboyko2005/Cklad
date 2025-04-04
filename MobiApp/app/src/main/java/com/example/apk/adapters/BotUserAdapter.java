package com.example.apk.adapters;

import android.content.Context;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageButton;
import android.widget.ImageView;
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
        
        holder.botUserNameTextView.setText(String.format("%d", botUser.getTelegramId()));
        holder.botUserIdTextView.setText(String.format("ID: %d", botUser.getTelegramId()));
        holder.botUserStatusTextView.setText(botUser.getRole());
        
        String idString = String.valueOf(botUser.getTelegramId());
        String initial = idString.length() > 0 ? String.valueOf(idString.charAt(0)) : "Б";
        holder.botAvatarTextView.setText(initial);
        
        if (holder.botAvatarBackground != null && 
            holder.botAvatarBackground.getBackground() instanceof android.graphics.drawable.AnimationDrawable) {
            android.graphics.drawable.AnimationDrawable animationDrawable = 
                (android.graphics.drawable.AnimationDrawable) holder.botAvatarBackground.getBackground();
            animationDrawable.start();
        }
        
        holder.editButton.setOnClickListener(v -> {
            listener.onEditClick(botUser);
        });
        
        holder.deleteButton.setOnClickListener(v -> {
            listener.onDeleteClick(botUser);
        });
        
        holder.itemView.setOnClickListener(v -> {
            listener.onEditClick(botUser);
        });
    }

    @Override
    public int getItemCount() {
        return botUsers.size();
    }

    public static class BotUserViewHolder extends RecyclerView.ViewHolder {
        TextView botUserNameTextView;
        TextView botUserIdTextView;
        TextView botUserStatusTextView;
        TextView botAvatarTextView;
        ImageView botAvatarBackground;
        ImageButton editButton;
        ImageButton deleteButton;

        public BotUserViewHolder(@NonNull View itemView) {
            super(itemView);
            botUserNameTextView = itemView.findViewById(R.id.botUserName);
            botUserIdTextView = itemView.findViewById(R.id.botUserId);
            botUserStatusTextView = itemView.findViewById(R.id.botUserStatus);
            botAvatarTextView = itemView.findViewById(R.id.botAvatarText);
            botAvatarBackground = itemView.findViewById(R.id.botAvatarBackground);
            editButton = itemView.findViewById(R.id.botUserEditButton);
            deleteButton = itemView.findViewById(R.id.botUserDeleteButton);
        }
    }

    public interface BotUserItemClickListener {
        void onEditClick(BotUserResponse botUser);
        void onDeleteClick(BotUserResponse botUser);
    }
} 