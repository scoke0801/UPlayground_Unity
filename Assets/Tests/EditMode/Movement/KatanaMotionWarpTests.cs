using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Event;
using UPlayGround.MovementController;

namespace UPlayGround.Movement.Tests
{
    /// <summary>실제 Katana 매핑의 보정 정책과 원본 공격 타임라인 보존을 검증한다.</summary>
    public class KatanaMotionWarpTests
    {
        private const string Root = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/";

        private static ActorAnimationMotionSet LoadRaonKatanaMotionOwner()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab");
            return prefab.GetComponentInChildren<PlayerActorAnimator>(true).PlayerMotionSet
                .GetActorAnimationMotionSet(UPlayGround.Data.EnumType.WeaponType.Katana);
        }

        [Test]
        public void 공격에서_선택한_대상없음도_이벤트_재탐색을_막고_다음_타격에서_풀린다()
        {
            var owner = new GameObject("공격 대상 선정 검사");
            try
            {
                var warp = owner.AddComponent<MotionWarpController>();
                warp.BeginTargetLock();
                warp.SetTarget((Transform)null);
                Assert.That(warp.HasLockedTargetSelection("primary"), Is.True);
                Assert.That(warp.HasLockedTargetSelection("secondary"), Is.False);
                warp.BeginTargetLock();
                Assert.That(warp.HasLockedTargetSelection("primary"), Is.False);
                warp.SetTarget((Transform)null);
                warp.EndTargetLock();
                Assert.That(warp.HasLockedTargetSelection("primary"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        private static IEnumerable<string> AttackKeys()
        {
            for (int index = 0; index < 10; index++) yield return $"Katana.Light.{index:00}";
            for (int index = 0; index < 6; index++) yield return $"Katana.Heavy.{index:00}";
        }

        [TestCaseSource(nameof(AttackKeys))]
        public void 일반_공격은_원본_타이밍과_적_모션을_보존하며_접근을_제한한다(string key)
        {
            var owner = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(Root + "Humanoid_Player_KatanaAnimationSet.asset");
            var enemy = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(Root + "MonsterRaon_KatanaAnimationSet.asset");
            MotionSetAsset asset = owner.GetAbilityMotionAsset(new MotionKey(key));
            Assert.That(asset, Is.Not.Null, key);
            var motion = asset.motionSet.motions.Single();
            MotionEvent_MotionWarp warp = motion.events.OfType<MotionEvent_MotionWarp>().SingleOrDefault();
            // 기존에 워핑이 없는 강공격에는 타겟 이동을 새로 만들지 않는다.
            if (warp == null)
            {
                Assert.That(key, Is.EqualTo("Katana.Heavy.01"));
                return;
            }

            string path = AssetDatabase.GetAssetPath(asset);
            Assert.That(path, Does.EndWith("_PlayerGrounded.asset"));
            var original = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(path.Replace("_PlayerGrounded.asset", ".asset"));
            Assert.That(original, Is.Not.Null);
            Assert.That(enemy.GetAbilityMotionAsset(new MotionKey(key)), Is.Not.EqualTo(asset));
            var originalMotion = original.motionSet.motions.Single();
            Assert.That(motion.motionClip, Is.EqualTo(originalMotion.motionClip));
            Assert.That(motion.Duration, Is.EqualTo(originalMotion.Duration));
            Assert.That(motion.events.Count, Is.EqualTo(originalMotion.events.Count));
            for (int index = 0; index < motion.events.Count; index++)
            {
                if (motion.events[index] is MotionEvent_MotionWarp)
                    continue;
                Assert.That(JsonUtility.ToJson(motion.events[index]),
                    Is.EqualTo(JsonUtility.ToJson(originalMotion.events[index])), $"{key}: 이벤트 {index}");
            }
            var originalWarp = originalMotion.events.OfType<MotionEvent_MotionWarp>().Single();
            Assert.That(warp.startTime, Is.EqualTo(originalWarp.startTime));
            Assert.That(warp.endTime, Is.InRange(warp.startTime, motion.Duration));
            MotionWarpWindowSettings settings = warp.ResolveSettings();
            Assert.That(settings.modifierType, Is.EqualTo(MotionWarpModifierType.EndpointCorrection));
            Assert.That(settings.arrivalMode, Is.EqualTo(WarpArrivalMode.ContactShell));
            Assert.That(settings.targetPolicy, Is.EqualTo(MotionWarpTargetPolicy.Snapshot));
            Assert.That(warp.playbackRateWarpPolicy, Is.EqualTo(PlaybackRateWarpPolicy.Disabled));
            Assert.That(settings.usePlaybackRateWarp, Is.False);
            Assert.That(settings.amplifyEnabled, Is.False);
            Assert.That(settings.maxCorrectionRatio, Is.InRange(0.01f, 8f));
            Assert.That(settings.maxCorrectionDistance, Is.InRange(0.01f, 0.8f));
            Assert.That(warp.endTime - settings.translationEndLeadTime,
                Is.LessThan(motion.events.OfType<BeginCollisionEvent>().Min(hit => hit.startTime)));
            Assert.That(warp.bakedProfiles, Is.Not.Empty);
            foreach (MotionWarpRootMotionBakeProfile profile in warp.bakedProfiles)
            {
                Assert.That(warp.TryGetBakedProfile(profile.avatar, profile.animatorScale, out _), Is.True);
                Assert.That(profile.sourceFingerprint, Is.Not.Empty);
            }
        }

        [Test]
        public void Custom_모션의_이동금지와_수동_보정값은_런타임까지_유지된다()
        {
            var warp = new MotionEvent_MotionWarp
            {
                preset = MotionWarpPreset.Custom,
                endTime = 0.4f,
                translationWeight = 0f,
                rotationWeight = 0.7f,
                maxCorrectionDistance = 0.07f,
                maxCorrectionRatio = 0.1f,
            };
            MotionWarpWindowSettings settings = warp.ResolveSettings();
            Assert.That(settings.translationWeight, Is.Zero);
            Assert.That(settings.rotationWeight, Is.EqualTo(0.7f));
            Assert.That(settings.maxCorrectionDistance, Is.EqualTo(0.07f));
            Assert.That(settings.maxCorrectionRatio, Is.EqualTo(0.1f));
        }

        [Test]
        public void 실제_검격_프로필은_먼_목표에도_허용량만_접근한다()
        {
            var owner = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(Root + "Humanoid_Player_KatanaAnimationSet.asset");
            foreach (string key in AttackKeys())
            {
                var asset = owner.GetAbilityMotionAsset(new MotionKey(key));
                var warp = asset.motionSet.motions.Single().events.OfType<MotionEvent_MotionWarp>().SingleOrDefault();
                if (warp == null || warp.translationWeight == 0f) continue;
                var settings = warp.ResolveSettings();
                foreach (var profile in warp.bakedProfiles)
                {
                    Assert.That(MotionWarpTrajectory.MatchesSource(profile, asset.motionSet,
                        MotionWarpTrajectory.BuildSourceSignature(asset.motionSet)), Is.True, key);
                    bool resolved = MotionWarpMath.TryResolveEndpointCorrection(profile.localTotal,
                        profile.pathLen, Vector3.forward * 7f, settings, out var correction);
                    if (resolved)
                    {
                        Assert.That(settings.correctionLimitPolicy,
                            Is.EqualTo(MotionWarpCorrectionLimitPolicy.Clamp), key);
                        Assert.That(correction.magnitude,
                            Is.LessThanOrEqualTo(settings.maxCorrectionDistance + .0001f), key);
                        Assert.That(Vector3.Distance(profile.localTotal + correction, Vector3.forward * 7f),
                            Is.GreaterThan(1f), "일부 접근을 먼 목표 도달로 바꾸면 안 된다: " + key);
                    }
                    else
                        Assert.That(correction, Is.EqualTo(Vector3.zero), key);
                }
            }
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void 이동을_끄고_Live_타겟이_옆으로_지나가도_회전_상한은_윈도우_시작_기준이다(int frameRate)
        {
            var actor = new GameObject("RotationLimitActor");
            var target = new GameObject("RotationLimitTarget");
            try
            {
                var controller = actor.AddComponent<MotionWarpController>();
                target.transform.position = Vector3.forward * 2f;
                controller.SetTarget(target.transform);
                var settings = MotionWarpWindowSettings.Default(1f);
                settings.modifierType = MotionWarpModifierType.DeltaWarp;
                settings.arrivalMode = WarpArrivalMode.ContactShell;
                settings.targetPolicy = MotionWarpTargetPolicy.Live;
                settings.translationWeight = 0f;
                settings.maxWarpAngle = 20f;
                settings.overrideDistance = true;
                settings.maxDistance = 3f;
                controller.BeginWarpWindow(settings);
                Quaternion rotation = Quaternion.identity;
                for (int frame = 1; frame <= frameRate; frame++)
                {
                    float progress = frame / (float)frameRate;
                    target.transform.position = Quaternion.Euler(0f, 120f * progress, 0f) * Vector3.forward * 2f;
                    Assert.That(controller.TryEvaluateRotation(rotation, Vector3.zero, true,
                        1f - progress, 1f, 0f, 3f, 10f, out rotation), Is.True);
                    actor.transform.rotation = rotation;
                    Assert.That(Quaternion.Angle(Quaternion.identity, rotation), Is.LessThanOrEqualTo(20.001f));
                }
                controller.EndMotionWarpForStateExit();
                Assert.That(controller.TryEvaluateRotation(rotation, Vector3.zero, false,
                    0f, 1f, 0f, 3f, 10f, out _), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actor);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void 회전_소유권은_전신_유효_윈도우에만_있다()
        {
            var warp = new MotionEvent_MotionWarp { endTime = 0.3f, rotationWeight = 0f };
            var set = new MotionSet { globalEvents = new List<MotionEventBase> { warp } };
            Assert.That(MotionWarpController.HasAuthoredRotation(set), Is.False);
            warp.rotationWeight = 1f;
            Assert.That(MotionWarpController.HasAuthoredRotation(set), Is.True);
            set.globalEvents.Clear();
            set.motions = new List<UPlayGround.Animation.Motion> { new() { events = new List<MotionEventBase> { warp } } };
            Assert.That(MotionWarpController.HasAuthoredRotation(set), Is.True);
            warp.endTime = 0f;
            Assert.That(MotionWarpController.HasAuthoredRotation(set), Is.False);
        }

        /// <summary>
        /// 기본 공격의 방향 입력은 해당 모션이 조준할 수 있는 범위에서만 대상을 선정한다.
        /// </summary>
        [TestCase("Katana.Light.00")]
        [TestCase("Katana.Light.01")]
        [TestCase("Katana.Light.02")]
        public void 기본타_조준_보정_상한은_대상_선정_원뿔을_덮되_넘지_않는다(string key)
        {
            var owner = LoadRaonKatanaMotionOwner();
            var motion = owner.GetAbilityMotionAsset(new MotionKey(key)).motionSet;
            var warp = motion.motions.Single().events.OfType<MotionEvent_MotionWarp>().Single();
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Player.prefab");
            var combat = player.GetComponentInChildren<UPlayGround.Components.PlayerCombat>(true);
            var serialized = new SerializedObject(combat);
            float inputCone = serialized.FindProperty("_attackAssistSearchAngle").floatValue;
            float freeCone = serialized.FindProperty("_freeAttackFacingSearchAngle").floatValue;
            MotionWarpWindowSettings settings = warp.ResolveSettings();
            Assert.That(settings.translationWeight, Is.GreaterThan(0f));
            var resolve = typeof(UPlayGround.Components.PlayerCombat).GetMethod("ResolveAttackFacingSearchAngle",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That((float)resolve.Invoke(combat, new object[] { inputCone, Vector3.forward, motion }),
                Is.EqualTo(Mathf.Min(inputCone, freeCone, settings.maxWarpAngle)));
            Assert.That((float)resolve.Invoke(combat, new object[] { inputCone, Vector3.zero, motion }),
                Is.EqualTo(freeCone));
        }

        [Test]
        public void 회전전용_워프는_남아있는_베이크로_원본_이동을_대체하지_않는다()
        {
            var actor = new GameObject("RotationOnlyRoot");
            try
            {
                var controller = actor.AddComponent<MotionWarpController>();
                var settings = MotionWarpWindowSettings.Default(1f);
                settings.modifierType = MotionWarpModifierType.DeltaWarp;
                settings.translationWeight = 0f;
                settings.usePlaybackRateWarp = false;
                controller.BeginWarpWindow(settings);
                Vector3 rawVelocity = new(0f, -2f, 3f);
                Vector3 result = controller.EvaluateVelocity(rawVelocity, Vector3.zero, true,
                    0.5f, 1f, 0f, 4f, 10f, 0.5f);
                Assert.That(result, Is.EqualTo(rawVelocity));
            }
            finally { UnityEngine.Object.DestroyImmediate(actor); }
        }

        [Test]
        public void 라온_카타나_바인딩은_전용_공격과_회피교대_모션을_사용한다()
        {
            var owner = LoadRaonKatanaMotionOwner();
            Assert.That(AssetDatabase.GetAssetPath(owner), Is.EqualTo(Root + "UltimateRedesign/Bindings/Raon_WeaponMotions.asset"));
            Assert.That(AssetDatabase.GetAssetPath(owner.fallbackMotionSet),
                Is.EqualTo("Assets/10.Datas/Actor/Footsteps/Motions/Set_Katana.asset"));
            var shared = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(Root + "Humanoid_Player_KatanaAnimationSet.asset");
            foreach (string attack in new[] { "Light.00", "Light.01", "Light.02", "Heavy.00", "SwapEvade" })
            {
                var key = new MotionKey($"Katana.{attack}");
                var motion = owner.GetAbilityMotionAsset(key);
                string path = $"Assets/10.Datas/Ability/Player/Raon/Motion_Raon_{attack.Replace('.', '_')}_Flow.asset";
                Assert.That(AssetDatabase.GetAssetPath(motion), Is.EqualTo(path));
                Assert.That(motion, Is.Not.SameAs(shared.GetAbilityMotionAsset(key)));
                Assert.That(motion.motionSet.motions.SelectMany(value => value.events).OfType<BeginCollisionEvent>(),
                    Is.Not.Empty, key.ToString());
            }
            var dashKey = new MotionKey("Katana.Dash.00");
            var dash = owner.GetAbilityMotionAsset(dashKey);
            Assert.That(dash, Is.Not.Null);
            Assert.That(dash, Is.Not.SameAs(shared.GetAbilityMotionAsset(dashKey)));
            Assert.That(dash.motionSet.motions.Single().events.OfType<MotionEvent_MotionWarp>().Single().bakedProfiles,
                Is.Not.Empty);
        }

        [TestCase("Player/MonsterRaon_KatanaAnimationSet.asset", "Monster.Bokusei.Attack.1.01")]
        [TestCase("Player/MonsterRaon_KatanaAnimationSet.asset", "Monster.Bokusei.HeavyAttack.1.10")]
        [TestCase("Humanoid/P09_Armor06_GreatSwordAnimationSet.asset", "Humanoid.GreatSwordAttackData.Attack.1.01")]
        [TestCase("Humanoid/P09_Armor06_GreatSwordAnimationSet.asset", "Humanoid.GreatSwordAttackData.HeavyAttack.1.11")]
        public void 대표_적_공격은_첫_타격_전에_방향을_확정하고_판정과_연출을_보존한다(string ownerPath, string key)
        {
            const string motionRoot = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/";
            var owner = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(motionRoot + ownerPath);
            Assert.That(owner, Is.Not.Null);
            var asset = owner.GetAbilityMotionAsset(new MotionKey(key));
            var path = AssetDatabase.GetAssetPath(asset);
            var original = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(path.Replace("_BossCommitted", "").Replace("_P09Committed", ""));
            Assert.That(original, Is.Not.EqualTo(asset));
            var motion = asset.motionSet.motions.Single();
            var warp = motion.events.OfType<MotionEvent_MotionWarp>().Single();
            Assert.That(motion.motionClip, Is.EqualTo(original.motionSet.motions.Single().motionClip));
            Assert.That(warp.endTime, Is.LessThanOrEqualTo(motion.events.OfType<BeginCollisionEvent>().Min(e => e.startTime) - 0.149f));
            Assert.That(warp.ResolveSettings().targetPolicy, Is.EqualTo(MotionWarpTargetPolicy.Snapshot));
            Assert.That(warp.ResolveSettings().arrivalMode, Is.EqualTo(WarpArrivalMode.ContactShell));
            Assert.That(warp.ResolveSettings().usePlaybackRateWarp, Is.False);
            if (warp.translationWeight > 0f) Assert.That(warp.bakedProfiles, Is.Not.Empty);
            foreach (var profile in warp.bakedProfiles)
                Assert.That(warp.TryGetBakedProfile(profile.avatar, profile.animatorScale, out _), Is.True);
            var originalEvents = original.motionSet.motions.Single().events.Where(e => e is not MotionEvent_MotionWarp).Select(e => JsonUtility.ToJson(e));
            Assert.That(motion.events.Where(e => e is not MotionEvent_MotionWarp).Select(e => JsonUtility.ToJson(e)), Is.EqualTo(originalEvents));
        }

        [Test]
        public void 루트가_정지한_준비구간에서는_시간이_흘러도_위치를_보정하지_않는다()
        {
            Assert.That(MotionWarpMath.DistributeEndpointCorrection(Vector3.forward * 0.1f,
                0f, 0f, 0.3f, 0f), Is.EqualTo(Vector3.zero));
            Assert.That(MotionWarpMath.DistributeEndpointCorrection(Vector3.forward * 0.1f,
                0f, 0.1f, 0.3f, 0f), Is.EqualTo(Vector3.zero));
        }
    }
}
