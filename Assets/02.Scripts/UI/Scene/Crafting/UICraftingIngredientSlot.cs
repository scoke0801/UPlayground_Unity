using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 제작 UI — 재료 슬롯 1개
    /// UI_Crafting의 재료 리스트에서 Instantiate해 사용한다.
    /// </summary>
    public class UICraftingIngredientSlot : MonoBehaviour
    {
        [SerializeField] private Image            _imgIcon;
        [SerializeField] private TextMeshProUGUI  _txtName;
        [SerializeField] private TextMeshProUGUI  _txtCount;   // "보유/필요"
        [SerializeField] private Image            _imgCountBg; // 충족 여부 배경색

        [Header("색상")]
        [SerializeField] private Color _colorSufficientBg = new Color(0.2f, 0.8f, 0.3f, 0.85f);
        [SerializeField] private Color _colorInsufficientBg = new Color(0.9f, 0.25f, 0.25f, 0.85f);
        [SerializeField] private Color _colorCountText = new Color(0.95f, 0.97f, 1f, 1f);

        private int _ingredientItemID;

        private void Awake()
        {
            EnsureReferences();
        }

        /// <summary>
        /// 슬롯 데이터 설정.
        /// quantity : 현재 UI에서 선택된 제작 수량 (재료 필요 수량에 곱해진다)
        /// </summary>
        public void Init(int ingredientItemID, int requiredPerCraft, int quantity = 1, bool? isAvailable = null)
        {
            _ingredientItemID = ingredientItemID;

            var itemData = Svc.Item.GetItemData(ingredientItemID);
            int needed   = requiredPerCraft * quantity;
            int have     = UISvc.Inventory.GetItemCount(ingredientItemID);

            // 아이콘 / 이름
            if (itemData != null)
            {
                _imgIcon.sprite  = itemData.icon;
                _imgIcon.color   = Color.white;
                _imgIcon.enabled = itemData.icon != null;
                _txtName.text    = itemData.itemName;
            }
            else
            {
                _imgIcon.enabled = false;
                _txtName.text    = $"ID:{ingredientItemID}";
            }

            ApplyCountState(have, needed, isAvailable);
        }

        /// <summary>
        /// 인벤토리 변동 후 수량만 갱신할 때 사용
        /// </summary>
        public void RefreshCount(int requiredPerCraft, int quantity = 1, bool? isAvailable = null)
        {
            int needed = requiredPerCraft * quantity;
            int have   = UISvc.Inventory.GetItemCount(_ingredientItemID);

            ApplyCountState(have, needed, isAvailable);
        }

        private void ApplyCountState(int have, int needed, bool? isAvailable = null)
        {
            EnsureReferences();

            bool sufficient = isAvailable ?? have >= needed;
            Color stateBgColor = sufficient ? _colorSufficientBg : _colorInsufficientBg;

            if (_txtCount != null)
            {
                _txtCount.text = $"{have}/{needed}";
                _txtCount.color = _colorCountText;
                _txtCount.raycastTarget = false;
            }

            if (_imgCountBg != null)
                _imgCountBg.color = stateBgColor;
        }

        private void EnsureReferences()
        {
            if (_txtCount == null)
            {
                Transform count = transform.Find("CountBg/Count");
                if (count != null)
                    _txtCount = count.GetComponent<TextMeshProUGUI>();
            }

            if (_imgCountBg == null && _txtCount != null)
                _imgCountBg = _txtCount.transform.parent?.GetComponent<Image>();

            if (_imgCountBg == null)
            {
                Transform countBg = transform.Find("CountBg");
                if (countBg != null)
                    _imgCountBg = countBg.GetComponent<Image>();
            }

            if (_imgIcon == null)
            {
                Transform icon = transform.Find("Icon");
                if (icon != null)
                    _imgIcon = icon.GetComponent<Image>();
            }

            if (_txtName == null)
            {
                Transform name = transform.Find("Name");
                if (name != null)
                    _txtName = name.GetComponent<TextMeshProUGUI>();
            }
        }
    }
}
