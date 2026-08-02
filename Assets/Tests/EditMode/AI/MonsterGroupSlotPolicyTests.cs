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

        [Test]
        public void 근접_진형의_도착_경계는_공격_가능_거리_안에_유지된다()
        {
            MonsterGroupSlotPolicy.ClampFormationToAttackRange(
                requestedRadius: 1.05f,
                requestedArrivalTolerance: 0.65f,
                maxAttackRange: 1.25f,
                entryMargin: 0.05f,
                minimumRadius: 0.8f,
                out float radius,
                out float arrivalTolerance);

            Assert.That(radius, Is.EqualTo(1.05f).Within(0.0001f));
            Assert.That(arrivalTolerance, Is.EqualTo(0.15f).Within(0.0001f));
            Assert.That(radius + arrivalTolerance, Is.LessThanOrEqualTo(1.2f));
        }

        [Test]
        public void 선호_진형_반경도_공격_거리보다_크면_함께_축소된다()
        {
            MonsterGroupSlotPolicy.ClampFormationToAttackRange(
                requestedRadius: 2f,
                requestedArrivalTolerance: 0.65f,
                maxAttackRange: 1.25f,
                entryMargin: 0.05f,
                minimumRadius: 0.8f,
                out float radius,
                out float arrivalTolerance);

            Assert.That(radius, Is.EqualTo(1.15f).Within(0.0001f));
            Assert.That(arrivalTolerance, Is.EqualTo(0.05f).Within(0.0001f));
        }

        [Test]
        public void 공격_거리가_개인_공간보다_짧아도_타깃_캡슐을_파고들지_않는다()
        {
            MonsterGroupSlotPolicy.ClampFormationToAttackRange(
                requestedRadius: 1.2f,
                requestedArrivalTolerance: 0.65f,
                maxAttackRange: 0.6f,
                entryMargin: 0.05f,
                minimumRadius: 0.8f,
                out float radius,
                out float arrivalTolerance);

            // 사거리가 개인 공간보다 짧으면 하한이 우선한다. 그래도 도착 오차는 최소치로 줄어든다.
            Assert.That(radius, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(arrivalTolerance, Is.EqualTo(0.05f).Within(0.0001f));
        }

        [Test]
        public void 공격_거리를_모르면_요청값을_하한만_적용해_그대로_쓴다()
        {
            MonsterGroupSlotPolicy.ClampFormationToAttackRange(
                requestedRadius: 0.3f,
                requestedArrivalTolerance: 0.65f,
                maxAttackRange: 0f,
                entryMargin: 0.05f,
                minimumRadius: 0.8f,
                out float radius,
                out float arrivalTolerance);

            Assert.That(radius, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(arrivalTolerance, Is.EqualTo(0.65f).Within(0.0001f));
        }
    }
}
