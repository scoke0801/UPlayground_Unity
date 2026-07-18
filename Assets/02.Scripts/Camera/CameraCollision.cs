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
        private bool _hasFloorRescueY;
        private float _floorRescueY;
        private float _floorRescueYVelocity;
        private bool _isCollisionActive;
        private float _occlusionTimer;
        private float _clearTimer;
        private float _heldBlockedDistance;

        private const float FLOOR_RESCUE_SMOOTH_TIME = 0.06f;
        private const float FLOOR_RESCUE_IMMEDIATE_THRESHOLD = 0.35f;

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
            float blockedDistance = GetRaycastDistance(pivot, camDir, desiredDistance);
            float targetDistance = ResolveTargetDistance(blockedDistance, desiredDistance);
            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);

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
            _hasFloorRescueY = false;
            _floorRescueYVelocity = 0f;
            _isCollisionActive = false;
            _occlusionTimer = 0f;
            _clearTimer = 0f;
            _heldBlockedDistance = distance;
        }

        private float ResolveTargetDistance(float blockedDistance, float desiredDistance)
        {
            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            float enterEpsilon = 0.01f;
            float releaseMargin = Mathf.Max(_settings.collisionReleaseHysteresis, 0f);
            bool hasBlockingHit = blockedDistance < desiredDistance - enterEpsilon;
            bool canRelease = blockedDistance >= desiredDistance - releaseMargin;

            if (hasBlockingHit)
            {
                _occlusionTimer += deltaTime;

                if (!_isCollisionActive && _occlusionTimer < _settings.collisionMinimumOcclusionTime)
                    return desiredDistance;

                if (!_isCollisionActive)
                {
                    _isCollisionActive = true;
                    _heldBlockedDistance = blockedDistance;
                }
                else if (Mathf.Abs(blockedDistance - _heldBlockedDistance) > enterEpsilon)
                {
                    // 충돌 상태가 완전히 해제되기 전이라도 장애물이 멀어지면 확보된 거리만큼
                    // 복귀 타깃을 갱신한다. Evaluate의 비대칭 감쇠가 안쪽은 즉시,
                    // 바깥쪽은 collisionReturnSpeed로 부드럽게 처리한다.
                    _heldBlockedDistance = blockedDistance;
                }

                if (canRelease)
                {
                    _clearTimer += deltaTime;
                    if (_clearTimer >= Mathf.Max(_settings.collisionSmoothingHoldTime, 0f))
                    {
                        _isCollisionActive = false;
                        _clearTimer = 0f;
                        _collisionDistanceVel = 0f;
                        return desiredDistance;
                    }
                }
                else
                {
                    _clearTimer = 0f;
                }

                return _heldBlockedDistance;
            }

            _occlusionTimer = 0f;

            if (_isCollisionActive)
            {
                _clearTimer += deltaTime;

                if (_clearTimer < Mathf.Max(_settings.collisionSmoothingHoldTime, 0f))
                    return _heldBlockedDistance;

                _isCollisionActive = false;
                _clearTimer = 0f;
                _collisionDistanceVel = 0f;
            }

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
                return;

            LayerMask layers = _settings.floorRescueLayerMask.value != 0
                ? _settings.floorRescueLayerMask
                : _collisionLayers;

            float clearance = Mathf.Max(_settings.groundClearance, _settings.cameraRadius);
            float pivotDrop = pivot.y - cameraPosition.y;
            float requiredMinY = float.NegativeInfinity;

            if (pivotDrop > _settings.floorRescueDropThreshold)
            {
                Vector3 pivotRayOrigin = pivot + Vector3.up * 0.25f;
                float rayDistance = pivotDrop + clearance + 0.5f;
                if (Physics.Raycast(pivotRayOrigin, Vector3.down, out RaycastHit pivotGroundHit, rayDistance, layers, QueryTriggerInteraction.Ignore))
                {
                    float minY = pivotGroundHit.point.y + clearance;
                    requiredMinY = Mathf.Max(requiredMinY, minY);
                }
            }

            Vector3 cameraRayOrigin = cameraPosition + Vector3.up * (clearance + 0.25f);
            float cameraRayDistance = clearance + 0.5f;
            if (Physics.Raycast(cameraRayOrigin, Vector3.down, out RaycastHit cameraGroundHit, cameraRayDistance, layers, QueryTriggerInteraction.Ignore))
            {
                float groundClearanceY = cameraGroundHit.point.y + clearance;
                requiredMinY = Mathf.Max(requiredMinY, groundClearanceY);
            }

            if (float.IsNegativeInfinity(requiredMinY))
            {
                return;
            }

            if (!_hasFloorRescueY || requiredMinY - _floorRescueY > FLOOR_RESCUE_IMMEDIATE_THRESHOLD)
            {
                _floorRescueY = requiredMinY;
                _floorRescueYVelocity = 0f;
                _hasFloorRescueY = true;
            }
            else
            {
                _floorRescueY = Mathf.SmoothDamp(
                    _floorRescueY,
                    requiredMinY,
                    ref _floorRescueYVelocity,
                    FLOOR_RESCUE_SMOOTH_TIME,
                    Mathf.Infinity,
                    deltaTime);
            }

            if (cameraPosition.y < _floorRescueY)
                cameraPosition.y = _floorRescueY;
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

            float normalAlignment = Vector3.Dot(hit.normal, -axisDir);
            if (normalAlignment < _settings.minNormalAlignment)
                return;

            float projectedReach = Vector3.Dot(hit.point - pivot, axisDir);
            projectedReach -= _settings.collisionSkinWidth;
            minReach = Mathf.Min(minReach, Mathf.Clamp(projectedReach, 0f, desiredDistance));
        }

    }
}
