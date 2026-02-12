using System;
using System.Collections.Generic;
using UnityEngine;

namespace Iris.Settings
{
    public class IrisSettings
    {
        public bool enableFilters = true;

        // 启用的外部 Shader 列表（仅名称）
        public List<string> enabledFilters = new();

        // 外部 Shader 的配置列表
        public List<FilterConfig> filterConfigs = new();
    }

    public class FilterConfig
    {
        public string id = ""; // Format: "SourceFile:ShaderName"
        public string name = ""; // Display name
        public bool enabled = false;
        public List<FilterParam> paramsList = new();
    }

    public class FilterParam
    {
        public string name = "";
        public float[] values = new float[4]; // Support for Float (v[0]), Vector/Color (v[0-3])
        public string type = "Float";
    }

    public class UISettings
    {
        public bool showWatermark = true;
    }

    public enum SkinMode
    {
        SingleGlobal,
        PerScene,
        Slideshow
    }

    public class AppearanceSettings
    {
        public bool enableMenuSkin = false;
        public SkinMode mode = SkinMode.SingleGlobal;

        public SkinConfig globalSkin = new();

        public SkinConfig mainUISkin = new();
        public SkinConfig clsSkin = new();
        public SkinConfig dlcUISkin = new();

        public float slideDuration = 30f;
        public int slideshowCount = 3;
        public SkinConfig[] slideshowSkins = new SkinConfig[20];

        public bool enableTrackCustomization = false;
        public Color trackColor = Color.white;
        public float trackBrightness = 1f;
        public float trackOpacity = 1f;
        public bool trackColorR = true;
        public bool trackColorG = true;
        public bool trackColorB = true;

        public AppearanceSettings()
        {
            for (int i = 0; i < 20; i++)
            {
                slideshowSkins[i] = new SkinConfig();
            }
        }

        public void EnsureSlideshowSize()
        {
            slideshowCount = (int)Mathf.Clamp(slideshowCount, 1, 20);
        }
    }

    public class SkinConfig
    {
        public string path = "";
        public float scale = 1f;
        public float offsetX = 0f;
        public float offsetY = 0f;
        public float opacity = 1f;
        public float brightness = 1f;
        public float saturation = 1f;
        public float contrast = 1f;
        public float hue = 0f;
        public float playbackSpeed = 1f;
    }
}