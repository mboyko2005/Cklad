package com.example.apk.dialogs;

import android.app.Dialog;
import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.Color;
import android.graphics.Matrix;
import android.os.Bundle;
import android.view.View;
import android.view.Window;
import android.widget.ImageButton;
import android.widget.LinearLayout;

import androidx.annotation.NonNull;

import com.example.apk.R;
import com.example.apk.views.CropView;
import com.example.apk.views.DrawingView;
import com.example.apk.views.ImageViewer;

public class ImageEditorDialog extends Dialog {
    private ImageViewer imageViewer;
    private DrawingView drawingView;
    private CropView cropView;
    private Bitmap originalBitmap;
    private OnImageEditListener editListener;
    private boolean isDrawingMode = false;
    private boolean isCropMode = false;

    // Основные меню
    private LinearLayout mainTools;
    private LinearLayout drawingTools;
    private LinearLayout cropTools;

    public interface OnImageEditListener {
        void onImageEdited(Bitmap editedBitmap);
    }

    public ImageEditorDialog(@NonNull Context context, Bitmap bitmap, OnImageEditListener listener) {
        super(context);
        this.originalBitmap = bitmap;
        this.editListener = listener;
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        requestWindowFeature(Window.FEATURE_NO_TITLE);
        setContentView(R.layout.image_viewer_layout);

        // Инициализация представлений
        imageViewer = findViewById(R.id.image_viewer);
        drawingView = findViewById(R.id.drawing_view);
        cropView = findViewById(R.id.crop_view);
        
        mainTools = findViewById(R.id.main_tools);
        drawingTools = findViewById(R.id.drawing_tools);
        cropTools = findViewById(R.id.crop_tools);
        
        imageViewer.setImage(originalBitmap);

        // Основные кнопки
        ImageButton btnRotate = findViewById(R.id.btn_rotate);
        ImageButton btnCrop = findViewById(R.id.btn_crop);
        ImageButton btnDraw = findViewById(R.id.btn_draw);
        ImageButton btnSave = findViewById(R.id.btn_save);

        btnRotate.setOnClickListener(v -> rotateImage());
        btnCrop.setOnClickListener(v -> toggleCropMode());
        btnDraw.setOnClickListener(v -> toggleDrawingMode());
        btnSave.setOnClickListener(v -> saveImage());

        // Кнопки обрезки
        ImageButton btnCancelCrop = findViewById(R.id.btn_cancel_crop);
        ImageButton btnApplyCrop = findViewById(R.id.btn_apply_crop);
        
        btnCancelCrop.setOnClickListener(v -> cancelCrop());
        btnApplyCrop.setOnClickListener(v -> applyCrop());

        // Кнопки выбора цвета
        ImageButton btnRed = findViewById(R.id.btn_color_red);
        ImageButton btnGreen = findViewById(R.id.btn_color_green);
        ImageButton btnBlue = findViewById(R.id.btn_color_blue);
        ImageButton btnBlack = findViewById(R.id.btn_color_black);
        ImageButton btnWhite = findViewById(R.id.btn_color_white);
        ImageButton btnYellow = findViewById(R.id.btn_color_yellow);
        ImageButton btnOrange = findViewById(R.id.btn_color_orange);
        ImageButton btnPurple = findViewById(R.id.btn_color_purple);
        ImageButton btnPink = findViewById(R.id.btn_color_pink);
        ImageButton btnBrown = findViewById(R.id.btn_color_brown);

        btnRed.setOnClickListener(v -> setDrawingColor(getContext().getResources().getColor(R.color.drawing_red)));
        btnGreen.setOnClickListener(v -> setDrawingColor(getContext().getResources().getColor(R.color.drawing_green)));
        btnBlue.setOnClickListener(v -> setDrawingColor(getContext().getResources().getColor(R.color.drawing_blue)));
        btnBlack.setOnClickListener(v -> setDrawingColor(getContext().getResources().getColor(R.color.drawing_black)));
        btnWhite.setOnClickListener(v -> setDrawingColor(Color.WHITE));
        btnYellow.setOnClickListener(v -> setDrawingColor(Color.parseColor("#FFEB3B")));
        btnOrange.setOnClickListener(v -> setDrawingColor(Color.parseColor("#FF9800")));
        btnPurple.setOnClickListener(v -> setDrawingColor(Color.parseColor("#9C27B0")));
        btnPink.setOnClickListener(v -> setDrawingColor(Color.parseColor("#E91E63")));
        btnBrown.setOnClickListener(v -> setDrawingColor(Color.parseColor("#795548")));

        // Кнопки изменения толщины линии
        ImageButton btnThin = findViewById(R.id.btn_stroke_thin);
        ImageButton btnMedium = findViewById(R.id.btn_stroke_medium);
        ImageButton btnThick = findViewById(R.id.btn_stroke_thick);

        btnThin.setOnClickListener(v -> drawingView.setStrokeWidth(3f));
        btnMedium.setOnClickListener(v -> drawingView.setStrokeWidth(8f));
        btnThick.setOnClickListener(v -> drawingView.setStrokeWidth(15f));

        // Кнопка очистки
        ImageButton btnClear = findViewById(R.id.btn_clear);
        btnClear.setOnClickListener(v -> drawingView.clear());

        // Показываем анимацию появления
        imageViewer.showWithAnimation();
    }

    private void setDrawingColor(int color) {
        drawingView.setColor(color);
    }

    private void rotateImage() {
        // Если мы в режиме рисования или обрезки, сначала выходим из них
        if (isDrawingMode) {
            toggleDrawingMode();
        } else if (isCropMode) {
            cancelCrop();
        }
        
        Matrix matrix = new Matrix();
        matrix.postRotate(90);
        Bitmap rotatedBitmap = Bitmap.createBitmap(
            originalBitmap, 0, 0, 
            originalBitmap.getWidth(), 
            originalBitmap.getHeight(), 
            matrix, true
        );
        imageViewer.setImage(rotatedBitmap);
        originalBitmap = rotatedBitmap;
    }

    private void toggleCropMode() {
        // Если мы в режиме рисования, выходим из него
        if (isDrawingMode) {
            toggleDrawingMode();
        }
        
        isCropMode = !isCropMode;
        
        if (isCropMode) {
            // Переключаемся в режим обрезки
            cropView.setVisibility(View.VISIBLE);
            cropView.setImage(originalBitmap);
            imageViewer.setVisibility(View.GONE);
            
            mainTools.setVisibility(View.GONE);
            cropTools.setVisibility(View.VISIBLE);
        } else {
            // Выходим из режима обрезки
            cropView.setVisibility(View.GONE);
            imageViewer.setVisibility(View.VISIBLE);
            
            mainTools.setVisibility(View.VISIBLE);
            cropTools.setVisibility(View.GONE);
        }
    }

    private void cancelCrop() {
        toggleCropMode();
    }

    private void applyCrop() {
        if (cropView != null) {
            Bitmap croppedBitmap = cropView.getCroppedBitmap();
            if (croppedBitmap != null) {
                originalBitmap = croppedBitmap;
                imageViewer.setImage(originalBitmap);
            }
        }
        toggleCropMode();
    }

    private void toggleDrawingMode() {
        // Если мы в режиме обрезки, выходим из него
        if (isCropMode) {
            cancelCrop();
        }
        
        isDrawingMode = !isDrawingMode;
        
        if (isDrawingMode) {
            // Переключаемся в режим рисования
            drawingTools.setVisibility(View.VISIBLE);
            imageViewer.setVisibility(View.GONE);
            drawingView.setVisibility(View.VISIBLE);
            drawingView.setImage(originalBitmap);
            
            mainTools.setVisibility(View.GONE);
        } else {
            // Выходим из режима рисования
            drawingTools.setVisibility(View.GONE);
            imageViewer.setVisibility(View.VISIBLE);
            drawingView.setVisibility(View.GONE);
            
            mainTools.setVisibility(View.VISIBLE);
            
            // Сохраняем изменения из режима рисования
            if (drawingView.getBitmap() != null) {
                originalBitmap = drawingView.getBitmap();
                imageViewer.setImage(originalBitmap);
            }
        }
    }

    private void saveImage() {
        // Если мы в режиме рисования или обрезки, сначала выходим из них
        if (isDrawingMode) {
            toggleDrawingMode();
        } else if (isCropMode) {
            cancelCrop();
        }
        
        if (editListener != null) {
            editListener.onImageEdited(originalBitmap);
        }
        dismiss();
    }

    @Override
    public void dismiss() {
        imageViewer.hideWithAnimation(super::dismiss);
    }
} 