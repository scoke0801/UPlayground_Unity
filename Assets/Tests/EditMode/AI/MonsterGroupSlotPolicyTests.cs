using NUnit.Framework;
using UPlayGround.Group;

namespace UPlayGround.AI.Tests
{
    public sealed class MonsterGroupSlotPolicyTests
    {
        [TestCase(2, 0.5f, 3, 1)]
        [TestCase(5, 0.5f, 3, 3)]
        [TestCase(8, 0.5f, 3, 3)]
        public void 생존_인원_비율과_Cap으로_슬롯_수를_계산한다(
            int aliveCount,
            float ratio,
            int cap,
            int expected)
        {
            Assert.That(
                MonsterGroupSlotPolicy.CalculateLimit(aliveCount, ratio, cap, false),
                Is.EqualTo(expected));
        }

        [Test]
        public void 최근_그룹_피격은_근접_슬롯을_하나_줄이되_최소_하나는_유지한다()
        {
            Assert.That(MonsterGroupSlotPolicy.CalculateLimit(6, 0.5f, 3, true), Is.EqualTo(2));
            Assert.That(MonsterGroupSlotPolicy.CalculateLimit(1, 0.5f, 3, true), Is.EqualTo(1));
        }

        [TestCase(0.8f, 0.7f, 0.1f, true)]
        [TestCase(0.72f, 0.7f, 0.1f, false)]
        public void 슬롯_교체_마진은_Fitness_스케일에_정규화된다(
            float requester,
            float owner,
            float margin,
            bool expected)
        {
            Assert.That(
                MonsterGroupSlotPolicy.HasNormalizedTakeoverMargin(requester, owner, margin),
                Is.EqualTo(expected));
        }
    }
}
