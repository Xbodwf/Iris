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
                
                // 测试模式开关
                filters.enableTestMode = UIUtils.M3Switch(filters.enableTestMode, "开启内置测试滤镜 (反色效果)");
                if (GUI.changed) FilterManager.ApplyFilters();

                GUILayout.Space(8);
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

            GUILayout.EndVertical(); // End Left Column

            GUILayout.EndHorizontal();

            if (GUI.changed) Save(modEntry);
        }

        public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);
    }
}