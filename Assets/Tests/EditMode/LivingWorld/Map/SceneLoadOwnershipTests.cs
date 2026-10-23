using System;
using System.Reflection;
using NUnit.Framework;

namespace UPlayGround.Core.Tests
{
    /// <summary>공용 테스트 asmdef를 바꾸지 않고 기존 런타임 전환의 소유권 회귀를 검사한다.</summary>
    public sealed class SceneLoadOwnershipTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static Type GameObjectType => Type.GetType("UnityEngine.GameObject, UnityEngine.CoreModule", true);
        private static object Create(string name) => Activator.CreateInstance(GameObjectType, new object[] { name });
        private static object Add(object go, string type) => GameObjectType.GetMethod("AddComponent", new[] { typeof(Type) })
            .Invoke(go, new object[] { Type.GetType(type, true) });
        private static void Destroy(object go) => Type.GetType("UnityEngine.Object, UnityEngine.CoreModule", true)
            .GetMethod("DestroyImmediate", new[] { Type.GetType("UnityEngine.Object, UnityEngine.CoreModule"), typeof(bool) })
            .Invoke(null, new[] { go, (object)false });

        [Test]
        public void 잘못된_중복요청도_수락된_도착ID를_덮어쓰지_않는다()
        {
            var go = Create("B_TransitionTest");
            var type = Type.GetType("UPlayGround.Manager.SceneManager, Assembly-CSharp", true);
            object manager = Add(go, type.AssemblyQualifiedName);
            var pending = type.GetProperty("PendingArrivalId");
            object previous = pending.GetValue(null);
            try
            {
                type.GetField("_isLoading", PrivateInstance).SetValue(manager, true);
                pending.GetSetMethod(true).Invoke(null, new object[] { "accepted-arrival" });
                bool accepted = (bool)type.GetMethod("TryLoadScene").Invoke(manager, new object[] { "", "rejected-arrival" });
                Assert.IsFalse(accepted);
                Assert.AreEqual("accepted-arrival", pending.GetValue(null));
                type.GetMethod("LoadScene", new[] { typeof(string) }).Invoke(manager, new object[] { "missing-scene" });
                Assert.AreEqual("accepted-arrival", pending.GetValue(null));
            }
            finally
            {
                type.GetField("_isLoading", PrivateInstance).SetValue(manager, false);
                pending.GetSetMethod(true).Invoke(null, new[] { previous });
                Destroy(go);
            }
        }

        [Test]
        public void 미적용_도착정보를_완료로_보고하지_않는다()
        {
            var go = Create("B_PendingArrivalTest");
            var type = Type.GetType("UPlayGround.Manager.SceneManager, Assembly-CSharp", true);
            object manager = Add(go, type.AssemblyQualifiedName);
            var pending = type.GetProperty("PendingArrivalId");
            object previous = pending.GetValue(null);
            try
            {
                type.GetField("_isLoading", PrivateInstance).SetValue(manager, true);
                pending.GetSetMethod(true).Invoke(null, new object[] { "not-applied" });
                var error = Assert.Throws<TargetInvocationException>(() =>
                    type.GetMethod("CompleteCurrentLoad", PrivateInstance).Invoke(manager, null));
                Assert.IsInstanceOf<InvalidOperationException>(error.InnerException);
                Assert.AreEqual("not-applied", pending.GetValue(null));
            }
            finally
            {
                type.GetField("_isLoading", PrivateInstance).SetValue(manager, false);
                pending.GetSetMethod(true).Invoke(null, new[] { previous });
                Destroy(go);
            }
        }

        [Test]
        public void 중복_도착ID는_선택하지_않고_해제_후_유일한_도착을_찾는다()
        {
            var a = Create("B_ArrivalA"); var b = Create("B_ArrivalB");
            var type = Type.GetType("UPlayGround.SceneArrivalPoint, Assembly-CSharp", true);
            var registry = Type.GetType("UPlayGround.SceneArrivalRegistry, Assembly-CSharp", true);
            var first = Add(a, type.AssemblyQualifiedName); var second = Add(b, type.AssemblyQualifiedName);
            const string id = "b.test.duplicate.arrival";
            try
            {
                type.GetField("_id", PrivateInstance).SetValue(first, id);
                type.GetField("_id", PrivateInstance).SetValue(second, id);
                registry.GetMethod("Register").Invoke(null, new[] { first });
                registry.GetMethod("Register").Invoke(null, new[] { second });
                object[] args = { id, null };
                Assert.IsFalse((bool)registry.GetMethod("TryGet").Invoke(null, args));
                registry.GetMethod("Unregister").Invoke(null, new[] { first });
                Assert.IsTrue((bool)registry.GetMethod("TryGet").Invoke(null, args));
                Assert.AreSame(second, args[1]);
            }
            finally
            {
                registry.GetMethod("Unregister").Invoke(null, new[] { first });
                registry.GetMethod("Unregister").Invoke(null, new[] { second });
                Destroy(a); Destroy(b);
            }
        }
    }
}
