using UnityEngine;

namespace UPlayGround.Data.Actor.NPC
{
    /// <summary>작업 시작 모션의 접촉 전에 끝내는 수평 정렬 범위를 정의한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/NPC/Work Alignment")]
    public sealed class NpcWorkAlignmentSO : ScriptableObject
    {
        [Tooltip("시작부터 손이 닿는 반복 모션은 보행으로 정렬을 마친 뒤 재생합니다.")]
        public bool alignBeforeMotion;
        [Min(0f)] public float approachTimeout = 3f;
        [Min(0f)] public float endTime = 0.55f;
        [Min(0f)] public float captureDistance = 0.15f;
        [Min(0f)] public float maxHeightDifference = 0.08f;
        [Range(0f, 180f)] public float maxStartAngle = 15f;
        [Min(0f)] public float maxSpeed = 0.5f;
        [Min(0f)] public float maxRotationSpeed = 90f;
        [Min(0f)] public float positionTolerance = 0.003f;
        [Range(0f, 180f)] public float angleTolerance = 1f;

        public bool IsValid => IsPositiveFinite(endTime) && IsPositiveFinite(approachTimeout) && IsPositiveFinite(captureDistance)
            && IsPositiveFinite(maxSpeed) && IsPositiveFinite(maxRotationSpeed)
            && IsPositiveFinite(positionTolerance) && positionTolerance <= captureDistance
            && IsNonNegativeFinite(maxHeightDifference) && IsNonNegativeFinite(maxStartAngle)
            && maxStartAngle <= 180f && IsNonNegativeFinite(angleTolerance) && angleTolerance <= maxStartAngle;

        public float ArrivalDistance => alignBeforeMotion ? positionTolerance : captureDistance;

        /// <summary>수직 이동이나 큰 방향 전환을 작업 모션 보정으로 숨기지 않는다.</summary>
        public bool CanStart(Vector3 position, Quaternion rotation, Vector3 target, Quaternion targetRotation, Vector3 up)
        {
            Vector3 offset = target - position;
            return IsValid && Vector3.ProjectOnPlane(offset, up).magnitude <= captureDistance
                && Mathf.Abs(Vector3.Dot(offset, up)) <= maxHeightDifference
                && Quaternion.Angle(rotation, targetRotation) <= maxStartAngle;
        }

        /// <summary>클립 시각의 목표 자세까지 남은 거리만 요청하여 물리 스텝 중복 적용을 막는다.</summary>
        public Vector3 EvaluateVelocity(Vector3 origin, Vector3 target, Vector3 current, Vector3 up, float motionTime, float deltaTime)
        {
            if (!IsValid || deltaTime <= 0f || float.IsNaN(motionTime)) return Vector3.zero;
            float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(motionTime / endTime));
            Vector3 desired = origin + Vector3.ProjectOnPlane(target - origin, up) * alpha;
            Vector3 remaining = Vector3.ProjectOnPlane(desired - current, up);
            return Vector3.ClampMagnitude(remaining / deltaTime, maxSpeed);
        }

        /// <summary>실제 도착과 높이 이탈을 확인해 정렬 뒤 공중에서 소품을 집는 것도 막는다.</summary>
        public bool HasArrived(Vector3 position, Quaternion rotation, Vector3 target, Quaternion targetRotation, Vector3 up)
            => IsValid && Vector3.ProjectOnPlane(target - position, up).magnitude <= positionTolerance
                && Mathf.Abs(Vector3.Dot(target - position, up)) <= maxHeightDifference
                && Quaternion.Angle(rotation, targetRotation) <= angleTolerance;

        private static bool IsPositiveFinite(float value) => value > 0f && !float.IsInfinity(value);
        private static bool IsNonNegativeFinite(float value) => value >= 0f && !float.IsInfinity(value);
    }
}
