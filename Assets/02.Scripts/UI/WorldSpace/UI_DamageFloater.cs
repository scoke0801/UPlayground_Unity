using TMPro;
using UnityEngine;
using UPlayGround.Data.UI;

namespace UPlayGround.UI
{
    /// <summary>
    /// 피격 지점 위로 떠오르는 데미지 숫자 하나.
    /// UI_WorldSpaceHudLayer가 풀에서 꺼내서 Play()를 호출한다.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UI_DamageFloater : MonoBehaviour
    {
        private TextMeshProUGUI      _text;
        private RectTransform        _rect;
        private Camera               _camera;
        private Canvas               _canvas;
        private DamageFloaterConfigSO _config;
        private UI_WorldSpaceHudLayer _owner; // 풀 반환 대상

        private Vector3 _worldOrigin;
        private float   _elapsed;
        private bool    _isPlaying;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            _rect = GetComponent<RectTransform>();
        }

        public void Init(Camera cam, Canvas canvas, DamageFloaterConfigSO config, UI_WorldSpaceHudLayer owner)
        {
            _camera = cam;
            _canvas = canvas;
            _config = config;
            _owner  = owner;
        }

        public void Play(Vector3 worldPos, string label, FloatStyle style)
        {
            _worldOrigin = worldPos + Vector3.up * _config.startHeight
                         + new Vector3(
                             Random.Range(-_config.spreadRadius, _config.spreadRadius),
                             0f,
                             Random.Range(-_config.spreadRadius, _config.spreadRadius));

            _text.text = label;
            ApplyStyle(style);

            _elapsed   = 0f;
            _isPlaying = true;
            gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if (!_isPlaying) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _config.lifetime);

            Vector3 worldPos  = _worldOrigin + Vector3.up * (_config.riseCurve.Evaluate(t) * _config.riseHeight);
            Vector3 screenPos = _camera.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0f)
            {
                _text.alpha = 0f;
            }
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvas.GetComponent<RectTransform>(),
                    screenPos, null, out var localPoint);
                _rect.anchoredPosition = localPoint;
                _text.alpha = _config.fadeCurve.Evaluate(t);
            }

            float s = _config.scaleCurve.Evaluate(t);
            _rect.localScale = Vector3.one * (s > 0f ? s : 1f);

            if (t >= 1f)
                ReturnToPool();
        }

        private void ApplyStyle(FloatStyle style)
        {
            switch (style)
            {
                case FloatStyle.Critical:
                    _text.color    = _config.criticalColor;
                    _text.fontSize = _config.criticalFontSize;
                    break;
                case FloatStyle.Heal:
                    _text.color    = _config.healColor;
                    _text.fontSize = _config.healFontSize;
                    break;
                case FloatStyle.Miss:
                    _text.color    = _config.missColor;
                    _text.fontSize = _config.normalFontSize;
                    break;
                default:
                    _text.color    = _config.normalColor;
                    _text.fontSize = _config.normalFontSize;
                    break;
            }
        }

        private void ReturnToPool()
        {
            _isPlaying = false;
            gameObject.SetActive(false);
            _owner.ReturnFloaterToPool(this);
        }
    }

    public enum FloatStyle
    {
        Normal,
        Critical,
        Heal,
        Miss,
    }
}
