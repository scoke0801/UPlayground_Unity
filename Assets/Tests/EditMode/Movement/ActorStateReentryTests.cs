using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Movement.Tests
{
    public class ActorStateReentryTests
    {
        private GameObject _gameObject;
        private ActorMovementController _controller;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject(nameof(ActorStateReentryTests));
            _gameObject.SetActive(false);
            LogAssert.Expect(
                LogType.Error,
                "[ActorMovementController] KinematicCharacterMotor를 찾을 수 없습니다.");
            _controller = _gameObject.AddComponent<ActorMovementController>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void TransitionToState_일반상태의_같은타입재진입은_무시한다()
        {
            var first = new TestState(_controller, allowsSameTypeReentry: false);
            var second = new TestState(_controller, allowsSameTypeReentry: false);

            _controller.TransitionToState(first);
            _controller.TransitionToState(second);

            Assert.That(_controller.CurrentState, Is.SameAs(first));
            Assert.That(first.ExitCount, Is.Zero);
            Assert.That(second.EnterCount, Is.Zero);
        }

        [Test]
        public void TransitionToState_허용상태의_같은타입재진입은_새인스턴스로전환한다()
        {
            var first = new TestState(_controller, allowsSameTypeReentry: true);
            var second = new TestState(_controller, allowsSameTypeReentry: true);

            _controller.TransitionToState(first);
            _controller.TransitionToState(second);

            Assert.That(_controller.CurrentState, Is.SameAs(second));
            Assert.That(first.ExitCount, Is.EqualTo(1));
            Assert.That(second.EnterCount, Is.EqualTo(1));
        }

        [Test]
        public void TransitionToState_실행종류가다른_같은상태재진입은_무시한다()
        {
            var first = new TestState(
                _controller,
                allowsSameTypeReentry: true,
                executionType: 1);
            var second = new TestState(
                _controller,
                allowsSameTypeReentry: true,
                executionType: 2);

            _controller.TransitionToState(first);
            _controller.TransitionToState(second);

            Assert.That(_controller.CurrentState, Is.SameAs(first));
            Assert.That(first.ExitCount, Is.Zero);
            Assert.That(second.EnterCount, Is.Zero);
        }

        private sealed class TestState : GameActorState
        {
            private readonly bool _allowsSameTypeReentry;
            private readonly int _executionType;

            public TestState(
                ActorMovementController controller,
                bool allowsSameTypeReentry,
                int executionType = 0)
                : base(controller)
            {
                _allowsSameTypeReentry = allowsSameTypeReentry;
                _executionType = executionType;
            }

            public override string StateName => "Test";
            public override bool AllowsSameTypeReentry => _allowsSameTypeReentry;
            public override bool CanReenterFrom(GameActorState currentState)
                => currentState is TestState current
                   && current._executionType == _executionType;
            public int EnterCount { get; private set; }
            public int ExitCount { get; private set; }

            public override bool CanTransitionState(string stateName) => true;

            public override void OnEnter(GameActorState fromState)
            {
                EnterCount++;
            }

            public override void OnExit(GameActorState toState)
            {
                ExitCount++;
            }
        }
    }
}
