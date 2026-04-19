using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Item;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

/// <summary>
/// 플레이어 사망 후 부활 방식을 선택하는 팝업.
/// UIManager.ShowUI(UIKeyType.RespawnPopup) 로 표시한 뒤 Setup()을 호출한다.
/// </summary>
public class UI_RespawnPopup : UI_Base
{
    [Header("제자리 부활")]
    [SerializeField] private Button           _spotReviveButton;
    [SerializeField] private TextMeshProUGUI  _spotReviveLabel;
    [SerializeField] private TextMeshProUGUI  _spotItemCountText;

    [Header("포탈 부활")]
    [SerializeField] private Button           _portalReviveButton;

    [Header("설정")]
    [Tooltip("제자리 부활에 사용할 아이템 ID (기본: 부활석=100006)")]
    [SerializeField] private int _revivalItemId = (int)ItemIdType.None;
    [Tooltip("제자리 부활 시 회복할 HP 비율 (0~1)")]
    [SerializeField] private float _spotHealPercent = 0.5f;

    private Action _onSpotRevive;
    private Action _onPortalRevive;

    protected override void Awake()
    {
        base.Awake();
        _canCloseWithEsc = false;

        if (_spotReviveButton  != null) _spotReviveButton.onClick.AddListener(OnSpotReviveClicked);
        if (_portalReviveButton != null) _portalReviveButton.onClick.AddListener(OnPortalReviveClicked);
    }

    protected override void OnShow()
    {
        base.OnShow();
        GameTimeManager.Instance?.SetPause(true);
        InputManager.Instance?.SetInputLayer(_layer.ToInputLayer());
        FadeIn(0.3f);
        RefreshItemCount();
    }

    protected override void OnHide()
    {
        GameTimeManager.Instance?.SetPause(false);
        InputManager.Instance?.SetInputLayer(UPlayGround.InputDefine.InputLayer.None);
        _onSpotRevive  = null;
        _onPortalRevive = null;
        base.OnHide();
    }

    /// <summary>
    /// 팝업 표시 후 반드시 호출. 버튼 콜백과 소지 아이템 수를 세팅한다.
    /// </summary>
    public void Setup(Action onSpotRevive, Action onPortalRevive)
    {
        _onSpotRevive   = onSpotRevive;
        _onPortalRevive = onPortalRevive;
        RefreshItemCount();
    }

    private void RefreshItemCount()
    {
        int count = InventoryManager.Instance?.GetItemCount(_revivalItemId) ?? 0;

        if (_spotItemCountText != null)
            _spotItemCountText.text = $"부활석 x{count}";

        if (_spotReviveButton != null)
            _spotReviveButton.interactable = count > 0;

        if (_spotReviveLabel != null)
            _spotReviveLabel.color = count > 0
                ? Color.white
                : new Color(0.5f, 0.5f, 0.5f, 1f);
    }

    private void OnSpotReviveClicked()
    {
        bool consumed = InventoryManager.Instance?.RemoveItem(_revivalItemId, 1) ?? false;
        if (!consumed) return;

        UIManager.Instance.HideUI(UPlayGround.Data.Path.UIKeyType.RespawnPopup.ToKey());
        _onSpotRevive?.Invoke();
    }

    private void OnPortalReviveClicked()
    {
        UIManager.Instance.HideUI(UPlayGround.Data.Path.UIKeyType.RespawnPopup.ToKey());
        _onPortalRevive?.Invoke();
    }
}
