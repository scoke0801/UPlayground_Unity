using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Item;
using UPlayGround.Data.Merchant;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>상인의 판매 목록과 플레이어의 매입 가능 물품을 한 화면에서 거래한다.</summary>
    public sealed class UI_Scene_Merchant : UI_SceneBase
    {
        [Header("상점 머리글")]
        [SerializeField] private TextMeshProUGUI _merchantName;
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private RectTransform _goldPanel;
        [SerializeField] private Button _closeButton;

        [Header("거래 목록")]
        [SerializeField] private UITabGroup _tradeTabs;
        [SerializeField] private ScrollRect _listScroll;
        [SerializeField] private Transform _listContent;
        [SerializeField] private UIMerchantItemSlot _itemSlotPrefab;
        [SerializeField] private GameObject _emptyState;
        [SerializeField] private TextMeshProUGUI _emptyStateText;

        [Header("품목 상세")]
        [SerializeField] private GameObject _detailPanel;
        [SerializeField] private Image _detailIcon;
        [SerializeField] private TextMeshProUGUI _detailName;
        [SerializeField] private TextMeshProUGUI _detailDescription;
        [SerializeField] private TextMeshProUGUI _detailPrice;
        [SerializeField] private TextMeshProUGUI _detailAvailability;

        [Header("거래 조작")]
        [SerializeField] private Button _quantityMinusButton;
        [SerializeField] private Button _quantityPlusButton;
        [SerializeField] private Button _quantityMaxButton;
        [SerializeField] private TextMeshProUGUI _quantityText;
        [SerializeField] private Button _tradeButton;
        [SerializeField] private TextMeshProUGUI _tradeButtonText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private CanvasGroup _statusCanvas;

        private readonly List<MerchantListing> _listings = new();
        private readonly List<UIMerchantItemSlot> _spawnedSlots = new();
        private readonly List<KeyValuePair<int, ItemInstance>> _sellCandidates = new();

        private IMerchantService _merchant;
        private IUIInventoryService _inventory;
        private MerchantTradeMode _mode;
        private int _selectedIndex = -1;
        private int _quantity = 1;
        private Tween _goldTween;
        private Sequence _statusSequence;

        protected override bool BlocksLowerInput => true;

        protected override void Awake()
        {
            base.Awake();
            _closeButton.onClick.AddListener(CloseSession);
            _quantityMinusButton.onClick.AddListener(DecreaseQuantity);
            _quantityPlusButton.onClick.AddListener(IncreaseQuantity);
            _quantityMaxButton.onClick.AddListener(SetMaximumQuantity);
            _tradeButton.onClick.AddListener(ExecuteTrade);
            _tradeTabs.SelectionChanged += SelectTradeMode;
            ConfigureTabShortcuts(mainTabs: _tradeTabs);
        }

        protected override void OnShow()
        {
            base.OnShow();
            _merchant = Services.Get<IMerchantService>();
            _inventory = UISvc.Inventory;
            if (_merchant?.ActiveCatalog == null || _inventory == null)
            {
                Hide();
                return;
            }

            _merchant.OnSessionChanged += RefreshAfterStateChanged;
            _merchant.OnSessionClosed += HideAfterSessionClosed;
            _merchant.OnTradeCompleted += ShowTradeReceipt;
            _inventory.OnInventoryChanged += RefreshAfterStateChanged;
            _inventory.OnGoldChanged += RefreshGold;

            _merchantName.text = _merchant.ActiveCatalog.DisplayName;
            SetStatus(string.Empty, false);
            _tradeTabs.Select(0);
        }

        protected override void OnHide()
        {
            UnsubscribeEvents();
            if (_merchant?.IsSessionOpen == true)
                _merchant.CloseMerchant();
            KillFeedbackTweens();
            base.OnHide();
        }

        protected override void OnDispose()
        {
            UnsubscribeEvents();
            _closeButton?.onClick.RemoveListener(CloseSession);
            _quantityMinusButton?.onClick.RemoveListener(DecreaseQuantity);
            _quantityPlusButton?.onClick.RemoveListener(IncreaseQuantity);
            _quantityMaxButton?.onClick.RemoveListener(SetMaximumQuantity);
            _tradeButton?.onClick.RemoveListener(ExecuteTrade);
            if (_tradeTabs != null)
                _tradeTabs.SelectionChanged -= SelectTradeMode;
            KillFeedbackTweens();
            base.OnDispose();
        }

        public override bool PerformBackFunction()
        {
            CloseSession();
            return false;
        }

        /// <summary>동적 목록 슬롯이 가리키는 품목을 상세 패널에 선택한다.</summary>
        public void SelectListing(int index)
        {
            if (index < 0 || index >= _listings.Count)
                return;

            _selectedIndex = index;
            _quantity = 1;
            for (int i = 0; i < _spawnedSlots.Count; i++)
                _spawnedSlots[i].SetSelected(i == index);

            RefreshDetail();
        }

        private void SelectTradeMode(int index)
        {
            _mode = index == 0 ? MerchantTradeMode.Buy : MerchantTradeMode.Sell;
            _selectedIndex = -1;
            _quantity = 1;
            SetStatus(string.Empty, false);
            RefreshContent();
        }

        private void RefreshContent()
        {
            ListingIdentity previousSelection = GetSelectedIdentity();
            ClearListings();
            RefreshGold();

            if (_merchant?.ActiveCatalog == null)
                return;

            if (_mode == MerchantTradeMode.Buy)
                BuildBuyListings();
            else
                BuildSellListings();

            int restoredIndex = FindListing(previousSelection);
            if (restoredIndex < 0 && _listings.Count > 0)
                restoredIndex = 0;

            _emptyState.SetActive(_listings.Count == 0);
            if (_listings.Count == 0)
            {
                _emptyStateText.text = _mode == MerchantTradeMode.Buy
                    ? "지금 살 수 있는 물건이 없습니다."
                    : "이 상인이 사는 물건을 가지고 있지 않습니다.";
                _detailPanel.SetActive(false);
                RebuildNavigation();
                return;
            }

            SelectListing(restoredIndex);
            RebuildNavigation();
            UIFocusNavigation.ResetScrollToTop(_listScroll);
        }

        private void BuildBuyListings()
        {
            IReadOnlyList<MerchantOffer> offers = _merchant.ActiveCatalog.Offers;
            for (int i = 0; i < offers.Count; i++)
            {
                MerchantOffer offer = offers[i];
                if (offer == null || !offer.CanBuy)
                    continue;

                int remaining = _merchant.GetRemainingStock(offer.ItemId);
                string secondary = remaining < 0
                    ? "항상 준비됨"
                    : remaining == 0 ? "품절" : $"남은 수량 {remaining:N0}";
                AddListing(new MerchantListing(
                    offer,
                    offer.Item,
                    offer.ItemId,
                    offer.BuyPrice,
                    remaining,
                    secondary));
            }
        }

        private void BuildSellListings()
        {
            _sellCandidates.Clear();
            foreach (KeyValuePair<int, ItemInstance> pair in _inventory.ItemDict)
            {
                if (pair.Value?.data == null
                    || !_merchant.ActiveCatalog.TryGetOffer(pair.Value.data.itemId, out MerchantOffer offer)
                    || !offer.CanSell)
                {
                    continue;
                }

                _sellCandidates.Add(pair);
            }

            _sellCandidates.Sort(CompareSellCandidates);
            for (int i = 0; i < _sellCandidates.Count; i++)
            {
                KeyValuePair<int, ItemInstance> pair = _sellCandidates[i];
                MerchantOffer offer = GetOffer(pair.Value.data.itemId);
                bool equipped = _inventory.GetEquippingCharacters(pair.Key).Count > 0;
                string secondary = equipped
                    ? "장착 중"
                    : $"보유 {pair.Value.count:N0}";
                AddListing(new MerchantListing(
                    offer,
                    pair.Value.data,
                    pair.Key,
                    offer.SellPrice,
                    pair.Value.count,
                    secondary));
            }
        }

        private void AddListing(MerchantListing listing)
        {
            int index = _listings.Count;
            _listings.Add(listing);
            UIMerchantItemSlot slot = Instantiate(_itemSlotPrefab, _listContent);
            slot.Initialize(index, listing.Item, listing.UnitPrice, listing.Secondary, this);
            _spawnedSlots.Add(slot);
        }

        private void ClearListings()
        {
            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                if (_spawnedSlots[i] != null)
                    Destroy(_spawnedSlots[i].gameObject);
            }
            _spawnedSlots.Clear();
            _listings.Clear();
        }

        private void RefreshDetail()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _listings.Count)
            {
                _detailPanel.SetActive(false);
                return;
            }

            MerchantListing listing = _listings[_selectedIndex];
            _detailPanel.SetActive(true);
            _detailName.text = listing.Item.itemName;
            _detailDescription.text = listing.Item.itemDescription;
            _detailPrice.text = $"개당 {listing.UnitPrice:N0} G";
            _detailIcon.sprite = listing.Item.icon;
            _detailIcon.enabled = listing.Item.icon != null;
            _detailAvailability.text = GetAvailabilityLabel(listing);
            _quantity = Mathf.Clamp(_quantity, 1, Mathf.Max(1, GetMaximumQuantity(listing)));
            RefreshTradeControls();
        }

        private void RefreshTradeControls()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _listings.Count)
                return;

            MerchantListing listing = _listings[_selectedIndex];
            int maximum = GetMaximumQuantity(listing);
            _quantityText.text = _quantity.ToString("N0");
            _quantityMinusButton.interactable = _quantity > 1;
            _quantityPlusButton.interactable = maximum > 0 && _quantity < maximum;
            _quantityMaxButton.interactable = maximum > 1 && _quantity < maximum;

            MerchantTradeResult availability = GetAvailability(listing, _quantity);
            _tradeButton.interactable = availability == MerchantTradeResult.Success;
            _tradeButtonText.text = _mode == MerchantTradeMode.Buy ? "구매" : "판매";
            if (availability != MerchantTradeResult.Success)
                SetStatus(GetResultMessage(availability), false);
            else
                SetStatus(string.Empty, false);
        }

        private int GetMaximumQuantity(MerchantListing listing)
        {
            return _mode == MerchantTradeMode.Buy
                ? _merchant.GetMaxBuyQuantity(listing.Item.itemId)
                : listing.AvailableQuantity;
        }

        private MerchantTradeResult GetAvailability(MerchantListing listing, int quantity)
        {
            return _mode == MerchantTradeMode.Buy
                ? _merchant.GetBuyAvailability(listing.Item.itemId, quantity)
                : _merchant.GetSellAvailability(listing.InventorySlotKey, quantity);
        }

        private void DecreaseQuantity()
        {
            if (_quantity <= 1)
                return;
            _quantity--;
            RefreshTradeControls();
        }

        private void IncreaseQuantity()
        {
            if (_selectedIndex < 0)
                return;
            int maximum = GetMaximumQuantity(_listings[_selectedIndex]);
            if (_quantity >= maximum)
                return;
            _quantity++;
            RefreshTradeControls();
        }

        private void SetMaximumQuantity()
        {
            if (_selectedIndex < 0)
                return;
            _quantity = Mathf.Max(1, GetMaximumQuantity(_listings[_selectedIndex]));
            RefreshTradeControls();
        }

        private void ExecuteTrade()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _listings.Count)
                return;

            MerchantListing listing = _listings[_selectedIndex];
            MerchantTradeResult result = _mode == MerchantTradeMode.Buy
                ? _merchant.TryBuy(listing.Item.itemId, _quantity)
                : _merchant.TrySell(listing.InventorySlotKey, _quantity);

            if (result != MerchantTradeResult.Success)
                SetStatus(GetResultMessage(result), true);
        }

        private void ShowTradeReceipt(MerchantTradeReceipt receipt)
        {
            string verb = receipt.Mode == MerchantTradeMode.Buy ? "샀습니다" : "팔았습니다";
            SetStatus($"{receipt.Item.itemName} {receipt.Quantity:N0}개를 {verb}.", true);
            PulseGold();
        }

        private void RefreshAfterStateChanged()
        {
            if (IsVisible)
                RefreshContent();
        }

        private void RefreshGold()
        {
            if (_goldText != null && _inventory != null)
                _goldText.text = $"{_inventory.Gold:N0} G";
        }

        private void PulseGold()
        {
            if (_goldPanel == null)
                return;

            _goldTween?.Kill();
            Vector3 baseScale = Vector3.one;
            _goldPanel.localScale = baseScale;
            _goldTween = DOTween.To(
                    () => _goldPanel.localScale,
                    value => _goldPanel.localScale = value,
                    baseScale * 1.08f,
                    0.1f)
                .SetEase(Ease.OutQuad)
                .SetLoops(2, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void SetStatus(string message, bool animate)
        {
            _statusText.text = message ?? string.Empty;
            if (_statusCanvas == null)
                return;

            _statusSequence?.Kill();
            _statusCanvas.alpha = string.IsNullOrEmpty(message) ? 0f : 1f;
            if (!animate || string.IsNullOrEmpty(message))
                return;

            _statusCanvas.alpha = 0f;
            _statusSequence = DOTween.Sequence().SetUpdate(true);
            _statusSequence.Append(DOTween.To(
                () => _statusCanvas.alpha,
                value => _statusCanvas.alpha = value,
                1f,
                0.12f));
        }

        private void RebuildNavigation()
        {
            var tabs = new List<Selectable>();
            for (int i = 0; i < _tradeTabs.TabCount; i++)
            {
                Button tab = _tradeTabs.GetTab(i)?.Button;
                if (tab != null)
                    tabs.Add(tab);
            }
            UIFocusNavigation.ConfigureHorizontal(tabs, true);

            var itemSelectables = new List<Selectable>();
            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                if (_spawnedSlots[i]?.Selectable != null)
                    itemSelectables.Add(_spawnedSlots[i].Selectable);
            }
            UIFocusNavigation.ConfigureVertical(itemSelectables, true);

            Selectable[] actions =
            {
                _quantityMinusButton,
                _quantityPlusButton,
                _quantityMaxButton,
                _tradeButton,
                _closeButton,
            };
            UIFocusNavigation.ConfigureHorizontal(actions);

            Selectable firstItem = itemSelectables.Count > 0 ? itemSelectables[0] : null;
            Selectable firstAction = UIFocusNavigation.FirstNavigable(actions);
            Selectable selectedTab = _tradeTabs.GetTab(_tradeTabs.SelectedIndex)?.Button;
            for (int i = 0; i < tabs.Count; i++)
            {
                Navigation navigation = tabs[i].navigation;
                navigation.selectOnDown = firstItem ?? firstAction;
                tabs[i].navigation = navigation;
            }
            for (int i = 0; i < itemSelectables.Count; i++)
            {
                Navigation navigation = itemSelectables[i].navigation;
                if (i == 0)
                    navigation.selectOnUp = selectedTab;
                navigation.selectOnRight = firstAction;
                itemSelectables[i].navigation = navigation;
            }
            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i] == null)
                    continue;
                Navigation navigation = actions[i].navigation;
                navigation.selectOnLeft = firstItem;
                actions[i].navigation = navigation;
            }

            SetDefaultFocus(firstItem ?? selectedTab ?? firstAction, IsVisible);
        }

        private ListingIdentity GetSelectedIdentity()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _listings.Count)
                return default;
            MerchantListing listing = _listings[_selectedIndex];
            return new ListingIdentity(listing.Item.itemId, listing.InventorySlotKey);
        }

        private int FindListing(ListingIdentity identity)
        {
            if (!identity.IsValid)
                return -1;
            for (int i = 0; i < _listings.Count; i++)
            {
                MerchantListing listing = _listings[i];
                if (listing.Item.itemId == identity.ItemId
                    && listing.InventorySlotKey == identity.InventorySlotKey)
                {
                    return i;
                }
            }
            return -1;
        }

        private string GetAvailabilityLabel(MerchantListing listing)
        {
            if (_mode == MerchantTradeMode.Buy)
            {
                return listing.AvailableQuantity < 0
                    ? $"보유 {_inventory.GetItemCount(listing.Item.itemId):N0} · 재고 제한 없음"
                    : $"보유 {_inventory.GetItemCount(listing.Item.itemId):N0} · 남은 수량 {listing.AvailableQuantity:N0}";
            }

            bool equipped = _inventory.GetEquippingCharacters(listing.InventorySlotKey).Count > 0;
            return equipped
                ? $"보유 {listing.AvailableQuantity:N0} · 장착 중"
                : $"판매 가능 {listing.AvailableQuantity:N0}";
        }

        private static int CompareSellCandidates(
            KeyValuePair<int, ItemInstance> left,
            KeyValuePair<int, ItemInstance> right)
        {
            int nameComparison = string.Compare(
                left.Value.data.itemName,
                right.Value.data.itemName,
                System.StringComparison.CurrentCulture);
            return nameComparison != 0 ? nameComparison : left.Key.CompareTo(right.Key);
        }

        private MerchantOffer GetOffer(int itemId)
        {
            _merchant.ActiveCatalog.TryGetOffer(itemId, out MerchantOffer offer);
            return offer;
        }

        private static string GetResultMessage(MerchantTradeResult result)
        {
            return result switch
            {
                MerchantTradeResult.NoActiveMerchant => "거래가 끝났습니다.",
                MerchantTradeResult.InvalidQuantity => "수량을 다시 확인해 주세요.",
                MerchantTradeResult.ItemUnavailable => "이 상인은 그 물건을 거래하지 않습니다.",
                MerchantTradeResult.OutOfStock => "남은 물건이 부족합니다.",
                MerchantTradeResult.NotEnoughGold => "골드가 부족합니다.",
                MerchantTradeResult.InventoryCapacityExceeded => "가방에 담을 수 없습니다.",
                MerchantTradeResult.NotEnoughItems => "팔 수량이 부족합니다.",
                MerchantTradeResult.EquippedItem => "장착 중인 장비는 팔 수 없습니다.",
                MerchantTradeResult.GoldCapacityExceeded => "골드를 더 보관할 수 없습니다.",
                MerchantTradeResult.InvalidPrice => "가격을 확인할 수 없습니다.",
                MerchantTradeResult.TransactionFailed => "거래를 마치지 못했습니다. 다시 시도해 주세요.",
                _ => string.Empty,
            };
        }

        private void CloseSession()
        {
            if (_merchant?.IsSessionOpen == true)
                _merchant.CloseMerchant();
            else
                Hide();
        }

        private void HideAfterSessionClosed()
        {
            if (IsVisible)
                Hide();
        }

        private void UnsubscribeEvents()
        {
            if (_merchant != null)
            {
                _merchant.OnSessionChanged -= RefreshAfterStateChanged;
                _merchant.OnSessionClosed -= HideAfterSessionClosed;
                _merchant.OnTradeCompleted -= ShowTradeReceipt;
            }
            if (_inventory != null)
            {
                _inventory.OnInventoryChanged -= RefreshAfterStateChanged;
                _inventory.OnGoldChanged -= RefreshGold;
            }
        }

        private void KillFeedbackTweens()
        {
            _goldTween?.Kill();
            _goldTween = null;
            _statusSequence?.Kill();
            _statusSequence = null;
        }

        private sealed class MerchantListing
        {
            public MerchantListing(
                MerchantOffer offer,
                ItemSO item,
                int inventorySlotKey,
                int unitPrice,
                int availableQuantity,
                string secondary)
            {
                Offer = offer;
                Item = item;
                InventorySlotKey = inventorySlotKey;
                UnitPrice = unitPrice;
                AvailableQuantity = availableQuantity;
                Secondary = secondary;
            }

            public MerchantOffer Offer { get; }
            public ItemSO Item { get; }
            public int InventorySlotKey { get; }
            public int UnitPrice { get; }
            public int AvailableQuantity { get; }
            public string Secondary { get; }
        }

        private readonly struct ListingIdentity
        {
            public ListingIdentity(int itemId, int inventorySlotKey)
            {
                ItemId = itemId;
                InventorySlotKey = inventorySlotKey;
            }

            public int ItemId { get; }
            public int InventorySlotKey { get; }
            public bool IsValid => ItemId > 0;
        }
    }
}
