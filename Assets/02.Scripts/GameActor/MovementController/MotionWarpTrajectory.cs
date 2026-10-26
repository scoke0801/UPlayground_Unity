using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Event;
using System.Globalization;
using System.Text;

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

        /// <summary>Player에서도 비교할 수 있는 클립 재생 범위·속도 서명이다. 임포트 내용은 빌드 검증이 확인한다.</summary>
        public static string BuildSourceSignature(MotionSet set)
        {
            if (set?.motions == null || set.HasPlaybackLayers) return string.Empty;
            var builder = new StringBuilder();
            foreach (UPlayGround.Animation.Motion motion in set.motions)
            {
                if (motion?.motionClip == null) return string.Empty;
                builder.Append(motion.motionClip.name).Append('|');
                builder.Append(motion.motionClip.length.ToString("R", CultureInfo.InvariantCulture)).Append('|');
                builder.Append(motion.ClipStartTime.ToString("R", CultureInfo.InvariantCulture)).Append('|');
                builder.Append(motion.ClipEndTime.ToString("R", CultureInfo.InvariantCulture)).Append('|');
                builder.Append(motion.playbackSpeed.ToString("R", CultureInfo.InvariantCulture)).Append(';');
            }
            return Hash128.Compute(builder.ToString()).ToString();
        }

        /// <summary>동명 클립도 참조 자체와 재생 구간을 함께 비교해 프로필 혼용을 막는다.</summary>
        public static bool MatchesSource(MotionWarpRootMotionBakeProfile profile, MotionSet set, string signature)
        {
            if (profile == null || set?.motions == null) return false;
            if (string.IsNullOrEmpty(signature) || profile.runtimeSourceSignature != signature
                || profile.sourceClips == null || profile.sourceClips.Length != set.motions.Count) return false;
            for (int i = 0; i < profile.sourceClips.Length; i++)
                if (profile.sourceClips[i] != set.motions[i].motionClip) return false;
            return true;
        }

        /// <summary>
        /// 런타임이 이동 보정에 실제로 쓰는 프로필인지 판정한다.
        /// 베이크 도구·빌드 검증·테스트가 같은 기준을 써야 "에디터는 통과, 런타임은 거절"이 생기지 않는다.
        /// </summary>
        public static bool IsUsableAtRuntime(MotionWarpRootMotionBakeProfile profile, Avatar avatar,
            Vector3 animatorScale, MotionSet set, string signature)
        {
            return profile != null
                   && profile.HasTrajectory
                   && profile.formatVersion == MotionEvent_MotionWarp.CurrentBakeFormatVersion
                   && profile.Matches(avatar, animatorScale)
                   && MatchesSource(profile, set, signature);
        }

        /// <summary>베이크 성공 후 실제 클립 참조와 재생 계약을 함께 기록한다.</summary>
        public static void RecordSource(MotionWarpRootMotionBakeProfile profile, MotionSet set)
        {
            profile.runtimeSourceSignature = BuildSourceSignature(set);
            profile.sourceClips = new AnimationClip[set.motions.Count];
            for (int i = 0; i < profile.sourceClips.Length; i++) profile.sourceClips[i] = set.motions[i].motionClip;
        }

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

        /// <summary>
        /// 회전 워프가 구간 동안 더할 yaw를 반영해 원본 궤적의 월드 변위를 예측한다.
        /// 회전하는 동안 실제 루트 델타도 함께 돌아가므로, 시작 방향 그대로 투영하면
        /// 대상 쪽으로 도는 공격의 끝점을 엉뚱한 방향으로 예측해 각도 초과로 오판한다.
        /// 누적 위치는 베이크 시작 자세 기준이고 yaw는 같은 축에서 교환 가능하므로 구간별 증분을 그 시점의 워프 yaw로 돌려 더한다.
        /// </summary>
        public static Vector3 PredictRotatedDisplacement(
            MotionWarpRootMotionBakeProfile profile,
            float progress,
            Quaternion basisRotation,
            Vector3 up,
            float warpYawDegrees,
            AnimationCurve rotationCurve,
            float rotationWeight)
        {
            int count = profile?.cumulativeLocalPositions?.Length ?? 0;
            if (count < 2)
                return Vector3.zero;

            float end = Mathf.Clamp01(progress);
            float segment = 1f / (count - 1);
            Vector3 world = Vector3.zero;
            Vector3 previous = profile.cumulativeLocalPositions[0];
            for (int index = 1; index < count; index++)
            {
                float segmentStart = (index - 1) * segment;
                if (segmentStart >= end)
                    break;
                float segmentEnd = Mathf.Min(index * segment, end);
                SampleRawCumulative(profile, segmentEnd, out Vector3 current, out _, out _);
                float alpha = MotionWarpMath.EvaluateRotationAlpha(rotationCurve, (segmentStart + segmentEnd) * 0.5f)
                              * rotationWeight;
                world += Quaternion.AngleAxis(warpYawDegrees * alpha, up) * (basisRotation * (current - previous));
                previous = current;
            }

            return world;
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
