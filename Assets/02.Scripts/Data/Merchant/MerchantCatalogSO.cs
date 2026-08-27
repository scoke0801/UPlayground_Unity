using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Item;

namespace UPlayGround.Data.Merchant
{
    public enum MerchantStockMode
    {
        Unlimited = 0,
        Limited,
    }

    public enum MerchantTradeMode
    {
        Buy = 0,
        Sell,
    }

    public enum MerchantTradeResult
    {
        Success = 0,
        NoActiveMerchant,
        InvalidQuantity,
        ItemUnavailable,
        OutOfStock,
        NotEnoughGold,
        InventoryCapacityExceeded,
        NotEnoughItems,
        EquippedItem,
        GoldCapacityExceeded,
        InvalidPrice,
        TransactionFailed,
    }

    [Serializable]
    public sealed class MerchantOffer
    {
        [SerializeField] private ItemSO _item;
        [SerializeField, Min(0)] private int _buyPrice;
        [SerializeField, Min(0)] private int _sellPrice;
        [SerializeField] private MerchantStockMode _stockMode;
        [SerializeField, Min(0)] private int _initialStock;

        public ItemSO Item => _item;
        public int ItemId => _item != null ? _item.itemId : 0;
        public int BuyPrice => _buyPrice;
        public int SellPrice => _sellPrice;
        public MerchantStockMode StockMode => _stockMode;
        public int InitialStock => _stockMode == MerchantStockMode.Limited ? _initialStock : -1;
        public bool CanBuy => _item != null && _buyPrice > 0;
        public bool CanSell => _item != null && _sellPrice > 0;
    }

    /// <summary>상인 한 명의 판매·매입 품목과 한정 재고 시작값을 정의한다.</summary>
    [CreateAssetMenu(fileName = "Merchant_", menuName = "UPlayGround/상인/카탈로그")]
    public sealed class MerchantCatalogSO : ScriptableObject
    {
        [SerializeField] private string _merchantId;
        [SerializeField] private string _displayName;
        [SerializeField] private List<MerchantOffer> _offers = new();

        public string MerchantId => _merchantId;
        public string DisplayName => _displayName;
        public IReadOnlyList<MerchantOffer> Offers => _offers;

        /// <summary>아이템 ID가 일치하는 거래 조건을 찾는다.</summary>
        public bool TryGetOffer(int itemId, out MerchantOffer offer)
        {
            for (int i = 0; i < _offers.Count; i++)
            {
                MerchantOffer candidate = _offers[i];
                if (candidate != null && candidate.ItemId == itemId)
                {
                    offer = candidate;
                    return true;
                }
            }

            offer = null;
            return false;
        }

        /// <summary>저장 키와 품목 구성이 거래에 안전한지 검사한다.</summary>
        public bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(_merchantId))
            {
                error = "상인 ID가 비어 있습니다.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(_displayName))
            {
                error = "플레이어에게 보여 줄 상인 이름이 비어 있습니다.";
                return false;
            }
            if (_offers == null || _offers.Count == 0)
            {
                error = "거래 품목이 없습니다.";
                return false;
            }

            var itemIds = new HashSet<int>();
            for (int i = 0; i < _offers.Count; i++)
            {
                MerchantOffer offer = _offers[i];
                if (offer?.Item == null)
                {
                    error = $"{i + 1}번째 품목의 아이템이 비어 있습니다.";
                    return false;
                }
                if (!itemIds.Add(offer.ItemId))
                {
                    error = $"아이템 ID {offer.ItemId}가 두 번 등록되어 있습니다.";
                    return false;
                }
                if (!offer.CanBuy && !offer.CanSell)
                {
                    error = $"{offer.Item.itemName}의 판매가와 매입가가 모두 0입니다.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private void OnValidate()
        {
            _merchantId = _merchantId?.Trim();
            _displayName = _displayName?.Trim();
        }
    }

    /// <summary>성공한 상점 거래의 방향·품목·수량·총액을 전달한다.</summary>
    public readonly struct MerchantTradeReceipt
    {
        public MerchantTradeReceipt(
            MerchantTradeMode mode,
            ItemSO item,
            int quantity,
            int totalPrice)
        {
            Mode = mode;
            Item = item;
            Quantity = quantity;
            TotalPrice = totalPrice;
        }

        public MerchantTradeMode Mode { get; }
        public ItemSO Item { get; }
        public int Quantity { get; }
        public int TotalPrice { get; }
    }
}
