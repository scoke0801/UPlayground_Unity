using NUnit.Framework;
using UnityEngine;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Movement.Tests
{
    /// <summary>정렬 방향은 접근 사거리와 무관하고 수직 차이는 이동을 만들지 않는다.</summary>
    public sealed class EncounterAlignmentTests
    {
        [TestCase(2f)]
        [TestCase(5f)]
        [TestCase(100f)]
        public void TargetDistanceDoesNotLimitFacing(float distance)
        {
            Assert.That(PlayerMovementController.TryGetAlignmentDirection(Vector3.zero,
                new Vector3(0f, 4f, -distance), Vector3.up, out var direction), Is.True);
            Assert.That(Vector3.Dot(direction, Vector3.back), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void CoincidentPlanarTargetDoesNotInventDirection()
        {
            Assert.That(PlayerMovementController.TryGetAlignmentDirection(Vector3.zero,
                Vector3.up, Vector3.up, out _), Is.False);
        }

        [TestCase(ActorStateId.Guard, true)]
        [TestCase(ActorStateId.Attack, true)]
        [TestCase(ActorStateId.Charge, true)]
        [TestCase(ActorStateId.Ultimate, true)]
        [TestCase(ActorStateId.Idle, false)]
        [TestCase(ActorStateId.Dodge, false)]
        public void FreeMovementAndDodgeAreExcluded(ActorStateId state, bool expected)
            => Assert.That(PlayerMovementController.IsAlignedAction(state), Is.EqualTo(expected));
    }
}
