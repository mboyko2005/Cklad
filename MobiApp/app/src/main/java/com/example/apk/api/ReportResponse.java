package com.example.apk.api;

import java.util.List;
import com.google.gson.annotations.SerializedName;

public class ReportResponse {
    @SerializedName("labels")
    private List<String> labels;
    
    @SerializedName("data")
    private List<Double> data;
    
    @SerializedName("title")
    private String title;
    
    @SerializedName("xTitle")
    private String xTitle;
    
    @SerializedName("yTitle")
    private String yTitle;

    public List<String> getLabels() {
        return labels;
    }

    public List<Double> getData() {
        return data;
    }

    public String getTitle() {
        return title;
    }

    public String getXTitle() {
        return xTitle;
    }

    public String getYTitle() {
        return yTitle;
    }
} 
 
 