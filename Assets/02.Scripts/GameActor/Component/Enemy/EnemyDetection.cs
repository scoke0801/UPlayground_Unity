using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Debugging;
using UPlayGround.Manager;
using UPlayGround.Data.Event;

namespace UPlayGround.Components
{
    /// <summary>
    /// 적 탐지 시스템 - 플레이어 감지 및 추적 타겟 관리
    /// </summary>
    public class EnemyDetection : ActorComponent, IManagedTick, IDebugGizmoProvider
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
        private bool       _targetAcquiredExternally; // AlertGroup 등 외부 주입 여부
        private float _detectionTimer;
        private List<IDamageable> _cachedAllies = new List<IDamageable>();

        // 물리 쿼리 GC 방지용 재사용 버퍼 (OverlapSphere → OverlapSphereNonAlloc)
        // 주의: 범위 내 콜라이더가 64를 초과하면 초과분은 무시됨 (타겟/아군 레이어 특성상 충분)
        private readonly Collider[] _overlapBuffer = new Collider[64];

        private AgentTickManager _tickManager;
        
        public Transform CurrentTarget => _currentTarget;
        public bool HasTarget => _currentTarget != null;
        public float DistanceToTarget => HasTarget ? Vector3.Distance(transform.position, _currentTarget.position) : float.MaxValue;
        public float DetectionRadius => _detectionRadius;
        public float LostTargetRadius => _lostTargetRadius;
        public float FieldOfView => _fieldOfView;
        public float AllyDetectionRadius => _allyDetectionRadius;
        public LayerMask AllyLayer => _allyLayer;
        
        private void OnEnable()
        {
            // 개별 Update 대신 AgentTickManager가 일괄 틱한다.
            if (Application.isPlaying)
            {
                _tickManager = AgentTickManager.Instance;
                _tickManager?.Register(this);
                DebugGizmoBridge.RegisterProvider(this);
            }
        }

        private void OnDisable()
        {
            _tickManager?.Unregister(this);
            _tickManager = null;
            DebugGizmoBridge.UnregisterProvider(this);
        }

        /// <summary>
        /// <see cref="AgentTickManager"/>가 매 프레임 호출. 기존 Update 본문과 동일하다.
        /// </summary>
        public void ManagedTick(float deltaTime)
        {
            _detectionTimer += deltaTime;

            if (_detectionTimer >= _detectionInterval)
            {
                _detectionTimer = 0f;
                UpdateDetection();
            }
        }

        public void AcquireTarget(Transform target)
        {
            if (target.TryGetComponent<IDamageable>(out var targetDamageable) && !targetDamageable.IsAlive())
                return;

            bool wasWithoutTarget = !HasTarget;
            _currentTarget = target;
            _targetAcquiredExternally = wasWithoutTarget; // 새로 주입된 경우만 true

            if (wasWithoutTarget)
            {
                OnTargetAcquiredExternally?.Invoke();
                Svc.EventPublisher?.Send(GameMilestoneEvent.CombatStarted);
            }
        }

        /// <summary>
        /// 경보 전파 등으로 외부에서 타겟이 주입됐을 때 발생.
        /// EnemyAIController이 구독해서 즉시 Chase로 전환한다.
        /// </summary>
        public event System.Action OnTargetAcquiredExternally;
        public event System.Action OnTargetLost;
        
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
            int count = Physics.OverlapSphereNonAlloc(transform.position, _detectionRadius, _overlapBuffer, _targetLayer);

            for (int i = 0; i < count; i++)
            {
                Transform potentialTarget = _overlapBuffer[i].transform;
                
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

            // 타겟이 살아있는지 체크
            if (target.TryGetComponent<IDamageable>(out var damageable) && !damageable.IsAlive())
                return false;

            // 외부 주입(AlertGroup 등) 타겟은 거리 체크를 면제한다.
            // 자체 탐지한 타겟만 lostTargetRadius로 추적 해제한다.
            if (_targetAcquiredExternally)
            {
                // 스스로 감지 범위 안에 들어오면 이후부터는 일반 추적으로 전환
                float dist = Vector3.Distance(transform.position, target.position);
                if (dist <= _lostTargetRadius)
                    _targetAcquiredExternally = false;

                return true; // 아직 멀어도 타겟 유지
            }

            // 자체 탐지 타겟 — 추적 해제 범위 체크
            if (Vector3.Distance(transform.position, target.position) > _lostTargetRadius)
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

        /// <summary>
        /// 현재 타깃까지 탐지와 동일한 장애물 기준으로 시야가 열려 있는지 반환한다.
        /// 외부 경보로 주입된 타깃은 최초 탐지 시야 검사를 거치지 않으므로 공격 직전에도 사용한다.
        /// </summary>
        public bool HasLineOfSightToCurrentTarget()
        {
            return HasTarget && !IsObstructed(_currentTarget);
        }

        private void LostTarget()
        {
            Debug.Log($"[EnemyDetection] 타겟 상실: {_currentTarget?.name}");
            _currentTarget = null;
            _targetAcquiredExternally = false;
            OnTargetLost?.Invoke();
        }

        public void ForceResetTarget()
        {
            bool hadTarget = _currentTarget != null;
            _currentTarget = null;
            _targetAcquiredExternally = false;
            if (hadTarget)
                OnTargetLost?.Invoke();
        }
            
        #region Ally Detection
        /// <summary>
        /// 주변 아군 수 계산
        /// </summary>
        public int GetAllyCount()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _allyDetectionRadius, _overlapBuffer, _allyLayer);

            int count = 0;
            for (int i = 0; i < hitCount; i++)
            {
                var ally = _overlapBuffer[i];
                // 자기 자신 제외
                if (ally.transform == transform)
                    continue;

                // 살아있는 아군만 카운트
                if (ally.TryGetComponent<IDamageable>(out var damageable) && damageable.IsAlive())
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

            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _allyDetectionRadius, _overlapBuffer, _allyLayer);

            for (int i = 0; i < hitCount; i++)
            {
                var ally = _overlapBuffer[i];
                // 자기 자신 제외
                if (ally.transform == transform)
                    continue;

                if (ally.TryGetComponent<IDamageable>(out var damageable) && damageable.IsAlive())
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
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, radius, _overlapBuffer, _allyLayer);

            for (int i = 0; i < hitCount; i++)
            {
                var ally = _overlapBuffer[i];
                // 자기 자신 제외
                if (ally.transform == transform)
                    continue;

                if (ally.TryGetComponent<IDamageable>(out var damageable) && damageable.IsAlive())
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
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _allyDetectionRadius, _overlapBuffer, _allyLayer);

            IDamageable mostInjured = null;
            float lowestHealthPercent = 1f;

            for (int i = 0; i < hitCount; i++)
            {
                var ally = _overlapBuffer[i];
                // 자기 자신 제외
                if (ally.transform == transform)
                    continue;

                if (ally.TryGetComponent<IDamageable>(out var damageable) && damageable.IsAlive())
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
            if (DebugGizmoBridge.ShouldSuppressLocalGizmos(DebugGizmoCategory.AI, gameObject, DebugGizmoContentType.EnemyDetection))
                return;

            DrawDetectionGizmos();
        }
#endif

        /// <summary>
        /// 탐지/추적해제/아군 범위, 시야각, 타겟 라인 기즈모.
        /// 에디트 모드의 OnDrawGizmosSelected 와 플레이 모드의 중앙 DrawGizmos 가 공유한다.
        /// </summary>
        private void DrawDetectionGizmos()
        {
            Vector3 position = transform.position;

            // 탐지 범위
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(position, _detectionRadius);

            // 추적 해제 범위
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(position, _lostTargetRadius);

            // 아군 탐지 범위
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(position, _allyDetectionRadius);

            // 시야각
            Gizmos.color = Color.blue;
            Vector3 forward = transform.forward * _detectionRadius;
            Vector3 leftBoundary = Quaternion.Euler(0f, -_fieldOfView * 0.5f, 0f) * forward;
            Vector3 rightBoundary = Quaternion.Euler(0f, _fieldOfView * 0.5f, 0f) * forward;
            Gizmos.DrawLine(position, position + leftBoundary);
            Gizmos.DrawLine(position, position + rightBoundary);

            // 현재 타겟
            if (HasTarget)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(position, _currentTarget.position);
            }
        }

        #region Debug Gizmo

        public DebugGizmoCategory Category => DebugGizmoCategory.AI;
        public DebugGizmoContentType ContentType => DebugGizmoContentType.EnemyDetection;
        public UnityEngine.Object Owner => this;
        public bool IsAvailable => this != null && isActiveAndEnabled;

        public void CollectSnapshot(DebugGizmoFrameSnapshot snapshot)
        {
            snapshot.texts.Add(new DebugGizmoTextEntry
            {
                owner = this,
                category = Category,
                position = transform.position,
                text = HasTarget ? $"target={_currentTarget.name}" : "target=None",
            });
        }

        public void DrawGizmos(DebugGizmoDrawContext context)
        {
            DrawDetectionGizmos();

            context.DrawLabel(
                transform.position + Vector3.up * 2f,
                HasTarget ? $"AI Target: {_currentTarget.name}" : "AI Target: None");
        }

        #endregion
    }
}
