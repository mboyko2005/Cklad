package com.example.apk.views;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.graphics.RectF;
import android.util.AttributeSet;
import android.view.MotionEvent;
import android.view.View;

public class CropView extends View {
    private Bitmap bitmap;
    private Paint borderPaint;
    private Paint overlayPaint;
    private RectF cropRect;

    private boolean isInitialized = false;
    private boolean isDragging = false;
    private boolean isResizing = false;
    private int touchCorner = -1;
    private float lastTouchX;
    private float lastTouchY;
    private float cornerSize = 40f;

    // Константы для определения угла, за который тянет пользователь
    private static final int TOP_LEFT = 0;
    private static final int TOP_RIGHT = 1;
    private static final int BOTTOM_LEFT = 2;
    private static final int BOTTOM_RIGHT = 3;
    private static final int NONE = -1;

    public CropView(Context context) {
        super(context);
        init();
    }

    public CropView(Context context, AttributeSet attrs) {
        super(context, attrs);
        init();
    }

    private void init() {
        borderPaint = new Paint();
        borderPaint.setColor(Color.WHITE);
        borderPaint.setStrokeWidth(3f);
        borderPaint.setStyle(Paint.Style.STROKE);

        overlayPaint = new Paint();
        overlayPaint.setColor(Color.parseColor("#80000000"));
        overlayPaint.setStyle(Paint.Style.FILL);
    }

    public void setImage(Bitmap bitmap) {
        this.bitmap = bitmap;
        isInitialized = false;
        invalidate();
    }

    private void initCropRect() {
        if (bitmap != null && !isInitialized) {
            float imageRatio = (float) bitmap.getWidth() / bitmap.getHeight();
            float viewRatio = (float) getWidth() / getHeight();

            int bitmapWidth = bitmap.getWidth();
            int bitmapHeight = bitmap.getHeight();
            
            if (imageRatio > viewRatio) {
                // Изображение шире, чем вид
                float scale = (float) getWidth() / bitmapWidth;
                float scaledHeight = bitmapHeight * scale;
                float marginTop = (getHeight() - scaledHeight) / 2;
                
                float cropSize = Math.min(getWidth(), scaledHeight) * 0.8f;
                float left = (getWidth() - cropSize) / 2;
                float top = marginTop + (scaledHeight - cropSize) / 2;
                
                cropRect = new RectF(left, top, left + cropSize, top + cropSize);
            } else {
                // Изображение выше, чем вид
                float scale = (float) getHeight() / bitmapHeight;
                float scaledWidth = bitmapWidth * scale;
                float marginLeft = (getWidth() - scaledWidth) / 2;
                
                float cropSize = Math.min(scaledWidth, getHeight()) * 0.8f;
                float left = marginLeft + (scaledWidth - cropSize) / 2;
                float top = (getHeight() - cropSize) / 2;
                
                cropRect = new RectF(left, top, left + cropSize, top + cropSize);
            }
            
            isInitialized = true;
        }
    }

    @Override
    protected void onDraw(Canvas canvas) {
        super.onDraw(canvas);
        
        if (bitmap != null) {
            initCropRect();
            
            // Отрисовка изображения
            float scale = Math.max((float) getWidth() / bitmap.getWidth(), 
                    (float) getHeight() / bitmap.getHeight());
            
            float scaledWidth = bitmap.getWidth() * scale;
            float scaledHeight = bitmap.getHeight() * scale;
            
            float left = (getWidth() - scaledWidth) / 2;
            float top = (getHeight() - scaledHeight) / 2;
            
            RectF destRect = new RectF(left, top, left + scaledWidth, top + scaledHeight);
            canvas.drawBitmap(bitmap, null, destRect, null);
            
            // Отрисовка затемнения вокруг области обрезки
            canvas.drawRect(0, 0, getWidth(), cropRect.top, overlayPaint);
            canvas.drawRect(0, cropRect.top, cropRect.left, cropRect.bottom, overlayPaint);
            canvas.drawRect(cropRect.right, cropRect.top, getWidth(), cropRect.bottom, overlayPaint);
            canvas.drawRect(0, cropRect.bottom, getWidth(), getHeight(), overlayPaint);
            
            // Отрисовка рамки области обрезки
            canvas.drawRect(cropRect, borderPaint);
            
            // Отрисовка маркеров по углам
            drawCornerMarker(canvas, cropRect.left, cropRect.top); // Верхний левый
            drawCornerMarker(canvas, cropRect.right, cropRect.top); // Верхний правый
            drawCornerMarker(canvas, cropRect.left, cropRect.bottom); // Нижний левый
            drawCornerMarker(canvas, cropRect.right, cropRect.bottom); // Нижний правый
        }
    }

    private void drawCornerMarker(Canvas canvas, float x, float y) {
        canvas.drawCircle(x, y, cornerSize / 2, borderPaint);
    }

    @Override
    public boolean onTouchEvent(MotionEvent event) {
        float x = event.getX();
        float y = event.getY();
        
        switch (event.getAction()) {
            case MotionEvent.ACTION_DOWN:
                touchCorner = getCornerAtPoint(x, y);
                
                if (touchCorner != NONE) {
                    isResizing = true;
                } else if (isPointInCropRect(x, y)) {
                    isDragging = true;
                }
                
                lastTouchX = x;
                lastTouchY = y;
                return true;
                
            case MotionEvent.ACTION_MOVE:
                if (isResizing) {
                    resizeCropRect(touchCorner, x, y);
                } else if (isDragging) {
                    float dx = x - lastTouchX;
                    float dy = y - lastTouchY;
                    
                    cropRect.offset(dx, dy);
                    
                    // Убедимся, что рамка не выходит за границы вида
                    if (cropRect.left < 0) cropRect.offset(-cropRect.left, 0);
                    if (cropRect.top < 0) cropRect.offset(0, -cropRect.top);
                    if (cropRect.right > getWidth()) cropRect.offset(getWidth() - cropRect.right, 0);
                    if (cropRect.bottom > getHeight()) cropRect.offset(0, getHeight() - cropRect.bottom);
                }
                
                lastTouchX = x;
                lastTouchY = y;
                invalidate();
                return true;
                
            case MotionEvent.ACTION_UP:
            case MotionEvent.ACTION_CANCEL:
                isDragging = false;
                isResizing = false;
                touchCorner = NONE;
                return true;
        }
        
        return false;
    }

    private boolean isPointInCropRect(float x, float y) {
        return cropRect.contains(x, y);
    }

    private int getCornerAtPoint(float x, float y) {
        float touchArea = cornerSize;
        
        if (Math.abs(x - cropRect.left) <= touchArea && Math.abs(y - cropRect.top) <= touchArea) {
            return TOP_LEFT;
        } else if (Math.abs(x - cropRect.right) <= touchArea && Math.abs(y - cropRect.top) <= touchArea) {
            return TOP_RIGHT;
        } else if (Math.abs(x - cropRect.left) <= touchArea && Math.abs(y - cropRect.bottom) <= touchArea) {
            return BOTTOM_LEFT;
        } else if (Math.abs(x - cropRect.right) <= touchArea && Math.abs(y - cropRect.bottom) <= touchArea) {
            return BOTTOM_RIGHT;
        }
        
        return NONE;
    }

    private void resizeCropRect(int corner, float x, float y) {
        float minSize = cornerSize * 2; // Минимальный размер рамки
        
        switch (corner) {
            case TOP_LEFT:
                cropRect.left = Math.min(cropRect.right - minSize, Math.max(0, x));
                cropRect.top = Math.min(cropRect.bottom - minSize, Math.max(0, y));
                break;
                
            case TOP_RIGHT:
                cropRect.right = Math.max(cropRect.left + minSize, Math.min(getWidth(), x));
                cropRect.top = Math.min(cropRect.bottom - minSize, Math.max(0, y));
                break;
                
            case BOTTOM_LEFT:
                cropRect.left = Math.min(cropRect.right - minSize, Math.max(0, x));
                cropRect.bottom = Math.max(cropRect.top + minSize, Math.min(getHeight(), y));
                break;
                
            case BOTTOM_RIGHT:
                cropRect.right = Math.max(cropRect.left + minSize, Math.min(getWidth(), x));
                cropRect.bottom = Math.max(cropRect.top + minSize, Math.min(getHeight(), y));
                break;
        }
    }

    public Bitmap getCroppedBitmap() {
        if (bitmap == null || cropRect == null) {
            return null;
        }
        
        // Перевести координаты из представления в координаты изображения
        float scale = Math.max((float) getWidth() / bitmap.getWidth(), 
                (float) getHeight() / bitmap.getHeight());
        
        float scaledWidth = bitmap.getWidth() * scale;
        float scaledHeight = bitmap.getHeight() * scale;
        
        float left = (getWidth() - scaledWidth) / 2;
        float top = (getHeight() - scaledHeight) / 2;
        
        // Координаты обрезки в системе координат изображения
        int cropX = Math.max(0, Math.round((cropRect.left - left) / scale));
        int cropY = Math.max(0, Math.round((cropRect.top - top) / scale));
        int cropWidth = Math.min(bitmap.getWidth() - cropX, Math.round(cropRect.width() / scale));
        int cropHeight = Math.min(bitmap.getHeight() - cropY, Math.round(cropRect.height() / scale));
        
        // Обрезка изображения
        return Bitmap.createBitmap(bitmap, cropX, cropY, cropWidth, cropHeight);
    }
} 