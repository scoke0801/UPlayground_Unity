
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Component;
using UPlayGround.InputDefine;
using UPlayGround.Data.Path;
using UPlayGround.Manager;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UPlayGround.UI.DevCheat;
#endif
using UPlayGround.UI.InputPrompt;

class UI_GamePlay : UI_Base
{
    private const string HudWorldClockKey = "HudWorldClock";

    [SerializeField] Button _menuButton;

    private PlayerActor _playerActor;

    private PlayerCombat _playerCombat;

    private UI_HudPlayerInfo _hudPlayerInfo;
    private UI_HudParty _hudParty;
    private UI_HudQuest _hudQuest;
    private UI_HudSkill _hudSkill;
    private UPlayGround.UI.HUD.Notification.UI_Notification _notification;
    
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

        if (UIManager.Instance.GetUIPrefabEntry(UIKeyType.Notification.ToKey()) != null)
        {
            _notification = UIManager.Instance.ShowUI(UIKeyType.Notification, CanvasLayer.HUD)
                ?.GetComponent<UPlayGround.UI.HUD.Notification.UI_Notification>();
        }

        // 인게임 시계 (UIKeyType은 자동 생성 enum이라 문자열 키 사용. DB 미등록 시 생략)
        if (UIManager.Instance.GetUIPrefabEntry(HudWorldClockKey) != null)
        {
            UIManager.Instance.ShowUI(HudWorldClockKey, CanvasLayer.HUD);
        }

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
        var uiManager = UIManager.Instance;
        if (uiManager != null)
        {
            uiManager.HideUI(UIKeyType.HudPlayerInfo);
            uiManager.HideUI(UIKeyType.Minimap);
            uiManager.HideUI(UIKeyType.HudParty);
            uiManager.HideUI(UIKeyType.HudQuest);
            uiManager.HideUI(UIKeyType.HudSkill);
            uiManager.HideUI(UIKeyType.Notification);
            uiManager.HideUI(UIKeyType.OffscreenThreatIndicator);
            uiManager.HideUI(HudWorldClockKey);
        }

        if (_playerCombat == null)
        {
            return;
        }

        _playerCombat.OnChangeCombatState -= OnPlayerCombatStateChanged;
        _playerCombat = null;
        _playerActor = null;
        _hudPlayerInfo = null;
        _notification = null;
    }

    protected override void RegisterInputEvents()
    {
        var inputManager = InputManager.Instance;
        if (inputManager == null)
            return;

        inputManager.RegisterInputEvent(InputMapNames.UI, UIAction.Inventory,
            null, OnPerformedInventory, null, null, null, InputLayer.Level_0);
       
        inputManager.RegisterInputEvent(InputMapNames.UI, UIAction.Map,
            null, OnPerformedMap, null, null, null, InputLayer.Level_0);
        
        inputManager.RegisterInputEvent(InputMapNames.UI, UIAction.Party,
            null, OnPerformedParty, null, null, null, InputLayer.Level_0);
        
        inputManager.RegisterInputEvent(InputMapNames.UI, UIAction.MenuPanel,
            null, OnPerformedMenuPanel, null, null, null, InputLayer.Level_0);
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        inputManager.RegisterInputEvent(InputMapNames.UI, UIAction.CheatPanel,
            null, OnPerformedCheatPanel, null, null, null, InputLayer.Level_0);
#endif
    }

    protected override void UnRegisterInputEvents()
    {
        var inputManager = InputManager.Instance;
        if (inputManager == null)
            return;

        inputManager.UnRegisterInputEvent(InputMapNames.UI, UIAction.Inventory, null, OnPerformedInventory, null);
        inputManager.UnRegisterInputEvent(InputMapNames.UI, UIAction.Map, null, OnPerformedMap, null);
        inputManager.UnRegisterInputEvent(InputMapNames.UI, UIAction.Party, null, OnPerformedParty, null);
        inputManager.UnRegisterInputEvent(InputMapNames.UI, UIAction.MenuPanel, null, OnPerformedMenuPanel, null);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        inputManager.UnRegisterInputEvent(InputMapNames.UI, UIAction.CheatPanel, null, OnPerformedCheatPanel, null);
#endif
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
    
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void OnPerformedCheatPanel(InputAction.CallbackContext obj)
    {
        var mgr = UIManager.Instance;
        if (mgr == null)
            return;

        var panel = mgr.GetUI<UI_DevCheatPanel>();
        if (panel != null && panel.IsVisible)
            panel.Hide();
        else
            mgr.ShowUI("DevCheatPanel");
    }
#endif
    
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
