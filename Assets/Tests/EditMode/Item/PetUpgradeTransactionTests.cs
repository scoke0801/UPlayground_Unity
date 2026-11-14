using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.Pet;

namespace UPlayGround.Item.Tests
{
    public sealed class PetUpgradeTransactionTests
    {
        [Test]
        public void 정책이_바뀐_거래는_골드와_레벨을_그대로_둔다()
        {
            var skill = new PetSkillDefinition { skillId = PetSkillCatalogSO.DefenseBuff };
            skill.levels.Add(new PetSkillLevel { upgradeCost = 100 }); skill.levels.Add(new PetSkillLevel());
            var request = new PetUpgradeRequest(skill.skillId, 1, 100, skill.GetRevision());
            skill.triggerPolicy.internalCooldown = 15f;
            var type = Type.GetType("UPlayGround.Manager.InventoryManager, Assembly-CSharp", true);
            var root = new GameObject("정책 변경 거래");
            try
            {
                var inventory = root.AddComponent(type);
                var wallet = type.GetField("_goldWallet", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(inventory);
                wallet.GetType().GetMethod("Restore").Invoke(wallet, new object[] { 1000 });
                int level = 1;
                var spend = type.GetMethod("TrySpendGold", new[] { typeof(int), typeof(Func<bool>) });
                bool committed = (bool)spend.Invoke(inventory, new object[] { 100, (Func<bool>)(() =>
                {
                    if (!PetUpgradeRules.CanUpgrade(skill, level, request)) return false;
                    level++; return true;
                }) });
                Assert.That(committed, Is.False);
                Assert.That(type.GetProperty("Gold").GetValue(inventory), Is.EqualTo(1000));
                Assert.That(level, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        [TestCase(true, 900)]
        [TestCase(false, 1000)]
        public void 구매상태와_골드를_같이_확정하거나_원복한다(bool accepted, int expectedGold)
        {
            var type = Type.GetType("UPlayGround.Manager.InventoryManager, Assembly-CSharp", true);
            var root = new GameObject("UpgradeTransaction");
            try
            {
                var inventory = root.AddComponent(type);
                var wallet = type.GetField("_goldWallet", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(inventory);
                wallet.GetType().GetMethod("Restore").Invoke(wallet, new object[] { 1000 });
                var spend = type.GetMethod("TrySpendGold", new[] { typeof(int), typeof(Func<bool>) });
                int level = 1;
                bool result = (bool)spend.Invoke(inventory, new object[] { 100, (Func<bool>)(() => { if (accepted) level++; return accepted; }) });
                Assert.That(result, Is.EqualTo(accepted));
                Assert.That(type.GetProperty("Gold").GetValue(inventory), Is.EqualTo(expectedGold));
                Assert.That(level, Is.EqualTo(accepted ? 2 : 1));
                Assert.That((bool)spend.Invoke(inventory, new object[] { 9999, (Func<bool>)(() => { level++; return true; }) }), Is.False);
                Assert.That(level, Is.EqualTo(accepted ? 2 : 1));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
