using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;
using NUnit.Framework;
using UPlayGround.State;

namespace UPlayGround.Movement.Tests
{
    public class ActorStateTransitionGuardTests
    {
        [Test]
        public void ActorStateId_숫자값은_중복되지않는다()
        {
            var values = (ActorStateId[])Enum.GetValues(typeof(ActorStateId));
            var unique = new HashSet<int>();

            foreach (ActorStateId value in values)
                Assert.That(unique.Add((int)value), Is.True, $"중복 ActorStateId: {value}");
        }

        [Test]
        public void StateName은_StateId의_디버그표현이다()
        {
            PlayerIdleState state = CreateWithoutConstructor<PlayerIdleState>();

            Assert.That(state.StateId, Is.EqualTo(ActorStateId.Idle));
            Assert.That(state.StateName, Is.EqualTo(nameof(ActorStateId.Idle)));
        }

        [Test]
        public void 적_공중전환유예는_공통가상멤버를_재정의한다()
        {
            MethodInfo getter = typeof(EnemyActorState)
                .GetProperty(
                    "AirborneGracePeriod",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetMethod;

            Assert.That(getter, Is.Not.Null);
            Assert.That(
                getter.GetBaseDefinition().DeclaringType,
                Is.EqualTo(typeof(GameActorState)));
        }

        [TestCase(ActorStateId.Death, true)]
        [TestCase(ActorStateId.Grabbed, true)]
        [TestCase(ActorStateId.Idle, false)]
        [TestCase(ActorStateId.Attack, false)]
        public void 플레이어_넉다운_전이가드_행렬(ActorStateId fromState, bool expected)
        {
            PlayerKnockdownState state = CreateWithoutConstructor<PlayerKnockdownState>();
            Assert.That(state.CanTransitionState(fromState), Is.EqualTo(expected));
        }

        [TestCase(ActorStateId.Death, false)]
        [TestCase(ActorStateId.Grabbed, false)]
        [TestCase(ActorStateId.SpecialBreakVictim, false)]
        [TestCase(ActorStateId.Idle, true)]
        [TestCase(ActorStateId.Attack, true)]
        [TestCase(ActorStateId.Hit, true)]
        public void 몬스터_스턴_전이가드_행렬(ActorStateId fromState, bool expected)
        {
            EnemyStunState state = CreateWithoutConstructor<EnemyStunState>();
            Assert.That(state.CanTransitionState(fromState), Is.EqualTo(expected));
        }

        [TestCase(ActorStateId.Death, false)]
        [TestCase(ActorStateId.Grabbed, false)]
        [TestCase(ActorStateId.SpecialBreakVictim, false)]
        [TestCase(ActorStateId.Idle, true)]
        [TestCase(ActorStateId.Attack, true)]
        [TestCase(ActorStateId.Hit, true)]
        public void 몬스터_넉다운_전이가드_행렬(ActorStateId fromState, bool expected)
        {
            EnemyKnockdownState state = CreateWithoutConstructor<EnemyKnockdownState>();
            Assert.That(state.CanTransitionState(fromState), Is.EqualTo(expected));
        }

        [TestCase(ActorStateId.Death, false)]
        [TestCase(ActorStateId.Grabbed, false)]
        [TestCase(ActorStateId.SpecialBreakVictim, false)]
        [TestCase(ActorStateId.Idle, true)]
        [TestCase(ActorStateId.Attack, true)]
        public void 몬스터_잡힘_전이가드_행렬(ActorStateId fromState, bool expected)
        {
            EnemyGrabbedState state = CreateWithoutConstructor<EnemyGrabbedState>();
            Assert.That(state.CanTransitionState(fromState), Is.EqualTo(expected));
        }

        [Test]
        public void 몬스터_사망상태는_Idle_복귀를_차단한다()
        {
            EnemyDeathState death = CreateWithoutConstructor<EnemyDeathState>();
            EnemyIdleState idle = CreateWithoutConstructor<EnemyIdleState>();

            Assert.That(death.BlocksExitTo(idle), Is.True);
            Assert.That(death.BlocksExitTo(death), Is.False);
        }

#pragma warning disable SYSLIB0050
        private static T CreateWithoutConstructor<T>() where T : class
            => (T)FormatterServices.GetUninitializedObject(typeof(T));
#pragma warning restore SYSLIB0050
    }
}
