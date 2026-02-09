using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Iris.Settings
{
    public class KeyviewerConfig
    {
        [JsonProperty("name")] public string Name { get; set; } = "";
        [JsonProperty("containerPosition")] public Vector2 ContainerPosition { get; set; } = Vector2.zero;
        [JsonProperty("keys")] public List<KeyConfig> Keys { get; set; } = new();
        
        [JsonProperty("globalScale")] public float GlobalScale { get; set; } = 1.0f;
        [JsonProperty("globalOpacity")] public float GlobalOpacity { get; set; } = 1.0f;
        [JsonProperty("globalBorderRadius")] public float GlobalBorderRadius { get; set; } = 8f;
        [JsonProperty("gapLength")] public float GapLength { get; set; } = 5f;
        
        [JsonProperty("rainingKeySpeed")] public float RainingKeySpeed { get; set; } = 100f;
        [JsonProperty("rainingKeyThreshold")] public float RainingKeyThreshold { get; set; } = 500f;
    }

    public class KeyConfig
    {
        [JsonProperty("label")] public string Label { get; set; } = "";
        [JsonProperty("keyCode")] public KeyCode KeyCode { get; set; } = KeyCode.None;
        [JsonProperty("position")] public Vector2 Position { get; set; } = Vector2.zero;
        [JsonProperty("size")] public Vector2 Size { get; set; } = new Vector2(50, 50);
        
        [JsonProperty("foregroundColor")] public Color ForegroundColor { get; set; } = Color.white;
        [JsonProperty("backgroundColor")] public Color BackgroundColor { get; set; } = new Color(0, 0, 0, 0.5f);
        [JsonProperty("borderWidth")] public float BorderWidth { get; set; } = 1f;
        [JsonProperty("borderColor")] public Color BorderColor { get; set; } = Color.white;
        [JsonProperty("borderRadius")] public float BorderRadius { get; set; } = -1f;
        
        [JsonProperty("fontName")] public string FontName { get; set; } = "Arial";
        [JsonProperty("fontSize")] public int FontSize { get; set; } = 20;
        [JsonProperty("backgroundImage")] public string BackgroundImage { get; set; } = "";
        [JsonProperty("showRainingKey")] public bool ShowRainingKey { get; set; } = false;

        // 按下时的覆盖属性
        [JsonProperty("onHold")] public KeyHoldConfig? OnHold { get; set; } = new();
    }

    public class KeyHoldConfig
    {
        [JsonProperty("label")] public string? Label { get; set; }
        [JsonProperty("foregroundColor")] public Color? ForegroundColor { get; set; }
        [JsonProperty("backgroundColor")] public Color? BackgroundColor { get; set; }
        [JsonProperty("borderColor")] public Color? BorderColor { get; set; }
        [JsonProperty("borderWidth")] public float? BorderWidth { get; set; }
        [JsonProperty("borderRadius")] public float? BorderRadius { get; set; }
        [JsonProperty("fontSize")] public int? FontSize { get; set; }
        [JsonProperty("size")] public Vector2? Size { get; set; }
    }
}
