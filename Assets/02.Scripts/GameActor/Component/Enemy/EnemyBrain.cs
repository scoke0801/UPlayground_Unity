using UnityEngine;
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
    public class EnemyBrain : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private EnemyBehaviorSO _behaviorData;

        [Header("References")]
        [SerializeField] private EnemyDetection          _detection;
        [SerializeField] private ActorMovementController _movementController;
        [SerializeField] private EnemyCombat             _combat;
        [SerializeField] private EnemyTacticalMemory     _memory;

        [Header("Decision Interval")]
        [SerializeField] private float _decisionInterval = 0.1f;

        private float         _decisionTimer;
        private float         _lastAttackTime;
        private float         _lastSkillCheckTime;
        private Vector3       _spawnPosition;
        private float         _maxAttackRange;
        private bool          _hasGuardMotion;
        private BehaviorPhase _currentPhase;

        // 전투 리듬 제어
        /// <summary> 다음 공격까지 기다리는 '의도적 지연 시간'. 매번 랜덤으로 변한다. </summary>
        private float _nextActionDelay;
        /// <summary> 현재 전투 행동 후 경과 타이머 </summary>
        private float _actionCooldownTimer;
        /// <summary> 연속 후퇴 방지 카운터 </summary>
        private int   _consecutiveDefensiveCount;

        private const float SKILL_CHECK_INTERVAL = 0.5f;
        private const float MIN_ACTION_DELAY     = 0.5f;  // 최소 행동 간 대기 (0.3 -> 0.5)
        private const float MAX_ACTION_DELAY     = 1.5f;  // 최대 행동 간 대기 (1.2 -> 1.5)
        private const int   MAX_DEFENSIVE_STREAK = 2;     // 연속 방어 행동 제한

        // 그룹 연동
        private MonsterGroupController _groupController;
        private MemberPriority         _memberPriority;
        private MonsterActor           _monster;
        private AttackType             _myAttackType; // Start()에서 1회 결정, 런타임 불변

        // SO 값 접근
        private EnemyBehaviorSO data => _behaviorData;

        public float PatrolRadius    => data?.patrolRadius    ?? 5f;
        public float PatrolWaitTime  => data?.patrolWaitTime  ?? 2f;
        public Vector3 SpawnPosition => _spawnPosition;
        public bool EnablePatrol     => data?.enablePatrol    ?? true;
        public float GuardDuration   => data?.guardDuration   ?? 1.5f;
        public float RetreatDistance => data?.retreatDistance  ?? 3f;
        public float CircleDuration  => data?.circleDuration  ?? 2.5f;
        public bool HasGuardMotion   => _hasGuardMotion;
        public MonsterGroupController Group => _groupController;
        /// <summary> 현재 플레이어(타겟)를 인식하고 추적 중인지 여부 </summary>
        public bool HasAggroTarget   => _detection != null && _detection.HasTarget;

        public float ContinueAttackChance => _currentPhase?.continueAttackChance ?? data?.continueAttackChance ?? 0.3f;
        public float GuardChance          => _currentPhase?.guardChance          ?? data?.guardChance          ?? 0.25f;
        public float RetreatChance        => _currentPhase?.retreatChance        ?? data?.retreatChance        ?? 0.2f;
        public float ChaseSpeedMultiplier => _currentPhase?.chaseSpeedMultiplier ?? data?.chaseSpeedMultiplier ?? 1.2f;

        private float ChargeChance          => _currentPhase?.chargeChance          ?? 0f;
        private float FlankChance           => _currentPhase?.flankChance           ?? 0f;
        private bool  AllowCharge           => _currentPhase?.allowCharge           ?? false;
        private bool  AllowFlank            => _currentPhase?.allowFlank            ?? false;
        private int   MaxConsecutiveAttacks => _currentPhase?.maxConsecutiveAttacks ?? 3;

        public float OptimalCombatDistance  => data?.optimalCombatDistance  ?? 2.5f;
        public float MinCombatDistance      => data?.minCombatDistance      ?? 1.5f;
        public bool  MaintainDistance       => data?.maintainDistance       ?? true;
        public float ChaseStopDistance      => data?.chaseStopDistance      ?? 2.0f;
        public float PersonalSpaceDistance  => data?.personalSpaceDistance  ?? 0.8f;

        // 공격 타이밍의 긴장감을 위한 가변 계수
        /// <summary> 페이즈가 진행될수록 행동이 빨라지는 비율 (0~1, 낮을수록 빠름) </summary>
        private float AggressionFactor
        {
            get
            {
                // 기본 0.7, 페이즈가 낮을수록 (HP가 낮을수록) 더 공격적
                float baseAggression = 0.7f;
                if (_currentPhase != null)
                {
                    // Charge/Flank 허용 페이즈는 더 공격적
                    if (_currentPhase.allowCharge || _currentPhase.allowFlank)
                        baseAggression = 0.4f;
                }
                return baseAggression;
            }
        }

        #region Mono & Init
        public void Init(EnemyBehaviorSO data)
        {
            _behaviorData = data;
        }

        protected virtual void Awake()
        {
            _detection          ??= GetComponent<EnemyDetection>();
            _movementController ??= GetComponent<ActorMovementController>();
            _combat             ??= GetComponent<EnemyCombat>();
            _memory             ??= GetComponent<EnemyTacticalMemory>();
            _monster             = GetComponent<MonsterActor>();
            _spawnPosition       = transform.position;

        }

        protected virtual void Start()
        {
            if (_combat?.AttackData != null)
            {
                _maxAttackRange = _combat.AttackData.GetMaxAttackRange();
                if (data != null && data.optimalCombatDistance > _maxAttackRange)
                    data.optimalCombatDistance = _maxAttackRange * 0.8f;
            }
            else
            {
                _maxAttackRange = 2.5f;
            }

            _lastAttackTime = -(_combat?.AttackData?.globalCooldown ?? 1f);

            // 공격 타입 캐싱 — 모든 스킬이 Ranged면 원거리, 하나라도 Melee면 근접
            _myAttackType = (_combat.AttackData?.HasRangedSkill() == true
                             && !_combat.AttackData.HasMeleeSkill())
                ? AttackType.Ranged
                : AttackType.Melee;

            var actor = GetComponent<GameActor>();
            _hasGuardMotion = actor?.Animator?.HasMotion(AnimKey.Guard) ?? false;

            if (_detection != null)
            {
                _detection.OnTargetAcquiredExternally += OnTargetInjected;
            }

            RollNextActionDelay();
        }

        protected virtual void OnDestroy()
        {
            if (_detection != null)
            {
                _detection.OnTargetAcquiredExternally -= OnTargetInjected;
            }
        }

        /// <summary>
        /// AlertGroup 등 외부 주입으로 타겟이 생겼을 때 호출.
        /// 쿨다운을 무시하고 즉시 Chase로 전환한다.
        /// </summary>
        private void OnTargetInjected()
        {
            if (!enabled) return;

            string state = _movementController?.CurrentState?.StateName;
            if (state is "Death" or "Attack" or "Hit" or "Grabbed") return;

            if (_monster?.Stat?.walkSpeed == 0)
                return;
            
            // 쿨다운 리셋 후 즉시 Chase
            _actionCooldownTimer = _nextActionDelay;
            _movementController.TransitionToState(
                new EnemyChaseState(_movementController, this, _detection));
        }

        protected virtual void Update()
        {
            _decisionTimer += Time.deltaTime;
            _actionCooldownTimer += Time.deltaTime;

            if (_decisionTimer >= _decisionInterval)
            {
                _decisionTimer = 0f;
                MakeDecision();
            }

            if (_detection != null)
            {
                if(_detection.HasTarget && _memory != null)
                    _memory.SetPlayerTarget(_detection.CurrentTarget);
            }
        }

        #endregion

        #region 페이즈
        public void UpdatePhase(float hpPercent)
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
            Debug.Log($"[EnemyBrain] {gameObject.name} 페이즈 전환 → {phase.phaseName}");
            _memory?.ResetAttackCount();
            _consecutiveDefensiveCount = 0;
            RollNextActionDelay(); // 페이즈 전환 시 즉시 행동 템포 리셋
        }

        #endregion

        #region 의사 결정

        protected virtual void MakeDecision()
        {
            if (_movementController?.CurrentState == null) return;

            string state = _movementController.CurrentState.StateName;

            // 절대 개입하지 않는 State
            if (state is "Death" or "Hit" or "Attack" or "Counter" or "Airborne" or "Grabbed" or "LaunchSmash"
                or "Land" or "TakeOff" or "AerialAttack")
                return;

            // 공중 체공 중에는 AerialState 내부 로직이 공격/착지를 결정
            if (state == "Aerial")
                return;

            // 비전투 스킬 체크 (힐/버프)
            if (Time.time - _lastSkillCheckTime >= SKILL_CHECK_INTERVAL)
            {
                _lastSkillCheckTime = Time.time;
                if (TryNonCombatSkill()) return;
            }

            if (_detection != null && _detection.HasTarget)
                HandleCombatBehavior(state);
            else
                HandleIdleBehavior(state);
        }

        private void HandleIdleBehavior(string state)
        {
            if (EnablePatrol)
            {
                if (state != "Patrol")
                    _movementController.TransitionToState(new EnemyPatrolState(_movementController, this));
            }
            else
            {
                if (state != "Idle")
                    _movementController.TransitionToState(new EnemyIdleState(_movementController));
            }
        }
        
        private void HandleCombatBehavior(string state)
        {
            float dist = _detection.DistanceToTarget;

            // 물리적 겹침 방지: personalSpace 이내로 진입하면 강제 후퇴
            // Attack Active 중(state == "Attack")에는 흐름을 끊지 않는다
            if (dist < PersonalSpaceDistance && state != "Retreat")
            {
                TransitionRetreating();
                return;
            }

            // 연속 공격 한계 초과 → 강제 후퇴
            if (_memory != null && _memory.IsOverAttacking(MaxConsecutiveAttacks))
            {
                _memory.ResetAttackCount();
                TransitionRetreating();
                return;
            }

            // 플레이어 상태에 따른 반응형 판단
            if (TryReactToPlayerState(state, dist))
                return;

            // Circle/Guard/Retreat 중에도 갑자기 공격 시도 - 불확실성 부여
            if (TryInterruptCurrentState(state, dist))
                return;

            // 행동 쿨다운이 안 끝났으면 현재 State 유지
            if (_actionCooldownTimer < _nextActionDelay)
            {
                // 단, Chase 중이고 사정거리 안이면 바로 공격
                if (state == "Chase" && IsInAttackablePosition(dist) && CanUseSkill())
                {
                    ExecuteAttack();
                    return;
                }
                return;
            }

            // 공격 가능 위치(사정거리 + 최소 접근 거리 충족)면 공격
            if (IsInAttackablePosition(dist) && CanUseSkill() && _combat.HasAvailableSkillAtDistance(dist))
            {
                ExecuteAttack();
                return;
            }

            // 공격 불가 위치 → 접근
            if (dist > OptimalCombatDistance && state is not "Chase" and not "Charge" and not "Flank")
            {
                if (_monster?.Stat?.walkSpeed != 0)
                {
                    _movementController.TransitionToState(
                        new EnemyChaseState(_movementController, this, _detection));
                }

                return;
            }

            // 거리 기반 행동
            HandleDistanceBasedBehavior(state, dist);
        }

        /// <summary>
        /// 공격 가능한 위치인지 판단.
        /// 사정거리 안 + OptimalCombatDistance 이내여야 한다.
        /// 원거리 몬스터는 minRange도 충족해야 하므로 HasAvailableSkillAtDistance로 최종 검증.
        /// </summary>
        private bool IsInAttackablePosition(float dist)
        {
            return dist <= _maxAttackRange && dist <= OptimalCombatDistance * 1.2f;
        }
        
        /// <summary>
        /// 플레이어의 현재 상태를 보고 반응하는 로직.
        /// 적이 '멍하니 기다리지 않고' 플레이어의 빈틈을 노린다.
        /// </summary>
        private bool TryReactToPlayerState(string myState, float dist)
        {
            if (_memory == null) return false;

            //플레이어가 공격 중 -> 뒤를 잡거나 가드
            if (_memory.IsPlayerAttacking())
            {
                // 근거리에 있으면 가드로 방어
                if (dist <= OptimalCombatDistance && _hasGuardMotion && myState != "Guard")
                {
                    if (Random.value < 0.5f)
                    {
                        _movementController.TransitionToState(
                            new EnemyGuardState(_movementController, this, _detection, GuardDuration * 0.6f));
                        OnDefensiveAction();
                        return true;
                    }
                }
                // 중거리면 Flank로 측면 돌파
                if (AllowFlank && dist > MinCombatDistance && dist <= OptimalCombatDistance * 1.5f && myState != "Flank")
                {
                    if (Random.value < 0.4f)
                    {
                        _movementController.TransitionToState(
                            new EnemyFlankState(_movementController, _combat, this, _detection));
                        _memory.NotifyCombatAction();
                        return true;
                    }
                }
                return false;
            }

            // 플레이어가 가드 중 -> 거리 좁혀서 잡기 또는 돌진
            if (_memory.IsPlayerGuarding())
            {
                // 가까우면 바로 공격 (가드 브레이크 기대)
                if (dist <= _maxAttackRange && CanUseSkill())
                {
                    ExecuteAttack();
                    return true;
                }
                
                // 멀면 Charge로 급접근
                if (AllowCharge && dist > OptimalCombatDistance && myState is not "Charge")
                {
                    _movementController.TransitionToState(
                        new EnemyChargeState(_movementController, _combat, this, _detection, _memory));
                    _memory.NotifyCombatAction();
                    return true;
                }
                return false;
            }

            // 플레이어가 피격 경직 중 -> 추가타
            if (_memory.IsPlayerStaggered())
            {
                if (dist <= _maxAttackRange * 1.3f && CanUseSkill())
                {
                    ExecuteAttack();
                    return true;
                }
                // 경직 중인데 멀면 빠르게 접근
                if (myState is not "Chase" and not "Charge")
                {
                    _movementController.TransitionToState(
                        new EnemyChaseState(_movementController, this, _detection));
                    return true;
                }
            }

            // 플레이어가 가만히 서 있음 -> 압박
            if (_memory.IsPlayerRecovering() && dist > OptimalCombatDistance)
            {
                if (myState is not "Chase" and not "Charge" and not "Flank")
                {
                    // 접근하여 압박
                    if (AllowCharge && Random.value < 0.3f)
                    {
                        _movementController.TransitionToState(
                            new EnemyChargeState(_movementController, _combat, this, _detection, _memory));
                    }
                    else
                    {
                        _movementController.TransitionToState(
                            new EnemyChaseState(_movementController, this, _detection));
                    }
                    _memory.NotifyCombatAction();
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Circle/Guard/Retreat 같은 비공격 State 도중,
        /// 랜덤한 타이밍에 갑자기 공격으로 전환하여 예측 불가능성을 높인다.
        /// </summary>
        private bool TryInterruptCurrentState(string state, float dist)
        {
            if (!CanUseSkill()) return false;

            // Circle 중 갑자기 공격
            if (state == "Circle" && dist <= _maxAttackRange * 1.3f)
            {
                // Circle 진입 후 랜덤 시간이 지나면 갑자기 공격
                float circleAggressionChance = 0.02f + (1f - AggressionFactor) * 0.05f;
                if (Random.value < circleAggressionChance && _combat.HasAvailableSkillAtDistance(dist))
                {
                    ExecuteAttack();
                    return true;
                }
            }

            // Guard 중 카운터 기회 (플레이어가 공격 안 하면 가드 풀고 공격)
            if (state == "Guard" && dist <= _maxAttackRange)
            {
                if (!_memory?.IsPlayerAttacking() == true && Random.value < 0.03f)
                {
                    ExecuteAttack();
                    return true;
                }
            }

            // Retreat 중이라도 플레이어가 경직이면 즉시 반격
            if (state == "Retreat" && _memory?.IsPlayerStaggered() == true && dist <= _maxAttackRange * 1.5f)
            {
                if (Random.value < 0.4f)
                {
                    ExecuteAttack();
                    return true;
                }
            }

            return false;
        }

        private void HandleDistanceBasedBehavior(string state, float dist)
        {
            // 너무 가까우면 후퇴 (단, 연속 방어 제한)
            if (MaintainDistance && dist < MinCombatDistance && _consecutiveDefensiveCount < MAX_DEFENSIVE_STREAK)
            {
                if (state != "Retreat")
                {
                    TransitionRetreating();
                    return;
                }
            }

            // 사정거리 밖
            if (dist > OptimalCombatDistance)
            {
                if (state is "Chase" or "Charge" or "Flank") return;

                // Charge 판단 — 단순 dodge 빈도 외에 '거리가 멀 때' 자체가 조건
                if (AllowCharge && dist > OptimalCombatDistance * 1.5f && Random.value < ChargeChance)
                {
                    _movementController.TransitionToState(
                        new EnemyChargeState(_movementController, _combat, this, _detection, _memory));
                    return;
                }

                if (AllowFlank && Random.value < FlankChance)
                {
                    _movementController.TransitionToState(
                        new EnemyFlankState(_movementController, _combat, this, _detection));
                    return;
                }

                _movementController.TransitionToState(
                    new EnemyChaseState(_movementController, this, _detection));
                return;
            }

            // 적정 거리 안에서 행동 결정 (가드 or Circle or Idle)
            float roll = Random.value;
            if (_hasGuardMotion && roll < GuardChance && _consecutiveDefensiveCount < MAX_DEFENSIVE_STREAK)
            {
                _movementController.TransitionToState(
                    new EnemyGuardState(_movementController, this, _detection, GuardDuration));
                OnDefensiveAction();
                return;
            }

            // Circle로 돌되, 짧은 시간만
            float shortCircle = CircleDuration * Random.Range(0.4f, 0.8f);
            _movementController.TransitionToState(
                new EnemyCircleState(_movementController, this, _detection, shortCircle));
        }

        
        //  공격 후 다음 행동 판단
        public void DecidePostAttack(bool attackHit)
        {
            if (attackHit) _memory?.NotifyAttackLanded();
            else           _memory?.NotifyAttackMissed();

            _actionCooldownTimer = 0f;
            RollNextActionDelay();

            // 공격 실패 시 (빗나감 또는 퍼펙트 가드 당함) 대기 시간을 대폭 늘려 반격의 기회를 줌
            if (!attackHit)
            {
                _nextActionDelay += Random.Range(0.6f, 1.2f);
                
                // 시각적으로 멍하게 만들기 위해 Idle 상태로 즉시 전환
                _movementController.TransitionToState(new EnemyIdleState(_movementController));
            }

            if (!_detection.HasTarget)
            {
                _movementController.TransitionToState(
                    EnablePatrol
                        ? (GameActorState)new EnemyPatrolState(_movementController, this)
                        : new EnemyIdleState(_movementController));
                return;
            }

            float dist = _detection.DistanceToTarget;

            // 적중 시: 공격적으로 연속 공격 확률 UP 
            if (attackHit)
            {
                float continueChance = ContinueAttackChance;

                // 플레이어가 경직 중이면 추가타 확률 대폭 상승
                if (_memory?.IsPlayerStaggered() == true)
                    continueChance = Mathf.Max(continueChance, 0.6f);

                if (Random.value < continueChance && dist <= _maxAttackRange * 1.2f)
                {
                    ExecuteAttack();
                    return;
                }
            }

            // 빗나감 시: 플레이어 반응에 따라 분기
            if (!attackHit)
            {
                // Dodge 많이 하는 플레이어 → Charge 또는 Flank
                if (_memory?.IsPlayerDodgingFrequently() == true)
                {
                    if (AllowCharge && Random.value < ChargeChance * 1.5f)
                    {
                        _movementController.TransitionToState(
                            new EnemyChargeState(_movementController, _combat, this, _detection, _memory));
                        return;
                    }
                    if (AllowFlank && Random.value < FlankChance * 1.5f)
                    {
                        _movementController.TransitionToState(
                            new EnemyFlankState(_movementController, _combat, this, _detection));
                        return;
                    }
                }
            }

            // 기본 분기: 여러 행동 중 가중치 기반 선택
            DecidePostAttackWeighted(dist);
        }

        /// <summary>
        /// 공격 후 행동을 가중치 기반으로 선택한다.
        /// 매번 같은 패턴이 나오지 않도록 상황에 따라 가중치가 동적으로 변한다.
        /// </summary>
        private void DecidePostAttackWeighted(float dist)
        {
            float wChase    = 0.3f;
            float wCircle   = 0.25f;
            float wGuard    = _hasGuardMotion ? GuardChance : 0f;
            float wRetreat  = (dist < RetreatDistance) ? RetreatChance : 0f;
            float wCharge   = AllowCharge ? ChargeChance : 0f;
            float wFlank    = AllowFlank  ? FlankChance  : 0f;

            // 연속 방어 했으면 공격적 행동 가중치 UP
            if (_consecutiveDefensiveCount >= MAX_DEFENSIVE_STREAK)
            {
                wChase  += 0.3f;
                wCharge += 0.2f;
                wGuard   = 0f;
                wRetreat = 0f;
            }

            // 최근 후퇴했으면 또 후퇴하지 않음
            if (_memory != null && _memory.TimeSinceLastRetreat() < 3f)
                wRetreat = 0f;

            float total = wChase + wCircle + wGuard + wRetreat + wCharge + wFlank;
            float roll  = Random.value * total;

            float acc = 0f;

            acc += wCharge;
            if (roll < acc && dist > OptimalCombatDistance)
            {
                _movementController.TransitionToState(
                    new EnemyChargeState(_movementController, _combat, this, _detection, _memory));
                return;
            }

            acc += wFlank;
            if (roll < acc)
            {
                _movementController.TransitionToState(
                    new EnemyFlankState(_movementController, _combat, this, _detection));
                return;
            }

            acc += wGuard;
            if (roll < acc)
            {
                _movementController.TransitionToState(
                    new EnemyGuardState(_movementController, this, _detection, GuardDuration));
                OnDefensiveAction();
                return;
            }

            acc += wRetreat;
            if (roll < acc)
            {
                TransitionRetreating();
                return;
            }

            acc += wCircle;
            if (roll < acc)
            {
                float shortCircle = CircleDuration * Random.Range(0.3f, 0.6f);
                _movementController.TransitionToState(
                    new EnemyCircleState(_movementController, this, _detection, shortCircle));
                return;
            }

            // 기본: Chase
            _movementController.TransitionToState(
                new EnemyChaseState(_movementController, this, _detection));
        }

        #endregion

        #region 비전투 스킬 - 힐 / 버프
        private bool TryNonCombatSkill()
        {
            if (_combat?.AttackData == null) return false;
            var skill = _combat.SelectAndExecuteSkill(float.MaxValue);
            if (skill == null) return false;
            if (skill.skillType is not (SkillType.Heal or SkillType.Buff)) return false;

            _lastAttackTime = Time.time;
            _movementController.TransitionToState(
                new EnemyAttackState(_movementController, _combat, this, _detection));
            return true;
        }
        #endregion

        #region 기타
        
        protected void ExecuteAttack()
        {
            // 그룹 슬롯 요청 — 거절당하면 공격하지 않고 Circle로 대기
            if (_groupController != null)
            {
                if (!_groupController.RequestAttackSlot(_monster, _myAttackType))
                {
                    float waitCircle = CircleDuration * Random.Range(0.3f, 0.6f);
                    _movementController.TransitionToState(
                        new EnemyCircleState(_movementController, this, _detection, waitCircle));
                    return;
                }
            }

            _lastAttackTime = Time.time;
            _actionCooldownTimer = 0f;
            _consecutiveDefensiveCount = 0;
            _memory?.NotifyCombatAction();

            if (_detection.HasTarget)
                _groupController?.AlertGroup(_detection.CurrentTarget);

            _movementController.TransitionToState(
                new EnemyAttackState(_movementController, _combat, this, _detection));
        }

        protected bool CanUseSkill()
        {
            if (_combat?.AttackData == null) return false;
            return Time.time - _lastAttackTime >= _combat.AttackData.globalCooldown;
        }

        private void TransitionRetreating()
        {
            _memory?.NotifyRetreated();
            OnDefensiveAction();
            _movementController.TransitionToState(
                new EnemyRetreatState(_movementController, this, _detection, RetreatDistance));
        }

        private void OnDefensiveAction()
        {
            _consecutiveDefensiveCount++;
        }

        /// <summary> 다음 행동까지의 대기 시간을 랜덤으로 결정 </summary>
        private void RollNextActionDelay()
        {
            float max = Mathf.Lerp(MIN_ACTION_DELAY, MAX_ACTION_DELAY, AggressionFactor);
            _nextActionDelay = Random.Range(MIN_ACTION_DELAY, max);
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
            Debug.Log($"[EnemyBrain] {gameObject.name} 패리 처리 - 다음 행동까지 {_nextActionDelay:F2}초 대기");
        }

        public float GetMaxAttackRange() => _maxAttackRange;

        public Vector3 GetRandomPatrolPoint()
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
        public void ReleaseGroupSlot()
        {
            if (_monster != null)
                _groupController?.ReleaseAttackSlot(_monster);
        }

        /// <summary> AerialBehaviorLayer가 페이즈 오버라이드 접근에 사용 </summary>
        public EnemyBehaviorSO GetBehaviorSO() => _behaviorData;

        public void Freeze()
        {
            if (_movementController.CurrentState.StateName == "Death") return;
            enabled = false;
            _movementController.TransitionToState(new EnemyIdleState(_movementController));
        }

        public void Unfreeze() => enabled = true;
        #endregion
    }
}
