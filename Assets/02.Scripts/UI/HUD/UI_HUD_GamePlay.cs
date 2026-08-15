
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
    class UI_HUD_GamePlay : UI_Base
    {
        // 매니저 참조 캐싱 — 반복 Instance 조회(락 경합) 방지, 파괴 시 fake-null로 재조회
        private IUIRuntimeService _cachedUIManager;
        private IUIRuntimeService UIMgr => _cachedUIManager != null ? _cachedUIManager : (_cachedUIManager = UISvc.UI);


        private const string HudWorldClockKey = "HudWorldClock";
        private const string HudQuickSlotKey = "HudQuickSlot";
        private const string HudWorldMarkerKey = "HudWorldMarker";

        [SerializeField] Button _menuButton;

        private PlayerActor _playerActor;

        private PlayerCombat _playerCombat;

        private UI_HUD_PlayerInfo _hudPlayerInfo;
        private UI_HUD_Party _hudParty;
        private UI_HUD_Quest _hudQuest;
        private UI_HUD_Skill _hudSkill;
        private UI_HUD_QuickSlot _hudQuickSlot;
        private UI_HUD_WorldClock _hudWorldClock;
        private UPlayGround.UI.HUD.Notification.UI_Scene_Notification _notification;
        private int _hudContextVersion;

        #region UI_Base

        protected override void Awake()
        {
            base.Awake();
            _menuButton.onClick.AddListener(OnClickedMenuButton);
        }

        protected override void OnShow()
        {
            _hudPlayerInfo = UIMgr.ShowUI(UIKeyType.HudPlayerInfo)?.GetComponent<UI_HUD_PlayerInfo>();
            UIMgr.ShowUI(UIKeyType.Minimap);

            _hudParty = UIMgr.ShowUI(UIKeyType.HudParty)?.GetComponent<UI_HUD_Party>();

            _hudQuest = UIMgr.ShowUI(UIKeyType.HudQuest)?.GetComponent<UI_HUD_Quest>();

            _hudSkill = UIMgr.ShowUI(UIKeyType.HudSkill)?.GetComponent<UI_HUD_Skill>();

            if (UIMgr.GetUIPrefabEntry(HudQuickSlotKey) != null)
            {
                _hudQuickSlot = UIMgr.ShowUI(HudQuickSlotKey, CanvasLayer.HUD)
                    ?.GetComponent<UI_HUD_QuickSlot>();
            }

            if (UIMgr.GetUIPrefabEntry(UIKeyType.Notification.ToKey()) != null)
            {
                _notification = UIMgr.ShowUI(UIKeyType.Notification, CanvasLayer.HUD)
                    ?.GetComponent<UPlayGround.UI.HUD.Notification.UI_Scene_Notification>();
            }

            // 인게임 시계 (UIKeyType은 자동 생성 enum이라 문자열 키 사용. DB 미등록 시 생략)
            if (UIMgr.GetUIPrefabEntry(HudWorldClockKey) != null)
            {
                _hudWorldClock = UIMgr.ShowUI(HudWorldClockKey, CanvasLayer.HUD)
                    ?.GetComponent<UI_HUD_WorldClock>();
            }

            UIMgr.ShowUI(UIKeyType.OffscreenThreatIndicator);

            // 인게임 월드 마커 HUD (UIKeyType은 자동 생성 enum이라 문자열 키 사용. DB 미등록 시 생략)
            if (UIMgr.GetUIPrefabEntry(HudWorldMarkerKey) != null)
            {
                UIMgr.ShowUI(HudWorldMarkerKey, CanvasLayer.HUD);
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

            ApplyHudContext(_playerCombat?.IsInCombat ?? false, animate: false);
        }

        protected override void OnHide()
        {
            _hudContextVersion++;
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
                uiManager.HideUI(HudWorldClockKey);
            }

            if (_playerCombat != null)
                _playerCombat.OnChangeCombatState -= OnPlayerCombatStateChanged;
            _playerCombat = null;
            _playerActor = null;
            _hudPlayerInfo = null;
            _hudParty = null;
            _hudQuest = null;
            _hudSkill = null;
            _hudQuickSlot = null;
            _hudWorldClock = null;
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

            ApplyHudContext(isInCombat, animate: true);
        }

        /// <summary>
        /// 탐험 정보와 전투 정보를 같은 밀도로 겹치지 않는다.
        /// 전투 중에도 길찾기에 필요한 미니맵·마커는 남기고, 당장 읽지 않는
        /// 퀘스트 추적과 시계만 접어 시선 경쟁을 줄인다.
        /// </summary>
        private void ApplyHudContext(bool isInCombat, bool animate)
        {
            int version = ++_hudContextVersion;
            if (isInCombat)
            {
                HideExplorationHud(_hudQuest, UIKeyType.HudQuest.ToKey(), version, animate);
                HideExplorationHud(_hudWorldClock, HudWorldClockKey, version, animate);
                return;
            }

            _hudQuest = ShowExplorationHud(_hudQuest, UIKeyType.HudQuest.ToKey(), animate);
            if (UIMgr.GetUIPrefabEntry(HudWorldClockKey) != null)
                _hudWorldClock = ShowExplorationHud(_hudWorldClock, HudWorldClockKey, animate);
        }

        private T ShowExplorationHud<T>(T current, string key, bool animate) where T : UI_Base
        {
            if (current != null && current.IsVisible)
            {
                // 전투 진입 FadeOut이 끝나기 전에 전투가 해제되면 기존 코루틴을
                // 취소하고 현재 알파에서 복귀시킨다. IsVisible만으로는 페이드 상태를 알 수 없다.
                current.FadeTo(1f, animate ? 0.20f : 0f);
                return current;
            }

            T ui = UIMgr.ShowUI(key, CanvasLayer.HUD)?.GetComponent<T>();
            if (animate)
                ui?.FadeIn(0.20f);
            return ui;
        }

        private void HideExplorationHud(
            UI_Base ui,
            string key,
            int version,
            bool animate)
        {
            if (ui == null || !ui.IsVisible)
                return;

            if (!animate)
            {
                UIMgr.HideUI(key);
                return;
            }

            ui.FadeOut(0.14f, () =>
            {
                if (version == _hudContextVersion)
                    UIMgr?.HideUI(key);
            });
        }

        #endregion
    }
}
