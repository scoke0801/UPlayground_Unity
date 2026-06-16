
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Component;
using UPlayGround.InputDefine;
using UPlayGround.Data.Path;
using UPlayGround.Manager;
using UPlayGround.UI.InputPrompt;

class UI_GamePlay : UI_Base
{
    [SerializeField] Button _menuButton;

    private PlayerActor _playerActor;

    private PlayerCombat _playerCombat;

    private UI_HudPlayerInfo _hudPlayerInfo;
    private UI_HudParty _hudParty;
    private UI_HudQuest _hudQuest;
    private UI_HudSkill _hudSkill;
    
    #region UI_Base

    protected override void Awake()
    {
        base.Awake();
        _menuButton.onClick.AddListener(OnClickedMenuButton);
    }

    protected override void OnShow()
    {
        _hudPlayerInfo = UIManager.Instance.ShowUI(UIKeyType.HudPlayerInfo)?.GetComponent<UI_HudPlayerInfo>();
        UIManager.Instance.ShowUI(UIKeyType.Minimap);

        _hudParty = UIManager.Instance.ShowUI(UIKeyType.HudParty)?.GetComponent<UI_HudParty>();

        _hudQuest = UIManager.Instance.ShowUI(UIKeyType.HudQuest)?.GetComponent<UI_HudQuest>();

        _hudSkill = UIManager.Instance.ShowUI(UIKeyType.HudSkill)?.GetComponent<UI_HudSkill>();

        UIManager.Instance.ShowUI(UIKeyType.OffscreenThreatIndicator);

        if (GameObjectManager.Instance != null)
        {
            _playerActor = GameObjectManager.Instance.Player;
            _playerCombat = _playerActor?.GetCombat();
            if (_playerCombat != null)
            {
                _playerCombat.OnChangeCombatState += OnPlayerCombatStateChanged;
            }
        }
    }

    protected override void OnHide()
    {
        UIManager.Instance.HideUI(UIKeyType.HudPlayerInfo);
        UIManager.Instance.HideUI(UIKeyType.Minimap);
        UIManager.Instance.HideUI(UIKeyType.HudParty);
        UIManager.Instance.HideUI(UIKeyType.HudQuest);
        UIManager.Instance.HideUI(UIKeyType.HudSkill);
        UIManager.Instance.HideUI(UIKeyType.OffscreenThreatIndicator);

        if (_playerCombat == null)
        {
            return;
        }

        _playerCombat.OnChangeCombatState -= OnPlayerCombatStateChanged;
        _playerCombat = null;
        _playerActor = null;
        _hudPlayerInfo = null;
    }

    protected override void RegisterInputEvents()
    {
        InputManager.Instance.RegisterInputEvent(InputMapNames.UI, UIAction.Inventory,
            null, OnPerformedInventory, null, null, null, InputLayer.Level_0);
       
        InputManager.Instance.RegisterInputEvent(InputMapNames.UI, UIAction.Map,
            null, OnPerformedMap, null, null, null, InputLayer.Level_0);
        
        InputManager.Instance.RegisterInputEvent(InputMapNames.UI, UIAction.Party,
            null, OnPerformedParty, null, null, null, InputLayer.Level_0);
        
        InputManager.Instance.RegisterInputEvent(InputMapNames.UI, UIAction.MenuPanel,
            null, OnPerformedMenuPanel, null, null, null, InputLayer.Level_0);
    }

    protected override void UnRegisterInputEvents()
    {
        InputManager.Instance.UnRegisterInputEvent(InputMapNames.UI, UIAction.Inventory, null, OnPerformedInventory,null);
        InputManager.Instance.UnRegisterInputEvent(InputMapNames.UI, UIAction.Map, null, OnPerformedMap,null);
        InputManager.Instance.UnRegisterInputEvent(InputMapNames.UI, UIAction.Party, null, OnPerformedParty, null);
        InputManager.Instance.UnRegisterInputEvent(InputMapNames.UI, UIAction.MenuPanel, null, OnPerformedMenuPanel, null);
    }

    #endregion

    #region InputCallback

    private void ToggleMap()
    {
        var mapObj = UIManager.Instance.GetActiveUI(UIKeyType.Map);
        var map    = mapObj?.GetComponent<UI_Map>();
        if (map != null && map.IsVisible)
            UIManager.Instance.HideUI(UIKeyType.Map);
        else
            UIManager.Instance.ShowUI(UIKeyType.Map);
    }

    private void OnPerformedInventory(InputAction.CallbackContext obj)
    {
        UI_Inventory inventory = UIManager.Instance.GetActiveUI(UIKeyType.Inventory)?.GetComponent<UI_Inventory>();
        if (inventory == null || inventory.IsVisible == false)
        {
            UIManager.Instance.ShowUI(UIKeyType.Inventory);
        }
        else
        {
            UIManager.Instance.HideUI(UIKeyType.Inventory);
        }
    }


    private void OnPerformedMap(InputAction.CallbackContext obj)
    {
        ToggleMap();
    }
    
    private void OnPerformedParty(InputAction.CallbackContext obj)
    {
        UI_PartyMenu party = UIManager.Instance.GetActiveUI(UIKeyType.Party)?.GetComponent<UI_PartyMenu>();
        if (party == null || party.IsVisible == false)
        {
            UIManager.Instance.ShowUI(UIKeyType.Party);
        }
        else
        {
            UIManager.Instance.HideUI(UIKeyType.Party);
        }
    }
    
    private void OnPerformedMenuPanel(InputAction.CallbackContext obj)
    {
        OnClickedMenuButton();
    }
    

    private void OnClickedMenuButton()
    {
        UI_MenuPanel party = UIManager.Instance.GetActiveUI(UIKeyType.MenuPanel)?.GetComponent<UI_MenuPanel>();
        if (party == null || party.IsVisible == false)
        {
            UIManager.Instance.ShowUI(UIKeyType.MenuPanel);
        }
        else
        {
            UIManager.Instance.HideUI(UIKeyType.MenuPanel);
        }
    }
    #endregion

    #region EventCallback

    private void OnPlayerCombatStateChanged(bool isInCombat)
    {
        if (_hudPlayerInfo != null)
        {
            _hudPlayerInfo.AnimationChange(isInCombat ? "Show" : "Hide");
            _hudPlayerInfo.SetIsInCombat(isInCombat);
        }
    }

    #endregion
}
