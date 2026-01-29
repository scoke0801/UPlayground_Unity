using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UPlayGround.Data.Enum;
using UPlayGround.Data.Event;
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
     
    private void Awake()
    {
        _EquipSword.onClick.AddListener(OnClickedEquipSword);
        _EquipShield.onClick.AddListener(OnClickedEquipShield);
        _EquipGreatSword.onClick.AddListener(OnClickedEquipGreatSword);
        _EquipStaff.onClick.AddListener(OnClickedEquipStaff);
        _EquipBow.onClick.AddListener(OnClickedEquipBow);
        _EquipArrow.onClick.AddListener(OnClickedEquipArrow);
    }

    protected override void OnShow()
    {
    }

    #region ButtonCallback
    private void OnClickedEquipArrow()
    {
        PlayerEquipChangeEvent eventData = new PlayerEquipChangeEvent()
        {
            weaponKey =  "Arrow_1",
            weaponType = WeaponType.Arrow
        };
        
        EventManager.Instance.Send(PlayerEvent.ChangeWeapon, eventData);
    }

    private void OnClickedEquipStaff()
    {        
        PlayerEquipChangeEvent eventData = new PlayerEquipChangeEvent()
        {
            weaponKey =  "Staff_1",
            weaponType = WeaponType.Staff
        };
        
        EventManager.Instance.Send(PlayerEvent.ChangeWeapon, eventData);
    }

    private void OnClickedEquipBow()
    {
        PlayerEquipChangeEvent eventData = new PlayerEquipChangeEvent()
        {
            weaponKey =  "Bow_1",
            weaponType = WeaponType.Bow
        };
        
        EventManager.Instance.Send(PlayerEvent.ChangeWeapon, eventData);
    }

    private void OnClickedEquipGreatSword()
    {
        PlayerEquipChangeEvent eventData = new PlayerEquipChangeEvent()
        {
            weaponKey =  "GreatSword_1",
            weaponType = WeaponType.GreatSword
        };
        
        EventManager.Instance.Send(PlayerEvent.ChangeWeapon, eventData);
    }

    private void OnClickedEquipShield()
    {
        PlayerEquipChangeEvent eventData = new PlayerEquipChangeEvent()
        {
            weaponKey =  "Shield_1",
            weaponType = WeaponType.Shield
        };
        
        EventManager.Instance.Send(PlayerEvent.ChangeWeapon, eventData);
    }

    private void OnClickedEquipSword()
    {
        PlayerEquipChangeEvent eventData = new PlayerEquipChangeEvent()
        {
            weaponKey =  "Sword_1",
            weaponType = WeaponType.Sword
        };
        
        EventManager.Instance.Send(PlayerEvent.ChangeWeapon, eventData);
    }
    #endregion
}
