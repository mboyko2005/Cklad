package com.example.apk.views;

import android.content.Context;
import android.graphics.Rect;
import android.text.TextUtils;
import android.util.AttributeSet;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import androidx.appcompat.widget.SearchView;

import java.lang.reflect.Field;

/**
 * Кастомный SearchView с выровненным по левому краю текстом поиска и иконкой
 */
public class CenteredSearchView extends SearchView {
    
    public CenteredSearchView(Context context) {
        super(context);
        adjustSearchTextLayout();
    }

    public CenteredSearchView(Context context, AttributeSet attrs) {
        super(context, attrs);
        adjustSearchTextLayout();
    }

    public CenteredSearchView(Context context, AttributeSet attrs, int defStyleAttr) {
        super(context, attrs, defStyleAttr);
        adjustSearchTextLayout();
    }

    /**
     * Настраивает положение текста и иконки в поисковом поле
     */
    private void adjustSearchTextLayout() {
        try {
            // Получаем приватное поле mSearchSrcTextView через рефлексию
            Field searchField = SearchView.class.getDeclaredField("mSearchSrcTextView");
            searchField.setAccessible(true);
            TextView searchTextView = (TextView) searchField.get(this);
            
            if (searchTextView != null) {
                // Устанавливаем выравнивание по левому краю
                searchTextView.setGravity(Gravity.CENTER_VERTICAL | Gravity.START);
                searchTextView.setTextAlignment(TEXT_ALIGNMENT_VIEW_START);
                
                // Уменьшаем отступ между иконкой и текстом
                Rect padding = new Rect();
                searchTextView.getBackground().getPadding(padding);
                searchTextView.setPadding(padding.left + 8, padding.top, padding.right, padding.bottom);
                
                // Максимальная длина текста и поведение многоточия
                searchTextView.setEllipsize(TextUtils.TruncateAt.END);
                searchTextView.setSingleLine(true);
                
                // Настраиваем цвет текста (возьмёт из атрибутов XML)
                // searchTextView.setHintTextColor - устанавливается через XML атрибуты
                // searchTextView.setTextColor - устанавливается через XML атрибуты
            }
            
            // Делаем, чтобы значок поиска был ближе к тексту
            int searchIconId = getContext().getResources().getIdentifier(
                    "android:id/search_mag_icon", null, null);
            View searchIcon = findViewById(searchIconId);
            if (searchIcon != null) {
                ViewGroup.LayoutParams layoutParams = searchIcon.getLayoutParams();
                if (layoutParams instanceof ViewGroup.MarginLayoutParams) {
                    ViewGroup.MarginLayoutParams marginParams = (ViewGroup.MarginLayoutParams) layoutParams;
                    marginParams.leftMargin = 2; // Уменьшаем отступ слева
                    searchIcon.setLayoutParams(marginParams);
                }
            }
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
} 