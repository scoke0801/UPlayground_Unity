
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

class UI_GamePlay : UI_Base
{
    
    #region UI_Base

    protected override void RegisterInputEvents()
    {
        InputManager.Instance.RegisterInputEvent(InputMapNames.UI, UIAction.Inventory,
            null, OnPerformedInventory, null, null, null, InputLayer.Level_0);
       
        InputManager.Instance.RegisterInputEvent(InputMapNames.UI, UIAction.EquipInventory,
            null, OnPerformedEquipInventory, null, null, null, InputLayer.Level_0); 
        
    }

    protected override void UnRegisterInputEvents()
    {
        InputManager.Instance.UnRegisterInputEvent(InputMapNames.UI, UIAction.Inventory,
            null, OnPerformedInventory,null);
        
        InputManager.Instance.UnRegisterInputEvent(InputMapNames.UI, UIAction.EquipInventory,
            null, OnPerformedEquipInventory,null);
    }

    #endregion

    #region InputCallback

    private void OnPerformedInventory(InputAction.CallbackContext obj)
    {
        UI_Inventory inventory = UIManager.Instance.GetActiveUI("Inventory")?.GetComponent<UI_Inventory>();
        if (inventory == null || inventory.IsVisible == false)
        {
            UIManager.Instance.ShowUI("Inventory");
        }
        else
        {
            UIManager.Instance.HideUI("Inventory");
        }
    }

    private void OnPerformedEquipInventory(InputAction.CallbackContext obj)
    {      
        UI_EquipInventory inventory = UIManager.Instance.GetActiveUI("EquipInventory")?.GetComponent<UI_EquipInventory>();
        if (inventory == null || inventory.IsVisible == false)
        {
            UIManager.Instance.ShowUI("EquipInventory");
        }
        else
        {
            UIManager.Instance.HideUI("EquipInventory");
        }
    }

    #endregion
}