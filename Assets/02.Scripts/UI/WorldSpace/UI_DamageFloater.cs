using TMPro;
using UnityEngine;
using UPlayGround.Data.UI;

namespace UPlayGround.UI
{
    public enum FloatStyle
    {
        Normal,       // 일반 공격 데미지 (흰색)
        Critical,     // 강/스킬 공격 데미지 (골드)
        Heal,         // 플레이어 체력 회복 (밝은 그린)
        MonsterHeal,  // 몬스터 체력 회복 (황록색)
        Miss,         // 회피·무적 (회색)
        PlayerDamage, // 플레이어 피격 데미지 (레드)
    }

    /// <summary>
    /// 피격 지점 위로 떠오르는 데미지 숫자 하나.
    ///
    /// 스케일 애니메이션 3단계:
    ///   [0 → scalePopEndT]      0 → scalePopPeak  (EaseOut overshoot)
    ///   [scalePopEndT → scaleShrinkStartT]  scalePopPeak → 1.0  (EaseInOut 안정화)
    ///   [scaleShrinkStartT → 1]  1.0 → scaleShrinkEndValue  (EaseIn 축소)
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UI_DamageFloater : MonoBehaviour
    {
        private TextMeshProUGUI       _text;
        private RectTransform         _rect;
        private Camera                _camera;
        private RectTransform         _canvasRect;
        private DamageFloaterConfigSO _config;
        private UI_WorldSpaceHudLayer _owner;

        private Vector3 _worldOrigin;
        private float   _elapsed;
        private bool    _isPlaying;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            _rect = GetComponent<RectTransform>();
        }

        public void Init(Camera cam, RectTransform canvasRect, DamageFloaterConfigSO config, UI_WorldSpaceHudLayer owner)
        {
            _camera = cam;
            _canvasRect = canvasRect;
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

        /// <summary>씬 전환 후 카메라 레퍼런스를 갱신할 때 사용</summary>
        public void UpdateCamera(Camera cam) => _camera = cam;

        public bool ManagedLateTick(float deltaTime, float unscaledTime)
        {
            if (!_isPlaying) return false;

            // 카메라가 파괴/미초기화 상태면 Camera.main으로 재시도
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return true;

            _elapsed += deltaTime;
            float t = Mathf.Clamp01(_elapsed / _config.lifetime);

            // ── 위치 ──────────────────────────────────────────────────
            Vector3 worldPos  = _worldOrigin + Vector3.up * (_config.riseCurve.Evaluate(t) * _config.riseHeight);
            Vector3 screenPos = _camera.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0f)
            {
                _text.alpha = 0f;
            }
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect,
                    screenPos, null, out var localPoint);
                _rect.anchoredPosition = localPoint;
                _text.alpha = _config.fadeCurve.Evaluate(t);
            }

            // ── 스케일 ────────────────────────────────────────────────
            _rect.localScale = Vector3.one * EvaluateScale(t);

            if (t >= 1f)
                ReturnToPool();

            return _isPlaying;
        }

        /// <summary>
        /// 3단계 스케일 계산.
        ///   팝  : 0 → peak  (EaseOutQuart — 빠르게 튀어오름)
        ///   안정 : peak → 1  (EaseInOutQuad — 부드럽게 안착)
        ///   축소 : 1 → end   (EaseInQuad — 가속 소멸)
        /// </summary>
        private float EvaluateScale(float t)
        {
            float popEnd     = _config.scalePopEndT;
            float shrinkStart = _config.scaleShrinkStartT;
            float peak       = _config.scalePopPeak;
            float endVal     = _config.scaleShrinkEndValue;

            if (t < popEnd)
            {
                // 팝 구간: 0 → peak (EaseOutQuart)
                float tN = t / popEnd;
                float eased = 1f - Mathf.Pow(1f - tN, 4f);
                return Mathf.LerpUnclamped(0f, peak, eased);
            }
            else if (t < shrinkStart)
            {
                // 안정 구간: peak → 1 (EaseInOutQuad)
                float tN = (t - popEnd) / (shrinkStart - popEnd);
                float eased = tN < 0.5f
                    ? 2f * tN * tN
                    : 1f - Mathf.Pow(-2f * tN + 2f, 2f) * 0.5f;
                return Mathf.LerpUnclamped(peak, 1f, eased);
            }
            else
            {
                // 축소 구간: 1 → endVal (EaseInQuad)
                float tN = (t - shrinkStart) / (1f - shrinkStart);
                float eased = tN * tN;
                return Mathf.LerpUnclamped(1f, endVal, eased);
            }
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
                case FloatStyle.MonsterHeal:
                    _text.color    = _config.monsterHealColor;
                    _text.fontSize = _config.monsterHealFontSize;
                    break;
                case FloatStyle.Miss:
                    _text.color    = _config.missColor;
                    _text.fontSize = _config.normalFontSize;
                    break;
                case FloatStyle.PlayerDamage:
                    _text.color    = _config.playerDamageColor;
                    _text.fontSize = _config.playerDamageFontSize;
                    break;
                default: // Normal
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
}
