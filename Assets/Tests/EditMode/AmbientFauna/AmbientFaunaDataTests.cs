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

        [TestCase("Butterfly")][TestCase("Gnat")][TestCase("Firefly")][TestCase("AirMote")][TestCase("Butterfly_AmberSoft")]
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
        public void 전용_곤충_외형과_복부_발광이_나비와_분리된다()
        {
            var butterfly = AssetDatabase.LoadAssetAtPath<AmbientFaunaProfileSO>(Root + "Butterfly.asset");
            foreach (string name in new[] { "Gnat", "Firefly" })
            {
                var profile = AssetDatabase.LoadAssetAtPath<AmbientFaunaProfileSO>(Root + name + ".asset");
                Assert.AreNotSame(butterfly.Mesh, profile.Mesh);
                Assert.AreEqual(1, profile.Mesh.bounds.size.x, 0.0001f);
                Assert.AreEqual(profile.Mesh.vertexCount, profile.Mesh.colors.Length);
                int glow = 0;
                foreach (var color in profile.Mesh.colors) if (color.a > 0.5f) glow++;
                if (name == "Firefly") { Assert.Greater(glow, 0); Assert.Less(glow, profile.Mesh.vertexCount / 2); }
                else Assert.AreEqual(0, glow);
            }
            Assert.AreEqual(0.06f, butterfly.SizeRange.x, 0.0001f);
            Assert.AreEqual(0.10f, butterfly.SizeRange.y, 0.0001f);
        }

        [Test]
        public void 캡슐_끝점과_자극_우선순위가_보존된다()
        {
            _root = new GameObject("FaunaCapsule");
            var host = _root.AddComponent(RuntimeType("AmbientFaunaHabitat"));
            Assert.IsFalse((bool)Call(host, "EmitCapsuleStimulus", Vector3.zero, Vector3.one * float.NaN, 1f, 1f, 1f, 1));
            for (int i = 0; i < 8; i++) Assert.IsTrue((bool)Call(host, "EmitCapsuleStimulus", Vector3.zero, Vector3.up * 2, 1f, 1f, 1f, i));
            Assert.IsTrue((bool)Call(host, "EmitCapsuleStimulus", Vector3.zero, Vector3.forward * 3, 1f, 5f, 2f, 99));
            var ends = new Vector4[8];
            Call(host, "CopyInfluenceEnds", _root.transform, ends);
            Assert.AreEqual((Vector4)(Vector3.forward * 3), ends[0]);
            Assert.AreEqual((Vector4)(Vector3.up * 2), ends[1]);
            Assert.AreEqual(1, host.GetType().GetProperty("DroppedStimuli").GetValue(host));
            Call(host, "SetRegionActive", false);
            Assert.AreEqual(0, Call(host, "CopyInfluences", _root.transform, new Vector4[8], new Vector4[8]));
            Call(host, "SetRegionActive", true);
            Assert.AreEqual(0, Call(host, "CopyInfluences", _root.transform, new Vector4[8], new Vector4[8]));
            Assert.IsTrue((bool)Call(host, "EmitSourcedCapsuleStimulus", Vector3.zero, Vector3.up, 1f, 1f, 1f, 100, 7));
            Assert.IsTrue((bool)Call(host, "EmitSourcedCapsuleStimulus", Vector3.zero, Vector3.up, 1f, 1f, 1f, 200, 7));
            Assert.IsFalse((bool)Call(host, "EmitSourcedCapsuleStimulus", Vector3.zero, Vector3.up, 1f, 1f, 1f, 100, 7));
        }

        [Test]
        public void 품질_단계가_실제_예약_상한에_적용된다()
        {
            _root = new GameObject("FaunaQuality");
            var host = _root.AddComponent(RuntimeType("AmbientFaunaHabitat"));
            var quality = ScriptableObject.CreateInstance<AmbientFaunaQualitySO>();
            try
            {
                quality.Tiers[0] = new AmbientFaunaQualitySO.Tier { Capacity = 4, Groups = 1, ScreenFraction = 0.001f };
                Set(host, "_quality", quality); Call(host, "SetQualityTier", 0);
                var first = CreatePreparedGroup(host.transform, "First");
                var second = CreatePreparedGroup(host.transform, "Second");
                Assert.IsFalse((bool)Call(host, "TryReserve", first, 5));
                Assert.IsTrue((bool)Call(host, "TryReserve", first, 4));
                Assert.IsFalse((bool)Call(host, "TryReserve", second, 1));
            }
            finally { UnityEngine.Object.DestroyImmediate(quality); }
        }

        [Test]
        public void 표면과_체적_밀도의_샘플_단위를_혼용하지_않는다()
        {
            _root = new GameObject("FaunaDensityUnits");
            var placement = _root.AddComponent(RuntimeType("AmbientFaunaPlacement"));
            var field = placement.GetType().GetField("DensityMode");
            var surface = placement.GetType().GetField("UseSurface");
            object[] args = { null };
            field.SetValue(placement, Enum.ToObject(field.FieldType, 1)); surface.SetValue(placement, false);
            Assert.IsFalse((bool)Call(placement, "TryValidate", args));
            surface.SetValue(placement, true); Assert.IsTrue((bool)Call(placement, "TryValidate", args));
            field.SetValue(placement, Enum.ToObject(field.FieldType, 2));
            Assert.IsFalse((bool)Call(placement, "TryValidate", args));
            surface.SetValue(placement, false); Assert.IsTrue((bool)Call(placement, "TryValidate", args));
        }

        [Test]
        public void 볼륨_베이크가_재현되고_제외와_설정_stale을_검출한다()
        {
            _root = new GameObject("FaunaBake");
            _root.transform.position = Vector3.up * 10000;
            var host = _root.AddComponent(RuntimeType("AmbientFaunaHabitat"));
            var group = CreatePreparedGroup(host.transform, "Volume");
            var placement = group.gameObject.AddComponent(RuntimeType("AmbientFaunaPlacement"));
            placement.GetType().GetField("UseSurface").SetValue(placement, false);
            var authoring = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), a => a.GetType("UPlayGround.Tool.Editor.Map.AmbientFaunaAuthoring") != null)
                .GetType("UPlayGround.Tool.Editor.Map.AmbientFaunaAuthoring");
            var bake = authoring.GetMethod("BakePlacement");
            var first = (Vector4[])bake.Invoke(null, new object[] { placement });
            var second = (Vector4[])bake.Invoke(null, new object[] { placement });
            CollectionAssert.AreEqual(first, second);
            Call(group, "SetBakedAnchors", (object)first);
            object[] args = { null };
            Assert.IsTrue((bool)Call(group, "TryPrepare", args), (string)args[0]);
            placement.GetType().GetField("Seed").SetValue(placement, 999);
            Assert.IsFalse((bool)Call(group, "TryPrepare", args));
            var exclusionRoot = new GameObject("Exclusion"); exclusionRoot.transform.SetParent(_root.transform, false);
            var exclusion = exclusionRoot.AddComponent<BoxCollider>(); exclusion.size = Vector3.one * 2;
            placement.GetType().GetField("Exclusions").SetValue(placement, new Collider[] { exclusion });
            Assert.IsTrue((bool)Call(placement, "IntersectsExclusion", _root.transform.position, 0.2f));
            Assert.IsFalse((bool)Call(placement, "IntersectsExclusion", _root.transform.position + Vector3.right * 5, 0.2f));
            exclusionRoot.transform.position = group.transform.position;
            Physics.SyncTransforms();
            var geometry = authoring.GetMethod("ComputeGeometryHash");
            string before = (string)geometry.Invoke(null, new object[] { placement });
            exclusionRoot.transform.position += Vector3.right * 0.25f;
            Physics.SyncTransforms();
            Assert.AreNotEqual(before, geometry.Invoke(null, new object[] { placement }));
        }

        [Test]
        public void 기존_정적_구조물이_변하면_재베이크까지_군집을_중단한다()
        {
            _root = new GameObject("FaunaGeometryChange");
            var host = _root.AddComponent(RuntimeType("AmbientFaunaHabitat"));
            var group = CreatePreparedGroup(host.transform, "GeometryGroup");
            var wall = new GameObject("StaticWall"); wall.transform.SetParent(_root.transform, false);
            wall.transform.position = group.transform.position + Vector3.right * 1.5f;
            wall.AddComponent<BoxCollider>().size = Vector3.one * 0.2f;
            Call(group, "SetBakedAnchors", (object)new[] { new Vector4(0, 0, 0, 1) });
            object[] args = { null };
            Assert.IsTrue((bool)Call(group, "TryPrepare", args), (string)args[0]);
            wall.transform.position += Vector3.right * 0.1f;
            Physics.SyncTransforms();
            Call(group, "CheckBakedGeometry");
            Assert.IsTrue((bool)group.GetType().GetProperty("IsGeometryInvalidated").GetValue(group));
            Assert.IsFalse((bool)Call(group, "TryPrepare", args));
            Call(group, "SetBakedAnchors", (object)new[] { new Vector4(0, 0, 0, 1) });
            Assert.IsTrue((bool)Call(group, "TryPrepare", args), (string)args[0]);
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
