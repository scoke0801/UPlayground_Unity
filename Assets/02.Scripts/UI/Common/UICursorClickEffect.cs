using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    /// <summary>
    /// 마우스 커서가 표시된 동안 클릭 위치에 전역 리플 FX를 표시한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UICursorClickEffect : MonoBehaviour
    {
        private sealed class Ripple
        {
            public RectTransform RectTransform;
            public Image Image;
            public float StartedAt;
            public bool IsPlaying;
        }

        private const int TextureSize = 64;

        [Header("리플")]
        [SerializeField, Min(1)] private int _poolSize = 8;
        [SerializeField, Min(0.01f)] private float _duration = 0.28f;
        [SerializeField, Min(0f)] private float _startSize = 15f;
        [SerializeField, Min(0f)] private float _endSize = 60f;
        [SerializeField] private Color _color = new(0.6f, 0.9f, 1f, 0.9f);
        [SerializeField] private bool _includeSecondaryButtons = true;

        private readonly List<Ripple> _ripples = new();
        private RectTransform _rootRect;
        private Canvas _canvas;
        private Texture2D _rippleTexture;
        private Sprite _rippleSprite;
        private int _nextRippleIndex;

        private void Awake()
        {
            _rootRect = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
            CreateRippleSprite();
            EnsurePool();
        }

        private void LateUpdate()
        {
            // System Canvas에 UI가 나중에 추가되더라도 클릭 FX가 항상 최상단에 남도록 한다.
            transform.SetAsLastSibling();

            Mouse mouse = Mouse.current;
            if (mouse != null && Cursor.visible && WasClickedThisFrame(mouse))
                Play(mouse.position.ReadValue());

            AnimateRipples();
        }

        private bool WasClickedThisFrame(Mouse mouse)
        {
            if (mouse.leftButton.wasPressedThisFrame)
                return true;

            return _includeSecondaryButtons
                   && (mouse.rightButton.wasPressedThisFrame
                       || mouse.middleButton.wasPressedThisFrame);
        }

        private void Play(Vector2 screenPosition)
        {
            EnsurePool();
            if (_ripples.Count == 0)
                return;

            Camera eventCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rootRect,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPosition))
            {
                return;
            }

            Ripple ripple = _ripples[_nextRippleIndex];
            _nextRippleIndex = (_nextRippleIndex + 1) % _ripples.Count;

            ripple.RectTransform.anchoredPosition = localPosition;
            ripple.RectTransform.sizeDelta = Vector2.one * _startSize;
            ripple.Image.color = _color;
            ripple.Image.gameObject.SetActive(true);
            ripple.StartedAt = Time.unscaledTime;
            ripple.IsPlaying = true;
        }

        private void AnimateRipples()
        {
            float duration = Mathf.Max(0.01f, _duration);

            foreach (Ripple ripple in _ripples)
            {
                if (!ripple.IsPlaying)
                    continue;

                float progress = Mathf.Clamp01((Time.unscaledTime - ripple.StartedAt) / duration);
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                float size = Mathf.LerpUnclamped(_startSize, _endSize, easedProgress);

                ripple.RectTransform.sizeDelta = Vector2.one * size;

                Color color = _color;
                color.a *= 1f - progress;
                ripple.Image.color = color;

                if (progress < 1f)
                    continue;

                ripple.IsPlaying = false;
                ripple.Image.gameObject.SetActive(false);
            }
        }

        private void EnsurePool()
        {
            int targetCount = Mathf.Max(1, _poolSize);
            while (_ripples.Count < targetCount)
            {
                var rippleObject = new GameObject(
                    $"CursorClickRipple_{_ripples.Count + 1}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

                var rippleRect = (RectTransform)rippleObject.transform;
                rippleRect.SetParent(transform, false);
                rippleRect.anchorMin = new Vector2(0.5f, 0.5f);
                rippleRect.anchorMax = new Vector2(0.5f, 0.5f);
                rippleRect.pivot = new Vector2(0.5f, 0.5f);

                var image = rippleObject.GetComponent<Image>();
                image.sprite = _rippleSprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                rippleObject.SetActive(false);

                _ripples.Add(new Ripple
                {
                    RectTransform = rippleRect,
                    Image = image
                });
            }
        }

        private void CreateRippleSprite()
        {
            _rippleTexture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = "UICursorClickRippleTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            var pixels = new Color32[TextureSize * TextureSize];
            Vector2 center = Vector2.one * (TextureSize - 1) * 0.5f;
            float maxRadius = TextureSize * 0.5f;

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float normalizedDistance = Vector2.Distance(new Vector2(x, y), center) / maxRadius;
                    float outerAlpha = 1f - SmoothThreshold(0.82f, 0.94f, normalizedDistance);
                    float innerAlpha = SmoothThreshold(0.62f, 0.74f, normalizedDistance);
                    byte alpha = (byte)Mathf.RoundToInt(255f * outerAlpha * innerAlpha);
                    pixels[y * TextureSize + x] = alpha > 0
                        ? new Color32(255, 255, 255, alpha)
                        : new Color32(0, 0, 0, 0);
                }
            }

            _rippleTexture.SetPixels32(pixels);
            _rippleTexture.Apply(false, true);

            _rippleSprite = Sprite.Create(
                _rippleTexture,
                new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                TextureSize);
            _rippleSprite.name = "UICursorClickRippleSprite";
            _rippleSprite.hideFlags = HideFlags.DontSave;
        }

        private static float SmoothThreshold(float min, float max, float value)
        {
            float t = Mathf.InverseLerp(min, max, value);
            return t * t * (3f - 2f * t);
        }

        private void OnDestroy()
        {
            if (_rippleSprite != null)
                Destroy(_rippleSprite);

            if (_rippleTexture != null)
                Destroy(_rippleTexture);
        }
    }
}
