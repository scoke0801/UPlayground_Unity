using System;
using System.Reflection;
using NUnit.Framework;

namespace UPlayGround.Core.Tests
{
    /// <summary>전역 환경 소유권과 씬 종료 잔류를 실제 Shader API로 검사한다.</summary>
    public sealed class EnvironmentShaderStateTests
    {
        private static Type State => Type.GetType("UPlayGround.Gameplay.World.LivingWorld.EnvironmentShaderState, Assembly-CSharp", true);
        private static Type Shader => Type.GetType("UnityEngine.Shader, UnityEngine.CoreModule", true);
        private static float Get(string name) => (float)Shader.GetMethod("GetGlobalFloat", new[] { typeof(string) }).Invoke(null, new object[] { name });
        private static void Set(string name, float value) => Shader.GetMethod("SetGlobalFloat", new[] { typeof(string), typeof(float) }).Invoke(null, new object[] { name, value });
        private static object Call(object state, string method, params object[] args) => State.GetMethod(method).Invoke(state, args);

        [Test]
        public void 중복_작성자와_늦은_해제가_다음_환경을_오염시키지_않는다()
        {
            object first = Activator.CreateInstance(State);
            object second = Activator.CreateInstance(State);
            float original = Get("SICSGlobalWindStrength");
            float wetness = Get("_LW_Wetness");
            try
            {
                Set("SICSGlobalWindStrength", 0.37f);
                Assert.IsTrue((bool)Call(first, "TryAcquire"));
                Assert.IsFalse((bool)Call(second, "TryAcquire"));
                Set("SICSGlobalWindStrength", 0.9f);
                Set("_LW_Wetness", 0.8f);
                Call(first, "Release");
                Assert.AreEqual(0.37f, Get("SICSGlobalWindStrength"));
                Assert.AreEqual(0, Get("_LW_Wetness"));
                Assert.IsTrue((bool)Call(second, "TryAcquire"));
                Set("_LW_Wetness", 0.6f);
                Call(first, "Release");
                Assert.AreEqual(0.6f, Get("_LW_Wetness"));
            }
            finally
            {
                Call(first, "Release");
                Call(second, "Release");
                Set("SICSGlobalWindStrength", original);
                Set("_LW_Wetness", wetness);
            }
        }
    }
}
