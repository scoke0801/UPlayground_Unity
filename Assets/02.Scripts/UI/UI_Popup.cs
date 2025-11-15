using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 간단한 팝업 UI 예시
/// </summary>
public class UI_Popup : UI_Base
{
    [Header("UI 컴포넌트")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Button _closeButton;

    private System.Action _onConfirm;
    private System.Action _onCancel;

    protected override void OnInit()
    {
        base.OnInit();

        // 버튼 이벤트 바인딩
        if (_confirmButton != null)
        {
            _confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.AddListener(OnCancelClicked);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Close);
        }

        Debug.Log("[UI_Popup] 초기화 완료");
    }

    protected override void OnShow()
    {
        base.OnShow();

        // 페이드 인 효과
        FadeIn(0.2f);
    }

    protected override void OnHide()
    {
        base.OnHide();

        // 콜백 초기화
        _onConfirm = null;
        _onCancel = null;
    }

    protected override void OnDispose()
    {
        base.OnDispose();

        // 버튼 이벤트 해제
        if (_confirmButton != null)
        {
            _confirmButton.onClick.RemoveAllListeners();
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.RemoveAllListeners();
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
        }
    }

    #region Public API

    /// <summary>
    /// 팝업 설정
    /// </summary>
    public void Setup(string title, string message, System.Action onConfirm = null, System.Action onCancel = null)
    {
        if (_titleText != null)
        {
            _titleText.text = title;
        }

        if (_messageText != null)
        {
            _messageText.text = message;
        }

        _onConfirm = onConfirm;
        _onCancel = onCancel;

        // 취소 버튼이 없으면 비활성화
        if (_cancelButton != null)
        {
            _cancelButton.gameObject.SetActive(onCancel != null);
        }
    }

    #endregion

    #region 버튼 이벤트

    private void OnConfirmClicked()
    {
        _onConfirm?.Invoke();
        Close();
    }

    private void OnCancelClicked()
    {
        _onCancel?.Invoke();
        Close();
    }

    #endregion
}
