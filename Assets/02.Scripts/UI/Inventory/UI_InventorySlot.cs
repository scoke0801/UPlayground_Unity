using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 인벤토리 UI 슬롯
/// </summary>
public class UI_InventorySlot : UI_Base, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject _rootContent;  
    [SerializeField] private GameObject _rootEmptySlot;
    [SerializeField] private TextMeshProUGUI _txtCount;
    [SerializeField] private TextMeshProUGUI _txtWeight;
    [SerializeField] private Image _imgItem;
    [SerializeField] private Image _imgRarity;
    
    private int _slotIndex = 0;
    private ItemInstance _itemInstance;

    private UI_Inventory _parent;
    
    private void Awake()
    {
    }
    
    private void OnEnable()
    {
        
    }
    
    private void OnDisable()
    {
    }

    public void Init(ItemInstance itemInstance)
    {
        _itemInstance = itemInstance;
    }

    public void SetParent(UI_Inventory inventory)
    {
        _parent = inventory;
    }

    public void RefreshUI()
    {
        if (_itemInstance == null)
        {
            _rootContent.SetActive(false);
            _rootEmptySlot.SetActive(true);
        }
        else
        {            
            _rootContent.SetActive(true);
            _rootEmptySlot.SetActive(false);
            _imgRarity.sprite = AssetManager.Instance.GetAtlas(_itemInstance.data.itemRarity.ToString());
            _imgItem.sprite = AssetManager.Instance.GetAtlas(_itemInstance.data.itemId.ToString());
            _txtCount.text = _itemInstance.count.ToString();
            _txtWeight.text = $"{InventoryManager.Instance.GetItemWeight(_itemInstance.data.itemId):0.0}";
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _parent.SetItemClickAnimation(this);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        _parent.OnSlotPointerExit();
    }
}
