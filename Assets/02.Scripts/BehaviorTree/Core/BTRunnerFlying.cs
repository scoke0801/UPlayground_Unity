using UnityEngine;
using UPlayGround.State;

namespace UPlayGround.Component
{
    using UPlayGround.BehaviorTree;

    /// <summary>
    /// EnemyFlyingBrain을 상속하여 지상 전투 의사결정을 BT로 대체한다.
    /// 공중 루프(TakeOff → AirCircle → Descend)는 상태 콜백 흐름을 그대로 유지한다.
    /// </summary>
    public class BTRunnerFlying : EnemyFlyingBrain
    {
        [Header("Behavior Tree")]
        [SerializeField] private BehaviorTreeSO _mainTreeSO;
        [SerializeField] private BehaviorTreeSO _postGroundAttackTreeSO;

        private BTNode             _mainTree;
        private BTNode             _postAttackTree;
        private RuntimeBlackboard  _bb;

        public BTNode             MainTree   => _mainTree;
        public RuntimeBlackboard  Blackboard => _bb;

        private const float MIN_DELAY = 0.4f;
        private const float MAX_DELAY = 1.2f;

        // ── 초기화 ─────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            // base.Start()가 초기 상태 전환까지 처리 → 먼저 실행
            base.Start();
            BuildTrees();
        }

        private void BuildTrees()
        {
            _bb = new RuntimeBlackboard
            {
                FlyingRunner = this,
                Detection    = _detection,
                Combat       = _combat,
                Movement     = _movementController,
            };

            if (_mainTreeSO != null)
            {
                _mainTreeSO.blackboard?.InitializeBlackboard(_bb);
                _mainTree = _mainTreeSO.CreateRuntimeTree(_bb);
            }

            if (_postGroundAttackTreeSO != null)
                _postAttackTree = _postGroundAttackTreeSO.CreateRuntimeTree(_bb);
        }

        // ── MakeDecision 오버라이드 ─────────────────────────────────────

        protected override void MakeDecision(string stateName)
        {
            if (_mainTree == null || _bb == null)
            {
                base.MakeDecision(stateName);
                return;
            }

            RefreshBlackboard(stateName);
            _mainTree.Tick(_bb);
        }

        // ── 공격 후 지상 행동 오버라이드 ────────────────────────────────

        public override void OnGroundAttackFinished()
        {
            _groundAttackCount++;
            _lastAttackTime = Time.time;

            if (ShouldTakeOff())
            {
                TriggerTakeOff();
                return;
            }

            if (_postAttackTree == null || _bb == null)
            {
                base.DecidePostGroundAttack();
                return;
            }

            RefreshBlackboard(_movementController.CurrentState?.StateName ?? "");
            _postAttackTree.Tick(_bb);
        }

        // ── Blackboard 갱신 ──────────────────────────────────────────────

        private void RefreshBlackboard(string stateName)
        {
            _bb.Set(BBKey.HasTarget,        _detection?.HasTarget        ?? false);
            _bb.Set(BBKey.DistanceToTarget, _detection?.DistanceToTarget ?? float.MaxValue);
            _bb.Set(BBKey.CurrentStateName, stateName);

            _bb.Set(BBKey.ShouldTakeOff,     ShouldTakeOff());
            _bb.Set(BBKey.ShouldDescend,     _airAttackCount >= _currentAirAttackLimit);
            _bb.Set(BBKey.IsAirState,        IsAirState(stateName));
            _bb.Set(BBKey.GroundAttackCount, _groundAttackCount);
            _bb.Set(BBKey.AirAttackCount,    _airAttackCount);

            _bb.Set(BBKey.OptimalCombatDistance, OptimalCombatDistance);
            _bb.Set(BBKey.PersonalSpaceDistance, _personalSpaceDistance);
            _bb.Set(BBKey.MinCombatDistance,     _minCombatDistance);
            _bb.Set(BBKey.MaxAttackRange,        _maxAttackRange);

            float hpPct = (_monster != null && _monster.MaxHealth > 0f)
                ? _monster.CurrentHealth / _monster.MaxHealth
                : 1f;
            _bb.Set(BBKey.SelfHPPercent, hpPct);
        }

        // ── 액션 노드 트리거 메서드 ─────────────────────────────────────

        public void TriggerTakeOff()           => TransitionToTakeOff();
        public void TriggerDescend()           => TransitionToDescend();

        public void TriggerFlyingChase()
            => _movementController.TransitionToState(new EnemyFlyingChaseState(_movementController, this));

        public void TriggerFlyingGroundAttack()
            => _movementController.TransitionToState(new EnemyFlyingGroundAttackState(_movementController, this));

        public void TriggerFlyingRetreat()
            => _movementController.TransitionToState(new EnemyFlyingRetreatState(_movementController, this));

        public void TriggerFlyingPatrol()
            => _movementController.TransitionToState(new EnemyFlyingPatrolState(_movementController, this));

        public void TriggerFlyingIdle()
            => _movementController.TransitionToState(new EnemyIdleState(_movementController));

        public void TriggerFlyingCircle(float duration)
        {
            if (_bb != null)
            {
                _bb.Set(BBKey.LastActionTime,  Time.time);
                _bb.Set(BBKey.NextActionDelay, Random.Range(MIN_DELAY, MAX_DELAY));
            }
            _movementController.TransitionToState(new EnemyFlyingCircleState(_movementController, this, duration));
        }
    }
}
