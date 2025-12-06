using System;
using UnityEngine;

public class UI_InteractionHPBoard : UI_Base
{
    protected override void OnShow()
    {
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

    private void OnInteractionOut()
    {
        AnimationChange("Out");
    }
}
