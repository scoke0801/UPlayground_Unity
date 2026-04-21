using UnityEngine;
using UPlayGround.State;
using UPlayGround.Component;
using UPlayGround.MovementController;

namespace UPlayGround.Component
{
    using UPlayGround.BehaviorTree;

    /// <summary>
    /// EnemyBrain을 상속하여 MakeDecision을 BT로 대체한다.
    /// 기존 State 생성자는 EnemyBrain을 요구하므로 IS-A 관계로 호환성을 유지한다.
    /// </summary>
    public class BTRunner : EnemyBrain
    {
        [Header("Behavior Tree")]
        [SerializeField] private BehaviorTreeSO _behaviorTreeSO;

        private BTNode            _runtimeTree;
        private RuntimeBlackboard _bb;

        /// <summary> 에디터에서 런타임 하이라이트에 사용 </summary>
        public BTNode             RuntimeTree => _runtimeTree;
        public RuntimeBlackboard  Blackboard  => _bb;

        // 캐시 — base의 private 필드에 접근 불가하므로 별도 캐싱
        private EnemyDetection         _detectionCache;
        private ActorMovementController _movementCache;

        public EnemyDetection         BtDetection => _detectionCache;
        public ActorMovementController BtMovement  => _movementCache;

        // ── 페이즈 정보 공개 (protected _currentPhase → Blackboard) ──
        public bool  PhaseAllowCharge           => _currentPhase?.allowCharge           ?? false;
        public bool  PhaseAllowFlank            => _currentPhase?.allowFlank            ?? false;
        public float PhaseChargeChance          => _currentPhase?.chargeChance          ?? 0f;
        public float PhaseFlankChance           => _currentPhase?.flankChance           ?? 0f;
        public int   PhaseMaxConsecutiveAttacks  => _currentPhase?.maxConsecutiveAttacks ?? 3;

        /// <summary> BTCond_CanAttack에서 호출 </summary>
        public bool CanUseSkillPublic() => CanUseSkill();

        private const float MIN_DELAY = 0.5f;
        private const float MAX_DELAY = 1.5f;

        protected override void Awake()
        {
            base.Awake();
            _detectionCache = GetComponent<EnemyDetection>();
            _movementCache  = GetComponent<ActorMovementController>();
        }

        protected override void Start()
        {
            base.Start();
            BuildTree();
        }

        private void BuildTree()
        {
            if (_behaviorTreeSO == null)
            {
                Debug.LogWarning($"[BTRunner] {gameObject.name}: BehaviorTreeSO가 없습니다. base.MakeDecision() 사용.");
                return;
            }

            _bb = new RuntimeBlackboard
            {
                Runner    = this,
                Detection = _detectionCache,
                Combat    = GetComponent<EnemyCombat>(),
                Memory    = GetComponent<EnemyTacticalMemory>(),
                Movement  = _movementCache,
            };

            _behaviorTreeSO.blackboard?.InitializeBlackboard(_bb);

            _runtimeTree = _behaviorTreeSO.CreateRuntimeTree(_bb);
        }

        protected override void MakeDecision()
        {
            if (_runtimeTree == null || _bb == null)
            {
                base.MakeDecision();
                return;
            }

            // Blackboard 갱신
            _bb.Set(BBKey.HasTarget,        _detectionCache?.HasTarget        ?? false);
            _bb.Set(BBKey.DistanceToTarget, _detectionCache?.DistanceToTarget ?? float.MaxValue);
            _bb.Set(BBKey.CurrentStateName, _movementCache?.CurrentState?.StateName ?? "");

            _bb.Set(BBKey.PhaseAllowCharge,           PhaseAllowCharge);
            _bb.Set(BBKey.PhaseAllowFlank,            PhaseAllowFlank);
            _bb.Set(BBKey.PhaseChargeChance,          PhaseChargeChance);
            _bb.Set(BBKey.PhaseFlankChance,           PhaseFlankChance);
            _bb.Set(BBKey.PhaseMaxConsecutiveAttacks, PhaseMaxConsecutiveAttacks);

            _bb.Set(BBKey.OptimalCombatDistance, OptimalCombatDistance);
            _bb.Set(BBKey.MaxAttackRange,        GetMaxAttackRange());
            _bb.Set(BBKey.PersonalSpaceDistance, PersonalSpaceDistance);
            _bb.Set(BBKey.MinCombatDistance,     MinCombatDistance);
            _bb.Set(BBKey.RetreatDistance,       RetreatDistance);
            _bb.Set(BBKey.HasGuardMotion,        HasGuardMotion);

            _runtimeTree.Tick(_bb);
        }

        protected override void OnDefensiveAction()
        {
            base.OnDefensiveAction();
            if (_bb != null)
                _bb.Set(BBKey.ConsecutiveDefensiveCount, _bb.GetInt(BBKey.ConsecutiveDefensiveCount) + 1);
        }

        // ── 액션 노드가 호출하는 트리거 메서드 ──────────────────────
        public void TriggerAttack()
        {
            if (_bb != null)
            {
                _bb.Set(BBKey.LastActionTime,  Time.time);
                _bb.Set(BBKey.NextActionDelay, Random.Range(MIN_DELAY, MAX_DELAY));
            }
            ExecuteAttack();
        }

        public void TriggerRetreat()
        {
            if (_bb != null)
            {
                _bb.Set(BBKey.LastActionTime,  Time.time);
                _bb.Set(BBKey.NextActionDelay, Random.Range(MIN_DELAY, MAX_DELAY));
            }
            TransitionRetreating();
        }

        public void TriggerCircle(float duration)
        {
            if (_bb != null)
            {
                _bb.Set(BBKey.LastActionTime,  Time.time);
                _bb.Set(BBKey.NextActionDelay, Random.Range(MIN_DELAY * 0.5f, MAX_DELAY * 0.5f));
            }
            _movementCache.TransitionToState(new EnemyCircleState(_movementCache, this, _detectionCache, duration));
        }

        public void TriggerChase()
        {
            _movementCache.TransitionToState(new EnemyChaseState(_movementCache, this, _detectionCache));
        }

        public void TriggerIdle()
        {
            _movementCache.TransitionToState(new EnemyIdleState(_movementCache));
        }

        public void TriggerCharge()
        {
            if (_bb != null)
            {
                _bb.Set(BBKey.LastActionTime,  Time.time);
                _bb.Set(BBKey.NextActionDelay, Random.Range(MIN_DELAY, MAX_DELAY));
            }
            _bb?.Memory?.NotifyCombatAction();
            _movementCache.TransitionToState(
                new EnemyChargeState(_movementCache, _bb?.Combat, this, _detectionCache, _bb?.Memory));
        }

        public void TriggerFlank()
        {
            if (_bb != null)
            {
                _bb.Set(BBKey.LastActionTime,  Time.time);
                _bb.Set(BBKey.NextActionDelay, Random.Range(MIN_DELAY * 0.5f, MAX_DELAY * 0.5f));
            }
            _bb?.Memory?.NotifyCombatAction();
            _movementCache.TransitionToState(
                new EnemyFlankState(_movementCache, _bb?.Combat, this, _detectionCache));
        }

        public void TriggerGuard(float duration)
        {
            if (_bb != null)
            {
                _bb.Set(BBKey.LastActionTime,  Time.time);
                _bb.Set(BBKey.NextActionDelay, Random.Range(MIN_DELAY, MAX_DELAY));
            }
            OnDefensiveAction();
            _movementCache.TransitionToState(
                new EnemyGuardState(_movementCache, this, _detectionCache, duration));
        }

        public void TriggerPatrol()
        {
            _movementCache.TransitionToState(new EnemyPatrolState(_movementCache, this));
        }

        public void TriggerConsecutiveDefensiveReset()
        {
            if (_bb != null)
                _bb.Set(BBKey.ConsecutiveDefensiveCount, 0);
        }
    }
}
