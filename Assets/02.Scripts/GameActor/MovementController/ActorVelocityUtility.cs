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
    }
}
