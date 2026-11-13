using System;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.World;

namespace UPlayGround.Tests.AmbientFauna
{
    /// <summary>GPU 경계의 데이터·예산·자극 상한과 실제 파생 메시 계약을 검사한다.</summary>
    public sealed class AmbientFaunaDataTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private const string Root = "Assets/10.Datas/World/AmbientFauna/";
        private GameObject _root;
        private static Type RuntimeType(string name) => Type.GetType("UPlayGround.Gameplay.World.LivingWorld." + name + ", Assembly-CSharp", true);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method).Invoke(target, args);

        [TearDown]
        public void Cleanup()
        {
            if (_root == null) return;
            foreach (Component group in _root.GetComponentsInChildren(RuntimeType("AmbientFaunaGroup"), true))
            {
                var host = group.GetComponentInParent(RuntimeType("AmbientFaunaHabitat"));
                if (host != null) Call(host, "Unregister", group);
            }
            UnityEngine.Object.DestroyImmediate(_root);
        }

        [TestCase("Butterfly")][TestCase("Gnat")][TestCase("Firefly")][TestCase("AirMote")]
        public void 생성_프로필과_셰이더가_유효하다(string species)
        {
            var profile = AssetDatabase.LoadAssetAtPath<AmbientFaunaProfileSO>(Root + species + ".asset");
            Assert.IsNotNull(profile);
            Assert.IsTrue(profile.TryValidate(out string error), error);
            Assert.IsFalse(ShaderUtil.ShaderHasError(profile.Material.shader));
            Assert.IsTrue(profile.Simulation.HasKernel("Initialize"));
            Assert.IsTrue(profile.Simulation.HasKernel("Simulate"));
            Assert.AreEqual(profile.Mesh.vertexCount, profile.Mesh.uv2.Length);
            Assert.AreEqual(profile.Mesh.vertexCount, profile.Mesh.uv3.Length);
        }

        [Test]
        public void 정적_나비의_몸통과_좌우_날개_채널이_분리된다()
        {
            var profile = AssetDatabase.LoadAssetAtPath<AmbientFaunaProfileSO>(Root + "Butterfly.asset");
            var mesh = profile.Mesh;
            int body = 0, left = 0, right = 0;
            foreach (Vector2 channel in mesh.uv2)
            {
                if (channel.y == 0) { Assert.AreEqual(0, channel.x); body++; }
                else if (channel.x == -1) left++;
                else if (channel.x == 1) right++;
                else Assert.Fail("알 수 없는 날개 채널");
            }
            Assert.Greater(body, 0); Assert.Greater(left, 0); Assert.AreEqual(left, right);
            Assert.AreEqual(1, mesh.bounds.size.x, 0.0001f);
            Assert.AreEqual(1, mesh.subMeshCount);
            Assert.AreEqual(48, Marshal.SizeOf(RuntimeType("AmbientFaunaGroup").GetNestedType("ParticleState", BindingFlags.NonPublic)));
        }

        [Test]
        public void 반딧불_자정_밀도와_잘못된_입력이_안정적이다()
        {
            var profile = AssetDatabase.LoadAssetAtPath<AmbientFaunaProfileSO>(Root + "Firefly.asset");
            Assert.AreEqual(profile.EvaluateDensity(0, 0, 0), profile.EvaluateDensity(24, 0, 0));
            Assert.AreEqual(0, profile.EvaluateDensity(float.NaN, 0, 0));
            Assert.Less(profile.EvaluateDensity(22, 1, 0), profile.EvaluateDensity(22, 0, 0));
            var clone = UnityEngine.Object.Instantiate(profile);
            try { clone.SizeRange = new Vector2(float.NaN, 1); Assert.IsFalse(clone.TryValidate(out _)); }
            finally { UnityEngine.Object.DestroyImmediate(clone); }
        }

        [Test]
        public void 앵커_0개와_NaN_및_stale_bake는_할당_전에_실패한다()
        {
            _root = new GameObject("FaunaInvalidData");
            _root.SetActive(false);
            Component group = _root.AddComponent(RuntimeType("AmbientFaunaGroup"));
            Set(group, "_profile", AssetDatabase.LoadAssetAtPath<AmbientFaunaProfileSO>(Root + "Butterfly.asset"));
            object[] arguments = { null };
            Assert.IsFalse((bool)Call(group, "TryPrepare", arguments));
            Set(group, "_anchors", new[] { new Vector4(float.NaN, 0, 0, 1) });
            Assert.IsFalse((bool)Call(group, "TryPrepare", arguments));
            Call(group, "SetBakedAnchors", (object)new[] { new Vector4(0, 0, 0, 1) });
            _root.transform.position = Vector3.one;
            Assert.IsFalse((bool)Call(group, "TryPrepare", arguments));
            Assert.AreEqual(0, group.GetType().GetProperty("AllocatedCapacity").GetValue(group));
        }

        [Test]
        public void 예약된_그룹도_예산에_포함하고_반환후_다음_그룹을_허용한다()
        {
            _root = new GameObject("FaunaBudget");
            Component habitat = _root.AddComponent(RuntimeType("AmbientFaunaHabitat"));
            Set(habitat, "_maxCapacity", 16);
            Component first = CreatePreparedGroup(habitat.transform, "First");
            Component second = CreatePreparedGroup(habitat.transform, "Second");
            Assert.IsTrue((bool)Call(habitat, "TryReserve", first, 16));
            Assert.IsFalse((bool)Call(habitat, "TryReserve", second, 1));
            Assert.AreEqual(16, habitat.GetType().GetProperty("AllocatedCapacity").GetValue(habitat));
            Call(habitat, "ReleaseReservation", first);
            Assert.IsTrue((bool)Call(habitat, "TryReserve", second, 1));
            Assert.AreEqual(1, habitat.GetType().GetProperty("AllocatedCapacity").GetValue(habitat));
        }

        private static Component CreatePreparedGroup(Transform parent, string name)
        {
            var gameObject = new GameObject(name); gameObject.transform.SetParent(parent);
            var group = gameObject.AddComponent(RuntimeType("AmbientFaunaGroup"));
            Set(group, "_profile", AssetDatabase.LoadAssetAtPath<AmbientFaunaProfileSO>(Root + "Butterfly.asset"));
            Call(group, "SetBakedAnchors", (object)new[] { new Vector4(0, 0, 0, 1) });
            object[] arguments = { null };
            Assert.IsTrue((bool)Call(group, "TryPrepare", arguments), (string)arguments[0]);
            Assert.IsTrue((bool)Call(parent.GetComponent(RuntimeType("AmbientFaunaHabitat")), "TryRegister", group));
            return group;
        }

        [Test]
        public void 별도_서식지도_같은_GPU_예산을_공유한다()
        {
            _root = new GameObject("FaunaGlobalBudget");
            var firstHost = _root.AddComponent(RuntimeType("AmbientFaunaHabitat"));
            var secondRoot = new GameObject("SecondHabitat"); secondRoot.transform.SetParent(_root.transform);
            var secondHost = secondRoot.AddComponent(RuntimeType("AmbientFaunaHabitat"));
            Set(firstHost, "_maxCapacity", 16); Set(secondHost, "_maxCapacity", 16);
            var first = CreatePreparedGroup(firstHost.transform, "First");
            var second = CreatePreparedGroup(secondHost.transform, "Second");
            Assert.IsTrue((bool)Call(firstHost, "TryReserve", first, 16));
            Assert.IsFalse((bool)Call(secondHost, "TryReserve", second, 1));
            Call(firstHost, "ReleaseReservation", first);
            Assert.IsTrue((bool)Call(secondHost, "TryReserve", second, 1));
        }

        [Test]
        public void 자극은_중복과_잘못된_입력을_거절하고_8개_상한을_지킨다()
        {
            _root = new GameObject("FaunaStimuli");
            var habitat = _root.AddComponent(RuntimeType("AmbientFaunaHabitat"));
            Assert.IsFalse((bool)Call(habitat, "EmitStimulus", Vector3.zero, float.NaN, 1f, 1f, 99));
            Assert.IsTrue((bool)Call(habitat, "EmitStimulus", Vector3.zero, 1f, 1f, 1f, 1));
            Assert.IsFalse((bool)Call(habitat, "EmitStimulus", Vector3.zero, 1f, 1f, 1f, 1));
            for (int i = 2; i <= 8; i++) Assert.IsTrue((bool)Call(habitat, "EmitStimulus", Vector3.zero, 1f, 1f, 1f, i));
            Assert.IsFalse((bool)Call(habitat, "EmitStimulus", Vector3.zero, 1f, 1f, 1f, 9));
            Assert.AreEqual(1, habitat.GetType().GetProperty("DroppedStimuli").GetValue(habitat));
        }

        [Test]
        public void 실제_렌더_연결_전에는_CPU를_유지하고_사냥_개체는_대체하지_않는다()
        {
            _root = new GameObject("FaunaFallback");
            var host = _root.AddComponent(RuntimeType("AmbientFaunaHabitat"));
            var cpuRoot = new GameObject("CpuOriginal"); cpuRoot.transform.SetParent(_root.transform);
            var cpu = cpuRoot.AddComponent(RuntimeType("WildlifeCreature"));
            var cpuProfile = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<WildlifeProfileSO>(
                "Assets/10.Datas/World/Wildlife/Butterfly_01.asset"));
            try
            {
                Set(cpu, "_profile", cpuProfile);
                var group = CreatePreparedGroup(host.transform, "GpuReplacement");
                Array fallbacks = Array.CreateInstance(RuntimeType("WildlifeCreature"), 1);
                fallbacks.SetValue(cpu, 0); Set(group, "_cpuFallbacks", fallbacks);
                object[] arguments = { null };
                Assert.IsTrue((bool)Call(group, "TryPrepare", arguments), (string)arguments[0]);
                Assert.IsTrue((bool)Call(host, "TryRegister", group));
                Assert.IsTrue(cpuRoot.activeSelf, "실제 렌더러가 확인되기 전에 CPU 원본을 숨겼습니다.");
                group.GetType().GetMethod("MarkRendererAvailable", Private).Invoke(group, null);
                Assert.IsFalse(cpuRoot.activeSelf);
                Call(group, "SetSuspended", true);
                Assert.IsTrue(cpuRoot.activeSelf);
                cpuProfile.MaxHealth = 1;
                Assert.IsFalse((bool)Call(group, "TryPrepare", arguments));
                Assert.IsTrue(cpuRoot.activeSelf);
            }
            finally { UnityEngine.Object.DestroyImmediate(cpuProfile); }
        }
    }
}
