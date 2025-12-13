using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
        
        if (InputManager.Instance?.InteractAction != null)
        {
            InputManager.Instance.InteractAction.performed += OnInteract;
            _isSubscribed = true;
        }
    }
    
    private void UnsubscribeEvents()
    {
        if (!_isSubscribed) return;
        
        if (InputManager.Instance?.InteractAction != null)
        {
            InputManager.Instance.InteractAction.performed -= OnInteract;
        }
        
        _isSubscribed = false;
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        Hide();
    }
}
