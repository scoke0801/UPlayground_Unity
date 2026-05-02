using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Crafting;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

public class UI_QuestMenu : UI_Base
{
    #region UI_Base 생명주기

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnShow()
    {
        InputManager.Instance.SetInputLayer(_layer.ToInputLayer());
    }

    protected override void OnHide()
    {
        InputManager.Instance.SetInputLayer(InputLayer.None);
    }

    protected override void OnDispose()
    {
    }

    public override bool PerformBackFunction()
    {
        Hide();
        return false;
    }

    protected override void Update()
    {
        base.Update();
    }

    #endregion
}