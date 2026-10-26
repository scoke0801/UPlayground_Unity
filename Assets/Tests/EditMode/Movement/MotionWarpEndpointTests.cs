using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Event;
using UPlayGround.MovementController;

namespace UPlayGround.Movement.Tests
{
    /// <summary>끝점 보정의 프레임 독립성·원본 보존·명시적 설정을 검증한다.</summary>
    public sealed class MotionWarpEndpointTests
    {
        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        [TestCase(7)]
        public void 구간_끝을_넘는_스팬도_같은_보정량을_한번만_소비한다(int rate)
        {
            Vector3 correction = new(0.08f, 0f, 0.12f);
            Vector3 sum = Vector3.zero;
            float previous = 0f;
            for (int frame = 1; frame <= rate + 1; frame++)
            {
                float current = Mathf.Clamp01(frame / (float)rate);
                float before = previous * previous;
                float after = current * current;
                sum += MotionWarpMath.DistributeEndpointCorrection(correction, before, after, 1f, after - before);
                previous = current;
            }
            Assert.That((sum - correction).magnitude, Is.LessThan(0.001f));
        }

        [Test]
        public void 정지와_충돌로_소비하지_못한_보정은_나중에_보충하지_않는다()
        {
            Vector3 correction = Vector3.forward * 0.2f;
            Assert.That(MotionWarpMath.DistributeEndpointCorrection(correction, 0f, 0.5f, 1f, 0f), Is.EqualTo(Vector3.zero));
            Assert.That(MotionWarpMath.DistributeEndpointCorrection(correction, 0.5f, 1f, 1f, 0.5f), Is.EqualTo(correction * 0.5f));
            Assert.That(MotionWarpMath.DistributeEndpointCorrection(correction, 1f, 1f, 1f, 1f), Is.EqualTo(Vector3.zero));
        }

        [TestCase(0f, 0f)]
        [TestCase(0.01f, 0.7f)]
        [TestCase(0.1f, 1f)]
        public void 제자리와_왕복_루트로_긴_전진을_생성하지_않는다(float net, float path)
        {
            var settings = Settings();
            Assert.That(MotionWarpMath.TryResolveEndpointCorrection(Vector3.forward * net, path,
                Vector3.forward, settings, out _), Is.False);
        }

        [Test]
        public void 같은_속력의_측면_이동도_벡터_보정으로_검사한다()
        {
            var settings = Settings();
            Assert.That(MotionWarpMath.TryResolveEndpointCorrection(Vector3.forward, 1f,
                Vector3.right, settings, out _), Is.False);
            Assert.That(MotionWarpMath.TryResolveEndpointCorrection(Vector3.forward, 1f,
                new Vector3(0.1f, 0f, 1.1f), settings, out var correction), Is.True);
            Assert.That(correction.magnitude, Is.EqualTo(Mathf.Sqrt(0.02f)).Within(0.0001f));
        }

        [Test]
        public void 프리셋_이름은_실행_설정과_탐색_범위를_덮어쓰지_않는다()
        {
            var warp = new MotionEvent_MotionWarp
            {
                preset = MotionWarpPreset.LightAttack, maxDistance = 2.1f,
                translationWeight = 0f, rotationWeight = 0.7f
            };
            var settings = warp.ResolveSettings();
            Assert.That(settings.maxDistance, Is.EqualTo(2.1f));
            Assert.That(settings.translationWeight, Is.Zero);
            Assert.That(settings.rotationWeight, Is.EqualTo(0.7f));
        }

        [Test]
        public void 공용_쌍도끼_접근_입력은_전진_원본과_유효_판정에_연결된다()
        {
            var set = AssetDatabase.LoadAssetAtPath<AbilitySetSO>("Assets/10.Datas/Ability/Migrated/PlayerDoubleAxeAttackData/AbilitySet_PlayerDoubleAxeAttackData.asset");
            var ability = set.GetCombatAbility(PlayerCombatAbilitySlot.DashAttack);
            Assert.That(ability, Is.Not.Null);
            var payload = ability.variants[0].executionPayload as UPlayGroundMotionAbilityPayloadSO;
            Assert.That(payload, Is.Not.Null);
            var mapping = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Humanoid_Player_DoubleAxeAnimationSet.asset");
            var motion = mapping.GetAbilityMotionAsset(payload.attackInfo.motionKey);
            Assert.That(motion, Is.Not.Null);
            var events = motion.motionSet.motions.SelectMany(item => item.events).Concat(motion.motionSet.globalEvents).ToArray();
            Assert.That(events.OfType<MotionEvent_MotionWarp>().Any(warp => warp.bakedProfiles.Any(profile => profile.HasTrajectory && profile.localTotal.magnitude > .05f)), Is.True);
            var collisions = events.OfType<BeginCollisionEvent>().ToArray();
            Assert.That(collisions, Is.Not.Empty);
            foreach (var collision in collisions)
                Assert.That(collision.hitPhaseIndex, Is.InRange(0, payload.attackInfo.baseInfo.hitPhases.Count - 1));
        }

        [TestCase("Player")]
        [TestCase("Humanoid")]
        public void 카타나_피니시는_연출_시작_전에_회전_창을_닫는다(string owner)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MotionSetAsset>($"Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/{owner}/Katana/Katana_Finish.asset");
            var warp = asset.motionSet.globalEvents.OfType<MotionEvent_MotionWarp>().Single();
            Assert.That(warp.startTime, Is.Zero);
            Assert.That(warp.endTime, Is.LessThanOrEqualTo(.5f));
            Assert.That(warp.translationWeight, Is.Zero);
            Assert.That(warp.rotationWeight, Is.GreaterThan(0f));
        }

        [Test]
        public void 빈_대상에서도_수동_조준은_작은_어시스트_각도에_갇히지_않는다()
        {
            var owner = new GameObject("입력 방향 검증");
            try
            {
                var warp = owner.AddComponent<MotionWarpController>();
                warp.BeginTargetLock(Vector3.right);
                warp.SetTarget((Transform)null);
                var settings = MotionWarpWindowSettings.Default(.25f);
                settings.maxWarpAngle = 10f;
                settings.translationWeight = 0f;
                warp.BeginWarpWindow(settings);
                Assert.That(warp.EvaluateVelocity(Vector3.zero, Vector3.zero, true, .25f, .25f,
                    0f, 2.5f, 18f, .02f), Is.EqualTo(Vector3.zero));
                Assert.That(warp.TryEvaluateRotation(Quaternion.identity, Vector3.zero, true,
                    0f, .25f, 0f, 2.5f, 18f, out var rotation), Is.True);
                Assert.That(Vector3.Angle(rotation * Vector3.forward, Vector3.right), Is.LessThan(.01f));
                warp.CommitTargetTracking();
                Assert.That(warp.TryEvaluateRotation(rotation, Vector3.zero, true,
                    0f, .25f, 0f, 2.5f, 18f, out _), Is.False);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [TestCase(1f, true)]
        [TestCase(3f, false)]
        public void 호출_상태의_레거시_거리로_저장된_회전_범위를_늘리지_않는다(float distance, bool expected)
        {
            var owner = new GameObject("저장 범위 검증");
            var target = new GameObject("범위 표적");
            try
            {
                target.transform.position = Vector3.right * distance;
                var warp = owner.AddComponent<MotionWarpController>();
                warp.BeginTargetLock();
                warp.SetTarget(target.transform);
                var settings = MotionWarpWindowSettings.Default(.25f);
                settings.overrideDistance = false;
                settings.maxDistance = 2.5f;
                settings.translationWeight = 0f;
                warp.BeginWarpWindow(settings);
                Assert.That(warp.TryEvaluateRotation(Quaternion.identity, Vector3.zero, true,
                    0f, .25f, 0f, 100f, 100f, out _), Is.EqualTo(expected));
            }
            finally { Object.DestroyImmediate(target); Object.DestroyImmediate(owner); }
        }

        private static MotionWarpWindowSettings Settings() => new()
        {
            minimumSourceDisplacement = 0.05f, minimumNetToPathRatio = 0.5f,
            maxCorrectionDistance = 0.2f, maxCorrectionRatio = 0.2f, maxWarpAngle = 30f
        };

        [TestCase(0.08f, 0.15f, 1f, 0.08f)]
        [TestCase(0.25f, 0.15f, 1f, 0.15f)]
        [TestCase(0.08f, 0.15f, 0f, 0f)]
        [TestCase(0.08f, 0.65f, 6f, 0.48f)]
        [TestCase(0.25f, 0.65f, 6f, 0.55f)]
        public void 일부_접근은_원본_비율과_절대_거리_중_작은_상한을_지킨다(
            float sourceDistance, float distanceLimit, float ratio, float expected)
        {
            var settings = Settings();
            settings.correctionLimitPolicy = MotionWarpCorrectionLimitPolicy.Clamp;
            settings.maxCorrectionDistance = distanceLimit;
            settings.maxCorrectionRatio = ratio;
            Assert.That(MotionWarpMath.TryResolveEndpointCorrection(Vector3.forward * sourceDistance,
                sourceDistance, Vector3.forward * 0.8f, settings, out var correction), Is.True);
            Assert.That(correction.z, Is.EqualTo(expected).Within(0.0001f));
            Assert.That(sourceDistance + correction.z, Is.LessThanOrEqualTo(0.8001f));
        }

        [TestCase(0f, 0f)]
        [TestCase(0.01f, 0.7f)]
        [TestCase(0.1f, 1f)]
        public void 일부_접근도_제자리와_왕복_루트에는_전진을_만들지_않는다(float net, float path)
        {
            var settings = Settings();
            settings.correctionLimitPolicy = MotionWarpCorrectionLimitPolicy.Clamp;
            Assert.That(MotionWarpMath.TryResolveEndpointCorrection(Vector3.forward * net, path,
                Vector3.forward, settings, out _), Is.False);
        }

        [Test]
        public void 일부_접근은_각도_제한을_우회하거나_가까운_목표를_넘어서지_않는다()
        {
            var settings = Settings();
            settings.correctionLimitPolicy = MotionWarpCorrectionLimitPolicy.Clamp;
            Assert.That(MotionWarpMath.TryResolveEndpointCorrection(Vector3.forward, 1f,
                Vector3.right, settings, out _), Is.False);
            Assert.That(MotionWarpMath.TryResolveEndpointCorrection(Vector3.forward, 1f,
                Vector3.forward * 1.05f, settings, out var correction), Is.True);
            Assert.That(correction.z, Is.EqualTo(0.05f).Within(0.0001f));
        }

        /// <summary>
        /// 클립 교체·MotionSet 복제 뒤 재베이크를 빠뜨리면 에디터 조회는 통과해도 런타임은 프로필을 버려 접근이 0이 된다.
        /// 실제 플레이어 모델의 Animator와 매핑을 따라 런타임과 같은 기준으로 확인한다.
        /// </summary>
        [Test]
        public void 플레이어_모델이_쓰는_이동_워프_구간은_모두_런타임_프로필을_해석한다()
        {
            var failures = new List<string>();
            int windowCount = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/03.Prefabs/Actor/Player/Models" }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                var playerAnimator = prefab.GetComponentInChildren<PlayerActorAnimator>(true);
                if (playerAnimator?.PlayerMotionSet == null)
                    continue;
                Animator animator = playerAnimator.GetComponent<Animator>()
                                    ?? playerAnimator.GetComponentInParent<Animator>(true)
                                    ?? playerAnimator.GetComponentInChildren<Animator>(true);
                if (animator == null)
                    continue;

                foreach (MotionSetAsset asset in EnumerateMappedMotionSets(playerAnimator.PlayerMotionSet))
                foreach (MotionEvent_MotionWarp warp in (asset.motionSet.globalEvents ?? new List<MotionEventBase>())
                             .Concat((asset.motionSet.motions ?? new List<UPlayGround.Animation.Motion>())
                                 .Where(motion => motion?.events != null)
                                 .SelectMany(motion => motion.events))
                             .OfType<MotionEvent_MotionWarp>())
                {
                    MotionWarpWindowSettings settings = warp.ResolveSettings();
                    bool movesActor = warp.endTime > warp.startTime && settings.translationWeight > 0f
                        && (settings.modifierType == MotionWarpModifierType.EndpointCorrection
                            || settings.modifierType == MotionWarpModifierType.DeltaWarp);
                    if (!movesActor)
                        continue;
                    windowCount++;
                    if (!warp.TryGetRuntimeProfile(animator.avatar, animator.transform.lossyScale, asset.motionSet, out _))
                        failures.Add($"{prefab.name} | {AssetDatabase.GetAssetPath(asset)} [{warp.startTime:F3}~{warp.endTime:F3}]");
                }
            }

            Assert.That(windowCount, Is.GreaterThan(0));
            Assert.That(failures, Is.Empty,
                "런타임이 거절하는 워프 프로필 — 루트모션 일괄 베이크의 [누락 프로필 준비]로 다시 측정하세요.\n"
                + string.Join("\n", failures.Distinct()));
        }

        /// <summary>무기별 세트·폴백 세트를 거쳐 플레이어 매핑이 참조하는 모든 MotionSet을 의존성으로 모은다.</summary>
        private static IEnumerable<MotionSetAsset> EnumerateMappedMotionSets(PlayerActorAnimationMotionSet playerSet)
        {
            foreach (string path in AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(playerSet), true))
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(MotionSetAsset))
                    continue;
                var asset = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(path);
                if (asset?.motionSet != null)
                    yield return asset;
            }
        }

        private static MotionWarpRootMotionBakeProfile StraightProfile(float distance)
        {
            int count = MotionEvent_MotionWarp.TrajectorySampleCount;
            var profile = new MotionWarpRootMotionBakeProfile
            {
                formatVersion = MotionEvent_MotionWarp.CurrentBakeFormatVersion,
                localTotal = Vector3.forward * distance,
                pathLen = distance,
                cumulativeLocalPositions = new Vector3[count],
                cumulativePathLengths = new float[count],
                cumulativeYaw = new float[count],
            };
            for (int index = 0; index < count; index++)
            {
                float progress = index / (float)(count - 1);
                profile.cumulativeLocalPositions[index] = Vector3.forward * (distance * progress);
                profile.cumulativePathLengths[index] = distance * progress;
            }
            return profile;
        }

        [Test]
        public void 회전_워프가_돌릴_방향까지_반영하면_측면_대상의_이동_보정을_거절하지_않는다()
        {
            var profile = StraightProfile(0.1f);
            var settings = Settings();
            settings.correctionLimitPolicy = MotionWarpCorrectionLimitPolicy.Clamp;
            settings.correctionTiming = MotionWarpCorrectionTiming.AuthoredCurve;
            settings.maxCorrectionDistance = 1.5f;
            Vector3 desired = Quaternion.AngleAxis(50f, Vector3.up) * Vector3.forward * 1.2f;

            Vector3 unrotated = MotionWarpTrajectory.PredictRotatedDisplacement(
                profile, 1f, Quaternion.identity, Vector3.up, 50f, null, 0f);
            Assert.That(MotionWarpMath.TryResolveEndpointCorrection(unrotated, profile.pathLen, desired, settings, out _),
                Is.False, "회전하지 않는 예측은 회전으로 맞출 50도를 이동 보정이 떠안아 각도 상한에 걸린다.");

            Vector3 rotated = MotionWarpTrajectory.PredictRotatedDisplacement(
                profile, 1f, Quaternion.identity, Vector3.up, 50f, null, 1f);
            Assert.That(Vector3.Angle(Vector3.forward, rotated), Is.InRange(20f, 50f));
            Assert.That(MotionWarpMath.TryResolveEndpointCorrection(rotated, profile.pathLen, desired, settings,
                out Vector3 correction), Is.True);
            Assert.That((rotated + correction - desired).magnitude, Is.LessThan(0.001f));
        }

        [Test]
        public void 회전_예측은_시작_자세_기준으로_원본_경로를_돌린다()
        {
            var profile = StraightProfile(0.2f);
            Quaternion basis = Quaternion.AngleAxis(90f, Vector3.up);
            Vector3 straight = MotionWarpTrajectory.PredictRotatedDisplacement(
                profile, 1f, basis, Vector3.up, 0f, null, 1f);
            Assert.That((straight - Vector3.right * 0.2f).magnitude, Is.LessThan(0.0001f));
            Vector3 half = MotionWarpTrajectory.PredictRotatedDisplacement(
                profile, 0.5f, basis, Vector3.up, 0f, null, 1f);
            Assert.That((half - Vector3.right * 0.1f).magnitude, Is.LessThan(0.0001f));
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        [TestCase(7)]
        public void 저작_곡선_보정은_프레임률과_무관하게_총량을_한번만_소비한다(int rate)
        {
            // 곡선 끝값이 1에 못 미쳐도 구간 끝에서는 전량을 소비해야 한다.
            var curve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 2.2f), new Keyframe(0.6f, 0.92f),
                new Keyframe(1f, 0.97f, 0f, 0f));
            Vector3 total = new(0.3f, 0f, 1.2f);
            float consumed = 0f;
            Vector3 sum = Vector3.zero;
            for (int frame = 1; frame <= rate + 2; frame++)
            {
                float target = MotionWarpMath.EvaluateCorrectionTimingFraction(curve, frame / (float)rate);
                sum += MotionWarpMath.ConsumeAuthoredCorrection(total, target, -1f, ref consumed, out bool limited);
                Assert.That(limited, Is.False);
            }
            Assert.That((sum - total).magnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void 저작_곡선은_초반에_대부분을_소비한다()
        {
            var curve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 2.2f), new Keyframe(0.6f, 0.92f),
                new Keyframe(1f, 1f, 0f, 0f));
            Assert.That(MotionWarpMath.EvaluateCorrectionTimingFraction(curve, 0.3f), Is.GreaterThan(0.5f));
            Assert.That(MotionWarpMath.EvaluateCorrectionTimingFraction(null, 0.3f), Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(MotionWarpMath.EvaluateCorrectionTimingFraction(curve, 1.4f), Is.EqualTo(1f));
        }

        [Test]
        public void 저작_곡선_보정은_속도_상한에_걸린_몫을_뒤_스텝에서_이어쓰되_총량을_넘지_않는다()
        {
            Vector3 total = Vector3.forward;
            float consumed = 0f;
            Vector3 first = MotionWarpMath.ConsumeAuthoredCorrection(total, 0.8f, 0.3f, ref consumed, out bool limited);
            Assert.That(limited, Is.True);
            Assert.That(first.magnitude, Is.EqualTo(0.3f).Within(0.0001f));
            Vector3 second = MotionWarpMath.ConsumeAuthoredCorrection(total, 0.8f, 0.3f, ref consumed, out _);
            Vector3 third = MotionWarpMath.ConsumeAuthoredCorrection(total, 1f, 0.3f, ref consumed, out _);
            Vector3 fourth = MotionWarpMath.ConsumeAuthoredCorrection(total, 1f, 0.3f, ref consumed, out _);
            Assert.That((first + second + third + fourth).magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(MotionWarpMath.ConsumeAuthoredCorrection(total, 1f, 0.3f, ref consumed, out _),
                Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void 저작_곡선은_원본_비율_대신_절대_거리_상한만_적용한다()
        {
            var settings = Settings();
            settings.correctionLimitPolicy = MotionWarpCorrectionLimitPolicy.Clamp;
            settings.maxCorrectionDistance = 1.5f;
            Vector3 source = Vector3.forward * 0.1f;
            Vector3 desired = Vector3.forward * 1.3f;
            Assert.That(MotionWarpMath.TryResolveEndpointCorrection(source, 0.1f, desired, settings,
                out Vector3 rootPath), Is.True);
            Assert.That(rootPath.magnitude, Is.EqualTo(0.02f).Within(0.0001f));

            settings.correctionTiming = MotionWarpCorrectionTiming.AuthoredCurve;
            Assert.That(MotionWarpMath.TryResolveEndpointCorrection(source, 0.1f, desired, settings,
                out Vector3 authored), Is.True);
            Assert.That(authored.magnitude, Is.EqualTo(1.2f).Within(0.0001f));

            settings.maxCorrectionDistance = 0.5f;
            Assert.That(MotionWarpMath.TryResolveEndpointCorrection(source, 0.1f, desired, settings,
                out authored), Is.True);
            Assert.That(authored.magnitude, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [TestCase(0f, 0f)]
        [TestCase(0.01f, 0.7f)]
        [TestCase(0.1f, 1f)]
        public void 저작_곡선도_제자리와_왕복_루트에는_전진을_만들지_않는다(float net, float path)
        {
            var settings = Settings();
            settings.correctionLimitPolicy = MotionWarpCorrectionLimitPolicy.Clamp;
            settings.correctionTiming = MotionWarpCorrectionTiming.AuthoredCurve;
            settings.maxCorrectionDistance = 2f;
            Assert.That(MotionWarpMath.TryResolveEndpointCorrection(Vector3.forward * net, path,
                Vector3.forward, settings, out _), Is.False);
        }

        /// <summary>
        /// 라온 기본 3연격은 원본 전진이 10~26cm뿐이라 원본 비례 보정으로는 접근이 느껴지지 않는다.
        /// 거리 좁히기를 저작 곡선으로 명시하고, 타격 전에 끝나며, 가까운 대상은 넘어가지 않는지 확인한다.
        /// </summary>
        [TestCase("Katana.Light.00")]
        [TestCase("Katana.Light.01")]
        [TestCase("Katana.Light.02")]
        public void 라온_기본타는_타격_전에_대상까지_거리를_좁히되_넘어가지_않는다(string key)
        {
            const string root = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/";
            var mapping = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(root + "Bokusei_KatanaAnimationSet.asset");
            var raon = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab");
            Animator animator = raon.GetComponentInChildren<Animator>(true);
            var asset = mapping.GetAbilityMotionAsset(new MotionKey(key));
            var motion = asset.motionSet.motions.Single();
            var warp = motion.events.OfType<MotionEvent_MotionWarp>().Single();
            var settings = warp.ResolveSettings();
            Assert.That(settings.translationWeight, Is.EqualTo(1f));
            Assert.That(settings.correctionLimitPolicy, Is.EqualTo(MotionWarpCorrectionLimitPolicy.Clamp));
            Assert.That(settings.correctionTiming, Is.EqualTo(MotionWarpCorrectionTiming.AuthoredCurve));
            Assert.That(settings.maxCorrectionDistance, Is.InRange(1f, 1.6f));

            float translationSpan = settings.duration - settings.translationEndLeadTime;
            Assert.That(warp.endTime - settings.translationEndLeadTime,
                Is.LessThan(motion.events.OfType<BeginCollisionEvent>().Min(hit => hit.startTime)),
                "이동은 첫 판정 전에 끝나야 한다.");
            Assert.That(settings.maxSpeed * translationSpan, Is.GreaterThan(settings.maxCorrectionDistance),
                "속도 상한 때문에 구간 안에 거리 예산을 다 쓰지 못하면 안 된다.");
            Assert.That(MotionWarpMath.EvaluateCorrectionTimingFraction(settings.correctionTimingCurve, 0.5f),
                Is.GreaterThanOrEqualTo(0.7f), "예비 동작 초반에 대부분 들어가야 타격 순간에 발이 멈춘다.");
            for (int sample = 1; sample <= 20; sample++)
                Assert.That(MotionWarpMath.EvaluateCorrectionTimingFraction(settings.correctionTimingCurve, sample / 20f),
                    Is.GreaterThanOrEqualTo(MotionWarpMath.EvaluateCorrectionTimingFraction(
                        settings.correctionTimingCurve, (sample - 1) / 20f) - 0.0001f));

            Assert.That(warp.TryGetRuntimeProfile(animator.avatar, animator.transform.lossyScale, asset.motionSet,
                out MotionWarpRootMotionBakeProfile profile), Is.True);
            MotionWarpTrajectory.SampleRawCumulative(profile, 1f - settings.translationEndLeadTime / settings.duration,
                out Vector3 source, out float path, out _);
            Assert.That(MotionWarpMath.TryResolveEndpointCorrection(source, path,
                source + source.normalized * 0.5f, settings, out Vector3 near), Is.True);
            Assert.That(near.magnitude, Is.EqualTo(0.5f).Within(0.0001f), "가까운 대상은 정확히 접촉 거리까지만 간다.");
            Assert.That(MotionWarpMath.TryResolveEndpointCorrection(source, path,
                source + source.normalized * 4f, settings, out Vector3 far), Is.True);
            Assert.That(far.magnitude, Is.EqualTo(settings.maxCorrectionDistance).Within(0.0001f),
                "먼 대상은 예산만큼만 다가가고 도착으로 바꾸지 않는다.");
        }

        [Test]
        public void 대시_공격은_접근_예산이_부족해도_제한된_거리를_좁힌다()
        {
            const string root = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/";
            var mapping = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(root + "Bokusei_KatanaAnimationSet.asset");
            var asset = mapping.GetAbilityMotionAsset(new MotionKey("Katana.Dash.00"));
            var motion = asset.motionSet.motions.Single();
            var warp = motion.events.OfType<MotionEvent_MotionWarp>().Single();
            var settings = warp.ResolveSettings();
            Assert.That(settings.correctionLimitPolicy, Is.EqualTo(MotionWarpCorrectionLimitPolicy.Clamp));
            Assert.That(settings.maxCorrectionDistance, Is.InRange(0.7f, 1f));
            Assert.That(warp.endTime - settings.translationEndLeadTime,
                Is.LessThan(motion.events.OfType<BeginCollisionEvent>().Min(hit => hit.startTime)));
            foreach (var profile in warp.bakedProfiles)
            {
                MotionWarpTrajectory.SampleRawCumulative(profile,
                    1f - settings.translationEndLeadTime / settings.duration, out var source, out float path, out _);
                Assert.That(MotionWarpMath.TryResolveEndpointCorrection(source, path,
                    source + source.normalized * 2f, settings, out var correction), Is.True);
                Assert.That(correction.magnitude, Is.InRange(0.5f, 1.0001f));
            }
        }
    }
}
