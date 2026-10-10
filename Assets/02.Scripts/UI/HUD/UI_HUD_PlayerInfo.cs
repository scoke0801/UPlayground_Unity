using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Contracts.Ability;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Config;

namespace UPlayGround.UI
{
    public class UI_HUD_PlayerInfo : UI_Base
    {
        [SerializeField] private Image _boardHpFill;
        [SerializeField] private Image _boardHpWhiteFill;

        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _elementText;
        [SerializeField] private Image _elementIcon;
        [SerializeField] private Image _elementFrame;
        [SerializeField] private ElementIconEntry[] _elementIcons;
        private bool _hasCharacterIdentity;
        private CharacterActorType _displayedCharacterType;
        private CombatElement _displayedElement;
        private int _displayedLevel;

        [System.Serializable]
        private struct ElementIconEntry
        {
            public CombatElement element;
            public Sprite sprite;
        }


        [Header("캐릭터 문양")]
        [SerializeField] private UICharacterGauge _characterGauge;
        [SerializeField, Min(0.01f)] private float _abilityStateRefreshInterval = 0.05f;
        private IAbilityRuntimeReader _abilityReader;
        private SettingsData _settings;
        private float _nextAbilityRefresh;

        [SerializeField] private Vector3 _staminaWorldOffset = new(0f, 1f, 0f);
        [SerializeField] private Vector2 _staminaScreenOffset = new(80f, 0f);
        private UnityEngine.Camera _worldCamera;
        private RectTransform _staminaParent;
        private Canvas _staminaCanvas;
        private Vector2 _staminaFollowVelocity;
        private bool _hasStaminaPosition;
        private int _staminaRenderedFrame = -1;
        private float _lastStaminaChangeTime = float.NegativeInfinity;
        private bool _shouldShowStamina;
        private Tween _staminaVisibilityTween;
        private Material _staminaWaterMaterial;
        private Material _staminaWaterSource;
        private float _staminaDisplayedRatio;
        private Color _staminaTint = Color.white;
        private static readonly int StaminaResourceId = Shader.PropertyToID("_Resource");
        private static readonly int StaminaTintId = Shader.PropertyToID("_EnergyTint");

        [Header("스태미나 추적과 표시")]
        [SerializeField] private CanvasGroup _staminaGroup;
        [SerializeField, Min(0f)] private float _staminaFollowSmoothTime = 0.06f;
        [SerializeField, Min(0f)] private float _staminaFollowDeadZone = 1.5f;
        [SerializeField, Min(1f)] private float _staminaFollowSnapDistance = 240f;
        [Tooltip("물리 틱 사이의 표시 깜빡임을 막는 짧은 유예. 회복 대기 시간과는 무관합니다.")]
        [SerializeField, Min(0f)] private float _staminaActivityGraceTime = 0.1f;
        [Tooltip("숨김 조건이 유지되어야 하는 시간(초). 소비나 회복이 다시 발생하면 대기를 처음부터 계산합니다.")]
        [SerializeField, Min(0f)] private float _staminaHideDelay = 1f;
        [SerializeField, Min(0f)] private float _staminaFadeInDuration = 0.12f;
        [SerializeField, Min(0f)] private float _staminaFadeOutDuration = 0.08f;

        [Header("Stamina")]
        [SerializeField] private RectTransform _staminaPanel;
        [SerializeField] private Image _staminaFill;
        [SerializeField] private TextMeshProUGUI _staminaText;
        [SerializeField] private Color _staminaNormalColor =
            Color.white;
        [SerializeField] private Color _staminaLowColor =
            new(1f, 0.55f, 0.38f, 1f);
        [SerializeField, Range(0f, 1f)] private float _staminaLowRatio = 0.2f;
        [SerializeField, Min(0f)] private float _staminaSpendPulseThreshold = 5f;
        [SerializeField, Min(1f)] private float _staminaSpendPulseScale = 1.04f;
        [SerializeField, Min(0f)] private float _staminaSpendPulseDuration = 0.12f;

        [Header("Buff / Debuff")]
        [SerializeField] private RectTransform _effectArea;
        [SerializeField] private RectTransform _effectIconRoot;
        [SerializeField] private UIGameplayEffectIcon _effectIconTemplate;
        [SerializeField] private TextMeshProUGUI _effectOverflowText;
        [SerializeField] private Sprite _effectFallbackIcon;
        [SerializeField, Min(1)] private int _maxVisibleEffects = 10;

        [Header("Animation Settings")]
        [SerializeField] private float _hpDecreaseDelayTime = 0.3f;
        [SerializeField] private float _hpFillSpeed         = 5.0f;
        [SerializeField] private float _staminaFillSpeed    = 10.0f;

        private Coroutine _hpFillCoroutine;
        private static readonly int WaterClockId = Shader.PropertyToID("_UnscaledTime");
        private static readonly int WaterSpriteRectId = Shader.PropertyToID("_SpriteRect");
        private Material _healthWaterMaterial;
        private Material _healthWaterSource;
        private PlayerActor _playerActor;
        private IGameplayEffectRuntimeReader _effectReader;
        private readonly List<GameplayEffectViewState> _effectViews = new();
        private readonly List<GameplayEffectViewState> _selectedEffectViews = new();
        private readonly List<UIGameplayEffectIcon> _effectIcons = new();
        private int _activeEffectIconCount;
        private float _staminaTargetRatio = 1f;
        private float _lastStamina;
        private int _displayedStamina = int.MinValue;
        private int _displayedMaximumStamina = int.MinValue;
        private bool _hasStaminaSnapshot;
        private bool _hasStaminaDeficit;
        private bool _isStaminaLow;
        private bool _hasStaminaPanelBaseScale;
        private Vector3 _staminaPanelBaseScale = Vector3.one;
        private Tween _staminaSpendTween;
        private Tween _staminaColorTween;

        private bool _isInCombat = false;
        private bool _hasReducedMotion;

        #region UI_Base
        protected override void OnShow()
        {
            ClearCharacterIdentity();
            ResetStaminaPresentation();
            InitializeHealthWater();
            InitializeStaminaWater();
            _boardHpFill.fillAmount      = 1.0f;
            _boardHpWhiteFill.fillAmount = 1.0f;

            if (UISvc.Actors == null) return;

            _playerActor = UISvc.Actors.Player;
            if (_playerActor == null) return;

            _playerActor.EnsureCharacterRuntimeInitialized();
            _settings = UISvc.Settings?.Data;
            _worldCamera = Svc.Camera?.GetMainCamera();
            _staminaParent = _staminaPanel != null ? _staminaPanel.parent as RectTransform : null;
            _staminaCanvas = _staminaPanel != null ? _staminaPanel.GetComponentInParent<Canvas>() : null;
            if (_staminaCanvas != null) _staminaCanvas = _staminaCanvas.rootCanvas;
            // KCC 보간과 카메라 LateUpdate가 끝난 뒤 같은 프레임의 화면 좌표를 사용합니다.
            Canvas.willRenderCanvases -= RenderStamina;
            Canvas.willRenderCanvases += RenderStamina;
            BindCharacterGauge();

            _playerActor.OnHpChanged         += SetHp;
            _playerActor.OnSkillGaugeChanged += SetSkillGauge;
            _playerActor.OnStaminaChanged    += SetStamina;

            SetHp(_playerActor.CurrentHealth, _playerActor.MaxHealth);

            float gauge    = _playerActor.SkillGauge?.CurrentGauge ?? 0f;
            float maxGauge = _playerActor.SkillGauge?.MaxGauge     ?? 0f;
            SetSkillGaugeImmediate(gauge, maxGauge);
            SetStaminaImmediate(
                _playerActor.Stamina?.Current ?? 0f,
                _playerActor.Stamina?.Maximum ?? 0f);

            var partyManager = UISvc.Party;
            if (partyManager != null)
            {
                partyManager.OnSwapCompleted += OnPlayerSwapCompleted;
                partyManager.OnPartyProgressionChanged += OnCharacterProgressionChanged;
            }

            RefreshCharacterIdentity();
            BindEffectReader(_playerActor.Effects);
        }

        protected override void OnHide()
        {
            Canvas.willRenderCanvases -= RenderStamina;
            ResetStaminaPresentation();
            KillStaminaTweens();
            StopAllCoroutines();
            _hpFillCoroutine = null;
            UnbindCharacterGauge();
            _hasStaminaSnapshot = false;

            if (_playerActor != null)
            {
                _playerActor.OnHpChanged         -= SetHp;
                _playerActor.OnSkillGaugeChanged -= SetSkillGauge;
                _playerActor.OnStaminaChanged    -= SetStamina;
            }

            var partyManager = UISvc.Party;
            if (partyManager != null)
            {
                partyManager.OnSwapCompleted -= OnPlayerSwapCompleted;
                partyManager.OnPartyProgressionChanged -= OnCharacterProgressionChanged;
            }

            UnbindEffectReader();
            ReleaseAllEffectIcons();
        }

        protected override void OnClose() { }
        #endregion

        protected override void Update()
        {
            base.Update();
            if (!IsVisible) return;
            bool reduceMotion = _settings != null && _settings.reduceHudMotion;
            if (_hasReducedMotion != reduceMotion)
            {
                _hasReducedMotion = reduceMotion;
                if (reduceMotion)
                {
                    KillStaminaTweens();
                    SetStaminaColorImmediate();
                    if (_hpFillCoroutine != null) StopCoroutine(_hpFillCoroutine);
                    _hpFillCoroutine = null;
                    _boardHpWhiteFill.fillAmount = _boardHpFill.fillAmount;
                }
            }
            if (Time.unscaledTime >= _nextAbilityRefresh)
            {
                RefreshCharacterIdentity();
                RefreshGaugeAvailability();
                _nextAbilityRefresh = Time.unscaledTime + _abilityStateRefreshInterval;
            }
            _characterGauge?.Tick(Time.unscaledTime, _settings != null && _settings.reduceHudMotion);
            TickHealthWater(reduceMotion);
            UpdateStaminaFill();
            TickStaminaWater(reduceMotion);
            RefreshEffectTimers();
        }

        /// <summary>현재 체력을 즉시 표시하고 손실분만 지연하여 줄인다.</summary>
        public void SetHp(float hp, float maxHp)
        {
            float ratio = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;
            _boardHpFill.fillAmount = ratio;

            if (_hpFillCoroutine != null) StopCoroutine(_hpFillCoroutine);
            if (ratio >= _boardHpWhiteFill.fillAmount || (_settings != null && _settings.reduceHudMotion))
                _boardHpWhiteFill.fillAmount = ratio;
            else
                _hpFillCoroutine = StartCoroutine(HpDelayFillCoroutine());

            _hpText.text = $"{(int)hp}/{(int)maxHp}";
        }

        private void InitializeHealthWater()
        {
            if (_healthWaterMaterial != null || _boardHpFill == null) return;
            _healthWaterSource = _boardHpFill.material;
            if (_healthWaterSource == null || !_healthWaterSource.HasProperty(WaterClockId)) return;
            _healthWaterMaterial = new Material(_healthWaterSource);
            _boardHpFill.material = _healthWaterMaterial;
            Sprite sprite = _boardHpFill.overrideSprite;
            if (sprite != null)
            {
                Vector4 uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
                _healthWaterMaterial.SetVector(WaterSpriteRectId,
                    new Vector4(uv.x, uv.y, uv.z - uv.x, uv.w - uv.y));
            }
        }

        private void TickHealthWater(bool reduceMotion)
        {
            if (_healthWaterMaterial == null) return;
            float clock = reduceMotion ? 0f : Time.unscaledTime;
            _healthWaterMaterial.SetFloat(WaterClockId, clock);
            // Mask가 파생시킨 재질에도 시간을 전달해 클리핑 중 흐름이 멈추지 않게 합니다.
            Material rendered = _boardHpFill.materialForRendering;
            if (rendered != _healthWaterMaterial && rendered != null)
                rendered.SetFloat(WaterClockId, clock);
        }

        protected override void OnDispose()
        {
            Canvas.willRenderCanvases -= RenderStamina;
            ResetStaminaPresentation();
            KillStaminaTweens();
            if (_staminaFill != null && _staminaWaterMaterial != null)
                _staminaFill.material = _staminaWaterSource;
            if (_staminaWaterMaterial != null) Destroy(_staminaWaterMaterial);
            _staminaWaterMaterial = null;
            if (_boardHpFill != null && _healthWaterMaterial != null)
                _boardHpFill.material = _healthWaterSource;
            if (_healthWaterMaterial != null) Destroy(_healthWaterMaterial);
            _healthWaterMaterial = null;
            base.OnDispose();
        }

        /// <summary>현재 자원을 문양에 전달합니다.</summary>
        public void SetSkillGauge(float gauge, float maxGauge)
        {
            _characterGauge?.SetResource(gauge, maxGauge);
            RefreshGaugeAvailability();
        }

        private void SetSkillGaugeImmediate(float gauge, float maxGauge)
        {
            _characterGauge?.SetResource(gauge, maxGauge, immediate: true);
            RefreshGaugeAvailability();
        }

        private void BindCharacterGauge()
        {
            UnbindCharacterGauge();
            if (_playerActor == null) return;
            var profile = UISvc.Party?.GetCharacterDefinition(_playerActor.CharacterType)?.gaugeVisualProfile;
            _characterGauge?.Bind(profile, _settings != null && _settings.reduceHudMotion);
            _abilityReader = _playerActor.Abilities;
            if (_abilityReader != null) _abilityReader.StateChanged += RefreshGaugeAvailability;
        }

        private void UnbindCharacterGauge()
        {
            if (_abilityReader != null) _abilityReader.StateChanged -= RefreshGaugeAvailability;
            _abilityReader = null;
            _characterGauge?.Clear();
        }

        private void RefreshGaugeAvailability()
        {
            if (_characterGauge == null) return;
            // 최초 HUD 표시 뒤에 Addressables 캐릭터 정의가 준비될 수 있다.
            var profile = _playerActor != null
                ? UISvc.Party?.GetCharacterDefinition(_playerActor.CharacterType)?.gaugeVisualProfile
                : null;
            if (_characterGauge.TryBindProfile(profile, _settings != null && _settings.reduceHudMotion))
            {
                _characterGauge.SetResource(
                    _playerActor?.SkillGauge?.CurrentGauge ?? 0f,
                    _playerActor?.SkillGauge?.MaxGauge ?? 0f,
                    immediate: true);
            }
            UPlayGround.Ability.Core.AbilitySlotViewState state = default;
            bool hasState = _abilityReader != null
                && _abilityReader.TryGetPlayerSlotState(PlayerSkillSlot.Ultimate, out state);
            _characterGauge.SetAbilityState(hasState, state);
        }

        /// <summary>스태미나 목표값과 정수 표시를 갱신한다.</summary>
        public void SetStamina(float stamina, float maximum)
        {
            _hasStaminaDeficit = maximum > 0f && stamina < maximum;
            _staminaTargetRatio = maximum > 0f
                ? Mathf.Clamp01(stamina / maximum)
                : 0f;
            bool isLow = maximum > 0f
                && _staminaTargetRatio <= _staminaLowRatio;
            if (!_hasStaminaSnapshot)
            {
                _hasStaminaSnapshot = true;
                _lastStamina = stamina;
                _isStaminaLow = isLow;
                SetStaminaColorImmediate();
            }
            else
            {
                if (!Mathf.Approximately(_lastStamina, stamina))
                    _lastStaminaChangeTime = Time.unscaledTime;
                if (_lastStamina - stamina >= _staminaSpendPulseThreshold)
                    PlayStaminaSpendFeedback();
                _lastStamina = stamina;
                if (_isStaminaLow != isLow)
                {
                    _isStaminaLow = isLow;
                    TweenStaminaColor();
                }
            }
            UpdateStaminaText(stamina, maximum);
        }

        private void SetStaminaImmediate(float stamina, float maximum)
        {
            ResetStaminaPresentation();
            KillStaminaTweens();
            _hasStaminaSnapshot = false;
            SetStamina(stamina, maximum);
            _staminaDisplayedRatio = _staminaTargetRatio;
            TickStaminaWater(_settings != null && _settings.reduceHudMotion);
        }

        private void InitializeStaminaWater()
        {
            if (_staminaWaterMaterial != null || _staminaFill == null) return;
            _staminaWaterSource = _staminaFill.material;
            if (_staminaWaterSource == null || !_staminaWaterSource.HasProperty(WaterClockId)) return;
            _staminaWaterMaterial = new Material(_staminaWaterSource);
            _staminaFill.material = _staminaWaterMaterial;
            ApplyStaminaMaterial(_staminaWaterMaterial, 0f);
            Sprite sprite = _staminaFill.overrideSprite;
            if (sprite == null) return;
            Vector4 uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            _staminaWaterMaterial.SetVector(WaterSpriteRectId,
                new Vector4(uv.x, uv.y, uv.z - uv.x, uv.w - uv.y));
        }

        private void TickStaminaWater(bool reduceMotion)
        {
            if (_staminaWaterMaterial == null) return;
            float clock = reduceMotion ? 0f : Time.unscaledTime;
            ApplyStaminaMaterial(_staminaWaterMaterial, clock);
            Material rendered = _staminaFill.materialForRendering;
            if (rendered != null && rendered != _staminaWaterMaterial)
                ApplyStaminaMaterial(rendered, clock);
        }

        private void ApplyStaminaMaterial(Material material, float clock)
        {
            material.SetFloat(WaterClockId, clock);
            material.SetFloat(StaminaResourceId, _staminaDisplayedRatio);
            material.SetColor(StaminaTintId, _staminaTint);
        }

        private void RenderStamina()
        {
            if (!IsVisible || !isActiveAndEnabled || _staminaPanel == null
                || _staminaRenderedFrame == Time.frameCount) return;
            _staminaRenderedFrame = Time.frameCount;
            if (_worldCamera == null || _playerActor == null || _staminaParent == null)
            {
                SetStaminaVisibility(false, immediate: true);
                _staminaPanel.gameObject.SetActive(false);
                _hasStaminaPosition = false;
                return;
            }

            Vector3 screen = _worldCamera.WorldToScreenPoint(
                _playerActor.transform.position + _staminaWorldOffset);
            UnityEngine.Camera canvasCamera = _staminaCanvas != null
                && _staminaCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _staminaCanvas.worldCamera : null;
            if (screen.z <= 0f || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _staminaParent, screen, canvasCamera, out Vector2 local))
            {
                SetStaminaVisibility(false, immediate: true);
                _staminaPanel.gameObject.SetActive(false);
                _hasStaminaPosition = false;
                return;
            }

            RefreshStaminaVisibility(Time.unscaledTime);
            bool isShowing = _shouldShowStamina || (_staminaGroup != null && _staminaGroup.alpha > 0f);
            _staminaPanel.gameObject.SetActive(isShowing);
            if (!isShowing)
            {
                _hasStaminaPosition = false;
                return;
            }
            FollowStaminaPosition(local + _staminaScreenOffset);
        }

        private void FollowStaminaPosition(Vector2 target)
        {
            Vector2 current = _staminaPanel.localPosition;
            float distanceSquared = (target - current).sqrMagnitude;
            if (!_hasStaminaPosition || distanceSquared >= _staminaFollowSnapDistance * _staminaFollowSnapDistance)
            {
                current = target;
                _staminaFollowVelocity = Vector2.zero;
                _hasStaminaPosition = true;
            }
            else if (distanceSquared > _staminaFollowDeadZone * _staminaFollowDeadZone)
            {
                current = Vector2.SmoothDamp(current, target, ref _staminaFollowVelocity,
                    _staminaFollowSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            }
            else
            {
                _staminaFollowVelocity = Vector2.zero;
            }
            _staminaPanel.localPosition = new Vector3(current.x, current.y, _staminaPanel.localPosition.z);
        }

        private void RefreshStaminaVisibility(float currentTime)
        {
            float idleTime = currentTime - _lastStaminaChangeTime;
            bool hasActivity = _hasStaminaSnapshot
                && idleTime <= _staminaActivityGraceTime;
            // 회복 대기와 히트스톱은 값 변경 알림이 멈춰도 소비·회복 과정의 일부다.
            // 초기 표시와 캐릭터 교체는 실제 값 변화가 있을 때까지 숨김을 유지한다.
            bool isWaitingForRecovery = _hasStaminaSnapshot && _hasStaminaDeficit
                && !float.IsNegativeInfinity(_lastStaminaChangeTime);
            // 숨김 조건의 시작은 마지막 값 변화 시각으로 계산해 렌더 호출 간격에 영향받지 않는다.
            bool isWaitingToHide = _hasStaminaSnapshot && _shouldShowStamina
                && idleTime < _staminaActivityGraceTime + Mathf.Max(0f, _staminaHideDelay);
            SetStaminaVisibility(hasActivity || isWaitingForRecovery || isWaitingToHide);
        }

        private void SetStaminaVisibility(bool visible, bool immediate = false)
        {
            bool reduceMotion = _settings != null && _settings.reduceHudMotion;
            if (_shouldShowStamina == visible && !immediate
                && !(reduceMotion && _staminaVisibilityTween != null)) return;
            _shouldShowStamina = visible;
            _staminaVisibilityTween?.Kill();
            _staminaVisibilityTween = null;
            if (_staminaGroup == null) return;
            float alpha = visible ? 1f : 0f;
            float duration = visible ? _staminaFadeInDuration : _staminaFadeOutDuration;
            if (immediate || reduceMotion || duration <= 0f)
            {
                _staminaGroup.alpha = alpha;
                return;
            }
            _staminaVisibilityTween = DOTween.To(
                    () => _staminaGroup.alpha, value => _staminaGroup.alpha = value, alpha, duration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(() => _staminaVisibilityTween = null);
        }

        private void ResetStaminaPresentation()
        {
            SetStaminaVisibility(false, immediate: true);
            _lastStaminaChangeTime = float.NegativeInfinity;
            _hasStaminaPosition = false;
            _staminaFollowVelocity = Vector2.zero;
            _staminaRenderedFrame = -1;
            if (_staminaPanel != null) _staminaPanel.gameObject.SetActive(false);
        }

        private void UpdateStaminaFill()
        {
            _staminaDisplayedRatio = _settings != null && _settings.reduceHudMotion
                ? _staminaTargetRatio
                : Mathf.MoveTowards(_staminaDisplayedRatio, _staminaTargetRatio,
                    Time.unscaledDeltaTime * _staminaFillSpeed);
        }

        private void UpdateStaminaText(float stamina, float maximum)
        {
            if (_staminaText == null) return;
            int currentValue = Mathf.CeilToInt(stamina);
            int maximumValue = Mathf.CeilToInt(maximum);
            if (currentValue == _displayedStamina
                && maximumValue == _displayedMaximumStamina)
                return;

            _displayedStamina = currentValue;
            _displayedMaximumStamina = maximumValue;
            _staminaText.SetText("{0}/{1}", currentValue, maximumValue);
        }

        private void PlayStaminaSpendFeedback()
        {
            if (_staminaPanel == null || !isActiveAndEnabled
                || (_settings != null && _settings.reduceHudMotion)) return;
            EnsureStaminaPanelBaseScale();
            _staminaSpendTween?.Kill();
            _staminaPanel.localScale = _staminaPanelBaseScale;
            _staminaSpendTween = DOTween.To(
                    () => _staminaPanel.localScale,
                    value => _staminaPanel.localScale = value,
                    _staminaPanelBaseScale * _staminaSpendPulseScale,
                    _staminaSpendPulseDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .SetLoops(2, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void TweenStaminaColor()
        {
            if (_staminaFill == null) return;
            _staminaColorTween?.Kill();
            if (_settings != null && _settings.reduceHudMotion)
            {
                SetStaminaColorImmediate();
                return;
            }
            _staminaColorTween = DOTween.To(
                    () => _staminaTint,
                    value => _staminaTint = value,
                    GetStaminaColor(),
                    0.12f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        private void SetStaminaColorImmediate()
        {
            if (_staminaFill != null)
                _staminaTint = GetStaminaColor();
        }

        private Color GetStaminaColor() =>
            _isStaminaLow ? _staminaLowColor : _staminaNormalColor;

        private void EnsureStaminaPanelBaseScale()
        {
            if (_hasStaminaPanelBaseScale || _staminaPanel == null) return;
            _staminaPanelBaseScale = _staminaPanel.localScale;
            _hasStaminaPanelBaseScale = true;
        }

        private void KillStaminaTweens()
        {
            _staminaSpendTween?.Kill();
            _staminaColorTween?.Kill();
            _staminaSpendTween = null;
            _staminaColorTween = null;
            if (_staminaPanel == null) return;
            EnsureStaminaPanelBaseScale();
            _staminaPanel.localScale = _staminaPanelBaseScale;
        }

        public void SetIsInCombat(bool isInCombat)
        {
            _isInCombat = isInCombat;
        }

        private IEnumerator HpDelayFillCoroutine()
        {
            yield return new WaitForSecondsRealtime(_hpDecreaseDelayTime);

            while (_boardHpWhiteFill.fillAmount > _boardHpFill.fillAmount + 0.001f)
            {
                _boardHpWhiteFill.fillAmount = Mathf.Lerp(
                    _boardHpWhiteFill.fillAmount,
                    _boardHpFill.fillAmount,
                    Time.unscaledDeltaTime * _hpFillSpeed);
                yield return null;
            }

            _boardHpWhiteFill.fillAmount = _boardHpFill.fillAmount;
        }
        private void OnPlayerSwapCompleted(PlayerActor player)
        {
            // 스왑 시 PlayerActor 인스턴스는 유지되고 스탯/게이지만 교체되므로
            // 구독은 그대로 유효하다. 교체된 캐릭터 값으로 즉시 스냅만 한다.
            if (player == null) return;

            BindCharacterGauge();
            RefreshCharacterIdentity();
            SetHp(player.CurrentHealth, player.MaxHealth);

            float gauge    = player.SkillGauge?.CurrentGauge ?? 0f;
            float maxGauge = player.SkillGauge?.MaxGauge     ?? 100f;
            SetSkillGaugeImmediate(gauge, maxGauge);
            SetStaminaImmediate(
                player.Stamina?.Current ?? 0f,
                player.Stamina?.Maximum ?? 0f);
            BindEffectReader(player.Effects);
            RefreshEffects();
        }

        private void OnCharacterProgressionChanged(CharacterActorType type)
        {
            if (_playerActor != null && _playerActor.CharacterType == type)
                RefreshCharacterIdentity();
        }

        private void RefreshCharacterIdentity()
        {
            var party = UISvc.Party;
            if (_playerActor == null || party?.PartyMemberDataSO == null
                || _playerActor.CharacterType == CharacterActorType.None)
            {
                ClearCharacterIdentity();
                return;
            }
            CharacterActorType type = _playerActor.CharacterType;
            int level = party.GetLevel(type);
            CombatElement element = party.PartyMemberDataSO.GetCombatElement(type);
            // 최초 모델 적용과 데이터 로딩은 HUD 표시보다 늦을 수 있어 이벤트 외에도 동기화한다.
            if (_hasCharacterIdentity && _displayedCharacterType == type
                && _displayedElement == element && _displayedLevel == level) return;
            _hasCharacterIdentity = true;
            _displayedCharacterType = type;
            _displayedElement = element;
            _displayedLevel = level;
            if (_levelText != null)
            {
                _levelText.gameObject.SetActive(true);
                _levelText.SetText("LV {0}", level);
            }
            Sprite icon = FindElementIcon(element);
            if (_elementIcon != null)
            {
                _elementIcon.sprite = icon;
                _elementIcon.color = UICombatElementDisplay.Color(element);
                _elementIcon.gameObject.SetActive(icon != null);
            }
            if (_elementFrame != null)
            {
                _elementFrame.gameObject.SetActive(true);
                _elementFrame.color = UICombatElementDisplay.Color(element);
            }
            if (_elementText != null)
            {
                _elementText.text = UICombatElementDisplay.Label(element);
                _elementText.color = UICombatElementDisplay.Color(element);
                _elementText.gameObject.SetActive(_elementIcon == null || icon == null);
            }
        }

        private void ClearCharacterIdentity()
        {
            _hasCharacterIdentity = false;
            if (_levelText != null) _levelText.gameObject.SetActive(false);
            if (_elementIcon != null) _elementIcon.gameObject.SetActive(false);
            if (_elementFrame != null) _elementFrame.gameObject.SetActive(false);
            if (_elementText != null) _elementText.gameObject.SetActive(false);
        }

        private Sprite FindElementIcon(CombatElement element)
        {
            if (_elementIcons == null) return null;
            for (int i = 0; i < _elementIcons.Length; i++)
                if (_elementIcons[i].element == element)
                    return _elementIcons[i].sprite;
            return null;
        }

        private void BindEffectReader(IGameplayEffectRuntimeReader reader)
        {
            if (ReferenceEquals(_effectReader, reader))
            {
                RefreshEffects();
                return;
            }

            UnbindEffectReader();
            _effectReader = reader;
            if (_effectReader != null)
                _effectReader.StateChanged += RefreshEffects;
            RefreshEffects();
        }

        private void UnbindEffectReader()
        {
            if (_effectReader != null)
                _effectReader.StateChanged -= RefreshEffects;
            _effectReader = null;
        }

        private void RefreshEffects()
        {
            _effectViews.Clear();
            _selectedEffectViews.Clear();

            if (_effectReader == null
                || _effectIconRoot == null
                || _effectIconTemplate == null)
            {
                ReleaseAllEffectIcons();
                SetEffectOverflow(0);
                return;
            }

            _effectReader.CopyVisibleEffects(_effectViews);
            _effectViews.Sort(CompareSelectionPriority);

            int maxVisible = Mathf.Max(1, _maxVisibleEffects);
            int displayCount = Mathf.Min(maxVisible, _effectViews.Count);
            for (int i = 0; i < displayCount; i++)
                _selectedEffectViews.Add(_effectViews[i]);
            _selectedEffectViews.Sort(CompareDisplayOrder);

            EnsureEffectIconPool(displayCount);
            for (int i = 0; i < displayCount; i++)
                _effectIcons[i].Bind(_selectedEffectViews[i], _effectFallbackIcon);
            for (int i = displayCount; i < _effectIcons.Count; i++)
                _effectIcons[i].Release();

            _activeEffectIconCount = displayCount;
            SetEffectOverflow(_effectViews.Count - displayCount);
        }

        private void RefreshEffectTimers()
        {
            if (_effectReader == null || _activeEffectIconCount == 0)
                return;

            bool requiresFullRefresh = false;
            for (int i = 0; i < _activeEffectIconCount; i++)
            {
                UIGameplayEffectIcon icon = _effectIcons[i];
                if (_effectReader.TryGetVisibleEffect(
                        icon.RuntimeId,
                        out GameplayEffectViewState state))
                {
                    icon.Refresh(state);
                }
                else
                {
                    requiresFullRefresh = true;
                    break;
                }
            }

            if (requiresFullRefresh)
                RefreshEffects();
        }

        private void EnsureEffectIconPool(int count)
        {
            while (_effectIcons.Count < count)
            {
                UIGameplayEffectIcon icon = Instantiate(
                    _effectIconTemplate,
                    _effectIconRoot,
                    worldPositionStays: false);
                icon.Release();
                _effectIcons.Add(icon);
            }
        }

        private void ReleaseAllEffectIcons()
        {
            for (int i = 0; i < _effectIcons.Count; i++)
                _effectIcons[i].Release();
            _activeEffectIconCount = 0;
        }

        private void SetEffectOverflow(int overflowCount)
        {
            if (_effectOverflowText == null)
                return;
            bool visible = overflowCount > 0;
            _effectOverflowText.gameObject.SetActive(visible);
            _effectOverflowText.text = visible ? $"+{overflowCount}" : string.Empty;
        }

        private static int CompareSelectionPriority(
            GameplayEffectViewState left,
            GameplayEffectViewState right)
        {
            int priority = right.HudPriority.CompareTo(left.HudPriority);
            if (priority != 0) return priority;

            int harmful = PolaritySelectionRank(right.Polarity)
                .CompareTo(PolaritySelectionRank(left.Polarity));
            if (harmful != 0) return harmful;

            int remaining = left.RemainingSeconds.CompareTo(right.RemainingSeconds);
            if (remaining != 0) return remaining;
            return string.CompareOrdinal(left.EffectId, right.EffectId);
        }

        private static int CompareDisplayOrder(
            GameplayEffectViewState left,
            GameplayEffectViewState right)
        {
            int polarity = PolarityDisplayRank(left.Polarity)
                .CompareTo(PolarityDisplayRank(right.Polarity));
            if (polarity != 0) return polarity;

            int priority = right.HudPriority.CompareTo(left.HudPriority);
            if (priority != 0) return priority;
            return string.CompareOrdinal(left.EffectId, right.EffectId);
        }

        private static int PolaritySelectionRank(GameplayEffectPolarity polarity) =>
            polarity == GameplayEffectPolarity.Harmful ? 1 : 0;

        private static int PolarityDisplayRank(GameplayEffectPolarity polarity) =>
            polarity switch
            {
                GameplayEffectPolarity.Beneficial => 0,
                GameplayEffectPolarity.Neutral => 1,
                GameplayEffectPolarity.Harmful => 2,
                _ => 1,
            };
    }
}
