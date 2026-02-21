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
    [SerializeField] private TextMeshProUGUI _hpText;

    [SerializeField] private float _fillTimeScale = 5.0f;
    
    private Coroutine _fillCoroutine;
    private PlayerActor _playerActor;
    
    #region UI_Base
    protected override void OnShow()
    {
        _boardHpFill.fillAmount = 1.0f;
        _boardHpWhiteFill.fillAmount = 1.0f;

        if (GameObjectManager.Instance != null)
        {
            _playerActor = GameObjectManager.Instance.Player;

            if (_playerActor != null)
            {
                _playerActor.OnHpChanged += SetHp;
                SetHp(_playerActor.CurrentHealth, _playerActor.MaxHealth);
            }
        }
    }

    protected override void OnHide()
    {
        if (_playerActor != null)
        {
            _playerActor.OnHpChanged -= SetHp;
        }
    }

    protected override void OnClose()
    {
    }
    #endregion
    
    public void SetHp(float hp, float maxHp)
    {
        _boardHpFill.fillAmount = hp / maxHp;
        if (_fillCoroutine != null)
        {
            StopCoroutine(_fillCoroutine);
        }
        
        _fillCoroutine = StartCoroutine(FillCoroutine());
        _hpText.text = $"{(int)hp}/{(int)maxHp}";
    }
    
    private IEnumerator FillCoroutine()
    {
        while (_boardHpWhiteFill.fillAmount > _boardHpFill.fillAmount)
        {
            _boardHpWhiteFill.fillAmount = Mathf.Lerp(_boardHpWhiteFill.fillAmount,
                _boardHpFill.fillAmount, Time.deltaTime * _fillTimeScale);

            yield return null;
        }
    }
}