using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace UPlayGround.Core.Tests
{
    /// <summary>공용 asmdef를 변경하지 않고 실제 C 컴포넌트의 점유·중단·환경 입력 계약을 검사한다.</summary>
    public sealed class NpcRoutineContractTests
    {
        private readonly List<object> _objects = new();
        private readonly List<object> _points = new();
        private static Type GameObjectType => Type.GetType("UnityEngine.GameObject, UnityEngine.CoreModule", true);
        private static Type ObjectType => Type.GetType("UnityEngine.Object, UnityEngine.CoreModule", true);
        private static Type ActorType(string name) => Type.GetType("UPlayGround.Components.LivingWorld." + name + ", UPlayGround.Actor", true);
        private static object Call(object target, string method, params object[] args)
            => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, args);
        private static void Set(object target, string field, object value)
            => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(target, value);
        private static object Get(object target, string field)
            => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        private object CreateObject()
        {
            object go = Activator.CreateInstance(GameObjectType, new object[] { "C 계약 검사" });
            _objects.Add(go);
            return go;
        }

        private object Add(string name, out object go)
        {
            go = CreateObject();
            object component = GameObjectType.GetMethod("AddComponent", new[] { typeof(Type) }).Invoke(go, new object[] { ActorType(name) });
            // ExecuteAlways가 아닌 런타임 수명주기는 EditMode에서 명시 호출한다. 실제 호출은 PlayMode 회귀로 구별한다.
            if (name == "NpcWorkPoint") { Call(component, "OnEnable"); _points.Add(component); }
            return component;
        }

        private object Point(string map = "test.map", string id = "test.point")
        {
            object point = Add("NpcWorkPoint", out _);
            Set(point, "_mapId", map);
            Set(point, "_pointId", id);
            return point;
        }

        private object Routine()
        {
            object routine = Add("NpcRoutine", out _);
            Set(routine, "_mapId", "test.map");
            return routine;
        }

        private static bool Reserve(object point, object owner, out ulong token)
        {
            object[] args = { owner, 0UL };
            bool success = (bool)Call(point, "TryReserve", args);
            token = (ulong)args[1];
            return success;
        }

        [TearDown]
        public void TearDown()
        {
            MethodInfo destroy = ObjectType.GetMethod("DestroyImmediate", new[] { ObjectType, typeof(bool) });
            for (int i = 0; i < _points.Count; i++) Call(_points[i], "OnDisable");
            _points.Clear();
            for (int i = _objects.Count - 1; i >= 0; i--) destroy.Invoke(null, new[] { _objects[i], (object)false });
            _objects.Clear();
        }

        [Test]
        public void 자리_경쟁과_다른_소유자의_해제를_거부한다()
        {
            object point = Point(), first = CreateObject(), second = CreateObject();
            Assert.IsTrue(Reserve(point, first, out ulong token));
            Assert.IsFalse(Reserve(point, second, out _));
            Call(point, "Release", second, token);
            Assert.IsTrue((bool)Call(point, "IsOwnedBy", first, token));
        }

        [Test]
        public void 늦은_해제와_완료는_다음_실행을_건드리지_못한다()
        {
            object point = Point(), owner = CreateObject();
            Assert.IsTrue(Reserve(point, owner, out ulong oldToken));
            Call(point, "Release", owner, oldToken);
            Assert.IsTrue(Reserve(point, owner, out ulong newToken));
            Call(point, "Release", owner, oldToken);
            Assert.IsFalse((bool)Call(point, "IsOwnedBy", owner, oldToken));
            Assert.IsTrue((bool)Call(point, "IsOwnedBy", owner, newToken));
        }

        [Test]
        public void 같은_맵의_중복_ID는_양쪽_예약을_막고_다른_맵은_허용한다()
        {
            object first = Point(), duplicate = Point(), otherMap = Point("other.map"), owner = CreateObject();
            Assert.IsFalse(Reserve(first, owner, out _));
            Assert.IsFalse(Reserve(duplicate, owner, out _));
            Assert.IsTrue(Reserve(otherMap, owner, out _));
        }

        [Test]
        public void 작업점_비활성화와_재활성화는_옛_토큰을_무효화한다()
        {
            object point = Add("NpcWorkPoint", out object go), owner = CreateObject();
            Set(point, "_mapId", "test.map"); Set(point, "_pointId", "test.point");
            Assert.IsTrue(Reserve(point, owner, out ulong token));
            Call(go, "SetActive", false);
            Call(point, "OnDisable");
            Call(go, "SetActive", true);
            Call(point, "OnEnable");
            Assert.IsFalse((bool)Call(point, "IsOwnedBy", owner, token));
            Assert.IsTrue(Reserve(point, owner, out _));
        }

        [Test]
        public void 일과_취소와_비활성화는_예약을_멱등_해제한다()
        {
            object routine = Routine(), point = Point();
            object step = Activator.CreateInstance(ActorType("NpcRoutine").GetNestedType("Step"));
            Set(step, "point", point);
            Assert.IsTrue((bool)Call(routine, "TryReserve", step));
            Call(routine, "CancelRoutine"); Call(routine, "CancelRoutine");
            Assert.IsTrue(Reserve(point, CreateObject(), out _));
        }

        [Test]
        public void 필요한_소품_소켓이_없으면_예약하기_전에_실패한다()
        {
            object routine = Routine(), point = Point();
            Set(point, "_heldPropPrefab", CreateObject());
            object step = Activator.CreateInstance(ActorType("NpcRoutine").GetNestedType("Step"));
            Set(step, "point", point);
            Assert.IsFalse((bool)Call(routine, "TryReserve", step));
            Assert.IsTrue(Reserve(point, CreateObject(), out _));
        }

        [Test]
        public void 다른_맵과_이전_환경_생산자의_늦은_입력을_무시한다()
        {
            object routine = Routine(), first = new object(), second = new object();
            Call(routine, "BindEnvironment", first);
            Assert.IsFalse((bool)Call(routine, "ApplyEnvironment", first, "other.map", 1L, true, true));
            Assert.IsTrue((bool)Call(routine, "ApplyEnvironment", first, "test.map", 100L, true, true));
            Call(routine, "BindEnvironment", second);
            Assert.IsFalse((bool)Call(routine, "ApplyEnvironment", first, "test.map", 101L, true, false));
            Call(routine, "ReleaseEnvironment", first);
            Assert.IsTrue((bool)Call(routine, "ApplyEnvironment", second, "test.map", 1L, true, false));
            Assert.IsFalse((bool)Get(routine, "_needsShelter"));
        }

        [Test]
        public void 환경_미준비와_과거_revision은_현재_안전_상태를_유지한다()
        {
            object routine = Routine(), producer = new object();
            Call(routine, "BindEnvironment", producer);
            Call(routine, "ApplyEnvironment", producer, "test.map", 1L, true, true);
            Assert.IsTrue((bool)Call(routine, "ApplyEnvironment", producer, "test.map", 2L, false, false));
            Assert.IsTrue((bool)Get(routine, "_needsShelter"));
            Assert.IsFalse((bool)Call(routine, "ApplyEnvironment", producer, "test.map", 1L, true, false));
            Assert.IsTrue((bool)Call(routine, "ApplyEnvironment", producer, "test.map", 3L, true, false));
        }

        [TestCase(359f, 3)] [TestCase(360f, 0)] [TestCase(540f, 1)]
        [TestCase(1080f, 2)] [TestCase(1320f, 3)]
        public void 시간대_변경은_일과_커서를_재평가한다(float minute, int period)
        {
            object routine = Routine();
            Set(routine, "_cursor", 1);
            Call(routine, "OnMinuteChanged", 1, minute);
            Assert.AreEqual(period, Get(routine, "_period"));
            Assert.AreEqual(0, Get(routine, "_cursor"));
        }

        [Test]
        public void 로드는_예약을_비우고_환경_생산자의_최신_상태를_보존한다()
        {
            object routine = Routine(), point = Point(), producer = new object();
            object step = Activator.CreateInstance(ActorType("NpcRoutine").GetNestedType("Step"));
            Set(step, "point", point);
            Call(routine, "BindEnvironment", producer);
            Call(routine, "ApplyEnvironment", producer, "test.map", 4L, true, true);
            Assert.IsTrue((bool)Call(routine, "TryReserve", step));
            Call(routine, "OnFlagsReloaded");
            Assert.IsTrue((bool)Get(routine, "_needsShelter"));
            Assert.IsTrue(Reserve(point, CreateObject(), out _));
            Assert.AreEqual(0, Get(routine, "_cursor"));
        }
    }
}
