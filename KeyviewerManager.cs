using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Iris.Settings;
using Iris.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Iris
{
    public static class KeyviewerManager
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            Formatting = Formatting.Indented,
            ObjectCreationHandling = ObjectCreationHandling.Replace
        };

        public static List<KeyviewerConfig> LoadedConfigs { get; private set; } = new();
        public static Dictionary<string, bool> EnabledStates { get; private set; } = new();
        private static readonly Dictionary<string, scrKeyViewerContainer> _activeInstances = new();
        private static GameObject? _canvasObj;

        public static void LoadConfigs()
        {
            // 清理现有实例
            foreach (var instance in _activeInstances.Values)
            {
                if (instance != null) UnityEngine.Object.Destroy(instance.gameObject);
            }
            _activeInstances.Clear();

            LoadedConfigs.Clear();
            string kvDir = Path.Combine(ResourceLoader.ResourcesPath, "keyviewer");
            
            if (Directory.Exists(kvDir))
            {
                string[] files = Directory.GetFiles(kvDir, "*.json");
                foreach (string file in files)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var config = JsonConvert.DeserializeObject<KeyviewerConfig>(json, _jsonSettings);
                        if (config != null)
                        {
                            if (string.IsNullOrEmpty(config.Name))
                                config.Name = Path.GetFileNameWithoutExtension(file);
                            
                            LoadedConfigs.Add(config);
                            
                            if (!EnabledStates.ContainsKey(config.Name))
                            {
                                EnabledStates[config.Name] = false;
                            }
                            else if (EnabledStates[config.Name])
                            {
                                CreateInstance(config);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Main.Logger?.Error($"Failed to load keyviewer config {file}: {ex.Message}");
                    }
                }
            }
        }

        public static void SetEnabled(string name, bool enabled)
        {
            EnabledStates[name] = enabled;
            if (enabled)
            {
                var config = LoadedConfigs.Find(c => c.Name == name);
                if (config != null) CreateInstance(config);
            }
            else
            {
                if (_activeInstances.TryGetValue(name, out var instance))
                {
                    if (instance != null) UnityEngine.Object.Destroy(instance.gameObject);
                    _activeInstances.Remove(name);
                }
            }
        }

        public static void SaveConfig(KeyviewerConfig config)
        {
            try
            {
                string kvDir = Path.Combine(ResourceLoader.ResourcesPath, "keyviewer");
                if (!Directory.Exists(kvDir)) Directory.CreateDirectory(kvDir);
                
                string filePath = Path.Combine(kvDir, $"{config.Name}.json");
                string json = JsonConvert.SerializeObject(config, _jsonSettings);
                File.WriteAllText(filePath, json);
                
                // 如果当前正在运行，更新实例
                if (EnabledStates.TryGetValue(config.Name, out bool enabled) && enabled)
                {
                    SetEnabled(config.Name, false);
                    SetEnabled(config.Name, true);
                }
            }
            catch (Exception ex)
            {
                Main.Logger?.Error($"Failed to save config {config.Name}: {ex.Message}");
            }
        }

        public static bool IsListening { get; private set; }
        private static KeyviewerConfig? _listeningConfig;
        private static readonly KeyCode[] _allKeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));

        public static void StartListening(KeyviewerConfig config)
        {
            IsListening = true;
            _listeningConfig = config;
        }

        public static void StopListening()
        {
            IsListening = false;
            _listeningConfig = null;
        }

        public static void UpdateListener()
        {
            if (!IsListening || _listeningConfig == null) return;
            if (!Input.anyKeyDown) return;

            for (int i = 0; i < _allKeyCodes.Length; i++)
            {
                KeyCode kcode = _allKeyCodes[i];
                if (Input.GetKeyDown(kcode))
                {
                    // 过滤鼠标和 Escape
                    if (kcode == KeyCode.Escape || (kcode >= KeyCode.Mouse0 && kcode <= KeyCode.Mouse6))
                        continue;

                    // 检查是否已存在
                    if (_listeningConfig.Keys.Exists(k => k.KeyCode == kcode))
                        continue;

                    _listeningConfig.Keys.Add(new KeyConfig
                    {
                        Label = kcode.ToString(),
                        KeyCode = kcode,
                        Position = new Vector2(_listeningConfig.Keys.Count * 70, 0),
                        Size = new Vector2(60, 60)
                    });
                }
            }
        }

        private static void CreateInstance(KeyviewerConfig config)
        {
            if (_activeInstances.ContainsKey(config.Name)) return;

            if (_canvasObj == null)
            {
                _canvasObj = new GameObject("Iris_KeyviewerCanvas");
                UnityEngine.Object.DontDestroyOnLoad(_canvasObj);
                var canvas = _canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000;
                _canvasObj.AddComponent<CanvasScaler>();
                _canvasObj.AddComponent<GraphicRaycaster>();
            }

            var kvObj = new GameObject(config.Name);
            kvObj.transform.SetParent(_canvasObj.transform, false);
            var container = kvObj.AddComponent<scrKeyViewerContainer>();
            container.Initialize(config);
            _activeInstances[config.Name] = container;
        }
    }
}