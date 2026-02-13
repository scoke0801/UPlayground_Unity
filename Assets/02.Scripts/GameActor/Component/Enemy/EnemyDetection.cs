using UnityEngine;

namespace UPlayGround.Component
{
    /// <summary>
    /// 적 탐지 시스템 - 플레이어 감지 및 추적 타겟 관리
    /// </summary>
    public class EnemyDetection : ActorComponent
    {
        [Header("Detection Settings")]
        [SerializeField] private float _detectionRadius = 10f;
        [SerializeField] private float _lostTargetRadius = 15f;
        [SerializeField] private float _fieldOfView = 120f;
        [SerializeField] private LayerMask _targetLayer;
        [SerializeField] private LayerMask _obstacleLayer;
        
        [Header("Detection Optimization")]
        [SerializeField] private float _detectionInterval = 0.2f;
        
        private Transform _currentTarget;
        private float _detectionTimer;
        
        public Transform CurrentTarget => _currentTarget;
        public bool HasTarget => _currentTarget != null;
        public float DistanceToTarget => HasTarget ? Vector3.Distance(transform.position, _currentTarget.position) : float.MaxValue;

        private void Update()
        {
            _detectionTimer += Time.deltaTime;
            
            if (_detectionTimer >= _detectionInterval)
            {
                _detectionTimer = 0f;
                UpdateDetection();
            }
        }

        private void UpdateDetection()
        {
            if (HasTarget)
            {
                // 기존 타겟 유효성 검증
                if (!IsTargetValid(_currentTarget))
                {
                    LostTarget();
                }
            }
            else
            {
                // 새로운 타겟 탐지
                DetectNewTarget();
            }
        }

        private void DetectNewTarget()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, _detectionRadius, _targetLayer);
            
            foreach (var collider in colliders)
            {
                Transform potentialTarget = collider.transform;
                
                // 시야각 체크
                if (!IsInFieldOfView(potentialTarget))
                    continue;
                
                // 장애물 차폐 체크
                if (IsObstructed(potentialTarget))
                    continue;
                
                // 타겟 발견
                AcquireTarget(potentialTarget);
                break;
            }
        }

        private bool IsTargetValid(Transform target)
        {
            if (target == null)
                return false;
            
            float distance = Vector3.Distance(transform.position, target.position);
            
            // 추적 해제 범위를 벗어났는지 체크
            if (distance > _lostTargetRadius)
                return false;
            
            // 타겟이 살아있는지 체크 (IDamageable 확인)
            var damageable = target.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsAlive())
                return false;
            
            return true;
        }

        private bool IsInFieldOfView(Transform target)
        {
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            Vector3 forward = transform.forward;
            
            float angle = Vector3.Angle(forward, directionToTarget);
            
            return angle <= _fieldOfView * 0.5f;
        }

        private bool IsObstructed(Transform target)
        {
            Vector3 origin = transform.position + Vector3.up * 1f; // 눈 높이
            Vector3 targetPosition = target.position + Vector3.up * 1f;
            Vector3 direction = targetPosition - origin;
            
            if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, direction.magnitude, _obstacleLayer))
            {
                return true;
            }
            
            return false;
        }

        private void AcquireTarget(Transform target)
        {
            if (target.GetComponent<IDamageable>()?.IsAlive() == false)
            {
                return;
            }
            
            _currentTarget = target;
            Debug.Log($"[EnemyDetection] 타겟 획득: {target.name}");
        }

        private void LostTarget()
        {
            Debug.Log($"[EnemyDetection] 타겟 상실: {_currentTarget?.name}");
            _currentTarget = null;
        }

        public void ForceResetTarget()
        {
            _currentTarget = null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 탐지 범위
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);
            
            // 추적 해제 범위
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _lostTargetRadius);
            
            // 시야각
            Gizmos.color = Color.blue;
            Vector3 forward = transform.forward * _detectionRadius;
            Vector3 leftBoundary = Quaternion.Euler(0, -_fieldOfView * 0.5f, 0) * forward;
            Vector3 rightBoundary = Quaternion.Euler(0, _fieldOfView * 0.5f, 0) * forward;
            
            Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
            
            // 현재 타겟
            if (HasTarget)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
            }
        }
#endif
    }
}