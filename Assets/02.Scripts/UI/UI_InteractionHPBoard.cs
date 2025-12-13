using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UI_InteractionHPBoard : UI_Base
{
    [SerializeField] private Image _boardHpFill;
    [FormerlySerializedAs("_boardWhiteFill")] [SerializeField] private Image _boardHpWhiteFill;

    [SerializeField] private float _fillTimeScale = 5.0f;
    
    private Coroutine _fillCoroutine;
    private bool _isSubscribed = false;
    
    #region UI_Base
    protected override void OnShow()
    {
        _boardHpFill.fillAmount = 1.0f;
        _boardHpWhiteFill.fillAmount = 1.0f;
        
        AnimationChange("On");
        
        SubscribeEvents();
    }

    protected override void OnHide()
    {
        UnsubscribeEvents();
    }

    protected override void OnClose()
    {
        UnsubscribeEvents();
    }
    #endregion
    
    public void BoardFill(float hp, float maxHp)
    {
        _boardHpFill.fillAmount = hp / maxHp;
        if (_fillCoroutine != null)
        {
            StopCoroutine(_fillCoroutine);
        }
        
        _fillCoroutine = StartCoroutine(FillCoroutine());
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
    
    private void SubscribeEvents()
    {
        if (_isSubscribed) return;
        
        if (GameObjectManager.Instance != null)
        {
            GameObjectManager.Instance.OnInteractionOut += OnInteractionOut;
            _isSubscribed = true;
        }
    }
    
    private void UnsubscribeEvents()
    {
        if (!_isSubscribed) return;
        
        if (GameObjectManager.Instance != null)
        {
            GameObjectManager.Instance.OnInteractionOut -= OnInteractionOut;
        }
        
        _isSubscribed = false;
    }
    
    private void OnInteractionOut()
    {
        AnimationChange("Out");
    }
}
