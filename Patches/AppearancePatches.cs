using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using HarmonyLib;
using Iris.Settings;

namespace Iris.Patches
{
    public static class AppearancePatches
    {
        private static Camera? targetCam;
        private static bool hooked = false;
        private static string lastScene = "";
        private static SkinMode lastMode = (SkinMode)(-1);

        // Single/PerScene Resources
        private static Texture2D? singleTex;
        private static RenderTexture? singleRT;
        private static VideoPlayer? singlePlayer;
        private static SkinConfig? currentConfig;

        // Slideshow Resources
        private static int slideshowIndex = 0;
        private static float slideshowTimer = 0f;
        private static Texture2D? slideshowTex;
        private static RenderTexture? slideshowRT;
        private static VideoPlayer? slideshowPlayer;

        private static Material? effectMat;
        private static readonly HashSet<scrFloor> menuFloors = new();
        private static readonly Dictionary<scrFloor, Renderer[]> floorRendererCache = new();
        private static readonly Dictionary<UnityEngine.Object, float> originalAlphaCache = new();
        private static readonly string[] MENU_SCENES = { "scnLevelSelect", "scnCLS", "scnTaroMenu0" };

        private static bool IsInExclusionScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return sceneName.Contains("scnEditor") || sceneName.Contains("scnLevelEditor");
        }

        public static void OnUpdate(float dt)
        {
            if (Main.settings.appearance.enableMenuSkin)
            {
                Main.settings.appearance.EnsureSlideshowSize();
                string sceneName = SceneManager.GetActiveScene().name;
                SkinMode currentMode = Main.settings.appearance.mode;
                bool sceneChanged = sceneName != lastScene;
                bool modeChanged = currentMode != lastMode;

                if (sceneChanged || modeChanged)
                {
                    if (sceneChanged)
                    {
                        menuFloors.Clear();
                        floorRendererCache.Clear();
                        originalAlphaCache.Clear();
                        lastScene = sceneName;
                    }
                    lastMode = currentMode;
                    
                    if (hooked)
                    {
                        Main.Logger?.Log($"AppearancePatches: Scene or Mode changed, re-initializing skin...");
                        if (currentMode == SkinMode.Slideshow) InitSlideshow();
                        else StartSingleSkin(sceneName);
                    }
                }

                if (IsMenuScene(sceneName) && !InExclusionScene())
                {
                    Camera? main = targetCam;
                    if (sceneChanged || main == null || !main.isActiveAndEnabled)
                    {
                        main = Camera.main;
                        Camera[] allCameras = Camera.allCameras;
                        foreach (var c in allCameras)
                        {
                            if (c.name == "Background Camera" && c.isActiveAndEnabled)
                            {
                                main = c;
                                break;
                            }
                        }
                    }

                    if (main != null)
                    {
                        if (hooked && main != targetCam)
                        {
                            Main.Logger?.Log($"AppearancePatches: Target camera changed from {targetCam?.name} to {main.name}, re-hooking...");
                            Disable();
                        }

                        if (!hooked)
                        {
                            Main.Logger?.Log($"AppearancePatches: Hooking to camera {main.name} in scene {sceneName}");
                            targetCam = main;
                            if (Main.settings.appearance.mode == SkinMode.Slideshow)
                            {
                                InitSlideshow();
                            }
                            else
                            {
                                StartSingleSkin(sceneName);
                            }
                            Camera.onPreCull += OnCameraPreCull;
                            hooked = true;
                        }

                        if (Main.settings.appearance.mode == SkinMode.Slideshow)
                        {
                            UpdateSlideshow(dt);
                        }
                    }
                }
                else if (hooked)
                {
                    Main.Logger?.Log($"AppearancePatches: Disabled because scene {sceneName} is not a menu scene or is excluded");
                    Disable();
                }
            }
            else if (hooked)
            {
                Main.Logger?.Log("AppearancePatches: Disabled because enableMenuSkin is false");
                Disable();
            }

            if (IsMenuScene(SceneManager.GetActiveScene().name) && !InExclusionScene())
            {
                ApplyTrackCustomization();
            }
        }

        private static bool InExclusionScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return sceneName.Contains("Editor");
        }

        private static void StartSingleSkin(string scene)
        {
            CleanupSingle();
            currentConfig = GetCurrentSkinConfig(scene);
            if (currentConfig == null) return;
            if (string.IsNullOrEmpty(currentConfig.path)) return;
            if (!File.Exists(currentConfig.path)) return;

            LoadSkin(currentConfig.path, ref singleTex, ref singleRT, ref singlePlayer, "Iris_SingleSkin_Player");
        }

        private static void InitSlideshow()
        {
            CleanupSlideshow();
            slideshowIndex = 0;
            slideshowTimer = 0f;

            var cfg = Main.settings.appearance.slideshowSkins[slideshowIndex];
            if (cfg == null || string.IsNullOrEmpty(cfg.path) || !File.Exists(cfg.path)) return;

            LoadSkin(cfg.path, ref slideshowTex, ref slideshowRT, ref slideshowPlayer, "Iris_Slideshow_Player");
        }

        private static void UpdateSlideshow(float dt)
        {
            if (Main.settings.appearance.slideshowCount <= 0) return;

            slideshowTimer += dt;
            if (slideshowTimer < Main.settings.appearance.slideDuration) return;

            slideshowTimer = 0f;
            slideshowIndex = (slideshowIndex + 1) % Main.settings.appearance.slideshowCount;

            var cfg = Main.settings.appearance.slideshowSkins[slideshowIndex];
            if (cfg == null || string.IsNullOrEmpty(cfg.path) || !File.Exists(cfg.path)) return;

            LoadSkin(cfg.path, ref slideshowTex, ref slideshowRT, ref slideshowPlayer, "Iris_Slideshow_Player");
        }

        private static void LoadSkin(string path, ref Texture2D? tex, ref RenderTexture? rt, ref VideoPlayer? player, string playerName)
        {
            try
            {
                string ext = Path.GetExtension(path).ToLower();
                bool isVideo = ext == ".mp4" || ext == ".mov" || ext == ".webm";

                if (isVideo)
                {
                    if (tex != null) { UnityEngine.Object.Destroy(tex); tex = null; }
                    if (player == null)
                    {
                        GameObject go = new(playerName);
                        UnityEngine.Object.DontDestroyOnLoad(go);
                        player = go.AddComponent<VideoPlayer>();
                    }
                    player.source = VideoSource.Url;
                    player.url = path;
                    player.isLooping = true;
                    player.audioOutputMode = VideoAudioOutputMode.None;

                    if (currentConfig != null) player.playbackSpeed = currentConfig.playbackSpeed;
                    else if (Main.settings.appearance.mode == SkinMode.Slideshow)
                    {
                        var cfg = Main.settings.appearance.slideshowSkins[slideshowIndex];
                        if (cfg != null) player.playbackSpeed = cfg.playbackSpeed;
                    }

                    if (rt != null) rt.Release();
                    rt = new RenderTexture(Screen.width, Screen.height, 0);
                    player.renderMode = VideoRenderMode.RenderTexture;
                    player.targetTexture = rt;
                    player.Prepare();
                    player.Play();
                }
                else
                {
                    if (player != null) { player.Stop(); UnityEngine.Object.Destroy(player.gameObject); player = null; }
                    if (rt != null) { rt.Release(); UnityEngine.Object.Destroy(rt); rt = null; }

                    if (tex != null) UnityEngine.Object.Destroy(tex);
                    byte[] data = File.ReadAllBytes(path);
                    tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (ImageConversion.LoadImage(tex, data))
                    {
                        tex.wrapMode = TextureWrapMode.Clamp;
                        tex.filterMode = FilterMode.Bilinear;
                    }
                }
            }
            catch (Exception e)
            {
                Main.Logger?.Error($"AppearancePatches: Error loading skin: {e}");
            }
        }

        private static void OnCameraPreCull(Camera cam)
        {
            if (cam != targetCam) return;

            Texture? tex;
            SkinConfig? cfg;

            if (Main.settings.appearance.mode == SkinMode.Slideshow)
            {
                tex = (slideshowRT != null && slideshowPlayer != null && slideshowPlayer.isPlaying) ? slideshowRT : slideshowTex;
                cfg = slideshowIndex < Main.settings.appearance.slideshowSkins.Length
                    ? Main.settings.appearance.slideshowSkins[slideshowIndex]
                    : null;
            }
            else
            {
                tex = (singleRT != null && singlePlayer != null && singlePlayer.isPlaying) ? singleRT : singleTex;
                cfg = currentConfig;
            }

            if (tex == null || cfg == null) return;

            effectMat ??= new Material(Shader.Find("Sprites/Default"));
            effectMat.mainTexture = tex;
            Color finalTint = CalculateFinalTint(cfg);

            GL.PushMatrix();
            GL.LoadOrtho();
            effectMat.SetPass(0);

            float scale = Mathf.Max(cfg.scale, 0.01f);
            float x = (1f - scale) * 0.5f + cfg.offsetX;
            float y = (1f - scale) * 0.5f - cfg.offsetY;

            GL.Begin(GL.QUADS);
            GL.Color(finalTint);
            GL.TexCoord2(0, 0); GL.Vertex3(x, y, 0);
            GL.TexCoord2(0, 1); GL.Vertex3(x, y + scale, 0);
            GL.TexCoord2(1, 1); GL.Vertex3(x + scale, y + scale, 0);
            GL.TexCoord2(1, 0); GL.Vertex3(x + scale, y, 0);
            GL.End();

            GL.PopMatrix();
        }

        private static Color CalculateFinalTint(SkinConfig cfg)
        {
            float b = cfg.brightness;
            float s = cfg.saturation;
            float c = cfg.contrast;
            float h = cfg.hue;
            float opacity = cfg.opacity;

            float contrastGain = c;
            float contrastOffset = 0.5f * (1.0f - c);

            Color finalTint = new(
                (b * contrastGain) + contrastOffset,
                (b * contrastGain) + contrastOffset,
                (b * contrastGain) + contrastOffset,
                opacity
            );

            if (Mathf.Abs(h) > 0.1f)
            {
                float angle = h * Mathf.Deg2Rad;
                float cosA = Mathf.Cos(angle);
                float sinA = Mathf.Sin(angle);
                float r = finalTint.r, g = finalTint.g, b_ = finalTint.b;
                finalTint.r = (.299f + .701f * cosA + .168f * sinA) * r + (.587f - .587f * cosA + .330f * sinA) * g + (.114f - .114f * cosA - .497f * sinA) * b_;
                finalTint.g = (.299f - .299f * cosA - .328f * sinA) * r + (.587f + .413f * cosA + .035f * sinA) * g + (.114f - .114f * cosA + .292f * sinA) * b_;
                finalTint.b = (.299f - .300f * cosA + 1.25f * sinA) * r + (.587f - .588f * cosA - 1.05f * sinA) * g + (.114f + .886f * cosA - .203f * sinA) * b_;
            }

            if (Mathf.Abs(s - 1.0f) > 0.01f)
            {
                float lum = 0.299f * finalTint.r + 0.587f * finalTint.g + 0.114f * finalTint.b;
                finalTint.r = lum + s * (finalTint.r - lum);
                finalTint.g = lum + s * (finalTint.g - lum);
                finalTint.b = lum + s * (finalTint.b - lum);
            }

            return finalTint;
        }

        private static SkinConfig? GetCurrentSkinConfig(string scene)
        {
            return Main.settings.appearance.mode switch
            {
                SkinMode.SingleGlobal => Main.settings.appearance.globalSkin,
                SkinMode.PerScene when scene == "scnLevelSelect" => Main.settings.appearance.mainUISkin,
                SkinMode.PerScene when scene == "scnCLS" => Main.settings.appearance.clsSkin,
                SkinMode.PerScene when scene == "scnTaroMenu0" => Main.settings.appearance.dlcUISkin,
                _ => null
            };
        }

        private static bool IsMenuScene(string sceneName)
        {
            foreach (string s in MENU_SCENES) if (s == sceneName) return true;
            return false;
        }

        public static void Disable()
        {
            Camera.onPreCull -= OnCameraPreCull;
            CleanupSingle();
            CleanupSlideshow();
            targetCam = null;
            hooked = false;
        }

        private static void CleanupSingle()
        {
            if (singleTex != null) { UnityEngine.Object.Destroy(singleTex); singleTex = null; }
            if (singlePlayer != null) { singlePlayer.Stop(); UnityEngine.Object.Destroy(singlePlayer.gameObject); singlePlayer = null; }
            if (singleRT != null) { singleRT.Release(); UnityEngine.Object.Destroy(singleRT); singleRT = null; }
            currentConfig = null;
        }

        private static void CleanupSlideshow()
        {
            if (slideshowTex != null) { UnityEngine.Object.Destroy(slideshowTex); slideshowTex = null; }
            if (slideshowPlayer != null) { slideshowPlayer.Stop(); UnityEngine.Object.Destroy(slideshowPlayer.gameObject); slideshowPlayer = null; }
            if (slideshowRT != null) { slideshowRT.Release(); UnityEngine.Object.Destroy(slideshowRT); slideshowRT = null; }
            slideshowTimer = 0f;
        }

        [HarmonyPatch(typeof(scrFloor), "Start")]
        public static class FloorStartPatch
        {
            public static void Postfix(scrFloor __instance)
            {
                if (IsMenuScene(SceneManager.GetActiveScene().name))
                {
                    menuFloors.Add(__instance);
                    UpdateFloorStyle(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(scrFloor), nameof(scrFloor.Reset))]
        public static class FloorRefreshColorPatch
        {
            public static void Postfix(scrFloor __instance)
            {
                if (IsMenuScene(SceneManager.GetActiveScene().name)) UpdateFloorStyle(__instance);
            }
        }

        public static void UpdateFloorStyle(scrFloor floor)
        {
            if (!Main.settings.appearance.enableTrackCustomization) return;
            if (floor == null) return;

            var settings = Main.settings.appearance;
            Color c = settings.trackColor;
            float b = settings.trackBrightness;
            float a = settings.trackOpacity;

            float GetTargetAlphaForObj(UnityEngine.Object? obj, float currentAlpha)
            {
                if (obj == null) return Mathf.Min(currentAlpha, a);
                if (!originalAlphaCache.TryGetValue(obj, out float originalAlpha))
                {
                    originalAlpha = currentAlpha;
                    originalAlphaCache[obj] = originalAlpha;
                }
                return Mathf.Min(originalAlpha, a);
            }

            Color ApplyToColor(UnityEngine.Object obj, Color originalColor)
            {
                float r = settings.trackColorR ? (c.r * b) : originalColor.r;
                float g = settings.trackColorG ? (c.g * b) : originalColor.g;
                float b_ = settings.trackColorB ? (c.b * b) : originalColor.b;
                return new Color(r, g, b_, GetTargetAlphaForObj(obj, originalColor.a));
            }

            SpriteRenderer mainSr = floor.GetComponent<SpriteRenderer>();
            if (mainSr != null)
            {
                mainSr.color = ApplyToColor(mainSr, mainSr.color);
            }

            if (!floorRendererCache.TryGetValue(floor, out var renderers))
            {
                renderers = floor.GetComponentsInChildren<Renderer>(true);
                floorRendererCache[floor] = renderers;
            }

            foreach (var r in renderers)
            {
                if (r == null) continue;

                if (r is SpriteRenderer sr)
                {
                    sr.color = ApplyToColor(sr, sr.color);
                }
                else
                {
                    try
                    {
                        if (r.material != null)
                        {
                            r.material.color = ApplyToColor(r.material, r.material.color);
                        }
                    }
                    catch { }
                }
            }

            if (floor.legacyFloorSpriteRenderer != null)
            {
                var sr = floor.legacyFloorSpriteRenderer;
                sr.color = ApplyToColor(sr, sr.color);
            }

            if (floor.floorRenderer != null && floor.floorRenderer is FloorSpriteRenderer fsr && fsr.renderer != null && fsr.renderer is SpriteRenderer fsrSr)
            {
                fsrSr.color = ApplyToColor(fsrSr, fsrSr.color);
            }
        }

        [HarmonyPatch(typeof(scnLevelSelect), "Update")]
        [HarmonyPatch(typeof(scnCLS), "Update")]
        [HarmonyPatch(typeof(scnTaroMenu0), "Update")]
        public static class EditorFloorUpdatePatch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ret)
                    {
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AppearancePatches), nameof(ApplyTrackCustomization)));
                    }
                    yield return codes[i];
                }
            }
        }

        private static Color _lastTrackColor;
        private static float _lastTrackBrightness;
        private static float _lastTrackOpacity;
        private static bool _lastTrackColorR;
        private static bool _lastTrackColorG;
        private static bool _lastTrackColorB;

        public static void ApplyTrackCustomization()
        {
            var settings = Main.settings.appearance;
            if (!settings.enableTrackCustomization) return;

            string sceneName = SceneManager.GetActiveScene().name;
            if (!IsMenuScene(sceneName) || IsInExclusionScene()) return;

            bool settingsDirty = settings.trackColor != _lastTrackColor ||
                                 !Mathf.Approximately(settings.trackBrightness, _lastTrackBrightness) ||
                                 !Mathf.Approximately(settings.trackOpacity, _lastTrackOpacity) ||
                                 settings.trackColorR != _lastTrackColorR ||
                                 settings.trackColorG != _lastTrackColorG ||
                                 settings.trackColorB != _lastTrackColorB;

            if (!settingsDirty && menuFloors.Count == 0 && (scnLevelSelect.instance == null || scnLevelSelect.instance.editorFloor == null))
            {
                return;
            }

            if (settingsDirty)
            {
                _lastTrackColor = settings.trackColor;
                _lastTrackBrightness = settings.trackBrightness;
                _lastTrackOpacity = settings.trackOpacity;
                _lastTrackColorR = settings.trackColorR;
                _lastTrackColorG = settings.trackColorG;
                _lastTrackColorB = settings.trackColorB;
            }

            foreach (var floor in menuFloors)
            {
                if (floor != null)
                {
                    UpdateFloorStyle(floor);
                }
            }

            if (scnLevelSelect.instance != null && scnLevelSelect.instance.editorFloor != null)
            {
                Color c = settings.trackColor;
                float b = settings.trackBrightness;
                float a = settings.trackOpacity;
                UpdateFloorRenderers(scnLevelSelect.instance.editorFloor, c, b, a);
            }
        }

        private static void UpdateFloorRenderers(GameObject floorGo, Color color, float brightness, float opacity)
        {
            var settings = Main.settings.appearance;
            var renderers = floorGo.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r.name.Contains("Glow")) continue;

                if (r is SpriteRenderer sr)
                {
                    float r_ = settings.trackColorR ? (color.r * brightness) : sr.color.r;
                    float g = settings.trackColorG ? (color.g * brightness) : sr.color.g;
                    float b = settings.trackColorB ? (color.b * brightness) : sr.color.b;
                    sr.color = new Color(r_, g, b, opacity);
                }
                else
                {
                    try
                    {
                        if (r.material != null)
                        {
                            float r_ = settings.trackColorR ? (color.r * brightness) : r.material.color.r;
                            float g = settings.trackColorG ? (color.g * brightness) : r.material.color.g;
                            float b = settings.trackColorB ? (color.b * brightness) : r.material.color.b;
                            r.material.color = new Color(r_, g, b, opacity);
                        }
                    }
                    catch { }
                }
            }
        }
    }
}
