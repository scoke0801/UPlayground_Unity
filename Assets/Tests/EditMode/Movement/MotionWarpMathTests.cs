using NUnit.Framework;
using UnityEngine;
using UPlayGround.MovementController;

namespace UPlayGround.Movement.Tests
{
    public class MotionWarpMathTests
    {
        [Test]
        public void 접촉면_도착점은_양쪽_반경과_간격을_보존한다()
        {
            Vector3 destination = MotionWarpMath.ResolveContactShellDestination(
                attackerRoot: Vector3.zero,
                attackerCenter: new Vector3(0f, 1f, 0f),
                attackerRadius: 0.4f,
                targetCenter: new Vector3(0f, 2f, 4f),
                targetRotation: Quaternion.identity,
                targetRadius: 0.8f,
                desiredStandOff: 0.3f,
                localArrivalOffset: Vector3.zero);

            Assert.That(destination.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(destination.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(destination.z, Is.EqualTo(2.5f).Within(0.0001f));
        }

        [Test]
        public void 대형_타겟일수록_더_바깥의_접촉면에_도착한다()
        {
            Vector3 smallTarget = MotionWarpMath.ResolveContactShellDestination(
                Vector3.zero, Vector3.up, 0.4f,
                new Vector3(0f, 1f, 5f), Quaternion.identity,
                0.5f, 0.2f, Vector3.zero);
            Vector3 largeTarget = MotionWarpMath.ResolveContactShellDestination(
                Vector3.zero, Vector3.up, 0.4f,
                new Vector3(0f, 1f, 5f), Quaternion.identity,
                1.5f, 0.2f, Vector3.zero);

            Assert.That(largeTarget.z, Is.LessThan(smallTarget.z));
            Assert.That(smallTarget.z - largeTarget.z, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void 공격_리치_안에서는_위치_보정을_생략한다()
        {
            bool inside = MotionWarpMath.IsInsideContactDeadZone(
                centerDistance: 1.45f,
                attackerRadius: 0.4f,
                targetRadius: 0.7f,
                desiredStandOff: 0.2f,
                deadZone: 0.2f);
            bool outside = MotionWarpMath.IsInsideContactDeadZone(
                centerDistance: 1.51f,
                attackerRadius: 0.4f,
                targetRadius: 0.7f,
                desiredStandOff: 0.2f,
                deadZone: 0.2f);

            Assert.That(inside, Is.True);
            Assert.That(outside, Is.False);
        }

        [Test]
        public void 위치_보정은_절대거리와_루트모션_비율중_작은_예산을_따른다()
        {
            Vector3 absoluteLimited = MotionWarpMath.LimitCorrection(
                new Vector3(2f, 3f, 0f),
                remainingOriginalDistance: 4f,
                maxCorrectionDistance: 0.5f,
                maxCorrectionRatio: 0.5f);
            Vector3 ratioLimited = MotionWarpMath.LimitCorrection(
                new Vector3(2f, 3f, 0f),
                remainingOriginalDistance: 1f,
                maxCorrectionDistance: 2f,
                maxCorrectionRatio: 0.25f);

            Assert.That(absoluteLimited.magnitude, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(absoluteLimited.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(ratioLimited.magnitude, Is.EqualTo(0.25f).Within(0.0001f));
        }

        [Test]
        public void 허용각도_밖의_타겟은_워프하지_않는다()
        {
            Assert.That(MotionWarpMath.IsInsideWarpAngle(
                Vector3.forward,
                Quaternion.Euler(0f, 30f, 0f) * Vector3.forward,
                45f), Is.True);
            Assert.That(MotionWarpMath.IsInsideWarpAngle(
                Vector3.forward,
                Quaternion.Euler(0f, 60f, 0f) * Vector3.forward,
                45f), Is.False);
        }

        [Test]
        public void 타격_직전_리드타임에는_이동_가중치가_영이_된다()
        {
            AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

            Assert.That(MotionWarpMath.EvaluateTranslationWeight(
                curve, 0.75f, 0.12f, 0.08f),
                Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(MotionWarpMath.EvaluateTranslationWeight(
                curve, 0.95f, 0.05f, 0.08f),
                Is.EqualTo(0f));
        }

        [Test]
        public void 재생속도_정책은_레거시_호환과_명시적_설정을_구분한다()
        {
            Assert.That(MotionWarpMath.ResolvePlaybackRateWarp(
                PlaybackRateWarpPolicy.LegacyTargetCenter,
                WarpArrivalMode.TargetCenter,
                legacyAuthoredValue: false), Is.True);
            Assert.That(MotionWarpMath.ResolvePlaybackRateWarp(
                PlaybackRateWarpPolicy.Disabled,
                WarpArrivalMode.TargetCenter,
                legacyAuthoredValue: true), Is.False);
            Assert.That(MotionWarpMath.ResolvePlaybackRateWarp(
                PlaybackRateWarpPolicy.Enabled,
                WarpArrivalMode.ContactShell,
                legacyAuthoredValue: false), Is.True);
        }
    }
}
