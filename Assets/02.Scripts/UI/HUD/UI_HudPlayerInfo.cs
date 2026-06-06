using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

public class UI_HudPlayerInfo : UI_Base
{
    [SerializeField] private Image _boardHpFill;
    [SerializeField] private Image _boardHpWhiteFill;

    [SerializeField] private TextMeshProUGUI _hpText;
    [SerializeField] private TextMeshProUGUI _levelText;

    [Header("Ultimate Gauge")]
    [SerializeField] private Image _skillGaugeFill;
    [SerializeField] private TextMeshProUGUI _skillGaugeText;

    [Header("Animation Settings")]
    [SerializeField] private float _hpDecreaseDelayTime = 0.3f;
    [SerializeField] private float _hpFillSpeed         = 5.0f;
    [SerializeField] private float _skillGaugeFillSpeed = 8.0f;

    private Coroutine _hpFillCoroutine;
    private Coroutine _skillGaugeCoroutine;
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
        }

        SetLevel(_playerActor);
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

    }

    private void OnPartyProgressionChanged(CharacterActorType type)
    {
        if (_playerActor == null || type != _playerActor.CharacterType) return;
        SetLevel(_playerActor);
    }

    private void SetLevel(PlayerActor player)
    {
        if (_levelText == null || player == null) return;

        int level = PartyManager.Instance?.GetLevel(player.CharacterType) ?? 1;
        _levelText.text = $"Lv. {Mathf.Max(1, level)}";
    }
}

