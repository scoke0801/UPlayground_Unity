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
