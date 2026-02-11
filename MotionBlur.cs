using System;
using System.IO;
using UnityEngine;
using UnityModManagerNet;

namespace ADOFAIShaders
{
    public static class Main
    {
        private static UnityModManager.ModEntry mod;
        private static Shader motionBlurShader;
        private static Camera currentCamera;
        private static float checkTimer;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
            mod.Logger.Log("[MotionBlur] Mod loading started.");

            LoadShader(modEntry);

            modEntry.OnUpdate = OnUpdate;
            modEntry.OnUnload = OnUnload;

            return true;
        }

        private static void LoadShader(UnityModManager.ModEntry modEntry)
        {
            string bundlePath = Path.Combine(modEntry.Path, "shaders", "motionblur");

            if (!File.Exists(bundlePath))
            {
                mod.Logger.Error("[MotionBlur] AssetBundle not found: " + bundlePath);
                return;
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                mod.Logger.Error("[MotionBlur] Failed to load AssetBundle.");
                return;
            }

            Shader[] shaders = bundle.LoadAllAssets<Shader>();
            if (shaders == null || shaders.Length == 0)
            {
                mod.Logger.Error("[MotionBlur] No shader found in AssetBundle.");
                return;
            }

            motionBlurShader = shaders[0];
            mod.Logger.Log("[MotionBlur] Loaded shader: " + motionBlurShader.name);
        }

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
        {
            checkTimer += deltaTime;
            if (checkTimer < 1f)
                return;

            checkTimer = 0f;

            if (motionBlurShader == null)
                return;

            Camera cam = Camera.main;
            if (cam == null)
                return;

            if (currentCamera != cam)
            {
                currentCamera = cam;
                AttachEffect(cam);
            }
            else
            {
                if (cam.GetComponent<MotionBlurBehaviour>() == null)
                {
                    AttachEffect(cam);
                }
            }
        }

        private static void AttachEffect(Camera cam)
        {
            MotionBlurBehaviour effect = cam.GetComponent<MotionBlurBehaviour>();
            if (effect == null)
            {
                effect = cam.gameObject.AddComponent<MotionBlurBehaviour>();
                mod.Logger.Log("[MotionBlur] Motion blur attached to camera: " + cam.name);
            }

            effect.Initialize(motionBlurShader, mod);
        }

        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            mod.Logger.Log("[MotionBlur] Mod unloaded.");
            return true;
        }
    }

    public class MotionBlurBehaviour : MonoBehaviour
    {
        private Material material;
        private RenderTexture historyRT;
        private Shader shader;
        private UnityModManager.ModEntry mod;

        public float blurStrength = 0.8f;

        public void Initialize(Shader s, UnityModManager.ModEntry m)
        {
            shader = s;
            mod = m;

            if (material == null && shader != null)
            {
                material = new Material(shader);
                mod.Logger.Log("[MotionBlur] Material created.");
            }
        }

        private void OnDestroy()
        {
            if (historyRT != null)
            {
                historyRT.Release();
                historyRT = null;
                mod.Logger.Log("[MotionBlur] History RT released.");
            }
        }

        private RenderTexture tempRT;

private void OnRenderImage(RenderTexture src, RenderTexture dest)
{
    if (material == null)
    {
        Graphics.Blit(src, dest);
        return;
    }

    if (historyRT == null ||
        historyRT.width != src.width ||
        historyRT.height != src.height)
    {
        if (historyRT != null)
            historyRT.Release();

        historyRT = new RenderTexture(src.width, src.height, 0);
        historyRT.Create();

        Graphics.Blit(src, historyRT);
    }

    if (tempRT == null ||
        tempRT.width != src.width ||
        tempRT.height != src.height)
    {
        if (tempRT != null)
            tempRT.Release();

        tempRT = new RenderTexture(src.width, src.height, 0);
        tempRT.Create();
    }

    material.SetTexture("_HistoryTex", historyRT);
    material.SetFloat("_BlurStrength", blurStrength);

    // ① 先输出到 tempRT（方向统一）
    Graphics.Blit(src, tempRT, material);

    // ② 再输出到屏幕
    Graphics.Blit(tempRT, dest);

    // ③ ⭐ 用 tempRT 写入 history（永远不要用 dest）
    Graphics.Blit(tempRT, historyRT);
}
    }
}
