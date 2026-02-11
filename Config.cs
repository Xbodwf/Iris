using System;
using System.Collections.Generic;
using UnityModManagerNet;
using UnityEngine;
using Iris.Settings;
using Iris.UI;

namespace Iris
{
    public class Config : UnityModManager.ModSettings
    {
        public string language = "en";
        
        public IrisSettings filters = new();
        public UISettings ui = new();

        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            UIUtils.InitializeStyles();

            GUILayout.BeginHorizontal();

            // --- Left Column: Controls ---
            GUILayout.BeginVertical(GUILayout.Width(450));

            // Language & Basic
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.Label(Localization.Get("GeneralSettings"), UIUtils.HeaderStyle);
            GUILayout.BeginHorizontal();
            foreach (var lang in Localization.AvailableLanguages)
            {
                if (language == lang) GUI.color = new(0.66f, 0.76f, 1.0f);
                if (GUILayout.Button(Localization.GetDisplayName(lang).ToUpper(), UIUtils.ButtonStyle, GUILayout.Width(100))) language = lang;
                GUI.color = Color.white;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Shader Management Card
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("FilterSettings"), UIUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            filters.enableFilters = UIUtils.M3Switch(filters.enableFilters, "");
            GUILayout.EndHorizontal();

            if (filters.enableFilters)
            {
                GUILayout.Space(12);
                
                /* 暂时移除内置 Shader 设置
                // Posterize 开关
                filters.enablePosterize = UIUtils.M3Switch(filters.enablePosterize, Localization.Get("EnablePosterize"));
                if (filters.enablePosterize)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Localization.Get("PosterizeDistortion"), UIUtils.LabelStyle, GUILayout.Width(100));
                    filters.posterizeDistortion = GUILayout.HorizontalSlider(filters.posterizeDistortion, 1f, 256f);
                    GUILayout.EndHorizontal();
                }
                if (GUI.changed) FilterManager.ApplyFilters();

                GUILayout.Space(8);
                
                // VideoBloom 开关
                filters.enableVideoBloom = UIUtils.M3Switch(filters.enableVideoBloom, Localization.Get("EnableVideoBloom"));
                if (filters.enableVideoBloom)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Localization.Get("BloomAmount"), UIUtils.LabelStyle, GUILayout.Width(100));
                    filters.videoBloomAmount = GUILayout.HorizontalSlider(filters.videoBloomAmount, 0f, 5f);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Localization.Get("BloomThreshold"), UIUtils.LabelStyle, GUILayout.Width(100));
                    filters.videoBloomThreshold = GUILayout.HorizontalSlider(filters.videoBloomThreshold, 0f, 1f);
                    GUILayout.EndHorizontal();
                }
                if (GUI.changed) FilterManager.ApplyFilters();

                GUILayout.Space(8);
                */

                GUILayout.BeginHorizontal();
                GUILayout.Label(Localization.Get("AvailableFilters"), UIUtils.LabelStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(Localization.Get("RefreshList"), UIUtils.ButtonStyle, GUILayout.Width(100)))
                {
                    FilterManager.ScanFilters();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(8);
                // 渲染扫描到的 Shader 列表
                foreach (var filterName in FilterManager.ScannedFilters)
                {
                    bool isEnabled = filters.enabledFilters.Contains(filterName);
                    bool toggle = UIUtils.M3Switch(isEnabled, filterName);
                    if (toggle != isEnabled)
                    {
                        if (toggle) filters.enabledFilters.Add(filterName);
                        else filters.enabledFilters.Remove(filterName);
                        
                        // 如果在游玩中，实时更新
                        if (FilterManager.IsActive) FilterManager.ApplyFilters();
                    }
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(8);

            /* 暂时隐藏 Keyviewer 入口
            // Keyviewer Settings Card
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("KeyviewerSettings"), UIUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            filters.enableKeyviewer = UIUtils.M3Switch(filters.enableKeyviewer, Localization.Get("EnableKeyviewer"));
            GUILayout.EndHorizontal();

            if (filters.enableKeyviewer)
            {
                GUILayout.Space(12);
                if (GUILayout.Button(Localization.Get("ReloadKVConfigs"), UIUtils.ButtonStyle, GUILayout.Width(150)))
                {
                    KeyviewerManager.LoadConfigs();
                }

                GUILayout.Space(8);
                foreach (var config in KeyviewerManager.LoadedConfigs)
                {
                    GUILayout.BeginHorizontal();
                    bool isEnabled = KeyviewerManager.EnabledStates.TryGetValue(config.Name, out bool val) && val;
                    bool toggle = UIUtils.M3Switch(isEnabled, config.Name);
                    if (toggle != isEnabled)
                    {
                        KeyviewerManager.SetEnabled(config.Name, toggle);
                        filters.kvEnabledStates[config.Name] = toggle;
                    }
                    
                    if (GUILayout.Button(Localization.Get("EditConfig"), UIUtils.ButtonStyle, GUILayout.Width(80)))
                    {
                        _editingConfig = _editingConfig == config ? null : config;
                    }
                    GUILayout.EndHorizontal();

                    if (_editingConfig == config)
                    {
                        DrawKVEtidor(config);
                    }
                }
            }
            GUILayout.EndVertical();
            */

            GUILayout.EndVertical(); // End Left Column

            GUILayout.EndHorizontal();

            if (GUI.changed) Save(modEntry);
        }

        private KeyviewerConfig? _editingConfig;
        private Vector2 _scrollPos;
        private readonly Dictionary<string, string> _stringCache = new();

        private string GetCachedString(string id, float val)
        {
            if (!_stringCache.TryGetValue(id, out string str))
            {
                str = val.ToString("F2");
                _stringCache[id] = str;
            }
            return str;
        }

        private void DrawKVEtidor(KeyviewerConfig config)
        {
            GUILayout.BeginVertical(UIUtils.CardStyle);
            
            // Container \u0026 Global Styles
            GUILayout.Label(Localization.Get("ContainerSettings"), UIUtils.HeaderStyle);
            config.ContainerPosition = DrawVector2("pos_" + config.Name, Localization.Get("Position"), config.ContainerPosition);
            
            GUILayout.Label(Localization.Get("GlobalSettings"), UIUtils.HeaderStyle);
            config.GlobalScale = DrawFloat("scale_" + config.Name, Localization.Get("Scale"), config.GlobalScale);
            config.GlobalOpacity = DrawFloat("opacity_" + config.Name, Localization.Get("Opacity"), config.GlobalOpacity);
            config.GlobalBorderRadius = DrawFloat("radius_" + config.Name, Localization.Get("BorderRadius"), config.GlobalBorderRadius);
            config.GapLength = DrawFloat("gap_" + config.Name, Localization.Get("GapLength"), config.GapLength);

            GUILayout.Space(8);
            config.RainingKeySpeed = DrawFloat("speed_" + config.Name, Localization.Get("Speed"), config.RainingKeySpeed);
            config.RainingKeyThreshold = DrawFloat("thresh_" + config.Name, Localization.Get("Threshold"), config.RainingKeyThreshold);

            GUILayout.Space(8);
            if (KeyviewerManager.IsListening)
            {
                if (GUILayout.Button(Localization.Get("StopListening"), UIUtils.ButtonStyle)) KeyviewerManager.StopListening();
            }
            else
            {
                if (GUILayout.Button(Localization.Get("StartListening"), UIUtils.ButtonStyle)) KeyviewerManager.StartListening(config);
            }

            GUILayout.Space(8);
            GUILayout.Label(Localization.Get("KeySettings"), UIUtils.HeaderStyle);
            
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(400));
            for (int i = 0; i < config.Keys.Count; i++)
            {
                var key = config.Keys[i];
                string keyId = config.Name + "_" + i;
                
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.BeginHorizontal();
                    // 预览小图标 (使用 GetCachedRoundedTex)
                    float previewRadius = key.BorderRadius >= 0 ? key.BorderRadius : config.GlobalBorderRadius;
                    var previewTex = UIUtils.GetCachedRoundedTex(32, 32, previewRadius * (32f/key.Size.x), key.BackgroundColor);
                    GUILayout.Box(previewTex, GUILayout.Width(32), GUILayout.Height(32));
                    
                    GUILayout.Label($"{key.Label} ({key.KeyCode})", UIUtils.LabelStyle);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(Localization.Get("Delete"), UIUtils.ButtonStyle, GUILayout.Width(60)))
                    {
                        config.Keys.RemoveAt(i);
                        i--;
                        continue;
                    }
                    GUILayout.EndHorizontal();

                    key.Label = DrawString(Localization.Get("Label"), key.Label);
                    key.Position = DrawVector2(keyId + "_pos", Localization.Get("Position"), key.Position);
                    key.Size = DrawVector2(keyId + "_size", Localization.Get("Size"), key.Size);
                    
                    key.BorderWidth = DrawFloat(keyId + "_bw", Localization.Get("BorderWidth"), key.BorderWidth);
                    key.BorderRadius = DrawFloat(keyId + "_br", Localization.Get("BorderRadius"), key.BorderRadius);
                    
                    key.ShowRainingKey = GUILayout.Toggle(key.ShowRainingKey, Localization.Get("RainingKey"));
                    
                    key.BackgroundColor = DrawColor(Localization.Get("Colors") + " (BG)", key.BackgroundColor);
                    key.ForegroundColor = DrawColor(Localization.Get("Colors") + " (FG)", key.ForegroundColor);

                    // OnHold 覆盖预览/编辑
                    if (key.OnHold == null) key.OnHold = new KeyHoldConfig();
                    if (GUILayout.Button("Edit OnHold Styles", UIUtils.ButtonStyle))
                    {
                        // 这里可以添加一个折叠面板或切换逻辑，暂时简化处理
                    }
                    
                    key.OnHold.BackgroundColor = DrawOptionalColor("OnHold BG", key.OnHold.BackgroundColor);
                    key.OnHold.Label = DrawOptionalString("OnHold Label", key.OnHold.Label);

                    key.FontName = DrawString(Localization.Get("FontName"), key.FontName);
                    key.FontSize = (int)DrawFloat(keyId + "_fs", Localization.Get("FontSize"), key.FontSize);
                }
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button(Localization.Get("SaveConfig"), UIUtils.ButtonStyle))
            {
                KeyviewerManager.SaveConfig(config);
                _stringCache.Clear(); // 保存后清理缓存
            }
            
            GUILayout.EndVertical();
        }

        private Vector2 DrawVector2(string id, string label, Vector2 val)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100));
            val.x = DrawFloat(id + "_x", "X", val.x, 60);
            val.y = DrawFloat(id + "_y", "Y", val.y, 60);
            GUILayout.EndHorizontal();
            return val;
        }

        private float DrawFloat(string id, string label, float val, float width = 100)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(width));
            
            string cached = GetCachedString(id, val);
            string res = GUILayout.TextField(cached, GUILayout.Width(width));
            
            if (res != cached)
            {
                _stringCache[id] = res;
                if (float.TryParse(res, out float v)) val = v;
            }
            GUILayout.EndHorizontal();
            return val;
        }

        private string DrawString(string label, string val)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100));
            val = GUILayout.TextField(val);
            GUILayout.EndHorizontal();
            return val;
        }

        private Color DrawColor(string label, Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100));
            color.r = GUILayout.HorizontalSlider(color.r, 0, 1, GUILayout.Width(50));
            color.g = GUILayout.HorizontalSlider(color.g, 0, 1, GUILayout.Width(50));
            color.b = GUILayout.HorizontalSlider(color.b, 0, 1, GUILayout.Width(50));
            color.a = GUILayout.HorizontalSlider(color.a, 0, 1, GUILayout.Width(50));
            GUILayout.EndHorizontal();
            return color;
        }

        private Color? DrawOptionalColor(string label, Color? color)
        {
            GUILayout.BeginHorizontal();
            bool hasValue = color.HasValue;
            bool toggle = GUILayout.Toggle(hasValue, "", GUILayout.Width(20));
            if (toggle != hasValue)
            {
                color = toggle ? Color.white : null;
            }
            GUILayout.Label(label, GUILayout.Width(80));
            if (color.HasValue)
            {
                var c = color.Value;
                c.r = GUILayout.HorizontalSlider(c.r, 0, 1, GUILayout.Width(40));
                c.g = GUILayout.HorizontalSlider(c.g, 0, 1, GUILayout.Width(40));
                c.b = GUILayout.HorizontalSlider(c.b, 0, 1, GUILayout.Width(40));
                c.a = GUILayout.HorizontalSlider(c.a, 0, 1, GUILayout.Width(40));
                color = c;
            }
            else
            {
                GUILayout.Label("(Inherit)", UIUtils.LabelStyle);
            }
            GUILayout.EndHorizontal();
            return color;
        }

        private string? DrawOptionalString(string label, string? val)
        {
            GUILayout.BeginHorizontal();
            bool hasValue = val != null;
            bool toggle = GUILayout.Toggle(hasValue, "", GUILayout.Width(20));
            if (toggle != hasValue)
            {
                val = toggle ? "" : null;
            }
            GUILayout.Label(label, GUILayout.Width(80));
            if (val != null)
            {
                val = GUILayout.TextField(val);
            }
            else
            {
                GUILayout.Label("(Inherit)", UIUtils.LabelStyle);
            }
            GUILayout.EndHorizontal();
            return val;
        }

        public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);
    }
}