using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>연출을 제외한 화면 좌표로 대상을 데드존 경계까지만 추적한다.</summary>
    public sealed class CameraDeadZoneTracker
    {
        private Vector2 _angularVelocity;
        private int _horizontalSide;
        private int _verticalSide;
        private float _lastYawDirection = 1f;
        private bool _hasYawDirection;
        private bool _isNearTarget;
        private bool _hasPreviousTargetOffset;
        private Vector2 _previousTargetOffset;
        private float _crossingRecovery;

        /// <summary>대상 전환이나 카메라 모드 변경 시 이전 추적 관성을 제거한다.</summary>
        public void Reset()
        {
            _angularVelocity = Vector2.zero;
            _horizontalSide = 0;
            _verticalSide = 0;
            _lastYawDirection = 1f;
            _hasYawDirection = false;
            _isNearTarget = false;
            _hasPreviousTargetOffset = false;
            _crossingRecovery = 0f;
        }

        /// <summary>피벗·거리·FOV는 유지하고 데드존을 벗어난 축의 회전만 보정한다.</summary>
        public void Track(ref CameraPose pose, Vector3 focus, CameraSettings settings, float aspect, float deltaTime,
            Vector3? targetOffset = null)
        {
            if (deltaTime <= 0f || settings == null)
                return;

            // 포커스/피벗 보간이 실제 교차 시점을 늦추지 않도록 런타임에서는 액터 간 원본 오프셋을 받는다.
            UpdateProximity(targetOffset ?? focus - pose.PivotPosition, settings);
            // 가속도 적분을 작은 구간으로 나눠 30/60/120fps의 응답 차이를 줄인다.
            int steps = Mathf.Clamp(Mathf.CeilToInt(deltaTime * 120f), 1, 32);
            float stepTime = deltaTime / steps;
            for (int i = 0; i < steps; i++)
                TrackStep(ref pose, focus, settings, aspect, stepTime);
        }

        /// <summary>렌더 카메라를 참조하지 않고 안정된 포즈의 뷰포트 좌표를 계산한다.</summary>
        public static bool TryProject(in CameraPose pose, Vector3 focus, float aspect, out Vector2 viewport)
        {
            Vector3 local = Quaternion.Inverse(pose.CameraRotation) * (focus - pose.CameraPosition);
            viewport = Vector2.zero;
            if (local.z <= 0.001f)
                return false;

            float tanVertical = Mathf.Tan(Mathf.Clamp(pose.FieldOfView, 1f, 179f) * 0.5f * Mathf.Deg2Rad);
            viewport = new Vector2(
                0.5f + local.x / (2f * local.z * tanVertical * Mathf.Max(0.01f, aspect)),
                0.5f + local.y / (2f * local.z * tanVertical));
            return true;
        }

        private void TrackStep(ref CameraPose pose, Vector3 focus, CameraSettings settings, float aspect, float deltaTime)
        {
            if (!_isNearTarget)
                _crossingRecovery = Mathf.MoveTowards(_crossingRecovery, 0f,
                    deltaTime / Mathf.Max(0.01f, settings.lockOnCrossingRecoveryTime));
            Vector3 local = Quaternion.Inverse(pose.CameraRotation) * (focus - pose.CameraPosition);
            Vector2 error;
            float edgeStrength;
            if (TryProject(pose, focus, aspect, out Vector2 viewport))
            {
                Rect zone = GetSafeDeadZone(settings.lockOnDeadZone);
                float nearMargin = Mathf.Clamp(settings.lockOnCrossingScreenMargin, 0.01f, 0.45f);
                zone.xMin = Mathf.Lerp(zone.xMin, Mathf.Min(zone.xMin, nearMargin), _crossingRecovery);
                zone.xMax = Mathf.Lerp(zone.xMax, Mathf.Max(zone.xMax, 1f - nearMargin), _crossingRecovery);
                float horizontal = GetBoundary(viewport.x, zone.xMin, zone.xMax,
                    settings.lockOnDeadZoneHysteresis, ref _horizontalSide);
                float vertical = GetBoundary(viewport.y, zone.yMin, zone.yMax,
                    settings.lockOnDeadZoneHysteresis, ref _verticalSide);
                float tanVertical = Mathf.Tan(Mathf.Clamp(pose.FieldOfView, 1f, 179f) * 0.5f * Mathf.Deg2Rad);
                error = new Vector2(
                    _horizontalSide == 0 ? 0f : Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg
                        - Mathf.Atan((horizontal - 0.5f) * 2f * tanVertical * aspect) * Mathf.Rad2Deg,
                    _verticalSide == 0 ? 0f : CalculatePitchCorrection(pose, local,
                        (vertical - 0.5f) * 2f * tanVertical, settings.lockOnPitchLimits));
                float margin = Mathf.Clamp(settings.lockOnScreenEdgeMargin, 0.01f, 0.25f);
                float edgeDistance = Mathf.Min(Mathf.Min(viewport.x, 1f - viewport.x),
                    Mathf.Min(viewport.y, 1f - viewport.y));
                edgeStrength = 1f - Mathf.Clamp01(edgeDistance / margin);
            }
            else
            {
                // 뒤쪽 대상은 투영 좌표가 반전된다. 월드 방향으로 복귀하되 180도 부근의 좌우 진동을 막는다.
                Vector3 direction = focus - pose.PivotPosition;
                float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float yawError = Mathf.DeltaAngle(pose.Yaw, yaw);
                float rearBoundary = 180f - Mathf.Clamp(settings.lockOnRearDirectionHysteresis, 0f, 89f);
                if (_hasYawDirection && Mathf.Abs(yawError) >= rearBoundary
                    && Mathf.Sign(yawError) != _lastYawDirection)
                    yawError += 360f * _lastYawDirection;
                float pitch = -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
                error = new Vector2(yawError, Mathf.DeltaAngle(pose.Pitch, pitch));
                edgeStrength = 1f;
            }

            Vector3 fromPivot = focus - pose.PivotPosition;
            float poleRadius = Mathf.Max(0.01f, settings.lockOnYawSingularityRadius);
            if (fromPivot.x * fromPivot.x + fromPivot.z * fromPivot.z < poleRadius * poleRadius)
                error.x = 0f;
            if (Mathf.Abs(error.x) > 0.001f)
            {
                _lastYawDirection = Mathf.Sign(error.x);
                _hasYawDirection = true;
            }

            float strength = Mathf.Lerp(1f, Mathf.Max(1f, settings.lockOnEdgeResponseMultiplier), edgeStrength);
            float yawSpeedScale = Mathf.Lerp(1f,
                Mathf.Clamp(settings.lockOnCrossingYawSpeedScale, 0.05f, 1f), _crossingRecovery);
            _angularVelocity.x = Mathf.Clamp(_angularVelocity.x,
                -Mathf.Max(0f, settings.lockOnMaxAngularSpeed.x) * yawSpeedScale,
                Mathf.Max(0f, settings.lockOnMaxAngularSpeed.x) * yawSpeedScale);
            pose.Yaw += IntegrateAxis(error.x, settings.lockOnResponseTime.x / strength,
                settings.lockOnMaxAngularSpeed.x * yawSpeedScale, settings.lockOnAngularAcceleration.x,
                deltaTime, ref _angularVelocity.x);
            float nextPitch = pose.Pitch + IntegrateAxis(error.y, settings.lockOnResponseTime.y / strength,
                settings.lockOnMaxAngularSpeed.y, settings.lockOnAngularAcceleration.y,
                deltaTime, ref _angularVelocity.y);
            float minPitch = Mathf.Clamp(settings.lockOnPitchLimits.x, -89f, 89f);
            float maxPitch = Mathf.Clamp(settings.lockOnPitchLimits.y, minPitch, 89f);
            pose.Pitch = Mathf.Clamp(nextPitch, minPitch, maxPitch);
            if (pose.Pitch != nextPitch)
                _angularVelocity.y = 0f;
            pose.Yaw = Mathf.DeltaAngle(0f, pose.Yaw);
            pose.CameraRotation = Quaternion.Euler(pose.Pitch, pose.Yaw, 0f);
            pose.CameraPosition = pose.PivotPosition + pose.CameraRotation * Vector3.back * pose.Distance;
        }

        private static float CalculatePitchCorrection(in CameraPose pose, Vector3 local,
            float boundarySlope, Vector2 pitchLimits)
        {
            // 카메라는 피벗 주위를 공전한다. 카메라 제자리 회전의 화면 각도 차이를 쓰면
            // 피벗 뒤의 근접 대상에서는 보정 방향이 반전되어 피치가 제한각까지 발산한다.
            float pivotDepth = local.z - pose.Distance;
            float cosineCoefficient = local.y - boundarySlope * pivotDepth;
            float sineCoefficient = pivotDepth + boundarySlope * local.y;
            float radius = new Vector2(cosineCoefficient, sineCoefficient).magnitude;
            if (radius <= 0.0001f)
                return 0f;

            // 회전 후 local.y = boundarySlope * local.z를 만족하는 두 궤도 해 중
            // 화면 오차를 줄이는 가장 가까운 해를 고른다. 도달 불가능한 경계는 접점으로 제한한다.
            float phase = Mathf.Atan2(cosineCoefficient, sineCoefficient);
            float arc = Mathf.Asin(Mathf.Clamp(boundarySlope * pose.Distance / radius, -1f, 1f));
            float first = EvaluatePitchCandidate((arc - phase) * Mathf.Rad2Deg,
                pose, local, boundarySlope, pitchLimits);
            float second = EvaluatePitchCandidate((Mathf.PI - arc - phase) * Mathf.Rad2Deg,
                pose, local, boundarySlope, pitchLimits);
            float correction = Mathf.Abs(first) <= Mathf.Abs(second) ? first : second;
            return float.IsInfinity(correction) ? 0f : correction;
        }

        private static float EvaluatePitchCandidate(float correction, in CameraPose pose, Vector3 local,
            float boundarySlope, Vector2 pitchLimits)
        {
            float minPitch = Mathf.Clamp(pitchLimits.x, -89f, 89f);
            float maxPitch = Mathf.Clamp(pitchLimits.y, minPitch, 89f);
            correction = Mathf.Clamp(pose.Pitch + Mathf.DeltaAngle(0f, correction), minPitch, maxPitch) - pose.Pitch;
            float sine = Mathf.Sin(correction * Mathf.Deg2Rad);
            float cosine = Mathf.Cos(correction * Mathf.Deg2Rad);
            float pivotDepth = local.z - pose.Distance;
            float height = local.y * cosine + pivotDepth * sine;
            float depth = pose.Distance + pivotDepth * cosine - local.y * sine;
            if (depth <= 0.001f)
                return float.PositiveInfinity;

            float boundaryAngle = Mathf.Atan(boundarySlope);
            float currentError = Mathf.Abs(Mathf.Atan2(local.y, local.z) - boundaryAngle);
            float candidateError = Mathf.Abs(Mathf.Atan2(height, depth) - boundaryAngle);
            return candidateError < currentError ? correction : float.PositiveInfinity;
        }

        private void UpdateProximity(Vector3 targetOffset, CameraSettings settings)
        {
            Vector2 planarOffset = new Vector2(targetOffset.x, targetOffset.z);
            if (!settings.enableLockOnCrossingProtection)
            {
                _isNearTarget = false;
                _crossingRecovery = 0f;
                _hasPreviousTargetOffset = false;
                return;
            }

            float enterDistance = Mathf.Max(0f, settings.lockOnCrossingEnterDistance);
            float exitDistance = Mathf.Max(enterDistance, settings.lockOnCrossingExitDistance);
            float threshold = _isNearTarget ? exitDistance : enterDistance;
            _isNearTarget = planarOffset.sqrMagnitude <= threshold * threshold;

            // 한 프레임에 근접 영역 전체를 통과하는 대시도 선분의 최근접점으로 잡는다.
            bool hasCrossed = false;
            if (_hasPreviousTargetOffset && Vector2.Dot(_previousTargetOffset, planarOffset) < 0f)
            {
                Vector2 movement = planarOffset - _previousTargetOffset;
                float fraction = Mathf.Clamp01(-Vector2.Dot(_previousTargetOffset, movement)
                    / Mathf.Max(0.0001f, movement.sqrMagnitude));
                Vector2 nearest = _previousTargetOffset + movement * fraction;
                hasCrossed = nearest.sqrMagnitude <= enterDistance * enterDistance;
            }
            if (_isNearTarget || hasCrossed)
                _crossingRecovery = 1f;
            _previousTargetOffset = planarOffset;
            _hasPreviousTargetOffset = true;
        }

        private static Rect GetSafeDeadZone(Rect zone)
        {
            float left = Mathf.Clamp(zone.xMin, 0f, 0.98f);
            float bottom = Mathf.Clamp(zone.yMin, 0f, 0.98f);
            return Rect.MinMaxRect(left, bottom, Mathf.Clamp(zone.xMax, left + 0.01f, 1f),
                Mathf.Clamp(zone.yMax, bottom + 0.01f, 1f));
        }

        private static float GetBoundary(float value, float min, float max, float hysteresis, ref int side)
        {
            float inset = Mathf.Clamp(hysteresis, 0f, (max - min) * 0.49f);
            if (value < min) side = -1;
            else if (value > max) side = 1;
            else if ((side < 0 && value >= min + inset - 0.0001f)
                     || (side > 0 && value <= max - inset + 0.0001f)) side = 0;
            return side < 0 ? min + inset : max - inset;
        }

        private static float IntegrateAxis(float error, float responseTime, float maxSpeed,
            float acceleration, float deltaTime, ref float velocity)
        {
            if (Mathf.Abs(error) <= 0.0001f)
            {
                velocity = 0f;
                return 0f;
            }

            if (velocity * error < 0f)
                velocity = 0f;
            float speedLimit = Mathf.Max(0f, maxSpeed);
            float desiredSpeed = Mathf.Clamp(error / Mathf.Max(0.01f, responseTime), -speedLimit, speedLimit);
            velocity = Mathf.MoveTowards(velocity, desiredSpeed, Mathf.Max(0f, acceleration) * deltaTime);
            return Mathf.Sign(error) * Mathf.Min(Mathf.Abs(velocity * deltaTime), Mathf.Abs(error));
        }
    }
}
