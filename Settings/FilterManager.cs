using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Iris.Internal;

namespace Iris.Settings
{
    public class ShaderPropertyInfo
    {
        public string Name = "";
        public string Description = "";
        public string Type = ""; // Float, Range, Color, Vector
        public float MinValue = 0f;
        public float MaxValue = 1f;
        public float DefaultValue = 0f;
    }

    public class ShaderMetadata
    {
        public string SourceFile = ""; // Bundle or Shaderpack path
        public string ShaderName = "";
        public List<ShaderPropertyInfo> Properties = new();
    }

    public static class FilterManager
    {
        public static List<string> ScannedFilters { get; private set; } = new();
        public static List<ShaderMetadata> AvailableShaders { get; private set; } = new();
        public static bool IsActive { get; private set; } = false;
        
        private static Dictionary<string, Shader> _shaderCache = new();
        private static List<Material> _activeMaterials = new();

        public static string FilterPath => Path.Combine(Main.Mod!.Path, "Resources");

        public static void ScanFilters()
        {
            if (!Directory.Exists(FilterPath)) Directory.CreateDirectory(FilterPath);
            ScannedFilters.Clear();
            AvailableShaders.Clear();
            _shaderCache.Clear();
            
            var files = Directory.GetFiles(FilterPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext == ".shaderpack" || ext == ".bundle" || ext == "") // Some bundles have no extension
                {
                    string relativePath = file.Substring(FilterPath.Length + 1);
                    // 排除 lang 和 keyviewer 文件夹
                    if (relativePath.StartsWith("lang") || relativePath.StartsWith("keyviewer")) continue;

                    ScannedFilters.Add(relativePath);
                    
                    // 扫描文件内的 Shader
                    ScanShadersInBundle(file, relativePath);
                }
            }
            
            Main.Mod?.Logger.Log($"Scanned {ScannedFilters.Count} files, found {AvailableShaders.Count} shaders.");
        }

        private static void ScanShadersInBundle(string fullPath, string relativePath)
        {
            try
            {
                AssetBundle bundle = AssetBundle.LoadFromFile(fullPath);
                if (bundle == null) return;

                // 尝试加载所有 Shader
                var shaders = bundle.LoadAllAssets<Shader>();
                foreach (var s in shaders)
                {
                    if (s == null) continue;

                    var meta = new ShaderMetadata
                    {
                        SourceFile = relativePath,
                        ShaderName = s.name
                    };

                    // 提取参数
                    int count = s.GetPropertyCount();
                    for (int i = 0; i < count; i++)
                    {
                        var propName = s.GetPropertyName(i);
                        // 过滤掉隐藏属性和常用内置属性
                        if (propName.StartsWith("_") && !propName.Equals("_MainTex") && !propName.Equals("_Distortion") && !propName.Equals("_Fade")) 
                        {
                            // 如果是开发者明确暴露的属性（通常在 Properties 块中定义）
                            var propInfo = new ShaderPropertyInfo
                            {
                                Name = propName,
                                Description = s.GetPropertyDescription(i),
                                Type = s.GetPropertyType(i).ToString(),
                                DefaultValue = s.GetPropertyDefaultFloatValue(i)
                            };

                            if (s.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Range)
                            {
                                Vector4 limits = s.GetPropertyRangeLimits(i);
                                propInfo.MinValue = limits.x; // min
                                propInfo.MaxValue = limits.y; // max
                            }

                            meta.Properties.Add(propInfo);
                        }
                    }
                    AvailableShaders.Add(meta);
                    
                    // 缓存 Shader 引用，避免频繁加载 Bundle
                    if (!_shaderCache.ContainsKey(s.name))
                    {
                        _shaderCache[s.name] = s;
                    }
                }
                bundle.Unload(false); // 保持 Shader 在内存中
            }
            catch (Exception ex)
            {
                Main.Mod?.Logger.Error($"Failed to scan bundle {relativePath}: {ex.Message}");
            }
        }

        public static void SetPlayState(bool playing)
        {
            Main.Mod?.Logger.Log($"SetPlayState: {playing}, Active: {IsActive}");
            if (playing && Main.settings.filters.enableFilters)
            {
                IsActive = true;
                ApplyFilters();
            }
            else
            {
                IsActive = false;
                ClearFilters();
            }
        }

        public static void ApplyFilters()
        {
            ClearFilters();
            if (!IsActive) return;

            Main.Mod?.Logger.Log($"Applying filters...");
            
            // 确保已经扫描过 Shader
            if (AvailableShaders.Count == 0) ScanFilters();

            // 加载用户配置的外部 Shader
            foreach (var filterName in Main.settings.filters.enabledFilters)
            {
                var meta = AvailableShaders.Find(m => m.SourceFile == filterName || m.ShaderName == filterName);
                if (meta != null)
                {
                    // 查找对应的配置
                    var config = Main.settings.filters.filterConfigs.Find(c => c.name == meta.ShaderName);
                    if (config == null)
                    {
                        config = new FilterConfig { name = meta.ShaderName };
                        // 可以在这里初始化默认参数
                    }
                    LoadAndApplyShader(meta, config);
                }
            }
            
            UpdateCameraEffects();
        }

        private static void LoadAndApplyShader(ShaderMetadata meta, FilterConfig config)
        {
            try
            {
                Shader? s = null;
                if (!_shaderCache.TryGetValue(meta.ShaderName, out s) || s == null)
                {
                    string path = Path.Combine(FilterPath, meta.SourceFile);
                    if (!File.Exists(path)) return;

                    AssetBundle bundle = AssetBundle.LoadFromFile(path);
                    if (bundle == null) return;

                    s = bundle.LoadAsset<Shader>(meta.ShaderName);
                    if (s != null) _shaderCache[meta.ShaderName] = s;
                    bundle.Unload(false);
                }

                if (s != null)
                {
                    Material mat = new Material(s);
                    if (IsMaterialValid(mat))
                    {
                        // 应用参数
                        foreach (var param in config.paramsList)
                        {
                            if (mat.HasProperty(param.name))
                            {
                                mat.SetFloat(param.name, param.value);
                            }
                        }
                        _activeMaterials.Add(mat);
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Mod?.Logger.Error($"Failed to load shader {meta.ShaderName} from {meta.SourceFile}: {ex.Message}");
            }
        }

        private static bool IsMaterialValid(Material? mat)
        {
            if (mat == null || mat.shader == null) return false;
            // 检查 Shader 是否能在当前硬件/管线上运行
            if (!mat.shader.isSupported)
            {
                Main.Mod?.Logger.Error($"Shader '{mat.shader.name}' is NOT supported on this platform!");
                return false;
            }
            return true;
        }

        private static void UpdateCameraEffects()
        {
            foreach (var cam in Camera.allCameras)
            {
                if (cam == null) continue;
                if (cam.name.Contains("UI")) continue;

                var handler = cam.GetComponent<IrisPostProcessHandler>();
                if (handler == null)
                {
                    handler = cam.gameObject.AddComponent<IrisPostProcessHandler>();
                }
                handler.Materials = new List<Material>(_activeMaterials);
                handler.enabled = true;
            }
        }

        private static void ClearFilters()
        {
            foreach (var cam in Camera.allCameras)
            {
                if (cam == null) continue;
                var handler = cam.GetComponent<IrisPostProcessHandler>();
                if (handler != null)
                {
                    handler.enabled = false;
                    UnityEngine.Object.Destroy(handler);
                }
            }
            _activeMaterials.Clear();
        }
    }

    // 后处理渲染代理组件
    public class IrisPostProcessHandler : MonoBehaviour
    {
        public List<Material> Materials = new();

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (Materials == null || Materials.Count == 0)
            {
                Graphics.Blit(source, destination);
                return;
            }

            RenderTexture temp1 = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
            RenderTexture temp2 = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);

            RenderTexture currentIn = temp1;
            RenderTexture currentOut = temp2;

            Graphics.Blit(source, currentIn);

            for (int i = 0; i < Materials.Count; i++)
            {
                if (Materials[i] == null) continue;
                Graphics.Blit(currentIn, currentOut, Materials[i]);
                
                RenderTexture temp = currentIn;
                currentIn = currentOut;
                currentOut = temp;
            }

            Graphics.Blit(currentIn, destination);

            RenderTexture.ReleaseTemporary(temp1);
            RenderTexture.ReleaseTemporary(temp2);
        }
    }
}