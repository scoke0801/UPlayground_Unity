using UnityEngine;

namespace UPlayGround.MovementController
{
    /// <summary>애니메이션 시간 구간에서 조회한 원본 trajectory의 현재·잔여 누적값.</summary>
    public readonly struct MotionWarpTrajectoryFrame
    {
        public readonly Vector3 FrameLocalDelta;
        public readonly Vector3 RemainingLocalDelta;
        public readonly float PathBefore;
        public readonly float PathAfter;
        public readonly float TotalPath;
        public readonly float RemainingPath;
        public readonly float FrameYaw;
        public readonly float SourceSpeed;

        public MotionWarpTrajectoryFrame(
            Vector3 frameLocalDelta,
            Vector3 remainingLocalDelta,
            float pathBefore,
            float pathAfter,
            float totalPath,
            float frameYaw,
            float sourceSpeed)
        {
            FrameLocalDelta = frameLocalDelta;
            RemainingLocalDelta = remainingLocalDelta;
            PathBefore = pathBefore;
            PathAfter = pathAfter;
            TotalPath = totalPath;
            RemainingPath = Mathf.Max(0f, totalPath - pathBefore);
            FrameYaw = frameYaw;
            SourceSpeed = sourceSpeed;
        }
    }

    /// <summary>v3 누적 루트 trajectory를 선형 보간하며 프레임 분할에 독립적인 span을 만든다.</summary>
    public static class MotionWarpTrajectory
    {
        private const float MinimumDuration = 0.0001f;

        public static bool TryEvaluateFrame(
            MotionWarpRootMotionBakeProfile profile,
            float previousProgress,
            float currentProgress,
            float windowDuration,
            bool amplifyEnabled,
            AnimationCurve amplifyCurve,
            float amplifyMaxSpeed,
            out MotionWarpTrajectoryFrame frame)
        {
            frame = default;
            if (profile == null || !profile.HasTrajectory)
                return false;

            float q0 = Mathf.Clamp01(previousProgress);
            float q1 = Mathf.Clamp(currentProgress, q0, 1f);
            SampleCumulative(
                profile,
                q0,
                windowDuration,
                amplifyEnabled,
                amplifyCurve,
                amplifyMaxSpeed,
                out Vector3 position0,
                out float path0,
                out float yaw0);
            SampleCumulative(
                profile,
                q1,
                windowDuration,
                amplifyEnabled,
                amplifyCurve,
                amplifyMaxSpeed,
                out Vector3 position1,
                out float path1,
                out float yaw1);
            SampleCumulative(
                profile,
                1f,
                windowDuration,
                amplifyEnabled,
                amplifyCurve,
                amplifyMaxSpeed,
                out Vector3 endPosition,
                out float totalPath,
                out _);

            float animationSpan = (q1 - q0) * Mathf.Max(windowDuration, 0f);
            Vector3 frameDelta = position1 - position0;
            float sourceSpeed = animationSpan > MinimumDuration
                ? frameDelta.magnitude / animationSpan
                : 0f;
            frame = new MotionWarpTrajectoryFrame(
                frameDelta,
                endPosition - position0,
                path0,
                path1,
                totalPath,
                yaw1 - yaw0,
                sourceSpeed);
            return true;
        }

        /// <summary>균등 샘플 trajectory의 정규화 시각 누적값을 선형 보간한다.</summary>
        public static void SampleRawCumulative(
            MotionWarpRootMotionBakeProfile profile,
            float progress,
            out Vector3 localPosition,
            out float pathLength,
            out float yaw)
        {
            int count = profile?.cumulativeLocalPositions?.Length ?? 0;
            if (count < 2
                || profile.cumulativePathLengths == null
                || profile.cumulativeYaw == null
                || profile.cumulativePathLengths.Length != count
                || profile.cumulativeYaw.Length != count)
            {
                localPosition = Vector3.zero;
                pathLength = 0f;
                yaw = 0f;
                return;
            }

            float samplePosition = Mathf.Clamp01(progress) * (count - 1);
            int index = Mathf.Min(Mathf.FloorToInt(samplePosition), count - 2);
            float alpha = samplePosition - index;
            localPosition = Vector3.LerpUnclamped(
                profile.cumulativeLocalPositions[index],
                profile.cumulativeLocalPositions[index + 1],
                alpha);
            pathLength = Mathf.LerpUnclamped(
                profile.cumulativePathLengths[index],
                profile.cumulativePathLengths[index + 1],
                alpha);
            yaw = Mathf.LerpUnclamped(
                profile.cumulativeYaw[index],
                profile.cumulativeYaw[index + 1],
                alpha);
        }

        private static void SampleCumulative(
            MotionWarpRootMotionBakeProfile profile,
            float progress,
            float windowDuration,
            bool amplifyEnabled,
            AnimationCurve amplifyCurve,
            float amplifyMaxSpeed,
            out Vector3 localPosition,
            out float pathLength,
            out float yaw)
        {
            if (!amplifyEnabled)
            {
                SampleRawCumulative(
                    profile,
                    progress,
                    out localPosition,
                    out pathLength,
                    out yaw);
                return;
            }

            Vector3[] positions = profile.cumulativeLocalPositions;
            float[] paths = profile.cumulativePathLengths;
            float[] yaws = profile.cumulativeYaw;
            int segmentCount = positions.Length - 1;
            float scaledProgress = Mathf.Clamp01(progress) * segmentCount;
            int completedSegments = Mathf.Min(
                Mathf.FloorToInt(scaledProgress),
                segmentCount);
            float partial = completedSegments < segmentCount
                ? scaledProgress - completedSegments
                : 0f;
            float segmentDuration = Mathf.Max(windowDuration, 0f) / segmentCount;

            localPosition = Vector3.zero;
            pathLength = 0f;
            for (int index = 0; index < completedSegments; index++)
                AccumulateAmplifiedSegment(
                    positions,
                    paths,
                    index,
                    1f,
                    segmentDuration,
                    amplifyCurve,
                    amplifyMaxSpeed,
                    ref localPosition,
                    ref pathLength);

            if (completedSegments < segmentCount && partial > 0f)
            {
                AccumulateAmplifiedSegment(
                    positions,
                    paths,
                    completedSegments,
                    partial,
                    segmentDuration,
                    amplifyCurve,
                    amplifyMaxSpeed,
                    ref localPosition,
                    ref pathLength);
            }

            float yawSample = Mathf.Clamp01(progress) * segmentCount;
            int yawIndex = Mathf.Min(Mathf.FloorToInt(yawSample), segmentCount - 1);
            float yawAlpha = yawSample - yawIndex;
            yaw = Mathf.LerpUnclamped(
                yaws[yawIndex],
                yaws[yawIndex + 1],
                yawAlpha);
        }

        private static void AccumulateAmplifiedSegment(
            Vector3[] positions,
            float[] paths,
            int index,
            float fraction,
            float segmentDuration,
            AnimationCurve amplifyCurve,
            float amplifyMaxSpeed,
            ref Vector3 localPosition,
            ref float pathLength)
        {
            Vector3 rawDelta = positions[index + 1] - positions[index];
            float rawPathDelta = Mathf.Max(0f, paths[index + 1] - paths[index]);
            float normalizedMidpoint = (index + 0.5f) / (positions.Length - 1);
            float gain = amplifyCurve != null && amplifyCurve.length > 0
                ? Mathf.Max(0f, amplifyCurve.Evaluate(normalizedMidpoint))
                : 1f;
            float appliedGain = gain;
            if (amplifyMaxSpeed > 0f && segmentDuration > MinimumDuration)
            {
                float speed = rawPathDelta * appliedGain / segmentDuration;
                if (speed > amplifyMaxSpeed)
                    appliedGain *= amplifyMaxSpeed / speed;
            }

            Vector3 amplifiedDelta = rawDelta * appliedGain;
            Vector3 consumed = amplifiedDelta * Mathf.Clamp01(fraction);
            localPosition += consumed;
            pathLength += rawPathDelta
                          * appliedGain
                          * Mathf.Clamp01(fraction);
        }
    }
}
