
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround;
using UPlayGround.Component;
using UPlayGround.InputDefine;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

class UI_GamePlay : UI_Base
{
    private PlayerActor _playerActor;

    private PlayerCombat _playerCombat;
    
    private UI_HudPlayerInfo _hudPlayerInfo;
    
    #region UI_Base

    protected override void OnShow()
    {
        _hudPlayerInfo = UIManager.Instance.ShowUI(UIKeyType.HudPlayerInfo)?.GetComponent<UI_HudPlayerInfo>();
        UIManager.Instance.ShowUI(UIKeyType.Minimap);

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
        UIManager.Instance.HideUI(UIKeyType.Minimap);

        if (_playerCombat == null)
        {
            return;
        }

        _playerActor.GetCombat().OnChangeCombatState -= OnPlayerCombatStateChanged;
    }

    protected override void RegisterInputEvents()
    {
        InputManager.Instance.RegisterInputEvent(InputMapNames.UI, UIAction.Inventory,
            null, OnPerformedInventory, null, null, null, InputLayer.Level_0);
       
    }

    protected override void UnRegisterInputEvents()
    {
        InputManager.Instance.UnRegisterInputEvent(InputMapNames.UI, UIAction.Inventory,
            null, OnPerformedInventory,null);
        
    }

    #endregion

    #region InputCallback

    private void OnPerformedInventory(InputAction.CallbackContext obj)
    {
        UI_Inventory inventory = UIManager.Instance.GetActiveUI("Inventory")?.GetComponent<UI_Inventory>();
        if (inventory == null || inventory.IsVisible == false)
        {
            UIManager.Instance.ShowUI(UIKeyType.Inventory);
        }
        else
        {
            UIManager.Instance.HideUI(UIKeyType.Inventory);
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