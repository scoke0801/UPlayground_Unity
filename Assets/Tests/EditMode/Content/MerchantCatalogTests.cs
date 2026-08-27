using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UPlayGround.Data.Merchant;
using UPlayGround.Data.Save;

namespace UPlayGround.Content.Tests
{
    public sealed class MerchantCatalogTests
    {
        private const string CatalogPath = "Assets/10.Datas/Merchant/Merchant_Penny.asset";

        [Test]
        public void PennyCatalog_저장키와품목구성이유효하다()
        {
            MerchantCatalogSO catalog = AssetDatabase.LoadAssetAtPath<MerchantCatalogSO>(CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.TryValidate(out string error), Is.True, error);
            Assert.That(catalog.MerchantId, Is.EqualTo("merchant_penny"));
            Assert.That(catalog.Offers.Count, Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void PennyCatalog_구매가가매입가보다높고품목ID가겹치지않는다()
        {
            MerchantCatalogSO catalog = AssetDatabase.LoadAssetAtPath<MerchantCatalogSO>(CatalogPath);
            var itemIds = new HashSet<int>();

            foreach (MerchantOffer offer in catalog.Offers)
            {
                Assert.That(itemIds.Add(offer.ItemId), Is.True, $"중복 품목 ID: {offer.ItemId}");
                if (offer.CanBuy && offer.CanSell)
                    Assert.That(offer.BuyPrice, Is.GreaterThan(offer.SellPrice), offer.Item.itemName);
            }
        }

        [Test]
        public void 새세이브는_상인한정재고컨테이너를가진다()
        {
            var saveData = new GameSaveData();

            Assert.That(saveData.saveVersion, Is.EqualTo("3.3"));
            Assert.That(saveData.merchant, Is.Not.Null);
            Assert.That(saveData.merchant.limitedStocks, Is.Not.Null);
            Assert.That(saveData.merchant.limitedStocks, Is.Empty);
        }
    }
}
