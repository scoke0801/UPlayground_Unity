using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Combat.Tests
{
    /// <summary>주민 대기의 시간차·회전 범위와 서사 제어 우선순위를 검사한다.</summary>
    public sealed class NpcIdleBehaviourTests
    {
        private GameObject _root;
        private NpcBrain _brain;
        private Random.State _randomState;

        [SetUp]
        public void SetUp()
        {
            _randomState = Random.state;
            Random.InitState(7301);
            _root = new GameObject("NPC 대기 검사");
            _root.SetActive(false);
            _root.AddComponent<CapsuleCollider>();
            _brain = _root.AddComponent<NpcBrain>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_root);
            Random.state = _randomState;
        }

        [Test]
        public void 같은_배치에서도_대기시간과_바라보는_방향이_분산된다()
        {
            Quaternion home = Quaternion.Euler(0f, 180f, 0f);
            float firstWait = _brain.SamplePatrolWaitTime();
            float minWait = firstWait, maxWait = firstWait;
            float minYaw = 0f, maxYaw = 0f;
            for (int i = 0; i < 32; i++)
            {
                float wait = _brain.SamplePatrolWaitTime();
                Assert.That(wait, Is.InRange(3f * 0.55f, 3f * 1.45f));
                minWait = Mathf.Min(minWait, wait);
                maxWait = Mathf.Max(maxWait, wait);
                _brain.BeginIdleLook(home);
                Quaternion look = _brain.RotateIdleLook(home, 10f);
                float yaw = Mathf.DeltaAngle(home.eulerAngles.y, look.eulerAngles.y);
                Assert.That(Mathf.Abs(yaw), Is.LessThanOrEqualTo(40.01f));
                minYaw = Mathf.Min(minYaw, yaw);
                maxYaw = Mathf.Max(maxYaw, yaw);
            }
            Assert.That(maxWait - minWait, Is.GreaterThan(1f));
            Assert.That(minYaw, Is.LessThan(-10f));
            Assert.That(maxYaw, Is.GreaterThan(10f));
        }

        [Test]
        public void 오래_대기해도_회전이_누적되어_배치_방향을_벗어나지_않는다()
        {
            Quaternion home = Quaternion.Euler(0f, 275f, 0f);
            Quaternion rotation = home;
            _brain.BeginIdleLook(home);
            for (int i = 0; i < 500; i++)
            {
                _brain.TickIdleLook(0.2f);
                Quaternion next = _brain.RotateIdleLook(rotation, 0.2f);
                Assert.That(Quaternion.Angle(rotation, next), Is.LessThanOrEqualTo(5.01f));
                Assert.That(Quaternion.Angle(home, next), Is.LessThanOrEqualTo(40.01f));
                rotation = next;
            }
        }

        [Test]
        public void 시간_정지와_살피기_비활성화는_현재_방향을_유지한다()
        {
            Quaternion rotation = Quaternion.Euler(0f, 83f, 0f);
            _brain.BeginIdleLook(Quaternion.identity);
            Assert.That(Quaternion.Angle(rotation, _brain.RotateIdleLook(rotation, 0f)), Is.LessThan(0.01f));
            SetField(_brain, "_enableIdleLook", false);
            _brain.TickIdleLook(30f);
            Assert.That(Quaternion.Angle(rotation, _brain.RotateIdleLook(rotation, 30f)), Is.LessThan(0.01f));
        }

        [Test]
        public void 배치_복원은_이전_장소의_시선을_버린다()
        {
            _brain.BeginIdleLook(Quaternion.identity);
            Quaternion restored = Quaternion.Euler(0f, 180f, 0f);
            _brain.SetHomePose(Vector3.one, restored);
            Quaternion look = _brain.RotateIdleLook(restored, 10f);
            Assert.That(Quaternion.Angle(restored, look), Is.LessThanOrEqualTo(40.01f));
        }

        [TestCase("_routinePauses", 1)]
        [TestCase("_dialogueStageHolds", 1)]
        [TestCase("_isInteracting", true)]
        public void 대화와_배치_복원_동안_Idle과_배회가_몸을_돌리지_않는다(string field, object value)
        {
            NpcActor actor = _root.AddComponent<NpcActor>();
            var controller = _root.GetComponent<NpcMovementController>();
            var idle = new NpcIdleState(controller);
            var wander = new NpcWanderState(controller, _brain);
            SetField(idle, "npcActor", actor);
            SetField(idle, "_brain", _brain);
            SetField(wander, "npcActor", actor);
            SetField(wander, "_isWaiting", true);
            SetField(actor, field, value);
            _brain.BeginIdleLook(Quaternion.identity);
            Quaternion original = Quaternion.Euler(0f, 90f, 0f);
            Quaternion rotation = original;
            idle.UpdateRotation(ref rotation, 1f);
            wander.UpdateRotation(ref rotation, 1f);
            Assert.That(Quaternion.Angle(original, rotation), Is.LessThan(0.01f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 주변_살피기와_플레이어_인식은_대기중인_몸을_돌리지_않는다(bool noticesPlayer)
        {
            NpcActor actor = _root.AddComponent<NpcActor>();
            var controller = _root.GetComponent<NpcMovementController>();
            var idle = new NpcIdleState(controller);
            var wander = new NpcWanderState(controller, _brain);
            SetField(idle, "npcActor", actor);
            SetField(idle, "_brain", _brain);
            SetField(wander, "npcActor", actor);
            SetField(wander, "_isWaiting", true);
            var player = new GameObject("시선 대상");
            try
            {
                player.transform.position = new Vector3(2f, 0f, 2f);
                SetField(_brain, "_playerTransform", player.transform);
                _brain.BeginIdleLook(Quaternion.identity);
                SetField(_brain, "_isNoticingPlayer", noticesPlayer);
                Quaternion original = Quaternion.Euler(0f, 15f, 0f);
                Quaternion rotation = original;
                idle.UpdateRotation(ref rotation, 1f);
                wander.UpdateRotation(ref rotation, 1f);
                Assert.That(Quaternion.Angle(original, rotation), Is.LessThan(0.01f));
            }
            finally { UnityEngine.Object.DestroyImmediate(player); }
        }

        [Test]
        public void 앞쪽_플레이어만_인식하고_뒤로_가면_시선을_해제한다()
        {
            var player = new GameObject("시선 대상");
            try
            {
                SetField(_brain, "_playerTransform", player.transform);
                SetField(_brain, "_noticeDelayRange", Vector2.zero);
                player.transform.position = Vector3.forward * 2f;
                _brain.TickIdleLook(0.3f);
                Assert.That(_brain.IsNoticingPlayer, Is.True);
                player.transform.position = Vector3.back * 2f;
                _brain.TickIdleLook(0.3f);
                Assert.That(_brain.IsNoticingPlayer, Is.False);
                player.transform.position = Vector3.forward * 2f;
                _brain.TickIdleLook(0.3f);
                Assert.That(_brain.IsNoticingPlayer, Is.True);
                _brain.EndIdleLook();
                Assert.That(_brain.IsNoticingPlayer, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(player); }
        }

        [Test]
        public void 작업_모션이_있으면_주변_시선보다_작업대_방향을_우선한다()
        {
            NpcActor actor = _root.AddComponent<NpcActor>();
            var idle = new NpcIdleState(_root.GetComponent<NpcMovementController>());
            SetField(idle, "npcActor", actor);
            SetField(idle, "_brain", _brain);
            SetField(_brain, "_enableWander", false);
            var motion = ScriptableObject.CreateInstance<MotionSetAsset>();
            try
            {
                _brain.SetHomePose(Vector3.zero, Quaternion.identity);
                _brain.SetWorkMotion(motion);
                Quaternion rotation = Quaternion.identity;
                idle.UpdateRotation(ref rotation, 1f);
                Assert.That(Quaternion.Angle(Quaternion.identity, rotation), Is.LessThan(0.01f));
            }
            finally { UnityEngine.Object.DestroyImmediate(motion); }
        }

        [Test]
        public void 배회_목적지는_현재_위치와_떨어지고_생활_반경을_벗어나지_않는다()
        {
            Vector3 home = new Vector3(10000f, 2f, 10000f);
            _brain.SetHomePose(home, Quaternion.identity);
            SetField(_brain, "_patrolRadius", 3.5f);
            Vector3 current = home;
            for (int i = 0; i < 40; i++)
            {
                Assert.That(_brain.TryGetWanderPoint(current, out Vector3 next), Is.True);
                Assert.That(Vector3.Distance(current, next), Is.GreaterThan(1.2f));
                Assert.That(Vector3.Distance(home, next), Is.LessThanOrEqualTo(3.501f));
                current = next;
            }
        }

        [TestCase(false, 5f)]
        [TestCase(true, 0f)]
        [TestCase(true, 0.5f)]
        public void 고정_주민과_너무_좁은_배회는_이동_목적지를_만들지_않는다(bool enabled, float radius)
        {
            SetField(_brain, "_enableWander", enabled);
            SetField(_brain, "_patrolRadius", radius);
            Vector3 current = _root.transform.position;
            Assert.That(_brain.TryGetWanderPoint(current, out var destination), Is.False);
            Assert.That(destination, Is.EqualTo(current));
        }

        [Test]
        public void 플레이어가_머물러도_잠깐_바라본_뒤_일상으로_돌아간다()
        {
            var player = new GameObject("시선 대상");
            try
            {
                player.transform.position = Vector3.forward * 2f;
                SetField(_brain, "_playerTransform", player.transform);
                SetField(_brain, "_noticeDelayRange", Vector2.zero);
                SetField(_brain, "_attentionDuration", Vector2.one);
                SetField(_brain, "_attentionCooldown", Vector2.one * 5f);
                _brain.TickIdleLook(0.3f);
                Assert.That(_brain.IsNoticingPlayer, Is.True);
                for (int i = 0; i < 5; i++) _brain.TickIdleLook(0.3f);
                Assert.That(_brain.IsNoticingPlayer, Is.False);
                for (int i = 0; i < 5; i++) _brain.TickIdleLook(0.3f);
                Assert.That(_brain.IsNoticingPlayer, Is.False);
                for (int i = 0; i < 11; i++) _brain.TickIdleLook(0.3f);
                Assert.That(_brain.IsNoticingPlayer, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(player); }
        }

        [Test]
        public void 중복_슬롯이_있어도_같은_생활동작을_연속_선택하지_않는다()
        {
            var first = ScriptableObject.CreateInstance<MotionSetAsset>();
            var second = ScriptableObject.CreateInstance<MotionSetAsset>();
            try
            {
                SetField(_brain, "_idleGestures", new[] { first, first, second, null });
                MotionSetAsset previous = null;
                for (int i = 0; i < 20; i++)
                {
                    SetField(_brain, "_gestureTimer", 0f);
                    Assert.That(_brain.TryTakeLifeGesture(out var motion), Is.True);
                    Assert.That(motion, Is.Not.SameAs(previous));
                    previous = motion;
                }
                SetField(_brain, "_idleGestures", new MotionSetAsset[] { null });
                SetField(_brain, "_gestureTimer", 0f);
                Assert.That(_brain.TryTakeLifeGesture(out _), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
