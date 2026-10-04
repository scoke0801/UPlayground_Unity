using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>암이 접힌 락온 구도에서 두 대상의 가독성과 지형 여유를 함께 확보한다.</summary>
    public sealed class CameraObstructionFraming
    {
        private const int YawSteps = 6;
        private const int PitchSteps = 2;
        private bool _isActive;
        private float _anchorYaw;
        private Vector2 _selectedAngles;
        private Vector2 _velocity;
        private float _searchTimer;

        /// <summary>대상·모드 변경 시 이전 벽의 회피 방향을 제거한다.</summary>
        public void Reset()
        {
            _isActive = false;
            _velocity = Vector2.zero;
            _searchTimer = 0f;
        }

        /// <summary>좁은 공간에서만 회전을 담당하며 일반 데드존 추적과 동시에 회전하지 않는다.</summary>
        public bool TryTrack(ref CameraPose pose, CameraContext context, float desiredDistance, float deltaTime)
        {
            CameraSettings settings = context.Settings;
            if (!settings.enableLockOnObstructionFraming || context.Collision == null)
            {
                Reset();
                return false;
            }

            float available = context.Collision.GetAvailableDistance(pose.PivotPosition,
                pose.CameraRotation * Vector3.back, desiredDistance);
            float minimum = Mathf.Min(desiredDistance, Mathf.Max(0.1f, settings.lockOnObstructionMinDistance));
            float stability = Mathf.Max(0.01f, settings.lockOnObstructionStability);
            float releaseDistance = Mathf.Min(desiredDistance, minimum + stability);
            bool canFramePair = CalculateRequiredDistance(pose, context) <= Mathf.Min(available, pose.Distance);
            bool hasComfortablePitch = pose.Pitch <= settings.lockOnObstructionMaxPitch + 0.001f;
            bool hasClearance = available >= (_isActive ? releaseDistance : minimum) - 0.001f;
            bool isCompressed = available < desiredDistance - stability;
            if ((!_isActive && (!isCompressed || (hasComfortablePitch && (canFramePair || hasClearance))))
                || (hasClearance && canFramePair && hasComfortablePitch))
            {
                Reset();
                return false;
            }

            if (!_isActive)
            {
                _isActive = true;
                _anchorYaw = pose.Yaw;
                _selectedAngles = new Vector2(pose.Yaw, pose.Pitch);
            }

            float minPitch = Mathf.Max(0f, settings.lockOnPitchLimits.x);
            float maxPitch = Mathf.Max(minPitch, Mathf.Min(settings.lockOnPitchLimits.y,
                settings.lockOnObstructionMaxPitch));
            float preferredPitch = Mathf.Clamp(settings.lockOnPreferredPitch, minPitch, maxPitch);
            _selectedAngles.y = Mathf.Clamp(_selectedAngles.y, minPitch, maxPitch);
            Vector2 bestAngles = _selectedAngles;
            float bestScore = EvaluateCandidate(pose, bestAngles, context, desiredDistance, minimum,
                out bool canFrameBest);
            _searchTimer -= Mathf.Max(0f, deltaTime);
            // 고정된 진입 방향을 기준으로 탐색해야 매 프레임 탐색 범위가 누적되어 한 바퀴 돌지 않는다.
            for (int yawStep = -YawSteps; _searchTimer <= 0f && yawStep <= YawSteps; yawStep++)
            {
                float yaw = _anchorYaw + settings.lockOnObstructionYawRange * yawStep / YawSteps;
                for (int pitchStep = 0; pitchStep <= PitchSteps; pitchStep++)
                {
                    float pitch = Mathf.Lerp(preferredPitch, maxPitch, (float)pitchStep / PitchSteps);
                    Vector2 angles = new Vector2(yaw, pitch);
                    float score = EvaluateCandidate(pose, angles, context, desiredDistance, minimum,
                        out bool canFrameCandidate);
                    // 화면 안에 들어오는 해가 있으면 미세한 회전 억제보다 두 대상의 가독성을 우선한다.
                    if ((canFrameBest && !canFrameCandidate)
                        || (canFrameBest == canFrameCandidate && score <= bestScore + stability))
                        continue;
                    bestScore = score;
                    bestAngles = angles;
                    canFrameBest = canFrameCandidate;
                }
            }
            if (_searchTimer <= 0f)
                _searchTimer = Mathf.Max(0.01f, settings.lockOnObstructionSearchInterval);

            if (float.IsNegativeInfinity(bestScore))
            {
                // 탈출 구도가 없는 모서리에서도 탐색 간격은 유지한다.
                _velocity = Vector2.zero;
                return false;
            }

            _selectedAngles = bestAngles;
            if (deltaTime > 0f)
            {
                pose.Yaw += IntegrateAngle(Mathf.DeltaAngle(pose.Yaw, bestAngles.x),
                    settings.lockOnMaxAngularSpeed.x, settings.lockOnAngularAcceleration.x,
                    settings.lockOnResponseTime.x, deltaTime, ref _velocity.x);
                pose.Pitch += IntegrateAngle(bestAngles.y - pose.Pitch,
                    settings.lockOnMaxAngularSpeed.y, settings.lockOnAngularAcceleration.y,
                    settings.lockOnResponseTime.y, deltaTime, ref _velocity.y);
            }
            pose.CameraRotation = Quaternion.Euler(pose.Pitch, pose.Yaw, 0f);
            pose.CameraPosition = pose.PivotPosition + pose.CameraRotation * Vector3.back * pose.Distance;
            return true;
        }

        private float EvaluateCandidate(in CameraPose source, Vector2 angles, CameraContext context,
            float desiredDistance, float minimumDistance, out bool canFramePair)
        {
            CameraSettings settings = context.Settings;
            CameraPose candidate = source;
            candidate.CameraRotation = Quaternion.Euler(angles.y, angles.x, 0f);
            candidate.Distance = context.Collision.GetAvailableDistance(candidate.PivotPosition,
                candidate.CameraRotation * Vector3.back, desiredDistance);
            candidate.CameraPosition = candidate.PivotPosition
                + candidate.CameraRotation * Vector3.back * candidate.Distance;
            Vector3 playerFocus = context.Target.position + context.State.CameraOffset;
            Vector3 focus = context.LockOn.FocusPosition;
            float required = CalculateRequiredDistance(candidate, context);
            canFramePair = false;
            // 벽 반대편의 빈 공간은 두 대상까지 시야가 이어지는 경우에만 사용한다.
            if (!HasClearView(candidate.CameraPosition, playerFocus, context.Collision)
                || !HasClearView(candidate.CameraPosition, focus, context.Collision))
                return float.NegativeInfinity;

            canFramePair = required <= candidate.Distance;

            float angleCost = Mathf.Abs(Mathf.DeltaAngle(_anchorYaw, angles.x))
                / Mathf.Max(1f, settings.lockOnObstructionYawRange);
            angleCost += Mathf.Abs(angles.y - settings.lockOnPreferredPitch)
                / Mathf.Max(1f, settings.lockOnObstructionMaxPitch);
            return Mathf.Min(candidate.Distance, minimumDistance)
                - Mathf.Max(0f, required - candidate.Distance)
                - angleCost * settings.lockOnObstructionStability;
        }

        private static bool HasClearView(Vector3 camera, Vector3 focus, CameraCollision collision)
        {
            Vector3 offset = focus - camera;
            float distance = offset.magnitude;
            return distance <= 0.001f
                || collision.GetAvailableDistance(camera, offset / distance, distance) >= distance - 0.001f;
        }

        private static float RequiredDistance(in CameraPose pose, Vector3 point, float aspect, float safeFraction)
        {
            Vector3 local = Quaternion.Inverse(pose.CameraRotation) * (point - pose.PivotPosition);
            float slope = Mathf.Tan(Mathf.Clamp(pose.FieldOfView, 1f, 179f) * Mathf.Deg2Rad * 0.5f)
                * Mathf.Clamp(safeFraction, 0.3f, 1f);
            return Mathf.Max(Mathf.Abs(local.y) / slope,
                Mathf.Abs(local.x) / (slope * Mathf.Max(0.01f, aspect))) - local.z;
        }

        private static float CalculateRequiredDistance(in CameraPose pose, CameraContext context)
        {
            float aspect = context.MainCamera.aspect;
            float fraction = context.Settings.lockOnFitSafeFraction;
            float required = RequiredDistance(pose, context.Target.position, aspect, fraction);
            required = Mathf.Max(required, RequiredDistance(pose,
                context.Target.position + context.State.CameraOffset, aspect, fraction));
            return Mathf.Max(required, RequiredDistance(pose, context.LockOn.FocusPosition, aspect, fraction));
        }

        private static float IntegrateAngle(float error, float maxSpeed, float acceleration,
            float responseTime, float deltaTime, ref float velocity)
        {
            if (Mathf.Abs(error) < 0.001f)
            {
                velocity = 0f;
                return error;
            }
            if (velocity * error <= 0f)
                velocity = 0f;
            float speed = Mathf.Clamp(error / Mathf.Max(0.01f, responseTime), -maxSpeed, maxSpeed);
            velocity = Mathf.MoveTowards(velocity, speed, Mathf.Max(0f, acceleration) * deltaTime);
            return Mathf.Sign(error) * Mathf.Min(Mathf.Abs(velocity * deltaTime), Mathf.Abs(error));
        }
    }
}
