using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>플레이어 크기를 보호하며 필요한 만큼만 피벗·거리를 보정해 높은 락온 지점을 담는다.</summary>
    public sealed class LockOnFitDistanceCameraModifier : ICameraModifier, ICameraModifierLifecycle
    {
        private float _heightOffset;
        private float _heightVelocity;
        private float _distanceOffset;
        private float _distanceVelocity;

        public int Priority => 740;

        /// <summary>다른 모드의 구도가 락온 프레이밍에 남지 않도록 초기화한다.</summary>
        public void OnEnter(CameraContext context, CameraModeEnterParams enterParams) => Reset();

        /// <summary>모드 종료 시 프레이밍 보간을 정리한다.</summary>
        public void OnExit(CameraContext context) => Reset();

        /// <summary>추적 완료된 피벗을 기준으로 프레이밍을 합성하고 사용자 줌 원본은 보존한다.</summary>
        public void Apply(ref CameraFrame frame)
        {
            CameraContext context = frame.Context;
            CameraSettings settings = context?.Settings;
            if (settings == null || frame.State == null || context.Target == null || context.MainCamera == null)
                return;

            if (context.IsInputLocked || context.LookAtOverride != null
                || (context.RotationTransition?.IsActive ?? false))
            {
                Reset();
                return;
            }

            Vector3 focus = Vector3.zero;
            Vector3 top = Vector3.zero;
            bool canFrame = (context.LockOn?.IsActive ?? false)
                && context.LockOn.TryGetTargetFramingPoints(settings.lockOnFitTopPadding, out focus, out top);
            top.y = Mathf.Min(top.y, focus.y + Mathf.Max(0f, settings.lockOnFramingFocusPadding));
            Vector3 playerFocus = context.Target.position + frame.State.CameraOffset;
            Vector3 forward = frame.Pose.CameraRotation * Vector3.forward;
            float baselineDepth = Mathf.Max(0.001f, frame.Pose.Distance
                + Vector3.Dot(playerFocus - frame.Pose.PivotPosition, forward));
            float maxPlayerDepth = baselineDepth / Mathf.Clamp(settings.lockOnFramingMinPlayerScale, 0.1f, 1f);
            float heightWeight = canFrame && settings.enableLockOnHeightFraming
                ? EvaluateHeightWeight(focus.y - playerFocus.y, settings) : 0f;
            bool hasHeightFraming = heightWeight > 0f;
            float framingPitch = frame.Pose.Pitch;
            if (hasHeightFraming && settings.enableLockOnPitchRecovery)
                framingPitch = Mathf.Clamp(settings.lockOnPreferredPitch,
                    settings.lockOnPitchLimits.x, settings.lockOnPitchLimits.y);
            Quaternion framingRotation = Quaternion.Euler(framingPitch, frame.Pose.Yaw, 0f);
            CameraPose heightPose = frame.Pose;
            heightPose.CameraRotation = framingRotation;
            heightPose.FieldOfView = context.DistanceController?.BaseFOV ?? frame.Pose.FieldOfView;

            float targetHeight = hasHeightFraming
                ? CalculateHeightOffset(heightPose, context.Target.position, focus, top, settings) * heightWeight : 0f;
            float smoothTime = Mathf.Max(0.001f, settings.lockOnFitSmoothTime);
            float deltaTime = Mathf.Max(0f, frame.DeltaTime);
            if (deltaTime > 0f)
                _heightOffset = Mathf.SmoothDamp(_heightOffset, targetHeight, ref _heightVelocity,
                    smoothTime, Mathf.Infinity, deltaTime);
            // 상승도 플레이어를 카메라 깊이 방향으로 멀어지게 하므로 거리와 같은 크기 예산을 쓴다.
            if (forward.y < -0.001f)
                _heightOffset = Mathf.Min(_heightOffset, (maxPlayerDepth - baselineDepth) / -forward.y);

            Vector3 pivot = frame.Pose.PivotPosition;
            Vector3 desiredPivot = pivot + Vector3.up * _heightOffset;
            // 상승한 피벗이 천장 너머로 넘어가면 뒤쪽 스프링암 검사만으로는 관통을 막을 수 없다.
            frame.Pose.PivotPosition = context.Collision != null
                ? context.Collision.ConstrainPivotPosition(pivot, desiredPivot) : desiredPivot;
            float distanceCap = Mathf.Max(frame.Pose.Distance, Mathf.Min(settings.lockOnFitMaxDistance,
                maxPlayerDepth - Vector3.Dot(playerFocus - frame.Pose.PivotPosition, forward)));

            float targetDistanceOffset = 0f;
            if (canFrame && (hasHeightFraming || settings.enableLockOnFitDistance))
            {
                CameraPose fittingPose = frame.Pose;
                fittingPose.FieldOfView = context.DistanceController?.BaseFOV ?? frame.Pose.FieldOfView;
                bool includeTop = hasHeightFraming || top.y - playerFocus.y >= settings.lockOnFitMinHeightDiff;
                float requiredDistance = ComputeRequiredDistance(fittingPose, context.Target.position,
                    focus, includeTop ? top : focus, context.MainCamera.aspect, settings.lockOnFitSafeFraction);
                // 복귀할 피치에서도 공간을 미리 확보해야 피치와 줌이 서로 뒤쫓지 않는다.
                fittingPose.CameraRotation = framingRotation;
                requiredDistance = Mathf.Max(requiredDistance, ComputeRequiredDistance(fittingPose,
                    context.Target.position, focus, includeTop ? top : focus,
                    context.MainCamera.aspect, settings.lockOnFitSafeFraction));
                targetDistanceOffset = Mathf.Clamp(requiredDistance - frame.Pose.Distance,
                    0f, distanceCap - frame.Pose.Distance);
            }

            if (deltaTime > 0f)
                _distanceOffset = Mathf.SmoothDamp(_distanceOffset, targetDistanceOffset,
                    ref _distanceVelocity, smoothTime, Mathf.Infinity, deltaTime);
            _distanceOffset = Mathf.Min(_distanceOffset, distanceCap - frame.Pose.Distance);
            frame.Pose.Distance += _distanceOffset;
            frame.DistanceCeiling = Mathf.Max(frame.DistanceCeiling, frame.Pose.Distance);
            frame.Pose.CameraPosition = frame.Pose.PivotPosition
                + frame.Pose.CameraRotation * Vector3.back * frame.Pose.Distance;
            if (hasHeightFraming)
                frame.LockOnFramingPitch = framingPitch;
        }

        private static float EvaluateHeightWeight(float height, CameraSettings settings)
        {
            float start = Mathf.Max(0f, settings.lockOnHeightFramingRange.x);
            float full = Mathf.Max(start + 0.01f, settings.lockOnHeightFramingRange.y);
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, full, height));
        }

        private static float CalculateHeightOffset(in CameraPose pose, Vector3 playerFeet,
            Vector3 focus, Vector3 top, CameraSettings settings)
        {
            Quaternion inverse = Quaternion.Inverse(pose.CameraRotation);
            Vector3 feetLocal = inverse * (playerFeet - pose.PivotPosition);
            Vector3 focusLocal = inverse * (focus - pose.PivotPosition);
            Vector3 topLocal = inverse * (top - pose.PivotPosition);
            float tanVertical = CalculateSafeVerticalSlope(pose.FieldOfView, settings.lockOnFitSafeFraction);
            float upper = Mathf.Max(feetLocal.y - tanVertical * feetLocal.z,
                Mathf.Max(focusLocal.y - tanVertical * focusLocal.z, topLocal.y - tanVertical * topLocal.z));
            float lower = Mathf.Max(-feetLocal.y - tanVertical * feetLocal.z,
                Mathf.Max(-focusLocal.y - tanVertical * focusLocal.z, -topLocal.y - tanVertical * topLocal.z));
            Vector3 up = pose.CameraRotation * Vector3.up;
            Vector3 forward = pose.CameraRotation * Vector3.forward;
            float balancedLift = (upper - lower) / Mathf.Max(0.01f, 2f * up.y);
            float necessaryLift = (upper - tanVertical * pose.Distance)
                / Mathf.Max(0.01f, up.y - tanVertical * forward.y);
            // 이미 안전 영역에 들어오면 중심을 맞추려고 움직이지 않는다.
            // 넘친 경우에도 현재 거리에서 필요한 상승량과 최소 거리 해 중 작은 값만 사용한다.
            return Mathf.Clamp(Mathf.Min(balancedLift, necessaryLift),
                0f, Mathf.Max(0f, settings.lockOnHeightFramingMaxLift));
        }

        private static float ComputeRequiredDistance(in CameraPose pose, Vector3 playerFeet,
            Vector3 focus, Vector3 top, float aspect, float safeFraction)
        {
            Quaternion inverse = Quaternion.Inverse(pose.CameraRotation);
            float tanVertical = CalculateSafeVerticalSlope(pose.FieldOfView, safeFraction);
            float tanHorizontal = tanVertical * Mathf.Max(0.01f, aspect);
            float required = RequiredFor(inverse * (playerFeet - pose.PivotPosition), tanVertical, tanHorizontal);
            required = Mathf.Max(required, RequiredFor(inverse * (focus - pose.PivotPosition), tanVertical, tanHorizontal));
            return Mathf.Max(required, RequiredFor(inverse * (top - pose.PivotPosition), tanVertical, tanHorizontal));
        }

        private static float RequiredFor(Vector3 local, float tanVertical, float tanHorizontal)
        {
            return Mathf.Max(Mathf.Abs(local.y) / tanVertical, Mathf.Abs(local.x) / tanHorizontal) - local.z;
        }

        private static float CalculateSafeVerticalSlope(float fieldOfView, float safeFraction)
        {
            float halfFov = Mathf.Clamp(fieldOfView, 1f, 179f) * 0.5f * Mathf.Deg2Rad;
            // 안전 비율은 각도가 아닌 실제 뷰포트 크기에 적용한다.
            return Mathf.Tan(halfFov) * Mathf.Clamp(safeFraction, 0.3f, 1f);
        }

        private void Reset()
        {
            _heightOffset = 0f;
            _heightVelocity = 0f;
            _distanceOffset = 0f;
            _distanceVelocity = 0f;
        }
    }
}
