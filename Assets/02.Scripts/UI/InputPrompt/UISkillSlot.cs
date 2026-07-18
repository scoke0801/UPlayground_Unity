using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Ability.Core;
using UPlayGround.Components;
using UPlayGround.Data.Combat;
using UPlayGround.Contracts.Ability;

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

        [Header("트윈 (DOTween)")]
        [Tooltip("상태 전이(사용 순간 / 쿨타임 시작 / 사용 가능 복귀) 시 펀치 트윈을 재생할지 여부.")]
        [SerializeField] private bool _enableTween = true;
        [Tooltip("펀치 대상. 비우면 이 슬롯 자신의 transform을 사용한다.")]
        [SerializeField] private RectTransform _tweenTarget;
        [Tooltip("스킬 사용 순간 슬롯 펀치 세기(스케일 가산). 0.15=1.0→1.15.")]
        [SerializeField] private float _usePunch = 0.15f;
        [Tooltip("스킬 사용 순간 펀치 지속(초).")]
        [SerializeField] private float _useDuration = 0.25f;
        [Tooltip("사용 가능 복귀 시 슬롯 펀치 세기(사용보다 살짝 크게).")]
        [SerializeField] private float _readyPunch = 0.2f;
        [Tooltip("사용 가능 복귀 펀치 지속(초).")]
        [SerializeField] private float _readyDuration = 0.3f;
        [Tooltip("쿨타임 시작/사용 가능 시 하위 오버레이(_cooldownRoot·_availableRoot) 팝-인 세기.")]
        [SerializeField] private float _overlayPunch = 0.25f;
        [Tooltip("오버레이 팝-인 지속(초).")]
        [SerializeField] private float _overlayDuration = 0.25f;

        public ComboInputToken Token => _token;

        /// <summary>이 슬롯이 게이지를 요구하는지(스킬 슬롯).</summary>
        public bool RequiresGauge { get; private set; }
        /// <summary>게이지 비용 슬롯 인덱스(RequiresGauge일 때 유효).</summary>
        public int GaugeSlot { get; private set; }

        // 게이지/쿨타임 UI가 _availableRoot 하위인지 여부. UI 계층은 런타임에 불변이라 Initialize에서 1회 캐시한다.
        private bool _gaugeUiUnderAvailableRoot;
        private bool _cooldownUiUnderAvailableRoot;

        /// <summary>
        /// 게이지와 무관한 외부 쿨타임 소스(remaining, duration)를 샘플링한다. 예: 대시 쿨타임은
        /// 이동 컨트롤러가 소유한다. null이면 게이지 슬롯(<see cref="GaugeSlot"/>)에서 쿨타임을 읽는다.
        /// </summary>
        public delegate void CooldownSample(out float remaining, out float duration);
        private CooldownSample _cooldownSource;

        /// <summary>외부 쿨타임 소스를 주입/해제한다. HUD가 슬롯 토큰에 맞는 소스를 배선한다.</summary>
        public void SetCooldownSource(CooldownSample source) => _cooldownSource = source;

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
            ResetTweenState();   // 바인드/스왑 시 트윈 베이스라인 재설정(오발화 방지).
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
            bool hasGauge = RequiresGauge && gauge != null;

            // 쿨타임 소스 결정: 외부 소스(대시 등)가 있으면 우선, 없으면 게이지 슬롯에서 읽는다.
            float cooldownRemaining = 0f;
            float cooldownDuration  = 0f;
            if (_cooldownSource != null)
                _cooldownSource(out cooldownRemaining, out cooldownDuration);
            else if (hasGauge)
            {
                cooldownRemaining = gauge.GetSkillCooldownRemaining(GaugeSlot);
                cooldownDuration  = gauge.GetSkillCooldownDuration(GaugeSlot);
            }

            // 표시할 자원도 쿨타임 소스도 없는 슬롯(약/강 등)은 비활성 처리한다.
            if (!hasGauge && _cooldownSource == null)
            {
                SetGaugeState(true);
                ClearGaugeState();
                return false;
            }

            bool hasCooldown  = cooldownRemaining > 0f;
            bool showCooldown = _showCooldownUi && hasCooldown;

            // ── 게이지(자원) 표시: 게이지 슬롯일 때만 유효. 외부 쿨타임 전용 슬롯은 0 처리. ──
            float current = hasGauge ? gauge.CurrentGauge : 0f;
            float max     = hasGauge ? gauge.MaxGauge : 0f;
            bool usesGaugeCost = hasGauge && PlayerSkillGauge.UsesGaugeCost(GaugeSlot);
            bool showGauge = _showGaugeUi && usesGaugeCost;
            float gaugeRatio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            bool isFullGauge = max > 0f && current >= max;
            // 게이지 슬롯은 게이지 충족, 외부 쿨타임 전용 슬롯은 쿨타임이 없을 때 사용 가능.
            bool ready = hasGauge ? gauge.CanUseSkill(GaugeSlot) : !hasCooldown;

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

            // 상태 전이 트윈은 오버레이 SetActive 이후에 평가한다(팝-인 대상이 활성 상태여야 함).
            DriveTweens(ready, hasCooldown);

            return showCooldown;
        }

        /// <summary>
        /// 신규 Ability 런타임의 읽기 전용 상태를 표시한다.
        /// UI는 비용/사용 가능 여부를 재계산하지 않고 전달된 값을 그대로 사용한다.
        /// </summary>
        public bool SetAbilityState(in AbilitySlotViewState state)
        {
            float cooldownRemaining = Mathf.Max(0f, state.CooldownRemaining);
            float cooldownDuration = Mathf.Max(0f, state.CooldownDuration);
            bool hasCooldown = cooldownRemaining > 0f;
            bool showCooldown = _showCooldownUi && hasCooldown;
            bool hasResourceCost = state.ResourceRequired > 0f;
            float resourceRatio = hasResourceCost
                ? Mathf.Clamp01(state.ResourceCurrent / state.ResourceRequired)
                : 1f;

            SetGaugeState(state.IsReady);
            if (_availableRoot != null)
                _availableRoot.SetActive(ShouldShowAvailableRoot(state.IsReady, showCooldown));

            if (_gaugeFill != null)
            {
                bool showGauge = _showGaugeUi && hasResourceCost;
                _gaugeFill.gameObject.SetActive(showGauge);
                _gaugeFill.fillAmount = showGauge ? resourceRatio : 0f;
            }

            if (_gaugeText != null)
            {
                bool showGauge = _showGaugeUi && hasResourceCost;
                _gaugeText.gameObject.SetActive(showGauge);
                _gaugeText.text = showGauge
                    ? $"{Mathf.FloorToInt(state.ResourceCurrent)}/{Mathf.FloorToInt(state.ResourceRequired)}"
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

            DriveTweens(state.IsReady, hasCooldown);
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
                _cooldownFill.gameObject.SetActive(_showCooldownUi && (RequiresGauge || _cooldownSource != null));
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

        // ── 트윈(DOTween) ────────────────────────────────────────────
        // SetGaugeState에서 상태 전이를 엣지 감지해 1회 연출한다:
        //   #1 사용 순간 + #2 쿨타임 시작 : 쿨타임이 새로 걸린 엣지(슬롯 펀치 + 쿨타임 오버레이 팝-인)
        //   #3 사용 가능 복귀            : 사용 불가→가능 엣지(슬롯 펀치 + 사용가능 오버레이 팝-인)
        // 펀치는 DOPunchScale로 현재 스케일을 기준으로 가산 진동 후 원복하므로 스케일 드리프트가 없다.
        // 히트스톱(Time.timeScale 변경)에 얼지 않도록 SetUpdate(true)로 독립 갱신한다.
        private bool _stateInitialized;
        private bool _prevOnCooldown;
        private bool _prevReady;
        private Vector3 _baseScale = Vector3.one;
        private Tween _slotTween;
        private Tween _cooldownTween;
        private Tween _availableTween;

        private Transform ScaleTarget => _tweenTarget != null ? (Transform)_tweenTarget : transform;

        private void Awake()
        {
            // 스왑 중 펀치 진행 도중 Initialize가 다시 호출되어도 베이스 스케일이 어긋나지 않도록 1회만 캐시한다.
            _baseScale = ScaleTarget.localScale;
        }

        private void OnDisable()
        {
            KillTweens();
            ScaleTarget.localScale = _baseScale;
        }

        private void DriveTweens(bool ready, bool onCooldown)
        {
            if (!_enableTween) return;

            // 첫 갱신은 베이스라인만 잡고 연출하지 않는다(바인드/스왑 직후 오발화 방지).
            if (!_stateInitialized)
            {
                _prevReady        = ready;
                _prevOnCooldown   = onCooldown;
                _stateInitialized = true;
                return;
            }

            if (onCooldown && !_prevOnCooldown)
            {
                Punch(ref _slotTween, ScaleTarget, _usePunch, _useDuration);            // #1
                if (_cooldownRoot != null)
                    Punch(ref _cooldownTween, _cooldownRoot.transform, _overlayPunch, _overlayDuration); // #2
            }
            else if (ready && !_prevReady)
            {
                Punch(ref _slotTween, ScaleTarget, _readyPunch, _readyDuration);        // #3
                if (_availableRoot != null)
                    Punch(ref _availableTween, _availableRoot.transform, _overlayPunch, _overlayDuration);
            }

            _prevReady      = ready;
            _prevOnCooldown = onCooldown;
        }

        // 공용 펀치 스케일. 대상이 비활성이면 건너뛰고, 직전 펀치는 원복(complete:true)한 뒤 새로 시작한다.
        private static void Punch(ref Tween tween, Transform target, float strength, float duration)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return;
            tween?.Kill(complete: true);
            tween = target.DOPunchScale(Vector3.one * strength, duration, vibrato: 1, elasticity: 0.5f)
                          .SetUpdate(true);
        }

        private void ResetTweenState()
        {
            KillTweens();
            ScaleTarget.localScale = _baseScale;
            _stateInitialized = false;
        }

        private void KillTweens()
        {
            // complete:true로 종료해 펀치를 시작(클린) 스케일로 스냅한다.
            // 중간 스케일에서 멈추면 다음 DOPunchScale이 드리프트를 기준으로 시작·복귀해
            // 오버레이 루트(_cooldownRoot/_availableRoot) 스케일이 누적 드리프트한다.
            _slotTween?.Kill(complete: true);
            _cooldownTween?.Kill(complete: true);
            _availableTween?.Kill(complete: true);
            _slotTween = _cooldownTween = _availableTween = null;
        }
    }
}
