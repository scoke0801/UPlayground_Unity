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
    
    #region UI_Base
    protected override void OnShow()
    {
        _boardHpFill.fillAmount = 1.0f;
        _boardHpWhiteFill.fillAmount = 1.0f;
        
        AnimationChange("On");
        
        GameObjectManager.Instance.OnInteractionOut += OnInteractionOut;
    }

    protected override void OnHide()
    {
        GameObjectManager.Instance.OnInteractionOut -= OnInteractionOut;
    }

    protected override void OnClose()
    {
        if (GameObjectManager.Instance)
        {
            GameObjectManager.Instance.OnInteractionOut -= OnInteractionOut;
        }
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
    
    private void OnInteractionOut()
    {
        AnimationChange("Out");
    }
}
