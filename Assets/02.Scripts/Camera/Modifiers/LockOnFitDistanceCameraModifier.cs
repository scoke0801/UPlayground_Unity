using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// (670) 락온 대상이 상단·공중에 있어 피치 클램프(lockOnPitchMin)만으로는 화면에 담기지 않을 때,
    /// 카메라 거리를 늘려 플레이어 피벗과 대상이 모두 프러스텀 안에 들어오게 한다.
    ///
    /// - 거리만 조정한다(FOV/피치/회전 불변). 필요 시 일반 maxDistance를 넘어 lockOnFitMaxDistance까지.
    /// - "필요할 때만 키우는" max(기본거리, 요구거리) 방식이라 수평/하단 대상에는 무영향.
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
                // 비활성 전환: 인플레이트된 거리를 maxDistance까지 부드럽게 되돌린 뒤 소유권을 놓는다.
                // (락온 해제 시 비-락온 거리 로직은 유저 줌을 존중해 -1을 반환하므로 여기서 정리하지 않으면 멀어진 채 남는다.)
                if (!_active)
                    return;

                _fitDistance = Mathf.SmoothDamp(_fitDistance, settings.maxDistance, ref _fitVelocity, smoothTime, Mathf.Infinity, dt);
                if (_fitDistance <= settings.maxDistance + 0.01f)
                {
                    _active = false;
                    return;
                }

                state.TargetDistance = _fitDistance;
                frame.DistanceCeiling = Mathf.Max(frame.DistanceCeiling, _fitDistance);
                return;
            }

            Vector3 pivot = context.Target.position + state.CameraOffset;
            float baseDistance = state.TargetDistance;

            // 대상 상단이 피벗보다 충분히 높을 때만 피팅을 시작한다(미세 진동 방지).
            float requiredDistance = 0f;
            if (top.y - pivot.y >= settings.lockOnFitMinHeightDiff)
                requiredDistance = ComputeRequiredDistance(context.MainCamera, settings, state, pivot, focus, top);

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
            Vector3 focus,
            Vector3 top)
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

            float required = RequiredFor(focus - pivot, fwd, up, right, tanV, tanH);
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
