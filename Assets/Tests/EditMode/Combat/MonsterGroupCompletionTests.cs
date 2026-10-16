using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Gameplay.Ability;
using UPlayGround.Group;

namespace UPlayGround.Combat.Tests
{
    /// <summary>저장 복원용 개체 제거를 실제 전멸과 구분한다.</summary>
    public sealed class MonsterGroupCompletionTests
    {
        [TestCase(100f, 0)]
        [TestCase(0f, 1)]
        public void 마지막멤버가_실제사망했을때만_전멸을_알린다(float health, int expected)
        {
            var root = new GameObject("GroupCompletionTest");
            root.SetActive(false);
            try
            {
                var group = root.AddComponent<MonsterGroupController>();
                var monster = root.AddComponent<MonsterActor>();
                var ability = root.AddComponent<AbilitySystemComponent>();
                ability.EnsureInitialized();
                typeof(GameActor).GetProperty(nameof(GameActor.AbilitySystem)).SetValue(monster, ability);
                ability.Attributes.SetBase(global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth, 100f);
                ability.Attributes.SetBase(global::UPlayGround.Data.Stat.Attributes.Vital.Health, health);
                typeof(MonsterGroupController).GetField("_isActivated", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(group, true);
                int defeated = 0;
                group.OnGroupDefeated += () => defeated++;
                group.RegisterMember(monster, MemberPriority.Normal);
                group.UnregisterMember(monster);
                Assert.AreEqual(expected, defeated);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
