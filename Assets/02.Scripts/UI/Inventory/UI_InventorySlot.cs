using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 인벤토리 UI 슬롯
/// </summary>
public class UI_InventorySlot : UI_Base
{
    [SerializeField] private GameObject _rootContent;  
    [SerializeField] private GameObject _rootEmptySlot;
    [SerializeField] private TextMeshProUGUI _txtCount;
    [SerializeField] private TextMeshProUGUI _txtWeight;
    [SerializeField] private Image _imgItem;
    [SerializeField] private Image _imgRarity;
    
    private int _slotIndex = 0;
    private ItemInstance _itemInstance;
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
        }
    }
    

}
