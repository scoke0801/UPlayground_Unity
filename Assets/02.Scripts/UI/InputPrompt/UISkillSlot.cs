using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Component;
using UPlayGround.Data.Combat;

namespace UPlayGround.UI.InputPrompt
{
    /// <summary>
    /// 고정 스킬바의 슬롯 1개. Ability(Skill1) / Ultimate(Skill2)는 <see cref="ComboInputToken"/>으로 정의되며
    /// 그 토큰이 키캡 글리프·콤보 힌트 매칭·게이지 슬롯을 모두 결정한다.
    ///
    /// 글로우는 모두 <b>콤보 힌트</b>로 켜진다(게이지 충족과 무관):
    /// - <b>ReadyGlow</b> / <b>ComboGlow</b>: 현재 콤보의 '다음 키'가 이 슬롯일 때 켠다(CollectHints).
    /// 게이지(자원) 부족은 글로우가 아니라 <b>dim</b>으로만 표현한다.
    /// </summary>
    public class UISkillSlot : MonoBehaviour
    {
        [Header("정의")]
        [Tooltip("이 슬롯이 대표하는 입력 토큰. Skill1은 Ability, Skill2는 Ultimate로 취급한다.")]
        [SerializeField] private ComboInputToken _token = ComboInputToken.Skill1;

        [Tooltip("스킬 아이콘. ※v1: 프리팹 직렬화라 캐릭터 교체를 따라가지 않음(스왑 미추적).")]
        [SerializeField] private Sprite _icon;

        [Tooltip("게이지 비용 슬롯 오버라이드(-1=토큰에서 자동: Skill1→0, Skill2→1, 그 외 게이지 없음).")]
        [SerializeField] private int _gaugeSlotOverride = -1;

        [Tooltip("키캡 표시용 입력 액션 오버라이드. 비우면 토큰 기본 매핑을 사용한다.")]
        [SerializeField] private string _inputActionOverride;

        [Header("자원 표시 옵션")]
        [Tooltip("이 슬롯에서 Ability/Ultimate 비용 및 쿨타임 판정을 사용할지 여부. 약공격/강공격처럼 자원 UI가 필요 없는 슬롯은 끈다.")]
        [SerializeField] private bool _useGaugeFeature = true;
        [Tooltip("게이지가 최대치일 때만 사용 가능 루트를 켠다.")]
        [SerializeField] private bool _showOnlyWhenGaugeFull = true;
        [Tooltip("슬롯 내부 게이지 UI를 표시할지 여부.")]
        [SerializeField] private bool _showGaugeUi = true;
        [Tooltip("슬롯 내부 쿨타임 UI를 표시할지 여부.")]
        [SerializeField] private bool _showCooldownUi = true;
        [Tooltip("쿨타임 텍스트 표시 형식. 예: 0.0 = 소수 1자리, 0.00 = 소수 2자리")]
        [SerializeField] private string _cooldownTextFormat = "0.0";

        [Header("렌더 타깃")]
        [SerializeField] private Image              _iconImage;
        [SerializeField] private UI_InputPromptIcon _keyIcon;   // 키캡 글리프(디바이스 자동 전환)
        [SerializeField] private GameObject         _readyGlow;  // 콤보 다음 키 발광
        [SerializeField] private GameObject         _comboGlow;  // 콤보 다음 키 강조(추가 연출)
        
        [Tooltip("게이지 부족 시 어둡게(선택). 없으면 무시.")]
        [SerializeField] private CanvasGroup        _dimGroup;
        [SerializeField] private float              _dimAlpha = 0.5f;
        
        [SerializeField] private GameObject         _availableRoot;
        [SerializeField] private Image              _gaugeFill;
        [SerializeField] private TextMeshProUGUI    _gaugeText;
        [SerializeField] private GameObject         _cooldownRoot;
        [SerializeField] private Image              _cooldownFill;
        [SerializeField] private TextMeshProUGUI    _cooldownText;

        public ComboInputToken Token => _token;

        /// <summary>이 슬롯이 게이지를 요구하는지(스킬 슬롯).</summary>
        public bool RequiresGauge { get; private set; }
        /// <summary>게이지 비용 슬롯 인덱스(RequiresGauge일 때 유효).</summary>
        public int GaugeSlot { get; private set; }

        // 게이지/쿨타임 UI가 _availableRoot 하위인지 여부. UI 계층은 런타임에 불변이라 Initialize에서 1회 캐시한다.
        private bool _gaugeUiUnderAvailableRoot;
        private bool _cooldownUiUnderAvailableRoot;

        /// <summary>런타임에서 HUD가 고정 슬롯을 보강할 때 사용한다.</summary>
        public void Configure(
            ComboInputToken token,
            Sprite icon,
            string inputActionOverride,
            bool useGaugeFeature,
            bool showGaugeUi,
            bool showCooldownUi)
        {
            _token = token;
            _icon = icon;
            _inputActionOverride = inputActionOverride;
            _useGaugeFeature = useGaugeFeature;
            _showGaugeUi = showGaugeUi;
            _showCooldownUi = showCooldownUi;
        }

        /// <summary>아이콘/키캡/게이지 슬롯을 1회 설정한다(바인드 시 호출).</summary>
        public void Initialize()
        {
            if (_iconImage != null)
            {
                _iconImage.sprite  = _icon;
                _iconImage.enabled = _icon != null;
            }

            if (_keyIcon != null && TryResolveInputAction(out string map, out string action))
                _keyIcon.SetAction(map, action);

            ResolveGaugeSlot();
            CacheAvailableRootMembership();
            SetComboHint(false);
            ClearGaugeState();
        }

        private bool TryResolveInputAction(out string map, out string action)
        {
            if (!string.IsNullOrEmpty(_inputActionOverride)
                && ComboTokenInput.TryGetAction(_token, out map, out _, out _))
            {
                action = _inputActionOverride;
                return true;
            }

            return ComboTokenInput.TryGetAction(_token, out map, out action, out _);
        }

        /// <summary>게이지/쿨타임 UI가 _availableRoot 하위인지 1회 계산해 캐시한다(계층은 런타임 불변).</summary>
        private void CacheAvailableRootMembership()
        {
            _gaugeUiUnderAvailableRoot =
                IsChildOfAvailableRoot(_gaugeFill) || IsChildOfAvailableRoot(_gaugeText);

            _cooldownUiUnderAvailableRoot =
                IsChildOfAvailableRoot(_cooldownRoot)
                || IsChildOfAvailableRoot(_cooldownFill)
                || IsChildOfAvailableRoot(_cooldownText);
        }

        private void ResolveGaugeSlot()
        {
            if (!_useGaugeFeature)
            {
                RequiresGauge = false;
                GaugeSlot     = -1;
                return;
            }

            if (_gaugeSlotOverride >= 0)
            {
                RequiresGauge = true;
                GaugeSlot     = _gaugeSlotOverride;
                return;
            }

            switch (_token)
            {
                case ComboInputToken.Skill1: RequiresGauge = true; GaugeSlot = 0; break;
                case ComboInputToken.Skill2: RequiresGauge = true; GaugeSlot = 1; break;
                default:                     RequiresGauge = false; GaugeSlot = -1; break;
            }
        }

        /// <summary>
        /// 게이지 상태 갱신. ready=지금 사용 가능(게이지 충족). 게이지는 글로우가 아니라 <b>dim</b>으로만
        /// 표현한다(부족 시 어둡게). 공유 게이지의 연속 fill은 파티 패널이 담당.
        /// 게이지 비요구 슬롯(약/강 등)은 항상 ready 취급.
        /// </summary>
        public void SetGaugeState(bool ready)
        {
            if (_dimGroup != null)
                _dimGroup.alpha = (RequiresGauge && !ready) ? _dimAlpha : 1f;
        }

        /// <summary>
        /// 이 슬롯에 연결된 자원/쿨타임 UI를 갱신한다. Ability는 쿨타임만, Ultimate는 게이지와 쿨타임을 표시한다.
        /// 반환값은 프레임 갱신이 필요한 쿨타임 표시 여부.
        /// </summary>
        public bool SetGaugeState(PlayerSkillGauge gauge)
        {
            if (!RequiresGauge || gauge == null)
            {
                SetGaugeState(true);
                ClearGaugeState();
                return false;
            }

            float current = gauge.CurrentGauge;
            float max = gauge.MaxGauge;
            bool ready = gauge.CanUseSkill(GaugeSlot);
            bool usesGaugeCost = PlayerSkillGauge.UsesGaugeCost(GaugeSlot);
            bool showGauge = _showGaugeUi && usesGaugeCost;
            float gaugeRatio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            bool isFullGauge = max > 0f && current >= max;
            float cooldownRemaining = gauge.GetSkillCooldownRemaining(GaugeSlot);
            float cooldownDuration = gauge.GetSkillCooldownDuration(GaugeSlot);
            bool hasCooldown = cooldownRemaining > 0f;
            bool showCooldown = _showCooldownUi && hasCooldown;

            SetGaugeState(ready);

            bool canShowAvailable = ready && (!usesGaugeCost || !_showOnlyWhenGaugeFull || isFullGauge);
            if (_availableRoot != null)
                _availableRoot.SetActive(ShouldShowAvailableRoot(canShowAvailable, showCooldown));

            if (_gaugeFill != null)
            {
                _gaugeFill.gameObject.SetActive(showGauge);
                _gaugeFill.fillAmount = showGauge ? gaugeRatio : 0f;
            }

            if (_gaugeText != null)
            {
                _gaugeText.gameObject.SetActive(showGauge);
                _gaugeText.text = showGauge && max > 0f
                    ? $"{Mathf.FloorToInt(current)}/{Mathf.FloorToInt(max)}"
                    : string.Empty;
            }

            if (_cooldownRoot != null)
                _cooldownRoot.SetActive(showCooldown);

            if (_cooldownFill != null)
            {
                _cooldownFill.gameObject.SetActive(_showCooldownUi);
                _cooldownFill.fillAmount = showCooldown && cooldownDuration > 0f
                    ? Mathf.Clamp01(cooldownRemaining / cooldownDuration)
                    : 0f;
            }

            if (_cooldownText != null)
            {
                _cooldownText.gameObject.SetActive(showCooldown);
                _cooldownText.text = showCooldown
                    ? cooldownRemaining.ToString(_cooldownTextFormat)
                    : string.Empty;
            }

            return showCooldown;
        }

        private bool ShouldShowAvailableRoot(bool canShowAvailable, bool showCooldown)
        {
            if (canShowAvailable)
                return true;

            bool containsGaugeUi = _showGaugeUi
                                   && PlayerSkillGauge.UsesGaugeCost(GaugeSlot)
                                   && _gaugeUiUnderAvailableRoot;
            bool containsCooldownUi = showCooldown && _cooldownUiUnderAvailableRoot;

            return containsGaugeUi || containsCooldownUi;
        }

        private bool IsChildOfAvailableRoot(UnityEngine.Component component)
        {
            return component != null
                   && _availableRoot != null
                   && component.transform.IsChildOf(_availableRoot.transform);
        }

        private bool IsChildOfAvailableRoot(GameObject target)
        {
            return target != null
                   && _availableRoot != null
                   && target.transform.IsChildOf(_availableRoot.transform);
        }

        private void ClearGaugeState()
        {
            bool showGauge = _showGaugeUi
                             && RequiresGauge
                             && PlayerSkillGauge.UsesGaugeCost(GaugeSlot);

            if (_availableRoot != null)
                _availableRoot.SetActive(false);

            if (_gaugeFill != null)
            {
                _gaugeFill.gameObject.SetActive(showGauge);
                _gaugeFill.fillAmount = 0f;
            }

            if (_gaugeText != null)
            {
                _gaugeText.gameObject.SetActive(showGauge);
                _gaugeText.text = string.Empty;
            }

            if (_cooldownRoot != null)
                _cooldownRoot.SetActive(false);

            if (_cooldownFill != null)
            {
                _cooldownFill.gameObject.SetActive(_showCooldownUi && RequiresGauge);
                _cooldownFill.fillAmount = 0f;
            }

            if (_cooldownText != null)
            {
                _cooldownText.gameObject.SetActive(false);
                _cooldownText.text = string.Empty;
            }
        }

        /// <summary>콤보 '다음 키' 강조 토글. ReadyGlow/ComboGlow 모두 콤보 힌트로만 켠다.</summary>
        public void SetComboHint(bool active)
        {
            if (_readyGlow != null)
                _readyGlow.SetActive(active);

            if (_comboGlow != null)
                _comboGlow.SetActive(active);
        }
    }
}
