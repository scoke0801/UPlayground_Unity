
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Components;
using UPlayGround.InputDefine;
using UPlayGround.Data.Path;
using UPlayGround.Data.UI;
using UPlayGround.Manager;
using UPlayGround.UI.Guidance;
using UPlayGround.UI.InputPrompt;

namespace UPlayGround.UI
{
    class UI_HUD_GamePlay : UI_Base
    {
        // 매니저 참조 캐싱 — 반복 Instance 조회(락 경합) 방지, 파괴 시 fake-null로 재조회
        private IUIRuntimeService _cachedUIManager;
        private IUIRuntimeService UIMgr => _cachedUIManager != null ? _cachedUIManager : (_cachedUIManager = UISvc.UI);


        private const string HudQuickSlotKey = "HudQuickSlot";
        private const string HudWorldMarkerKey = "HudWorldMarker";
        private const string HudBreathWarningKey = "HudBreathWarning";

        [SerializeField] Button _menuButton;

        [Tooltip("퀘스트 길안내 연출 프로필. 비워 두면 길안내 입력을 받지 않는다.")]
        [SerializeField] private GuideProfileSO _guideProfile;

        [Tooltip("탐험 중 미니맵 HUD를 표시한다. 기본은 비표시.")]
        [SerializeField] private bool _showMinimapHud = false;

        private GuidanceController _guidance;

        private IUIInventoryService _quickSlotInventory;
        private PlayerActor _playerActor;

        private PlayerCombat _playerCombat;

        private UI_HUD_PlayerInfo _hudPlayerInfo;
        private UI_HUD_Party _hudParty;
        private UI_HUD_Quest _hudQuest;
        private UI_HUD_Skill _hudSkill;
        private UPlayGround.UI.HUD.Notification.UI_Scene_Notification _notification;

        #region UI_Base

        protected override void Awake()
        {
            base.Awake();
            _menuButton.onClick.AddListener(OnClickedMenuButton);
        }

        protected override void OnShow()
        {
            _hudPlayerInfo = UIMgr.ShowUI(UIKeyType.HudPlayerInfo)?.GetComponent<UI_HUD_PlayerInfo>();
            bool showExploration = UISvc.Scene?.ShowExplorationHud ?? true;
            // 미니맵 HUD는 기본 비표시다. 목표 방향은 월드 마커와 길안내 연출이 맡고, 목표 좌표 소스인
            // MinimapMarkerRegistry는 HUD와 무관하게 유지된다.
            if (showExploration && _showMinimapHud) UIMgr.ShowUI(UIKeyType.Minimap);
            else UIMgr.HideUI(UIKeyType.Minimap);

            _hudParty = UIMgr.ShowUI(UIKeyType.HudParty)?.GetComponent<UI_HUD_Party>();

            _hudQuest = UIMgr.ShowUI(UIKeyType.HudQuest)?.GetComponent<UI_HUD_Quest>();

            _hudSkill = UIMgr.ShowUI(UIKeyType.HudSkill)?.GetComponent<UI_HUD_Skill>();

            UIMgr.HideUI(HudQuickSlotKey);
            _quickSlotInventory = UISvc.Inventory;
            if (_quickSlotInventory != null) _quickSlotInventory.OnInventoryChanged += EnsureQuickSlotAssignment;
            EnsureQuickSlotAssignment();

            if (UIMgr.GetUIPrefabEntry(UIKeyType.Notification.ToKey()) != null)
            {
                _notification = UIMgr.ShowUI(UIKeyType.Notification, CanvasLayer.HUD)
                    ?.GetComponent<UPlayGround.UI.HUD.Notification.UI_Scene_Notification>();
            }

            UIMgr.ShowUI(UIKeyType.OffscreenThreatIndicator);

            // 잠수 경고 화면 효과 (UIKeyType은 자동 생성 enum이라 문자열 키 사용. DB 미등록 시 생략)
            if (UIMgr.GetUIPrefabEntry(HudBreathWarningKey) != null)
                UIMgr.ShowUI(HudBreathWarningKey, CanvasLayer.HUD);

            // 인게임 월드 마커 HUD (UIKeyType은 자동 생성 enum이라 문자열 키 사용. DB 미등록 시 생략)
            if (UIMgr.GetUIPrefabEntry(HudWorldMarkerKey) != null)
            {
                UIMgr.ShowUI(HudWorldMarkerKey, CanvasLayer.HUD);
            }

            if (_guideProfile != null && _guidance == null)
            {
                _guidance = gameObject.AddComponent<GuidanceController>();
                _guidance.Initialize(_guideProfile);
            }

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
            if (_quickSlotInventory != null) _quickSlotInventory.OnInventoryChanged -= EnsureQuickSlotAssignment;
            _quickSlotInventory = null;
            var uiManager = UIMgr;
            if (uiManager != null)
            {
                uiManager.HideUI(UIKeyType.HudPlayerInfo);
                uiManager.HideUI(UIKeyType.Minimap);
                uiManager.HideUI(UIKeyType.HudParty);
                uiManager.HideUI(UIKeyType.HudQuest);
                uiManager.HideUI(UIKeyType.HudSkill);
                uiManager.HideUI(HudQuickSlotKey);
                uiManager.HideUI(UIKeyType.Notification);
                uiManager.HideUI(UIKeyType.OffscreenThreatIndicator);
                uiManager.HideUI(HudWorldMarkerKey);
                uiManager.HideUI(HudBreathWarningKey);
            }

            if (_guidance != null)
                Destroy(_guidance);
            _guidance = null;

            if (_playerCombat != null)
                _playerCombat.OnChangeCombatState -= OnPlayerCombatStateChanged;
            _playerCombat = null;
            _playerActor = null;
            _hudPlayerInfo = null;
            _hudParty = null;
            _hudQuest = null;
            _hudSkill = null;
            _notification = null;
        }

        protected override void RegisterInputEvents()
        {
            var inputManager = Svc.Input;
            if (inputManager == null)
                return;

            inputManager.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Up,
                null, OnQuickSlotUp, null, null, null, InputLayer.Level_0);
            inputManager.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Right,
                null, OnQuickSlotRight, null, null, null, InputLayer.Level_0);
            inputManager.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Down,
                null, OnQuickSlotDown, null, null, null, InputLayer.Level_0);
            inputManager.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Left,
                null, OnQuickSlotLeft, null, null, null, InputLayer.Level_0);

            inputManager.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Guide,
                null, OnPerformedGuide, null, null, null, InputLayer.Level_0);

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

            inputManager.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Up, null, OnQuickSlotUp, null);
            inputManager.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Right, null, OnQuickSlotRight, null);
            inputManager.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Down, null, OnQuickSlotDown, null);
            inputManager.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.QuickSlot_Left, null, OnQuickSlotLeft, null);

            inputManager.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Guide, null, OnPerformedGuide, null);
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

        private void EnsureQuickSlotAssignment() => UIQuickSlotAssignments.TryAssignStartingPotion(_quickSlotInventory);
        private void OnQuickSlotUp(InputAction.CallbackContext context) => UseQuickSlot(0);
        private void OnQuickSlotRight(InputAction.CallbackContext context) => UseQuickSlot(1);
        private void OnQuickSlotDown(InputAction.CallbackContext context) => UseQuickSlot(2);
        private void OnQuickSlotLeft(InputAction.CallbackContext context) => UseQuickSlot(3);

        private void UseQuickSlot(int index)
        {
            int itemId = UIQuickSlotAssignments.GetItemId(index);
            if (itemId > 0) _quickSlotInventory?.TryUseItem(itemId);
        }

        private void ToggleMap()
        {
            var mapObj = UIMgr.GetActiveUI(UIKeyType.Map);
            var map    = mapObj?.GetComponent<UI_Scene_Map>();
            if (map != null && map.IsVisible)
                UIMgr.HideUI(UIKeyType.Map);
            else
                UIMgr.ShowUI(UIKeyType.Map);
        }

        private void OnPerformedInventory(InputAction.CallbackContext obj)
        {
            UI_Scene_Inventory inventory = UIMgr.GetActiveUI(UIKeyType.Inventory)?.GetComponent<UI_Scene_Inventory>();
            if (inventory == null || inventory.IsVisible == false)
            {
                UIMgr.ShowUI(UIKeyType.Inventory);
            }
            else
            {
                UIMgr.HideUI(UIKeyType.Inventory);
            }
        }


        // 전투 중에는 나비·퀘스트 표시 모두 동작하지 않는다. 퀘스트 표시는 길 유무와 무관하게 수행한다.
        private void OnPerformedGuide(InputAction.CallbackContext obj)
        {
            if (_guidance == null || _guidance.IsBlockedByCombat())
                return;

            _hudQuest?.PeekQuestInfo();
            _guidance.TryRequestGuide();
        }

        private void OnPerformedMap(InputAction.CallbackContext obj)
        {
            ToggleMap();
        }

        private void OnPerformedParty(InputAction.CallbackContext obj)
        {
            UI_Scene_PartyMenu party = UIMgr.GetActiveUI(UIKeyType.Party)?.GetComponent<UI_Scene_PartyMenu>();
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

            // GetActiveUI는 객체 존재만 반환한다(Hide 후에도 캐시에 남음). 다른 토글과 동일하게
            // 실제 가시성(IsVisible)으로 판단해야 한 번 닫은 뒤에도 다시 열 수 있다.
            var panelObj = mgr.GetActiveUI("DevCheatPanel");
            var panel = panelObj != null ? panelObj.GetComponentInChildren<UI_Base>(true) : null;
            if (panel != null && panel.IsVisible)
                mgr.HideUI("DevCheatPanel");
            else
                mgr.ShowUI("DevCheatPanel");
        }
    #endif

        private void OnClickedMenuButton()
        {
            UI_Scene_MenuPanel party = UIMgr.GetActiveUI(UIKeyType.MenuPanel)?.GetComponent<UI_Scene_MenuPanel>();
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
