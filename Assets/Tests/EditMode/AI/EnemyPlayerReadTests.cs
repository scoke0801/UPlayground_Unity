using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Group;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.AI.Tests
{
    public sealed class EnemyPlayerReadTests
    {
        [Test]
        public void 차지를_발사한_뒤에는_차지_중_태그를_해제한다()
        {
            var root = new GameObject("ChargeReadTagTest");
            root.SetActive(false);
            try
            {
                var controller = root.AddComponent<ActorMovementController>();
                var state = new PlayerChargeState(controller);
                Assert.That((state.StateTags & ActorStateTag.CastingOrCharging) != 0, Is.True);
                typeof(PlayerChargeState).GetField("_isFired", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(state, true);
                Assert.That((state.StateTags & ActorStateTag.CastingOrCharging) == 0, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(119f, false)]
        [TestCase(120f, true)]
        [TestCase(180f, true)]
        [TestCase(0f, false)]
        public void 후방_각도_경계를_포함해_판정한다(float yaw, bool expected)
        {
            var toTarget = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Assert.That(EnemyTacticalMemory.IsWithinRearArc(Vector3.forward, toTarget, 120f), Is.EqualTo(expected));
        }

        [TestCase(10f, 10f, 2f, true)]
        [TestCase(10f, 11.99f, 2f, true)]
        [TestCase(10f, 12f, 2f, false)]
        [TestCase(10f, 9f, 2f, false)]
        [TestCase(10f, 10f, 0f, false)]
        public void 가드_파괴_기억은_기한과_시각을_지킨다(float last, float now, float duration, bool expected)
            => Assert.That(EnemyTacticalMemory.IsGuardBreakMemoryActive(last, now, duration), Is.EqualTo(expected));

        [Test]
        public void 슬롯_조회는_점유와_대기열을_변경하지_않는다()
        {
            var root = new GameObject("SlotReadTest");
            root.SetActive(false);
            try
            {
                var group = root.AddComponent<MonsterGroupController>();
                var member = root.AddComponent<MonsterActor>();
                group.RegisterMember(member, MemberPriority.Normal);
                for (int i = 0; i < 3; i++) group.CanRequestAttackSlot(member, AttackType.Melee);
                foreach (string field in new[] { "_meleeSlotOwners", "_meleeSlotCandidates", "_meleeSlotAcquiredTimes" })
                {
                    var dictionary = (IDictionary)typeof(MonsterGroupController).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(group);
                    Assert.That(dictionary.Count, Is.Zero, field);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
