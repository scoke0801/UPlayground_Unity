using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Item;
using UPlayGround.Data.Merchant;
using UPlayGround.Data.Save;
using UPlayGround.Economy;
using UPlayGround.UI;

namespace UPlayGround.Manager
{
    /// <summary>상인 세션, 저장되는 한정 재고, 구매·판매 트랜잭션을 한 경계에서 관리한다.</summary>
    public sealed class MerchantManager : BaseManager<MerchantManager>, IManager, ISaveable, IMerchantService
    {
        private const string MerchantUiKey = "Merchant";

        private readonly Dictionary<MerchantStockKey, int> _limitedStocks = new();

        public event Action OnSessionChanged;
        public event Action OnSessionClosed;
        public event Action<MerchantTradeReceipt> OnTradeCompleted;

        public MerchantCatalogSO ActiveCatalog { get; private set; }
        public bool IsSessionOpen => ActiveCatalog != null;

        public void Init()
        {
            SaveManager.Instance.RegisterSaveable(this);
        }

        public void AfterInit() { }
        public void OnUpdate() { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }
        public void OnSceneChanged(string sceneType) => CloseMerchant();

        public void Dispose()
        {
            ActiveCatalog = null;
            OnSessionChanged = null;
            OnSessionClosed = null;
            OnTradeCompleted = null;
        }

        /// <summary>검증된 카탈로그를 활성화하고 상점 화면을 연다.</summary>
        public bool TryOpenMerchant(MerchantCatalogSO catalog)
        {
            if (catalog == null)
            {
                Debug.LogError("[MerchantManager] 상점을 열 수 없습니다: 카탈로그가 비어 있습니다.");
                return false;
            }
            if (!catalog.TryValidate(out string error))
            {
                Debug.LogError($"[MerchantManager] 상점을 열 수 없습니다: {error}", catalog);
                return false;
            }

            if (IsSessionOpen)
                CloseMerchant();

            ActiveCatalog = catalog;
            GameObject merchantUi = UISvc.UI?.ShowUI(MerchantUiKey);
            if (merchantUi == null)
            {
                ActiveCatalog = null;
                Debug.LogError("[MerchantManager] Merchant UI 프리팹을 열 수 없습니다.");
                return false;
            }

            OnSessionChanged?.Invoke();
            return true;
        }

        /// <summary>현재 상점 세션을 끝내고 상호작용 소유자와 UI에 종료를 알린다.</summary>
        public void CloseMerchant()
        {
            if (!IsSessionOpen)
                return;

            ActiveCatalog = null;
            OnSessionClosed?.Invoke();
        }

        public int GetRemainingStock(int itemId)
        {
            if (!TryGetOffer(itemId, out MerchantOffer offer))
                return 0;
            if (offer.StockMode == MerchantStockMode.Unlimited)
                return -1;

            var key = new MerchantStockKey(ActiveCatalog.MerchantId, itemId);
            return _limitedStocks.TryGetValue(key, out int remaining)
                ? remaining
                : offer.InitialStock;
        }

        public int GetMaxBuyQuantity(int itemId)
        {
            if (!TryGetOffer(itemId, out MerchantOffer offer) || !offer.CanBuy)
                return 0;

            IInventoryService inventory = Svc.Inventory;
            if (inventory == null)
                return 0;

            return MerchantTradeCalculator.GetMaxAffordableQuantity(
                inventory.Gold,
                offer.BuyPrice,
                GetRemainingStock(itemId));
        }

        public MerchantTradeResult GetBuyAvailability(int itemId, int quantity)
        {
            if (!IsSessionOpen)
                return MerchantTradeResult.NoActiveMerchant;
            if (quantity <= 0)
                return MerchantTradeResult.InvalidQuantity;
            if (!TryGetOffer(itemId, out MerchantOffer offer) || !offer.CanBuy)
                return MerchantTradeResult.ItemUnavailable;
            if (!MerchantTradeCalculator.TryCalculateTotal(offer.BuyPrice, quantity, out int totalPrice))
                return MerchantTradeResult.InvalidPrice;

            int remainingStock = GetRemainingStock(itemId);
            if (remainingStock >= 0 && remainingStock < quantity)
                return MerchantTradeResult.OutOfStock;

            IInventoryService inventory = Svc.Inventory;
            if (inventory == null)
                return MerchantTradeResult.TransactionFailed;
            if (inventory.Gold < totalPrice)
                return MerchantTradeResult.NotEnoughGold;
            if (!inventory.CanAddItem(itemId, quantity))
                return MerchantTradeResult.InventoryCapacityExceeded;

            return MerchantTradeResult.Success;
        }

        /// <summary>골드 차감과 아이템 지급이 모두 성공할 때만 구매를 확정한다.</summary>
        public MerchantTradeResult TryBuy(int itemId, int quantity)
        {
            MerchantTradeResult availability = GetBuyAvailability(itemId, quantity);
            if (availability != MerchantTradeResult.Success)
                return availability;

            MerchantOffer offer = GetRequiredOffer(itemId);
            MerchantTradeCalculator.TryCalculateTotal(offer.BuyPrice, quantity, out int totalPrice);
            IInventoryService inventory = Svc.Inventory;

            if (!inventory.TrySpendGold(totalPrice))
                return MerchantTradeResult.NotEnoughGold;

            if (!inventory.TryAddItem(itemId, quantity))
            {
                if (!inventory.TryAddGold(totalPrice))
                    Debug.LogError("[MerchantManager] 구매 실패 후 골드 롤백에 실패했습니다.");
                return MerchantTradeResult.TransactionFailed;
            }

            DecreaseLimitedStock(offer, quantity);
            CompleteTrade(MerchantTradeMode.Buy, offer.Item, quantity, totalPrice);
            return MerchantTradeResult.Success;
        }

        public MerchantTradeResult GetSellAvailability(int inventorySlotKey, int quantity)
        {
            if (!IsSessionOpen)
                return MerchantTradeResult.NoActiveMerchant;
            if (quantity <= 0)
                return MerchantTradeResult.InvalidQuantity;

            IInventoryService inventory = Svc.Inventory;
            ItemInstance instance = inventory?.GetInventoryItemBySlotKey(inventorySlotKey);
            if (instance?.data == null || instance.count < quantity)
                return MerchantTradeResult.NotEnoughItems;
            if (!TryGetOffer(instance.data.itemId, out MerchantOffer offer) || !offer.CanSell)
                return MerchantTradeResult.ItemUnavailable;
            if (instance.data is EquipmentSO && quantity != 1)
                return MerchantTradeResult.InvalidQuantity;
            if (inventory.IsInventorySlotEquipped(inventorySlotKey))
                return MerchantTradeResult.EquippedItem;
            if (!MerchantTradeCalculator.TryCalculateTotal(offer.SellPrice, quantity, out int totalPrice))
                return MerchantTradeResult.InvalidPrice;
            if (!inventory.CanAddGold(totalPrice))
                return MerchantTradeResult.GoldCapacityExceeded;

            return MerchantTradeResult.Success;
        }

        /// <summary>선택 슬롯 제거와 골드 지급이 모두 성공할 때만 판매를 확정한다.</summary>
        public MerchantTradeResult TrySell(int inventorySlotKey, int quantity)
        {
            MerchantTradeResult availability = GetSellAvailability(inventorySlotKey, quantity);
            if (availability != MerchantTradeResult.Success)
                return availability;

            IInventoryService inventory = Svc.Inventory;
            ItemInstance instance = inventory.GetInventoryItemBySlotKey(inventorySlotKey);
            MerchantOffer offer = GetRequiredOffer(instance.data.itemId);
            MerchantTradeCalculator.TryCalculateTotal(offer.SellPrice, quantity, out int totalPrice);

            if (!inventory.TryRemoveInventorySlotInstances(
                    inventorySlotKey,
                    quantity,
                    out List<ItemInstance> removedItems))
            {
                return MerchantTradeResult.NotEnoughItems;
            }

            if (!inventory.TryAddGold(totalPrice))
            {
                inventory.RestoreItemInstances(removedItems);
                return MerchantTradeResult.TransactionFailed;
            }

            CompleteTrade(MerchantTradeMode.Sell, offer.Item, quantity, totalPrice);
            return MerchantTradeResult.Success;
        }

        private void CompleteTrade(
            MerchantTradeMode mode,
            ItemSO item,
            int quantity,
            int totalPrice)
        {
            var receipt = new MerchantTradeReceipt(mode, item, quantity, totalPrice);
            OnSessionChanged?.Invoke();
            OnTradeCompleted?.Invoke(receipt);
        }

        private bool TryGetOffer(int itemId, out MerchantOffer offer)
        {
            if (ActiveCatalog != null)
                return ActiveCatalog.TryGetOffer(itemId, out offer);

            offer = null;
            return false;
        }

        private MerchantOffer GetRequiredOffer(int itemId)
        {
            ActiveCatalog.TryGetOffer(itemId, out MerchantOffer offer);
            return offer;
        }

        private void DecreaseLimitedStock(MerchantOffer offer, int quantity)
        {
            if (offer.StockMode != MerchantStockMode.Limited)
                return;

            var key = new MerchantStockKey(ActiveCatalog.MerchantId, offer.ItemId);
            _limitedStocks[key] = GetRemainingStock(offer.ItemId) - quantity;
        }

        public void ExportSaveData(GameSaveData saveData)
        {
            saveData.merchant ??= new MerchantSaveData();
            saveData.merchant.limitedStocks ??= new List<MerchantStockSaveEntry>();
            saveData.merchant.limitedStocks.Clear();

            foreach (KeyValuePair<MerchantStockKey, int> stock in _limitedStocks)
            {
                saveData.merchant.limitedStocks.Add(new MerchantStockSaveEntry
                {
                    merchantId = stock.Key.MerchantId,
                    itemId = stock.Key.ItemId,
                    remainingStock = stock.Value,
                });
            }
        }

        public void ImportSaveData(GameSaveData saveData)
        {
            _limitedStocks.Clear();
            foreach (MerchantStockSaveEntry stock in
                     saveData?.merchant?.limitedStocks ?? new List<MerchantStockSaveEntry>())
            {
                if (stock == null || string.IsNullOrWhiteSpace(stock.merchantId) || stock.itemId <= 0)
                    continue;

                _limitedStocks[new MerchantStockKey(stock.merchantId, stock.itemId)] =
                    Mathf.Max(0, stock.remainingStock);
            }
        }

        public void ResetForNewGame()
        {
            CloseMerchant();
            _limitedStocks.Clear();
        }

        private readonly struct MerchantStockKey : IEquatable<MerchantStockKey>
        {
            public MerchantStockKey(string merchantId, int itemId)
            {
                MerchantId = merchantId;
                ItemId = itemId;
            }

            public string MerchantId { get; }
            public int ItemId { get; }

            public bool Equals(MerchantStockKey other) =>
                ItemId == other.ItemId && string.Equals(MerchantId, other.MerchantId, StringComparison.Ordinal);

            public override bool Equals(object obj) => obj is MerchantStockKey other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(MerchantId, ItemId);
        }
    }
}
