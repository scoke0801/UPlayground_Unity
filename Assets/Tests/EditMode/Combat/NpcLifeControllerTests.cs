using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Components.LivingWorld;
using UPlayGround.Data.Actor.NPC;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.Combat.Tests
{
    /// <summary>생활 실행 취소와 국소 사건의 시간·생산자 경계를 검사한다.</summary>
    public sealed class NpcLifeControllerTests
    {
        private readonly List<GameObject> _objects = new();
        private NpcLifeController _life;
        private NpcRoutine _routine;
        private NpcLifeProfileSO _profile;
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Set(object target, string name, object value)
            => target.GetType().GetField(name, Fields).SetValue(target, value);
        private static object Get(object target, string name)
            => target.GetType().GetField(name, Fields).GetValue(target);
        private static object Call(object target, string name, params object[] args)
            => target.GetType().GetMethod(name, Fields).Invoke(target, args);

        private GameObject Create()
        { var go = new GameObject("생활 계약 검사"); _objects.Add(go); return go; }

        [SetUp]
        public void SetUp()
        {
            var root = Create();
            _routine = root.AddComponent<NpcRoutine>();
            _life = root.AddComponent<NpcLifeController>();
            _profile = ScriptableObject.CreateInstance<NpcLifeProfileSO>();
            Set(_life, "_profile", _profile);
            Set(_routine, "_mapId", "test.life");
            Call(_life, "OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            Call(_life, "OnDisable");
            foreach (var go in _objects)
                foreach (var point in go.GetComponents<NpcWorkPoint>()) Call(point, "OnDisable");
            for (int i = _objects.Count - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
            UnityEngine.Object.DestroyImmediate(_profile);
        }

        private NpcRoutine.Step Reserve(NpcRoutine routine, string id)
        {
            var point = Create().AddComponent<NpcWorkPoint>();
            Set(point, "_mapId", "test.life"); Set(point, "_pointId", id);
            Call(point, "OnEnable");
            var step = new NpcRoutine.Step { point = point };
            Assert.IsTrue((bool)Call(routine, "TryReserve", step));
            return step;
        }

        [Test]
        public void 취소는_예약을_반환하고_세대를_한번만_변경한다()
        {
            var step = Reserve(_routine, "work");
            ulong generation = _life.Generation;
            _routine.CancelRoutine();
            Assert.Greater(_life.Generation, generation);
            generation = _life.Generation;
            _routine.CancelRoutine();
            Assert.AreEqual(generation, _life.Generation);
            Assert.IsTrue(step.point.TryReserve(Create(), out _));
        }

        [Test]
        public void 과거_세션_중복취소는_새_예약을_해제하지_않는다()
        {
            var other = Create().AddComponent<NpcLifeController>();
            var otherRoutine = other.GetComponent<NpcRoutine>();
            Set(otherRoutine, "_mapId", "test.life");
            Set(other, "_routine", otherRoutine); Set(other, "_profile", _profile);
            Reserve(_routine, "first"); Reserve(otherRoutine, "second");
            var session = new NpcSocialSession(null, _life, other, null, null);
            Set(_life, "_session", session); Set(other, "_session", session);
            session.Cancel();
            Assert.IsFalse(_routine.HasReservation); Assert.IsFalse(otherRoutine.HasReservation);
            Reserve(_routine, "next");
            session.Cancel();
            Assert.IsTrue(_routine.HasReservation);
        }

        [Test]
        public void 디스폰_해제는_예약과_몸_참조를_함께_비운다()
        {
            Reserve(_routine, "despawn");
            _routine.UnbindActor();
            Assert.IsFalse(_routine.HasReservation);
            Assert.IsNull(Get(_routine, "_actor"));
            Assert.IsNull(Get(_routine, "_controller"));
        }

        [Test]
        public void 사건_재전달과_교체전_생산자는_소비하지_않는다()
        {
            var first = new object(); var second = new object();
            _life.BindStimulus(first);
            Consume(first, "test.life", 3, Time.time, Time.time + 2);
            Assert.AreEqual(3L, Get(_life, "_stimulusRevision"));
            Consume(first, "test.life", 2, Time.time, Time.time + 2);
            Assert.AreEqual(3L, Get(_life, "_stimulusRevision"));
            _life.BindStimulus(second); _life.ReleaseStimulus(first);
            Consume(first, "test.life", 9, Time.time, Time.time + 2);
            Assert.AreEqual(-1L, Get(_life, "_stimulusRevision"));
            Consume(second, "test.life", 1, Time.time, Time.time + 2);
            Assert.AreEqual(1L, Get(_life, "_stimulusRevision"));
        }

        private void Consume(object producer, string map, long id, float occurred, float expires)
            => _life.ConsumeStimulus(producer, map, id, Vector3.zero, occurred, expires, 1f);

        [TestCase("other", 0f, 2f)]
        [TestCase("test.life", -2f, -1f)]
        [TestCase("test.life", 1f, 2f)]
        [TestCase("test.life", float.NaN, 2f)]
        [TestCase("test.life", 0f, float.PositiveInfinity)]
        public void 다른맵_만료_미래_비정상시각은_거부한다(string map, float occurred, float expires)
        {
            var producer = new object(); _life.BindStimulus(producer);
            Consume(producer, map, 1, Time.time + occurred, Time.time + expires);
            Assert.AreEqual(-1L, Get(_life, "_stimulusRevision"));
        }

        [Test]
        public void 준비전에는_교류와_활동을_시작하지_않는다()
        {
            _profile.canSocialize = true;
            Assert.IsFalse(_life.CanSocialize);
            Assert.IsFalse(_life.TryStart());
            Assert.IsFalse(_routine.HasReservation);
        }

        private NpcLifeController CreateReadyParticipant(string id)
        {
            var life = Create().AddComponent<NpcLifeController>();
            var routine = life.GetComponent<NpcRoutine>();
            Set(life, "_profile", _profile); Set(life, "_residentId", id);
            Set(life, "_neighborhoodId", "test.neighborhood");
            Call(life, "OnEnable"); Set(life, "_socialReadyAt", float.NegativeInfinity);
            var body = Create(); body.AddComponent<CapsuleCollider>();
            Set(routine, "_actor", body.AddComponent<NpcLifeTestActor>());
            Set(routine, "_mapId", "test.life"); Set(routine, "_time", new Clock());
            Set(routine, "_flags", new Flags()); Set(routine, "_isPlacementReady", true);
            Set(routine, "_period", 2); _profile.canSocialize = true;
            Assert.IsTrue(life.CanSocialize, "예약 실패를 주입하기 전 참여 준비가 유효해야 합니다.");
            return life;
        }

        [Test]
        public void 두번째_자리_예약실패는_첫자리와_양쪽_동의를_반환한다()
        {
            var first = CreateReadyParticipant("first"); var second = CreateReadyParticipant("second");
            var step = Reserve(_routine, "shared"); _routine.CancelRoutine();
            var point = Create().AddComponent<NpcSocialPoint>();
            var session = new NpcSocialSession(point, first, second, null, null);
            Assert.IsFalse(session.TryBegin(step, step));
            Assert.IsFalse(first.Routine.HasReservation); Assert.IsFalse(second.Routine.HasReservation);
            Assert.IsNull(Get(first, "_session")); Assert.IsNull(Get(second, "_session"));
            Assert.IsTrue(step.point.TryReserve(Create(), out _));
        }

        [Test]
        public void 상태진입_실패도_양쪽_예약을_반환한다()
        {
            var first = CreateReadyParticipant("first"); var second = CreateReadyParticipant("second");
            var step1 = Reserve(_routine, "seat.first"); _routine.CancelRoutine();
            var step2 = Reserve(_routine, "seat.second"); _routine.CancelRoutine();
            var session = new NpcSocialSession(Create().AddComponent<NpcSocialPoint>(), first, second, null, null);
            // 이동 컨트롤러가 없는 몸은 예약 뒤 진입에 실패해야 한다.
            Assert.IsFalse(session.TryBegin(step1, step2));
            Assert.IsFalse(first.Routine.HasReservation); Assert.IsFalse(second.Routine.HasReservation);
            Assert.IsTrue(step1.point.TryReserve(Create(), out _));
            Assert.IsTrue(step2.point.TryReserve(Create(), out _));
        }

        private NpcSocialSession CreateApproachingPair(out NpcLifeController first, out NpcLifeController second)
        {
            first = CreateReadyParticipant("first"); second = CreateReadyParticipant("second");
            Reserve(first.Routine, "approach.first"); Reserve(second.Routine, "approach.second");
            var session = new NpcSocialSession(Create().AddComponent<NpcSocialPoint>(), first, second, null, null);
            Set(first, "_session", session); Set(second, "_session", session);
            return session;
        }

        [Test]
        public void 세션_시간은_진행자_한명만_갱신한다()
        {
            var session = CreateApproachingPair(out var first, out var second);
            session.Tick(second, 0.25f); Assert.AreEqual(0f, Get(session, "_elapsed"));
            session.Tick(first, 0.25f); Assert.AreEqual(0.25f, Get(session, "_elapsed"));
            session.Cancel();
        }

        [Test]
        public void 상대_도착_기한이_지나면_양쪽을_해제한다()
        {
            var session = CreateApproachingPair(out var first, out var second);
            session.Tick(first, _profile.socialArrivalTimeout + 1f);
            Assert.IsFalse(first.Routine.HasReservation); Assert.IsFalse(second.Routine.HasReservation);
            Assert.IsNull(Get(first, "_session")); Assert.IsNull(Get(second, "_session"));
        }

        [Test]
        public void 진행자_소실은_상대의_틱에서도_양쪽을_해제한다()
        {
            var session = CreateApproachingPair(out var first, out var second);
            first.enabled = false;
            session.Tick(second, 0.1f);
            Assert.IsFalse(first.Routine.HasReservation); Assert.IsFalse(second.Routine.HasReservation);
        }

        private (NpcRoutine routine, object motor, NpcRoutine.Step step, Vector3 origin) CreateJoinTest()
        {
            var life = CreateReadyParticipant("path"); var routine = life.Routine;
            var actor = (Component)Get(routine, "_actor");
            Type motorType = Type.GetType("KinematicCharacterController.KinematicCharacterMotor, KinematicCharacterController", true);
            var motor = actor.GetComponent(motorType) ?? actor.gameObject.AddComponent(motorType); Call(motor, "Awake");
            var controller = actor.GetComponent<UPlayGround.MovementController.NpcMovementController>();
            typeof(UPlayGround.MovementController.ActorMovementController).GetProperty("Motor").SetValue(controller, motor);
            Set(routine, "_controller", controller);
            Vector3 origin = new(-5000f, 100f, -5000f);
            motorType.GetMethod("SetPositionAndRotation").Invoke(motor, new object[] { origin, Quaternion.identity, true });
            var step = Reserve(_routine, "join"); _routine.CancelRoutine();
            step.point.transform.position = origin + Vector3.right * 3f;
            var waypoint = Create().transform; waypoint.position = origin + Vector3.forward * 2f; step.waypoints = new[] { waypoint };
            var wall = Create(); wall.transform.position = origin + Vector3.right * 1.5f + Vector3.up;
            var box = wall.AddComponent<BoxCollider>(); box.size = new Vector3(0.4f, 2f, 0.4f);
            Physics.SyncTransforms(); return (routine, motor, step, origin);
        }

        [Test]
        public void 벽을_가로지르지_않는_합류점을_고르고_대화후에는_지나온_경유점으로_되돌아가지_않는다()
        {
            var context = CreateJoinTest();
            Assert.AreEqual(0, Call(context.routine, "FindSafeJoin", context.step));
            context.motor.GetType().GetMethod("SetPositionAndRotation").Invoke(context.motor,
                new object[] { context.origin + Vector3.right * 2.6f, Quaternion.identity, true });
            Physics.SyncTransforms();
            Assert.AreEqual(1, Call(context.routine, "FindSafeJoin", context.step));
            Assert.AreEqual(context.origin + Vector3.right * 2.6f,
                context.motor.GetType().GetProperty("TransientPosition").GetValue(context.motor), "경로 검사는 몸을 순간이동시키지 않아야 합니다.");
        }

        [Test]
        public void 모든_합류점이_막히면_예약도_반환한다()
        {
            var context = CreateJoinTest(); context.step.waypoints[0].position = context.origin + Vector3.right * 2f;
            Physics.SyncTransforms();
            Assert.IsFalse((bool)Call(context.routine, "TryReserve", context.step));
            Assert.IsFalse(context.routine.HasReservation);
            Assert.IsTrue(context.step.point.TryReserve(Create(), out _));
        }

        [Test]
        public void 같은_생산자_재연결은_사건_중복_기록을_지우지_않고_비정상_위치를_거부한다()
        {
            var producer = new object(); _life.BindStimulus(producer);
            Consume(producer, "test.life", 1L, Time.time, Time.time + 1f); _life.BindStimulus(producer);
            Assert.AreEqual(1L, Get(_life, "_stimulusRevision"));
            _life.ConsumeStimulus(producer, "test.life", 2L, new Vector3(float.NaN, 0f, 0f), Time.time, Time.time + 1f, 1f);
            Assert.AreEqual(1L, Get(_life, "_stimulusRevision"));
        }

        [Test]
        public void 예약이_없는_관심_지연도_대화_중단에서_취소한다()
        {
            Set(_life, "_noticeUntil", Time.time + 5f); ulong generation = _life.Generation;
            _routine.CancelRoutine();
            Assert.AreEqual(0f, Get(_life, "_noticeUntil")); Assert.Greater(_life.Generation, generation);
            generation = _life.Generation; _routine.CancelRoutine(); Assert.AreEqual(generation, _life.Generation);
        }

        [Test]
        public void 보류한_작업은_참조를_유지해도_실행하지_않고_승인된_작업만_순환한다()
        {
            var ready = new NpcRoutine.Step(); var pending = new NpcRoutine.Step { isContentPending = true };
            Set(_routine, "_steps", new[] { pending, ready });
            Assert.AreEqual(1, Call(_routine, "GetNextReadyStep", -1));
            Assert.AreEqual(1, Call(_routine, "GetNextReadyStep", 1));
            ready.isContentPending = true; Assert.AreEqual(-1, Call(_routine, "GetNextReadyStep", 0));
            var step = Reserve(_routine, "pending"); _routine.CancelRoutine(); step.isContentPending = true;
            Assert.IsFalse((bool)Call(_routine, "TryReserve", step));
            Assert.IsTrue(step.point.TryReserve(Create(), out _));
        }

        [Test]
        public void 교류는_첫_표현이_끝난_뒤_응답하고_전체_모션보다_먼저_종료하지_않는다()
        {
            CreateApproachingPair(out var first, out var second);
            var clip = new AnimationClip();
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 2.5f, 0f));
            var motion = ScriptableObject.CreateInstance<UPlayGround.Animation.MotionSetAsset>();
            motion.motionSet = new UPlayGround.Animation.MotionSet
            { motions = new List<UPlayGround.Animation.Motion> { new() { motionClip = clip } } };
            try
            {
                _profile.socialDuration = 1f;
                var session = new NpcSocialSession(Create().AddComponent<NpcSocialPoint>(), first, second, motion, motion);
                Set(first, "_session", session); Set(second, "_session", session);
                Set(first.Routine, "_isWorking", true); Set(second.Routine, "_isWorking", true);
                session.Tick(first, 2.4f);
                Assert.IsTrue(first.Routine.HasReservation); Assert.IsFalse((bool)Get(session, "_hasResponded"));
                session.Tick(first, 0.2f); Assert.IsTrue((bool)Get(session, "_hasResponded"));
                session.Tick(first, 2.3f); Assert.IsTrue(second.Routine.HasReservation);
                session.Tick(first, 0.2f); Assert.IsFalse(first.Routine.HasReservation); Assert.IsFalse(second.Routine.HasReservation);
            }
            finally { UnityEngine.Object.DestroyImmediate(motion); UnityEngine.Object.DestroyImmediate(clip); }
        }

        private sealed class Clock : IGameTimeService
        {
            public event Action<int, float> OnGameMinuteChanged { add { } remove { } }
            public bool IsPaused => false;
            public bool IsSlowed => false;
            public int CurrentDay => 1;
            public float MinuteOfDay => 1080f;
            public DayPeriod CurrentDayPeriod => default;
            public int Request(float scale) => 0;
            public void UpdateRequestScale(int id, float scale) { }
            public void Release(int id) { }
            public void SetPause(bool pause) { }
            public string FormatPlayTime() => "";
        }

        private sealed class Flags : IGlobalFlagService
        {
            public event Action<string, bool> OnFlagChanged { add { } remove { } }
            public event Action OnFlagsReloaded { add { } remove { } }
            public bool GetFlag(string key) => false;
            public void SetFlag(string key, bool value) => Assert.Fail("생활 AI가 진행도를 변경했습니다.");
        }
    }

    /// <summary>생활 계약 검사에서 외형·전투 초기화를 제외한 주민 몸이다.</summary>
    public sealed class NpcLifeTestActor : NpcActor
    {
        protected override void Awake() { }
        protected override void Start() { }
        protected override void Update() { }
    }
}
