using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Debugging;
using UPlayGround.Manager;
using UPlayGround.Data.Event;

namespace UPlayGround.Components
{
    public static class EnemyAggroPolicy
    {
        public static bool ShouldSwitchTarget(
            float currentThreat,
            float candidateThreat,
            float switchMultiplier)
        {
            return candidateThreat
                   > Mathf.Max(0f, currentThreat) * Mathf.Max(1f, switchMultiplier);
        }

        public static bool ShouldLoseTarget(
            bool targetAlive,
            float targetDistance,
            float distanceFromAnchor,
            bool acquiredExternally,
            float externalAcquireElapsed,
            bool hasLineOfSight,
            float lostSightElapsed,
            float lostTargetRadius,
            float maxChaseDistanceFromAnchor,
            float externalTargetMaxDuration,
            float lostSightGraceDuration)
        {
            if (!targetAlive)
                return true;

            if (maxChaseDistanceFromAnchor > 0f && distanceFromAnchor > maxChaseDistanceFromAnchor)
                return true;

            if (!hasLineOfSight && lostSightElapsed > Mathf.Max(0f, lostSightGraceDuration))
                return true;

            if (acquiredExternally)
            {
                return externalTargetMaxDuration > 0f
                       && externalAcquireElapsed > externalTargetMaxDuration;
            }

            return targetDistance > Mathf.Max(0f, lostTargetRadius);
        }
    }

    /// <summary>
    /// 적 탐지 시스템 - 플레이어 감지 및 추적 타겟 관리
    /// </summary>
    public class EnemyDetection : ActorComponent, IManagedTick, IActorSimulationResumeHandler, IDebugGizmoProvider
    {
        [Header("Detection Settings")]
        [SerializeField] private float _detectionRadius = 10f;
        [SerializeField] private float _lostTargetRadius = 15f;
        [SerializeField] private float _fieldOfView = 120f;
        [SerializeField] private LayerMask _targetLayer;
        [SerializeField] private LayerMask _obstacleLayer;

        [Header("Aggro Release")]
        [Tooltip("타겟이 장애물 뒤로 사라진 뒤 추적을 유지하는 시간(초)")]
        [Min(0f)] [SerializeField] private float _lostSightGraceDuration = 3f;
        [Tooltip("그룹 경보로 받은 먼 타겟을 직접 확인하지 못했을 때 추적할 최대 시간(초). 0이면 제한하지 않습니다.")]
        [Min(0f)] [SerializeField] private float _externalTargetMaxDuration = 6f;
        [Tooltip("최초 활성 위치에서 이 거리보다 멀리 추격하면 어그로를 해제합니다. 0이면 제한하지 않습니다.")]
        [Min(0f)] [SerializeField] private float _maxChaseDistanceFromAnchor = 30f;
        
        [Header("Ally Detection")]
        [SerializeField] private float _allyDetectionRadius = 10f;
        [SerializeField] private LayerMask _allyLayer;
        
        [Header("Detection Optimization")]
        [SerializeField] private float _detectionInterval = 0.2f;

        [Header("Threat")]
        [Min(1f)] [SerializeField] private float _targetSwitchThreatMultiplier = 1.25f;
        [Min(0.1f)] [SerializeField] private float _threatMemorySeconds = 12f;
        
        private Transform _currentTarget;
        private bool       _targetAcquiredExternally; // AlertGroup 등 외부 주입 여부
        private Vector3    _aggroAnchorPosition;
        private float      _targetAcquiredTime;
        private float      _lastLineOfSightTime;
        private float _detectionTimer;
        private List<IDamageable> _cachedAllies = new List<IDamageable>();

        // 물리 쿼리 GC 방지용 재사용 버퍼 (OverlapSphere → OverlapSphereNonAlloc)
        // 주의: 범위 내 콜라이더가 64를 초과하면 초과분은 무시됨 (타겟/아군 레이어 특성상 충분)
        private readonly Collider[] _overlapBuffer = new Collider[64];

        private AgentTickManager _tickManager;
        private IDisposable _simulationLease;
        private GameActor _owner;
        private readonly Dictionary<int, ThreatEntry> _threatEntries = new();
        
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
            if (!HasTarget)
                _aggroAnchorPosition = transform.position;

            // 개별 Update 대신 AgentTickManager가 일괄 틱한다.
            if (Application.isPlaying)
            {
                _owner ??= GetComponent<GameActor>();
                _tickManager = AgentTickManager.Instance;
                _tickManager?.Register(_owner, this);
                DebugGizmoBridge.RegisterProvider(this);
            }
        }

        private void OnDisable()
        {
            _tickManager?.Unregister(_owner, this);
            _tickManager = null;
            DebugGizmoBridge.UnregisterProvider(this);
            ReleaseSimulationLease();
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

        public void OnActorSimulationResumed()
        {
            _detectionTimer = 0f;
            UpdateDetection();
        }

        public void AcquireTarget(Transform target, bool acquiredExternally = true)
        {
            RegisterThreat(target, acquiredExternally ? 2f : 1f, acquiredExternally);
        }

        /// <summary>피격 피해량을 위협도로 누적하고 현재 대상보다 충분히 높을 때만 전환한다.</summary>
        public void RegisterDamageThreat(Transform target, float damage)
        {
            RegisterThreat(target, Mathf.Max(1f, damage), true);
        }

        private void RegisterThreat(Transform target, float threat, bool acquiredExternally)
        {
            if (!TryResolveHostileTarget(target, out GameActor targetActor))
                return;

            PruneExpiredThreat();
            int targetId = targetActor.CombatantRuntimeId;
            if (!_threatEntries.TryGetValue(targetId, out ThreatEntry entry))
            {
                entry = new ThreatEntry(targetActor);
                _threatEntries.Add(targetId, entry);
            }
            entry.Score += Mathf.Max(0f, threat);
            entry.LastUpdatedTime = Time.time;

            GameActor currentActor = _currentTarget != null
                ? _currentTarget.GetComponentInParent<GameActor>()
                : null;
            if (currentActor != null && currentActor != targetActor)
            {
                float currentThreat = _threatEntries.TryGetValue(
                    currentActor.CombatantRuntimeId,
                    out ThreatEntry currentEntry)
                    ? currentEntry.Score
                    : 1f;
                if (!EnemyAggroPolicy.ShouldSwitchTarget(
                        currentThreat,
                        entry.Score,
                        _targetSwitchThreatMultiplier))
                {
                    return;
                }
            }

            bool wasWithoutTarget = !HasTarget;
            _currentTarget = targetActor.transform;
            if (wasWithoutTarget)
            {
                // Unity fake-null 타겟이 파괴된 뒤 재획득하면 이전 lease가 남아 있을 수 있다.
                ReleaseSimulationLease();
                _owner ??= GetComponent<GameActor>();
                _targetAcquiredExternally = acquiredExternally;
                _targetAcquiredTime = Time.time;
                _lastLineOfSightTime = Time.time;
                _simulationLease = ActorSvc.Simulation?.AcquireActiveLease(
                    _owner, this, "EnemyTarget");
            }
            else
            {
                _targetAcquiredExternally = acquiredExternally;
                _targetAcquiredTime = Time.time;
                _lastLineOfSightTime = Time.time;
            }

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
            _owner ??= GetComponent<GameActor>();
            LayerMask candidateLayers = _owner != null
                ? _owner.GetAttackTargetLayerMask()
                : _targetLayer;
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                _detectionRadius,
                _overlapBuffer,
                candidateLayers);

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
                AcquireTarget(potentialTarget, acquiredExternally: false);
                break;
            }
        }

        private bool IsTargetValid(Transform target)
        {
            if (!TryResolveHostileTarget(target, out GameActor targetActor))
                return false;

            // 타겟이 살아있는지 체크
            bool targetAlive = targetActor.IsCombatAvailable;
            float targetDistance = Vector3.Distance(transform.position, targetActor.transform.position);
            float distanceFromAnchor = Vector3.Distance(_aggroAnchorPosition, transform.position);
            bool hasLineOfSight = !IsObstructed(target);

            if (hasLineOfSight)
                _lastLineOfSightTime = Time.time;

            // 외부 경보 타겟이 일반 추적 범위까지 들어오고 시야로 확인되면
            // 이후부터 직접 획득한 타겟과 같은 해제 규칙을 적용한다.
            if (_targetAcquiredExternally)
            {
                if (hasLineOfSight && targetDistance <= _lostTargetRadius)
                    _targetAcquiredExternally = false;
            }

            return !EnemyAggroPolicy.ShouldLoseTarget(
                targetAlive,
                targetDistance,
                distanceFromAnchor,
                _targetAcquiredExternally,
                Time.time - _targetAcquiredTime,
                hasLineOfSight,
                Time.time - _lastLineOfSightTime,
                _lostTargetRadius,
                _maxChaseDistanceFromAnchor,
                _externalTargetMaxDuration,
                _lostSightGraceDuration);
        }

        private bool TryResolveHostileTarget(Transform candidate, out GameActor targetActor)
        {
            targetActor = candidate != null
                ? candidate.GetComponentInParent<GameActor>()
                : null;
            if (targetActor == null || targetActor == _owner)
                return false;

            _owner ??= GetComponent<GameActor>();
            return _owner != null
                   && targetActor.IsCombatAvailable
                   && CombatRelationUtility.CanTarget(_owner, targetActor);
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
            _targetAcquiredTime = 0f;
            _lastLineOfSightTime = 0f;
            ReleaseSimulationLease();
            OnTargetLost?.Invoke();
        }

        public void ForceResetTarget()
        {
            bool hadTarget = _currentTarget != null;
            _currentTarget = null;
            _targetAcquiredExternally = false;
            _targetAcquiredTime = 0f;
            _lastLineOfSightTime = 0f;
            _threatEntries.Clear();
            ReleaseSimulationLease();
            if (hadTarget)
                OnTargetLost?.Invoke();
        }

        private void ReleaseSimulationLease()
        {
            _simulationLease?.Dispose();
            _simulationLease = null;
        }

        private void PruneExpiredThreat()
        {
            if (_threatEntries.Count == 0)
                return;

            float oldestAllowed = Time.time - Mathf.Max(0.1f, _threatMemorySeconds);
            _cleanupThreatIds.Clear();
            foreach (KeyValuePair<int, ThreatEntry> pair in _threatEntries)
            {
                if (pair.Value.Actor == null
                    || !pair.Value.Actor.IsCombatAvailable
                    || pair.Value.LastUpdatedTime < oldestAllowed)
                {
                    _cleanupThreatIds.Add(pair.Key);
                }
            }
            for (int i = 0; i < _cleanupThreatIds.Count; i++)
                _threatEntries.Remove(_cleanupThreatIds[i]);
        }

        private readonly List<int> _cleanupThreatIds = new();

        private sealed class ThreatEntry
        {
            public ThreatEntry(GameActor actor) => Actor = actor;

            public GameActor Actor { get; }
            public float Score { get; set; }
            public float LastUpdatedTime { get; set; }
        }
            
        #region Ally Detection
        /// <summary>
        /// 주변 아군 수 계산
        /// </summary>
        public int GetAllyCount()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                _allyDetectionRadius,
                _overlapBuffer,
                ResolveCombatantLayers(_allyLayer));

            int count = 0;
            for (int i = 0; i < hitCount; i++)
            {
                if (TryResolveLivingAlly(_overlapBuffer[i], out _))
                    count++;
            }

            return count;
        }
        
        /// <summary>
        /// 주변 아군 리스트 가져오기
        /// </summary>
        public List<IDamageable> GetNearbyAllies()
        {
            _cachedAllies.Clear();

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                _allyDetectionRadius,
                _overlapBuffer,
                ResolveCombatantLayers(_allyLayer));

            for (int i = 0; i < hitCount; i++)
            {
                if (TryResolveLivingAlly(_overlapBuffer[i], out IDamageable damageable)
                    && !_cachedAllies.Contains(damageable))
                    _cachedAllies.Add(damageable);
            }

            return _cachedAllies;
        }
        
        /// <summary>
        /// 특정 HP 이하인 아군이 주변에 있는지 체크
        /// </summary>
        public bool HasInjuredAllyNearby(float maxHealthPercent, float searchRadius = -1f)
        {
            float radius = searchRadius > 0 ? searchRadius : _allyDetectionRadius;
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                radius,
                _overlapBuffer,
                ResolveCombatantLayers(_allyLayer));

            for (int i = 0; i < hitCount; i++)
            {
                if (TryResolveLivingAlly(_overlapBuffer[i], out IDamageable damageable))
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
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                _allyDetectionRadius,
                _overlapBuffer,
                ResolveCombatantLayers(_allyLayer));

            IDamageable mostInjured = null;
            float lowestHealthPercent = 1f;

            for (int i = 0; i < hitCount; i++)
            {
                if (TryResolveLivingAlly(_overlapBuffer[i], out IDamageable damageable))
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

        private LayerMask ResolveCombatantLayers(LayerMask configuredLayers)
        {
            _owner ??= GetComponent<GameActor>();
            return _owner != null
                ? configuredLayers.value | _owner.GetAttackTargetLayerMask().value
                : configuredLayers;
        }

        private bool TryResolveLivingAlly(Collider candidate, out IDamageable damageable)
        {
            damageable = null;
            if (candidate == null)
                return false;

            GameActor allyActor = candidate.GetComponentInParent<GameActor>();
            if (allyActor == null || allyActor == _owner || !allyActor.IsCombatAvailable)
                return false;

            _owner ??= GetComponent<GameActor>();
            if (_owner == null
                || CombatRelationUtility.GetRelation(_owner, allyActor)
                != UPlayGround.Data.Combat.CombatRelation.Ally)
            {
                return false;
            }

            damageable = allyActor as IDamageable;
            return damageable != null && damageable.IsAlive();
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

            // 최초 활성 위치 기준 최대 추격 범위
            if (_maxChaseDistanceFromAnchor > 0f)
            {
                Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.8f);
                Gizmos.DrawWireSphere(_aggroAnchorPosition, _maxChaseDistanceFromAnchor);
                Gizmos.DrawLine(position, _aggroAnchorPosition);
            }

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
