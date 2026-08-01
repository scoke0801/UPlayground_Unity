using NUnit.Framework;
using UPlayGround.Components;

namespace UPlayGround.AI.Tests
{
    public sealed class EnemyAggroPolicyTests
    {
        [Test]
        public void 직접_감지한_타겟은_추적_반경을_벗어나면_해제한다()
        {
            Assert.That(Evaluate(targetDistance: 15.1f), Is.True);
        }

        [Test]
        public void 외부_경보_타겟은_유예시간_동안_먼_거리에서도_유지한다()
        {
            Assert.That(Evaluate(targetDistance: 40f, external: true, externalElapsed: 5f), Is.False);
            Assert.That(Evaluate(targetDistance: 40f, external: true, externalElapsed: 6.1f), Is.True);
        }

        [Test]
        public void 시야_상실은_유예시간_후_해제한다()
        {
            Assert.That(Evaluate(hasLineOfSight: false, lostSightElapsed: 2.9f), Is.False);
            Assert.That(Evaluate(hasLineOfSight: false, lostSightElapsed: 3.1f), Is.True);
        }

        [Test]
        public void 스폰_앵커_추격_한계를_넘으면_즉시_해제한다()
        {
            Assert.That(Evaluate(distanceFromAnchor: 30.1f), Is.True);
        }

        private static bool Evaluate(
            float targetDistance = 5f,
            float distanceFromAnchor = 5f,
            bool external = false,
            float externalElapsed = 0f,
            bool hasLineOfSight = true,
            float lostSightElapsed = 0f)
        {
            return EnemyAggroPolicy.ShouldLoseTarget(
                targetAlive: true,
                targetDistance,
                distanceFromAnchor,
                external,
                externalElapsed,
                hasLineOfSight,
                lostSightElapsed,
                lostTargetRadius: 15f,
                maxChaseDistanceFromAnchor: 30f,
                externalTargetMaxDuration: 6f,
                lostSightGraceDuration: 3f);
        }
    }
}
