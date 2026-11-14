using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.Enemy;
using UPlayGround.MovementController;
using UPlayGround.Components;
using UPlayGround.State;
using System.Reflection;

namespace UPlayGround.AI.Tests
{
    public sealed class EnemyCombatMovementTests
    {
        [TestCase(19, true)]
        [TestCase(0, false)]
        public void 이동_경로는_모터의_비충돌_레이어를_제외하고_실제_장애물은_막는다(int obstacleLayer, bool expectedClear)
        {
            var root = new GameObject("CombatPathCollisionTest");
            root.SetActive(false);
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var capsule = root.AddComponent<CapsuleCollider>();
                capsule.radius = 0.5f;
                capsule.height = 2f;
                capsule.center = Vector3.up;
                var movement = root.AddComponent<EnemyMovementController>();
                var motorProperty = typeof(ActorMovementController).GetProperty("Motor");
                var motorType = motorProperty.PropertyType;
                var motor = root.AddComponent(motorType);
                motorType.GetField("Capsule").SetValue(motor, capsule);
                motorType.GetField("CollidableLayers").SetValue(motor, (LayerMask)~(1 << 19));
                motorProperty.SetValue(movement, motor);
                obstacle.layer = obstacleLayer;
                obstacle.transform.position = new Vector3(0f, 1f, 1.5f);
                Physics.SyncTransforms();

                var query = typeof(EnemyMovementController).GetMethod("HasClearCombatPath", BindingFlags.Instance | BindingFlags.NonPublic);
                var clear = (bool)query.Invoke(movement, new object[] { Vector3.forward, 2f, new EnemyCombatMovementSettings() });
                Assert.That(clear, Is.EqualTo(expectedClear));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(obstacle);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase(ActorStateId.Chase)]
        [TestCase(ActorStateId.Retreat)]
        public void 복귀_중에도_보호_모션의_종료_전환을_거절해_상태에_가두지_않는다(ActorStateId exitId)
        {
            var root = new GameObject("CombatCompletionTest");
            root.SetActive(false);
            var behavior = ScriptableObject.CreateInstance<EnemyBehaviorSO>();
            try
            {
                var ai = root.AddComponent<EnemyAIController>();
                typeof(EnemyAIController).GetField("_behaviorData", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ai, behavior);
                var movement = root.AddComponent<EnemyMovementController>();
                typeof(EnemyMovementController).GetField("_combatAI", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, ai);
                Assert.That(movement.TransitionToState(new ProtectedProbeState(movement)), Is.True);
                typeof(EnemyMovementController).GetField("_returningHome", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, true);
                var exit = new EntryProbeState(movement, exitId);
                Assert.That(movement.TransitionToState(exit), Is.True);
                Assert.That(exit.Entered, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(behavior); }
        }

        private sealed class ProtectedProbeState : GameActorState
        {
            public ProtectedProbeState(ActorMovementController controller) : base(controller) { }
            public override ActorStateId StateId => ActorStateId.Counter;
            public override bool BlocksBehaviorTree => true;
            public override bool CanTransitionState(ActorStateId fromState) => true;
            public override void OnEnter(GameActorState fromState) { }
            public override void OnExit(GameActorState toState) { }
        }

        [Test]
        public void 감지기만_재활성화하면_귀속을_보존하고_풀_재스폰은_갱신한다()
        {
            var root = new GameObject("CombatAnchorTest");
            try
            {
                var detection = root.AddComponent<EnemyDetection>();
                detection.SetCombatAnchor(Vector3.right * 5f);
                root.transform.position = Vector3.right * 12f;
                var enable = typeof(EnemyDetection).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
                var disable = typeof(EnemyDetection).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);
                disable.Invoke(detection, null);
                enable.Invoke(detection, null);
                Assert.That(detection.AggroAnchorPosition, Is.EqualTo(Vector3.right * 5f));
                root.SetActive(false);
                disable.Invoke(detection, null);
                root.transform.position = Vector3.right * 30f;
                enable.Invoke(detection, null);
                Assert.That(detection.AggroAnchorPosition, Is.EqualTo(Vector3.right * 30f));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(true, ActorStateId.Attack, false)]
        [TestCase(true, ActorStateId.Chase, false)]
        [TestCase(false, ActorStateId.Retreat, false)]
        [TestCase(false, ActorStateId.Counter, false)]
        [TestCase(true, ActorStateId.Hit, true)]
        [TestCase(false, ActorStateId.Death, true)]
        public void 복귀와_회복_제약은_직접_전환도_검사하되_강제_상태는_허용한다(bool returning, ActorStateId id, bool expected)
        {
            var root = new GameObject("CombatEntryTest");
            root.SetActive(false);
            var behavior = ScriptableObject.CreateInstance<EnemyBehaviorSO>();
            try
            {
                var ai = root.AddComponent<EnemyAIController>();
                typeof(EnemyAIController).GetField("_behaviorData", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ai, behavior);
                var movement = root.AddComponent<EnemyMovementController>();
                typeof(EnemyMovementController).GetField("_combatAI", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, ai);
                typeof(EnemyMovementController).GetField("_returningHome", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, returning);
                if (!returning)
                {
                    var budget = new EnemyDodgeBudget();
                    budget.Reset();
                    budget.RecordEnded(Time.time, behavior.combatMovement);
                    typeof(EnemyMovementController).GetField("_dodgeBudget", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, budget);
                }
                var state = new EntryProbeState(movement, id);
                Assert.That(movement.TransitionToState(state), Is.EqualTo(expected));
                Assert.That(state.Entered, Is.EqualTo(expected));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(behavior); }
        }

        private sealed class EntryProbeState : GameActorState
        {
            private readonly ActorStateId _id;
            public bool Entered { get; private set; }
            public override ActorStateId StateId => _id;
            public EntryProbeState(ActorMovementController controller, ActorStateId id) : base(controller) { _id = id; }
            public override bool CanTransitionState(ActorStateId fromState) => true;
            public override void OnEnter(GameActorState fromState) { Entered = true; }
        }

        [Test]
        public void 컨트롤러_생성은_Unity_네이티브_경로를_생성자에서_호출하지_않는다()
        {
            var root = new GameObject("CombatMovementConstructionTest");
            root.SetActive(false);
            try { Assert.DoesNotThrow(() => root.AddComponent<EnemyMovementController>()); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(8f, 10f, 10f)]
        [TestCase(10f, 10f, 10f)]
        [TestCase(12f, 10f, 12f)]
        public void 자발_이동은_허용_반경을_늘리지_않는다(float positionX, float velocityX, float expectedX)
        {
            var position = new Vector3(positionX, 7f, 0f);
            var velocity = EnemyCombatMovementPolicy.ConstrainVelocity(position, Vector3.zero, new Vector3(velocityX, -2f, 0f), 10f, 1f);
            Assert.That(position.x + velocity.x, Is.EqualTo(expectedX).Within(0.00001f));
            Assert.That(velocity.y, Is.EqualTo(-2f));
        }

        [Test]
        public void 경계의_접선_이동도_다음_위치가_구역_안에_남는다()
        {
            var position = new Vector3(10f, 0f, 0f);
            var velocity = EnemyCombatMovementPolicy.ConstrainVelocity(position, Vector3.zero, new Vector3(0f, 3f, 10f), 10f, 1f);
            Assert.That(EnemyCombatMovementPolicy.PlanarDistance(position + velocity, Vector3.zero), Is.LessThanOrEqualTo(10.00001f));
            Assert.That(velocity.y, Is.EqualTo(3f));
        }

        [Test]
        public void 이미_밖에_있어도_안쪽_복귀를_순간이동으로_바꾸지_않는다()
        {
            var velocity = EnemyCombatMovementPolicy.ConstrainVelocity(new Vector3(14f, 0f, 0f), Vector3.zero, Vector3.left, 10f, 1f);
            Assert.That(velocity, Is.EqualTo(Vector3.left));
        }

        [Test]
        public void 복귀는_몸과_타겟의_해제_조건을_모두_기다린다()
        {
            var settings = new EnemyCombatMovementSettings { territoryRadius = 10f, boundaryMargin = 2f, returnArrivalDistance = 1f };
            Assert.That(EnemyCombatMovementPolicy.ShouldReturn(new Vector3(11f, 0f, 0f), Vector3.zero, false, default, false, settings), Is.True);
            Assert.That(EnemyCombatMovementPolicy.ShouldReturn(new Vector3(3f, 0f, 0f), Vector3.zero, false, default, true, settings), Is.True);
            Assert.That(EnemyCombatMovementPolicy.ShouldReturn(Vector3.zero, Vector3.zero, true, new Vector3(9f, 0f, 0f), true, settings), Is.True);
            Assert.That(EnemyCombatMovementPolicy.ShouldReturn(Vector3.zero, Vector3.zero, true, new Vector3(8f, 0f, 0f), true, settings), Is.False);
        }

        [Test]
        public void 실패한_회피_조회는_예산을_소모하지_않는다()
        {
            var settings = new EnemyCombatMovementSettings();
            var budget = new EnemyDodgeBudget();
            budget.Reset();
            Assert.That(budget.CanStart(0f, settings), Is.True);
            Assert.That(budget.CanStart(0f, settings), Is.True);
            Assert.That(budget.ConsecutiveCount, Is.Zero);
        }

        [Test]
        public void 성공한_회피는_쿨다운과_회복_구간과_연속_상한을_모두_지킨다()
        {
            var settings = new EnemyCombatMovementSettings { dodgeCooldown = 2f, dodgeRecoveryDuration = 1f, maxConsecutiveDodges = 1, dodgeChainResetDuration = 4f };
            var budget = new EnemyDodgeBudget();
            budget.Reset();
            budget.RecordStarted(0f, settings);
            budget.RecordEnded(0.5f, settings);
            Assert.That(budget.CanStart(0.6f, settings), Is.False);
            Assert.That(budget.CanStart(2f, settings), Is.False);
            Assert.That(budget.CanStart(4.49f, settings), Is.False);
            Assert.That(budget.CanStart(4.5f, settings), Is.True);
            budget.RecordStarted(4.5f, settings);
            Assert.That(budget.ConsecutiveCount, Is.EqualTo(1));
        }

        [Test]
        public void 활성화_초기화는_이전_스폰의_회피_예산을_남기지_않는다()
        {
            var settings = new EnemyCombatMovementSettings();
            var budget = new EnemyDodgeBudget();
            budget.Reset();
            budget.RecordStarted(100f, settings);
            budget.RecordEnded(101f, settings);
            budget.Reset();
            Assert.That(budget.CanStart(0f, settings), Is.True);
            Assert.That(budget.ConsecutiveCount, Is.Zero);
        }
    }
}
