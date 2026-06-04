using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Manager;

public class UI_InteractionKey : UI_Base
{
    private bool _isSubscribed = false;
    
    protected override void OnShow()
    {
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