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

namespace UPlayGround.UI
{
    public class UI_HUD_PlayerInfo : UI_Base
    {
        [SerializeField] private Image _boardHpFill;
        [SerializeField] private Image _boardHpWhiteFill;

        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _levelText;

        [Header("Ultimate Gauge")]
        [SerializeField] private Image _skillGaugeFill;
        [SerializeField] private TextMeshProUGUI _skillGaugeText;

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

        [Header("EXP")]
        [SerializeField] private Image _expFill;
        [SerializeField] private TextMeshProUGUI _expText;

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
        [SerializeField] private float _skillGaugeFillSpeed = 8.0f;
        [SerializeField] private float _staminaFillSpeed    = 10.0f;
        [SerializeField] private float _expFillSpeed        = 8.0f;
        [SerializeField] private float _levelPunchScale     = 1.3f;
        [SerializeField] private float _levelPunchDuration  = 0.35f;

        private Coroutine _hpFillCoroutine;
        private Coroutine _skillGaugeCoroutine;
        private Coroutine _expFillCoroutine;
        private Coroutine _levelPunchCoroutine;
        private Vector3?  _levelTextBaseScale;
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

        #region UI_Base
        protected override void OnShow()
        {
            _boardHpFill.fillAmount      = 1.0f;
            _boardHpWhiteFill.fillAmount = 1.0f;

            if (UISvc.Actors == null) return;

            _playerActor = UISvc.Actors.Player;
            if (_playerActor == null) return;

            _playerActor.EnsureCharacterRuntimeInitialized();

            _playerActor.OnHpChanged         += SetHp;
            _playerActor.OnSkillGaugeChanged += SetSkillGauge;
            _playerActor.OnStaminaChanged    += SetStamina;

            SetHp(_playerActor.CurrentHealth, _playerActor.MaxHealth);

            float gauge    = _playerActor.SkillGauge?.CurrentGauge ?? 0f;
            float maxGauge = _playerActor.SkillGauge?.MaxGauge     ?? 100f;
            SetSkillGaugeImmediate(gauge, maxGauge);
            SetStaminaImmediate(
                _playerActor.Stamina?.Current ?? 0f,
                _playerActor.Stamina?.Maximum ?? 0f);

            var partyManager = UISvc.Party;
            if (partyManager != null)
            {
                partyManager.OnSwapCompleted += OnPlayerSwapCompleted;
                partyManager.OnPartyProgressionChanged += OnPartyProgressionChanged;
                partyManager.OnExpChanged += OnExpChanged;
                partyManager.OnLevelUp += OnLevelUp;
            }

            SetLevel(_playerActor);
            RefreshExp(_playerActor.CharacterType);
            BindEffectReader(_playerActor.Effects);
        }

        protected override void OnHide()
        {
            KillStaminaTweens();
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
                partyManager.OnPartyProgressionChanged -= OnPartyProgressionChanged;
                partyManager.OnExpChanged -= OnExpChanged;
                partyManager.OnLevelUp -= OnLevelUp;
            }

            UnbindEffectReader();
            ReleaseAllEffectIcons();
        }

        protected override void OnClose() { }
        #endregion

        protected override void Update()
        {
            base.Update();
            UpdateStaminaFill();
            RefreshEffectTimers();
        }

        public void SetHp(float hp, float maxHp)
        {
            float ratio = maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;
            _boardHpFill.fillAmount = ratio;

            if (_hpFillCoroutine != null) StopCoroutine(_hpFillCoroutine);
            _hpFillCoroutine = StartCoroutine(HpDelayFillCoroutine());

            _hpText.text = $"{(int)hp}/{(int)maxHp}";
        }

        /// <summary>현재 Phase에서는 Ultimate 게이지 변경 시 호출한다. Fill을 부드럽게 보간한다.</summary>
        public void SetSkillGauge(float gauge, float maxGauge)
        {
            float ratio = maxGauge > 0f ? Mathf.Clamp01(gauge / maxGauge) : 0f;

            if (_skillGaugeText != null)
                _skillGaugeText.text = $"{(int)gauge}/{(int)maxGauge}";

            if (_skillGaugeFill == null) return;

            if (_skillGaugeCoroutine != null) StopCoroutine(_skillGaugeCoroutine);
            _skillGaugeCoroutine = StartCoroutine(SkillGaugeFillCoroutine(ratio));
        }

        /// <summary>보간 없이 즉시 Ultimate 게이지를 반영한다(초기화/캐릭터 교체 스냅용).</summary>
        private void SetSkillGaugeImmediate(float gauge, float maxGauge)
        {
            float ratio = maxGauge > 0f ? Mathf.Clamp01(gauge / maxGauge) : 0f;

            if (_skillGaugeCoroutine != null)
            {
                StopCoroutine(_skillGaugeCoroutine);
                _skillGaugeCoroutine = null;
            }

            if (_skillGaugeFill != null) _skillGaugeFill.fillAmount = ratio;
            if (_skillGaugeText != null) _skillGaugeText.text = $"{(int)gauge}/{(int)maxGauge}";
        }

        private IEnumerator SkillGaugeFillCoroutine(float targetRatio)
        {
            while (Mathf.Abs(_skillGaugeFill.fillAmount - targetRatio) > 0.001f)
            {
                _skillGaugeFill.fillAmount = Mathf.Lerp(
                    _skillGaugeFill.fillAmount,
                    targetRatio,
                    Time.unscaledDeltaTime * _skillGaugeFillSpeed);
                yield return null;
            }

            _skillGaugeFill.fillAmount = targetRatio;
            _skillGaugeCoroutine = null;
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
                _staminaFill.fillAmount = _staminaTargetRatio;
        }

        private void UpdateStaminaFill()
        {
            if (_staminaFill == null) return;
            _staminaFill.fillAmount = Mathf.MoveTowards(
                _staminaFill.fillAmount,
                _staminaTargetRatio,
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
            if (_staminaPanel == null || !isActiveAndEnabled) return;
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

            SetHp(player.CurrentHealth, player.MaxHealth);

            float gauge    = player.SkillGauge?.CurrentGauge ?? 0f;
            float maxGauge = player.SkillGauge?.MaxGauge     ?? 100f;
            SetSkillGaugeImmediate(gauge, maxGauge);
            SetStaminaImmediate(
                player.Stamina?.Current ?? 0f,
                player.Stamina?.Maximum ?? 0f);
            SetLevel(player);
            RefreshExp(player.CharacterType);
            BindEffectReader(player.Effects);
            RefreshEffects();
        }

        private void OnPartyProgressionChanged(CharacterActorType type)
        {
            if (_playerActor == null || type != _playerActor.CharacterType) return;
            SetLevel(_playerActor);
            RefreshExp(type);
        }

        private void SetLevel(PlayerActor player)
        {
            if (_levelText == null || player == null) return;

            int level = UISvc.Party?.GetLevel(player.CharacterType) ?? 1;
            _levelText.text = $"Lv. {Mathf.Max(1, level)}";
        }

        // ── EXP ──────────────────────────────────────────────────────

        private void OnExpChanged(CharacterActorType type, long current, long required)
        {
            if (_playerActor == null || type != _playerActor.CharacterType) return;
            SetExp(current, required);
        }

        private void OnLevelUp(CharacterActorType type, int newLevel)
        {
            if (_playerActor == null || type != _playerActor.CharacterType) return;

            // OnLevelUp은 PartyManager가 레벨 딕셔너리에 최종 값을 커밋하기 전에 발화한다.
            // GetLevel을 다시 조회하지 않고 이벤트로 전달된 새 레벨을 즉시 표시한다.
            if (_levelText != null)
                _levelText.text = $"Lv. {Mathf.Max(1, newLevel)}";

            PunchLevelText();
        }

        /// <summary>현재 활성 캐릭터의 경험치를 즉시 스냅한다(초기화/교체용).</summary>
        private void RefreshExp(CharacterActorType type)
        {
            var pm = UISvc.Party;
            if (pm == null) return;
            SetExpImmediate(pm.GetExp(type), pm.GetRequiredExp(type));
        }

        private void SetExp(long current, long required)
        {
            float ratio = required > 0 ? Mathf.Clamp01((float)current / required) : 1f;

            if (_expText != null)
                _expText.text = $"{current}/{required}";

            if (_expFill == null) return;

            if (_expFillCoroutine != null) StopCoroutine(_expFillCoroutine);
            _expFillCoroutine = StartCoroutine(ExpFillCoroutine(ratio));
        }

        private void SetExpImmediate(long current, long required)
        {
            float ratio = required > 0 ? Mathf.Clamp01((float)current / required) : 1f;

            if (_expFillCoroutine != null)
            {
                StopCoroutine(_expFillCoroutine);
                _expFillCoroutine = null;
            }

            if (_expFill != null) _expFill.fillAmount = ratio;
            if (_expText != null) _expText.text = $"{current}/{required}";
        }

        private IEnumerator ExpFillCoroutine(float targetRatio)
        {
            // 레벨업으로 게이지가 줄어드는 경우(다음 레벨로 리셋)에도 자연스럽게 보간한다.
            while (Mathf.Abs(_expFill.fillAmount - targetRatio) > 0.001f)
            {
                _expFill.fillAmount = Mathf.Lerp(
                    _expFill.fillAmount, targetRatio, Time.unscaledDeltaTime * _expFillSpeed);
                yield return null;
            }

            _expFill.fillAmount = targetRatio;
            _expFillCoroutine = null;
        }

        private void PunchLevelText()
        {
            if (_levelText == null) return;

            Transform t = _levelText.transform;
            // 최초 1회만 휴지(rest) 스케일을 캐싱한다. 펀치 도중 재호출 시 부풀어 있는 스케일을
            // 기준으로 잡으면 점점 커지는(drift) 버그가 생기므로, 항상 캐싱된 기준으로 복원 후 재생.
            if (!_levelTextBaseScale.HasValue) _levelTextBaseScale = t.localScale;
            if (_levelPunchCoroutine != null)
            {
                StopCoroutine(_levelPunchCoroutine);
                t.localScale = _levelTextBaseScale.Value;
            }
            _levelPunchCoroutine = StartCoroutine(LevelPunchCoroutine(_levelTextBaseScale.Value));
        }

        private IEnumerator LevelPunchCoroutine(Vector3 baseScale)
        {
            Transform t = _levelText.transform;
            float half = Mathf.Max(0.01f, _levelPunchDuration) * 0.5f;

            float e = 0f;
            while (e < half)
            {
                e += Time.unscaledDeltaTime;
                t.localScale = Vector3.Lerp(baseScale, baseScale * _levelPunchScale, e / half);
                yield return null;
            }
            e = 0f;
            while (e < half)
            {
                e += Time.unscaledDeltaTime;
                t.localScale = Vector3.Lerp(baseScale * _levelPunchScale, baseScale, e / half);
                yield return null;
            }

            t.localScale = baseScale;
            _levelPunchCoroutine = null;
        }

        // ── Buff / Debuff ───────────────────────────────────────────

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
