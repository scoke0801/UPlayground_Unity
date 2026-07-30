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

        [Test]
        public void SeedRotationNextUpdate_상태회전후_목표회전을한번만확정한다()
        {
            Quaternion stateRotation = Quaternion.Euler(0f, 30f, 0f);
            Quaternion targetRotation = Quaternion.Euler(0f, 120f, 0f);
            _controller.TransitionToState(
                new RotationTestState(_controller, stateRotation));
            _controller.SeedRotationNextUpdate(targetRotation);

            Quaternion currentRotation = Quaternion.identity;
            _controller.UpdateRotation(ref currentRotation, 0.02f);
            Assert.That(
                Quaternion.Angle(currentRotation, targetRotation),
                Is.LessThan(0.01f));

            currentRotation = Quaternion.identity;
            _controller.UpdateRotation(ref currentRotation, 0.02f);
            Assert.That(
                Quaternion.Angle(currentRotation, stateRotation),
                Is.LessThan(0.01f));
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

        private sealed class RotationTestState : GameActorState
        {
            private readonly Quaternion _rotation;

            public RotationTestState(
                ActorMovementController controller,
                Quaternion rotation)
                : base(controller)
            {
                _rotation = rotation;
            }

            public override string StateName => "RotationTest";
            public override bool CanTransitionState(string stateName) => true;

            public override void UpdateRotation(
                ref Quaternion currentRotation,
                float deltaTime)
            {
                currentRotation = _rotation;
            }
        }
    }
}
