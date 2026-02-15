using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UPlayGround.Data.Enum;
using UPlayGround.Data.Event;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

/// <summary>
/// 인벤토리 UI
/// </summary>
public class UI_EquipInventory : UI_Base
{
    [SerializeField] private Button _EquipSword;
    [SerializeField] private Button _EquipShield;
    [SerializeField] private Button _EquipGreatSword;
    [SerializeField] private Button _EquipStaff;
    [SerializeField] private Button _EquipBow;
    [SerializeField] private Button _EquipArrow;
     
    protected override void Awake()
    {
        base.Awake();
        
        _EquipSword.onClick.AddListener(OnClickedEquipSword);
        _EquipShield.onClick.AddListener(OnClickedEquipShield);
        _EquipGreatSword.onClick.AddListener(OnClickedEquipGreatSword);
        _EquipStaff.onClick.AddListener(OnClickedEquipStaff);
        _EquipBow.onClick.AddListener(OnClickedEquipBow);
        _EquipArrow.onClick.AddListener(OnClickedEquipArrow);
    }

    protected override void OnShow()
    {
        InputManager.Instance.SetInputLayer(InputLayer.Level_1);
    }

    protected override void OnHide()
    {
        InputManager.Instance.SetInputLayer(InputLayer.None);
    }

    public override bool PerformBackFunction()
    {
        // ESC 키 입력 시 닫는다.
        Hide();
        return false;
    }
    #region ButtonCallback
    private void OnClickedEquipArrow()
    {
        PlayerEquipChangeEvent eventData = new PlayerEquipChangeEvent()
        {
            weaponType = WeaponType.Arrow,
            equipPosition = EquipPosition.LeftHand,
        };
        
        EventManager.Instance.Send(PlayerEvent.ChangeWeapon, eventData);
    }

    private void OnClickedEquipStaff()
    {        
        PlayerEquipChangeEvent eventData = new PlayerEquipChangeEvent()
        {
            weaponType = WeaponType.Staff,
            equipPosition = EquipPosition.RightHand,
            
        };
        
        EventManager.Instance.Send(PlayerEvent.ChangeWeapon, eventData);
    }

    private void OnClickedEquipBow()
    {
        PlayerEquipChangeEvent eventData = new PlayerEquipChangeEvent()
        {
            weaponType = WeaponType.Bow,
            equipPosition = EquipPosition.RightHand,
        };
        
        EventManager.Instance.Send(PlayerEvent.ChangeWeapon, eventData);
    }

    private void OnClickedEquipGreatSword()
    {
        PlayerEquipChangeEvent eventData = new PlayerEquipChangeEvent()
        {
            weaponType = WeaponType.GreatSword,
            equipPosition = EquipPosition.RightHand,
        };
        
        EventManager.Instance.Send(PlayerEvent.ChangeWeapon, eventData);
    }

    private void OnClickedEquipShield()
    {
        PlayerEquipChangeEvent eventData = new PlayerEquipChangeEvent()
        {
            weaponType = WeaponType.Shield,
            equipPosition = EquipPosition.LeftHand,
        };
        
        EventManager.Instance.Send(PlayerEvent.ChangeWeapon, eventData);
    }

    private void OnClickedEquipSword()
    {
        PlayerEquipChangeEvent eventData = new PlayerEquipChangeEvent()
        {
            weaponType = WeaponType.Sword,
            equipPosition = EquipPosition.RightHand,
        };
        
        EventManager.Instance.Send(PlayerEvent.ChangeWeapon, eventData);
    }
    #endregion
}
