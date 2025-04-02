package com.example.apk.admin;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.example.apk.R;
import com.example.apk.models.UserData;

import java.util.ArrayList;
import java.util.List;
import java.util.Locale;

/**
 * Адаптер для отображения списка пользователей
 */
public class UserAdapter extends RecyclerView.Adapter<UserAdapter.UserViewHolder> {

    private List<UserData> users;
    private List<UserData> filteredUsers;
    private OnUserClickListener listener;
    private UserData selectedUser;

    public UserAdapter() {
        this.users = new ArrayList<>();
        this.filteredUsers = new ArrayList<>();
    }

    @NonNull
    @Override
    public UserViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View view = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_user, parent, false);
        return new UserViewHolder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull UserViewHolder holder, int position) {
        UserData user = filteredUsers.get(position);
        holder.bind(user);
    }

    @Override
    public int getItemCount() {
        return filteredUsers.size();
    }

    public void updateUsers(List<UserData> users) {
        this.users = users;
        this.filteredUsers = new ArrayList<>(users);
        notifyDataSetChanged();
    }

    public void filter(String query) {
        filteredUsers.clear();

        if (query.isEmpty()) {
            filteredUsers.addAll(users);
        } else {
            String lowerCaseQuery = query.toLowerCase(Locale.getDefault());

            for (UserData user : users) {
                if (user.getUsername().toLowerCase(Locale.getDefault()).contains(lowerCaseQuery) ||
                        user.getRoleName().toLowerCase(Locale.getDefault()).contains(lowerCaseQuery) ||
                        String.valueOf(user.getUserId()).contains(lowerCaseQuery)) {
                    filteredUsers.add(user);
                }
            }
        }

        notifyDataSetChanged();
    }

    public UserData getUserAt(int position) {
        if (position >= 0 && position < filteredUsers.size()) {
            return filteredUsers.get(position);
        }
        return null;
    }

    public void setOnUserClickListener(OnUserClickListener listener) {
        this.listener = listener;
    }

    public void setSelectedUser(UserData user) {
        this.selectedUser = user;
        notifyDataSetChanged();
    }

    public interface OnUserClickListener {
        void onUserClick(UserData user, int position);
    }

    public class UserViewHolder extends RecyclerView.ViewHolder {
        private TextView usernameTextView;
        private TextView userIdTextView;
        private TextView roleTextView;
        private TextView initialTextView;

        public UserViewHolder(@NonNull View itemView) {
            super(itemView);
            usernameTextView = itemView.findViewById(R.id.usernameTextView);
            userIdTextView = itemView.findViewById(R.id.userIdTextView);
            roleTextView = itemView.findViewById(R.id.roleTextView);
            initialTextView = itemView.findViewById(R.id.initialTextView);

            itemView.setOnClickListener(v -> {
                int position = getAdapterPosition();
                if (position != RecyclerView.NO_POSITION && listener != null) {
                    listener.onUserClick(filteredUsers.get(position), position);
                }
            });
        }

        public void bind(UserData user) {
            usernameTextView.setText(user.getUsername());
            userIdTextView.setText(String.format(Locale.getDefault(), "ID: %d", user.getUserId()));
            roleTextView.setText(user.getRoleName());

            // Set initial letter
            String username = user.getUsername();
            if (username != null && !username.isEmpty()) {
                initialTextView.setText(username.substring(0, 1).toUpperCase());
            } else {
                initialTextView.setText("?");
            }

            // Highlight if selected
            itemView.setSelected(user.equals(selectedUser));
        }
    }
} 