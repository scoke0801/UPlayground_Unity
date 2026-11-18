using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Components.LivingWorld;
using UPlayGround.Data.Actor.NPC;

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

    /// <summary>접촉 전 정렬이 물리 스텝·정지·충돌에서 순간이동을 만들지 않는지 검사한다.</summary>
    public sealed class NpcWorkAlignmentTests
    {
        private NpcWorkAlignmentSO _settings;

        [SetUp]
        public void SetUp() => _settings = ScriptableObject.CreateInstance<NpcWorkAlignmentSO>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_settings);

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void 보정은_다른_물리_주기에서도_접촉_전에_도착하고_높이를_보존한다(int rate)
        {
            Vector3 origin = new(10f, 3f, 20f), position = origin, target = origin + Vector3.forward * .14f;
            float deltaTime = 1f / rate;
            for (int step = 1; step <= Mathf.CeilToInt(_settings.endTime * rate); step++)
            {
                Vector3 velocity = _settings.EvaluateVelocity(origin, target, position, Vector3.up, step * deltaTime, deltaTime);
                Assert.That(velocity.magnitude, Is.LessThanOrEqualTo(_settings.maxSpeed + .00001f));
                position += velocity * deltaTime;
            }
            Assert.That(Vector3.Distance(position, target), Is.LessThanOrEqualTo(_settings.positionTolerance));
            Assert.That(position.y, Is.EqualTo(origin.y));
        }

        [Test]
        public void 히트스톱에서는_모션이_진행했어도_이동하지_않는다()
        {
            Assert.That(_settings.EvaluateVelocity(Vector3.zero, Vector3.forward * .1f,
                Vector3.zero, Vector3.up, _settings.endTime, 0f), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void 같은_모션_표본을_다시_소비해도_이동을_중복하지_않는다()
        {
            Vector3 target = Vector3.forward * .1f;
            Vector3 velocity = _settings.EvaluateVelocity(Vector3.zero, target, Vector3.zero, Vector3.up, .2f, .1f);
            Vector3 position = velocity * .1f;
            Assert.That(_settings.EvaluateVelocity(Vector3.zero, target, position, Vector3.up, .2f, .1f).magnitude,
                Is.LessThan(.00001f));
        }

        [Test]
        public void 벽에_막힌_위치는_시간이_끝나도_도착으로_판정하지_않는다()
        {
            Vector3 target = Vector3.forward * .1f;
            Assert.IsFalse(_settings.HasArrived(Vector3.zero, Quaternion.identity, target, Quaternion.identity, Vector3.up));
            Assert.IsTrue(_settings.HasArrived(target, Quaternion.identity, target, Quaternion.identity, Vector3.up));
        }

        [Test]
        public void 큰_거리와_높이와_회전은_짧은_보정으로_처리하지_않는다()
        {
            Assert.IsFalse(_settings.CanStart(Vector3.zero, Quaternion.identity, Vector3.forward, Quaternion.identity, Vector3.up));
            Assert.IsFalse(_settings.CanStart(Vector3.zero, Quaternion.identity, Vector3.up, Quaternion.identity, Vector3.up));
            Assert.IsFalse(_settings.CanStart(Vector3.zero, Quaternion.identity, Vector3.zero,
                Quaternion.Euler(0f, 90f, 0f), Vector3.up));
            Assert.IsTrue(_settings.CanStart(Vector3.zero, Quaternion.identity, Vector3.forward * .1f,
                Quaternion.Euler(0f, 5f, 0f), Vector3.up));
        }

        [Test]
        public void 수평_정렬을_마쳐도_접촉_전에_높이가_이탈하면_도착으로_보지_않는다()
        {
            Assert.IsFalse(_settings.HasArrived(Vector3.up, Quaternion.identity,
                Vector3.zero, Quaternion.identity, Vector3.up));
        }

        [Test]
        public void 시작부터_접촉하는_모션은_보정_진입반경이_아닌_정밀_도착반경까지_걷는다()
        {
            _settings.alignBeforeMotion = true;
            Assert.That(_settings.ArrivalDistance, Is.EqualTo(_settings.positionTolerance));
            Assert.IsFalse(_settings.HasArrived(Vector3.forward * .02f, Quaternion.identity,
                Vector3.zero, Quaternion.identity, Vector3.up));
            _settings.alignBeforeMotion = false;
            Assert.That(_settings.ArrivalDistance, Is.EqualTo(_settings.captureDistance));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void 무효_구간은_예약_실행을_허용하지_않는다(float endTime)
        {
            _settings.endTime = endTime;
            Assert.IsFalse(_settings.IsValid);
            Assert.That(_settings.EvaluateVelocity(Vector3.zero, Vector3.forward, Vector3.zero, Vector3.up, 1f, .02f),
                Is.EqualTo(Vector3.zero));
        }
    }

}
