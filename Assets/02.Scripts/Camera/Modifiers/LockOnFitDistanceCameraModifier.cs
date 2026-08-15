using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (670) 플레이어 기준 피벗은 유지하고, 락온 대상이 현재 거리로 화면에 담기지 않을 때만
    /// 카메라 거리를 늘려 플레이어와 대상을 모두 프러스텀 안에 넣는다.
    ///
    /// - 거리만 조정한다(FOV/피치/회전 불변). 필요 시 일반 maxDistance를 넘어 lockOnFitMaxDistance까지.
    /// - "필요할 때만 키우는" max(기본거리, 요구거리) 방식이라 카메라를 앞으로 당기지 않는다.
    /// - 대상 포커스는 항상 검사하고, 실제 상단은 의미 있는 고저차가 있을 때만 검사해 미세 진동을 줄인다.
    /// - 거리 상한은 frame.DistanceCeiling으로 Follow(700)/Collision(800)의 클램프에 전달한다.
    ///
    /// 요구 거리는 현재 yaw/pitch가 고정된 한 프레임에서 닫힌 식으로 계산한다.
    /// 카메라 위치 C = pivot - camForward*d 이므로, 프레이밍 점 X에 대해
    ///   z = dot(X-pivot, camForward) + d, (y,x) = dot(X-pivot, camUp/Right)
    /// 가 되고, 세로 FOV 안에 담기려면 |y|/z ≤ tan(α) → d ≥ |y|/tan(α) - dot(X-pivot,camForward).
    /// (가로도 동일) 모든 프레이밍 점의 최댓값이 요구 거리.
    /// </summary>
    public sealed class LockOnFitDistanceCameraModifier : ICameraModifier
    {
        private bool _active;
        private float _fitDistance;
        private float _fitVelocity;
        private float _baseDistance;

        public int Priority => 670;

        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            CameraSettings settings = context?.Settings;
            CameraState state = frame.State;
            if (settings == null || state == null)
                return;

            float smoothTime = Mathf.Max(0.0001f, settings.lockOnFitSmoothTime);
            float dt = Mathf.Max(frame.DeltaTime, 0.0001f);

            Vector3 focus = Vector3.zero;
            Vector3 top = Vector3.zero;
            bool isLockOn = context.LockOn?.IsActive ?? false;
            bool canFit = settings.enableLockOnFitDistance && isLockOn && !context.IsInputLocked
                          && context.MainCamera != null && context.Target != null
                          && context.LockOn.TryGetTargetFramingPoints(settings.lockOnFitTopPadding, out focus, out top);

            if (!canFit)
            {
                // 비활성 전환: 프레이밍 적용 전 기준 거리로 복귀한다.
                // 락온 해제 시 일반 거리 로직은 유저 줌을 존중해 -1을 반환할 수 있으므로,
                // 여기서 마지막 인플레이트 거리를 남기지 않도록 정리한다.
                if (!_active)
                    return;

                float restoreDistance = Mathf.Clamp(_baseDistance, settings.minDistance, settings.maxDistance);
                _fitDistance = Mathf.SmoothDamp(
                    _fitDistance,
                    restoreDistance,
                    ref _fitVelocity,
                    smoothTime,
                    Mathf.Infinity,
                    dt);
                state.TargetDistance = _fitDistance;
                frame.DistanceCeiling = Mathf.Max(frame.DistanceCeiling, _fitDistance);

                if (Mathf.Abs(_fitDistance - restoreDistance) <= 0.01f)
                {
                    state.TargetDistance = restoreDistance;
                    _active = false;
                    _fitVelocity = 0f;
                }
                return;
            }

            // Follow(700)에서 적용될 쌍 프레이밍 피벗을 같은 기준으로 사용한다.
            Vector3 playerFocus = context.Target.position + state.CameraOffset;
            Vector3 pivot = playerFocus + context.LockOn.CurrentPivotOffset;
            float baseDistance = state.TargetDistance;
            _baseDistance = baseDistance;

            // 피벗 이동 사용 여부와 무관하게 플레이어/대상 포커스는 항상 검사한다.
            // 대상 상단은 충분한 고저차가 있을 때만 포함해 일반 지상 대상의 콜라이더 흔들림을 피한다.
            bool includeTop = top.y - pivot.y >= settings.lockOnFitMinHeightDiff;
            float requiredDistance = ComputeRequiredDistance(
                context.MainCamera,
                settings,
                state,
                pivot,
                playerFocus,
                focus,
                top,
                includeTop);

            float cap = Mathf.Max(baseDistance, settings.lockOnFitMaxDistance);
            float target = Mathf.Clamp(Mathf.Max(baseDistance, requiredDistance), settings.minDistance, cap);

            if (!_active)
            {
                _fitDistance = baseDistance;
                _fitVelocity = 0f;
                _active = true;
            }

            _fitDistance = Mathf.SmoothDamp(_fitDistance, target, ref _fitVelocity, smoothTime, Mathf.Infinity, dt);

            state.TargetDistance = _fitDistance;
            // 상한 후보로 _fitDistance를 올린다. _fitDistance가 maxDistance 이하면 소비측(Follow/Collision)이
            // Max(settings.maxDistance, ceiling)로 흡수해 결국 maxDistance가 적용되므로 효과는 일반과 동일하다.
            frame.DistanceCeiling = Mathf.Max(frame.DistanceCeiling, _fitDistance);
        }

        private static float ComputeRequiredDistance(
            UnityEngine.Camera cam,
            CameraSettings settings,
            CameraState state,
            Vector3 pivot,
            Vector3 playerFocus,
            Vector3 focus,
            Vector3 top,
            bool includeTop)
        {
            Quaternion rot = Quaternion.Euler(state.CurrentPitch, state.CurrentYaw, 0f);
            Vector3 fwd = rot * Vector3.forward;
            Vector3 up = rot * Vector3.up;
            Vector3 right = rot * Vector3.right;

            float safe = Mathf.Clamp(settings.lockOnFitSafeFraction, 0.3f, 1f);
            float vHalf = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float tanV = Mathf.Max(0.0001f, Mathf.Tan(vHalf * safe));
            float hHalf = Mathf.Atan(Mathf.Tan(vHalf) * Mathf.Max(0.0001f, cam.aspect));
            float tanH = Mathf.Max(0.0001f, Mathf.Tan(hHalf * safe));

            float required = RequiredFor(playerFocus - pivot, fwd, up, right, tanV, tanH);
            required = Mathf.Max(required, RequiredFor(focus - pivot, fwd, up, right, tanV, tanH));
            if (includeTop)
                required = Mathf.Max(required, RequiredFor(top - pivot, fwd, up, right, tanV, tanH));
            return required;
        }

        private static float RequiredFor(Vector3 rel, Vector3 fwd, Vector3 up, Vector3 right, float tanV, float tanH)
        {
            float aFwd = Vector3.Dot(rel, fwd);
            float reqV = Mathf.Abs(Vector3.Dot(rel, up)) / tanV - aFwd;
            float reqH = Mathf.Abs(Vector3.Dot(rel, right)) / tanH - aFwd;
            return Mathf.Max(reqV, reqH);
        }
    }
}
