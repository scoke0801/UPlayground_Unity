using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;
using UPlayGround.Data.Path;
using UPlayGround.Data.Quest;
using UPlayGround.Data.Reward;

namespace UPlayGround.Content.Tests
{
    public sealed class RewardSystemDataTests
    {
        private const string PotionPath =
            "Assets/10.Datas/Item/Consume/companion_exp_potion.asset";
        private const string ItemDatabasePath =
            "Assets/10.Datas/Path/ItemDatabase.asset";
        private const string StartingInventoryPath =
            "Assets/10.Datas/Item/StartingInventory.asset";

        [Test]
        public void 퀘스트_보상은_공용_보상_묶음으로_변환된다()
        {
            var questReward = new QuestRewardData
            {
                gold = 10,
                exp = 20,
                items =
                {
                    new QuestItemReward { itemId = 30, count = 2 },
                },
            };

            RewardData reward = questReward.ToRewardData();

            Assert.AreEqual(10, reward.gold);
            Assert.AreEqual(20, reward.exp);
            Assert.AreEqual(30, reward.items[0].itemId);
            Assert.AreEqual(2, reward.items[0].count);
        }

        [Test]
        public void 보상_데이터는_중복_아이템을_거부한다()
        {
            var reward = new RewardData
            {
                items =
                {
                    new ItemRewardData { itemId = 10, count = 1 },
                    new ItemRewardData { itemId = 10, count = 2 },
                },
            };

            Assert.AreEqual(
                RewardDataValidationResult.DuplicateItem,
                reward.Validate());
        }

        [Test]
        public void 경험의_물약은_동료_성장용으로_등록된다()
        {
            ConsumableSO potion = AssetDatabase.LoadAssetAtPath<ConsumableSO>(PotionPath);
            ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);
            StartingInventorySO startingInventory =
                AssetDatabase.LoadAssetAtPath<StartingInventorySO>(StartingInventoryPath);

            Assert.IsNotNull(potion);
            Assert.IsNotNull(database);
            Assert.IsNotNull(startingInventory);
            Assert.AreEqual(50000, potion.itemId);
            Assert.AreEqual(ItemType.CONSUMABLE, potion.itemType);
            Assert.AreEqual(ConsumableEffectType.CompanionExperience, potion.effectType);
            Assert.Greater(potion.experienceAmount, 0);
            Assert.IsTrue(potion.RequiresCharacterTarget);
            Assert.IsFalse(potion.IsQuickSlotCompatible);
            Assert.AreEqual(1, database.AllItems.Count(item => item == potion));
            Assert.IsTrue(startingInventory.items.Any(
                entry => entry.item == potion && entry.count > 0));
        }
    }
}
