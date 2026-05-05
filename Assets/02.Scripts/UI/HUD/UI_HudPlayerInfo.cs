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
    [SerializeField] private GameObject _fxObject;
    
    [Header("Animation Settings")]
    [SerializeField] private float _hpDecreaseDelayTime = 0.3f;
    [SerializeField] private float _hpFillSpeed         = 5.0f;
    
    [System.Serializable]
    private struct CharacterIconEntry
    {
        public CharacterActorType type;
        public Sprite icon;
    }

    [SerializeField] private CharacterIconEntry[] _actorIcons;

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

        _playerActor.OnHpChanged         += SetHp;

        SetHp(_playerActor.CurrentHealth, _playerActor.MaxHealth);

        float cur = _playerActor.SkillGauge?.CurrentGauge ?? 0f;
        float max = _playerActor.SkillGauge?.MaxGauge     ?? 100f;
        
        var partyManager = PartyManager.Instance;
        if (partyManager != null)
        {
            partyManager.OnSwapCompleted += OnPlayerSwapCompleted;
        }
    }

    protected override void OnHide()
    {
        if (_playerActor == null) return;
        _playerActor.OnHpChanged         -= SetHp;
        
        var partyManager = PartyManager.Instance;
        if (partyManager != null)
        {
            partyManager.OnSwapCompleted -= OnPlayerSwapCompleted;
        }
    }

    protected override void OnClose() { }
    #endregion
    
    public void SetHp(float hp, float maxHp)
    {
        _boardHpFill.fillAmount = hp / maxHp;

        if (_hpFillCoroutine != null) StopCoroutine(_hpFillCoroutine);
        _hpFillCoroutine = StartCoroutine(HpDelayFillCoroutine());

        _hpText.text = $"{(int)hp}/{(int)maxHp}";
    }

    public void SetIsInCombat(bool isInCombat)
    {
        _isInCombat = isInCombat;
        if (_isInCombat == false && _fxObject != null)
        {
            _fxObject.SetActive(false);
        }
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
        if (player == null)
        {
            return;
        }
    }
}

