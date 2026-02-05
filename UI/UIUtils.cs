using System;
using UnityEngine;

namespace Iris.UI
{
    public static class UIUtils
    {
        private static GUIStyle? _cardStyle;
        private static GUIStyle? _headerStyle;
        private static GUIStyle? _buttonStyle;
        private static GUIStyle? _labelStyle;
        private static GUIStyle? _textFieldStyle;
        private static GUIStyle? _infoBoxStyle;
        private static GUIStyle? _warningBoxStyle;
        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> _textureCache = [];

        public static GUIStyle CardStyle => _cardStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle HeaderStyle => _headerStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle ButtonStyle => _buttonStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle LabelStyle => _labelStyle ?? throw new InvalidOperationException("UI not initialized");
        public static GUIStyle TextFieldStyle => _textFieldStyle ?? throw new InvalidOperationException("UI not initialized");

        public static void InitializeStyles()
        {
            if (_cardStyle != null) return;

            // Android 14 / Material 3 Dark Palette
            Color surfaceContainer = new(0.13f, 0.13f, 0.15f);
            Color primary = new(0.66f, 0.76f, 1.0f);
            Color onSurface = new(0.88f, 0.88f, 0.9f);
            Color surfaceContainerHigh = new(0.17f, 0.17f, 0.19f);
            Color errorContainer = new(0.35f, 0.1f, 0.1f);
            Color onErrorContainer = new(1.0f, 0.7f, 0.7f);
            Color infoContainer = new(0.1f, 0.2f, 0.35f);
            Color onInfoContainer = new(0.7f, 0.85f, 1.0f);

            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 12, 12),
                margin = new RectOffset(0, 0, 6, 6),
                normal = { background = GetCachedRoundedTex(128, 128, 12, surfaceContainer) }
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Normal,
                normal = { textColor = primary },
                margin = new RectOffset(0, 0, 0, 8)
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = onSurface },
                alignment = TextAnchor.MiddleLeft
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fixedHeight = 28,
                padding = new RectOffset(12, 12, 0, 0),
                normal = { background = GetCachedRoundedTex(64, 64, 8, surfaceContainerHigh), textColor = primary },
                hover = { background = GetCachedRoundedTex(64, 64, 8, primary * 0.2f), textColor = Color.white },
                active = { background = GetCachedRoundedTex(64, 64, 8, primary), textColor = Color.black }
            };

            _textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                fixedHeight = 24,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(4, 4, 0, 0),
                normal = { background = GetCachedRoundedTex(64, 64, 4, surfaceContainerHigh), textColor = onSurface },
                focused = { background = GetCachedRoundedTex(64, 64, 4, surfaceContainerHigh), textColor = Color.white }
            };

            _infoBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 4, 4),
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                normal = { background = GetCachedRoundedTex(64, 64, 8, infoContainer), textColor = onInfoContainer }
            };

            _warningBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 4, 4),
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                normal = { background = GetCachedRoundedTex(64, 64, 8, errorContainer), textColor = onErrorContainer }
            };
        }

        public static void DrawInfoBox(string text, bool isError = false)
        {
            GUILayout.Box(text, isError ? _warningBoxStyle : _infoBoxStyle, GUILayout.ExpandWidth(true));
        }

        public static bool M3Switch(bool value, string label)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(32));
            if (!string.IsNullOrEmpty(label)) GUILayout.Label(label, _labelStyle, GUILayout.ExpandWidth(true));
            
            Color trackColor = value ? new(0.66f, 0.76f, 1.0f) : new(0.28f, 0.28f, 0.31f);
            Color thumbColor = value ? new(0.0f, 0.2f, 0.4f) : new(0.55f, 0.55f, 0.58f);

            Rect rect = GUILayoutUtility.GetRect(40, 24, GUILayout.Width(40), GUILayout.Height(24));
            
            GUI.color = trackColor;
            GUI.DrawTexture(rect, GetCachedRoundedTex(64, 32, 16, Color.white));
            
            float thumbSize = 18;
            float thumbX = value ? rect.x + rect.width - thumbSize - 3 : rect.x + 3;
            Rect thumbRect = new(thumbX, rect.y + (rect.height - thumbSize) / 2, thumbSize, thumbSize);
            GUI.color = thumbColor;
            GUI.DrawTexture(thumbRect, GetCachedRoundedTex(32, 32, 16, Color.white));
            
            GUI.color = Color.white;
            if (GUI.Button(rect, "", GUIStyle.none)) value = !value;
            
            GUILayout.EndHorizontal();
            return value;
        }

        public static Texture2D GetCachedRoundedTex(int width, int height, float radius, Color col)
        {
            string key = $"{width}_{height}_{radius}_{col.r}_{col.g}_{col.b}_{col.a}";
            if (_textureCache.TryGetValue(key, out Texture2D tex) && tex != null) return tex;

            tex = MakeRoundedTex(width, height, radius, col);
            tex.hideFlags = HideFlags.HideAndDontSave;
            _textureCache[key] = tex;
            return tex;
        }

        private static Texture2D MakeRoundedTex(int width, int height, float radius, Color col)
        {
            Texture2D tex = new(width, height);
            Color[] pix = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = Math.Min(x, width - 1 - x);
                    float dy = Math.Min(y, height - 1 - y);

                    if (dx < radius && dy < radius)
                    {
                        float d = (float)Math.Sqrt(Math.Pow(radius - dx, 2) + Math.Pow(radius - dy, 2));
                        if (d > radius)
                        {
                            pix[y * width + x] = Color.clear;
                        }
                        else
                        {
                            float alpha = Math.Min(1, radius + 0.5f - d);
                            pix[y * width + x] = new Color(col.r, col.g, col.b, col.a * alpha);
                        }
                    }
                    else
                    {
                        pix[y * width + x] = col;
                    }
                }
            }

            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
    }
}
