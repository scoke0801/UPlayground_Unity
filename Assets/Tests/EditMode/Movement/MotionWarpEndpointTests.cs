using NUnit.Framework;
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

        [TestCase("Katana.Light.00")]
        [TestCase("Katana.Light.01")]
        public void 보쿠세이_기본타는_타격_전에_유효한_원본으로_제한된_접근을_한다(string key)
        {
            const string root = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/";
            var mapping = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(root + "Bokusei_KatanaAnimationSet.asset");
            var asset = mapping.GetAbilityMotionAsset(new MotionKey(key));
            var motion = asset.motionSet.motions.Single();
            var warp = motion.events.OfType<MotionEvent_MotionWarp>().Single();
            var settings = warp.ResolveSettings();
            Assert.That(settings.translationWeight, Is.GreaterThan(0f));
            Assert.That(settings.correctionLimitPolicy, Is.EqualTo(MotionWarpCorrectionLimitPolicy.Clamp));
            Assert.That(settings.maxCorrectionDistance, Is.InRange(0.5f, 0.65f));
            Assert.That(settings.maxCorrectionRatio, Is.InRange(1f, 7f));
            Assert.That(warp.endTime - settings.translationEndLeadTime,
                Is.LessThan(motion.events.OfType<BeginCollisionEvent>().Min(hit => hit.startTime)));
            Assert.That(warp.bakedProfiles, Is.Not.Empty);
            foreach (var profile in warp.bakedProfiles)
            {
                Assert.That(warp.TryGetBakedProfile(profile.avatar, profile.animatorScale, out _), Is.True);
                Assert.That(MotionWarpTrajectory.MatchesSource(profile, asset.motionSet,
                    MotionWarpTrajectory.BuildSourceSignature(asset.motionSet)), Is.True);
                float cutoff = 1f - settings.translationEndLeadTime / settings.duration;
                MotionWarpTrajectory.SampleRawCumulative(profile, cutoff, out var source, out float path, out _);
                Assert.That(MotionWarpMath.TryResolveEndpointCorrection(source, path,
                    source + source.normalized * 0.5f, settings, out var correction), Is.True);
                Assert.That(correction.magnitude, Is.InRange(0.35f, 0.5001f),
                    "근거리에서 부족한 반걸음을 메우되 목표를 넘어가지 않아야 한다.");
            }
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
