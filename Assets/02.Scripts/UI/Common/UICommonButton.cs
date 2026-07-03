

using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public enum UICommonButtonClickResult
{
    None = 0,
    Success,
    Failed,
}

public class UICommonButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _buttonText;

    public TextMeshProUGUI Text => this._buttonText;
    public Button Button => _button;
    public UICommonButtonClickResult LastClickResult { get; private set; } = UICommonButtonClickResult.None;

    public event Action<UICommonButtonClickResult> OnClickResultChanged;

    private Func<UICommonButtonClickResult> _clickResultHandler;

    private void Awake()
    {
        _button?.onClick.AddListener(InvokeClickResultHandler);
    }

    private void OnDestroy()
    {
        _button?.onClick.RemoveListener(InvokeClickResultHandler);
    }

    public void BindClickResult(Func<UICommonButtonClickResult> handler)
    {
        _clickResultHandler = handler;
        LastClickResult = UICommonButtonClickResult.None;
    }

    public void ClearClickResult()
    {
        _clickResultHandler = null;
        LastClickResult = UICommonButtonClickResult.None;
    }

    private void InvokeClickResultHandler()
    {
        if (_clickResultHandler == null)
        {
            return;
        }

        LastClickResult = _clickResultHandler.Invoke();
        OnClickResultChanged?.Invoke(LastClickResult);
    }
}
