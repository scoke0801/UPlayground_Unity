using UnityEngine;
using UPlayGround.AI.BehaviorTree;
using UPlayGround.AI.Debugging;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Group;
using UPlayGround.MovementController;
using UPlayGround.State;
using Random = UnityEngine.Random;

namespace UPlayGround.Component
{
    /// <summary>
    /// 적 행동 결정자
    /// 플레이어 상태를 관찰하여 반응형으로 행동을 결정한다.
    /// 공격적 접근 + 불확실한 전환 + 빠른 리듬이 핵심.
    /// </summary>
    public class EnemyAIController : EnemyAIContext, IEnemyAIController
    {
        [HideInInspector, SerializeField] private EnemyBehaviorSO _behaviorData;

        [Header("References")]
        [SerializeField] private EnemyDetection          _detection;
        [SerializeField] private ActorMovementController _movementController;
        [SerializeField] private EnemyCombat             _combat;
        [SerializeField] private EnemyTacticalMemory     _memory;
        [SerializeField] private BehaviorTreeRunner      _behaviorTreeRunner;

        protected float       _lastAttackTime;
        private Vector3       _spawnPosition;
        private float         _maxAttackRange;
        private float         _effectiveOptimalCombatDistance;
        private bool          _hasGuardMotion;
        protected BehaviorPhase _currentPhase;

        // 전투 리듬 제어
        /// <summary> 다음 공격까지 기다리는 '의도적 지연 시간'. 매번 랜덤으로 변한다. </summary>
        private float _nextActionDelay;
        /// <summary> 현재 전투 행동 후 경과 타이머 </summary>
        private float _actionCooldownTimer;
        /// <summary> 연속 후퇴 방지 카운터 </summary>
        protected int _consecutiveDefensiveCount;

        private const float MIN_ACTION_DELAY     = 0.5f;  // 최소 행동 간 대기 (0.3 -> 0.5)
        private const float MAX_ACTION_DELAY     = 1.5f;  // 최대 행동 간 대기 (1.2 -> 1.5)
        // 그룹 연동
        private MonsterGroupController _groupController;
        private MemberPriority         _memberPriority;
        private MonsterActor           _monster;
        private AttackType             _myAttackType; // Start()에서 1회 결정, 런타임 불변

        // SO 값 접근
        private EnemyBehaviorSO data => _behaviorData;

        public override EnemyBehaviorSO BehaviorData => _behaviorData;
        public override BehaviorPhase CurrentPhase => _currentPhase;
        public override float HealthPercent => _monster != null ? _monster.GetHealthPercent() : 1f;
        public override float PatrolRadius    => data?.patrolRadius    ?? 5f;
        public override float PatrolWaitTime  => data?.patrolWaitTime  ?? 2f;
        public override Vector3 SpawnPosition => _spawnPosition;
        public override bool EnablePatrol     => data?.enablePatrol    ?? true;
        public override float GuardDuration   => data?.guardDuration   ?? 1.5f;
        public override float RetreatDistance => data?.retreatDistance  ?? 3f;
        public override float CircleDuration  => data?.circleDuration  ?? 2.5f;
        public override bool HasGuardMotion   => _hasGuardMotion;
        public MonsterGroupController Group => _groupController;
        /// <summary> 현재 플레이어(타겟)를 인식하고 추적 중인지 여부 </summary>
        public bool HasAggroTarget   => _detection != null && _detection.HasTarget;

        public float ContinueAttackChance => _currentPhase?.continueAttackChance ?? data?.continueAttackChance ?? 0.3f;
        public float GuardChance          => _currentPhase?.guardChance          ?? data?.guardChance          ?? 0.25f;
        public float RetreatChance        => _currentPhase?.retreatChance        ?? data?.retreatChance        ?? 0.2f;
        public override float ChaseSpeedMultiplier => _currentPhase?.chaseSpeedMultiplier ?? data?.chaseSpeedMultiplier ?? 1.2f;

        public override float OptimalCombatDistance  => _effectiveOptimalCombatDistance;
        public override float MinCombatDistance      => data?.minCombatDistance      ?? 1.5f;
        public bool  MaintainDistance       => data?.maintainDistance       ?? true;
        public override float ChaseStopDistance      => data?.chaseStopDistance      ?? 2.0f;
        public override float PersonalSpaceDistance  => data?.personalSpaceDistance  ?? 0.8f;
        public override GroupIntentBias CurrentGroupIntentBias
            => _groupController != null && _monster != null
                ? _groupController.GetIntentBias(_monster, _myAttackType)
                : GroupIntentBias.Neutral;
        public override MonsterGroupMemory CurrentGroupMemory => _groupController != null ? _groupController.Memory : null;

        #region Mono & Init
        public void Init(EnemyBehaviorSO data)
        {
            _behaviorData = data;
            EnsureBehaviorTreeRunner();
        }

        public void Init(ActorDefinitionSO definition)
        {
            if (definition?.behaviorData == null) return;
            Init(definition.behaviorData);
        }

        protected virtual void Awake()
        {
            _detection          ??= GetComponent<EnemyDetection>();
            _movementController ??= GetComponent<ActorMovementController>();
            _combat             ??= GetComponent<EnemyCombat>();
            _memory             ??= GetComponent<EnemyTacticalMemory>();
            _behaviorTreeRunner ??= GetComponent<BehaviorTreeRunner>();
#if UNITY_EDITOR
            if (GetComponent<IntentScoreTimeline>() == null)
                gameObject.AddComponent<IntentScoreTimeline>();
            if (GetComponent<EncounterReplayRecorder>() == null)
                gameObject.AddComponent<EncounterReplayRecorder>();
#endif
            _monster             = GetComponent<MonsterActor>();
            if (_monster?.Definition != null)
                Init(_monster.Definition);
            _spawnPosition       = transform.position;
            if (_detection != null)
                _detection.OnTargetAcquiredExternally += HandleTargetAcquired;
            EnsureBehaviorTreeRunner();

        }

        protected virtual void Start()
        {
            _effectiveOptimalCombatDistance = data?.optimalCombatDistance ?? 2.5f;

            if (_combat?.AttackData != null)
            {
                _maxAttackRange = _combat.AttackData.GetMaxAttackRange();
                if (_effectiveOptimalCombatDistance > _maxAttackRange)
                    _effectiveOptimalCombatDistance = _maxAttackRange * 0.8f;
            }
            else
            {
                _maxAttackRange = 2.5f;
            }

            _lastAttackTime = -(_combat?.AttackData?.globalCooldown ?? 1f);

            // 공격 타입 캐싱 — 모든 스킬이 Ranged면 원거리, 하나라도 Melee면 근접
            _myAttackType = (_combat?.AttackData?.HasRangedSkill() == true
                             && !_combat.AttackData.HasMeleeSkill())
                ? AttackType.Ranged
                : AttackType.Melee;

            var actor = GetComponent<GameActor>();
            _hasGuardMotion = actor?.Animator?.HasMotion(AnimKey.Guard) ?? false;

            RollNextActionDelay();
            EnsureBehaviorTreeRunner();
        }

        protected virtual void OnDestroy()
        {
            if (_detection != null)
                _detection.OnTargetAcquiredExternally -= HandleTargetAcquired;
        }

        protected virtual void Update()
        {
            _actionCooldownTimer += Time.deltaTime;

            if (_detection != null)
            {
                if(_detection.HasTarget && _memory != null)
                    _memory.SetPlayerTarget(_detection.CurrentTarget);
                if (_detection.HasTarget)
                    _groupController?.Memory?.SetPlayerTarget(_detection.CurrentTarget);
            }
        }

        #endregion

        private void EnsureBehaviorTreeRunner()
        {
            if (_behaviorData?.behaviorTree == null)
                return;

            _behaviorTreeRunner ??= GetComponent<BehaviorTreeRunner>();
            _behaviorTreeRunner ??= gameObject.AddComponent<BehaviorTreeRunner>();
            _behaviorTreeRunner.SetTreeAsset(_behaviorData.behaviorTree, restartIfRunning: false);

            if (isActiveAndEnabled && !_behaviorTreeRunner.IsRunning && !_behaviorTreeRunner.IsPaused)
                _behaviorTreeRunner.StartTree();
        }

        private void HandleTargetAcquired()
        {
            if (_movementController == null || _detection == null || !_detection.HasTarget)
                return;

            var stateName = _movementController.CurrentState?.StateName;
            if (stateName is not ("Idle" or "Patrol"))
                return;

            _movementController.TransitionToState(new EnemyChaseState(_movementController, this, _detection));
        }

        #region 페이즈
        public override void UpdatePhase(float hpPercent)
        {
            if (data?.phases == null || data.phases.Length == 0) return;

            foreach (var phase in data.phases)
            {
                if (hpPercent <= phase.hpThreshold)
                {
                    if (_currentPhase == phase) return;
                    _currentPhase = phase;
                    OnPhaseEntered(phase);
                    return;
                }
            }
        }

        private void OnPhaseEntered(BehaviorPhase phase)
        {
            Debug.Log($"[EnemyAIController] {gameObject.name} 페이즈 전환 → {phase.phaseName}");
            _memory?.ResetAttackCount();
            _consecutiveDefensiveCount = 0;
            RollNextActionDelay(); // 페이즈 전환 시 즉시 행동 템포 리셋
        }

        #endregion

        #region BT 콜백

        public override void DecidePostAttack(bool attackHit)
        {
            if (attackHit) _memory?.NotifyAttackLanded();
            else           _memory?.NotifyAttackMissed();
            if (attackHit) _groupController?.Memory?.NotifyAttackLanded();
            else           _groupController?.Memory?.NotifyAttackMissed();

            _actionCooldownTimer = 0f;
            RollNextActionDelay();

            if (!attackHit)
            {
                _nextActionDelay += Random.Range(0.6f, 1.2f);
            }
            else
            {
                var maxComboPressure = 2;
                var blackboard = _behaviorTreeRunner?.Context?.Blackboard;
                if (blackboard != null
                    && blackboard.TryGetInt(EnemyBlackboardKeys.AIMaxComboPressureCount, out var configuredMax))
                {
                    maxComboPressure = Mathf.Max(1, configuredMax);
                }

                if (_memory != null && _memory.ConsecutiveAttackCount < maxComboPressure)
                    _nextActionDelay = Random.Range(0.12f, 0.38f);
            }

            _behaviorTreeRunner?.Context?.Blackboard?.SetFloat(
                EnemyBlackboardKeys.NextActionAllowedTime,
                Time.time + _nextActionDelay);

            _movementController.TransitionToState(new EnemyIdleState(_movementController));
        }

        #endregion

        #region 기타
        
        public override bool CanUseSkill()
        {
            if (_combat?.AttackData == null) return false;
            return Time.time - _lastAttackTime >= _combat.AttackData.globalCooldown;
        }

        public override bool TryRequestAttackSlot()
        {
            if (_groupController == null || _monster == null)
                return true;

            return _groupController.RequestAttackSlot(_monster, _myAttackType);
        }

        public override bool TryGetFormationSlotPosition(float radius, out Vector3 position)
        {
            position = default;
            if (_groupController == null || _monster == null || _detection == null || !_detection.HasTarget)
                return false;

            return _groupController.TryGetFormationSlotPosition(
                _monster,
                _detection.CurrentTarget.position,
                _detection.CurrentTarget.forward,
                radius,
                out position);
        }

        public override void NotifyBTAttackStarted()
        {
            _lastAttackTime = Time.time;
            _actionCooldownTimer = 0f;
            _consecutiveDefensiveCount = 0;
            _memory?.NotifyCombatAction();

            if (_detection != null && _detection.HasTarget)
                _groupController?.AlertGroup(_detection.CurrentTarget);
        }

        /// <summary> 다음 행동까지의 대기 시간을 랜덤으로 결정 </summary>
        private void RollNextActionDelay()
        {
            _nextActionDelay = Random.Range(MIN_ACTION_DELAY, MAX_ACTION_DELAY);
            _actionCooldownTimer = 0f;
        }

        /// <summary>
        /// 플레이어 패리로 공격이 무효화됐을 때 호출.
        /// 다음 행동까지 대기 시간을 늘려 플레이어에게 반격 창을 열어준다.
        /// </summary>
        public void OnParried()
        {
            _actionCooldownTimer = 0f;
            _nextActionDelay     = Mathf.Max(_nextActionDelay, UnityEngine.Random.Range(1.0f, 2.0f));
            _memory?.NotifyAttackMissed();
            Debug.Log($"[EnemyAIController] {gameObject.name} 패리 처리 - 다음 행동까지 {_nextActionDelay:F2}초 대기");
        }

        public float GetMaxAttackRange() => _maxAttackRange;

        public override Vector3 GetRandomPatrolPoint()
        {
            Vector2 c = Random.insideUnitCircle * PatrolRadius;
            return _spawnPosition + new Vector3(c.x, 0, c.y);
        }

        /// <summary>
        /// MonsterGroupController가 Awake 또는 동적 소환 시 주입한다.
        /// </summary>
        public void SetGroup(MonsterGroupController group, MemberPriority priority)
        {
            _groupController = group;
            _memberPriority  = priority;
        }

        /// <summary> EnemyAttackState.OnExit에서 호출. 점유한 슬롯을 반환한다. </summary>
        public override void ReleaseGroupSlot()
        {
            if (_monster != null)
                _groupController?.NotifyMemberAttackEnded(_monster);
        }

        public override void ReleaseFormationSlot()
        {
            if (_monster != null)
                _groupController?.ReleaseFormationSlot(_monster);
        }

        /// <summary> AerialBehaviorLayer가 페이즈 오버라이드 접근에 사용 </summary>
        public EnemyBehaviorSO GetBehaviorSO() => _behaviorData;

        public void Freeze()
        {
            if (this == null) return;
            if (_movementController == null || _movementController.CurrentState?.StateName == "Death") return;
            _behaviorTreeRunner?.DisableBehavior(pause: true);
            enabled = false;
            _movementController.TransitionToState(new EnemyIdleState(_movementController));
        }

        public void Unfreeze()
        {
            if (this == null) return;
            enabled = true;
            _behaviorTreeRunner?.EnableBehavior();
        }
        #endregion
    }
}
