using NUnit.Framework;
using UPlayGround.Simulation;

namespace UPlayGround.AI.Tests
{
    public sealed class ActorSimulationPolicyTests
    {
        [Test]
        public void 플레이어가_없으면_항상_활성이다()
        {
            ActorSimulationState result = Evaluate(
                ActorSimulationState.Suspended,
                hasPlayer: false,
                hasLease: false,
                canSuspend: true,
                distanceSquared: 10000f,
                time: 10f,
                lastActivatedTime: 0f,
                out var reason);

            Assert.That(result, Is.EqualTo(ActorSimulationState.Active));
            Assert.That(reason, Is.EqualTo(ActorSimulationTransitionReason.PlayerUnavailable));
        }

        [Test]
        public void 활성_임대는_거리보다_우선한다()
        {
            ActorSimulationState result = Evaluate(
                ActorSimulationState.Suspended,
                hasPlayer: true,
                hasLease: true,
                canSuspend: true,
                distanceSquared: 10000f,
                time: 10f,
                lastActivatedTime: 0f,
                out _);

            Assert.That(result, Is.EqualTo(ActorSimulationState.Active));
        }

        [Test]
        public void 정지_중_안전_조건이_깨지면_거리와_무관하게_활성화한다()
        {
            ActorSimulationState result = Evaluate(
                ActorSimulationState.Suspended,
                hasPlayer: true,
                hasLease: false,
                canSuspend: false,
                distanceSquared: 10000f,
                time: 10f,
                lastActivatedTime: 0f,
                out var reason);

            Assert.That(result, Is.EqualTo(ActorSimulationState.Active));
            Assert.That(reason, Is.EqualTo(ActorSimulationTransitionReason.Unsafe));
        }

        [Test]
        public void Wake와_Sleep_거리_사이에서는_현재_상태를_유지한다()
        {
            ActorSimulationState active = Evaluate(
                ActorSimulationState.Active, true, false, true, 60f * 60f,
                10f, 0f, out _);
            ActorSimulationState suspended = Evaluate(
                ActorSimulationState.Suspended, true, false, true, 60f * 60f,
                10f, 0f, out _);

            Assert.That(active, Is.EqualTo(ActorSimulationState.Active));
            Assert.That(suspended, Is.EqualTo(ActorSimulationState.Suspended));
        }

        [Test]
        public void 최소_활성_시간이_지난_뒤에만_Sleep_거리에서_정지한다()
        {
            ActorSimulationState early = Evaluate(
                ActorSimulationState.Active, true, false, true, 70f * 70f,
                0.5f, 0f, out _);
            ActorSimulationState afterMinimum = Evaluate(
                ActorSimulationState.Active, true, false, true, 70f * 70f,
                1.1f, 0f, out _);

            Assert.That(early, Is.EqualTo(ActorSimulationState.Active));
            Assert.That(afterMinimum, Is.EqualTo(ActorSimulationState.Suspended));
        }

        private static ActorSimulationState Evaluate(
            ActorSimulationState current,
            bool hasPlayer,
            bool hasLease,
            bool canSuspend,
            float distanceSquared,
            float time,
            float lastActivatedTime,
            out ActorSimulationTransitionReason reason) =>
            ActorSimulationPolicy.Evaluate(
                current,
                hasPlayer,
                hasLease,
                canSuspend,
                distanceSquared,
                55f * 55f,
                65f * 65f,
                time,
                lastActivatedTime,
                1f,
                out reason);
    }
}
