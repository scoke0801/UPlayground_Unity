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
        private readonly LayerMask _collisionLayers;
        private bool _isCollisionActive;
        private float _releaseTimer;
        private float _heldBlockedDistance;
        private readonly RaycastHit[] _sphereCastHitBuffer = new RaycastHit[16];
        private readonly Collider[] _overlapBuffer = new Collider[16];

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
            blockedDistance = ResolveOverlapDistance(pivot, camDir, blockedDistance);
            float targetDistance = ResolveTargetDistance(blockedDistance, desiredDistance, deltaTime);

            if (targetDistance < _collisionDistance)
            {
                // 정설(asymmetric damping): 당김은 즉시. 스무딩하면 그 몇 프레임 동안 카메라가
                // 벽 뒤에 남아 지오메트리 내부가 비친다(클리핑). 안전 우선이라 속도 제한도 두지 않는다.
                // 당김 타깃은 카메라 반경 SphereCast와 최종 겹침 백스톱으로 산출한다.
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
            _isCollisionActive = false;
            _releaseTimer = 0f;
            _heldBlockedDistance = distance;
        }

        private float ResolveTargetDistance(float blockedDistance, float desiredDistance, float deltaTime)
        {
            const float BLOCKING_EPSILON = 0.001f;

            deltaTime = Mathf.Max(deltaTime, 0.0001f);
            float distanceDeadZone = Mathf.Max(_settings.collisionDistanceDeadZone, 0f);
            bool hasBlockingHit = blockedDistance < desiredDistance - BLOCKING_EPSILON;

            if (!_isCollisionActive)
            {
                if (!hasBlockingHit)
                {
                    _releaseTimer = 0f;
                    return desiredDistance;
                }

                // 월드와 접촉한 프레임에 바로 암을 줄인다. 진입 지연은 그동안 카메라를
                // 지오메트리 안에 남겨 두므로 스프링암 충돌에는 적용하지 않는다.
                _isCollisionActive = true;
                _releaseTimer = 0f;
                _heldBlockedDistance = blockedDistance;
                return _heldBlockedDistance;
            }

            if (hasBlockingHit)
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

            // 완전 미검출이 일정 시간 유지된 뒤에만 충돌 상태를 해제한다.
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

        private float GetRaycastDistance(Vector3 pivot, Vector3 camDir, float desiredDistance)
        {
            if (desiredDistance <= 0f || camDir.sqrMagnitude <= 0.0001f)
                return 0f;

            float r = Mathf.Max(_settings.cameraRadius, 0.01f);

            int hitCount = Physics.SphereCastNonAlloc(
                pivot,
                r,
                camDir.normalized,
                _sphereCastHitBuffer,
                desiredDistance,
                _collisionLayers,
                QueryTriggerInteraction.Ignore);

            float nearestDistance = desiredDistance;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _sphereCastHitBuffer[i];
                if (_target != null &&
                    (hit.transform == _target || hit.transform.IsChildOf(_target)))
                    continue;

                nearestDistance = Mathf.Min(
                    nearestDistance,
                    Mathf.Max(hit.distance - _settings.collisionOffset, 0f));
            }

            return nearestDistance;
        }

        /// <summary>
        /// SphereCast가 시작 겹침을 보고하지 않는 경우를 위한 최종 안전망.
        /// 겹침을 임의 방향으로 밀어내지 않고 궤도 위의 안전 거리까지 암만 줄인다.
        /// </summary>
        private float ResolveOverlapDistance(Vector3 pivot, Vector3 camDir, float candidateDistance)
        {
            if (candidateDistance <= 0f || camDir.sqrMagnitude <= 0.0001f)
                return 0f;

            float radius = Mathf.Max(_settings.cameraRadius, 0.01f);
            Vector3 direction = camDir.normalized;

            // 피벗부터 겹친 예외 상황에서는 유효한 연속 안전 구간이 없으므로 암을 완전히 접는다.
            if (HasBlockingOverlap(pivot, radius))
                return 0f;

            Vector3 candidatePosition = pivot + direction * candidateDistance;
            if (!HasBlockingOverlap(candidatePosition, radius))
                return candidateDistance;

            float safeDistance = 0f;
            float blockedDistance = candidateDistance;
            for (int i = 0; i < 8; i++)
            {
                float probeDistance = (safeDistance + blockedDistance) * 0.5f;
                Vector3 probePosition = pivot + direction * probeDistance;
                if (HasBlockingOverlap(probePosition, radius))
                    blockedDistance = probeDistance;
                else
                    safeDistance = probeDistance;
            }

            return Mathf.Max(safeDistance - Mathf.Max(_settings.collisionSkinWidth, 0f), 0f);
        }

        private bool HasBlockingOverlap(Vector3 position, float radius)
        {
            int count = Physics.OverlapSphereNonAlloc(
                position,
                radius,
                _overlapBuffer,
                _collisionLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider overlap = _overlapBuffer[i];
                if (overlap == null)
                    continue;
                if (_target != null &&
                    (overlap.transform == _target || overlap.transform.IsChildOf(_target)))
                    continue;

                return true;
            }

            return false;
        }
    }
}
