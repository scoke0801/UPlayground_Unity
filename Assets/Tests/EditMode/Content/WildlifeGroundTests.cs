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
        [TestCase(0f, 0f)]
        [TestCase(0.0005f, 0f)]
        [TestCase(0.25f, 4.5f)]
        [TestCase(1f, 18f)]
        public void 몸_회전은_실제_보행량에_비례하며_정지하면_회전하지_않는다(float actualSpeed, float expectedAngle)
        {
            var root = new GameObject("동물 방향 전환 검사");
            var profile = ScriptableObject.CreateInstance<WildlifeProfileSO>();
            try
            {
                Type type = Type.GetType("UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature, Assembly-CSharp", true);
                Component creature = root.AddComponent(type);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                profile.TurnSpeed = 180;
                type.GetField("_profile", flags).SetValue(creature, profile);
                type.GetMethod("RotateGroundBody", flags).Invoke(creature,
                    new object[] { Vector3.right, 0.1f, actualSpeed, 1f });
                Assert.That(Quaternion.Angle(Quaternion.identity, root.transform.rotation),
                    Is.EqualTo(expectedAngle).Within(0.01f));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(profile); }
        }

        [Test]
        public void 동료가_차지한_귀환점과_주변의_빈_지점을_구분한다()
        {
            var profile = ScriptableObject.CreateInstance<WildlifeProfileSO>();
            var root = new GameObject("귀환 간격 검사");
            var peer = new GameObject("귀환점 점유 동료");
            try
            {
                Type type = Type.GetType("UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature, Assembly-CSharp", true);
                Component creature = root.AddComponent(type);
                Component neighbour = peer.AddComponent(type);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                profile.PersonalSpace = 1;
                type.GetField("_profile", flags).SetValue(creature, profile);
                type.GetField("_profile", flags).SetValue(neighbour, profile);
                var neighbours = Array.CreateInstance(type, 1);
                neighbours.SetValue(neighbour, 0);
                type.GetField("_neighbours", flags).SetValue(creature, neighbours);
                var check = type.GetMethod("HasPersonalSpace", flags);
                Assert.That((bool)check.Invoke(creature, new object[] { Vector3.zero }), Is.False, "점유된 최초 귀환점");
                Assert.That((bool)check.Invoke(creature, new object[] { Vector3.forward * 0.5f }), Is.False, "동료의 개인 간격 안");
                Assert.That((bool)check.Invoke(creature, new object[] { Vector3.forward * 2 }), Is.True, "같은 생활권의 빈 지점");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(peer);
                Object.DestroyImmediate(profile);
            }
        }
        [TestCase(false)]
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

        [TestCase(9f, 6f, "Fleeing")]
        [TestCase(9f, 8.5f, "Watching")]
        public void 빠르게_다가오면_도주_거리_밖에서도_먼저_도주한다(float firstDistance, float secondDistance, string expected)
        {
            var root = new GameObject("접근 속도 도주 검사");
            var profile = ScriptableObject.CreateInstance<WildlifeProfileSO>();
            try
            {
                Type type = Type.GetType("UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature, Assembly-CSharp", true);
                Component creature = root.AddComponent(type);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                profile.AlertDistance = 10;
                profile.AvoidDistance = 6;
                profile.FleeDistance = 2;
                profile.FleeTimeToContact = 2;
                profile.DecisionInterval = 0.5f;
                type.GetField("_profile", flags).SetValue(creature, profile);
                var method = type.GetMethod("UpdateThreat", flags);
                method.Invoke(creature, new object[] { Vector3.forward * firstDistance, true });
                method.Invoke(creature, new object[] { Vector3.forward * secondDistance, true });
                Assert.That(type.GetField("_activity", flags).GetValue(creature).ToString(), Is.EqualTo(expected));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(profile); }
        }

        [TestCase(5f, true)]
        [TestCase(20f, false)]
        public void 도주하는_동료를_보면_반응_지연_뒤_같은_위협에서_따라_도주한다(float peerDistance, bool expectFlee)
        {
            var root = new GameObject("무리 도주 검사");
            var peer = new GameObject("도주 동료");
            var profile = ScriptableObject.CreateInstance<WildlifeProfileSO>();
            try
            {
                Type type = Type.GetType("UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature, Assembly-CSharp", true);
                Component creature = root.AddComponent(type);
                Component neighbour = peer.AddComponent(type);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                profile.SocialFleeDistance = 10;
                profile.AlarmDelaySeconds = new Vector2(0.2f, 0.2f);
                type.GetField("_profile", flags).SetValue(creature, profile);
                type.GetField("_profile", flags).SetValue(neighbour, profile);
                peer.transform.position = Vector3.forward * peerDistance;
                var activityField = type.GetField("_activity", flags);
                activityField.SetValue(neighbour, Enum.Parse(activityField.FieldType, "Fleeing"));
                type.GetField("_lastThreatPosition", flags).SetValue(neighbour, Vector3.back * 7);
                var neighbours = Array.CreateInstance(type, 1);
                neighbours.SetValue(neighbour, 0);
                type.GetField("_neighbours", flags).SetValue(creature, neighbours);
                type.GetMethod("QueueSocialFlee", flags).Invoke(creature, null);
                var update = type.GetMethod("UpdateSocialFlee", flags);
                update.Invoke(creature, new object[] { 0.1f });
                Assert.That(activityField.GetValue(creature).ToString(), Is.EqualTo("Roaming"), "반응 지연 중");
                update.Invoke(creature, new object[] { 0.2f });
                Assert.That(activityField.GetValue(creature).ToString(), Is.EqualTo(expectFlee ? "Fleeing" : "Roaming"));
                if (expectFlee)
                    Assert.That((Vector3)type.GetField("_alarmThreat", flags).GetValue(creature), Is.EqualTo(Vector3.back * 7));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(peer);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void 섭식_중_일정_간격마다_머리를_들었다가_다시_먹는다()
        {
            var root = new GameObject("섭식 경계 검사");
            var profile = ScriptableObject.CreateInstance<WildlifeProfileSO>();
            try
            {
                Type type = Type.GetType("UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature, Assembly-CSharp", true);
                Component creature = root.AddComponent(type);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                profile.FeedBurstSeconds = new Vector2(2, 2);
                profile.VigilanceSeconds = new Vector2(1, 1);
                type.GetField("_profile", flags).SetValue(creature, profile);
                var restField = type.GetField("_restActivity", flags);
                restField.SetValue(creature, Enum.ToObject(restField.FieldType, 1));
                type.GetField("_feedBurstTime", flags).SetValue(creature, 2f);
                var update = type.GetMethod("UpdateFeedVigilance", flags);
                var headUp = type.GetField("_headUpTime", flags);
                update.Invoke(creature, new object[] { 1f });
                Assert.That((float)headUp.GetValue(creature), Is.EqualTo(0f), "섭식 시간이 남은 동안");
                update.Invoke(creature, new object[] { 1f });
                Assert.That((float)headUp.GetValue(creature), Is.EqualTo(1f), "섭식 시간이 끝나면 머리를 든다");
                update.Invoke(creature, new object[] { 1f });
                Assert.That((float)headUp.GetValue(creature), Is.LessThanOrEqualTo(0f), "경계가 끝나면 다시 먹는다");
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(profile); }
        }

        [Test]
        public void 먹이_탐색은_섭식_관찰_짧은_이동_다음_섭식으로_이어진다()
        {
            var root = new GameObject("먹이 탐색 검사");
            var profile = ScriptableObject.CreateInstance<WildlifeProfileSO>();
            var randomState = UnityEngine.Random.state;
            try
            {
                Type type = Type.GetType("UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature, Assembly-CSharp", true);
                Component creature = root.AddComponent(type);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                profile.FeedSeconds = new Vector2(0.1f, 0.1f);
                profile.ObserveSeconds = new Vector2(1, 1);
                profile.ForageStepDistance = new Vector2(0.2f, 0.4f);
                type.GetField("_profile", flags).SetValue(creature, profile);
                type.GetField("_isForaging", flags).SetValue(creature, true);
                type.GetField("_forageStepsRemaining", flags).SetValue(creature, 2);
                var beginRest = type.GetMethod("BeginRest", flags);
                beginRest.Invoke(creature, null);
                Assert.That(type.GetField("_forageStepsRemaining", flags).GetValue(creature), Is.EqualTo(1));
                Assert.That(type.GetField("_restActivity", flags).GetValue(creature).ToString(), Is.EqualTo("Feed"));
                // 도착 판정만 비행식으로 바꿔 NavMesh 없이 생활 상태 전이를 검사한다.
                profile.IsFlying = true;
                type.GetMethod("UpdateRest", flags).Invoke(creature, new object[] { 0.2f });
                Assert.That(type.GetField("_restActivity", flags).GetValue(creature).ToString(), Is.EqualTo("Observe"));
                Assert.That(type.GetField("_idleTime", flags).GetValue(creature), Is.EqualTo(1f));
                Vector3 destination = (Vector3)type.GetMethod("ChooseRoamDestination", flags).Invoke(creature, null);
                Assert.That(destination.magnitude, Is.InRange(0.199f, 0.401f));
                beginRest.Invoke(creature, null);
                Assert.That(type.GetField("_restActivity", flags).GetValue(creature).ToString(), Is.EqualTo("Feed"));
                Assert.That(type.GetField("_forageStepsRemaining", flags).GetValue(creature), Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Random.state = randomState;
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(profile);
            }
        }

        [TestCase("Fleeing", false, false, 2f, true)]
        [TestCase("Watching", false, false, 2f, false)]
        [TestCase("Fleeing", true, false, 2f, false)]
        [TestCase("Fleeing", false, true, 2f, false)]
        [TestCase("Fleeing", false, false, 8f, false)]
        public void 동료의_도주에_경계하고_경계는_재전파하지_않는다(
            string activity, bool isDead, bool isDifferentProfile, float distance, bool expectsAlarm)
        {
            var root = new GameObject("무리 경계 검사");
            var peer = new GameObject("동료");
            var profile = ScriptableObject.CreateInstance<WildlifeProfileSO>();
            var otherProfile = ScriptableObject.CreateInstance<WildlifeProfileSO>();
            try
            {
                Type type = Type.GetType("UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature, Assembly-CSharp", true);
                Component creature = root.AddComponent(type);
                Component neighbour = peer.AddComponent(type);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                profile.SocialAlertDistance = 6;
                type.GetField("_profile", flags).SetValue(creature, profile);
                type.GetField("_profile", flags).SetValue(neighbour, isDifferentProfile ? otherProfile : profile);
                var activityField = type.GetField("_activity", flags);
                activityField.SetValue(neighbour, Enum.Parse(activityField.FieldType, activity));
                type.GetField("_isDead", flags).SetValue(neighbour, isDead);
                peer.transform.position = Vector3.forward * distance;
                var neighbours = Array.CreateInstance(type, 1);
                neighbours.SetValue(neighbour, 0);
                type.GetField("_neighbours", flags).SetValue(creature, neighbours);
                type.GetField("_isForaging", flags).SetValue(creature, true);
                var update = type.GetMethod("UpdateThreat", flags);
                update.Invoke(creature, new object[] { Vector3.zero, false });
                Assert.That(activityField.GetValue(creature).ToString(), Is.EqualTo(expectsAlarm ? "Watching" : "Roaming"));
                if (expectsAlarm)
                {
                    Assert.That(type.GetField("_watchPosition", flags).GetValue(creature), Is.EqualTo(peer.transform.position));
                    Assert.That(type.GetField("_isForaging", flags).GetValue(creature), Is.False);
                    activityField.SetValue(neighbour, Enum.Parse(activityField.FieldType, "Roaming"));
                    type.GetField("_calmTime", flags).SetValue(creature, 0f);
                    update.Invoke(creature, new object[] { Vector3.zero, false });
                    Assert.That(activityField.GetValue(creature).ToString(), Is.EqualTo("Roaming"));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(peer);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(otherProfile);
            }
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
