using System;
using System.Collections.Generic;
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
    
    [Header("Character Preview")]
    [SerializeField] private RawImage _characterPreview;
    [SerializeField] private UICharacterPreviewRenderer _previewRenderer;

    [Header("Character HP")]
    [SerializeField] private Image _boardHpFill;
    [SerializeField] private TextMeshProUGUI _hpText;
    
    [Header("Equipment Slot")] 
    [SerializeField] private UI_InventorySlot _headSlot;
    [SerializeField] private UI_InventorySlot _chestSlot;
    [SerializeField] private UI_InventorySlot _pantSlot;
    [SerializeField] private UI_InventorySlot _shoesSlot;
    [SerializeField] private UI_InventorySlot _glovesSlot;
    
    [SerializeField] private UI_InventorySlot _leftHandSlot;
    [SerializeField] private UI_InventorySlot _rightHandSlot;
    
    private Dictionary<EquipArmorType, UI_InventorySlot> _armorSlotMap; // SlotClass는 실제 슬롯 타입으로 변경하세요
    
    private List<UI_InventorySlot> _uiSlots = new List<UI_InventorySlot>();

    public GameObject _itemClickTap;
    
    protected override void Awake()
    {
        base.Awake();
        
        Init();
        
        _armorSlotMap = new Dictionary<EquipArmorType, UI_InventorySlot>
        {
            { EquipArmorType.Head, _headSlot },
            { EquipArmorType.Chest, _chestSlot },
            { EquipArmorType.Arm, _glovesSlot },
            { EquipArmorType.Waist, _pantSlot },
            { EquipArmorType.Leg, _shoesSlot }
        };
        
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
        InputManager.Instance.SetInputLayer(_layer.ToInputLayer());
        
        RefreshDictItem();
        SetInventory();
        InitPlayerEquipmentSlot();
        
        EventManager.Instance.Subscribe<PlayerEvent, PlayerEquipChangeEvent>(
            PlayerEvent.EquipItem, 
            OnEquipItem
        );
        
        // 캐릭터 프리뷰 활성화
        if (_previewRenderer != null)
        {
            _previewRenderer.ShowPreview();
        }
        
        PlayerActor playerActor = GameObjectManager.Instance?.Player;
        if (playerActor != null)
        {
            RefreshPlayerHp(playerActor.CurrentHealth, playerActor.MaxHealth);
            playerActor.OnHpChanged += RefreshPlayerHp;
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
        InputManager.Instance.SetInputLayer(InputLayer.None);

        EventManager.Instance.Unsubscribe<PlayerEvent, PlayerEquipChangeEvent>(
            PlayerEvent.EquipItem, 
            OnEquipItem
        );
        
        // 캐릭터 프리뷰 비활성화
        if (_previewRenderer != null)
        {
            _previewRenderer.HidePreview();
        }
        PlayerActor playerActor = GameObjectManager.Instance?.Player;
        if (playerActor != null)
        {
            playerActor.OnHpChanged -= RefreshPlayerHp;
        }
    }
    
    public void SetInventory()
    {
        foreach (var t in _uiSlots)
        {
            t.RefreshUI();
        }

        _glovesSlot.RefreshUI();
        
        _imgWeightFill.fillAmount = InventoryManager.Instance.GetTotalWeight() / InventoryManager.Instance.MaxWeight;
        _txtWeight.text =
            $"({InventoryManager.Instance.GetTotalWeight():0.0}/{InventoryManager.Instance.MaxWeight:0.0})";
    }

    public void RefreshPlayerHp(float hp, float maxHp)
    {
        _boardHpFill.fillAmount = hp / maxHp;
        _hpText.text = $"{(int)hp}/{(int)maxHp}";
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

        foreach (EquipArmorType type in Enum.GetValues(typeof(EquipArmorType)))
        {
            // None은 제외
            if (type == EquipArmorType.None) continue;

            // 해당 부위의 아이템 데이터 가져오기
            var itemKey = playerEquipment.GetActiveEquipmentKey(type);
            EquipmentSO itemData = manager.GetItemData(itemKey) as EquipmentSO;

            if (itemData != null)
            {
                // 매핑된 딕셔너리에서 슬롯을 찾아 Init
                if (_armorSlotMap.TryGetValue(type, out var slot))
                {
                    slot.Init(itemData, 1);
                    slot.RefreshUI();
                }
            }
        }
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
