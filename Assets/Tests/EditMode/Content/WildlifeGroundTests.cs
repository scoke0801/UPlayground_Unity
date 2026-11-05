using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.World;
using Object = UnityEngine.Object;

namespace UPlayGround.Content.Tests
{
    /// <summary>단순화된 NavMesh 높이와 실제 지면 높이를 분리하는 접지 계약을 검사한다.</summary>
    public sealed class WildlifeGroundTests
    {
        [TestCase(false)]
        [Test]
        public void 동료가_귀환점을_차지하면_가까운_빈_지점으로_돌아간다()
        {
            var profile = ScriptableObject.CreateInstance<WildlifeProfileSO>();
            var root = new GameObject("귀환 검사");
            var peer = new GameObject("귀환점 점유 동료");
            UnityEngine.AI.NavMeshData data = null;
            UnityEngine.AI.NavMeshDataInstance instance = default;
            try
            {
                var origin = new Vector3(-3000, 2000, -3000);
                var sources = new System.Collections.Generic.List<UnityEngine.AI.NavMeshBuildSource>
                {
                    new UnityEngine.AI.NavMeshBuildSource
                    {
                        shape = UnityEngine.AI.NavMeshBuildSourceShape.Box,
                        size = new Vector3(20, 0.2f, 20),
                        transform = Matrix4x4.TRS(origin, Quaternion.identity, Vector3.one)
                    }
                };
                data = UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(UnityEngine.AI.NavMesh.GetSettingsByID(0),
                    sources, new Bounds(origin, new Vector3(24, 4, 24)), Vector3.zero, Quaternion.identity);
                Assert.That(data, Is.Not.Null);
                instance = UnityEngine.AI.NavMesh.AddNavMeshData(data);
                Assert.That(UnityEngine.AI.NavMesh.SamplePosition(origin, out var home, 1, UnityEngine.AI.NavMesh.AllAreas), Is.True);
                root.transform.position = home.position + Vector3.forward * 4;
                peer.transform.position = home.position;
                var agent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
                Assert.That(agent.Warp(root.transform.position), Is.True);
                Type type = Type.GetType("UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature, Assembly-CSharp", true);
                Component creature = root.AddComponent(type);
                Component neighbour = peer.AddComponent(type);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                profile.RoamRadius = 10;
                profile.PersonalSpace = 1;
                type.GetField("_profile", flags).SetValue(creature, profile);
                type.GetField("_profile", flags).SetValue(neighbour, profile);
                type.GetField("_home", flags).SetValue(creature, home.position);
                type.GetField("_agent", flags).SetValue(creature, agent);
                type.GetField("_path", flags).SetValue(creature, new UnityEngine.AI.NavMeshPath());
                var neighbours = Array.CreateInstance(type, 1);
                neighbours.SetValue(neighbour, 0);
                type.GetField("_neighbours", flags).SetValue(creature, neighbours);
                Assert.That((bool)type.GetMethod("TryReturnHome", flags).Invoke(creature, null), Is.True);
                var target = (Vector3)type.GetField("_target", flags).GetValue(creature);
                Assert.That(Vector3.Distance(target, home.position), Is.InRange(profile.PersonalSpace, profile.RoamRadius));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(peer);
                if (instance.valid) instance.Remove();
                if (data != null) Object.DestroyImmediate(data);
                Object.DestroyImmediate(profile);
            }
        }
        [TestCase(true)]
        public void 휴식_시간은_목적지에_도착한_뒤에만_줄어든다(bool hasArrived)
        {
            var root = new GameObject("생활 리듬 검사");
            var profile = ScriptableObject.CreateInstance<WildlifeProfileSO>();
            try
            {
                Type type = Type.GetType("UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature, Assembly-CSharp", true);
                Component creature = root.AddComponent(type);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                // 비행 목적지 판정은 NavMesh 없이 도착 전후의 시간 계약을 검사할 수 있다.
                profile.IsFlying = true;
                type.GetField("_profile", flags).SetValue(creature, profile);
                type.GetField("_target", flags).SetValue(creature, hasArrived ? Vector3.zero : Vector3.forward * 10);
                type.GetField("_idleTime", flags).SetValue(creature, 5f);
                type.GetMethod("UpdateRest", flags).Invoke(creature, new object[] { 1f });
                Assert.That((float)type.GetField("_idleTime", flags).GetValue(creature), Is.EqualTo(hasArrived ? 4f : 5f));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(profile); }
        }

        [TestCase(8f, false, "Watching")]
        [TestCase(4f, false, "Avoiding")]
        [TestCase(1f, false, "Fleeing")]
        [TestCase(1f, true, "Watching")]
        public void 접근_거리와_반응_데이터에_따라_경계_회피_도주를_구분한다(float distance, bool freezes, string expected)
        {
            var root = new GameObject("접근 단계 검사");
            var profile = ScriptableObject.CreateInstance<WildlifeProfileSO>();
            try
            {
                Type type = Type.GetType("UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature, Assembly-CSharp", true);
                Component creature = root.AddComponent(type);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                profile.AlertDistance = 10;
                profile.AvoidDistance = 6;
                profile.FleeDistance = 2;
                profile.FreezesWhenApproached = freezes;
                type.GetField("_profile", flags).SetValue(creature, profile);
                var method = type.GetMethod("UpdateThreat", flags);
                method.Invoke(creature, new object[] { Vector3.forward * 8, true });
                type.GetField("_watchTime", flags).SetValue(creature, 10f);
                method.Invoke(creature, new object[] { Vector3.forward * distance, true });
                Assert.That(type.GetField("_activity", flags).GetValue(creature).ToString(), Is.EqualTo(expected));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(profile); }
        }

        [Test]
        public void 느린_기어가기_속도를_가축_보행_최솟값으로_올리지_않는다()
        {
            var profile = ScriptableObject.CreateInstance<WildlifeProfileSO>();
            try
            {
                profile.MoveSpeed = profile.FleeSpeed = 0.015f;
                typeof(WildlifeProfileSO).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(profile, null);
                Assert.That(profile.MoveSpeed, Is.EqualTo(0.015f));
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void 지면이_있으면_발을_맞추고_없으면_경로_높이를_보존한다(bool hasGround)
        {
            var root = new GameObject("동물 접지 검사");
            var floor = new GameObject("검사 지면", typeof(BoxCollider));
            var profile = ScriptableObject.CreateInstance<WildlifeProfileSO>();
            try
            {
                var origin = new Vector3(-2000, 1000, -2000);
                floor.transform.position = origin + Vector3.down * 0.1f;
                floor.GetComponent<BoxCollider>().size = new Vector3(4, 0.2f, 4);
                floor.SetActive(hasGround);
                Physics.SyncTransforms();
                Type type = Type.GetType("UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature, Assembly-CSharp", true);
                Component creature = root.AddComponent(type);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                profile.GroundClearance = 0.01f;
                profile.LandingProbeDistance = 4;
                type.GetField("_profile", flags).SetValue(creature, profile);
                Vector3 navigationPosition = origin + Vector3.up * 0.35f;
                var result = (Vector3)type.GetMethod("ProjectGroundPosition", flags)
                    .Invoke(creature, new object[] { navigationPosition });
                Assert.That(result.x, Is.EqualTo(navigationPosition.x));
                Assert.That(result.z, Is.EqualTo(navigationPosition.z));
                Assert.That(result.y, Is.EqualTo(origin.y + (hasGround ? 0.01f : 0.35f)).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(profile);
            }
        }
    }
}
