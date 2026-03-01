using UnityEngine;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.State;
using Random = UnityEngine.Random;

namespace UPlayGround.Component
{
    /// <summary>
    /// 적 행동 결정자.
    /// 수치 설정은 EnemyBehaviorSO로 분리. Init()으로 주입받거나 Inspector에서 직접 할당 가능.
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

        // ── 런타임 ──────────────────────────────
        private float         _decisionTimer;
        private float         _lastAttackTime;
        private float         _lastSkillCheckTime;
        private Vector3       _spawnPosition;
        private float         _maxAttackRange;
        private bool          _hasGuardMotion;
        private BehaviorPhase _currentPhase;   // null = SO 기본값

        private const float SKILL_CHECK_INTERVAL = 0.5f;

        // ── SO 값 접근 (null 안전) ───────────────
        private EnemyBehaviorSO D => _behaviorData;

        public float PatrolRadius    => D?.patrolRadius    ?? 5f;
        public float PatrolWaitTime  => D?.patrolWaitTime  ?? 2f;
        public Vector3 SpawnPosition => _spawnPosition;
        public bool EnablePatrol     => D?.enablePatrol    ?? true;
        public float GuardDuration   => D?.guardDuration   ?? 1.5f;
        public float RetreatDistance => D?.retreatDistance ?? 3f;
        public float CircleDuration  => D?.circleDuration  ?? 2.5f;
        public bool HasGuardMotion   => _hasGuardMotion;

        // 페이즈 오버라이드 우선, 없으면 SO 기본값
        public float ContinueAttackChance => _currentPhase?.continueAttackChance ?? D?.continueAttackChance ?? 0.3f;
        public float GuardChance          => _currentPhase?.guardChance          ?? D?.guardChance          ?? 0.25f;
        public float RetreatChance        => _currentPhase?.retreatChance        ?? D?.retreatChance        ?? 0.2f;
        public float ChaseSpeedMultiplier => _currentPhase?.chaseSpeedMultiplier ?? D?.chaseSpeedMultiplier ?? 1.2f;

        private float ChargeChance          => _currentPhase?.chargeChance          ?? 0f;
        private float FlankChance           => _currentPhase?.flankChance           ?? 0f;
        private bool  AllowCharge           => _currentPhase?.allowCharge           ?? false;
        private bool  AllowFlank            => _currentPhase?.allowFlank            ?? false;
        private int   MaxConsecutiveAttacks => _currentPhase?.maxConsecutiveAttacks ?? 3;

        private float OptimalCombatDistance => D?.optimalCombatDistance ?? 2.5f;
        private float MinCombatDistance     => D?.minCombatDistance     ?? 1.5f;
        private bool  MaintainDistance      => D?.maintainDistance      ?? true;

        // ── 초기화 ──────────────────────────────

        /// <summary> MonsterActor.Init()에서 SO를 주입할 때 사용 </summary>
        public void Init(EnemyBehaviorSO data)
        {
            _behaviorData = data;
        }

        private void Awake()
        {
            _detection          ??= GetComponent<EnemyDetection>();
            _movementController ??= GetComponent<ActorMovementController>();
            _combat             ??= GetComponent<EnemyCombat>();
            _memory             ??= GetComponent<EnemyTacticalMemory>();
            _spawnPosition       = transform.position;
        }

        private void Start()
        {
            // AttackData는 MonsterActor.Init 이후 세팅되므로 Start에서 읽음
            if (_combat?.AttackData != null)
            {
                _maxAttackRange = _combat.AttackData.GetMaxAttackRange();
                // optimalCombatDistance가 maxRange보다 크면 조정
                if (D != null && D.optimalCombatDistance > _maxAttackRange)
                    D.optimalCombatDistance = _maxAttackRange * 0.8f;
            }
            else
            {
                _maxAttackRange = 2.5f;
            }

            _lastAttackTime = -(_combat?.AttackData?.globalCooldown ?? 1f);

            var actor = GetComponent<GameActor>();
            _hasGuardMotion = actor?.Animator?.HasMotion(AnimKey.Guard) ?? false;
        }

        private void Update()
        {
            _decisionTimer += Time.deltaTime;
            if (_decisionTimer >= _decisionInterval)
            {
                _decisionTimer = 0f;
                MakeDecision();
            }
        }

        // ── 페이즈 ──────────────────────────────

        /// <summary> MonsterActor.TakeDamage에서 HP가 바뀔 때마다 호출 </summary>
        public void UpdatePhase(float hpPercent)
        {
            if (D?.phases == null || D.phases.Length == 0) return;

            foreach (var phase in D.phases)
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
        }

        // ── 의사 결정 ───────────────────────────

        private void MakeDecision()
        {
            if (_movementController?.CurrentState == null) return;

            string state = _movementController.CurrentState.StateName;

            if (state is "Death" or "Hit" or "Attack" or "Counter" or
                "Guard" or "Retreat" or "Circle" or "Charge" or "Flank")
                return;

            if (Time.time - _lastSkillCheckTime >= SKILL_CHECK_INTERVAL)
            {
                _lastSkillCheckTime = Time.time;
                if (TryNonCombatSkill()) return;
            }

            if (_detection.HasTarget)
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

        // ── 전투 행동 ───────────────────────────

        private void HandleCombatBehavior(string state)
        {
            float dist = _detection.DistanceToTarget;

            if (_memory != null && _memory.IsOverAttacking(MaxConsecutiveAttacks))
            {
                _memory.ResetAttackCount();
                TransitionRetreating();
                return;
            }

            if (CanUseSkill() && _combat.HasAvailableSkillAtDistance(dist))
            {
                _lastAttackTime = Time.time;
                _movementController.TransitionToState(
                    new EnemyAttackState(_movementController, _combat, this, _detection));
                return;
            }

            HandleDistanceBasedBehavior(state, dist);
        }

        private void HandleDistanceBasedBehavior(string state, float dist)
        {
            if (MaintainDistance && dist < MinCombatDistance)
            {
                if (state != "Retreat") TransitionRetreating();
                return;
            }

            if (dist > OptimalCombatDistance)
            {
                if (state is "Chase" or "Charge" or "Flank") return;

                if (AllowCharge && Random.value < ChargeChance && _memory?.IsPlayerDodgingFrequently() == true)
                {
                    _movementController.TransitionToState(
                        new EnemyChargeState(_movementController, _combat, this, _detection, _memory));
                    return;
                }

                if (AllowFlank && Random.value < FlankChance && _memory?.IsPlayerDodgingFrequently() == true)
                {
                    _movementController.TransitionToState(
                        new EnemyFlankState(_movementController, _combat, this, _detection));
                    return;
                }

                _movementController.TransitionToState(
                    new EnemyChaseState(_movementController, this, _detection));
                return;
            }

            if (MaintainDistance && _hasGuardMotion && state != "Guard" && Random.value < GuardChance)
            {
                _movementController.TransitionToState(
                    new EnemyGuardState(_movementController, this, _detection, GuardDuration));
                return;
            }

            if (state != "Idle")
                _movementController.TransitionToState(new EnemyIdleState(_movementController));
        }

        // ── 공격 후 다음 행동 ────────────────────

        public void DecidePostAttack(bool attackHit)
        {
            if (attackHit) _memory?.NotifyAttackLanded();
            else           _memory?.NotifyAttackMissed();

            if (!_detection.HasTarget)
            {
                _movementController.TransitionToState(
                    EnablePatrol
                        ? (GameActorState)new EnemyPatrolState(_movementController, this)
                        : new EnemyIdleState(_movementController));
                return;
            }

            float dist = _detection.DistanceToTarget;

            if (Random.value < ContinueAttackChance && dist <= _maxAttackRange * 1.2f &&
                _movementController.CurrentState.StateName != "Attack")
            {
                _lastAttackTime = Time.time;
                _movementController.TransitionToState(
                    new EnemyAttackState(_movementController, _combat, this, _detection));
                return;
            }

            if (_memory != null && _memory.IsPlayerDodgingFrequently())
            {
                if (AllowCharge && Random.value < ChargeChance)
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
            }

            if (_hasGuardMotion && Random.value < GuardChance)
            {
                _movementController.TransitionToState(
                    new EnemyGuardState(_movementController, this, _detection, GuardDuration));
                return;
            }

            if (Random.value < RetreatChance && dist < RetreatDistance)
            {
                TransitionRetreating();
                return;
            }

            _movementController.TransitionToState(
                new EnemyChaseState(_movementController, this, _detection));
        }

        // ── 비전투 스킬 ─────────────────────────

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

        // ── 유틸 ────────────────────────────────

        private bool CanUseSkill()
        {
            if (_combat?.AttackData == null) return false;
            return Time.time - _lastAttackTime >= _combat.AttackData.globalCooldown;
        }

        private void TransitionRetreating()
        {
            _movementController.TransitionToState(
                new EnemyRetreatState(_movementController, this, _detection, RetreatDistance));
        }

        public float GetMaxAttackRange() => _maxAttackRange;

        public Vector3 GetRandomPatrolPoint()
        {
            Vector2 c = Random.insideUnitCircle * PatrolRadius;
            return _spawnPosition + new Vector3(c.x, 0, c.y);
        }

        public void Freeze()
        {
            if (_movementController.CurrentState.StateName == "Death") return;
            enabled = false;
            _movementController.TransitionToState(new EnemyIdleState(_movementController));
        }

        public void Unfreeze() => enabled = true;
    }
}
