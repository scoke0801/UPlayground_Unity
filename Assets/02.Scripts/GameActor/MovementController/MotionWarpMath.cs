using UnityEngine;

namespace UPlayGround.MovementController
{
    /// <summary>
    /// Motion Warp의 도착점과 보정 예산을 계산하는 순수 수학 계층.
    /// KCC/MonoBehaviour에 의존하지 않아 EditMode에서 체감 계약을 회귀 검증할 수 있다.
    /// </summary>
    public static class MotionWarpMath
    {
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
