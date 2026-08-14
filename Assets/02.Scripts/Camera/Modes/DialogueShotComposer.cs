using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 샷 종류 + 인물 배치 → 카메라 포즈.
    ///
    /// 핵심 규칙은 가상선(180° 룰)이다. 카메라의 측면 방향을 매번 "주시 대상 기준"으로 다시 계산하면
    /// 화자가 바뀔 때 축이 180° 뒤집혀 카메라가 선을 넘고 두 인물의 화면 좌우가 교대로 뒤바뀐다.
    /// 그래서 측면 벡터는 세션이 고정한 AxisRight * SideSign만 사용하고,
    /// 화자 전환으로 바뀌는 것은 "대상을 향한 전후 방향"뿐이다.
    /// </summary>
    public static class DialogueShotComposer
    {
        public struct FramedPose
        {
            public Vector3 LookAt;
            public Vector3 Position;
            public Quaternion Rotation;
            public float FieldOfView;
        }

        /// <summary>
        /// 해당 샷이 주시하는 인물. Director와 Composer가 같은 규칙을 쓰도록 공용화한다.
        /// </summary>
        public static Transform ResolveSubject(in DialogueShotRequest request, DialogueShotType shotType)
        {
            switch (shotType)
            {
                case DialogueShotType.OverTheShoulderListener:
                    return request.Listener != null ? request.Listener : request.Speaker;

                case DialogueShotType.Reaction:
                    if (request.ReactionSubject != null)
                        return request.ReactionSubject;
                    return request.Listener != null ? request.Listener : request.Speaker;

                default:
                    return request.Speaker != null ? request.Speaker : request.Listener;
            }
        }

        /// <summary>주시 대상의 반대편 인물(어깨를 걸치는 쪽).</summary>
        public static Transform ResolveAnchor(in DialogueShotRequest request, Transform subject)
        {
            if (subject == request.Speaker)
                return request.Listener;
            if (subject == request.Listener)
                return request.Speaker;

            // 리액션 대상이 화자/청자 어느 쪽도 아니면 화자를 기준 인물로 삼는다.
            return request.Speaker != null ? request.Speaker : request.Listener;
        }

        public static FramedPose Compose(
            CameraContext context,
            DialogueCameraSettingsSO settings,
            DialogueShotSession session,
            in DialogueShotRequest request,
            DialogueShotType shotType,
            bool useCollision)
        {
            Transform subject = ResolveSubject(request, shotType);
            Transform anchor = ResolveAnchor(request, subject);

            DialogueShotPreset preset = settings.ResolvePreset(shotType);
            bool framesBoth = preset.framesBothActors && subject != null && anchor != null;

            Vector3 offset = request.HasShoulderOffsetOverride
                ? request.ShoulderOffsetOverride
                : preset.shoulderOffset;

            float distance = request.DistanceOverride > 0f ? request.DistanceOverride : preset.distance;

            Vector3 lookAt;
            if (framesBoth)
            {
                Vector3 mid = (subject.position + anchor.position) * 0.5f;
                lookAt = mid + preset.lookAtOffset;

                // 두 인물이 벌어질수록 물러나 둘 다 화면에 담는다.
                Vector3 separationDelta = subject.position - anchor.position;
                separationDelta.y = 0f;
                distance = Mathf.Max(distance, separationDelta.magnitude * preset.separationFitScale);
                distance = settings.ClampFramingDistance(distance);
            }
            else
            {
                Vector3 subjectPosition = subject != null
                    ? subject.position
                    : (context.Target != null ? context.Target.position : Vector3.zero);

                lookAt = subjectPosition + preset.lookAtOffset;
                distance = settings.ClampDistance(distance);
            }

            Vector3 forward = ResolveForward(session, settings, subject, anchor);
            Vector3 side = ResolveSide(session, settings, forward);

            Vector3 direction = side * offset.x + Vector3.up * offset.y + forward * offset.z;
            if (direction.sqrMagnitude < 0.0001f)
                direction = -forward;
            direction.Normalize();

            if (useCollision && context.Collision != null)
                distance = Mathf.Max(0.1f, context.Collision.Evaluate(lookAt, direction, distance));

            Vector3 position = lookAt + direction * distance;
            Vector3 toLookAt = lookAt - position;
            Quaternion rotation = toLookAt.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(toLookAt, Vector3.up)
                : Quaternion.identity;

            return new FramedPose
            {
                LookAt = lookAt,
                Position = position,
                Rotation = rotation,
                FieldOfView = preset.fieldOfView
            };
        }

        /// <summary>
        /// 기준 인물 → 주시 대상 방향(수평). 가상선이 있으면 그 축을 대상 쪽으로 정렬해서 쓴다.
        /// 축 자체는 세션이 고정하므로 인물이 미세하게 움직여도 구도가 흔들리지 않는다.
        /// </summary>
        private static Vector3 ResolveForward(
            DialogueShotSession session,
            DialogueCameraSettingsSO settings,
            Transform subject,
            Transform anchor)
        {
            Vector3 raw = Vector3.zero;
            if (subject != null && anchor != null)
            {
                raw = subject.position - anchor.position;
                raw.y = 0f;
            }

            if (session != null && session.HasAxis && settings.enforce180Rule)
            {
                Vector3 axis = session.AxisForward;

                // 대상이 축의 어느 끝인지에 따라 축을 뒤집는다(측면 벡터는 건드리지 않는다).
                if (raw.sqrMagnitude > 0.0001f)
                    return Vector3.Dot(axis, raw) >= 0f ? axis : -axis;

                // 대상이 하나뿐이면(상대 미확정) 플레이어 → 대상 방향으로 축을 맞춘다.
                if (subject != null && session.Player != null && subject != session.Player)
                {
                    Vector3 fromPlayer = subject.position - session.Player.position;
                    fromPlayer.y = 0f;
                    if (fromPlayer.sqrMagnitude > 0.0001f)
                        return Vector3.Dot(axis, fromPlayer) >= 0f ? axis : -axis;
                }

                return axis;
            }

            if (raw.sqrMagnitude > 0.0001f)
                return raw.normalized;

            Vector3 fallback = subject != null ? subject.forward : Vector3.forward;
            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
        }

        /// <summary>
        /// 카메라가 머무는 측면 방향. 세션이 고정한 쪽을 그대로 쓴다 — 이것이 180° 룰의 실질적 보장이다.
        /// </summary>
        private static Vector3 ResolveSide(
            DialogueShotSession session,
            DialogueCameraSettingsSO settings,
            Vector3 forward)
        {
            if (session != null && session.HasAxis && settings.enforce180Rule)
                return session.AxisRight * session.SideSign;

            return Vector3.Cross(Vector3.up, forward).normalized;
        }
    }
}
