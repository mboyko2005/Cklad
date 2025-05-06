package com.example.apk.views;

import android.animation.Animator;
import android.animation.AnimatorListenerAdapter;
import android.animation.ValueAnimator;
import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.Matrix;
import android.graphics.drawable.BitmapDrawable;
import android.util.AttributeSet;
import android.view.MotionEvent;
import android.view.ScaleGestureDetector;
import android.view.View;
import android.view.animation.AccelerateDecelerateInterpolator;
import android.widget.FrameLayout;
import android.widget.ImageView;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

public class ImageViewer extends FrameLayout {
    private ImageView imageView;
    private ScaleGestureDetector scaleDetector;
    private float scale = 1.0f;
    private float focusX;
    private float focusY;
    private float lastTouchX;
    private float lastTouchY;
    private boolean isDragging = false;
    private OnImageEditListener editListener;

    public interface OnImageEditListener {
        void onImageEdited(Bitmap editedBitmap);
    }

    public ImageViewer(@NonNull Context context) {
        super(context);
        init();
    }

    public ImageViewer(@NonNull Context context, @Nullable AttributeSet attrs) {
        super(context, attrs);
        init();
    }

    private void init() {
        imageView = new ImageView(getContext());
        imageView.setScaleType(ImageView.ScaleType.FIT_CENTER);
        addView(imageView, new LayoutParams(LayoutParams.MATCH_PARENT, LayoutParams.MATCH_PARENT));

        scaleDetector = new ScaleGestureDetector(getContext(), new ScaleListener());
    }

    public void setImage(Bitmap bitmap) {
        imageView.setImageBitmap(bitmap);
        resetScale();
    }

    public void setOnImageEditListener(OnImageEditListener listener) {
        this.editListener = listener;
    }

    private void resetScale() {
        scale = 1.0f;
        imageView.setScaleX(scale);
        imageView.setScaleY(scale);
    }

    @Override
    public boolean onTouchEvent(MotionEvent event) {
        scaleDetector.onTouchEvent(event);

        switch (event.getAction()) {
            case MotionEvent.ACTION_DOWN:
                lastTouchX = event.getX();
                lastTouchY = event.getY();
                isDragging = true;
                break;

            case MotionEvent.ACTION_MOVE:
                if (isDragging) {
                    float dx = event.getX() - lastTouchX;
                    float dy = event.getY() - lastTouchY;
                    imageView.setTranslationX(imageView.getTranslationX() + dx);
                    imageView.setTranslationY(imageView.getTranslationY() + dy);
                    lastTouchX = event.getX();
                    lastTouchY = event.getY();
                }
                break;

            case MotionEvent.ACTION_UP:
            case MotionEvent.ACTION_CANCEL:
                isDragging = false;
                break;
        }
        return true;
    }

    private class ScaleListener extends ScaleGestureDetector.SimpleOnScaleGestureListener {
        @Override
        public boolean onScale(ScaleGestureDetector detector) {
            scale *= detector.getScaleFactor();
            scale = Math.max(0.1f, Math.min(scale, 5.0f));
            imageView.setScaleX(scale);
            imageView.setScaleY(scale);
            return true;
        }
    }

    public void showWithAnimation() {
        setAlpha(0f);
        setVisibility(View.VISIBLE);
        animate()
            .alpha(1f)
            .setDuration(300)
            .setInterpolator(new AccelerateDecelerateInterpolator())
            .start();
    }

    public void hideWithAnimation(final Runnable onComplete) {
        animate()
            .alpha(0f)
            .setDuration(300)
            .setInterpolator(new AccelerateDecelerateInterpolator())
            .setListener(new AnimatorListenerAdapter() {
                @Override
                public void onAnimationEnd(Animator animation) {
                    setVisibility(View.GONE);
                    if (onComplete != null) {
                        onComplete.run();
                    }
                }
            })
            .start();
    }

    public Bitmap getEditedBitmap() {
        if (imageView.getDrawable() instanceof BitmapDrawable) {
            return ((BitmapDrawable) imageView.getDrawable()).getBitmap();
        }
        return null;
    }
} 