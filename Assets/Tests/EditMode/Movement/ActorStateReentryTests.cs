using NUnit.Framework;
using UnityEngine;
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

            bool firstTransitioned = _controller.TryTransitionToState(first);
            bool secondTransitioned = _controller.TryTransitionToState(second);

            Assert.That(firstTransitioned, Is.True);
            Assert.That(secondTransitioned, Is.False);
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
        public void TryTransitionToState_현재상태가_이탈을막으면_false를반환한다()
        {
            var first = new TestState(
                _controller,
                allowsSameTypeReentry: true,
                blocksExit: true);
            var second = new TestState(
                _controller,
                allowsSameTypeReentry: true);

            Assert.That(_controller.TryTransitionToState(first), Is.True);
            Assert.That(_controller.TryTransitionToState(second), Is.False);
            Assert.That(_controller.CurrentState, Is.SameAs(first));
            Assert.That(first.ExitCount, Is.Zero);
            Assert.That(second.EnterCount, Is.Zero);
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
            private readonly bool _blocksExit;

            public TestState(
                ActorMovementController controller,
                bool allowsSameTypeReentry,
                int executionType = 0,
                bool blocksExit = false)
                : base(controller)
            {
                _allowsSameTypeReentry = allowsSameTypeReentry;
                _executionType = executionType;
                _blocksExit = blocksExit;
            }

            public override ActorStateId StateId => ActorStateId.None;
            public override bool AllowsSameTypeReentry => _allowsSameTypeReentry;
            public override bool CanReenterFrom(GameActorState currentState)
                => currentState is TestState current
                   && current._executionType == _executionType;
            public override bool BlocksExitTo(GameActorState newState) => _blocksExit;
            public int EnterCount { get; private set; }
            public int ExitCount { get; private set; }

            public override bool CanTransitionState(ActorStateId fromState) => true;

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
