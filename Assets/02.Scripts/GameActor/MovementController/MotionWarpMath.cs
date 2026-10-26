using UnityEngine;

namespace UPlayGround.MovementController
{
    /// <summary>
    /// Motion Warp의 도착점과 보정 예산을 계산하는 순수 수학 계층.
    /// KCC/MonoBehaviour에 의존하지 않아 EditMode에서 체감 계약을 회귀 검증할 수 있다.
    /// </summary>
    public static class MotionWarpMath
    {
        /// <summary>원본이 실제로 진행한 구간에만 보정을 배분하며 미소비분을 다음 프레임으로 넘기지 않는다.</summary>
        public static Vector3 DistributeEndpointCorrection(Vector3 correction, float pathBefore,
            float pathAfter, float totalPath, float actualPath)
        {
            if (totalPath <= 0.0001f || actualPath <= 0f) return Vector3.zero;
            float traversed = Mathf.Min(Mathf.Max(0f, pathAfter - pathBefore), actualPath);
            return correction * (traversed / totalPath);
        }

        /// <summary>원본 진행·각도를 확인하고 저작 정책에 따라 끝점 보정을 거절하거나 제한한다.</summary>
        public static bool TryResolveEndpointCorrection(Vector3 source, float sourcePath,
            Vector3 desired, in MotionWarpWindowSettings settings, out Vector3 correction)
        {
            correction = Vector3.zero;
            float sourceLength = source.magnitude;
            if (sourcePath <= 0.0001f || sourceLength < settings.minimumSourceDisplacement
                || sourceLength / sourcePath < settings.minimumNetToPathRatio) return false;
            Vector3 candidate = desired - source;
            // 저작 곡선은 원본 경로를 늘리는 것이 아니라 거리 좁히기를 명시적으로 저작한 것이다.
            // 원본 순이동 비율 상한을 적용하면 원본 이동이 작은 검격일수록 다가가지 못하므로 절대 거리 상한만 쓴다.
            float ratioBudget = settings.correctionTiming == MotionWarpCorrectionTiming.AuthoredCurve
                ? float.PositiveInfinity
                : sourceLength * settings.maxCorrectionRatio;
            float allowed = Mathf.Max(0f, Mathf.Min(settings.maxCorrectionDistance, ratioBudget));
            if (Vector3.Angle(source, desired) > settings.maxWarpAngle) return false;
            if (candidate.magnitude > allowed + 0.0001f
                && settings.correctionLimitPolicy != MotionWarpCorrectionLimitPolicy.Clamp) return false;
            correction = Vector3.ClampMagnitude(candidate, allowed);
            return true;
        }

        /// <summary>
        /// 저작 곡선의 누적 보정 비율을 구한다. 구간 끝(1)에서는 곡선 끝값과 무관하게 전량을 소비하고,
        /// 곡선이 비어 있으면 일정한 속도로 소비한다.
        /// </summary>
        public static float EvaluateCorrectionTimingFraction(AnimationCurve curve, float translationProgress)
        {
            float progress = Mathf.Clamp01(translationProgress);
            if (progress >= 1f)
                return 1f;
            if (curve == null || curve.length == 0)
                return progress;
            return Mathf.Clamp01(curve.Evaluate(progress));
        }

        /// <summary>
        /// 저작 곡선의 목표 비율까지 보정을 소비한다. 한 스텝 거리 상한에 걸려 못 쓴 몫은 소비 비율에 남겨
        /// 곡선이 완만해지는 뒤 스텝이 이어 쓴다. 누적 소비는 총량을 넘지 않는다.
        /// </summary>
        public static Vector3 ConsumeAuthoredCorrection(Vector3 total, float targetFraction,
            float maxStepDistance, ref float consumedFraction, out bool isSpeedLimited)
        {
            isSpeedLimited = false;
            float totalLength = total.magnitude;
            float pendingFraction = Mathf.Clamp01(targetFraction) - consumedFraction;
            if (totalLength <= 0.000001f || pendingFraction <= 0f)
                return Vector3.zero;

            Vector3 step = total * pendingFraction;
            if (maxStepDistance >= 0f && step.magnitude > maxStepDistance)
            {
                step = Vector3.ClampMagnitude(step, maxStepDistance);
                isSpeedLimited = true;
            }

            consumedFraction = Mathf.Min(1f, consumedFraction + step.magnitude / totalLength);
            return step;
        }

        /// <summary>
        /// 회전 워프가 구간 진행도에 따라 소비한 비율(rotationWeight 적용 전)을 구한다.
        /// 회전 적용과 이동 예측이 같은 값을 써야 예측 궤적이 실제 회전과 어긋나지 않는다.
        /// </summary>
        public static float EvaluateRotationAlpha(AnimationCurve curve, float windowProgress)
        {
            float progress = Mathf.Clamp01(windowProgress);
            return curve != null && curve.length > 0
                ? Mathf.Clamp01(curve.Evaluate(progress))
                : 1f - (1f - progress) * (1f - progress);
        }

        public static Vector3 ResolveContactShellDestination(
            Vector3 attackerRoot,
            Vector3 attackerCenter,
            float attackerRadius,
            Vector3 targetCenter,
            Quaternion targetRotation,
            float targetRadius,
            float desiredStandOff,
            Vector3 localArrivalOffset)
        {
            Vector3 toTarget = targetCenter - attackerCenter;
            toTarget.y = 0f;
            Vector3 approachDirection = toTarget.sqrMagnitude > 0.000001f
                ? toTarget.normalized
                : Vector3.forward;

            float shellDistance = Mathf.Max(0f, attackerRadius)
                                  + Mathf.Max(0f, targetRadius)
                                  + Mathf.Max(0f, desiredStandOff);
            Vector3 desiredCenter = targetCenter
                                    - approachDirection * shellDistance
                                    + targetRotation * localArrivalOffset;
            Vector3 centerOffset = attackerCenter - attackerRoot;
            Vector3 desiredRoot = desiredCenter - centerOffset;
            desiredRoot.y = attackerRoot.y;
            return desiredRoot;
        }

        public static Vector3 LimitCorrection(
            Vector3 correction,
            float remainingOriginalDistance,
            float maxCorrectionDistance,
            float maxCorrectionRatio)
        {
            correction.y = 0f;
            float absoluteBudget = maxCorrectionDistance > 0f
                ? maxCorrectionDistance
                : float.PositiveInfinity;
            float ratioBudget = maxCorrectionRatio > 0f
                ? Mathf.Max(0f, remainingOriginalDistance) * maxCorrectionRatio
                : float.PositiveInfinity;
            float budget = Mathf.Min(absoluteBudget, ratioBudget);
            if (float.IsPositiveInfinity(budget))
                return correction;
            return Vector3.ClampMagnitude(correction, Mathf.Max(0f, budget));
        }

        public static bool IsInsideContactDeadZone(
            float centerDistance,
            float attackerRadius,
            float targetRadius,
            float desiredStandOff,
            float deadZone)
        {
            if (deadZone <= 0f)
                return false;
            float reach = Mathf.Max(0f, attackerRadius)
                          + Mathf.Max(0f, targetRadius)
                          + Mathf.Max(0f, desiredStandOff)
                          + deadZone;
            return centerDistance <= reach;
        }

        public static bool IsInsideWarpAngle(
            Vector3 forward,
            Vector3 toTarget,
            float maximumAngle)
        {
            if (maximumAngle <= 0f || maximumAngle >= 180f)
                return true;
            forward.y = 0f;
            toTarget.y = 0f;
            if (forward.sqrMagnitude <= 0.000001f
                || toTarget.sqrMagnitude <= 0.000001f)
                return true;
            return Vector3.Angle(forward, toTarget) <= maximumAngle;
        }

        public static float EvaluateTranslationWeight(
            AnimationCurve curve,
            float normalizedTime,
            float remainingTime,
            float endLeadTime)
        {
            if (endLeadTime > 0f && remainingTime <= endLeadTime)
                return 0f;
            if (curve == null || curve.length == 0)
                return 1f;
            return Mathf.Clamp01(curve.Evaluate(Mathf.Clamp01(normalizedTime)));
        }

        public static bool ResolvePlaybackRateWarp(
            PlaybackRateWarpPolicy policy,
            WarpArrivalMode arrivalMode,
            bool legacyAuthoredValue)
        {
            return policy switch
            {
                PlaybackRateWarpPolicy.Disabled => false,
                PlaybackRateWarpPolicy.Enabled => true,
                _ => arrivalMode == WarpArrivalMode.TargetCenter || legacyAuthoredValue,
            };
        }

        /// <summary>
        /// 현재 프레임을 포함한 윈도우 잔여 루트 변위를 현재 액터의 월드 방향으로 해석한다.
        /// </summary>
        public static Vector3 ResolveRemainingRootMotion(
            Vector3 totalLocal,
            Vector3 accumulatedLocalIncludingCurrentFrame,
            Vector3 currentFrameLocal,
            Quaternion actorRotation)
        {
            Vector3 accumulatedBeforeCurrentFrame =
                accumulatedLocalIncludingCurrentFrame - currentFrameLocal;
            Vector3 remainingLocal =
                totalLocal - accumulatedBeforeCurrentFrame;
            Vector3 remainingWorld = actorRotation * remainingLocal;
            remainingWorld.y = 0f;
            return remainingWorld;
        }

        /// <summary>현재 프레임을 포함한 잔여 루트 경로 길이를 구한다.</summary>
        public static float ResolveRemainingRootPath(
            float totalPath,
            float accumulatedPathIncludingCurrentFrame,
            float currentFramePath)
        {
            float accumulatedBeforeCurrentFrame =
                accumulatedPathIncludingCurrentFrame - currentFramePath;
            return Mathf.Max(
                Mathf.Max(0f, currentFramePath),
                totalPath - accumulatedBeforeCurrentFrame);
        }

        /// <summary>애니메이션 시간축의 워프 윈도우 진행도를 0~1로 환산한다.</summary>
        public static float ResolveWindowProgress(
            float animationTime,
            float windowStart,
            float windowEnd)
        {
            float duration = windowEnd - windowStart;
            if (duration <= 0.000001f)
                return 1f;
            return Mathf.Clamp01((animationTime - windowStart) / duration);
        }

        /// <summary>
        /// 누적 보정 진행도 q0→q1을 현재 남은 보정량에서 소비할 비율로 변환한다.
        /// 마지막 animation span이 종료점을 넘으면 항상 1을 반환한다.
        /// </summary>
        public static float ResolveResidualCorrectionShare(
            float previousProgress,
            float currentProgress)
        {
            float q0 = Mathf.Clamp01(previousProgress);
            float q1 = Mathf.Clamp(currentProgress, q0, 1f);
            return Mathf.Clamp01(
                (q1 - q0) / Mathf.Max(1f - q0, 0.0001f));
        }
    }
}
