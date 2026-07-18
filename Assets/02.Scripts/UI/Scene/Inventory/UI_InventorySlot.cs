using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.Data.Item;

namespace UPlayGround.UI
{
    /// <summary>
    /// 인벤토리 UI 슬롯
    ///
    /// 하이라이트는 두 종류:
    ///   - hover: UI_Inventory의 공유 _itemClickTap 오버레이가 마우스가 올라온 슬롯으로 이동
    ///   - focus: EventSystem이 이 슬롯을 선택(키보드/게임패드 네비게이션 또는 클릭)했을 때 _focusHighlight 표시
    /// focus를 받으려면 슬롯 루트에 Selectable이 있어야 한다(프리팹 빌더에서 부착).
    /// </summary>
    public class UI_InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
                                    ISelectHandler, IDeselectHandler
    {
        [SerializeField] private GameObject _rootContent;
        [SerializeField] private GameObject _rootEmptySlot;
        [SerializeField] private TextMeshProUGUI _txtCount;
        [SerializeField] private TextMeshProUGUI _txtWeight;
        [SerializeField] private GameObject _enhanceRoot;
        [SerializeField] private TextMeshProUGUI _txtEnhance;   // 강화 배지 "+N"
        [SerializeField] private Image _imgItem;
        [SerializeField] private Image _imgRarity;
        [SerializeField] private GameObject _focusHighlight;    // 포커스(선택) 시 표시되는 하이라이트 프레임
        [SerializeField] private GameObject _equippedBadge;     // 파티원이 장착 중일 때 표시 (선택적)
        [SerializeField] private Image _equippedPortrait;       // 장착 중인 파티원 초상 (선택적)
        [SerializeField] private TextMeshProUGUI _equippedBadgeText; // 2명 이상일 때 "+N" (선택적)
        [SerializeField] private GameObject _cooldownRoot;
        [SerializeField] private Image _cooldownFill;
        [SerializeField] private TextMeshProUGUI _cooldownText;

        private ItemSO _itemData = null;
        private int _itemCount = 0;
        private int _enhanceLevel = 0;
        private int _inventorySlotKey = -1;

        private UI_Inventory _parent;

        public bool HasItem => _itemData != null;

        private void OnEnable()
        {
            RefreshUI();
        }

        private void OnDisable()
        {
            // 비활성화되면 포커스 하이라이트도 해제(재사용 시 잔상 방지)
            SetFocus(false);
        }

        private void Update()
        {
            if (_itemData is ConsumableSO)
                RefreshCooldown();
        }

        public void Init(ItemSO itemData, int count, int enhanceLevel = 0, int inventorySlotKey = -1)
        {
            _itemData = itemData;
            _itemCount = count;
            _enhanceLevel = enhanceLevel;
            _inventorySlotKey = inventorySlotKey;
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
                if (_equippedBadge != null) _equippedBadge.SetActive(false);
                SetCooldownVisible(false, 0f, 0f);
            }
            else
            {
                _rootContent.SetActive(true);
                _rootEmptySlot.SetActive(false);
                _imgRarity.color = _itemData.itemRarity.ToColor();
                _imgItem.sprite = _itemData.icon;
                _txtCount.text = _itemCount.ToString();
                _txtWeight.text = $"{_itemData.weight * _itemCount:0.0}";

                bool hasEnhancement = _enhanceLevel > 0;
                GameObject enhanceRoot = _enhanceRoot;
                if (enhanceRoot == null
                    && _txtEnhance != null
                    && _txtEnhance.transform.parent != null
                    && _txtEnhance.transform.parent.name == "Enhance")
                {
                    enhanceRoot = _txtEnhance.transform.parent.gameObject;
                }

                if (enhanceRoot != null)
                    enhanceRoot.SetActive(hasEnhancement);
                if (_txtEnhance != null)
                    _txtEnhance.text = hasEnhancement ? $"+{_enhanceLevel}" : string.Empty;

                // 장착 중인 파티원 초상 뱃지. 프리팹에 뱃지 오브젝트가 없으면 무시.
                RefreshEquippedBadge();
                RefreshCooldown();
            }
        }

        private void RefreshCooldown()
        {
            float remaining = 0f;
            float duration = 0f;
            bool onCooldown = _itemData is ConsumableSO
                && UISvc.Inventory != null
                && UISvc.Inventory.TryGetConsumableCooldown(
                    _itemData.itemId, out remaining, out duration);

            SetCooldownVisible(onCooldown, remaining, duration);
        }

        private void SetCooldownVisible(bool visible, float remaining, float duration)
        {
            if (_cooldownRoot != null)
                _cooldownRoot.SetActive(visible);
            if (_cooldownFill != null)
                _cooldownFill.fillAmount = visible && duration > 0f
                    ? Mathf.Clamp01(remaining / duration)
                    : 0f;
            if (_cooldownText != null)
                _cooldownText.text = visible ? remaining.ToString("0.0") : string.Empty;
        }

        #region IPointerEnterHandler / IPointerExitHandler
        public void OnPointerEnter(PointerEventData eventData)
        {
            _parent?.SetItemClickAnimation(this);
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            _parent?.OnSlotPointerExit();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_parent == null)
                return;

            if (_itemData != null)
                _parent.ShowSelectedItemDetail(_itemData, _itemCount, _inventorySlotKey);
            else
                _parent.ClearSelectedItemDetail();
        }
        #endregion

        #region ISelectHandler / IDeselectHandler (키보드/게임패드 포커스)
        public void OnSelect(BaseEventData eventData)
        {
            SetFocus(true);

            if (_itemData != null)
                _parent?.ShowSelectedItemDetail(_itemData, _itemCount, _inventorySlotKey);
            else
                _parent?.ClearSelectedItemDetail();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            SetFocus(false);
        }
        #endregion

        // 이 아이템을 장착 중인 파티원의 초상을 슬롯 우상단에 표시한다(여러 명이면 첫 초상 + "+N").
        private void RefreshEquippedBadge()
        {
            if (_equippedBadge == null && _equippedPortrait == null && _equippedBadgeText == null)
                return;

            var equippers = UISvc.Inventory?.GetEquippingCharacters(_inventorySlotKey);
            bool anyEquipped = equippers != null && equippers.Count > 0;

            if (_equippedBadge != null)
                _equippedBadge.SetActive(anyEquipped);

            if (!anyEquipped)
            {
                if (_equippedPortrait != null) _equippedPortrait.enabled = false;
                if (_equippedBadgeText != null) _equippedBadgeText.text = string.Empty;
                return;
            }

            var memberData = UISvc.Party?.PartyMemberDataSO;
            Sprite head = memberData != null ? memberData.GetHeadSprite(equippers[0]) : null;

            if (_equippedPortrait != null)
            {
                _equippedPortrait.sprite  = head;
                _equippedPortrait.enabled = head != null;
            }
            if (_equippedBadgeText != null)
                _equippedBadgeText.text = equippers.Count > 1 ? $"+{equippers.Count - 1}" : string.Empty;
        }

        /// <summary> 포커스 하이라이트 표시/숨김. </summary>
        public void SetFocus(bool focused)
        {
            if (_focusHighlight != null)
                _focusHighlight.SetActive(focused);
        }
    }
}
