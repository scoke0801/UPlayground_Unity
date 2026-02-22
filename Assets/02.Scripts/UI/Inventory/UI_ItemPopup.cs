

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

public class UI_ItemPopup : UI_Base
{
    private enum BottomButtonType
    {
        None = 0,
        Equip,
        UnEquip,
        Use
    }
    [SerializeField] private UIItemSlot _itemSlot;
    [SerializeField] private TextMeshProUGUI _itemNameText;
    [SerializeField] private TextMeshProUGUI _itemWeightText;
    [SerializeField] private TextMeshProUGUI _itemDescText;
    [SerializeField] private UICommonButton _bottomButton;
    [SerializeField] private Button _closeButton;
    
    private ItemSO _cachedItemSo = null;
    private BottomButtonType _bottomButtonType = BottomButtonType.Equip;

    protected override void Awake()
    {
        base.Awake();
        _closeButton.onClick.AddListener(OnClickClose);
        _bottomButton.Button.onClick.AddListener(OnBottomButtonClick);
    }

    protected override void OnShow()
    {
        base.OnShow();
        
        InputManager.Instance.SetInputLayer(_layer.ToInputLayer());
    }

    protected override void OnHide()
    {
        _cachedItemSo = null;
        
        InputManager.Instance.SetInputLayer(InputLayer.None);
        base.OnHide();
    }

    public override bool PerformBackFunction()
    {
        // ESC 키 입력 시 닫는다.
        Hide();
        return false;
    }
    
    public void Init(ItemSO itemData, int count)
    {
        _cachedItemSo = itemData;
        _itemSlot.Init(itemData, count);
        
        _itemNameText.text = itemData.name;
        _itemDescText.text = itemData.itemDescription;
        
        _itemWeightText.text = $"{InventoryManager.Instance.GetItemWeight(itemData.itemId):0.0}";

        InitButton(itemData);
    }

    private void InitButton(ItemSO itemData)
    {
        // [TODO]버튼은 상황에 따라 다르게 하자
        // 1. 장착 2. 해제 3. 사용

        if (itemData.itemType == ItemType.NONE)
        {
            _bottomButtonType = BottomButtonType.None;
            _bottomButton.gameObject.SetActive(false);
        }
        else if (itemData.itemType == ItemType.CONSUMABLE)
        {
            _bottomButtonType = BottomButtonType.Use;
            
            _bottomButton.gameObject.SetActive(true);
            _bottomButton.Text.text = "사용";
        }
        else if (itemData.itemType == ItemType.EQUIPMENT)
        {
            _bottomButtonType = BottomButtonType.Equip;
            
            _bottomButton.gameObject.SetActive(true);
            _bottomButton.Text.text = "장착";
        }
        
    }
    
    private void OnClickClose()
    {
        Hide();
    }
    
    private void OnBottomButtonClick()
    {
        // 버튼 유형에 따라서 처리
        if (_bottomButtonType == BottomButtonType.Equip)
        {
            HandleEquip(); 
        }
        
        Hide();
    }

    private void HandleEquip()
    {
        EquipmentSO equipData = _cachedItemSo as EquipmentSO;
        if (equipData == null)
        {
            return;
        }
        PlayerEquipChangeEvent eventData = new PlayerEquipChangeEvent()
        {
            itemKey = equipData.itemId,
            weaponType = equipData.weaponType,
            equipPosition = equipData.equipSlot
        };
        
        EventManager.Instance.Send(PlayerEvent.EquipItem, eventData);  
    }
}