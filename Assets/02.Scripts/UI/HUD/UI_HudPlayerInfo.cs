using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    public class UI_HudPlayerInfo : UI_Base
    {
        [SerializeField] private Image _boardHpFill;
        [SerializeField] private Image _boardHpWhiteFill;

        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _levelText;

        [Header("Ultimate Gauge")]
        [SerializeField] private Image _skillGaugeFill;
        [SerializeField] private TextMeshProUGUI _skillGaugeText;

        [Header("EXP")]
        [SerializeField] private Image _expFill;
        [SerializeField] private TextMeshProUGUI _expText;

        [Header("Animation Settings")]
        [SerializeField] private float _hpDecreaseDelayTime = 0.3f;
        [SerializeField] private float _hpFillSpeed         = 5.0f;
        [SerializeField] private float _skillGaugeFillSpeed = 8.0f;
        [SerializeField] private float _expFillSpeed        = 8.0f;
        [SerializeField] private float _levelPunchScale     = 1.3f;
        [SerializeField] private float _levelPunchDuration  = 0.35f;

        private Coroutine _hpFillCoroutine;
        private Coroutine _skillGaugeCoroutine;
        private Coroutine _expFillCoroutine;
        private Coroutine _levelPunchCoroutine;
        private Vector3?  _levelTextBaseScale;
        private PlayerActor _playerActor;

        private bool _isInCombat = false;

        #region UI_Base
        protected override void OnShow()
        {
            _boardHpFill.fillAmount      = 1.0f;
            _boardHpWhiteFill.fillAmount = 1.0f;

            if (GameObjectManager.Instance == null) return;

            _playerActor = GameObjectManager.Instance.Player;
            if (_playerActor == null) return;

            _playerActor.EnsureCharacterRuntimeInitialized();

            _playerActor.OnHpChanged         += SetHp;
            _playerActor.OnSkillGaugeChanged += SetSkillGauge;

            SetHp(_playerActor.CurrentHealth, _playerActor.MaxHealth);

            float gauge    = _playerActor.SkillGauge?.CurrentGauge ?? 0f;
            float maxGauge = _playerActor.SkillGauge?.MaxGauge     ?? 100f;
            SetSkillGaugeImmediate(gauge, maxGauge);

            var partyManager = PartyManager.Instance;
            if (partyManager != null)
            {
                partyManager.OnSwapCompleted += OnPlayerSwapCompleted;
                partyManager.OnPartyProgressionChanged += OnPartyProgressionChanged;
                partyManager.OnExpChanged += OnExpChanged;
                partyManager.OnLevelUp += OnLevelUp;
            }

            SetLevel(_playerActor);
            RefreshExp(_playerActor.CharacterType);
        }

        protected override void OnHide()
        {
            if (_playerActor != null)
            {
                _playerActor.OnHpChanged         -= SetHp;
                _playerActor.OnSkillGaugeChanged -= SetSkillGauge;
            }

            var partyManager = PartyManager.Instance;
            if (partyManager != null)
            {
                partyManager.OnSwapCompleted -= OnPlayerSwapCompleted;
                partyManager.OnPartyProgressionChanged -= OnPartyProgressionChanged;
                partyManager.OnExpChanged -= OnExpChanged;
                partyManager.OnLevelUp -= OnLevelUp;
            }
        }

        protected override void OnClose() { }
        #endregion

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
                    Time.deltaTime * _skillGaugeFillSpeed);
                yield return null;
            }

            _skillGaugeFill.fillAmount = targetRatio;
            _skillGaugeCoroutine = null;
        }

        public void SetIsInCombat(bool isInCombat)
        {
            _isInCombat = isInCombat;
        }

        private IEnumerator HpDelayFillCoroutine()
        {
            yield return new WaitForSeconds(_hpDecreaseDelayTime);

            while (_boardHpWhiteFill.fillAmount > _boardHpFill.fillAmount + 0.001f)
            {
                _boardHpWhiteFill.fillAmount = Mathf.Lerp(
                    _boardHpWhiteFill.fillAmount,
                    _boardHpFill.fillAmount,
                    Time.deltaTime * _hpFillSpeed);
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
            SetLevel(player);
            RefreshExp(player.CharacterType);
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

            int level = PartyManager.Instance?.GetLevel(player.CharacterType) ?? 1;
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
            var pm = PartyManager.Instance;
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
                    _expFill.fillAmount, targetRatio, Time.deltaTime * _expFillSpeed);
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
                e += Time.deltaTime;
                t.localScale = Vector3.Lerp(baseScale, baseScale * _levelPunchScale, e / half);
                yield return null;
            }
            e = 0f;
            while (e < half)
            {
                e += Time.deltaTime;
                t.localScale = Vector3.Lerp(baseScale * _levelPunchScale, baseScale, e / half);
                yield return null;
            }

            t.localScale = baseScale;
            _levelPunchCoroutine = null;
        }
    }
}
