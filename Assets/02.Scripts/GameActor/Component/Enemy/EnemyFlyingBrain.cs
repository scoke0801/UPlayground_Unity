using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Group;
using UPlayGround.MovementController;
using UPlayGround.State;
using Random = UnityEngine.Random;

namespace UPlayGround.Component
{
    /// <summary>
    /// 비행형 몬스터 전용 Brain.
    /// 
    /// ■ 핵심 루프: 지상 추격/공격 → 이륙 → 공중 선회+투사체 → 급강하 or 착지 → 반복
    /// ■ 지상 행동: EnemyBrain과 유사한 Idle/Patrol/Chase/Circle/Retreat + 비행 전용 분기
    /// ■ 공중 행동: TakeOff → AirCircle(투사체) → Dive or Land
    ///
    /// 의사결정:
    /// 1) MakeDecision (0.15초 주기) — 지상 전투 판단 + 예외 복구
    /// 2) State 콜백 — 공중 루프의 정상 흐름 전환
    /// </summary>
    public class EnemyFlyingBrain : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyDetection _detection;
        [SerializeField] private EnemyCombat _combat;
        [SerializeField] private ActorMovementController _movementController;
        [SerializeField] private EnemyFlyingSettingsSO _flyingSettings;

        [Header("Ground Combat")]
        [SerializeField] private float _chaseStopDistance = 2f;
        [SerializeField] private float _chaseSpeedMultiplier = 1.2f;
        [SerializeField] private float _optimalCombatDistance = 1.5f;
        [SerializeField] private float _minCombatDistance = 1.5f;
        [SerializeField] private float _personalSpaceDistance = 0.8f;

        [Header("Ground Post-Attack Behavior")]
        [Range(0f, 1f)] [SerializeField] private float _circleChance = 0.3f;
        [Range(0f, 1f)] [SerializeField] private float _retreatChance = 0.2f;
        [SerializeField] private float _circleDuration = 2.0f;
        [SerializeField] private float _retreatDistance = 3.0f;

        [Header("TakeOff Conditions")]
        [Tooltip("지상 체류 시간 초과 시 강제 이륙 (랜덤 범위)")]
        [SerializeField] private float _groundStayLimitMin = 7f;
        [SerializeField] private float _groundStayLimitMax = 12f;
        [Tooltip("지상 공격 횟수가 이 값에 도달하면 이륙")]
        [SerializeField] private int _groundAttackLimit = 2;

        [Header("Patrol")]
        [SerializeField] private bool _enablePatrol = true;
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

        [Header("Decision")]
        [SerializeField] private float _decisionInterval = 0.15f;

        // ── 런타임 ──
        private float _groundTimer;
        private float _currentGroundStayLimit; // 매 루프마다 랜덤 결정
        private int _currentAirAttackLimit;     // 매 공중 루프마다 랜덤 결정
        private int _groundAttackCount;
        private int _airAttackCount;
        private float _decisionTimer;
        private float _lastAttackTime;
        private float _maxAttackRange;
        private Vector3 _spawnPosition;

        private MonsterActor _monster;

        // ── 프로퍼티 (State에서 접근) ──
        public EnemyDetection Detection => _detection;
        public EnemyCombat Combat => _combat;
        public float ChaseStopDistance => _chaseStopDistance;
        public float ChaseSpeedMultiplier => _chaseSpeedMultiplier;
        public float AirCircleRadius => _airCircleRadius;
        public float AirHoverHeight => _airHoverHeight;
        public float AirMoveSpeed => _airMoveSpeed;
        public int AirAttackLimit => _currentAirAttackLimit;
        public float DiveSpeed => _diveSpeed;
        public float DiveImpactRadius => _diveImpactRadius;
        public float GroundTimer => _groundTimer;
        public int GroundAttackCount => _groundAttackCount;
        public int AirAttackCount => _airAttackCount;

        // Patrol/Circle/Retreat용 (EnemyBrain 호환)
        public float PatrolRadius => _patrolRadius;
        public float PatrolWaitTime => _patrolWaitTime;
        public bool EnablePatrol => _enablePatrol;
        public Vector3 SpawnPosition => _spawnPosition;
        public float CircleDuration => _circleDuration;
        public float RetreatDistance => _retreatDistance;
        public float OptimalCombatDistance => _optimalCombatDistance;

        /// <summary> State들이 튜닝 값에 접근하는 단일 창구 </summary>
        public EnemyFlyingSettingsSO FlyingSettings => _flyingSettings;

        #region Mono

        private void Awake()
        {
            _detection ??= GetComponent<EnemyDetection>();
            _combat ??= GetComponent<EnemyCombat>();
            _movementController ??= GetComponent<ActorMovementController>();
            _monster = GetComponent<MonsterActor>();
            _spawnPosition = transform.position;
        }

        private void Start()
        {
            _maxAttackRange = _combat?.AttackData?.GetMaxAttackRange() ?? 3f;
            _lastAttackTime = -(_combat?.AttackData?.globalCooldown ?? 1f);

            ResetAllCounters();

            // 타겟이 없으면 Patrol/Idle, 있으면 Chase
            if (_detection.HasTarget)
                _movementController.TransitionToState(new EnemyFlyingChaseState(_movementController, this));
            else if (_enablePatrol)
                _movementController.TransitionToState(new EnemyFlyingPatrolState(_movementController, this));
            else
                _movementController.TransitionToState(new EnemyIdleState(_movementController));
        }

        private void Update()
        {
            string stateName = _movementController.CurrentState?.StateName;
            if (stateName is null or "Death") return;

            if (IsGroundCombatState(stateName))
                _groundTimer += Time.deltaTime;

            _decisionTimer += Time.deltaTime;
            if (_decisionTimer >= _decisionInterval)
            {
                _decisionTimer = 0f;
                MakeDecision(stateName);
            }
        }

        #endregion

        #region Decision Loop

        private void MakeDecision(string stateName)
        {
            // ── 개입 금지 ──
            if (stateName is "Flying_GroundAttack" or "Flying_TakeOff"
                or "Flying_Dive" or "Flying_Land" or "Hit" or "Grabbed" or "Death")
                return;

            // ── 피격 복귀 ──
            if (stateName is "Idle" or "Airborne")
            {
                ReenterLoop();
                return;
            }

            // ── 타겟 소실 ──
            if (!_detection.HasTarget)
            {
                if (IsAirState(stateName))
                {
                    TransitionToDescend();
                    return;
                }

                if (stateName is not "Idle" and not "Patrol" and not "Flying_Patrol")
                {
                    if (_enablePatrol)
                        _movementController.TransitionToState(new EnemyFlyingPatrolState(_movementController, this));
                    else
                        _movementController.TransitionToState(new EnemyIdleState(_movementController));
                }
                return;
            }

            // ── 비전투 → 전투 전환 (Patrol 중 타겟 발견) ──
            if (stateName is "Patrol" or "Flying_Patrol")
            {
                _movementController.TransitionToState(new EnemyFlyingChaseState(_movementController, this));
                return;
            }

            // ── 지상 Chase: 이륙 타이머 + 공격 거리 판단 ──
            if (stateName == "Flying_Chase")
            {
                if (_groundTimer >= _currentGroundStayLimit)
                {
                    TransitionToTakeOff();
                    return;
                }
            }

            // ── Circle/Retreat 중 이륙 조건 달성 ──
            if (stateName is "Flying_Circle" or "Flying_Retreat")
            {
                if (ShouldTakeOff())
                {
                    TransitionToTakeOff();
                    return;
                }
            }

            // ── AirCircle 안전장치 ──
            if (stateName == "Flying_AirCircle" && _airAttackCount >= _currentAirAttackLimit)
            {
                TransitionToDescend();
            }
        }

        private void ReenterLoop()
        {
            if (!_detection.HasTarget)
            {
                if (_enablePatrol)
                    _movementController.TransitionToState(new EnemyFlyingPatrolState(_movementController, this));
                else
                    _movementController.TransitionToState(new EnemyIdleState(_movementController));
                return;
            }

            if (ShouldTakeOff())
            {
                TransitionToTakeOff();
                return;
            }

            _movementController.TransitionToState(new EnemyFlyingChaseState(_movementController, this));
        }

        #endregion

        #region State 콜백 — 공중 루프

        public void OnAirAttackFinished()
        {
            _airAttackCount++;
            if (_airAttackCount >= _currentAirAttackLimit)
                TransitionToDescend();
        }

        /// <summary>
        /// AirCircle 안전장치(체류 시간 초과/공격 횟수 소진)에서 호출.
        /// TransitionToDescend를 통해 데이터 기반 Dive/Land 분기를 탄다.
        /// </summary>
        public void OnAirCircleForceDescend()
        {
            TransitionToDescend();
        }

        public void OnDiveLanded()
        {
            ResetAllCounters();
            _movementController.TransitionToState(new EnemyFlyingChaseState(_movementController, this));
        }

        #endregion

        #region State 콜백 — 지상 전투

        /// <summary>
        /// 지상 공격 완료 후 다음 행동 결정.
        /// EnemyBrain.DecidePostAttack와 유사하나 비행 루프와 연동.
        /// </summary>
        public void OnGroundAttackFinished()
        {
            _groundAttackCount++;
            _lastAttackTime = Time.time;

            // 이륙 조건 도달 → TakeOff
            if (ShouldTakeOff())
            {
                TransitionToTakeOff();
                return;
            }

            // 아직 이륙 안 함 → 지상 전투 행동 분기
            DecidePostGroundAttack();
        }

        /// <summary>
        /// Chase에서 매 프레임 호출. 거리/시간 기반 전환.
        /// </summary>
        public void EvaluateChase()
        {
            if (!_detection.HasTarget) return;

            float dist = _detection.DistanceToTarget;

            // 시간 초과 → 이륙
            if (_groundTimer >= _currentGroundStayLimit)
            {
                TransitionToTakeOff();
                return;
            }

            // personalSpace 침범 → 후퇴
            if (dist < _personalSpaceDistance)
            {
                _movementController.TransitionToState(
                    new EnemyFlyingRetreatState(_movementController, this));
                return;
            }

            // 공격 사거리 진입
            if (dist <= _chaseStopDistance && CanUseSkill() && _combat.HasAvailableSkillAtDistance(dist))
            {
                _movementController.TransitionToState(
                    new EnemyFlyingGroundAttackState(_movementController, this));
            }
        }

        /// <summary>
        /// 공격 후 지상 행동 가중치 분기.
        /// Chase(기본) / Circle(선회) / Retreat(후퇴) 중 선택.
        /// </summary>
        private void DecidePostGroundAttack()
        {
            if (!_detection.HasTarget)
            {
                _movementController.TransitionToState(new EnemyIdleState(_movementController));
                return;
            }

            float dist = _detection.DistanceToTarget;
            float roll = Random.value;

            // 가까우면 후퇴 확률 증가
            if (dist < _minCombatDistance && roll < _retreatChance + 0.2f)
            {
                _movementController.TransitionToState(
                    new EnemyFlyingRetreatState(_movementController, this));
                return;
            }

            // Circle
            if (roll < _circleChance)
            {
                float duration = _circleDuration * Random.Range(0.4f, 0.8f);
                _movementController.TransitionToState(
                    new EnemyFlyingCircleState(_movementController, this, duration));
                return;
            }

            // Retreat
            if (roll < _circleChance + _retreatChance)
            {
                _movementController.TransitionToState(
                    new EnemyFlyingRetreatState(_movementController, this));
                return;
            }

            // 기본: 다시 Chase
            _movementController.TransitionToState(new EnemyFlyingChaseState(_movementController, this));
        }

        #endregion

        #region 내부 헬퍼

        private bool ShouldTakeOff()
        {
            return _groundAttackCount >= _groundAttackLimit
                   || _groundTimer >= _currentGroundStayLimit;
        }

        private bool CanUseSkill()
        {
            if (_combat?.AttackData == null) return false;
            return Time.time - _lastAttackTime >= _combat.AttackData.globalCooldown;
        }

        private void TransitionToTakeOff()
        {
            _movementController.TransitionToState(new EnemyFlyingTakeOffState(_movementController, this));
        }

        /// <summary>
        /// 공중 루프 종료 시 하강 방식 결정.
        /// isDiveAttack 스킬이 있으면 가중치 확률로 Dive, 없으면 Land.
        /// Dive 스킬이 선택되면 해당 스킬을 Combat에 설정하여 DiveState가 참조.
        /// </summary>
        private void TransitionToDescend()
        {
            if (!_detection.HasTarget || _combat?.AttackData == null)
            {
                _movementController.TransitionToState(new EnemyFlyingLandState(_movementController, this));
                return;
            }

            // isDiveAttack 스킬만 수집
            float dist = _detection.DistanceToTarget;
            var diveSkills = new System.Collections.Generic.List<EnemyAttackInfo>();
            foreach (var skill in _combat.AttackData.skills)
            {
                if (skill.isDiveAttack)
                    diveSkills.Add(skill);
            }

            if (diveSkills.Count == 0)
            {
                // Dive 스킬이 없으면 항상 일반 착지
                _movementController.TransitionToState(new EnemyFlyingLandState(_movementController, this));
                return;
            }

            // _diveChance 확률로 Dive 시도
            if (Random.value >= _diveChance)
            {
                _movementController.TransitionToState(new EnemyFlyingLandState(_movementController, this));
                return;
            }

            // 가중치 기반 Dive 스킬 선택
            var selected = _combat.AttackData.SelectRandomAerialSkill(diveSkills);
            if (selected != null)
            {
                _combat.SetCurrentSkill(selected);
                _movementController.TransitionToState(new EnemyFlyingDiveState(_movementController, this));
            }
            else
            {
                _movementController.TransitionToState(new EnemyFlyingLandState(_movementController, this));
            }
        }

        public void ResetAllCounters()
        {
            _groundTimer = 0f;
            _groundAttackCount = 0;
            _airAttackCount = 0;
            _currentGroundStayLimit = Random.Range(_groundStayLimitMin, _groundStayLimitMax);
            _currentAirAttackLimit = Random.Range(_airAttackLimitMin, _airAttackLimitMax + 1); // +1: 상한 포함
        }

        public void ResetAirCounters()
        {
            _airAttackCount = 0;
            _currentAirAttackLimit = Random.Range(_airAttackLimitMin, _airAttackLimitMax + 1);
        }

        public Vector3 GetRandomPatrolPoint()
        {
            Vector2 c = Random.insideUnitCircle * _patrolRadius;
            return _spawnPosition + new Vector3(c.x, 0, c.y);
        }

        private bool IsAirState(string s) => s is "Flying_AirCircle" or "Flying_TakeOff";
        private bool IsGroundCombatState(string s) => s is "Flying_Chase" or "Flying_GroundAttack" or "Flying_Circle" or "Flying_Retreat";

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
