package com.example.apk.api;

import com.google.gson.annotations.SerializedName;
import android.net.Uri;
import android.graphics.Bitmap;

public class MessageResponse {
    @SerializedName("messageId")
    private int messageId;

    @SerializedName("senderId")
    private int senderId;

    @SerializedName("receiverId")
    private int receiverId;

    @SerializedName("text")
    private String text;

    @SerializedName("timestamp")
    private String timestamp;

    @SerializedName("isRead")
    private boolean isRead;

    @SerializedName("hasAttachment")
    private boolean hasAttachment;

    @SerializedName("type")
    private String attachmentType;

    private transient Uri localAttachmentUri = null;
    private transient Bitmap editedBitmap = null;

    public int getMessageId() {
        return messageId;
    }

    public int getSenderId() {
        return senderId;
    }

    public int getReceiverId() {
        return receiverId;
    }

    public String getText() {
        return text;
    }

    public String getTimestamp() {
        return timestamp;
    }

    public boolean isRead() {
        return isRead;
    }

    public boolean hasAttachment() {
        return hasAttachment;
    }
    
    public String getAttachmentType() {
        return attachmentType;
    }
    
    public void setAttachmentType(String attachmentType) {
        this.attachmentType = attachmentType;
    }

    public Uri getLocalAttachmentUri() {
        return localAttachmentUri;
    }

    public void setLocalAttachmentUri(Uri localAttachmentUri) {
        this.localAttachmentUri = localAttachmentUri;
    }

    public void setHasAttachment(boolean hasAttachment) {
        this.hasAttachment = hasAttachment;
    }

    public Bitmap getEditedBitmap() {
        return editedBitmap;
    }

    public void setEditedBitmap(Bitmap editedBitmap) {
        this.editedBitmap = editedBitmap;
    }
} 