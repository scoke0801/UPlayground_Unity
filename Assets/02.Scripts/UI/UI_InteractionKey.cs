using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class UI_InteractionKey : UI_Base
{
    protected override void OnShow()
    {
        AnimationChange("On");
        InputManager.Instance.InteractAction.performed += OnInteract;
    }

    protected override void OnHide()
    {
        InputManager.Instance.InteractAction.performed -= OnInteract;
    }

    protected override void OnClose()
    {
        if (InputManager.Instance == null) return;
        if (InputManager.Instance.InteractAction == null) return;

        InputManager.Instance.InteractAction.performed -= OnInteract;

    }

    protected void OnDisable()
    {
        
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        Hide();
    }
}
