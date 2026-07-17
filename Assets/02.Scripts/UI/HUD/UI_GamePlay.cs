
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Components;
using UPlayGround.InputDefine;
using UPlayGround.Data.Path;
using UPlayGround.Manager;
using UPlayGround.UI.InputPrompt;

namespace UPlayGround.UI
{
    class UI_GamePlay : UI_Base
    {
        // 매니저 참조 캐싱 — 반복 Instance 조회(락 경합) 방지, 파괴 시 fake-null로 재조회
        private IUIRuntimeService _cachedUIManager;
        private IUIRuntimeService UIMgr => _cachedUIManager != null ? _cachedUIManager : (_cachedUIManager = UISvc.UI);


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
            _hudPlayerInfo = UIMgr.ShowUI(UIKeyType.HudPlayerInfo)?.GetComponent<UI_HudPlayerInfo>();
            UIMgr.ShowUI(UIKeyType.Minimap);

            _hudParty = UIMgr.ShowUI(UIKeyType.HudParty)?.GetComponent<UI_HudParty>();

            _hudQuest = UIMgr.ShowUI(UIKeyType.HudQuest)?.GetComponent<UI_HudQuest>();

            _hudSkill = UIMgr.ShowUI(UIKeyType.HudSkill)?.GetComponent<UI_HudSkill>();

            if (UIMgr.GetUIPrefabEntry(UIKeyType.Notification.ToKey()) != null)
            {
                _notification = UIMgr.ShowUI(UIKeyType.Notification, CanvasLayer.HUD)
                    ?.GetComponent<UPlayGround.UI.HUD.Notification.UI_Notification>();
            }

            // 인게임 시계 (UIKeyType은 자동 생성 enum이라 문자열 키 사용. DB 미등록 시 생략)
            if (UIMgr.GetUIPrefabEntry(HudWorldClockKey) != null)
            {
                UIMgr.ShowUI(HudWorldClockKey, CanvasLayer.HUD);
            }

            UIMgr.ShowUI(UIKeyType.OffscreenThreatIndicator);

            if (UISvc.Actors != null)
            {
                _playerActor = UISvc.Actors.Player;
                _playerCombat = _playerActor?.GetCombat();
                if (_playerCombat != null)
                {
                    _playerCombat.OnChangeCombatState += OnPlayerCombatStateChanged;
                }
            }
        }

        protected override void OnHide()
        {
            var uiManager = UIMgr;
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
            var inputManager = Svc.Input;
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
            var inputManager = Svc.Input;
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
            var mapObj = UIMgr.GetActiveUI(UIKeyType.Map);
            var map    = mapObj?.GetComponent<UI_Map>();
            if (map != null && map.IsVisible)
                UIMgr.HideUI(UIKeyType.Map);
            else
                UIMgr.ShowUI(UIKeyType.Map);
        }

        private void OnPerformedInventory(InputAction.CallbackContext obj)
        {
            UI_Inventory inventory = UIMgr.GetActiveUI(UIKeyType.Inventory)?.GetComponent<UI_Inventory>();
            if (inventory == null || inventory.IsVisible == false)
            {
                UIMgr.ShowUI(UIKeyType.Inventory);
            }
            else
            {
                UIMgr.HideUI(UIKeyType.Inventory);
            }
        }


        private void OnPerformedMap(InputAction.CallbackContext obj)
        {
            ToggleMap();
        }

        private void OnPerformedParty(InputAction.CallbackContext obj)
        {
            UI_PartyMenu party = UIMgr.GetActiveUI(UIKeyType.Party)?.GetComponent<UI_PartyMenu>();
            if (party == null || party.IsVisible == false)
            {
                UIMgr.ShowUI(UIKeyType.Party);
            }
            else
            {
                UIMgr.HideUI(UIKeyType.Party);
            }
        }

        private void OnPerformedMenuPanel(InputAction.CallbackContext obj)
        {
            OnClickedMenuButton();
        }

    #if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnPerformedCheatPanel(InputAction.CallbackContext obj)
        {
            var mgr = UIMgr;
            if (mgr == null)
                return;

            var panel = mgr.GetActiveUI("DevCheatPanel");
            if (panel != null)
                mgr.HideUI("DevCheatPanel");
            else
                mgr.ShowUI("DevCheatPanel");
        }
    #endif

        private void OnClickedMenuButton()
        {
            UI_MenuPanel party = UIMgr.GetActiveUI(UIKeyType.MenuPanel)?.GetComponent<UI_MenuPanel>();
            if (party == null || party.IsVisible == false)
            {
                UIMgr.ShowUI(UIKeyType.MenuPanel);
            }
            else
            {
                UIMgr.HideUI(UIKeyType.MenuPanel);
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
}
