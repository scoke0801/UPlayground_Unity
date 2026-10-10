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


        [Header("캐릭터 문양")]
        [SerializeField] private UICharacterGauge _characterGauge;
        [SerializeField, Min(0.01f)] private float _abilityStateRefreshInterval = 0.05f;
        private IAbilityRuntimeReader _abilityReader;
        private SettingsData _settings;
        private float _nextAbilityRefresh;

        [SerializeField, Range(0.01f, 1f)] private float _staminaArcFraction = 0.22f;
        [SerializeField] private Vector3 _staminaWorldOffset = new(0f, 1f, 0f);
        [SerializeField] private Vector2 _staminaScreenOffset = new(80f, 0f);
        private UnityEngine.Camera _worldCamera;
        private RectTransform _staminaParent;

        [Header("Stamina")]
        [SerializeField] private RectTransform _staminaPanel;
        [SerializeField] private Image _staminaFill;
        [SerializeField] private TextMeshProUGUI _staminaText;
        [SerializeField] private Color _staminaNormalColor =
            new(0.96f, 0.7f, 0.18f, 1f);
        [SerializeField] private Color _staminaLowColor =
            new(1f, 0.3f, 0.12f, 1f);
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
            _boardHpFill.fillAmount      = 1.0f;
            _boardHpWhiteFill.fillAmount = 1.0f;

            if (UISvc.Actors == null) return;

            _playerActor = UISvc.Actors.Player;
            if (_playerActor == null) return;

            _playerActor.EnsureCharacterRuntimeInitialized();
            _settings = UISvc.Settings?.Data;
            _worldCamera = Svc.Camera?.GetMainCamera();
            _staminaParent = _staminaPanel != null ? _staminaPanel.parent as RectTransform : null;
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
            }

            BindEffectReader(_playerActor.Effects);
        }

        protected override void OnHide()
        {
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
                RefreshGaugeAvailability();
                _nextAbilityRefresh = Time.unscaledTime + _abilityStateRefreshInterval;
            }
            _characterGauge?.Tick(Time.unscaledTime, _settings != null && _settings.reduceHudMotion);
            UpdateStaminaFill();
            if (_worldCamera != null && _playerActor != null && _staminaParent != null)
            {
                Vector3 screen = _worldCamera.WorldToScreenPoint(_playerActor.transform.position + _staminaWorldOffset);
                _staminaPanel.gameObject.SetActive(screen.z > 0f);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_staminaParent, screen, null, out Vector2 local))
                    _staminaPanel.anchoredPosition = local + _staminaScreenOffset;
            }
            RefreshEffectTimers();
        }

        public void SetHp(float hp, float maxHp)
        {
            float ratio = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;
            _boardHpFill.fillAmount = ratio;

            if (_hpFillCoroutine != null) StopCoroutine(_hpFillCoroutine);
            if (_settings != null && _settings.reduceHudMotion)
                _boardHpWhiteFill.fillAmount = ratio;
            else
                _hpFillCoroutine = StartCoroutine(HpDelayFillCoroutine());

            _hpText.text = $"{(int)hp}/{(int)maxHp}";
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
            UPlayGround.Ability.Core.AbilitySlotViewState state = default;
            bool hasState = _abilityReader != null
                && _abilityReader.TryGetPlayerSlotState(PlayerSkillSlot.Ultimate, out state);
            _characterGauge.SetAbilityState(hasState, state);
        }

        /// <summary>스태미나 목표값과 정수 표시를 갱신한다.</summary>
        public void SetStamina(float stamina, float maximum)
        {
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
            KillStaminaTweens();
            _hasStaminaSnapshot = false;
            SetStamina(stamina, maximum);
            if (_staminaFill != null)
                _staminaFill.fillAmount = _staminaTargetRatio * _staminaArcFraction;
        }

        private void UpdateStaminaFill()
        {
            if (_staminaFill == null) return;
            if (_settings != null && _settings.reduceHudMotion)
            {
                _staminaFill.fillAmount = _staminaTargetRatio * _staminaArcFraction;
                return;
            }
            _staminaFill.fillAmount = Mathf.MoveTowards(
                _staminaFill.fillAmount,
                _staminaTargetRatio * _staminaArcFraction,
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
                    () => _staminaFill.color,
                    value => _staminaFill.color = value,
                    GetStaminaColor(),
                    0.12f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        private void SetStaminaColorImmediate()
        {
            if (_staminaFill != null)
                _staminaFill.color = GetStaminaColor();
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
