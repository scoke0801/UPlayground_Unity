using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 인벤토리 UI
/// </summary>
public class UI_Inventory : UI_Base
{
    [SerializeField] private UI_InventorySlot _itemPanelPrefab;
    [SerializeField] private Transform _content;
    
    private List<UI_InventorySlot> _uiSlots = new List<UI_InventorySlot>();
    private int itemMaximumValue = 50;
    
    private void Awake()
    {
    }

    private void Start()
    {
        Init();
    }

    private void OnEnable()
    {
        
    }
    
    private void OnDisable()
    {
    }

    protected override void OnShow()
    {
        RefreshDictItem();
        SetInventory();
    } 

    public void SetInventory()
    {
        for (int i = 0; i < _uiSlots.Count; ++i)
        {
            _uiSlots[i].RefreshUI();
        }
    }
    private void Init()
    {
        for (int i = 0; i < itemMaximumValue; ++i)
        {
            var go = Instantiate(_itemPanelPrefab, _content);
            _uiSlots.Add(go);
        }
    }

    private void RefreshDictItem()
    {
        int value = 0;
        foreach (var item in InventoryManager.Instance.ItemDict)
        {
            _uiSlots[value++].Init(item.Value);
        }
    }
}
