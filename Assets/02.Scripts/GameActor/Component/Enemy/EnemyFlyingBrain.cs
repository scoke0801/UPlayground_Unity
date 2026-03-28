using UnityEngine;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Component
{
    /// <summary>
    /// 비행형 보스 전용 Brain.
    /// "지상 추격 → 근접 공격 → 이륙 → 공중 선회 + 투사체 → 급강하 → 반복"
    /// 
    /// 의사결정 구조:
    /// 1) Update에서 주기적으로 MakeDecision 실행 (EnemyBrain과 동일 패턴)
    /// 2) 각 State 완료 시 콜백으로 다음 전환 판단
    /// 두 경로가 공존하므로, State가 정상 완료되면 콜백이, 예외 상황이면 Decision이 처리한다.
    /// </summary>
    public class EnemyFlyingBrain : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyDetection _detection;
        [SerializeField] private EnemyCombat _combat;
        [SerializeField] private ActorMovementController _movementController;

        [Header("Ground Settings")]
        [SerializeField] private float _chaseStopDistance = 3.0f;
        [SerializeField] private float _chaseSpeedMultiplier = 1.2f;

        [Header("TakeOff Conditions")]
        [Tooltip("지상 체류 시간이 이 값을 초과하면 강제 이륙")]
        [SerializeField] private float _groundStayLimit = 10f;
        [Tooltip("지상 공격 횟수가 이 값에 도달하면 이륙")]
        [SerializeField] private int _groundAttackLimit = 2;

        [Header("Air Settings")]
        [Tooltip("공중 선회 반경")]
        [SerializeField] private float _airCircleRadius = 8f;
        [Tooltip("공중 선회 고도")]
        [SerializeField] private float _airHoverHeight = 6f;
        [Tooltip("공중 선회 속도")]
        [SerializeField] private float _airMoveSpeed = 5f;
        [Tooltip("공중 투사체 발사 횟수")]
        [SerializeField] private int _airAttackLimit = 2;

        [Header("Dive Settings")]
        [Tooltip("급강하 속도")]
        [SerializeField] private float _diveSpeed = 20f;
        [Tooltip("착지 충격 판정 반경")]
        [SerializeField] private float _diveImpactRadius = 3f;

        [Header("Decision")]
        [SerializeField] private float _decisionInterval = 0.15f;

        // 런타임 카운터
        private float _groundTimer;
        private int _groundAttackCount;
        private int _airAttackCount;
        private float _decisionTimer;

        private MonsterActor _monster;

        // 프로퍼티
        public EnemyDetection Detection => _detection;
        public EnemyCombat Combat => _combat;
        public float ChaseStopDistance => _chaseStopDistance;
        public float ChaseSpeedMultiplier => _chaseSpeedMultiplier;
        public float AirCircleRadius => _airCircleRadius;
        public float AirHoverHeight => _airHoverHeight;
        public float AirMoveSpeed => _airMoveSpeed;
        public int AirAttackLimit => _airAttackLimit;
        public float DiveSpeed => _diveSpeed;
        public float DiveImpactRadius => _diveImpactRadius;
        public float GroundTimer => _groundTimer;
        public int GroundAttackCount => _groundAttackCount;
        public int AirAttackCount => _airAttackCount;

        private void Awake()
        {
            _detection ??= GetComponent<EnemyDetection>();
            _combat ??= GetComponent<EnemyCombat>();
            _movementController ??= GetComponent<ActorMovementController>();
            _monster = GetComponent<MonsterActor>();
        }

        private void Start()
        {
            ResetGroundCounters();
            _movementController.TransitionToState(
                new EnemyFlyingChaseState(_movementController, this));
        }

        private void Update()
        {
            string stateName = _movementController.CurrentState?.StateName;
            if (stateName is null or "Death") return;

            // 지상 상태일 때만 타이머 누적
            if (stateName is "Flying_Chase" or "Flying_GroundAttack")
                _groundTimer += Time.deltaTime;

            // 주기적 의사결정
            _decisionTimer += Time.deltaTime;
            if (_decisionTimer >= _decisionInterval)
            {
                _decisionTimer = 0f;
                MakeDecision(stateName);
            }
        }

        #region Decision Loop

        /// <summary>
        /// 주기적 의사결정.
        /// 각 State의 콜백이 정상 흐름을 처리하므로, 여기서는 예외/보정만 담당한다.
        /// - 타겟 소실 시 Idle 복귀
        /// - Hit/Airborne 복귀 후 루프 재진입
        /// - 지상 체류 시간 초과 시 강제 이륙
        /// </summary>
        private void MakeDecision(string stateName)
        {
            // ── 개입 금지 State ─────────────────────────────────
            // 이 상태들은 자체 완료 로직이 있으므로 Brain이 끼어들지 않는다
            if (stateName is "Flying_GroundAttack" or "Flying_TakeOff"
                or "Flying_Dive" or "Hit" or "Grabbed")
                return;

            // ── 피격 복귀 처리 ─────────────────────────────────
            // EnemyAirborneState(피격 에어본)에서 착지 후 Idle로 빠지는 경우 → 루프 재진입
            if (stateName is "Idle")
            {
                ReenterLoop();
                return;
            }

            // ── 타겟 소실 ────────────────────────────────────
            if (!_detection.HasTarget)
            {
                // 공중이면 강제 Dive로 내려온다
                if (IsAirState(stateName))
                {
                    _movementController.TransitionToState(
                        new EnemyFlyingDiveState(_movementController, this));
                    return;
                }

                // 지상이면 Idle 대기
                if (stateName is not "Idle" and not "Patrol")
                {
                    _movementController.TransitionToState(
                        new EnemyIdleState(_movementController));
                }
                return;
            }

            // ── 지상 Chase 중 강제 이륙 판단 ─────────────────
            if (stateName == "Flying_Chase")
            {
                // 시간 초과 시 강제 이륙 (EvaluateChase에서도 체크하지만 안전장치)
                if (_groundTimer >= _groundStayLimit)
                {
                    TransitionToTakeOff();
                    return;
                }
            }

            // ── 공중 AirCircle 체류 안전장치 ────────────────
            // AirCircle이 공격 실패 등으로 무한 체류하는 것 방지
            if (stateName == "Flying_AirCircle" && _airAttackCount >= _airAttackLimit)
            {
                _movementController.TransitionToState(
                    new EnemyFlyingDiveState(_movementController, this));
            }
        }

        /// <summary>
        /// 피격/에어본 복귀 후 현재 루프 위치에 맞게 재진입.
        /// 공격 카운터 상태를 보고 적절한 State로 돌아간다.
        /// </summary>
        private void ReenterLoop()
        {
            if (!_detection.HasTarget)
            {
                _movementController.TransitionToState(new EnemyIdleState(_movementController));
                return;
            }

            // 지상 공격을 충분히 했으면 이륙
            if (ShouldTakeOff())
            {
                TransitionToTakeOff();
                return;
            }

            // 기본: 추격으로 복귀
            _movementController.TransitionToState(
                new EnemyFlyingChaseState(_movementController, this));
        }

        private bool IsAirState(string stateName)
        {
            return stateName is "Flying_AirCircle" or "Flying_TakeOff";
        }

        #endregion

        #region State 콜백 (정상 흐름)

        /// <summary>
        /// 지상 공격 후 호출. 이륙 조건 판단.
        /// </summary>
        public void OnGroundAttackFinished()
        {
            _groundAttackCount++;

            if (ShouldTakeOff())
            {
                TransitionToTakeOff();
                return;
            }

            _movementController.TransitionToState(
                new EnemyFlyingChaseState(_movementController, this));
        }

        /// <summary>
        /// Chase State에서 매 프레임 호출. 거리 진입 → 공격, 시간 초과 → 이륙.
        /// </summary>
        public void EvaluateChase()
        {
            if (!_detection.HasTarget) return;

            float dist = _detection.DistanceToTarget;

            if (_groundTimer >= _groundStayLimit)
            {
                TransitionToTakeOff();
                return;
            }

            if (dist <= _chaseStopDistance && _combat.HasAvailableSkillAtDistance(dist))
            {
                _movementController.TransitionToState(
                    new EnemyFlyingGroundAttackState(_movementController, this));
            }
        }

        /// <summary>
        /// 공중 공격 모션 완료 후 호출. Dive 조건 판단.
        /// </summary>
        public void OnAirAttackFinished()
        {
            _airAttackCount++;

            if (_airAttackCount >= _airAttackLimit)
            {
                _movementController.TransitionToState(
                    new EnemyFlyingDiveState(_movementController, this));
            }
            // 아직 남았으면 AirCircle State가 자체적으로 다음 발사 진행
        }

        /// <summary>
        /// Dive 착지 완료 후 호출. 루프 시작으로 복귀.
        /// </summary>
        public void OnDiveLanded()
        {
            ResetGroundCounters();
            _movementController.TransitionToState(
                new EnemyFlyingChaseState(_movementController, this));
        }

        #endregion

        #region 내부 헬퍼

        private bool ShouldTakeOff()
        {
            return _groundAttackCount >= _groundAttackLimit
                   || _groundTimer >= _groundStayLimit;
        }

        private void TransitionToTakeOff()
        {
            _movementController.TransitionToState(
                new EnemyFlyingBossTakeOffState(_movementController, this));
        }

        public void ResetGroundCounters()
        {
            _groundTimer = 0f;
            _groundAttackCount = 0;
        }

        public void ResetAirCounters()
        {
            _airAttackCount = 0;
        }

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
