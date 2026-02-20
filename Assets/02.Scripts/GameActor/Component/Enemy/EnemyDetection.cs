using System.Collections.Generic;
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
        
        [Header("Ally Detection")]
        [SerializeField] private float _allyDetectionRadius = 10f;
        [SerializeField] private LayerMask _allyLayer;
        
        [Header("Detection Optimization")]
        [SerializeField] private float _detectionInterval = 0.2f;
        
        private Transform _currentTarget;
        private float _detectionTimer;
        private List<IDamageable> _cachedAllies = new List<IDamageable>();
        
        public Transform CurrentTarget => _currentTarget;
        public bool HasTarget => _currentTarget != null;
        public float DistanceToTarget => HasTarget ? Vector3.Distance(transform.position, _currentTarget.position) : float.MaxValue;
        public float AllyDetectionRadius => _allyDetectionRadius;
        public LayerMask AllyLayer => _allyLayer;
        
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
            
        #region Ally Detection
        /// <summary>
        /// 주변 아군 수 계산
        /// </summary>
        public int GetAllyCount()
        {
            Collider[] allies = Physics.OverlapSphere(transform.position, _allyDetectionRadius, _allyLayer);
            
            int count = 0;
            foreach (var ally in allies)
            {
                // 자기 자신 제외
                if (ally.transform == transform)
                    continue;
                
                // 살아있는 아군만 카운트
                var damageable = ally.GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive())
                {
                    count++;
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// 주변 아군 리스트 가져오기
        /// </summary>
        public List<IDamageable> GetNearbyAllies()
        {
            _cachedAllies.Clear();
            
            Collider[] allies = Physics.OverlapSphere(transform.position, _allyDetectionRadius, _allyLayer);
            
            foreach (var ally in allies)
            {
                // 자기 자신 제외
                if (ally.transform == transform)
                    continue;
                
                var damageable = ally.GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive())
                {
                    _cachedAllies.Add(damageable);
                }
            }
            
            return _cachedAllies;
        }
        
        /// <summary>
        /// 특정 HP 이하인 아군이 주변에 있는지 체크
        /// </summary>
        public bool HasInjuredAllyNearby(float maxHealthPercent, float searchRadius = -1f)
        {
            float radius = searchRadius > 0 ? searchRadius : _allyDetectionRadius;
            Collider[] allies = Physics.OverlapSphere(transform.position, radius, _allyLayer);
            
            foreach (var ally in allies)
            {
                // 자기 자신 제외
                if (ally.transform == transform)
                    continue;
                
                var damageable = ally.GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive())
                {
                    float healthPercent = damageable.GetHealthPercent();
                    if (healthPercent <= maxHealthPercent)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 가장 체력이 낮은 아군 찾기
        /// </summary>
        public IDamageable GetMostInjuredAlly()
        {
            Collider[] allies = Physics.OverlapSphere(transform.position, _allyDetectionRadius, _allyLayer);
            
            IDamageable mostInjured = null;
            float lowestHealthPercent = 1f;
            
            foreach (var ally in allies)
            {
                // 자기 자신 제외
                if (ally.transform == transform)
                    continue;
                
                var damageable = ally.GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive())
                {
                    float healthPercent = damageable.GetHealthPercent();
                    if (healthPercent < lowestHealthPercent)
                    {
                        lowestHealthPercent = healthPercent;
                        mostInjured = damageable;
                    }
                }
            }
            
            return mostInjured;
        }
        #endregion
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 탐지 범위
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);
            
            // 추적 해제 범위
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _lostTargetRadius);
              
            // 아군 탐지 범위
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _allyDetectionRadius);

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