using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            Assert.That(warp.endTime, Is.EqualTo(originalWarp.endTime));
            MotionWarpWindowSettings settings = warp.ResolveSettings();
            Assert.That(settings.modifierType, Is.EqualTo(MotionWarpModifierType.DeltaWarp));
            Assert.That(settings.arrivalMode, Is.EqualTo(WarpArrivalMode.ContactShell));
            Assert.That(settings.targetPolicy, Is.EqualTo(MotionWarpTargetPolicy.Snapshot));
            Assert.That(warp.playbackRateWarpPolicy, Is.EqualTo(PlaybackRateWarpPolicy.Disabled));
            Assert.That(settings.usePlaybackRateWarp, Is.False);
            Assert.That(settings.amplifyEnabled, Is.False);
            Assert.That(settings.maxCorrectionRatio, Is.InRange(0.01f, 0.4f));
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

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void 실제_검격_궤적은_먼_타겟까지_끌려가지_않고_타격전에_보정을_끝낸다(int frameRate)
        {
            var owner = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(Root + "Humanoid_Player_KatanaAnimationSet.asset");
            foreach (string key in AttackKeys())
            {
                MotionSetAsset asset = owner.GetAbilityMotionAsset(new MotionKey(key));
                var warp = asset.motionSet.motions.Single().events.OfType<MotionEvent_MotionWarp>().SingleOrDefault();
                if (warp == null) continue;
                MotionWarpRootMotionBakeProfile profile = warp.bakedProfiles.First();
                MotionWarpWindowSettings settings = warp.ResolveSettings();
                var actor = new GameObject("WarpTestActor");
                var target = new GameObject("WarpTestTarget");
                try
                {
                    var controller = actor.AddComponent<MotionWarpController>();
                    target.transform.position = Vector3.forward * 2f;
                    controller.SetTarget(target.transform);
                    controller.BeginWarpWindow(settings);
                    // 리그 선택은 데이터 테스트에서 검사하고 여기서는 동일 궤적에 대한 실제 보정 경로를 격리한다.
                    SetControllerField(controller, "_activeTrajectoryProfile", profile);
                    SetControllerField(controller, "_hasValidRootMotionProfile", true);
                    System.Type totalType = typeof(MotionWarpController).GetNestedType("RootMotionTotal", BindingFlags.NonPublic);
                    SetControllerField(controller, "_activeTotal", System.Activator.CreateInstance(totalType, profile.localTotal, profile.pathLen));
                    float time = 0f;
                    Vector3 position = Vector3.zero;
                    float addedPath = 0f;
                    while (time < settings.duration - 0.000001f)
                    {
                        float step = Mathf.Min(1f / frameRate, settings.duration - time);
                        float previous = time / settings.duration;
                        time += step;
                        float current = time / settings.duration;
                        SetControllerField(controller, "_warpRemainingTime", settings.duration - time);
                        Assert.That(MotionWarpTrajectory.TryEvaluateFrame(profile, previous, current,
                            settings.duration, false, null, 0f, out MotionWarpTrajectoryFrame frame), Is.True);
                        Vector3 rawVelocity = frame.FrameLocalDelta / step;
                        Vector3 velocity = controller.EvaluateVelocity(rawVelocity, position, true,
                            settings.duration - time, settings.duration, settings.minDistance,
                            settings.maxDistance, settings.maxSpeed, step);
                        addedPath += (velocity - rawVelocity).magnitude * step;
                        position += velocity * step;
                        actor.transform.position = position;
                        Assert.That(float.IsNaN(position.z), Is.False, key);
                        Assert.That(controller.WarpPlayRateScale, Is.EqualTo(1f), key);
                        if (settings.duration - time <= settings.translationEndLeadTime)
                            Assert.That((velocity - rawVelocity).magnitude, Is.LessThan(0.001f), key);
                    }
                    Assert.That(addedPath, Is.LessThanOrEqualTo(profile.pathLen * settings.maxCorrectionRatio + 0.001f), key);
                    controller.EndMotionWarpForStateExit();
                    Assert.That(controller.IsMotionWarping, Is.False);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(actor);
                    UnityEngine.Object.DestroyImmediate(target);
                }
            }
        }

        private static void SetControllerField(MotionWarpController controller, string name, object value)
            => typeof(MotionWarpController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, value);

        [Test]
        public void 루트가_정지한_준비구간에서는_시간이_흘러도_위치를_보정하지_않는다()
        {
            var profile = new MotionWarpRootMotionBakeProfile
            {
                formatVersion = MotionEvent_MotionWarp.CurrentBakeFormatVersion,
                pathLen = 0.3f,
                localTotal = Vector3.forward * 0.3f,
                cumulativeLocalPositions = new[] { Vector3.zero, Vector3.zero, Vector3.forward * 0.3f },
                cumulativePathLengths = new[] { 0f, 0f, 0.3f },
                cumulativeYaw = new[] { 0f, 0f, 0f },
            };
            var actor = new GameObject("StationaryWarpTest");
            var target = new GameObject("StationaryWarpTarget");
            try
            {
                var controller = actor.AddComponent<MotionWarpController>();
                target.transform.position = Vector3.forward * 2f;
                controller.SetTarget(target.transform);
                MotionWarpWindowSettings settings = MotionWarpWindowSettings.Default(1f);
                settings.modifierType = MotionWarpModifierType.DeltaWarp;
                settings.arrivalMode = WarpArrivalMode.ContactShell;
                settings.overrideDistance = true;
                settings.maxDistance = 3f;
                settings.maxSpeed = 10f;
                settings.maxCorrectionDistance = 0.5f;
                settings.maxCorrectionRatio = 0.3f;
                controller.BeginWarpWindow(settings);
                SetControllerField(controller, "_activeTrajectoryProfile", profile);
                SetControllerField(controller, "_hasValidRootMotionProfile", true);
                System.Type totalType = typeof(MotionWarpController).GetNestedType("RootMotionTotal", BindingFlags.NonPublic);
                SetControllerField(controller, "_activeTotal", System.Activator.CreateInstance(totalType, profile.localTotal, profile.pathLen));
                SetControllerField(controller, "_warpRemainingTime", 0.75f);
                Vector3 velocity = controller.EvaluateVelocity(Vector3.zero, Vector3.zero,
                    true, 0.75f, 1f, 0f, 3f, 10f, 0.25f);
                Assert.That(velocity.magnitude, Is.LessThan(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actor);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
