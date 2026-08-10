using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 카메라 충돌 감지 + 거리 스무딩.
    /// SphereCast로 카메라 반경 전체를 고려해 경사면도 정확히 감지한다.
    /// </summary>
    public class CameraCollision
    {
        private readonly CameraSettings _settings;
        private readonly Transform _target;

        private float _collisionDistance;
        private float _collisionDistanceVel;
        private LayerMask _collisionLayers;
        private float _floorRescueLift;
        private float _floorRescueLiftVelocity;
        private bool _isCollisionActive;
        private float _occlusionTimer;
        private float _releaseTimer;
        private float _heldBlockedDistance;
        private readonly RaycastHit[] _floorHitBuffer = new RaycastHit[8];
        private readonly RaycastHit[] _floorLiftHitBuffer = new RaycastHit[8];

        private const float FLOOR_RESCUE_OCCLUDED_SMOOTH_TIME = 0.045f;

        public CameraCollision(CameraSettings settings, Transform target, LayerMask collisionLayers, float initialDistance)
        {
            _settings = settings;
            _target = target;
            _collisionLayers = collisionLayers;
            _collisionDistance = initialDistance;
        }

        /// <summary>
        /// 충돌을 고려한 실제 카메라 배치 거리를 반환한다.
        /// </summary>
        public float Evaluate(Vector3 pivot, Vector3 camDir, float desiredDistance)
        {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            float blockedDistance = GetRaycastDistance(pivot, camDir, desiredDistance);
            float targetDistance = ResolveTargetDistance(blockedDistance, desiredDistance, deltaTime);

            if (targetDistance < _collisionDistance)
            {
                // 정설(asymmetric damping): 당김은 즉시. 스무딩하면 그 몇 프레임 동안 카메라가
                // 벽 뒤에 남아 지오메트리 내부가 비친다(클리핑). 안전 우선이라 속도 제한도 두지 않는다.
                // 당김 타깃은 MultiProbe+법선 필터로 안정화한다.
                // 더 가까워질 때만 즉시 스냅하고, 확보 공간이 늘어날 때는 아래 복귀 감쇠를 사용한다.
                _collisionDistance = targetDistance;
                _collisionDistanceVel = 0f;
            }
            else
            {
                // 복귀(밖으로)는 부드럽게. 즉시 튀어나오면 거슬린다(jarring snap-back).
                float maxSpeed = _settings.collisionMaxDistanceChangeSpeed > 0f
                    ? _settings.collisionMaxDistanceChangeSpeed
                    : Mathf.Infinity;

                if (_collisionDistanceVel < 0f)
                    _collisionDistanceVel = 0f;

                _collisionDistance = _settings.collisionReturnSpeed > 0f
                    ? Mathf.SmoothDamp(_collisionDistance, targetDistance, ref _collisionDistanceVel, _settings.collisionReturnSpeed, maxSpeed, deltaTime)
                    : MoveDistanceImmediateOrLimited(_collisionDistance, targetDistance, maxSpeed, deltaTime);
            }

            return Mathf.Clamp(_collisionDistance, 0f, desiredDistance);
        }

        public void ResetDistance(float distance)
        {
            _collisionDistance = distance;
            _collisionDistanceVel = 0f;
            ResetFloorRescue();
            _isCollisionActive = false;
            _occlusionTimer = 0f;
            _releaseTimer = 0f;
            _heldBlockedDistance = distance;
        }

        public void ResetFloorRescue()
        {
            _floorRescueLift = 0f;
            _floorRescueLiftVelocity = 0f;
        }

        private float ResolveTargetDistance(float blockedDistance, float desiredDistance, float deltaTime)
        {
            const float STATE_HYSTERESIS = 0.01f;

            deltaTime = Mathf.Max(deltaTime, 0.0001f);
            float distanceDeadZone = Mathf.Max(_settings.collisionDistanceDeadZone, 0f);
            float releaseMargin = Mathf.Max(_settings.collisionReleaseHysteresis, 0f);
            float releaseThreshold = desiredDistance - releaseMargin;

            if (!_isCollisionActive)
            {
                // 해제 여유 구간의 얕은 접촉은 진입 후보로 취급하지 않는다.
                // 기존에는 같은 접촉이 hasBlockingHit와 canRelease를 동시에 만족해
                // 충돌 상태가 주기적으로 해제/재진입하며 거리 펄스를 만들 수 있었다.
                bool canEnter = blockedDistance < releaseThreshold - STATE_HYSTERESIS;
                if (!canEnter)
                {
                    _occlusionTimer = 0f;
                    _releaseTimer = 0f;
                    return desiredDistance;
                }

                _occlusionTimer += deltaTime;
                if (_occlusionTimer < Mathf.Max(_settings.collisionMinimumOcclusionTime, 0f))
                    return desiredDistance;

                _isCollisionActive = true;
                _occlusionTimer = 0f;
                _releaseTimer = 0f;
                _heldBlockedDistance = blockedDistance;
                return _heldBlockedDistance;
            }

            _occlusionTimer = 0f;

            if (blockedDistance < releaseThreshold)
            {
                float distanceDelta = blockedDistance - _heldBlockedDistance;
                if (distanceDelta < -distanceDeadZone)
                {
                    // 안전 공간이 줄어드는 방향은 즉시 반영하되, 충돌 여유 거리 안의
                    // 미세한 메시/프로브 편차는 무시한다.
                    _heldBlockedDistance = blockedDistance;
                    _releaseTimer = 0f;
                }
                else if (distanceDelta > distanceDeadZone)
                {
                    // 더 가까운 지점이 한 번 검출된 직후 다시 바깥으로 움직이지 않도록
                    // 잠시 유지한다. 유지 시간이 지난 뒤에는 경사면을 따라 연속 복귀한다.
                    _releaseTimer += deltaTime;
                    if (_releaseTimer >= Mathf.Max(_settings.collisionSmoothingHoldTime, 0f))
                        _heldBlockedDistance = blockedDistance;
                }
                else if (_releaseTimer >= Mathf.Max(_settings.collisionSmoothingHoldTime, 0f)
                         && distanceDelta > 0f)
                {
                    _heldBlockedDistance = blockedDistance;
                }

                return _heldBlockedDistance;
            }

            // 완전 미검출뿐 아니라 releaseMargin 안의 얕은 접촉도 같은 해제 후보로 다룬다.
            // 비활성 상태의 진입 임계치와 분리되어 같은 접촉에서 재진입하지 않는다.
            _releaseTimer += deltaTime;
            if (_releaseTimer < Mathf.Max(_settings.collisionSmoothingHoldTime, 0f))
                return _heldBlockedDistance;

            _isCollisionActive = false;
            _releaseTimer = 0f;
            return desiredDistance;
        }

        private static float MoveDistanceImmediateOrLimited(float current, float target, float maxSpeed, float deltaTime)
        {
            if (float.IsInfinity(maxSpeed))
                return target;

            return Mathf.MoveTowards(current, target, maxSpeed * deltaTime);
        }

        public void ApplyFloorRescue(Vector3 pivot, ref Vector3 cameraPosition, float deltaTime)
        {
            if (!_settings.enableFloorRescue)
            {
                ResetFloorRescue();
                return;
            }

            LayerMask layers = _settings.floorRescueLayerMask.value != 0
                ? _settings.floorRescueLayerMask
                : _collisionLayers;

            float clearance = Mathf.Max(_settings.groundClearance, _settings.cameraRadius);
            float pivotDrop = pivot.y - cameraPosition.y;
            float probeRadius = Mathf.Max(Mathf.Min(_settings.cameraRadius * 0.5f, clearance * 0.5f), 0.02f);
            float probeExtraHeight = pivotDrop > _settings.floorRescueDropThreshold
                ? Mathf.Max(_settings.collisionOffset, 0.05f)
                : 0.05f;
            float probeHeight = clearance + probeRadius + probeExtraHeight;

            // 월드 상공에서 내리는 레이는 낮은 천장/계단 윗면을 바닥으로 오인할 수 있다.
            // 카메라 바로 위에서 시작하는 짧은 국소 SphereCast만 사용하고, 위쪽을 향한 지면 법선을 검증한다.
            // 작은 반경은 계단/메시 삼각형 경계에서 단일 Ray 표본이 교대로 바뀌는 현상을 줄인다.
            Vector3 probeOrigin = cameraPosition + Vector3.up * probeHeight;
            float probeDistance = probeHeight + clearance + 0.5f;
            float requiredLift = 0f;
            float nearestGroundDistance = float.PositiveInfinity;
            int groundHitCount = Physics.SphereCastNonAlloc(
                probeOrigin,
                probeRadius,
                Vector3.down,
                _floorHitBuffer,
                probeDistance,
                layers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < groundHitCount; i++)
            {
                RaycastHit groundHit = _floorHitBuffer[i];
                if (groundHit.transform == _target || groundHit.transform.IsChildOf(_target))
                    continue;
                if (groundHit.normal.y < _settings.groundCollisionMinNormalY)
                    continue;
                // FloorRescue는 카메라 아래의 지면 여유만 복구한다. 카메라보다 위에 있는
                // 얇은 천장/발판의 윗면을 지면으로 채택하면 천장을 관통해 위로 올라간다.
                if (groundHit.point.y > cameraPosition.y + Mathf.Max(_settings.collisionSkinWidth, 0.01f))
                    continue;
                if (groundHit.distance >= nearestGroundDistance)
                    continue;

                nearestGroundDistance = groundHit.distance;
                float requiredMinY = groundHit.point.y + clearance;
                requiredLift = Mathf.Max(requiredMinY - cameraPosition.y, 0f);
            }

            float safeDeltaTime = Mathf.Max(deltaTime, 0.0001f);
            float liftDelta = requiredLift - _floorRescueLift;
            float immediateThreshold = Mathf.Max(clearance - _settings.cameraRadius, 0.02f);
            if (liftDelta > immediateThreshold)
            {
                // 큰 관통은 한 프레임 안에 해소한다. 일반적인 경사 추종은 앞선 충돌 거리 단계가 담당한다.
                _floorRescueLift = requiredLift;
                _floorRescueLiftVelocity = 0f;
            }
            else
            {
                float smoothTime = requiredLift > _floorRescueLift
                    ? FLOOR_RESCUE_OCCLUDED_SMOOTH_TIME
                    : Mathf.Max(_settings.floorRescueReturnSmoothTime, 0f);

                if (smoothTime > 0f)
                {
                    _floorRescueLift = Mathf.SmoothDamp(
                        _floorRescueLift,
                        requiredLift,
                        ref _floorRescueLiftVelocity,
                        smoothTime,
                        Mathf.Infinity,
                        safeDeltaTime);
                }
                else
                {
                    _floorRescueLift = requiredLift;
                    _floorRescueLiftVelocity = 0f;
                }
            }

            if (requiredLift <= 0f && _floorRescueLift <= 0.001f)
            {
                _floorRescueLift = 0f;
                _floorRescueLiftVelocity = 0f;
            }

            float constrainedLift = ConstrainFloorRescueLift(
                cameraPosition,
                Mathf.Max(_floorRescueLift, 0f),
                layers);
            if (constrainedLift < _floorRescueLift)
            {
                _floorRescueLift = constrainedLift;
                _floorRescueLiftVelocity = 0f;
            }

            cameraPosition.y += constrainedLift;
        }

        private float ConstrainFloorRescueLift(Vector3 cameraPosition, float requestedLift, LayerMask layers)
        {
            if (requestedLift <= 0f)
                return 0f;

            int hitCount = Physics.SphereCastNonAlloc(
                cameraPosition,
                Mathf.Max(_settings.cameraRadius, 0.01f),
                Vector3.up,
                _floorLiftHitBuffer,
                requestedLift,
                layers,
                QueryTriggerInteraction.Ignore);

            float allowedLift = requestedLift;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _floorLiftHitBuffer[i];
                if (hit.transform == _target || hit.transform.IsChildOf(_target))
                    continue;

                // 시작 지점에서 맞는 지면 윗면은 위쪽 이동을 막지 않는다.
                if (hit.normal.y >= _settings.groundCollisionMinNormalY)
                    continue;

                float safeDistance = Mathf.Max(hit.distance - _settings.collisionOffset, 0f);
                allowedLift = Mathf.Min(allowedLift, safeDistance);
            }

            return allowedLift;
        }

        private float GetRaycastDistance(Vector3 pivot, Vector3 camDir, float desiredDistance)
        {
            if (_settings.useMultiProbe && _settings.collisionProbeCount > 0)
                return GetMultiProbeDistance(pivot, camDir, desiredDistance);

            float r = _settings.cameraRadius;

            if (Physics.SphereCast(pivot, r, camDir, out RaycastHit hit, desiredDistance, _collisionLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == _target || hit.transform.IsChildOf(_target))
                    return desiredDistance;

                return Mathf.Max(hit.distance - _settings.collisionOffset, 0f);
            }

            return desiredDistance;
        }

        private float GetMultiProbeDistance(Vector3 pivot, Vector3 camDir, float desiredDistance)
        {
            Vector3 desiredPosition = pivot + camDir * desiredDistance;
            Vector3 axisDir = (desiredPosition - pivot).normalized;
            if (axisDir.sqrMagnitude < 0.0001f)
                return desiredDistance;

            Vector3 right = Vector3.Cross(axisDir, Vector3.up);
            if (right.sqrMagnitude < 0.0001f)
                right = Vector3.Cross(axisDir, Vector3.forward);
            right.Normalize();
            Vector3 upDir = Vector3.Cross(right, axisDir).normalized;

            float minReach = desiredDistance;
            TryLineProbe(pivot, desiredPosition, pivot, axisDir, desiredDistance, ref minReach);

            int probeCount = Mathf.Max(1, _settings.collisionProbeCount);
            float angleStep = 360f / probeCount;
            for (int i = 0; i < probeCount; i++)
            {
                float rad = Mathf.Deg2Rad * (i * angleStep);
                Vector3 offset = (Mathf.Cos(rad) * right + Mathf.Sin(rad) * upDir) * _settings.cameraRadius;
                TryLineProbe(pivot + offset, desiredPosition + offset, pivot, axisDir, desiredDistance, ref minReach);
            }

            return Mathf.Clamp(minReach, 0f, desiredDistance);
        }

        private void TryLineProbe(
            Vector3 probeStart,
            Vector3 probeEnd,
            Vector3 pivot,
            Vector3 axisDir,
            float desiredDistance,
            ref float minReach)
        {
            if (!Physics.Linecast(probeStart, probeEnd, out RaycastHit hit, _collisionLayers, QueryTriggerInteraction.Ignore))
                return;

            if (hit.transform == _target || hit.transform.IsChildOf(_target))
                return;

            float projectedReach = Vector3.Dot(hit.point - pivot, axisDir);
            projectedReach -= _settings.collisionSkinWidth;
            minReach = Mathf.Min(minReach, Mathf.Clamp(projectedReach, 0f, desiredDistance));
        }

    }
}
