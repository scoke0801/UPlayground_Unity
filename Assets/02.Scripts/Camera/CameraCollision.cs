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
        private bool _isColliding;
        private float _collisionSustainedSec;

        // 당김은 즉시, 복귀는 부드럽게
        private const float PULL_SPEED = 0f;
        private const float COLLISION_EPSILON = 0.03f;

        public bool IsColliding => _isColliding;
        public float CollisionSustainedSec => _collisionSustainedSec;

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
            UpdateCollisionTelemetry(blockedDistance, desiredDistance);

            float smoothTime = blockedDistance < _collisionDistance
                ? PULL_SPEED
                : _settings.collisionReturnSpeed;

            _collisionDistance = smoothTime > 0f
                ? Mathf.SmoothDamp(_collisionDistance, blockedDistance, ref _collisionDistanceVel, smoothTime)
                : blockedDistance;

            return _collisionDistance;
        }

        public void ResetDistance(float distance)
        {
            _collisionDistance = distance;
            _collisionDistanceVel = 0f;
            _isColliding = false;
            _collisionSustainedSec = 0f;
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

            if (pivotDrop > _settings.floorRescueDropThreshold)
            {
                Vector3 pivotRayOrigin = pivot + Vector3.up * 0.25f;
                float rayDistance = pivotDrop + clearance + 0.5f;
                if (Physics.Raycast(pivotRayOrigin, Vector3.down, out RaycastHit pivotGroundHit, rayDistance, layers))
                {
                    float minY = pivotGroundHit.point.y + clearance;
                    if (cameraPosition.y < minY)
                        cameraPosition.y = minY;
                }
            }

            Vector3 cameraRayOrigin = cameraPosition + Vector3.up * (clearance + 0.25f);
            float cameraRayDistance = clearance + 0.5f;
            if (!Physics.Raycast(cameraRayOrigin, Vector3.down, out RaycastHit cameraGroundHit, cameraRayDistance, layers))
                return;

            float groundClearanceY = cameraGroundHit.point.y + clearance;
            if (cameraPosition.y < groundClearanceY)
                cameraPosition.y = groundClearanceY;
        }

        private float GetRaycastDistance(Vector3 pivot, Vector3 camDir, float desiredDistance)
        {
            if (_settings.useMultiProbe && _settings.collisionProbeCount > 0)
                return GetMultiProbeDistance(pivot, camDir, desiredDistance);

            float r = _settings.cameraRadius;

            if (Physics.SphereCast(pivot, r, camDir, out RaycastHit hit, desiredDistance, _collisionLayers))
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
            if (!Physics.Linecast(probeStart, probeEnd, out RaycastHit hit, _collisionLayers))
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

        private void UpdateCollisionTelemetry(float blockedDistance, float desiredDistance)
        {
            _isColliding = blockedDistance < desiredDistance - COLLISION_EPSILON;
            _collisionSustainedSec = _isColliding
                ? _collisionSustainedSec + Time.deltaTime
                : 0f;
        }
    }
}
