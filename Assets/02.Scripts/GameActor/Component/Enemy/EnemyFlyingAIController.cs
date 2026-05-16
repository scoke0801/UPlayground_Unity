using UnityEngine;
using UPlayGround.AI.BehaviorTree;
using UPlayGround.Data;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Group;
using UPlayGround.MovementController;
using UPlayGround.State;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace UPlayGround.Component
{
    /// <summary>
    /// 비행형 몬스터 전용 Brain.
    /// 
    /// ■ 핵심 루프: 지상 추격/공격 → 이륙 → 공중 선회+투사체 → 급강하 or 착지 → 반복
    /// ■ 지상 행동: EnemyAIController과 유사한 Idle/Patrol/Chase/Circle/Retreat + 비행 전용 분기
    /// ■ 공중 행동: TakeOff → AirCircle(투사체) → Dive or Land
    ///
    /// 의사결정은 BehaviorTreeRunner가 담당한다.
    /// State 콜백은 BT가 참조할 카운터와 타임스탬프만 갱신한다.
    /// </summary>
    public class EnemyFlyingAIController : EnemyFlyingAIContext
    {
        [Header("References")]
        [SerializeField] protected EnemyDetection _detection;
        [SerializeField] protected EnemyCombat _combat;
        [SerializeField] protected ActorMovementController _movementController;
        [SerializeField] protected EnemyFlyingSettingsSO _flyingSettings;
        [SerializeField] private BehaviorTreeRunner _behaviorTreeRunner;

        [Header("Ground Combat")]
        [SerializeField] protected float _chaseStopDistance = 2f;
        [SerializeField] private float _chaseSpeedMultiplier = 1.2f;
        [SerializeField] private float _optimalCombatDistance = 1.5f;
        [SerializeField] protected float _minCombatDistance = 1.5f;
        [SerializeField] protected float _personalSpaceDistance = 0.8f;

        [Header("Ground Post-Attack Behavior")]
        [SerializeField] private float _circleDuration = 2.0f;
        [SerializeField] private float _retreatDistance = 3.0f;

        [Header("TakeOff Conditions")]
        [Tooltip("지상 체류 시간 초과 시 강제 이륙 (랜덤 범위)")]
        [SerializeField] private float _groundStayLimitMin = 7f;
        [SerializeField] private float _groundStayLimitMax = 12f;
        [Tooltip("지상 공격 횟수가 이 값에 도달하면 이륙")]
        [SerializeField] private int _groundAttackLimit = 2;

        [Header("Patrol")]
        [SerializeField] protected bool _enablePatrol = true;
        [SerializeField] private float _patrolRadius = 5f;
        [SerializeField] private float _patrolWaitTime = 2f;

        [Header("Air Settings")]
        [SerializeField] private float _airCircleRadius = 6f;
        [SerializeField] private float _airHoverHeight = 4f;
        [SerializeField] private float _airMoveSpeed = 6f;
        
        [Tooltip("공중 투사체 발사 횟수 (랜덤 범위)")]
        [SerializeField] private int _airAttackLimitMin = 1;
        [SerializeField] private int _airAttackLimitMax = 3;

        [Header("Dive Settings")]
        [SerializeField] private float _diveSpeed = 20f;
        [SerializeField] private float _diveImpactRadius = 3f;
        [Range(0f, 1f)]
        [SerializeField] private float _diveChance = 0.4f;

        // ── 런타임 ──
        protected float _groundTimer;
        protected float _currentGroundStayLimit; // 매 루프마다 랜덤 결정
        protected int _currentAirAttackLimit;     // 매 공중 루프마다 랜덤 결정
        protected int _groundAttackCount;
        protected int _airAttackCount;
        protected float _lastAttackTime;
        protected float _maxAttackRange;
        protected Vector3 _spawnPosition;

        protected MonsterActor _monster;
        private readonly List<EnemyAttackInfo> _diveSkillBuffer = new();

        // ── 프로퍼티 (State에서 접근) ──
        public override EnemyDetection Detection => _detection;
        public override EnemyCombat Combat => _combat;
        public override float ChaseStopDistance => _chaseStopDistance;
        public override float ChaseSpeedMultiplier => _chaseSpeedMultiplier;
        public override float AirCircleRadius => _airCircleRadius;
        public override float AirHoverHeight => _airHoverHeight;
        public override float AirMoveSpeed => _airMoveSpeed;
        public override int AirAttackLimit => _currentAirAttackLimit;
        public override float DiveSpeed => _diveSpeed;
        public override float DiveImpactRadius => _diveImpactRadius;
        public override float DiveChance => _diveChance;
        public override float GroundTimer => _groundTimer;
        public override int GroundAttackCount => _groundAttackCount;
        public override int AirAttackCount => _airAttackCount;

        // Patrol/Circle/Retreat용 (EnemyAIController 호환)
        public override float PatrolRadius => _patrolRadius;
        public override float PatrolWaitTime => _patrolWaitTime;
        public override bool EnablePatrol => _enablePatrol;
        public override Vector3 SpawnPosition => _spawnPosition;
        public override float CircleDuration => _circleDuration;
        public override float RetreatDistance => _retreatDistance;
        public override float OptimalCombatDistance => _optimalCombatDistance;
        public override float MinCombatDistance => _minCombatDistance;
        public override float PersonalSpaceDistance => _personalSpaceDistance;

        /// <summary> State들이 튜닝 값에 접근하는 단일 창구 </summary>
        public override EnemyFlyingSettingsSO FlyingSettings => _flyingSettings;

        #region Mono

        protected virtual void Awake()
        {
            _detection ??= GetComponent<EnemyDetection>();
            _combat ??= GetComponent<EnemyCombat>();
            _movementController ??= GetComponent<ActorMovementController>();
            _behaviorTreeRunner ??= GetComponent<BehaviorTreeRunner>();
            _monster = GetComponent<MonsterActor>();
            _spawnPosition = transform.position;
        }

        protected virtual void Start()
        {
            _maxAttackRange = _combat?.AttackData?.GetMaxAttackRange() ?? 3f;
            _lastAttackTime = -(_combat?.AttackData?.globalCooldown ?? 1f);

            ResetAllCounters();

            if (_behaviorTreeRunner == null || !_behaviorTreeRunner.IsRunning)
                Debug.LogWarning($"[EnemyFlyingAIController] {gameObject.name}에 실행 중인 BehaviorTreeRunner가 없습니다. Phase 7 이후 비행형 의사결정은 BT가 담당합니다.", this);
        }

        private void Update()
        {
            string stateName = _movementController.CurrentState?.StateName;
            if (stateName is null or "Death") return;

            if (IsGroundCombatState(stateName))
                _groundTimer += Time.deltaTime;

        }

        #endregion

        #region State 콜백 — 공중 루프

        public override void OnAirAttackFinished()
        {
            _airAttackCount++;
        }

        /// <summary>
        /// AirCircle 안전장치(체류 시간 초과/공격 횟수 소진)에서 호출.
        /// TransitionToDescend를 통해 데이터 기반 Dive/Land 분기를 탄다.
        /// </summary>
        public override void OnAirCircleForceDescend()
        {
        }

        public override void OnDiveLanded()
        {
            ResetAllCounters();
        }

        #endregion

        #region State 콜백 — 지상 전투

        /// <summary>
        /// 지상 공격 완료 후 다음 행동 결정.
        /// EnemyAIController.DecidePostAttack와 유사하나 비행 루프와 연동.
        /// </summary>
        public override void OnGroundAttackFinished()
        {
            _groundAttackCount++;
            _lastAttackTime = Time.time;
        }

        /// <summary>
        /// Chase에서 매 프레임 호출. 거리/시간 기반 전환.
        /// </summary>
        public override void EvaluateChase()
        {
        }

        #endregion

        #region 내부 헬퍼

        public override bool ShouldTakeOff()
        {
            return _groundAttackCount >= _groundAttackLimit
                   || _groundTimer >= _currentGroundStayLimit;
        }

        public override bool CanUseSkill()
        {
            if (_combat?.AttackData == null) return false;
            return Time.time - _lastAttackTime >= _combat.AttackData.globalCooldown;
        }

        public override bool TryRequestAttackSlot()
        {
            // 비행형은 그룹 슬롯 시스템과 결합되지 않은 시점 — 항상 허용.
            // 추후 MonsterGroupController 도입 시 EnemyAIController과 동일하게 슬롯 요청으로 교체.
            return true;
        }

        public override void NotifyBTAttackStarted()
        {
            // 카운터 증가는 OnGroundAttackFinished / OnAirAttackFinished에서 처리하므로 여기서는 타임스탬프만 갱신.
            _lastAttackTime = Time.time;
        }

        protected virtual void TransitionToTakeOff()
        {
            _movementController.TransitionToState(new EnemyFlyingTakeOffState(_movementController, this));
        }

        /// <summary>
        /// 공중 루프 종료 시 하강 방식 결정. BT 미연결 폴백.
        /// BT가 직접 조립하려면 <see cref="HasDiveSkillAvailable"/> + <see cref="SelectAndSetDiveSkill"/>를 단계별로 호출한다.
        /// </summary>
        public override void TransitionToDescend()
        {
            if (!HasDiveSkillAvailable() || Random.value >= _diveChance || !SelectAndSetDiveSkill())
            {
                _movementController.TransitionToState(new EnemyFlyingLandState(_movementController, this));
                return;
            }

            _movementController.TransitionToState(new EnemyFlyingDiveState(_movementController, this));
        }

        /// <summary>
        /// 발사 가능한 dive 스킬이 있는지 판정. 타겟/AttackData 없으면 false.
        /// </summary>
        public override bool HasDiveSkillAvailable()
        {
            if (_detection == null || !_detection.HasTarget || _combat?.AttackData == null)
                return false;

            foreach (var skill in _combat.AttackData.skills)
            {
                if (skill.isDiveAttack && skill.IsUnlockedForLevel(_combat.CurrentLevel))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 가중치 기반 dive 스킬을 뽑아 Combat.CurrentSkill에 설정한다. 선택에 성공하면 true.
        /// </summary>
        public override bool SelectAndSetDiveSkill()
        {
            if (_combat?.AttackData == null)
                return false;

            _diveSkillBuffer.Clear();
            foreach (var skill in _combat.AttackData.skills)
            {
                if (skill.isDiveAttack && skill.IsUnlockedForLevel(_combat.CurrentLevel))
                    _diveSkillBuffer.Add(skill);
            }

            if (_diveSkillBuffer.Count == 0)
                return false;

            var selected = _combat.AttackData.SelectRandomAerialSkill(_diveSkillBuffer);
            if (selected == null)
                return false;

            _combat.SetCurrentSkill(selected);
            return true;
        }

        public override void ResetAllCounters()
        {
            _groundTimer = 0f;
            _groundAttackCount = 0;
            _airAttackCount = 0;
            _currentGroundStayLimit = Random.Range(_groundStayLimitMin, _groundStayLimitMax);
            _currentAirAttackLimit = Random.Range(_airAttackLimitMin, _airAttackLimitMax + 1); // +1: 상한 포함
        }

        public override void ResetAirCounters()
        {
            _airAttackCount = 0;
            _currentAirAttackLimit = Random.Range(_airAttackLimitMin, _airAttackLimitMax + 1);
        }

        public override Vector3 GetRandomPatrolPoint()
        {
            Vector2 c = Random.insideUnitCircle * _patrolRadius;
            return _spawnPosition + new Vector3(c.x, 0, c.y);
        }

        protected bool IsAirState(string s) => s is "Flying_AirCircle" or "Flying_TakeOff";
        protected bool IsGroundCombatState(string s) => s is "Flying_Chase" or "Flying_GroundAttack" or "Flying_Circle" or "Flying_Retreat";

        public void Freeze()
        {
            if (_movementController.CurrentState?.StateName == "Death") return;
            enabled = false;
            _movementController.TransitionToState(new EnemyIdleState(_movementController));
        }

        public void Unfreeze() => enabled = true;

        #endregion
    }
}
