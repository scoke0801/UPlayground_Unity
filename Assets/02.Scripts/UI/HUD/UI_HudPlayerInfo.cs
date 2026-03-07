using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Manager;

public class UI_HudPlayerInfo : UI_Base
{
    [SerializeField] private Image _boardHpFill;
    [SerializeField] private Image _boardHpWhiteFill;
    [SerializeField] private Image _characterIcon;
    [SerializeField] private Image _characterIconBG;
    [SerializeField] private Image _skillGuageFill;

    [SerializeField] private TextMeshProUGUI _hpText;
    [SerializeField] private GameObject _fxObject;
    
    [Header("Animation Settings")]
    [SerializeField] private float _hpDecreaseDelayTime = 0.3f;
    [SerializeField] private float _hpFillSpeed         = 5.0f;
    [SerializeField] private float _skillFillSpeed      = 8.0f;

    private Coroutine _hpFillCoroutine;
    private Coroutine _skillGaugeCoroutine;
    private PlayerActor _playerActor;

    private float _skillTargetRatio;

    #region UI_Base
    protected override void OnShow()
    {
        _boardHpFill.fillAmount      = 1.0f;
        _boardHpWhiteFill.fillAmount = 1.0f;

        if (_skillGuageFill != null) _skillGuageFill.fillAmount = 0f;
        _skillTargetRatio = 0f;

        if (GameObjectManager.Instance == null) return;

        _playerActor = GameObjectManager.Instance.Player;
        if (_playerActor == null) return;

        _playerActor.OnHpChanged         += SetHp;
        _playerActor.OnSkillGaugeChanged += SetSkillGauge;

        SetHp(_playerActor.CurrentHealth, _playerActor.MaxHealth);

        float cur = _playerActor.SkillGauge?.CurrentGauge ?? 0f;
        float max = _playerActor.SkillGauge?.MaxGauge     ?? 100f;
        SetSkillGauge(cur, max);
    }

    protected override void OnHide()
    {
        if (_playerActor == null) return;
        _playerActor.OnHpChanged         -= SetHp;
        _playerActor.OnSkillGaugeChanged -= SetSkillGauge;
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

    
    public void SetSkillGauge(float current, float max)
    {
        if (_skillGuageFill == null) return;

        _skillTargetRatio = current / max;

        bool isFullGauge = Mathf.Approximately(_skillTargetRatio, 1f);
        _animator.SetBool("IsSkillGaugeFull", isFullGauge);
        
        _fxObject.SetActive(isFullGauge);
        
        if (_skillGaugeCoroutine != null) StopCoroutine(_skillGaugeCoroutine);
        _skillGaugeCoroutine = StartCoroutine(SkillGaugeLerpCoroutine());
    }

    private IEnumerator SkillGaugeLerpCoroutine()
    {
        while (Mathf.Abs(_skillGuageFill.fillAmount - _skillTargetRatio) > 0.001f)
        {
            _skillGuageFill.fillAmount = Mathf.Lerp(
                _skillGuageFill.fillAmount,
                _skillTargetRatio,
                Time.deltaTime * _skillFillSpeed);
            yield return null;
        }

        _skillGuageFill.fillAmount = _skillTargetRatio;
    }
}

