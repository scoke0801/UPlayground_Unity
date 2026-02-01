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
public class UI_Inventory : UI_Base
{
    [SerializeField] private UI_InventorySlot _itemPanelPrefab;
    [SerializeField] private Transform _content;
    [SerializeField] private Image _imgWeightFill;
    [SerializeField] private TextMeshProUGUI _txtWeight;

    [Header("Slot Setting")]
    [SerializeField] private int _slotCountPerRow = 5;
    [SerializeField] private int _startRowCount = 10;
    
    [Header("Character Preview")]
    [SerializeField] private RawImage _characterPreview;
    [SerializeField] private UICharacterPreviewRenderer _previewRenderer;

    [Header("Equipment Slot")] 
    [SerializeField] private UI_InventorySlot _headSlot;
    [SerializeField] private UI_InventorySlot _chestSlot;
    [SerializeField] private UI_InventorySlot _pantSlot;
    [SerializeField] private UI_InventorySlot _shoesSlot;
    [SerializeField] private UI_InventorySlot _glovesSlot;
    
    [SerializeField] private UI_InventorySlot _leftHandSlot;
    [SerializeField] private UI_InventorySlot _rightHandSlot;
    
    
    private List<UI_InventorySlot> _uiSlots = new List<UI_InventorySlot>();
    private int itemMaximumValue = 50;

    public GameObject _itemClickTap;
    
    private void Awake()
    {
        Init();
        
        _headSlot.SetParent(this);
        _chestSlot.SetParent(this);
        _pantSlot.SetParent(this);
        _shoesSlot.SetParent(this);
        _glovesSlot.SetParent(this);
        _leftHandSlot.SetParent(this);
        _rightHandSlot.SetParent(this);
        
        // RenderTexture 연결
        if (_previewRenderer != null && _characterPreview != null)
        {
            _characterPreview.texture = _previewRenderer.GetRenderTexture();
        }
    }

    protected override void OnShow()
    {
        RefreshDictItem();
        SetInventory();
        
        // EventManager.Instance.Subscribe<PlayerEvent, PlayerEquipChangeEvent>(
        //     PlayerEvent.EquipItem, 
        //     OnEquipItem
        // );
        // 캐릭터 프리뷰 활성화
        if (_previewRenderer != null)
        {
            _previewRenderer.ShowPreview();
        }
    }

    public override bool PerformBackFunction()
    {
        // ESC 키 입력 시 닫는다.
        Hide();
        return false;
    }

    protected override void OnHide()
    {
        // EventManager.Instance.Unsubscribe<PlayerEvent, PlayerEquipChangeEvent>(
        //     PlayerEvent.EquipItem, 
        //     OnEquipItem
        // );
        // 캐릭터 프리뷰 비활성화
        if (_previewRenderer != null)
        {
            _previewRenderer.HidePreview();
        }
    }
    
    public void SetInventory()
    {
        foreach (var t in _uiSlots)
        {
            t.RefreshUI();
        }

        _imgWeightFill.fillAmount = InventoryManager.Instance.GetTotalWeight() / InventoryManager.Instance.MaxWeight;
        _txtWeight.text =
            $"({InventoryManager.Instance.GetTotalWeight():0.0}/{InventoryManager.Instance.MaxWeight:0.0})";
    }

    public void SetItemClickAnimation(UI_InventorySlot slot)
    {
        _itemClickTap.gameObject.SetActive(true);
        _itemClickTap.transform.SetParent(slot.transform);
        
        _itemClickTap.transform.localPosition = Vector3.zero;
        //_itemClickTap.GetComponent<RectTransform>().anchoredPosition = slot.GetComponent<RectTransform>().anchoredPosition;
    }

    public void OnSlotPointerExit()
    {
        _itemClickTap.gameObject.SetActive(false);
    }
    
    private void Init()
    {
        AddSlot(_startRowCount);
    }

    private void RefreshDictItem()
    {
        int value = 0;
        foreach (var item in InventoryManager.Instance.ItemDict)
        {
            if (_uiSlots.Count <= value + 1)
            {
                AddSlot(1);
            }
            _uiSlots[value++].Init(item.Value.data, item.Value.count);
        }
    }

    private void AddSlot(int count)
    {
        for (int i = 0; i < count; ++i)
        {
            for (int j = 0; j < _slotCountPerRow; ++j)
            {
                var go = Instantiate(_itemPanelPrefab, _content);
                _uiSlots.Add(go);
                go.SetParent(this);
            }
        }
    }

    private void OnEquipItem(PlayerEquipChangeEvent eventData)
    {
        UI_InventorySlot targetSlot = null;
        switch (eventData.equipPosition)
        {
            case EquipPosition.LeftHand: targetSlot = _leftHandSlot; break;
            case EquipPosition.RightHand: targetSlot = _rightHandSlot; break;
            case EquipPosition.Head: targetSlot = _headSlot; break;
            case EquipPosition.Chest: targetSlot = _chestSlot; break;
            case EquipPosition.Pants: targetSlot = _pantSlot; break;
            case EquipPosition.Gloves: targetSlot = _glovesSlot; break;
            case EquipPosition.Shoes: targetSlot = _shoesSlot; break;
        }

        if (targetSlot == null)
        {
            return;
        }

        ItemSO itemData = ItemManager.Instance.GetItemData(eventData.itemKey);
        if (itemData == null)
        {
            return;
        }
        targetSlot.Init(itemData, 1);
        targetSlot.RefreshUI();
    }

    public void RefreshPreviewModel()
    {
        if (_previewRenderer != null)
        {
            _previewRenderer.ShowPreview();
        }
    }
}
