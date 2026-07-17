
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.Data.Item;

namespace UPlayGround.UI
{
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

                _imgRarity.color = GetRarityColor(_itemData.itemRarity);
                _imgItem.sprite = _itemData.icon;
                _txtCount.text = _itemCount.ToString();
                _txtWeight.text = $"{UISvc.Inventory.GetItemWeight(_itemData.itemId):0.0}";
            }
        }

        private static Color GetRarityColor(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.COMMON => Color.white,
                ItemRarity.UNCOMMON => new Color(0.35f, 0.9f, 0.45f),
                ItemRarity.RARE => new Color(0.35f, 0.6f, 1f),
                ItemRarity.UNIQUE => new Color(0.85f, 0.45f, 1f),
                ItemRarity.LEGENDARY => new Color(1f, 0.65f, 0.2f),
                _ => Color.clear
            };
        }
    }
}
