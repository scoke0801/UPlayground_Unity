using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Components.LivingWorld;

namespace UPlayGround.Combat.Tests
{
    /// <summary>실물 작업 소품의 인계·중단 보존·이전 예약 차단을 검사한다.</summary>
    public sealed class NpcWorkPropTests
    {
        private GameObject _root, _owner, _other;
        private NpcWorkPoint _point;
        private Transform _prop, _hand;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("작업점 검사");
            _owner = new GameObject("사용자 검사");
            _other = new GameObject("다른 사용자 검사");
            _hand = _owner.transform;
            _hand.position = new Vector3(2f, 1f, 3f);
            _prop = new GameObject("실물 도구").transform;
            _prop.SetParent(_root.transform);
            _prop.position = new Vector3(1f, 0.8f, 0.5f);
            _point = _root.AddComponent<NpcWorkPoint>();
            Set("_mapId", "prop.test"); Set("_pointId", "work"); Set("_sceneProp", _prop);
            typeof(NpcWorkPoint).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_point, null);
        }

        private void Set(string field, object value)
            => typeof(NpcWorkPoint).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_point, value);

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_root);
            UnityEngine.Object.DestroyImmediate(_owner);
            UnityEngine.Object.DestroyImmediate(_other);
        }

        [Test]
        public void 집기와_놓기는_실물과_월드_자세를_보존한다()
        {
            Assert.IsTrue(_point.TryReserve(_owner, out ulong generation));
            Vector3 position = _prop.position;
            Quaternion rotation = _prop.rotation;
            Assert.IsTrue(_point.TryTakeSceneProp(_owner, generation, _hand));
            Assert.AreEqual(_hand, _prop.parent);
            Assert.That(Vector3.Distance(position, _prop.position), Is.LessThan(0.00001f));
            Assert.That(Quaternion.Angle(rotation, _prop.rotation), Is.LessThan(0.001f));
            Assert.IsTrue(_point.TryPlaceSceneProp(_owner, generation));
            Assert.AreEqual(_root.transform, _prop.parent);
            Assert.That(Vector3.Distance(position, _prop.position), Is.LessThan(0.00001f));
        }

        [Test]
        public void 중단은_손의_도구를_보존하고_다른_주민의_탈취를_거부한다()
        {
            Assert.IsTrue(_point.TryReserve(_owner, out ulong generation));
            Assert.IsTrue(_point.TryTakeSceneProp(_owner, generation, _hand));
            _point.Release(_owner, generation);
            Assert.AreEqual(_hand, _prop.parent);
            Assert.IsFalse(_point.TryReserve(_other, out _));
            Assert.IsTrue(_point.TryReserve(_owner, out ulong resumed));
            Assert.IsFalse(_point.TryPlaceSceneProp(_owner, generation));
            Assert.IsTrue(_point.TryTakeSceneProp(_owner, resumed, _hand));
            Assert.IsTrue(_point.TryPlaceSceneProp(_owner, resumed));
            _point.Release(_owner, resumed);
            Assert.IsTrue(_point.TryReserve(_other, out _));
        }

        [Test]
        public void 몸_해제는_자기_소품만_원래_현장에_복구한다()
        {
            Vector3 original = _prop.position;
            Assert.IsTrue(_point.TryReserve(_owner, out ulong generation));
            Assert.IsTrue(_point.TryTakeSceneProp(_owner, generation, _hand));
            _hand.position += Vector3.up;
            _point.ReleaseScenePropOnUnbind(_other);
            Assert.AreEqual(_hand, _prop.parent);
            _point.ReleaseScenePropOnUnbind(_owner);
            Assert.AreEqual(_root.transform, _prop.parent);
            Assert.That(Vector3.Distance(original, _prop.position), Is.LessThan(0.00001f));
        }

        [Test]
        public void 작업점_비활성화는_실물을_복구하고_소유권을_해제한다()
        {
            Vector3 original = _prop.position;
            Assert.IsTrue(_point.TryReserve(_owner, out ulong generation));
            Assert.IsTrue(_point.TryTakeSceneProp(_owner, generation, _hand));
            _hand.position += Vector3.up;
            typeof(NpcWorkPoint).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_point, null);
            Assert.IsFalse(_point.IsScenePropHeldBy(_owner));
            Assert.AreEqual(_root.transform, _prop.parent);
            Assert.That(Vector3.Distance(original, _prop.position), Is.LessThan(0.00001f));
            typeof(NpcWorkPoint).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_point, null);
            Assert.IsTrue(_point.TryReserve(_other, out _));
        }

        [TestCase(AnimatorCullingMode.CullUpdateTransforms)]
        [TestCase(AnimatorCullingMode.CullCompletely)]
        public void 접촉_작업은_화면밖_포즈를_갱신하고_중단시_기존_컬링을_복구한다(AnimatorCullingMode original)
        {
            _root.SetActive(false);
            var routine = _root.AddComponent<NpcRoutine>();
            var animator = _owner.AddComponent<Animator>();
            animator.cullingMode = original;
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(NpcRoutine).GetField("_boundAnimator", flags).SetValue(routine, animator);
            typeof(NpcRoutine).GetMethod("SetWorkPoseEvaluation", flags).Invoke(routine, new object[] { true });
            Assert.AreEqual(AnimatorCullingMode.AlwaysAnimate, animator.cullingMode);
            routine.CancelRoutine();
            Assert.AreEqual(original, animator.cullingMode);
            routine.CancelRoutine();
            Assert.AreEqual(original, animator.cullingMode);
        }

        [Test]
        public void 몸_비활성화_통지는_손의_실물을_현장으로_반환한다()
        {
            _other.SetActive(false);
            var routine = _other.AddComponent<NpcRoutine>();
            _owner.SetActive(false);
            _owner.AddComponent<CapsuleCollider>();
            var actor = _owner.AddComponent<NpcActor>();
            Assert.IsNotNull(actor, "NPC 필수 컴포넌트가 구성되어야 한다.");
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(NpcRoutine).GetField("_actor", flags).SetValue(routine, actor);
            typeof(NpcRoutine).GetField("_borrowedWorkPoint", flags).SetValue(routine, _point);
            Assert.IsTrue(_point.TryReserve(routine, out ulong generation));
            Assert.IsTrue(_point.TryTakeSceneProp(routine, generation, _hand));
            typeof(NpcRoutine).GetMethod("OnActorRoutineInterrupted", flags).Invoke(routine, null);
            Assert.AreEqual(_root.transform, _prop.parent);
            Assert.IsFalse(_point.IsScenePropHeldBy(routine));
        }
    }
}
