using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Path;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

/// <summary>
/// UI 창 열기 위한 메뉴 패널
/// </summary>
public class UI_MenuPanel : UI_Base
{
    #region UI_Base 생명주기

    [SerializeField] private Button _mapButton;
    [SerializeField] private Button _bagButton;
    [SerializeField] private Button _craftButton;
    [SerializeField] private Button _questButton;
    [SerializeField] private Button _partyButton;
    [SerializeField] private Button _configButton;
    
    
    protected override void Awake()
    {
        base.Awake();
        
        _mapButton.onClick.AddListener(OnClickedMapButton);
        _bagButton.onClick.AddListener(OnClickedBagButton);
        _craftButton.onClick.AddListener(OnClickedCraftButton);
        _questButton.onClick.AddListener(OnClickedQuestButton);
        _partyButton.onClick.AddListener(OnClickedPartyButton);
        _configButton.onClick.AddListener(OnClickedConfigButton);
    }

    protected override void OnShow()
    {
        base.OnShow();
        
        InputManager.Instance.ShowCursor(true);
    }

    protected override void OnHide()
    {
        InputManager.Instance.SetInputLayer(InputLayer.None);
        InputManager.Instance.ShowCursor(false);
    }

    protected override void OnDispose()
    {
    }

    public override bool PerformBackFunction()
    {
        Hide();
        return false;
    }
    #endregion

    private void OnClickedMapButton()
    {
        Toggle(UIKeyType.Map);
    }

    private void OnClickedBagButton()
    {
        Toggle(UIKeyType.Inventory);
    }

    private void OnClickedCraftButton()
    {
        Toggle(UIKeyType.Craft);
    }

    private void OnClickedQuestButton()
    {
        Toggle(UIKeyType.Quest);
    }

    private void OnClickedPartyButton()
    {
        Toggle(UIKeyType.Party);
    }

    private void OnClickedConfigButton()
    {
        Toggle(UIKeyType.Config);
    }

    private void Toggle(UIKeyType type)
    {
        GameObject go = UIManager.Instance.GetActiveUI(type);
        if (go == null)
        {
            UIManager.Instance.ShowUI(type);
            return;
        }

        UI_Base ui = go.GetComponent<UI_Base>();
        if (ui == null || ui.IsVisible == false)
        {
            UIManager.Instance.ShowUI(type);
        }
        else
        {
            UIManager.Instance.HideUI(type);
        }
    }
}