using Iris.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Iris.UI
{
    public class RainingKey : MonoBehaviour
    {
        private KeyConfig _config = null!;
        private KeyviewerConfig _parentConfig = null!;
        private RectTransform _rectTransform = null!;
        private Image _image = null!;
        
        private float _startY;
        private float _headY;
        private float _tailY;
        private bool _isKeyHeld = true;

        public void Initialize(KeyConfig config, KeyviewerConfig parentConfig)
        {
            _config = config;
            _parentConfig = parentConfig;
            
            _rectTransform = gameObject.AddComponent<RectTransform>();
            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.zero;
            _rectTransform.pivot = new Vector2(0.5f, 0f); // 轴点在底端中心

            _image = gameObject.AddComponent<Image>();
            _image.color = (_config.OnHold?.BackgroundColor != null) ? _config.OnHold.BackgroundColor.Value : _config.BackgroundColor;
            
            _startY = config.Position.y;
            _headY = _startY + config.Size.y;
            _tailY = _startY;
            
            UpdateTransform();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float speed = _parentConfig.RainingKeySpeed;

            // 顶端始终向上飞
            _headY += speed * dt;

            if (_isKeyHeld)
            {
                if (Input.GetKey(_config.KeyCode))
                {
                    // 按住时，底端固定在原位
                    _tailY = _startY;
                }
                else
                {
                    // 松开瞬间，记录状态
                    _isKeyHeld = false;
                }
            }
            
            if (!_isKeyHeld)
            {
                // 松开后，底端也向上飞
                _tailY += speed * dt;
            }

            UpdateTransform();

            // 消失逻辑
            if (_tailY - _startY > _parentConfig.RainingKeyThreshold)
            {
                float alpha = 1f - (_tailY - _startY - _parentConfig.RainingKeyThreshold) / 200f;
                Color c = _image.color;
                c.a = alpha;
                _image.color = c;

                if (alpha <= 0) Destroy(gameObject);
            }
        }

        private void UpdateTransform()
        {
            _rectTransform.anchoredPosition = new Vector2(_config.Position.x + _config.Size.x / 2f, _tailY);
            _rectTransform.sizeDelta = new Vector2(_config.Size.x, _headY - _tailY);
        }
    }
}
