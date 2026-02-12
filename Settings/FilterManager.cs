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
        public string Id => $"{SourceFile}:{ShaderName}";
        public List<ShaderPropertyInfo> Properties = new();
    }

    public static class FilterManager
    {
        public static List<string> ScannedFilters { get; private set; } = new(); // This now stores relative paths to bundles
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
            // We don't clear shader cache here to avoid re-loading bundles if not necessary, 
            // but for a clean scan it might be better. Let's keep it for performance.
            
            var files = Directory.GetFiles(FilterPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext == ".shaderpack" || ext == ".bundle" || ext == "") 
                {
                    string relativePath = file.Substring(FilterPath.Length + 1);
                    if (relativePath.StartsWith("lang") || relativePath.StartsWith("keyviewer")) continue;

                    ScannedFilters.Add(relativePath);
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

                var shaders = bundle.LoadAllAssets<Shader>();
                foreach (var s in shaders)
                {
                    if (s == null) continue;

                    var meta = new ShaderMetadata
                    {
                        SourceFile = relativePath,
                        ShaderName = s.name
                    };

                    int count = s.GetPropertyCount();
                    for (int i = 0; i < count; i++)
                    {
                        var propName = s.GetPropertyName(i);
                        if (propName == "_MainTex") continue;

                        var propType = s.GetPropertyType(i);
                        var propInfo = new ShaderPropertyInfo
                        {
                            Name = propName,
                            Description = s.GetPropertyDescription(i),
                            Type = propType.ToString(),
                            DefaultValue = propType == UnityEngine.Rendering.ShaderPropertyType.Float || propType == UnityEngine.Rendering.ShaderPropertyType.Range 
                                ? s.GetPropertyDefaultFloatValue(i) : 0f
                        };

                        if (propType == UnityEngine.Rendering.ShaderPropertyType.Range)
                        {
                            Vector4 limits = s.GetPropertyRangeLimits(i);
                            propInfo.MinValue = limits.x;
                            propInfo.MaxValue = limits.y;
                        }
                        else if (propType == UnityEngine.Rendering.ShaderPropertyType.Float)
                        {
                            propInfo.MinValue = -1000f;
                            propInfo.MaxValue = 1000f;
                        }

                        meta.Properties.Add(propInfo);
                    }
                    AvailableShaders.Add(meta);
                    
                    if (!_shaderCache.ContainsKey(s.name))
                    {
                        _shaderCache[s.name] = s;
                    }
                }
                bundle.Unload(false);
            }
            catch (Exception ex)
            {
                Main.Mod?.Logger.Error($"Failed to scan bundle {relativePath}: {ex.Message}");
            }
        }

        public static void SetPlayState(bool playing)
        {
            if (playing)
            {
                Main.Mod?.Logger.Log("FilterManager: Play state started.");
            }
            else
            {
                Main.Mod?.Logger.Log("FilterManager: Play state stopped.");
            }

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

        public static void ForceUpdate()
        {
            if (IsActive) ApplyFilters();
        }

        public static void ApplyFilters()
        {
            ClearFilters();
            if (!IsActive) return;

            Main.Mod?.Logger.Log($"Applying filters...");
            
            if (AvailableShaders.Count == 0) ScanFilters();

            foreach (var filterId in Main.settings.filters.enabledFilters)
            {
                var meta = AvailableShaders.Find(m => m.Id == filterId);
                if (meta != null)
                {
                    var config = Main.settings.filters.filterConfigs.Find(c => c.id == meta.Id);
                    if (config == null)
                    {
                        config = new FilterConfig { id = meta.Id, name = meta.ShaderName };
                        Main.settings.filters.filterConfigs.Add(config);
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
                        foreach (var param in config.paramsList)
                        {
                            if (mat.HasProperty(param.name))
                            {
                                if (param.type == "Float" || param.type == "Range")
                                {
                                    mat.SetFloat(param.name, param.values[0]);
                                }
                                else if (param.type == "Vector" || param.type == "Color")
                                {
                                    mat.SetVector(param.name, new Vector4(param.values[0], param.values[1], param.values[2], param.values[3]));
                                }
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

            if (Materials.Count == 1)
            {
                Graphics.Blit(source, destination, Materials[0]);
                return;
            }

            RenderTexture temp1 = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
            RenderTexture temp2 = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);

            RenderTexture currentIn = source;
            RenderTexture currentOut = temp1;

            for (int i = 0; i < Materials.Count; i++)
            {
                if (Materials[i] == null) continue;
                
                // For the last pass, blit directly to destination
                if (i == Materials.Count - 1)
                {
                    Graphics.Blit(currentIn, destination, Materials[i]);
                }
                else
                {
                    Graphics.Blit(currentIn, currentOut, Materials[i]);
                    
                    // Swap buffers for the next pass
                    if (currentIn == source) currentIn = temp1; // First pass: from source to temp1
                    
                    // Swap currentIn and currentOut between temp1 and temp2
                    RenderTexture nextOut = (currentOut == temp1) ? temp2 : temp1;
                    currentIn = currentOut;
                    currentOut = nextOut;
                }
            }

            RenderTexture.ReleaseTemporary(temp1);
            RenderTexture.ReleaseTemporary(temp2);
        }
    }
}