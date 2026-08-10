using UnityEngine;

namespace UPlayGround.MovementController
{
    public static class ActorVelocityUtility
    {
        /// <summary>
        /// 애니메이션/워프가 만든 평면 속도만 사용하고 수직 속도는 KCC 권위값을 보존한다.
        /// </summary>
        public static Vector3 ReplacePlanarPreserveVertical(
            Vector3 desiredVelocity,
            Vector3 authoritativeVelocity,
            Vector3 up)
        {
            Vector3 normalizedUp = up.sqrMagnitude > 0.0001f ? up.normalized : Vector3.up;
            return Vector3.ProjectOnPlane(desiredVelocity, normalizedUp)
                   + normalizedUp * Vector3.Dot(authoritativeVelocity, normalizedUp);
        }

        public static Vector3 Planar(Vector3 velocity, Vector3 up)
        {
            Vector3 normalizedUp = up.sqrMagnitude > 0.0001f ? up.normalized : Vector3.up;
            return Vector3.ProjectOnPlane(velocity, normalizedUp);
        }

        /// <summary>
        /// 지상 루트모션이 경사면 접선으로 투영되며 만든 양의 수직 속도가
        /// 접지 이탈 뒤 Launch처럼 이어지는 것을 제거한다.
        /// 명시적 ForceUnground와 이미 공중에서 시작한 속도는 보존한다.
        /// </summary>
        public static Vector3 SuppressGroundedSlopeUpwardCarry(
            Vector3 velocity,
            Vector3 up,
            Vector3 groundNormal,
            bool isStableOnGround,
            bool wasStableOnGround,
            bool mustUnground)
        {
            if (mustUnground || (!isStableOnGround && !wasStableOnGround))
                return velocity;

            Vector3 normalizedUp = up.sqrMagnitude > 0.0001f
                ? up.normalized
                : Vector3.up;
            float upwardSpeed = Vector3.Dot(velocity, normalizedUp);
            if (upwardSpeed <= 0f)
                return velocity;

            Vector3 planarVelocity =
                Vector3.ProjectOnPlane(velocity, normalizedUp);
            if (planarVelocity.sqrMagnitude <= 0.0001f)
                return velocity;

            Vector3 normalizedGroundNormal = groundNormal.sqrMagnitude > 0.0001f
                ? groundNormal.normalized
                : normalizedUp;
            Vector3 directionRight =
                Vector3.Cross(planarVelocity.normalized, normalizedUp);
            if (directionRight.sqrMagnitude <= 0.0001f)
                return velocity;

            Vector3 groundTangent =
                Vector3.Cross(normalizedGroundNormal, directionRight).normalized;
            if (Vector3.Dot(groundTangent, planarVelocity) < 0f)
                groundTangent = -groundTangent;

            float tangentUpward = Vector3.Dot(groundTangent, normalizedUp);
            float tangentPlanar =
                Vector3.ProjectOnPlane(groundTangent, normalizedUp).magnitude;
            if (tangentUpward <= 0f || tangentPlanar <= 0.0001f)
                return velocity;

            float slopeUpwardSpeed =
                planarVelocity.magnitude * tangentUpward / tangentPlanar;
            float suppressedSpeed = Mathf.Min(upwardSpeed, slopeUpwardSpeed);
            return velocity - normalizedUp * suppressedSpeed;
        }
    }
}
