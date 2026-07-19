using NUnit.Framework;
using UPlayGround.Data.Codex;

namespace UPlayGround.Ability.Tests
{
    public sealed class MonsterCodexCalculatorTests
    {
        private static readonly MonsterCodexBonus Bonus = new()
        {
            maxExpBonus = 0.2f,
            maxDamageDealtBonus = 0.1f,
            maxDamageTakenReduce = 0.15f,
        };

        [TestCase(0, 10, 0f)]
        [TestCase(5, 10, 0.5f)]
        [TestCase(10, 10, 1f)]
        [TestCase(20, 10, 1f)]
        [TestCase(1, 0, 1f)]
        public void 기록률은_처치목표에_따라_0과1사이로_계산된다(
            long kills,
            int target,
            float expected)
        {
            Assert.That(
                MonsterCodexCalculator.GetRecordRatio(kills, target),
                Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void 보정은_기록률에_따라_선형으로_증감한다()
        {
            Assert.That(
                MonsterCodexCalculator.GetExpMultiplier(0.5f, Bonus),
                Is.EqualTo(1.1f).Within(0.0001f));
            Assert.That(
                MonsterCodexCalculator.GetDamageDealtMultiplier(0.5f, Bonus),
                Is.EqualTo(1.05f).Within(0.0001f));
            Assert.That(
                MonsterCodexCalculator.GetDamageTakenMultiplier(0.5f, Bonus),
                Is.EqualTo(0.925f).Within(0.0001f));
        }

        [Test]
        public void 입는피해_감소는_음수로_내려가지_않는다()
        {
            MonsterCodexBonus excessive = Bonus;
            excessive.maxDamageTakenReduce = 3f;

            Assert.That(
                MonsterCodexCalculator.GetDamageTakenMultiplier(1f, excessive),
                Is.EqualTo(MonsterCodexCalculator.DamageTakenSafetyFloor));
        }
    }
}
