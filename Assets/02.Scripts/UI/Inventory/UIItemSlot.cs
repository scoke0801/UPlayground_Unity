
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

public class UIItemSlot : MonoBehaviour
{
    [SerializeField] private GameObject _rootContent;  
    [SerializeField] private TextMeshProUGUI _txtCount;
    [SerializeField] private TextMeshProUGUI _txtWeight;
    [SerializeField] private Image _imgItem;
    [SerializeField] private Image _imgRarity;

    private ItemSO _itemData = null;
    private int _itemCount = 0;
    
    public void Init(ItemSO itemData, int count)
    {
        _itemData = itemData;
        _itemCount = count;

        RefreshUI();
    }
    
    public void RefreshUI()
    {
        if (_itemData == null)
        {
            _rootContent.SetActive(false);
        }
        else
        {            
            _rootContent.SetActive(true);
            
            _imgRarity.sprite = AssetManager.Instance.GetAtlas(_itemData.itemRarity.ToString());
            _imgItem.sprite = _itemData.icon;
            _txtCount.text = _itemCount.ToString();
            _txtWeight.text = $"{InventoryManager.Instance.GetItemWeight(_itemData.itemId):0.0}";
        }
    }
}