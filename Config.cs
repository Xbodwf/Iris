using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityModManagerNet;
using UnityEngine;
using Iris.Settings;
using Iris.UI;
using Iris.Patches;

namespace Iris
{
    public class Config : UnityModManager.ModSettings
    {
        public string language = "en";
        public bool firstRun = true;

        public IrisSettings filters = new();
        public UISettings ui = new();
        public AppearanceSettings appearance = new();

        private bool _showFolderBrowser;
        private string _browserCurrentPath = "";
        private Vector2 _browserScroll;
        private string[] _browserSubFolders = Array.Empty<string>();
        private string[] _browserFiles = Array.Empty<string>();
        private string _selectedFile = "";
        private readonly string[] _supportedExtensions = { ".png", ".jpg", ".jpeg", ".mp4", ".mov", ".webm" };

        private SkinConfig? _targetSkinConfig;

        private void OpenFileBrowser(SkinConfig target)
        {
            _targetSkinConfig = target;
            _showFolderBrowser = true;
            string initialPath = target.path;
            if (string.IsNullOrEmpty(initialPath) || (!File.Exists(initialPath) && !Directory.Exists(initialPath)))
            {
                initialPath = Main.Mod?.Path ?? Directory.GetCurrentDirectory();
            }

            if (File.Exists(initialPath))
            {
                _browserCurrentPath = Path.GetDirectoryName(initialPath) ?? initialPath;
                _selectedFile = initialPath;
            }
            else
            {
                _browserCurrentPath = initialPath;
                _selectedFile = "";
            }

            RefreshBrowserFolders();
        }

        private void RefreshBrowserFolders()
        {
            try
            {
                if (Directory.Exists(_browserCurrentPath))
                {
                    _browserSubFolders = Directory.GetDirectories(_browserCurrentPath);
                    _browserFiles = Directory.GetFiles(_browserCurrentPath)
                        .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                        .ToArray();
                }
                else
                {
                    _browserSubFolders = Array.Empty<string>();
                    _browserFiles = Array.Empty<string>();
                }
            }
            catch (Exception ex)
            {
                Main.Logger?.Error($"Failed to get directory contents: {ex.Message}");
                _browserSubFolders = Array.Empty<string>();
                _browserFiles = Array.Empty<string>();
            }
        }

        private void DrawFolderBrowser()
        {
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.Label(Localization.Get("SelectBackgroundFile"), UIUtils.HeaderStyle);

            GUILayout.Label(Localization.Get("CurrentPath"), UIUtils.LabelStyle);
            GUILayout.TextArea(_browserCurrentPath, UIUtils.TextFieldStyle);

            GUILayout.Space(4);

            _browserScroll = GUILayout.BeginScrollView(_browserScroll, GUILayout.Height(300));

            try
            {
                var parent = Directory.GetParent(_browserCurrentPath);
                if (parent != null)
                {
                    if (GUILayout.Button($"📁 [{Localization.Get("Back")}]", UIUtils.ButtonStyle))
                    {
                        _browserCurrentPath = parent.FullName;
                        RefreshBrowserFolders();
                    }
                }
            }
            catch { }

            foreach (var folder in _browserSubFolders)
            {
                string folderName = Path.GetFileName(folder);
                if (GUILayout.Button($"📁 {folderName}", UIUtils.ButtonStyle))
                {
                    _browserCurrentPath = folder;
                    RefreshBrowserFolders();
                }
            }

            foreach (var file in _browserFiles)
            {
                string fileName = Path.GetFileName(file);
                bool isSelected = _selectedFile == file;

                if (isSelected) GUI.color = new Color(0.66f, 0.76f, 1.0f);
                if (GUILayout.Button($"📄 {fileName}", UIUtils.ButtonStyle))
                {
                    _selectedFile = file;
                }
                GUI.color = Color.white;
            }
            GUILayout.EndScrollView();

            GUILayout.Space(8);

            if (!string.IsNullOrEmpty(_selectedFile))
            {
                GUILayout.Label(Path.GetFileName(_selectedFile), UIUtils.LabelStyle);
                GUILayout.Space(4);
            }

            GUILayout.BeginHorizontal();
            GUI.enabled = !string.IsNullOrEmpty(_selectedFile);
            if (GUILayout.Button(Localization.Get("Select"), UIUtils.ButtonStyle, GUILayout.Width(100), GUILayout.Height(32)))
            {
                if (_targetSkinConfig != null)
                {
                    _targetSkinConfig.path = _selectedFile;
                }
                _showFolderBrowser = false;
            }
            GUI.enabled = true;
            GUILayout.Space(12);
            if (GUILayout.Button(Localization.Get("Cancel"), UIUtils.ButtonStyle, GUILayout.Width(100), GUILayout.Height(32)))
            {
                _showFolderBrowser = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            UIUtils.InitializeStyles();

            if (_showFolderBrowser)
            {
                DrawFolderBrowser();
                return;
            }

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(420));

            // Language Settings
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.Label(Localization.Get("GeneralSettings"), UIUtils.HeaderStyle);
            GUILayout.BeginHorizontal();
            foreach (var lang in Localization.AvailableLanguages)
            {
                if (language == lang) GUI.color = new Color(0.66f, 0.76f, 1.0f);
                if (GUILayout.Button(Localization.GetDisplayName(lang).ToUpper(), UIUtils.ButtonStyle, GUILayout.Width(100)))
                {
                    language = lang;
                    Save(modEntry);
                }
                GUI.color = Color.white;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8);

            DrawFilterUI();

            GUILayout.EndVertical();

            GUILayout.Space(16);

            GUILayout.BeginVertical(GUILayout.Width(420));

            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("AppearanceSettings"), UIUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            appearance.enableMenuSkin = UIUtils.M3Switch(appearance.enableMenuSkin, "");
            GUILayout.EndHorizontal();

            if (appearance.enableMenuSkin)
            {
                GUILayout.Space(8);

                GUILayout.Label(Localization.Get("SkinMode"), UIUtils.LabelStyle);
                appearance.mode = DrawSkinModeSelector(appearance.mode);

                GUILayout.Space(12);

                if (appearance.mode == SkinMode.SingleGlobal)
                {
                    DrawSkinConfigUI(appearance.globalSkin, Localization.Get("GlobalSkin"));
                }
                else if (appearance.mode == SkinMode.PerScene)
                {
                    DrawSkinConfigUI(appearance.mainUISkin, Localization.Get("MainUISkin"));
                    GUILayout.Space(8);
                    DrawSkinConfigUI(appearance.clsSkin, Localization.Get("CLSSkin"));
                    GUILayout.Space(8);
                    DrawSkinConfigUI(appearance.dlcUISkin, Localization.Get("DLCUISkin"));
                }
                else if (appearance.mode == SkinMode.Slideshow)
                {
                    appearance.EnsureSlideshowSize();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Localization.Get("SlideDuration"), GUILayout.Width(120));
                    appearance.slideDuration = GUILayout.HorizontalSlider(appearance.slideDuration, 1f, 600f);
                    GUILayout.Label(appearance.slideDuration.ToString("F0") + Localization.Get("SecondsUnit"), UIUtils.LabelStyle, GUILayout.Width(40));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Localization.Get("SlideCount"), GUILayout.Width(120));
                    string countStr = GUILayout.TextField(appearance.slideshowCount.ToString(), 2, UIUtils.TextFieldStyle, GUILayout.Width(50));
                    if (int.TryParse(countStr, out int newCount)) appearance.slideshowCount = Mathf.Clamp(newCount, 1, 20);
                    GUILayout.EndHorizontal();

                    GUILayout.Space(8);
                    for (int i = 0; i < appearance.slideshowCount; i++)
                    {
                        DrawSkinConfigUI(appearance.slideshowSkins[i], $"{Localization.Get("Slide")} {i + 1}");
                        if (i < appearance.slideshowCount - 1) GUILayout.Space(8);
                    }
                }

                GUILayout.Space(12);
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localization.Get("TrackCustomization"), UIUtils.LabelStyle);
                GUILayout.FlexibleSpace();
                appearance.enableTrackCustomization = UIUtils.M3Switch(appearance.enableTrackCustomization, "");
                GUILayout.EndHorizontal();

                if (appearance.enableTrackCustomization)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Localization.Get("Color"), GUILayout.Width(80));
                    appearance.trackColor = DrawColorField(appearance.trackColor);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(84);
                    appearance.trackColorR = GUILayout.Toggle(appearance.trackColorR, "R", GUILayout.Width(40));
                    appearance.trackColorG = GUILayout.Toggle(appearance.trackColorG, "G", GUILayout.Width(40));
                    appearance.trackColorB = GUILayout.Toggle(appearance.trackColorB, "B", GUILayout.Width(40));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Localization.Get("Opacity"), GUILayout.Width(80));
                    appearance.trackOpacity = GUILayout.HorizontalSlider(appearance.trackOpacity, 0f, 1f);
                    GUILayout.Label(appearance.trackOpacity.ToString("P0"), UIUtils.LabelStyle, GUILayout.Width(40));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Localization.Get("Brightness"), GUILayout.Width(80));
                    appearance.trackBrightness = GUILayout.HorizontalSlider(appearance.trackBrightness, 0f, 5f);
                    GUILayout.Label(appearance.trackBrightness.ToString("F1"), UIUtils.LabelStyle, GUILayout.Width(40));
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Space(16);
            GUIStyle versionStyle = new(UIUtils.LabelStyle)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f, 0.5f) }
            };
            GUILayout.Label($"Iris {VersionManager.GetFullVersionString()}", versionStyle);

            if (GUI.changed)
            {
                Save(modEntry);
                AppearancePatches.ApplyTrackCustomization();
                PatchManager.UpdateAllPatches();
            }
        }

        private void DrawSkinConfigUI(SkinConfig config, string label)
        {
            GUIStyle subContainerStyle = new()
            {
                normal = { background = UIUtils.GetCachedRoundedTex(64, 64, 8, new Color(1, 1, 1, 0.04f)) },
                padding = new RectOffset(12, 12, 12, 12),
                margin = new RectOffset(0, 0, 4, 4)
            };

            GUILayout.BeginVertical(subContainerStyle);
            GUILayout.Label(label, UIUtils.LabelStyle);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal(GUILayout.Height(28));
            config.path = GUILayout.TextField(config.path, 1024, UIUtils.TextFieldStyle, GUILayout.Width(300));
            GUILayout.Space(4);
            if (GUILayout.Button(Localization.Get("Browse"), UIUtils.ButtonStyle, GUILayout.Width(60)))
            {
                OpenFileBrowser(config);
            }
            GUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(config.path))
            {
                GUILayout.EndVertical();
                return;
            }

            bool isVideo = _supportedExtensions.Skip(3).Any(e => config.path.ToLower().EndsWith(e));

            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("Scale"), GUILayout.Width(80));
            config.scale = GUILayout.HorizontalSlider(config.scale, 0.1f, 5f);
            GUILayout.Label(config.scale.ToString("F1"), UIUtils.LabelStyle, GUILayout.Width(40));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("Offset X"), GUILayout.Width(80));
            config.offsetX = GUILayout.HorizontalSlider(config.offsetX, -1f, 1f);
            GUILayout.Label(config.offsetX.ToString("F2"), UIUtils.LabelStyle, GUILayout.Width(40));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("Offset Y"), GUILayout.Width(80));
            config.offsetY = GUILayout.HorizontalSlider(config.offsetY, -1f, 1f);
            GUILayout.Label(config.offsetY.ToString("F2"), UIUtils.LabelStyle, GUILayout.Width(40));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("Opacity"), GUILayout.Width(80));
            config.opacity = GUILayout.HorizontalSlider(config.opacity, 0f, 1f);
            GUILayout.Label(config.opacity.ToString("P0"), UIUtils.LabelStyle, GUILayout.Width(40));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("Brightness"), GUILayout.Width(80));
            config.brightness = GUILayout.HorizontalSlider(config.brightness, 0f, 5f);
            GUILayout.Label(config.brightness.ToString("F1"), UIUtils.LabelStyle, GUILayout.Width(40));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("Saturation"), GUILayout.Width(80));
            config.saturation = GUILayout.HorizontalSlider(config.saturation, 0f, 2f);
            GUILayout.Label(config.saturation.ToString("F1"), UIUtils.LabelStyle, GUILayout.Width(40));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("Contrast"), GUILayout.Width(80));
            config.contrast = GUILayout.HorizontalSlider(config.contrast, 0f, 2f);
            GUILayout.Label(config.contrast.ToString("F1"), UIUtils.LabelStyle, GUILayout.Width(40));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("Hue"), GUILayout.Width(80));
            config.hue = GUILayout.HorizontalSlider(config.hue, -180f, 180f);
            GUILayout.Label(config.hue.ToString("F0"), UIUtils.LabelStyle, GUILayout.Width(40));
            GUILayout.EndHorizontal();

            if (isVideo)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localization.Get("Playback"), GUILayout.Width(80));
                config.playbackSpeed = GUILayout.HorizontalSlider(config.playbackSpeed, 0.1f, 5f);
                GUILayout.Label(config.playbackSpeed.ToString("F1"), UIUtils.LabelStyle, GUILayout.Width(40));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        private SkinMode DrawSkinModeSelector(SkinMode current)
        {
            GUILayout.BeginHorizontal();

            GUI.color = current == SkinMode.SingleGlobal ? new Color(0.66f, 0.76f, 1.0f) : Color.white;
            if (GUILayout.Button(Localization.Get("ModeSingle"), UIUtils.ButtonStyle)) current = SkinMode.SingleGlobal;

            GUI.color = current == SkinMode.PerScene ? new Color(0.66f, 0.76f, 1.0f) : Color.white;
            if (GUILayout.Button(Localization.Get("ModePerScene"), UIUtils.ButtonStyle)) current = SkinMode.PerScene;

            GUI.color = current == SkinMode.Slideshow ? new Color(0.66f, 0.76f, 1.0f) : Color.white;
            if (GUILayout.Button(Localization.Get("ModeSlideshow"), UIUtils.ButtonStyle)) current = SkinMode.Slideshow;

            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            return current;
        }

        private Color DrawColorField(Color color)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("R", GUILayout.Width(16));
            color.r = GUILayout.HorizontalSlider(color.r, 0f, 1f);
            GUILayout.Label(color.r.ToString("F2"), UIUtils.LabelStyle, GUILayout.Width(36));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("G", GUILayout.Width(16));
            color.g = GUILayout.HorizontalSlider(color.g, 0f, 1f);
            GUILayout.Label(color.g.ToString("F2"), UIUtils.LabelStyle, GUILayout.Width(36));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("B", GUILayout.Width(16));
            color.b = GUILayout.HorizontalSlider(color.b, 0f, 1f);
            GUILayout.Label(color.b.ToString("F2"), UIUtils.LabelStyle, GUILayout.Width(36));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            return color;
        }

        private static readonly HashSet<string> _expandedFilters = new();

        private void DrawFilterUI()
        { // Method body
            GUILayout.BeginVertical(UIUtils.CardStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localization.Get("FilterSettings"), UIUtils.HeaderStyle);
            GUILayout.FlexibleSpace();
            filters.enableFilters = UIUtils.M3Switch(filters.enableFilters, "");
            GUILayout.EndHorizontal();

            if (filters.enableFilters)
            {
                GUILayout.Space(8);
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localization.Get("AvailableFilters"), UIUtils.LabelStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(Localization.Get("Refresh"), UIUtils.ButtonStyle, GUILayout.Width(100)))
                {
                    FilterManager.ScanFilters();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(8);
                
                if (FilterManager.AvailableShaders.Count == 0)
                {
                    GUILayout.Label("No shaders found in Resources folder.", UIUtils.LabelStyle);
                }

                foreach (var meta in FilterManager.AvailableShaders)
                {
                    bool isEnabled = filters.enabledFilters.Contains(meta.Id);
                    
                    GUIStyle itemStyle = new() 
                    { 
                        normal = { background = UIUtils.GetCachedRoundedTex(64, 64, 8, new Color(1, 1, 1, 0.05f)) }, 
                        padding = new RectOffset(10, 10, 5, 5),
                        border = new RectOffset(10, 10, 10, 10)
                    };
                    GUILayout.BeginVertical(itemStyle);
                    
                    GUILayout.BeginHorizontal();
                    bool toggle = UIUtils.M3Switch(isEnabled, meta.ShaderName);
                    if (toggle != isEnabled)
                    { // Toggle logic
                        if (toggle) filters.enabledFilters.Add(meta.Id);
                        else filters.enabledFilters.Remove(meta.Id);
                        
                        FilterManager.ForceUpdate();
                    }

                    GUILayout.FlexibleSpace();
                    
                    if (isEnabled)
                    {
                        string arrow = _expandedFilters.Contains(meta.Id) ? "▲" : "▼";
                        if (GUILayout.Button(arrow, UIUtils.LabelStyle, GUILayout.Width(20)))
                        {
                            if (_expandedFilters.Contains(meta.Id)) _expandedFilters.Remove(meta.Id);
                            else _expandedFilters.Add(meta.Id);
                        }
                    }
                    GUILayout.EndHorizontal();

                    // Display source file as small label
                    string sourceLabel = $"Source: {System.IO.Path.GetFileName(meta.SourceFile)}";
                    GUIStyle smallLabel = new(UIUtils.LabelStyle) { fontSize = 10, normal = { textColor = new Color(0.6f, 0.6f, 0.6f, 0.8f) } };
                    GUILayout.Label(sourceLabel, smallLabel);

                    if (isEnabled && _expandedFilters.Contains(meta.Id))
                    {
                        DrawFilterParams(meta);
                    }
                    
                    GUILayout.EndVertical();
                    GUILayout.Space(4);
                }
            }
            GUILayout.EndVertical();
        }

        private void DrawFilterParams(ShaderMetadata meta)
        {
            var config = filters.filterConfigs.Find(c => c.id == meta.Id);
            if (config == null)
            {
                config = new FilterConfig { id = meta.Id, name = meta.ShaderName };
                filters.filterConfigs.Add(config);
            }

            GUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(20, 10, 5, 5) });
            
            foreach (var prop in meta.Properties)
            {
                var param = config.paramsList.Find(p => p.name == prop.Name);
                if (param == null)
                {
                    param = new FilterParam { name = prop.Name, type = prop.Type };
                    param.values[0] = prop.DefaultValue;
                    config.paramsList.Add(param);
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(string.IsNullOrEmpty(prop.Description) ? prop.Name : prop.Description, GUILayout.Width(120));
                
                if (prop.Type == "Float" || prop.Type == "Range")
                {
                    float min = prop.Type == "Range" ? prop.MinValue : -10f;
                    float max = prop.Type == "Range" ? prop.MaxValue : 10f;
                    float oldVal = param.values[0];
                    param.values[0] = GUILayout.HorizontalSlider(param.values[0], min, max);
                    if (Mathf.Abs(oldVal - param.values[0]) > 0.0001f) FilterManager.ForceUpdate();
                }
                else if (prop.Type == "Color" || prop.Type == "Vector")
                {
                    GUILayout.EndHorizontal();
                    for (int i = 0; i < 4; i++)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        string subLabel = prop.Type == "Color" ? "RGBA"[i].ToString() : "XYZW"[i].ToString();
                        GUILayout.Label(subLabel, GUILayout.Width(20));
                        float oldVal = param.values[i];
                        param.values[i] = GUILayout.HorizontalSlider(param.values[i], 0f, 1f);
                        if (Mathf.Abs(oldVal - param.values[i]) > 0.0001f) FilterManager.ForceUpdate();
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.BeginHorizontal(); // Restart horizontal for the loop's EndHorizontal
                }
                
                GUILayout.EndHorizontal();
            }
            
            if (GUI.changed && FilterManager.IsActive)
            {
                FilterManager.ApplyFilters();
            }
            
            GUILayout.EndVertical();
        }

        public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);
    }
}
