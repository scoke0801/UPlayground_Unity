using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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
    }

    private void OnClickedEquipStaff()
    {
    }

    private void OnClickedEquipBow()
    {
    }

    private void OnClickedEquipGreatSword()
    {
    }

    private void OnClickedEquipShield()
    {
    }

    private void OnClickedEquipSword()
    {
    }
    #endregion
}
