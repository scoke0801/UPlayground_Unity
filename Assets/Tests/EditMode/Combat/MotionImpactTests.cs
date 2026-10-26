using NUnit.Framework;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Combat;

namespace UPlayGround.Combat.Tests
{
    /// <summary>실측 운동 데이터의 페이즈 선택과 피해·방향·피드백 경계를 검증한다.</summary>
    public sealed class MotionImpactTests
    {
        private MotionImpactCatalogSO _catalog;
        private MotionImpactResponseSO _profile;
        private readonly MotionKey _key = new("Test.Swing");

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<MotionImpactCatalogSO>();
            _profile = ScriptableObject.CreateInstance<MotionImpactResponseSO>();
            _catalog.windows.Add(new MotionImpactWindow
            {
                motionKey = _key, hitPhaseIndex = 0, start = 0.2f, end = 0.4f, meanSpeed = 8f,
                samples = new()
                {
                    new MotionImpactSample { time = 0.2f, localVelocity = Vector3.right * 8f },
                    new MotionImpactSample { time = 0.4f, localVelocity = Vector3.left * 8f },
                },
            });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_catalog);
            Object.DestroyImmediate(_profile);
        }

        [Test]
        public void SlowerSwing_HasHigherDamageAndLongerHitStop()
        {
            _profile.Evaluate(4f, out float slowDamage, out float slowStop);
            _profile.Evaluate(24f, out float fastDamage, out float fastStop);
            Assert.That(slowDamage, Is.GreaterThan(fastDamage));
            Assert.That(slowStop, Is.GreaterThan(fastStop));
        }

        [Test]
        public void ContactTime_ChangesDirectionButKeepsPhaseDamageStable()
        {
            var first = Evaluate(0.2f);
            var last = Evaluate(0.4f);
            Assert.That(first.WorldDirection.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(last.WorldDirection.x, Is.EqualTo(-1f).Within(0.001f));
            Assert.That(first.DamageMultiplier, Is.EqualTo(last.DamageMultiplier));
            Assert.That(first.HitStopDuration, Is.EqualTo(last.HitStopDuration));
        }

        [Test]
        public void ActorRotation_RotatesBakedSwingDirection()
        {
            var rotation = Quaternion.Euler(0f, 90f, 0f);
            var result = MotionImpactResolver.Evaluate(_catalog, _profile, _key, 0, 0.2f, rotation);
            Assert.That(Vector3.Distance(result.WorldDirection, rotation * Vector3.right), Is.LessThan(0.001f));
        }

        [Test]
        public void PosePastOpenWindow_KeepsDamageAndUsesBoundaryDirection()
        {
            var result = MotionImpactResolver.Evaluate(_catalog, _profile, _key, 0, 0.5f,
                Quaternion.identity, collisionTime: 0.35f);
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.DamageMultiplier, Is.EqualTo(Evaluate(0.35f).DamageMultiplier));
            Assert.That(result.WorldDirection, Is.EqualTo(Vector3.left));
        }

        [TestCase(0.1f, 0)]
        [TestCase(0.45f, 0)]
        [TestCase(0.35f, 1)]
        public void PosePastWindow_WithoutMatchingActivePhaseDoesNotApply(float collisionTime, int phase)
        {
            Assert.That(MotionImpactResolver.Evaluate(_catalog, _profile, _key, phase, 0.5f,
                Quaternion.identity, collisionTime).HasValue, Is.False);
        }

        [Test]
        public void PoseInLaterWindow_UsesStillOpenWindowOfSamePhase()
        {
            _catalog.windows.Add(new MotionImpactWindow
            {
                motionKey = _key, hitPhaseIndex = 0, start = 0.6f, end = 0.8f, meanSpeed = 24f,
                samples = new()
                {
                    new MotionImpactSample { time = 0.6f, localVelocity = Vector3.forward * 24f },
                    new MotionImpactSample { time = 0.8f, localVelocity = Vector3.forward * 24f },
                },
            });

            var result = MotionImpactResolver.Evaluate(_catalog, _profile, _key, 0, 0.7f,
                Quaternion.identity, collisionTime: 0.35f);
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.MeanSpeed, Is.EqualTo(8f));
            Assert.That(result.DamageMultiplier, Is.EqualTo(Evaluate(0.35f).DamageMultiplier));
            Assert.That(result.WorldDirection, Is.EqualTo(Vector3.left));

            var reopened = MotionImpactResolver.Evaluate(_catalog, _profile, _key, 0, 0.7f,
                Quaternion.identity, collisionTime: 0.65f);
            Assert.That(reopened.MeanSpeed, Is.EqualTo(24f));
            Assert.That(reopened.WorldDirection, Is.EqualTo(Vector3.forward));
        }

        [TestCase(0.1f)]
        [TestCase(0.45f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void PoseInsideWindow_WithoutValidEventTimeDoesNotApply(float collisionTime)
        {
            Assert.That(MotionImpactResolver.Evaluate(_catalog, _profile, _key, 0, 0.3f,
                Quaternion.identity, collisionTime).HasValue, Is.False);
        }

        [Test]
        public void RewoundPose_DoesNotReusePreviousEventTime()
        {
            Assert.That(MotionImpactResolver.Evaluate(_catalog, _profile, _key, 0, 0.25f,
                Quaternion.identity, collisionTime: 0.35f).HasValue, Is.False);
        }

        [Test]
        public void DifferentMotionKey_DoesNotReuseOpenWindow()
        {
            Assert.That(MotionImpactResolver.Evaluate(_catalog, _profile, new MotionKey("Test.Other"),
                0, 0.5f, Quaternion.identity, collisionTime: 0.35f).HasValue, Is.False);
        }

        [TestCase(0.19f, 0)]
        [TestCase(0.41f, 0)]
        [TestCase(0.3f, 1)]
        public void MissingWindowOrPhase_PreservesExistingBehavior(float time, int phase)
        {
            Assert.That(MotionImpactResolver.Evaluate(_catalog, _profile, _key, phase, time,
                Quaternion.identity).HasValue, Is.False);
        }

        [Test]
        public void RepeatedRequests_DoNotCompoundDamageOrMutateSource()
        {
            var attack = new AttackData { damage = 100f, motionImpact = Evaluate(0.2f) };
            var first = HitRequest.FromAttackData(attack);
            var second = HitRequest.FromAttackData(attack);
            Assert.That(first.Damage, Is.EqualTo(100f * attack.motionImpact.DamageMultiplier));
            Assert.That(second.Damage, Is.EqualTo(first.Damage));
            Assert.That(attack.damage, Is.EqualTo(100f));
        }

        [Test]
        public void Projectile_DoesNotInheritMeleeMotionDamageOrDirection()
        {
            var attack = new AttackData { damage = 100f, isProjectile = true,
                attackDirection = Vector3.forward, motionImpact = Evaluate(0.2f) };
            var request = HitRequest.FromAttackData(attack);
            Assert.That(request.Damage, Is.EqualTo(100f));
            Assert.That(request.AttackDirection, Is.EqualTo(Vector3.forward));
        }

        [Test]
        public void ManualHitStop_WinsOverMotionResponse()
        {
            var phase = new HitPhaseData();
            phase.reactionProfile.useManualOverride = true;
            phase.reactionProfile.manualOverride.overrideHitStop = true;
            phase.reactionProfile.manualOverride.hitStopDuration = 0.02f;
            var attack = new AttackData();
            PlayerAttackController.ApplyHitPhase(attack, phase, 0);
            attack.motionImpact = Evaluate(0.2f);
            Assert.That(attack.ResolveReactionData().hitStopDuration, Is.EqualTo(0.02f));
        }

        [Test]
        public void PhaseChange_ClearsPreviousMotionResult()
        {
            var attack = new AttackData { motionImpact = Evaluate(0.2f) };
            PlayerAttackController.ApplyHitPhase(attack, new HitPhaseData(), 1);
            Assert.That(attack.HasMotionImpact, Is.False);
        }

        [Test]
        public void ZeroSwingVelocity_KeepsDetectionDirection()
        {
            var result = Evaluate(0.3f);
            Assert.That(result.HasDirection, Is.False);
            var attack = new AttackData { attackDirection = Vector3.forward, motionImpact = result };
            Assert.That(attack.ResolvedAttackDirection, Is.EqualTo(Vector3.forward));
        }

        [Test]
        public void VerticalSwing_KeepsHorizontalKnockbackContract()
        {
            var victim = new GameObject("피격 대상");
            try
            {
                var direction = KnockbackDirectionResolver.ResolveHorizontal(Vector3.up, null, victim.transform, Vector3.up);
                Assert.That(direction.y, Is.EqualTo(0f).Within(0.001f));
                Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.001f));
            }
            finally { Object.DestroyImmediate(victim); }
        }

        private MotionImpactResult Evaluate(float time) =>
            MotionImpactResolver.Evaluate(_catalog, _profile, _key, 0, time, Quaternion.identity);
    }
}
