using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Iris.Settings
{
    public static class FilterManager
    {
        public static List<string> ScannedFilters { get; private set; } = new();
        public static bool IsActive { get; private set; } = false;
        
        private static List<Material> _activeMaterials = new();
        private static GameObject? _proxyObject;

        // 测试用的内置材质
        private static Material? _testMaterial;

        public static string FilterPath => Path.Combine(Main.Mod!.Path, "Resources", "shaderpacks");

        public static void ScanFilters()
        {
            if (!Directory.Exists(FilterPath)) Directory.CreateDirectory(FilterPath);
            ScannedFilters.Clear();
            
            // 现在支持扫描所有后缀，甚至是文件夹名
            var files = Directory.GetFiles(FilterPath, "*");
            foreach (var file in files)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext == ".shaderpack" || ext == ".bundle" || ext == ".mat")
                {
                    ScannedFilters.Add(Path.GetFileName(file));
                }
            }
            
            Main.Mod?.Logger.Log($"Scanned {ScannedFilters.Count} filter assets.");
        }

        public static void SetPlayState(bool playing)
        {
            if (playing && Main.config.filters.enableFilters)
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

            // 1. 加载用户勾选的外部资源
            foreach (var packName in Main.config.filters.enabledFilters)
            {
                LoadPack(packName);
            }

            // 2. 如果开启了测试模式，加入测试材质
            if (Main.config.filters.enableTestMode)
            {
                if (_testMaterial == null) _testMaterial = CreateTestMaterial();
                if (_testMaterial != null) _activeMaterials.Add(_testMaterial);
            }
            
            UpdateCameraEffects();
        }

        private static void LoadPack(string name)
        {
            string path = Path.Combine(FilterPath, name);
            
            // 如果是材质文件直接加载
            if (name.EndsWith(".mat"))
            {
                // 注意：Unity 运行时加载 loose material 通常需要 AssetBundle 或 Resources
                // 这里假设是打包好的 Bundle
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null) return;

            // 增强：加载所有的 Material
            var mats = bundle.LoadAllAssets<Material>();
            _activeMaterials.AddRange(mats);

            // 增强：加载所有的 Shader 并自动创建材质（如果没有材质的话）
            var shaders = bundle.LoadAllAssets<Shader>();
            foreach (var s in shaders)
            {
                // 如果这个 Shader 没被包含在上面的材质里，我们可以手动创建一个
                _activeMaterials.Add(new Material(s));
            }

            bundle.Unload(false);
        }

        private static void UpdateCameraEffects()
        {
            var cam = Camera.main;
            if (cam == null) return;

            if (_proxyObject == null)
            {
                _proxyObject = new GameObject("IrisFilterProxy");
                var component = _proxyObject.AddComponent<IrisPostProcessHandler>();
                component.Materials = _activeMaterials;
            }
            else
            {
                _proxyObject.GetComponent<IrisPostProcessHandler>().Materials = _activeMaterials;
            }
        }

        private static void ClearFilters()
        {
            if (_proxyObject != null) UnityEngine.Object.Destroy(_proxyObject);
            _proxyObject = null;
            _activeMaterials.Clear();
        }

        // 创建一个简单的反色 Shader 材质用于无文件测试
        private static Material? CreateTestMaterial()
        {
            // 这是一个最简单的 Unity 屏幕空间反色 Shader 字符串
            string shaderCode = @"
                Shader ""Hidden/IrisTestInvert"" {
                    Properties { _MainTex (""Texture"", 2D) = ""white"" {} }
                    SubShader {
                        Pass {
                            CGPROGRAM
                            #pragma vertex vert
                            #pragma fragment frag
                            #include ""UnityCG.cginc""
                            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
                            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };
                            v2f vert (appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
                            sampler2D _MainTex;
                            fixed4 frag (v2f i) : SV_Target {
                                fixed4 col = tex2D(_MainTex, i.uv);
                                return fixed4(1.0 - col.rgb, col.a); // 反色逻辑
                            }
                            ENDCG
                        }
                    }
                }";
            
            try {
                return new Material(shaderCode);
            } catch {
                Main.Mod?.Logger.Error("Failed to compile internal test shader.");
                return null;
            }
        }
    }

    // 后处理渲染代理组件
    public class IrisPostProcessHandler : MonoBehaviour
    {
        public List<Material> Materials = new();

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (Materials.Count == 0)
            {
                Graphics.Blit(source, destination);
                return;
            }

            RenderTexture temp1 = RenderTexture.GetTemporary(source.width, source.height);
            RenderTexture temp2 = RenderTexture.GetTemporary(source.width, source.height);

            Graphics.Blit(source, temp1);

            for (int i = 0; i < Materials.Count; i++)
            {
                var target = (i % 2 == 0) ? temp2 : temp1;
                var input = (i % 2 == 0) ? temp1 : temp2;
                Graphics.Blit(input, target, Materials[i]);
            }

            Graphics.Blit((Materials.Count % 2 == 0) ? temp1 : temp2, destination);

            RenderTexture.ReleaseTemporary(temp1);
            RenderTexture.ReleaseTemporary(temp2);
        }
    }
}