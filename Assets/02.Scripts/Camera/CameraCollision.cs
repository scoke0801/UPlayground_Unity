using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>시선과 피벗 추적을 유지하며 장애물 앞까지 카메라 암을 접는다.</summary>
    public sealed class CameraCollision : System.IDisposable
    {
        private const float ContactEpsilon = 0.001f;
        private const int QueryCapacity = 32;
        private readonly CameraSettings _settings;
        private readonly Transform _target;
        private readonly LayerMask _collisionLayers;
        private readonly RaycastHit[] _hits = new RaycastHit[QueryCapacity];
        private readonly Collider[] _overlaps = new Collider[QueryCapacity];
        private readonly SphereCollider _penetrationProbe;
        private float _distance;
        private float _returnVelocity;
        private float _returnDelay;

        /// <summary>마지막 충돌 계산에서 확보한 카메라 암 길이.</summary>
        public float CurrentDistance => _distance;

        /// <summary>충돌 계산에 필요한 형상과 거리 상태를 초기화한다.</summary>
        public CameraCollision(CameraSettings settings, Transform target, LayerMask collisionLayers, float initialDistance)
        {
            _settings = settings;
            _target = target;
            _collisionLayers = collisionLayers;
            ResetDistance(initialDistance);
            var probe = new GameObject("카메라 겹침 검사") { hideFlags = HideFlags.HideAndDontSave };
            _penetrationProbe = probe.AddComponent<SphereCollider>();
            _penetrationProbe.enabled = false;
            _penetrationProbe.isTrigger = true;
        }

        /// <summary>벽에서는 즉시 암을 접고, 연속으로 공간이 확보된 뒤에만 부드럽게 복귀한다.</summary>
        public float Evaluate(Vector3 pivot, Vector3 direction, float desiredDistance)
            => Evaluate(pivot, direction, desiredDistance, Time.deltaTime);

        /// <summary>지정한 프레임 시간으로 충돌 수축과 거리 복귀를 계산한다.</summary>
        public float Evaluate(Vector3 pivot, Vector3 direction, float desiredDistance, float deltaTime)
        {
            float available = GetAvailableDistance(pivot, direction, Mathf.Max(0f, desiredDistance));
            deltaTime = Mathf.Max(0f, deltaTime);
            if (available < _distance)
            {
                _distance = available;
                _returnVelocity = 0f;
                _returnDelay = Mathf.Max(0f, _settings.collisionSmoothingHoldTime);
                return _distance;
            }

            // 작은 틈이 반복되는 메시 모서리에서는 복귀 시간을 누적하지 않는다.
            if (available - _distance <= Mathf.Max(0f, _settings.collisionDistanceDeadZone))
            {
                _returnVelocity = 0f;
                _returnDelay = Mathf.Max(0f, _settings.collisionSmoothingHoldTime);
                return _distance;
            }
            if (_returnDelay > 0f)
            {
                float heldTime = Mathf.Min(_returnDelay, deltaTime);
                _returnDelay -= heldTime;
                deltaTime -= heldTime;
            }
            if (deltaTime <= 0f)
                return _distance;

            _distance = _settings.collisionReturnSpeed <= 0f ? available : Mathf.SmoothDamp(
                _distance, available, ref _returnVelocity, _settings.collisionReturnSpeed,
                Mathf.Infinity, deltaTime);
            return _distance;
        }

        /// <summary>연출 이동 등에서 충돌 복귀 상태를 바꾸지 않고 안전 거리를 조회한다.</summary>
        public float GetAvailableDistance(Vector3 pivot, Vector3 direction, float desiredDistance)
        {
            if (desiredDistance <= 0f || direction.sqrMagnitude <= ContactEpsilon * ContactEpsilon)
                return 0f;
            direction.Normalize();
            float radius = Mathf.Max(ContactEpsilon, _settings.cameraRadius);
            if (HasBlockingOverlap(pivot, radius))
                return 0f;

            float distance = CastDistance(pivot, direction, desiredDistance, radius,
                Mathf.Max(0f, _settings.collisionOffset));
            if (!HasBlockingOverlap(pivot + direction * distance, radius))
                return distance;

            // SphereCast가 시작 겹침이나 수치 오차로 놓친 끝점만 궤도 위에서 복구한다.
            float safe = 0f;
            for (int i = 0; i < 8; i++)
            {
                float middle = (safe + distance) * 0.5f;
                if (HasBlockingOverlap(pivot + direction * middle, radius)) distance = middle;
                else safe = middle;
            }
            return Mathf.Max(0f, safe - Mathf.Max(0f, _settings.collisionSkinWidth));
        }

        /// <summary>어깨·연출 오프셋이 플레이어와 벽 사이의 안전 경로를 벗어나지 않게 제한한다.</summary>
        public Vector3 ConstrainPivotPosition(Vector3 origin, Vector3 desiredPosition)
        {
            float radius = Mathf.Max(ContactEpsilon, _settings.cameraRadius)
                + Mathf.Max(0f, _settings.collisionSkinWidth);
            origin = ResolvePivotOverlap(origin, radius);
            Vector3 offset = desiredPosition - origin;
            float distance = offset.magnitude;
            if (distance <= ContactEpsilon || HasBlockingOverlap(origin, radius))
                return origin;
            Vector3 direction = offset / distance;
            return origin + direction * CastDistance(origin, direction, distance, radius, ContactEpsilon);
        }

        /// <summary>씬 전환과 대상 교체 시 충돌 복귀 관성을 제거한다.</summary>
        public void ResetDistance(float distance)
        {
            _distance = Mathf.Max(0f, distance);
            _returnVelocity = 0f;
            _returnDelay = 0f;
        }

        /// <summary>카메라 수명 종료 시 겹침 계산용 형상을 해제한다.</summary>
        public void Dispose()
        {
            if (_penetrationProbe == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(_penetrationProbe.gameObject);
            else UnityEngine.Object.DestroyImmediate(_penetrationProbe.gameObject);
        }

        private float CastDistance(Vector3 origin, Vector3 direction, float distance, float radius, float clearance)
        {
            int count = Physics.SphereCastNonAlloc(origin, radius, direction, _hits,
                distance + clearance, _collisionLayers, QueryTriggerInteraction.Ignore);
            // NonAlloc은 거리 순서가 보장되지 않는다. 포화되면 누락된 가까운 벽을 통과하지 않는다.
            if (count == _hits.Length) return 0f;
            for (int i = 0; i < count; i++)
                if (IsObstacle(_hits[i].collider))
                    distance = Mathf.Min(distance, Mathf.Max(0f, _hits[i].distance - clearance));
            return distance;
        }

        private bool HasBlockingOverlap(Vector3 position, float radius)
        {
            int count = Physics.OverlapSphereNonAlloc(position, radius, _overlaps,
                _collisionLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (IsObstacle(_overlaps[i])) return true;
            return count == _overlaps.Length;
        }

        private bool IsObstacle(Collider collider)
        {
            return collider != null && collider != _penetrationProbe
                && (_target == null || (collider.transform != _target && !collider.transform.IsChildOf(_target)));
        }

        private Vector3 ResolvePivotOverlap(Vector3 origin, float radius)
        {
            if (!HasBlockingOverlap(origin, radius)) return origin;
            Vector3 position = origin;
            _penetrationProbe.radius = radius;
            _penetrationProbe.enabled = true;
            try
            {
                for (int pass = 0; pass < 4; pass++)
                {
                    int count = Physics.OverlapSphereNonAlloc(position, radius, _overlaps,
                        _collisionLayers, QueryTriggerInteraction.Ignore);
                    bool hasCorrection = false;
                    for (int i = 0; i < count; i++)
                    {
                        Collider obstacle = _overlaps[i];
                        if (!IsObstacle(obstacle)) continue;
                        if (!Physics.ComputePenetration(_penetrationProbe, position, Quaternion.identity,
                            obstacle, obstacle.transform.position, obstacle.transform.rotation,
                            out Vector3 direction, out float depth)) continue;
                        position += direction * (depth + ContactEpsilon);
                        hasCorrection = true;
                    }
                    if (!hasCorrection) return position;
                }
                // 양 벽 사이에 구를 넣을 공간이 없으면 충돌체 순서에 따른 좌우 밀림을 버린다.
                return HasBlockingOverlap(position, radius) ? origin : position;
            }
            finally
            {
                _penetrationProbe.enabled = false;
            }
        }
    }
}
