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

        [Test]
        public void StateMachine_전이거부시_캐시상태컨텍스트를변경하지않는다()
        {
            var machine = new ActorStateMachine(_controller);
            var blocker = new TestState(
                _controller,
                allowsSameTypeReentry: false,
                blocksExit: true,
                stateId: ActorStateId.Idle);
            var target = new ConfigurableTestState(_controller);
            machine.Register(target);
            _controller.TransitionToState(blocker);

            bool transitioned = machine.TryTransition(
                ActorStateId.Airborne,
                7);

            Assert.That(transitioned, Is.False);
            Assert.That(target.ConfigureCount, Is.Zero);
            Assert.That(_controller.CurrentState, Is.SameAs(blocker));
        }

        [Test]
        public void StateMachine_캐시상태를재사용하며_진입전컨텍스트를갱신한다()
        {
            var machine = new ActorStateMachine(_controller);
            var target = new ConfigurableTestState(_controller);
            machine.Register(target);

            Assert.That(machine.TryTransition(ActorStateId.Airborne, 1), Is.True);
            Assert.That(target.LastEnteredValue, Is.EqualTo(1));
            _controller.TransitionToState(
                new TestState(
                    _controller,
                    allowsSameTypeReentry: false,
                    stateId: ActorStateId.Idle));
            Assert.That(machine.TryTransition(ActorStateId.Airborne, 2), Is.True);

            Assert.That(_controller.CurrentState, Is.SameAs(target));
            Assert.That(target.ConfigureCount, Is.EqualTo(2));
            Assert.That(target.LastEnteredValue, Is.EqualTo(2));
        }

        [Test]
        public void StateMachine_컨텍스트상태의_무인자전이를거부한다()
        {
            var machine = new ActorStateMachine(_controller);
            machine.Register(new ConfigurableTestState(_controller));

            Assert.Throws<System.InvalidOperationException>(
                () => machine.TryTransition(ActorStateId.Airborne));
        }

        [Test]
        public void StateMachine_같은ID의_다른상태등록을거부한다()
        {
            var machine = new ActorStateMachine(_controller);
            var first = new ConfigurableTestState(_controller);
            machine.Register(first);

            Assert.Throws<System.InvalidOperationException>(
                () => machine.Register(new ConfigurableTestState(_controller)));
            Assert.That(machine.Get(ActorStateId.Airborne), Is.SameAs(first));
        }

        private sealed class TestState : GameActorState
        {
            private readonly bool _allowsSameTypeReentry;
            private readonly int _executionType;
            private readonly bool _blocksExit;
            private readonly ActorStateId _stateId;

            public TestState(
                ActorMovementController controller,
                bool allowsSameTypeReentry,
                int executionType = 0,
                bool blocksExit = false,
                ActorStateId stateId = ActorStateId.None)
                : base(controller)
            {
                _allowsSameTypeReentry = allowsSameTypeReentry;
                _executionType = executionType;
                _blocksExit = blocksExit;
                _stateId = stateId;
            }

            public override ActorStateId StateId => _stateId;
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

        private sealed class ConfigurableTestState : GameActorState,
            IConfigurableState<int>
        {
            private int _value;

            public ConfigurableTestState(ActorMovementController controller)
                : base(controller)
            {
            }

            public override ActorStateId StateId => ActorStateId.Airborne;
            public int ConfigureCount { get; private set; }
            public int LastEnteredValue { get; private set; }

            public void Configure(in int context)
            {
                _value = context;
                ConfigureCount++;
            }

            public override bool CanTransitionState(ActorStateId fromState) => true;

            public override void OnEnter(GameActorState fromState)
            {
                LastEnteredValue = _value;
            }
        }
    }
}
