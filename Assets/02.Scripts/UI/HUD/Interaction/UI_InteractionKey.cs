using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

public class UI_InteractionKey : UI_Base
{
    // 상호작용 키 프롬프트. 활성 디바이스에 따라 F(키보드) / 버튼(게임패드)로 자동 전환된다.
    [SerializeField] private UI_InputPromptIcon _promptIcon;

    private bool _isSubscribed = false;

    protected override void OnShow()
    {
        AnimationChange("On");
        if (_promptIcon != null)
            _promptIcon.SetAction(InputMapNames.PlayerAction, PlayerAction.Interact);
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

    private void SubscribeEvents()
    {
        if (_isSubscribed) return;
    }
    
    private void UnsubscribeEvents()
    {
        if (!_isSubscribed) return;

        _isSubscribed = false;
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (this.gameObject != null)
        {
            Hide();   
        }
    }
}