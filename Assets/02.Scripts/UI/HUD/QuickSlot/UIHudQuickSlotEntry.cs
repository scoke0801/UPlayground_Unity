using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;

namespace UPlayGround.UI
{
    /// <summary>
    /// HUD 소비 아이템 퀵슬롯 한 칸.
    /// 아이템 사용 규칙은 직접 판단하지 않고 <see cref="IUIInventoryService"/>에 위임한다.
    /// </summary>
    public sealed class UIHudQuickSlotEntry : MonoBehaviour
    {
        [SerializeField] private int _slotIndex;
        [SerializeField] private Image _slotBackground;
        [SerializeField] private Outline _rarityOutline;
        [SerializeField] private Image _iconImage;
        [SerializeField] private GameObject _countRoot;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private GameObject _emptyMark;
        [SerializeField] private CanvasGroup _stateGroup;
        [SerializeField] private Button _useButton;

        public int ItemId => UIQuickSlotAssignments.GetItemId(_slotIndex);

        private void Awake()
        {
            if (_useButton != null)
                _useButton.onClick.AddListener(TryUse);
        }

        private void OnDestroy()
        {
            if (_useButton != null)
                _useButton.onClick.RemoveListener(TryUse);
        }

        public void Refresh(IUIInventoryService inventory)
        {
            int itemId = ItemId;
            ItemInstance item = inventory != null && itemId > 0
                ? inventory.GetItem(itemId)
                : null;
            int count = item != null
                ? inventory.GetItemCount(itemId)
                : 0;

            if (_iconImage != null)
            {
                _iconImage.sprite = item?.data?.icon;
                _iconImage.enabled = _iconImage.sprite != null;
            }

            Color rarityColor = item?.data != null
                ? item.data.itemRarity.ToColor()
                : Color.clear;
            if (_rarityOutline != null)
                _rarityOutline.effectColor = rarityColor.a > 0f
                    ? rarityColor
                    : new Color(0.12f, 0.68f, 1f, 0.95f);
            if (_slotBackground != null)
            {
                Color baseColor = new Color(0.055f, 0.14f, 0.22f, 0.84f);
                _slotBackground.color = rarityColor.a > 0f
                    ? Color.Lerp(baseColor, rarityColor, 0.24f)
                    : baseColor;
            }

            if (_countText != null)
            {
                _countText.text = count > 0 ? count.ToString() : string.Empty;
                _countText.gameObject.SetActive(itemId > 0);
            }
            if (_countRoot != null)
                _countRoot.SetActive(itemId > 0);

            if (_emptyMark != null)
                _emptyMark.SetActive(itemId <= 0);

            if (_stateGroup != null)
                _stateGroup.alpha = itemId <= 0 ? 0.55f : count > 0 ? 1f : 0.38f;

            if (_useButton != null)
                _useButton.interactable = count > 0;
        }

        public void TryUse()
        {
            var inventory = UISvc.Inventory;
            int itemId = ItemId;
            if (inventory == null || itemId <= 0)
                return;

            inventory.TryUseItem(itemId);
            Refresh(inventory);
        }
    }
}
