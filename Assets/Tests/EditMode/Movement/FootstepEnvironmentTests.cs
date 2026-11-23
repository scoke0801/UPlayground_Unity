using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.Actor;

namespace UPlayGround.Movement.Tests
{
    /// <summary>수면의 실제 영역과 높이 경계, 베이크 지형의 좌표 해석을 확인한다.</summary>
    public sealed class FootstepEnvironmentTests
    {
        [Test]
        public void PoseFoot_제자리_발재배치는_멎은_뒤_한번만_허용한다()
        {
            var type = typeof(ActorFootstep).GetNestedType("PoseFoot", BindingFlags.NonPublic);
            object foot = System.Activator.CreateInstance(type);
            var sample = type.GetMethod("Sample");
            var plant = type.GetMethod("Plant");
            bool Ready(Vector3 position, bool trackStride = false, bool strideReady = false) => (bool)sample.Invoke(foot,
                new object[] { position, Vector3.up, 0.11f, 0.085f, 0.01f, trackStride, strideReady });

            Assert.That(Ready(Vector3.zero), Is.False);
            Assert.That(Ready(new Vector3(0.06f, 0f, 0f)), Is.False);
            var newSupport = new Vector3(0.15f, 0f, 0f);
            Assert.That(Ready(newSupport), Is.False);
            Assert.That(Ready(newSupport, true), Is.False, "보폭 추적 중에는 일반 정착 판정이 개입하지 않는다.");
            Assert.That(Ready(newSupport), Is.True, "이동 방향이나 걷기 상태 없이 새 접지를 잡는다.");
            plant.Invoke(foot, new object[] { newSupport });
            Assert.That(Ready(newSupport, true, true), Is.False, "전환 중 보폭과 위치 검출이 중복되지 않는다.");
            Assert.That(Ready(newSupport + Vector3.up * 0.10f), Is.False);
            Assert.That(Ready(newSupport), Is.False);
            Assert.That(Ready(newSupport), Is.True, "같은 위치로 발을 다시 내려도 새 접지로 잡는다.");
        }

        [Test]
        public void LocomotionFoot_앞으로_내민_발이_멎을_때만_다음_디딤을_허용한다()
        {
            var type = typeof(ActorFootstep).GetNestedType("LocomotionFoot", BindingFlags.NonPublic);
            object foot = System.Activator.CreateInstance(type);
            var sample = type.GetMethod("Sample");
            var plant = type.GetMethod("Plant");
            bool Ready(float position) => (bool)sample.Invoke(foot, new object[] { position, 0.055f, 0.005f });

            Assert.That(Ready(0f), Is.False);
            Assert.That(Ready(-0.08f), Is.False);
            Assert.That(Ready(-0.04f), Is.False);
            Assert.That(Ready(0.02f), Is.False);
            Assert.That(Ready(0.023f), Is.True);
            Assert.That(Ready(0.020f), Is.True, "접촉이 늦으면 같은 디딤을 다시 검사한다.");
            plant.Invoke(foot, null);
            Assert.That(Ready(0.020f), Is.False, "지지 중인 발은 중복 기록하지 않는다.");
            Assert.That(Ready(-0.11f), Is.False);
            Assert.That(Ready(-0.05f), Is.False);
            Assert.That(Ready(-0.05f), Is.True);
        }

        [TestCase(0f, 0.13f, 0.13f)]
        [TestCase(30f, 0.13f, 0.0463f)]
        [TestCase(30f, -0.15f, -0.0663f)]
        public void FootprintGap_경사면에_닿은_발바닥_가장자리까지_고려한다(float slope, float gap, float expected)
        {
            var method = typeof(ActorFootstep).GetMethod("FootprintGap", BindingFlags.Static | BindingFlags.NonPublic);
            var normal = Quaternion.AngleAxis(slope, Vector3.right) * Vector3.up;
            float distance = (float)method.Invoke(null, new object[] { gap, normal, Vector3.forward,
                Vector3.up, new Vector2(0.13f, 0.29f) });
            Assert.That(distance, Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void FootprintContact_좌우발을_따로_재시도하고_발을_떼기까지_한번만_찍는다()
        {
            var type = typeof(ActorFootstep).GetNestedType("FootContact", BindingFlags.NonPublic);
            object left = System.Activator.CreateInstance(type);
            object right = System.Activator.CreateInstance(type);
            var retry = type.GetMethod("IsPending");
            var confirm = type.GetMethod("TryConfirm");
            var release = type.GetMethod("Release");
            var deadline = type.GetField("PendingUntil");
            deadline.SetValue(left, 1.18f);
            deadline.SetValue(right, 1.18f);

            Assert.That(confirm.Invoke(left, new object[] { 10, 0.14f, 0.07f }), Is.False);
            Assert.That(confirm.Invoke(right, new object[] { 10, 0.02f, 0.07f }), Is.True);
            Assert.That(retry.Invoke(left, new object[] { 1.10f }), Is.True);
            Assert.That(confirm.Invoke(left, new object[] { 11, 0.03f, 0.07f }), Is.True);
            Assert.That(retry.Invoke(left, new object[] { 1.11f }), Is.False);
            Assert.That(confirm.Invoke(right, new object[] { 11, 0.03f, 0.07f }), Is.False);

            release.Invoke(left, null);
            Assert.That(confirm.Invoke(left, new object[] { 11, 0.03f, 0.07f }), Is.False);
            deadline.SetValue(left, 1.38f);
            Assert.That(retry.Invoke(left, new object[] { 1.40f }), Is.False);
            Assert.That(confirm.Invoke(left, new object[] { 12, 0.03f, 0.07f }), Is.True);
        }

        [Test]
        public void Initialize_컨트롤러Awake전에도_모터를_연결한다()
        {
            var root = new GameObject("Footstep initialization test");
            root.SetActive(false);
            try
            {
                var actor = root.AddComponent<PlayerActor>();
                var cachedMotor = typeof(ActorFootstep).GetField("_motor", BindingFlags.Instance | BindingFlags.NonPublic);
                var motor = root.AddComponent(cachedMotor.FieldType);
                var receiver = root.AddComponent<ActorFootstep>();
                Assert.That(actor.PlayerController, Is.Null, "컨트롤러 초기화 이전 순서를 재현한다.");

                receiver.Initialize(actor);

                Assert.That(cachedMotor.GetValue(receiver), Is.SameAs(motor));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void Raon_실제모델의_보행과착지에_프로필이벤트가_연결된다()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab");
            var receiver = model.GetComponent<ActorFootstep>();
            Assert.That(receiver, Is.Not.Null);
            Assert.That(receiver.settings, Is.Not.Null);
            Assert.That(receiver.settings.groundLayers.value, Is.Not.Zero);
            Assert.That(receiver.settings.fallbackSurface, Is.Not.Null);
            var animator = model.GetComponent<UPlayGround.Animation.PlayerActorAnimator>();
            foreach (var weapon in new[] { UPlayGround.Data.EnumType.WeaponType.NoWeapon, UPlayGround.Data.EnumType.WeaponType.Katana })
            foreach (var tag in new[] { UPlayGround.Data.Actor.Animation.MotionTags.Walk,
                UPlayGround.Data.Actor.Animation.MotionTags.Run, UPlayGround.Data.Actor.Animation.MotionTags.Sprint,
                UPlayGround.Data.Actor.Animation.MotionTags.Land })
            {
                var motion = animator.PlayerMotionSet.GetMotionSetAsset(weapon, tag);
                Assert.That(motion, Is.Not.Null, $"{weapon}/{tag}");
                bool hasFootstep = false;
                foreach (var clip in motion.motionSet.motions)
                foreach (var evt in clip.events)
                    if (evt is UPlayGround.Data.Event.FootstepEvent step && step.requireSurfaceProfile)
                    {
                        hasFootstep = true;
                        Assert.That(step.RequiresPostEvaluation, Is.True);
                        Assert.That(step.reentryPolicy, Is.EqualTo(UPlayGround.Data.Event.MotionEventReentryPolicy.EveryCrossing));
                    }
                Assert.That(hasFootstep, Is.True, $"{weapon}/{tag}: 발 디딤 이벤트 누락");
            }
        }

        [Test]
        public void TerrainMap_레이어인덱스를_지면프로필로_해석한다()
        {
            var map = ScriptableObject.CreateInstance<FootstepTerrainMapSO>();
            var grass = ScriptableObject.CreateInstance<FootstepSurfaceProfileSO>();
            var stone = ScriptableObject.CreateInstance<FootstepSurfaceProfileSO>();
            try
            {
                map.width = 2; map.height = 2;
                map.surfaces = new[] { grass, stone };
                map.dominantLayers = new byte[] { 0, 1, 1, 0 };
                Assert.That(map.Resolve(new Vector2(0.1f, 0.1f)), Is.SameAs(grass));
                Assert.That(map.Resolve(new Vector2(0.9f, 0.1f)), Is.SameAs(stone));
                Assert.That(map.Resolve(new Vector2(0.1f, 0.9f)), Is.SameAs(stone));
                Assert.That(map.Resolve(Vector2.one), Is.SameAs(grass));
                map.dominantLayers = new byte[] { 0 };
                Assert.That(map.Resolve(Vector2.zero), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(map);
                UnityEngine.Object.DestroyImmediate(grass);
                UnityEngine.Object.DestroyImmediate(stone);
            }
        }

        [TestCase(0.2f, 9.8f, 0.2f, true)]
        [TestCase(0.2f, 10.1f, 0.2f, false)]
        [TestCase(0.2f, 9f, 0.2f, false)]
        [TestCase(0.8f, 9.8f, 0.8f, false)]
        [TestCase(1.1f, 9.8f, 0.2f, false)]
        public void WaterSurface_얕은물만_허용하고_다리와_깊은물과_수면밖을_거부한다(float x, float y, float z, bool expected)
        {
            var root = new GameObject("Footstep water test");
            root.SetActive(false);
            root.transform.position = new Vector3(0f, 10f, 0f);
            var mesh = new Mesh
            {
                vertices = new[] { Vector3.zero, Vector3.right, Vector3.forward },
                triangles = new[] { 0, 2, 1 }
            };
            var filter = root.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var water = root.AddComponent<FootstepWaterSurface>();
            water.surfaceMesh = filter;
            root.SetActive(true);
            water.RefreshMesh();
            try
            {
                bool actual = water.TryGetPoint(new Vector3(x, y, z), out var surface);
                Assert.That(actual, Is.EqualTo(expected));
                if (expected) Assert.That(surface.y, Is.EqualTo(10f).Within(0.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }
    }
}
