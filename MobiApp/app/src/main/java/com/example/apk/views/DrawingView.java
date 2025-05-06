package com.example.apk.views;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.graphics.Path;
import android.util.AttributeSet;
import android.view.MotionEvent;
import android.view.View;

import java.util.ArrayList;
import java.util.List;

public class DrawingView extends View {
    private Bitmap bitmap;
    private Canvas canvas;
    private Paint paint;
    private List<PathWithParams> paths;
    private PathWithParams currentPath;
    private float currentX;
    private float currentY;
    private int currentColor = Color.RED;
    private float strokeWidth = 5f;

    // Класс для хранения пути и его параметров
    private static class PathWithParams {
        Path path;
        int color;
        float strokeWidth;
        
        PathWithParams(Path path, int color, float strokeWidth) {
            this.path = path;
            this.color = color;
            this.strokeWidth = strokeWidth;
        }
    }

    public DrawingView(Context context) {
        super(context);
        init();
    }

    public DrawingView(Context context, AttributeSet attrs) {
        super(context, attrs);
        init();
    }

    private void init() {
        paint = new Paint();
        paint.setAntiAlias(true);
        paint.setDither(true);
        paint.setStyle(Paint.Style.STROKE);
        paint.setStrokeJoin(Paint.Join.ROUND);
        paint.setStrokeCap(Paint.Cap.ROUND);
        paint.setStrokeWidth(strokeWidth);
        paint.setColor(currentColor);

        paths = new ArrayList<>();
        currentPath = null;
    }

    public void setImage(Bitmap bitmap) {
        this.bitmap = bitmap.copy(Bitmap.Config.ARGB_8888, true);
        canvas = new Canvas(this.bitmap);
        invalidate();
    }

    public void setColor(int color) {
        currentColor = color;
    }

    public void setStrokeWidth(float width) {
        strokeWidth = width;
    }

    public void clear() {
        paths.clear();
        if (bitmap != null) {
            bitmap.eraseColor(Color.TRANSPARENT);
            canvas = new Canvas(bitmap);
        }
        invalidate();
    }

    public Bitmap getBitmap() {
        if (bitmap == null) return null;
        
        // Создаем новый Bitmap для финального результата
        Bitmap resultBitmap = bitmap.copy(bitmap.getConfig(), true);
        Canvas resultCanvas = new Canvas(resultBitmap);
        
        // Отрисовываем все сохраненные пути на итоговом Bitmap
        Paint pathPaint = new Paint(paint);
        for (PathWithParams pathWithParams : paths) {
            pathPaint.setColor(pathWithParams.color);
            pathPaint.setStrokeWidth(pathWithParams.strokeWidth);
            resultCanvas.drawPath(pathWithParams.path, pathPaint);
        }
        
        return resultBitmap;
    }

    @Override
    protected void onSizeChanged(int w, int h, int oldw, int oldh) {
        super.onSizeChanged(w, h, oldw, oldh);
        
        // Если размер изменился, нужно пересоздать Bitmap рисования
        if (bitmap == null) {
            bitmap = Bitmap.createBitmap(w, h, Bitmap.Config.ARGB_8888);
            canvas = new Canvas(bitmap);
        } else if (w > 0 && h > 0) {
            Bitmap scaledBitmap = Bitmap.createScaledBitmap(bitmap, w, h, true);
            bitmap = scaledBitmap.copy(Bitmap.Config.ARGB_8888, true);
            canvas = new Canvas(bitmap);
        }
    }

    @Override
    protected void onDraw(Canvas canvas) {
        super.onDraw(canvas);
        
        // Отрисовываем базовое изображение
        if (bitmap != null) {
            canvas.drawBitmap(bitmap, 0, 0, null);
        }
        
        // Отрисовываем все пути
        Paint pathPaint = new Paint(paint);
        for (PathWithParams pathWithParams : paths) {
            pathPaint.setColor(pathWithParams.color);
            pathPaint.setStrokeWidth(pathWithParams.strokeWidth);
            canvas.drawPath(pathWithParams.path, pathPaint);
        }
    }

    @Override
    public boolean onTouchEvent(MotionEvent event) {
        float x = event.getX();
        float y = event.getY();

        switch (event.getAction()) {
            case MotionEvent.ACTION_DOWN:
                // Начинаем новый путь
                Path path = new Path();
                path.moveTo(x, y);
                currentPath = new PathWithParams(path, currentColor, strokeWidth);
                paths.add(currentPath);
                currentX = x;
                currentY = y;
                break;

            case MotionEvent.ACTION_MOVE:
                if (currentPath != null) {
                    // Используем квадратичную кривую для более плавного рисования
                    currentPath.path.quadTo(currentX, currentY, (x + currentX) / 2, (y + currentY) / 2);
                    currentX = x;
                    currentY = y;
                }
                break;

            case MotionEvent.ACTION_UP:
                if (currentPath != null) {
                    // Завершаем линию в текущей точке
                    currentPath.path.lineTo(x, y);
                    // Сбрасываем текущий путь
                    currentPath = null;
                }
                break;
        }

        invalidate();
        return true;
    }
} 