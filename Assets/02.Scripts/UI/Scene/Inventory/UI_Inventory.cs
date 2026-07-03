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

    [Header("Selected Item Actions")]
    [SerializeField] private UICommonButton _useButton;
    [SerializeField] private UICommonButton _equipButton;
    [SerializeField] private UICommonButton _dropButton;

    [Header("Category Tabs")]
    [SerializeField] private Button _tabAll;
    [SerializeField] private Button _tabConsumable;
    [SerializeField] private Button _tabEquipment;
    [SerializeField] private Button _tabMaterial;
    [SerializeField] private Button _tabQuest;
    [SerializeField] private Button _tabImportant;

    [Header("Header / Footer")]
    [SerializeField] private TextMeshProUGUI _txtItemCount; // "전체 38 / 120"
    [SerializeField] private TextMeshProUGUI _txtGold;      // 골드
    [SerializeField] private TMP_Dropdown    _sortDropdown; // 정렬 드롭다운 (선택)
    [SerializeField] private UICommonButton  _sortButton;   // 하단 정렬 버튼 (선택, 클릭 시 순환)

    [Header("Detail - Extended")]
    [SerializeField] private TextMeshProUGUI _selectedRarityText;
    [SerializeField] private TextMeshProUGUI _selectedWeightText;
    [SerializeField] private TextMeshProUGUI _selectedEquipSlotText;
    [SerializeField] private GameObject      _statPanel;
    [SerializeField] private TextMeshProUGUI _statAttackText;
    [SerializeField] private TextMeshProUGUI _statCritText;
    [SerializeField] private TextMeshProUGUI _statCritDmgText;
    [SerializeField] private TextMeshProUGUI _statAtkSpeedText;

    private enum InventorySortMode { Default = 0, Name = 1, Rarity = 2, Weight = 3 }

    private List<UI_InventorySlot> _uiSlots = new List<UI_InventorySlot>();
    private ItemSO _selectedItemData;
    private int _selectedItemCount;
    private ItemType? _categoryFilter = null;   // null = 전체
    private InventorySortMode _sortMode = InventorySortMode.Default;

    public GameObject _itemClickTap;
    
    protected override void Awake()
    {
        base.Awake();
        
        Init();
        BindActionButtons();
        BindCategoryTabs();
        BindSortControls();
    }

    // 입력 레이어 상승/복원은 UI_Base가 BlocksLowerInput 기준으로 일괄 처리한다.
    protected override bool BlocksLowerInput => true;

    protected override void OnShow()
    {
        _categoryFilter = null;
        _sortMode       = InventorySortMode.Default;
        if (_sortDropdown != null) _sortDropdown.SetValueWithoutNotify(0);

        var items = RefreshDictItem();
        SetInventory();
        InitPlayerEquipmentSlot();

        var firstItem = items.FirstOrDefault();
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

        if (_txtGold != null)
            _txtGold.text = InventoryManager.Instance.Gold.ToString("N0");

        if (_txtItemCount != null)
            _txtItemCount.text = $"전체 {InventoryManager.Instance.ItemDict.Count} / {InventoryManager.Instance.MaxSlots}";
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

    private List<ItemInstance> RefreshDictItem()
    {
        var items = GetFilteredSortedItems();

        int value = 0;
        foreach (var inst in items)
        {
            if (_uiSlots.Count <= value + 1)
            {
                AddSlot(1);
            }
            _uiSlots[value++].Init(inst.data, inst.count, inst.enhancementLevel);
        }

        for (int i = value; i < _uiSlots.Count; i++)
        {
            _uiSlots[i].Clear();
        }

        return items;
    }

    /// <summary> 현재 카테고리 필터 + 정렬을 적용한 아이템 목록을 반환한다. </summary>
    private List<ItemInstance> GetFilteredSortedItems()
    {
        IEnumerable<ItemInstance> src = InventoryManager.Instance.ItemDict.Values
            .Where(i => i != null && i.data != null);

        if (_categoryFilter.HasValue)
            src = src.Where(i => i.data.itemType == _categoryFilter.Value);

        src = _sortMode switch
        {
            InventorySortMode.Name   => src.OrderBy(i => i.data.itemName),
            InventorySortMode.Rarity => src.OrderByDescending(i => (int)i.data.itemRarity)
                                           .ThenBy(i => i.data.itemId),
            InventorySortMode.Weight => src.OrderByDescending(i => i.data.weight)
                                           .ThenBy(i => i.data.itemId),
            _                        => src.OrderBy(i => i.data.itemId),
        };

        return src.ToList();
    }

    // ──── 카테고리 / 정렬 ────

    private void BindCategoryTabs()
    {
        _tabAll?.onClick.AddListener(()        => SetCategory(null));
        _tabConsumable?.onClick.AddListener(() => SetCategory(ItemType.CONSUMABLE));
        _tabEquipment?.onClick.AddListener(()  => SetCategory(ItemType.EQUIPMENT));
        _tabMaterial?.onClick.AddListener(()   => SetCategory(ItemType.MATERIAL));
        _tabQuest?.onClick.AddListener(()      => SetCategory(ItemType.QUEST));
        _tabImportant?.onClick.AddListener(()  => SetCategory(ItemType.IMPORTANT));
    }

    private void SetCategory(ItemType? type)
    {
        _categoryFilter = type;
        var items = RefreshDictItem();
        SetInventory();
        RefreshSelectionForItems(items);
    }

    private void BindSortControls()
    {
        if (_sortDropdown != null)
            _sortDropdown.onValueChanged.AddListener(OnSortDropdownChanged);

        _sortButton?.BindClickResult(OnClickCycleSort);
    }

    private void OnSortDropdownChanged(int index)
    {
        _sortMode = (InventorySortMode)Mathf.Clamp(index, 0, 3);
        var items = RefreshDictItem();
        SetInventory();
        RefreshSelectionForItems(items);
    }

    private UICommonButtonClickResult OnClickCycleSort()
    {
        _sortMode = (InventorySortMode)(((int)_sortMode + 1) % 4);
        if (_sortDropdown != null) _sortDropdown.SetValueWithoutNotify((int)_sortMode);
        var items = RefreshDictItem();
        SetInventory();
        RefreshSelectionForItems(items);
        return UICommonButtonClickResult.Success;
    }

    private void RefreshSelectionForItems(IReadOnlyList<ItemInstance> items)
    {
        if (items == null || items.Count == 0)
        {
            ClearSelectedItemDetail();
            return;
        }

        if (_selectedItemData != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var inst = items[i];
                if (inst?.data == null || inst.data.itemId != _selectedItemData.itemId)
                    continue;

                ShowSelectedItemDetail(inst.data, inst.count);
                return;
            }
        }

        var first = items[0];
        ShowSelectedItemDetail(first.data, first.count);
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

        _selectedItemData = itemData;
        _selectedItemCount = count;

        // 강화 레벨 조회 (장비 인스턴스에서)
        int enhance = 0;
        if (InventoryManager.Instance.ItemDict.TryGetValue(itemData.itemId, out var inst))
            enhance = inst.enhancementLevel;

        var equip = itemData as EquipmentSO;
        bool isEquip = equip != null;

        _selectedItemPrefab.SetActive(true);
        _selectedItemImage.sprite = itemData.icon;
        _selectedItemImage.enabled = true;
        _selectedItemCountText.text = "보유: " + count.ToString();
        _selectedItemNameText.text = (isEquip && enhance > 0)
            ? $"{itemData.itemName} +{enhance}"
            : itemData.itemName;
        _selectedItemTypeText.text = itemData.itemType.ToDisplayString();
        _selectedItemDescText.text = itemData.itemDescription;

        // 등급 / 무게
        if (_selectedRarityText != null)
        {
            _selectedRarityText.text  = itemData.itemRarity.ToDisplayString();
            _selectedRarityText.color = itemData.itemRarity.ToColor();
        }
        if (_selectedWeightText != null)
            _selectedWeightText.text = $"{itemData.weight:0.0}";

        // 장착 부위
        if (_selectedEquipSlotText != null)
        {
            _selectedEquipSlotText.gameObject.SetActive(isEquip);
            if (isEquip)
                _selectedEquipSlotText.text = equip.equipSlot.ToDisplayString(equip.weaponType);
        }

        // 능력치 (장비만)
        if (_statPanel != null) _statPanel.SetActive(isEquip);
        if (isEquip)
        {
            if (_statAttackText != null)   _statAttackText.text   = $"{equip.attackPower:0.#}";
            if (_statCritText != null)     _statCritText.text     = $"{equip.critChance:0.#}%";
            if (_statCritDmgText != null)  _statCritDmgText.text  = $"{equip.critDamage:0.#}%";
            if (_statAtkSpeedText != null) _statAtkSpeedText.text = $"{equip.attackSpeed:0.00}";
        }

        RefreshActionButtons();
    }

    public void ClearSelectedItemDetail()
    {
        _selectedItemData = null;
        _selectedItemCount = 0;

        _selectedItemImage.sprite = null;
        _selectedItemImage.enabled = false;
        _selectedItemCountText.text = string.Empty;
        _selectedItemNameText.text = string.Empty;
        _selectedItemTypeText.text = string.Empty;
        _selectedItemDescText.text = string.Empty;

        if (_selectedRarityText != null)    _selectedRarityText.text = string.Empty;
        if (_selectedWeightText != null)    _selectedWeightText.text = string.Empty;
        if (_selectedEquipSlotText != null) _selectedEquipSlotText.gameObject.SetActive(false);
        if (_statPanel != null)             _statPanel.SetActive(false);

        _selectedItemPrefab.SetActive(false);
        RefreshActionButtons();
    }

    private void BindActionButtons()
    {
        _useButton?.BindClickResult(OnClickUseSelectedItem);
        _equipButton?.BindClickResult(OnClickEquipSelectedItem);
        _dropButton?.BindClickResult(OnClickDropSelectedItem);
    }

    private void RefreshActionButtons()
    {
        bool hasItem = _selectedItemData != null && _selectedItemCount > 0;

        SetActionButtonActive(_useButton, hasItem && _selectedItemData.itemType == ItemType.CONSUMABLE);
        SetActionButtonActive(_equipButton, hasItem && _selectedItemData.itemType == ItemType.EQUIPMENT);
        SetActionButtonActive(_dropButton, hasItem);
    }

    private static void SetActionButtonActive(UICommonButton button, bool active)
    {
        if (button != null)
        {
            button.gameObject.SetActive(active);
        }
    }

    private UICommonButtonClickResult OnClickUseSelectedItem()
    {
        if (_selectedItemData == null)
        {
            return UICommonButtonClickResult.Failed;
        }

        InventoryActionResult result = InventoryManager.Instance.TryUseItem(_selectedItemData.itemId);
        return RefreshAfterAction(result);
    }

    private UICommonButtonClickResult OnClickEquipSelectedItem()
    {
        if (_selectedItemData == null)
        {
            return UICommonButtonClickResult.Failed;
        }

        InventoryActionResult result = InventoryManager.Instance.TryEquipItem(_selectedItemData.itemId);
        return RefreshAfterAction(result);
    }

    private UICommonButtonClickResult OnClickDropSelectedItem()
    {
        if (_selectedItemData == null)
        {
            return UICommonButtonClickResult.Failed;
        }

        InventoryActionResult result = InventoryManager.Instance.TryDropItem(_selectedItemData.itemId);
        return RefreshAfterAction(result);
    }

    private UICommonButtonClickResult RefreshAfterAction(InventoryActionResult result)
    {
        if (result != InventoryActionResult.Success)
        {
            Debug.LogWarning($"[UI_Inventory] 아이템 액션 실패: {result}");
            return UICommonButtonClickResult.Failed;
        }

        var items = RefreshDictItem();
        SetInventory();

        if (_selectedItemData != null && InventoryManager.Instance.HasItem(_selectedItemData.itemId))
        {
            RefreshSelectionForItems(items);
        }
        else
        {
            RefreshSelectionForItems(items);
        }

        return UICommonButtonClickResult.Success;
    }
}
