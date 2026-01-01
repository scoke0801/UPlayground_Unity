using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UI_ItemAcquisitionEntry : UI_Base
{
    [SerializeField] TextMeshProUGUI _itemInfoText;

    [SerializeField] private Image _rarityIcon;

    [SerializeField] private Image _itemIcon;

    public void Init(ItemSO itemData)
    {
        _rarityIcon.sprite = AssetManager.Instance.GetAtlas(itemData.itemRarity.ToString());
        _itemIcon.sprite = AssetManager.Instance.GetAtlas(itemData.itemId.ToString());
        _itemInfoText.text = itemData.itemName;
    }
}
