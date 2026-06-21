using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using Image = UnityEngine.UI.Image;

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
    
    [Header("Select Detail Panel")]
    [SerializeField] private GameObject _selectedItemPrefab;
    [SerializeField] private Image _selectedItemImage;
    [SerializeField] private TextMeshProUGUI _selectedItemCountText;
    [SerializeField] private TextMeshProUGUI _selectedItemNameText;
    [SerializeField] private TextMeshProUGUI _selectedItemTypeText;
    [SerializeField] private TextMeshProUGUI _selectedItemDescText;
    
    
    private List<UI_InventorySlot> _uiSlots = new List<UI_InventorySlot>();

    public GameObject _itemClickTap;
    
    protected override void Awake()
    {
        base.Awake();
        
        Init();
    }

    // 입력 레이어 상승/복원은 UI_Base가 BlocksLowerInput 기준으로 일괄 처리한다.
    protected override bool BlocksLowerInput => true;

    protected override void OnShow()
    {
        RefreshDictItem();
        SetInventory();
        InitPlayerEquipmentSlot();

        var firstItem = InventoryManager.Instance.ItemDict.Values.FirstOrDefault();
        if (firstItem != null)
            ShowSelectedItemDetail(firstItem.data, firstItem.count);
        else
            ClearSelectedItemDetail();
    }
    
    public override bool PerformBackFunction()
    {
        // ESC 키 입력 시 닫는다.
        Hide();
        return false;
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

    
    private void InitPlayerEquipmentSlot()
    {
        PlayerEquipment playerEquipment = GameObjectManager.Instance?.Player?.GetPlayerEquipment();
        if (playerEquipment == null)
        {
            return;
        }

        ItemManager manager = ItemManager.Instance;
        if (manager == null)
        {
            return;
        }
    }
    
    public void SetItemClickAnimation(UI_InventorySlot slot)
    {
        _itemClickTap.gameObject.SetActive(true);
        _itemClickTap.transform.SetParent(slot.transform);
        
        _itemClickTap.transform.localPosition = Vector3.zero;
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

    public void ShowSelectedItemDetail(ItemSO itemData, int count)
    {
        if (itemData == null)
        {
            ClearSelectedItemDetail();
            return;
        }
        _selectedItemPrefab.SetActive(true);
        _selectedItemImage.sprite = itemData.icon;
        _selectedItemImage.enabled = true;
        _selectedItemCountText.text = "보유: " + count.ToString();
        _selectedItemNameText.text = itemData.itemName;
        _selectedItemTypeText.text = itemData.itemType.ToDisplayString();
        _selectedItemDescText.text = itemData.itemDescription;
    }

    public void ClearSelectedItemDetail()
    {
        _selectedItemImage.sprite = null;
        _selectedItemImage.enabled = false;
        _selectedItemCountText.text = string.Empty;
        _selectedItemNameText.text = string.Empty;
        _selectedItemTypeText.text = string.Empty;
        _selectedItemDescText.text = string.Empty;

        _selectedItemPrefab.SetActive(false);
    }
}
