using System.Collections.Generic;
using Iris.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Iris.UI
{
    public class scrKeyViewerContainer : MonoBehaviour
    {
        public KeyviewerConfig Config { get; private set; } = null!;
        private readonly List<scrKeyComponent> _keys = new();
        private RectTransform _rectTransform = null!;

        public void Initialize(KeyviewerConfig config)
        {
            Config = config;
            gameObject.name = $"Keyviewer_{config.Name}";
            
            _rectTransform = gameObject.AddComponent<RectTransform>();
            _rectTransform.anchoredPosition = config.ContainerPosition;
            _rectTransform.localScale = Vector3.one * config.GlobalScale;
            
            var canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = config.GlobalOpacity;
            
            // 设置锚点为左下角，方便定位
            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.zero;
            _rectTransform.pivot = Vector2.zero;

            foreach (var keyConfig in config.Keys)
            {
                var keyObj = new GameObject($"Key_{keyConfig.Label}");
                keyObj.transform.SetParent(transform, false);
                var keyComp = keyObj.AddComponent<scrKeyComponent>();
                keyComp.Initialize(keyConfig, config);
                _keys.Add(keyComp);
            }
        }
    }

    public class scrKeyComponent : MonoBehaviour
    {
        private KeyConfig _config = null!;
        private KeyviewerConfig _parentConfig = null!;
        private RectTransform _rectTransform = null!;
        private Image _backgroundImage = null!;
        private Text _labelText = null!;
        private Outline _outline = null!;
        
        private bool _isPressed;

        public void Initialize(KeyConfig config, KeyviewerConfig parentConfig)
        {
            _config = config;
            _parentConfig = parentConfig;
            
            _rectTransform = gameObject.AddComponent<RectTransform>();
            _rectTransform.anchoredPosition = config.Position;
            _rectTransform.sizeDelta = config.Size;
            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.zero;
            _rectTransform.pivot = Vector2.zero;

            _backgroundImage = gameObject.AddComponent<Image>();
            
            float radius = config.BorderRadius >= 0 ? config.BorderRadius : parentConfig.GlobalBorderRadius;
            if (radius > 0)
            {
                // 运行时生成圆角纹理作为 Sprite
                _backgroundImage.sprite = CreateRoundedSprite((int)config.Size.x, (int)config.Size.y, radius, Color.white);
                _backgroundImage.type = Image.Type.Sliced; // 如果需要拉伸
            }
            
            _backgroundImage.color = config.BackgroundColor;

            if (!string.IsNullOrEmpty(config.BackgroundImage))
            {
                var tex = ResourceLoader.LoadTexture(config.BackgroundImage);
                if (tex != null)
                {
                    _backgroundImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
                }
            }

            if (config.BorderWidth > 0)
            {
                _outline = gameObject.AddComponent<Outline>();
                _outline.effectColor = config.BorderColor;
                _outline.effectDistance = new Vector2(config.BorderWidth, config.BorderWidth);
            }

            var textObj = new GameObject("Label");
            textObj.transform.SetParent(transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            _labelText = textObj.AddComponent<Text>();
            _labelText.text = config.Label;
            _labelText.alignment = TextAnchor.MiddleCenter;
            _labelText.color = config.ForegroundColor;
            _labelText.font = Font.CreateDynamicFontFromOSFont(config.FontName, config.FontSize);
            _labelText.fontSize = config.FontSize;
        }

        private Sprite CreateRoundedSprite(int w, int h, float r, Color c)
        {
            // 这里复用 UIUtils 的逻辑，但返回 Sprite
            var tex = UIUtils.GetCachedRoundedTex(w, h, r, c);
            return Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f);
        }

        private void OnDestroy()
        {
            if (_labelText != null && _labelText.font != null)
            {
                // 仅销毁动态创建的字体
                Destroy(_labelText.font);
            }
        }

        private void Update()
        {
            bool currentlyPressed = Input.GetKey(_config.KeyCode);
            
            if (currentlyPressed != _isPressed)
            {
                _isPressed = currentlyPressed;
                UpdateVisuals();
                
                if (_isPressed && _config.ShowRainingKey)
                {
                    SpawnRainingKey();
                }
            }
        }

        private void UpdateVisuals()
        {
            var hold = _config.OnHold;
            
            // 决定当前属性值
            Color bgCol = (_isPressed && hold?.BackgroundColor != null) ? hold.BackgroundColor.Value : _config.BackgroundColor;
            Color fgCol = (_isPressed && hold?.ForegroundColor != null) ? hold.ForegroundColor.Value : _config.ForegroundColor;
            Color brCol = (_isPressed && hold?.BorderColor != null) ? hold.BorderColor.Value : _config.BorderColor;
            float brWidth = (_isPressed && hold?.BorderWidth != null) ? hold.BorderWidth.Value : _config.BorderWidth;
            int fSize = (_isPressed && hold?.FontSize != null) ? hold.FontSize.Value : _config.FontSize;
            Vector2 size = (_isPressed && hold?.Size != null) ? hold.Size.Value : _config.Size;
            string label = (_isPressed && hold?.Label != null) ? hold.Label : _config.Label;

            // 应用视觉更新
            _backgroundImage.color = bgCol;
            _labelText.color = fgCol;
            _labelText.text = label;
            _labelText.fontSize = fSize;
            
            // 平滑尺寸更新 (如果需要)
            _rectTransform.sizeDelta = size;

            if (_outline != null)
            {
                _outline.effectColor = brCol;
                _outline.effectDistance = new Vector2(brWidth, brWidth);
            }
            
            // 不再使用硬编码的 0.95f 缩放，因为用户现在可以通过 onHold.size 实现 1.02x 效果
            transform.localScale = Vector3.one;
        }

        private void SpawnRainingKey()
        {
            var rainObj = new GameObject("RainingKey");
            rainObj.transform.SetParent(transform.parent, false); // 在容器下产生，不跟随按键缩放
            var rainComp = rainObj.AddComponent<RainingKey>();
            rainComp.Initialize(_config, _parentConfig);
        }
    }
}
