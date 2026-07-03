using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

/// <summary>
/// 인벤토리 UI 슬롯
/// </summary>
public class UI_InventorySlot : UI_Base, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private GameObject _rootContent;  
    [SerializeField] private GameObject _rootEmptySlot;
    [SerializeField] private TextMeshProUGUI _txtCount;
    [SerializeField] private TextMeshProUGUI _txtWeight;
    [SerializeField] private TextMeshProUGUI _txtEnhance;   // 강화 배지 "+N"
    [SerializeField] private Image _imgItem;
    [SerializeField] private Image _imgRarity;

    private ItemSO _itemData = null;
    private int _itemCount = 0;
    private int _enhanceLevel = 0;

    private UI_Inventory _parent;

    private void OnEnable()
    {
        RefreshUI();
    }
    
    private void OnDisable()
    {
    }

    public void Init(ItemSO itemData, int count, int enhanceLevel = 0)
    {
        _itemData = itemData;
        _itemCount = count;
        _enhanceLevel = enhanceLevel;
    }

    public void Clear()
    {
        Init(null, 0);
        RefreshUI();
    }

    public void SetParent(UI_Inventory inventory)
    {
        _parent = inventory;
    }

    public void RefreshUI()
    {
        if (_itemData == null)
        {
            _rootContent.SetActive(false);
            _rootEmptySlot.SetActive(true);
        }
        else
        {            
            _rootContent.SetActive(true);
            _rootEmptySlot.SetActive(false);
            _imgRarity.color = _itemData.itemRarity.ToColor();
            _imgItem.sprite = _itemData.icon;
            _txtCount.text = _itemCount.ToString();
            _txtWeight.text = $"{InventoryManager.Instance.GetItemWeight(_itemData.itemId):0.0}";

            if (_txtEnhance != null)
                _txtEnhance.text = _enhanceLevel > 0 ? $"+{_enhanceLevel}" : string.Empty;
        }
    }

    #region IPointerEnterHandler / IPointerExitHandler
    public void OnPointerEnter(PointerEventData eventData)
    {
        _parent.SetItemClickAnimation(this);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        _parent.OnSlotPointerExit();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_itemData != null)
            _parent.ShowSelectedItemDetail(_itemData, _itemCount);
        else
            _parent.ClearSelectedItemDetail();
    }
    #endregion
}
