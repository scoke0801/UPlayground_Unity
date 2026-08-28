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

        [Test]
        public void 왕복_루트모션은_총방향이_아니라_실제_잔여벡터를_사용한다()
        {
            Vector3 remaining = MotionWarpMath.ResolveRemainingRootMotion(
                totalLocal: new Vector3(0f, 0f, 0.1f),
                accumulatedLocalIncludingCurrentFrame:
                    new Vector3(0f, 0f, 0.7f),
                currentFrameLocal: new Vector3(0f, 0f, -0.1f),
                actorRotation: Quaternion.identity);

            Assert.That(remaining.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(remaining.z, Is.EqualTo(-0.7f).Within(0.0001f));
        }

        [Test]
        public void 잔여경로는_현재프레임_직전_누적량을_기준으로_계산한다()
        {
            float remaining = MotionWarpMath.ResolveRemainingRootPath(
                totalPath: 1.2f,
                accumulatedPathIncludingCurrentFrame: 0.7f,
                currentFramePath: 0.1f);

            Assert.That(remaining, Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void 베이크_프로필은_같은_Avatar와_스케일에만_일치한다()
        {
            var profile = new MotionWarpRootMotionBakeProfile
            {
                formatVersion = 2,
                animatorScale = Vector3.one,
                localTotal = Vector3.forward * 0.3f,
                pathLen = 0.3f,
            };

            Assert.That(profile.Matches(null, Vector3.one), Is.True);
            Assert.That(profile.Matches(null, Vector3.one * 1.1f), Is.False);
        }

        [Test]
        public void 경로가_없는_베이크_프로필은_유효하지_않다()
        {
            var profile = new MotionWarpRootMotionBakeProfile
            {
                formatVersion = 2,
                animatorScale = Vector3.one,
                pathLen = 0f,
            };

            Assert.That(profile.IsValid, Is.False);
        }

        [Test]
        public void PlayMode_기준값은_활성_베이크와_독립적으로_유효성을_가진다()
        {
            var profile = new MotionWarpRootMotionBakeProfile
            {
                formatVersion = 2,
                pathLen = 0.4f,
                playModeReferenceFormatVersion = 2,
                playModeReferencePathLen = 0.38f,
            };

            Assert.That(profile.IsValid, Is.True);
            Assert.That(profile.HasPlayModeReference, Is.True);
        }

        [Test]
        public void 애니메이션_윈도우_진행도는_시작과_끝에서_클램프된다()
        {
            Assert.That(MotionWarpMath.ResolveWindowProgress(0.1f, 0.2f, 0.6f), Is.EqualTo(0f));
            Assert.That(MotionWarpMath.ResolveWindowProgress(0.4f, 0.2f, 0.6f), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(MotionWarpMath.ResolveWindowProgress(0.8f, 0.2f, 0.6f), Is.EqualTo(1f));
        }

        [Test]
        public void 애니메이션이_멈추면_잔여_보정도_소비하지_않는다()
        {
            float share = MotionWarpMath.ResolveResidualCorrectionShare(
                previousProgress: 0.4f,
                currentProgress: 0.4f);

            Assert.That(share, Is.EqualTo(0f));
        }

        [Test]
        public void 마지막_스팬이_종료점을_넘으면_남은_보정을_전부_소비한다()
        {
            float share = MotionWarpMath.ResolveResidualCorrectionShare(
                previousProgress: 0.72f,
                currentProgress: 1.2f);

            Assert.That(share, Is.EqualTo(1f));
        }

        [Test]
        public void 누적_진행도_분배는_프레임_구성과_무관하게_보정을_완료한다()
        {
            float[] progress = { 0f, 0.12f, 0.37f, 0.81f, 1f };
            float remainingCorrection = 1f;

            for (int index = 1; index < progress.Length; index++)
            {
                float share = MotionWarpMath.ResolveResidualCorrectionShare(
                    progress[index - 1],
                    progress[index]);
                remainingCorrection *= 1f - share;
            }

            Assert.That(remainingCorrection, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void 워프_핸들은_컨트롤러와_시퀀스가_모두_같아야_일치한다()
        {
            var first = new MotionWarpHandle(10, 2);
            var same = new MotionWarpHandle(10, 2);
            var newer = new MotionWarpHandle(10, 3);

            Assert.That(first, Is.EqualTo(same));
            Assert.That(first, Is.Not.EqualTo(newer));
        }

        [Test]
        public void 늦은_완료_핸들은_새_워프_세션을_종료하지_못한다()
        {
            var gameObject = new GameObject("MotionWarpControllerTest");
            try
            {
                MotionWarpController controller =
                    gameObject.AddComponent<MotionWarpController>();
                MotionWarpWindowSettings settings =
                    MotionWarpWindowSettings.Default(0.4f);

                MotionWarpHandle first = controller.BeginWarpWindow(settings);
                MotionWarpHandle second = controller.BeginWarpWindow(settings);

                Assert.That(controller.RequestEndWarpWindow(first), Is.False);
                Assert.That(controller.IsPendingEnd, Is.False);
                Assert.That(controller.RequestEndWarpWindow(second), Is.True);
                Assert.That(controller.IsMotionWarping, Is.True);

                controller.CompleteDirectMotionStep(Vector3.zero);

                Assert.That(controller.IsMotionWarping, Is.False);
                Assert.That(controller.LastEndReason, Is.EqualTo(MotionWarpEndReason.Completed));

                controller.BeginWarpWindow(settings);
                controller.EndMotionWarpForStateExit();

                Assert.That(controller.IsMotionWarping, Is.False);
                Assert.That(controller.LastEndReason, Is.EqualTo(MotionWarpEndReason.StateExited));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void v3_프로필은_위치_경로_Yaw_샘플_수가_같아야_한다()
        {
            MotionWarpRootMotionBakeProfile profile = CreateCurvedTrajectory();

            Assert.That(profile.IsValid, Is.True);
            Assert.That(profile.HasTrajectory, Is.True);

            profile.cumulativeYaw = new float[3];
            Assert.That(profile.HasTrajectory, Is.False);
        }

        [Test]
        public void 곡선_trajectory는_현재_프레임과_잔여_벡터를_시간으로_조회한다()
        {
            MotionWarpRootMotionBakeProfile profile = CreateCurvedTrajectory();

            bool success = MotionWarpTrajectory.TryEvaluateFrame(
                profile,
                0.25f,
                0.5f,
                1f,
                false,
                null,
                0f,
                out MotionWarpTrajectoryFrame frame);

            Assert.That(success, Is.True);
            Assert.That(frame.FrameLocalDelta.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(frame.FrameLocalDelta.z, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(frame.RemainingLocalDelta.x, Is.EqualTo(-1f).Within(0.0001f));
            Assert.That(frame.RemainingLocalDelta.z, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(frame.RemainingPath, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(frame.FrameYaw, Is.EqualTo(45f).Within(0.0001f));
        }

        [Test]
        public void 증폭된_trajectory도_프레임_분할과_무관하게_같은_총량을_만든다()
        {
            MotionWarpRootMotionBakeProfile profile = CreateCurvedTrajectory();
            AnimationCurve gain = AnimationCurve.Linear(0f, 0.5f, 1f, 2f);

            MotionWarpTrajectory.TryEvaluateFrame(
                profile, 0f, 0.4f, 1f, true, gain, 100f,
                out MotionWarpTrajectoryFrame first);
            MotionWarpTrajectory.TryEvaluateFrame(
                profile, 0.4f, 1f, 1f, true, gain, 100f,
                out MotionWarpTrajectoryFrame second);
            MotionWarpTrajectory.TryEvaluateFrame(
                profile, 0f, 1f, 1f, true, gain, 100f,
                out MotionWarpTrajectoryFrame whole);

            Assert.That(
                Vector3.Distance(
                    first.FrameLocalDelta + second.FrameLocalDelta,
                    whole.FrameLocalDelta),
                Is.LessThan(0.0001f));
            Assert.That(
                first.PathAfter + (second.PathAfter - second.PathBefore),
                Is.EqualTo(whole.TotalPath).Within(0.0001f));
            Assert.That(second.PathAfter, Is.EqualTo(whole.TotalPath).Within(0.0001f));
        }

        [Test]
        public void amplifyMaxSpeed는_모든_trajectory_구간에_같은_기준으로_적용된다()
        {
            var profile = new MotionWarpRootMotionBakeProfile
            {
                formatVersion = 3,
                localTotal = new Vector3(0f, 0f, 4f),
                pathLen = 4f,
                cumulativeLocalPositions = new[]
                {
                    Vector3.zero,
                    new Vector3(0f, 0f, 1f),
                    new Vector3(0f, 0f, 2f),
                    new Vector3(0f, 0f, 3f),
                    new Vector3(0f, 0f, 4f),
                },
                cumulativePathLengths = new[] { 0f, 1f, 2f, 3f, 4f },
                cumulativeYaw = new[] { 0f, 0f, 0f, 0f, 0f },
            };

            MotionWarpTrajectory.TryEvaluateFrame(
                profile,
                0f,
                1f,
                1f,
                true,
                AnimationCurve.Linear(0f, 2f, 1f, 2f),
                2f,
                out MotionWarpTrajectoryFrame frame);

            Assert.That(frame.FrameLocalDelta.z, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(frame.TotalPath, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void 애니메이션_진행도가_멈추면_v3_frameBase도_0이다()
        {
            MotionWarpTrajectory.TryEvaluateFrame(
                CreateCurvedTrajectory(),
                0.4f,
                0.4f,
                1f,
                false,
                null,
                0f,
                out MotionWarpTrajectoryFrame frame);

            Assert.That(frame.FrameLocalDelta, Is.EqualTo(Vector3.zero));
            Assert.That(frame.SourceSpeed, Is.EqualTo(0f));
            Assert.That(frame.RemainingPath, Is.GreaterThan(0f));
        }

        private static MotionWarpRootMotionBakeProfile CreateCurvedTrajectory()
        {
            return new MotionWarpRootMotionBakeProfile
            {
                formatVersion = 3,
                localTotal = Vector3.zero,
                pathLen = 4f,
                cumulativeLocalPositions = new[]
                {
                    Vector3.zero,
                    new Vector3(1f, 0f, 0f),
                    new Vector3(1f, 0f, 1f),
                    new Vector3(0f, 0f, 1f),
                    Vector3.zero,
                },
                cumulativePathLengths = new[] { 0f, 1f, 2f, 3f, 4f },
                cumulativeYaw = new[] { 0f, 45f, 90f, 135f, 180f },
            };
        }
    }
}
