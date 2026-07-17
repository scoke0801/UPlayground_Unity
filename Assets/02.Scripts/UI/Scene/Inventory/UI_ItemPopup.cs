

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.Data.Item;

namespace UPlayGround.UI
{
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
            _bottomButton.BindClickResult(OnBottomButtonClick);
        }

        // 입력 레이어 상승/복원은 UI_Base가 BlocksLowerInput 기준으로 일괄 처리한다.
        protected override bool BlocksLowerInput => true;

        protected override void OnHide()
        {
            _cachedItemSo = null;
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

            _itemNameText.text = itemData.itemName;
            _itemDescText.text = itemData.itemDescription;

            _itemWeightText.text = $"{UISvc.Inventory.GetItemWeight(itemData.itemId):0.0}";

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

        private UICommonButtonClickResult OnBottomButtonClick()
        {
            InventoryActionResult result = InventoryActionResult.Failed;

            // 버튼 유형에 따라서 처리
            if (_bottomButtonType == BottomButtonType.Equip)
            {
                result = HandleEquip();
            }
            else if (_bottomButtonType == BottomButtonType.Use)
            {
                result = HandleUse();
            }

            if (result != InventoryActionResult.Success)
            {
                Debug.LogWarning($"[UI_ItemPopup] 아이템 액션 실패: {result}");
                return UICommonButtonClickResult.Failed;
            }

            Hide();
            return UICommonButtonClickResult.Success;
        }

        private InventoryActionResult HandleEquip()
        {
            if (_cachedItemSo == null)
            {
                return InventoryActionResult.InvalidItem;
            }

            return UISvc.Inventory.TryEquipItem(_cachedItemSo.itemId);
        }

        private InventoryActionResult HandleUse()
        {
            if (_cachedItemSo == null)
            {
                return InventoryActionResult.InvalidItem;
            }

            return UISvc.Inventory.TryUseItem(_cachedItemSo.itemId);
        }
    }
}
