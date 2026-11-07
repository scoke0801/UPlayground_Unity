using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 비행 몬스터 공중 선회 + 투사체 발사.
    /// 
    /// 카운트 관리:
    /// - Brain._airAttackCount가 유일한 카운터
    /// - 발사 모션 완료(또는 타임아웃) → Brain.OnAirAttackFinished()
    /// </summary>
    public class EnemyFlyingAirCircleState : EnemyActorState
    {
        public override ActorStateId StateId => ActorStateId.Flying_AirCircle;

        /// <summary>
        /// 커밋된 공격 모션이 아니라 홀딩 패턴이다. 이 상태를 벗어나는 결정(급강하 / 착지)은
        /// BT가 내려야 하므로 BT를 막지 않는다. 막으면 TransitionFlyingEnemyStateNode가
        /// 차단 상태에서 Failure를 반환해 영구히 선회만 하게 된다.
        /// 지상 짝인 EnemyCircleState가 EnemyActionResolver에서 locomotion으로 취급되는 것과
        /// 같은 규약이다. 발사 모션 자체의 보호는 State 내부 _isAttacking이 담당한다.
        /// </summary>
        public override bool BlocksBehaviorTree => false;
        public override GravityOwnership GravityOwner => GravityOwnership.None;

        private readonly EnemyFlyingAIContext _brain;

        // 선회
        private float _orbitAngle;
        private float _orbitDirection;
        private float _dirChangeTimer;
        private float _nextDirChangeTime;

        // 공격 타이밍
        private float _attackCooldown;
        private bool _isAttacking;

        // 고도 유지
        private float _verticalVelocity;
        private float _currentHoverHeight; // 매 진입마다 랜덤 결정
        private float _currentMaxStay;    // 매 진입마다 랜덤 결정

        // 전체 체류 타임아웃 — 어떤 이유로든 빠져나가지 못하는 상황 방지
        private float _totalTimer;

        private const float FirstShotDelay = 0.5f;       // SO 폴백용 기본값
        private const float ShotInterval = 0.8f;
        private const float AttackMotionTimeout = 3.0f;
        private const float MaxStayDuration = 8.0f;

        // SO에서 값을 읽되, SO 미할당 시 const 폴백
        private float Cfg_FirstShotDelay => _brain.FlyingSettings ? _brain.FlyingSettings.firstShotDelay : FirstShotDelay;
        private float Cfg_ShotInterval => _brain.FlyingSettings ? _brain.FlyingSettings.shotInterval : ShotInterval;
        private float Cfg_MotionTimeout => _brain.FlyingSettings ? _brain.FlyingSettings.attackMotionTimeout : AttackMotionTimeout;
        private float Cfg_DirChangeMin => _brain.FlyingSettings ? _brain.FlyingSettings.dirChangeTimeMin : 1.5f;
        private float Cfg_DirChangeMax => _brain.FlyingSettings ? _brain.FlyingSettings.dirChangeTimeMax : 3.5f;

        public EnemyFlyingAirCircleState(ActorMovementController controller, EnemyFlyingAIContext brain)
            : base(controller)
        {
            _brain = brain;
        }

        public override bool CanTransitionState(ActorStateId fromState) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _attackCooldown = Cfg_FirstShotDelay;
            _isAttacking = false;
            _verticalVelocity = 0f;
            _totalTimer = 0f;
            _orbitDirection = Random.value > 0.5f ? 1f : -1f;
            _dirChangeTimer = 0f;
            _nextDirChangeTime = Random.Range(Cfg_DirChangeMin, Cfg_DirChangeMax);

            // 고도 랜덤: Brain.AirHoverHeight ± SO.hoverHeightVariance
            float variance = _brain.FlyingSettings ? _brain.FlyingSettings.hoverHeightVariance : 1.5f;
            _currentHoverHeight = _brain.AirHoverHeight + Random.Range(-variance, variance);
            _currentHoverHeight = Mathf.Max(_currentHoverHeight, 2f); // 최소 2m

            // 체류 시간 랜덤
            float stayMin = _brain.FlyingSettings ? _brain.FlyingSettings.maxAirStayMin : 4f;
            float stayMax = _brain.FlyingSettings ? _brain.FlyingSettings.maxAirStayMax : MaxStayDuration;
            _currentMaxStay = Random.Range(stayMin, stayMax);

            if (_brain.Detection.HasTarget)
            {
                Vector3 offset = motor.TransientPosition - _brain.Detection.CurrentTarget.position;
                offset.y = 0;
                _orbitAngle = Mathf.Atan2(offset.z, offset.x);
            }

            motor.SetGroundSolvingActivation(false);
            gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Fly_Move, 0.2f);
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            CancelActiveAttack();
            _brain.ReleaseGroupSlot();
        }

        public override void UpdateState(float deltaTime)
        {
            _totalTimer += deltaTime;

            // ── 최대 체류 시간 초과 → 강제 Dive ──
            if (_totalTimer >= _currentMaxStay)
            {
                ForceDescend();
                return;
            }

            if (!_brain.Detection.HasTarget) return;
            
            // ── 공격 횟수 도달 → Dive ──
            if (_brain.AirAttackCount >= _brain.AirAttackLimit)
            {
                ForceDescend();
                return;
            }

            // ── 쿨다운 후 공격 ──
            _attackCooldown -= deltaTime;
            if (_attackCooldown <= 0f)
            {
                TryAerialAttack();
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (!_brain.Detection.HasTarget) return;

            Vector3 dir = (_brain.Detection.CurrentTarget.position - motor.TransientPosition);
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(dir.normalized);
                currentRotation = Quaternion.Slerp(currentRotation, target,
                    1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
            }
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!_brain.Detection.HasTarget)
            {
                currentVelocity = Vector3.zero;
                return;
            }

            Vector3 targetPos = _brain.Detection.CurrentTarget.position;

            // ── 공격 모션 중: 선회 정지, 고도만 유지 ──
            if (_isAttacking)
            {
                // 수평 감속 → 정지
                currentVelocity.x = Mathf.Lerp(currentVelocity.x, 0f, deltaTime * 8f);
                currentVelocity.z = Mathf.Lerp(currentVelocity.z, 0f, deltaTime * 8f);

                // 고도 유지
                float hoverY = targetPos.y + _currentHoverHeight;
                float hDiff = hoverY - motor.TransientPosition.y;
                _verticalVelocity += (hDiff * 6f - _verticalVelocity * 4f) * deltaTime;
                currentVelocity.y = _verticalVelocity;
                return;
            }

            // ── 선회 이동 ──
            float radius = _brain.AirCircleRadius;
            float speed = _brain.AirMoveSpeed;
            float angularSpeed = speed / radius;

            _dirChangeTimer += deltaTime;
            if (_dirChangeTimer >= _nextDirChangeTime)
            {
                _orbitDirection *= -1f;
                _dirChangeTimer = 0f;
                _nextDirChangeTime = Random.Range(Cfg_DirChangeMin, Cfg_DirChangeMax);
            }

            _orbitAngle += angularSpeed * _orbitDirection * deltaTime;

            Vector3 desiredPos = targetPos + new Vector3(
                Mathf.Cos(_orbitAngle) * radius,
                0f,
                Mathf.Sin(_orbitAngle) * radius);
            desiredPos.y = targetPos.y + _currentHoverHeight;

            Vector3 toDesired = desiredPos - motor.TransientPosition;
            Vector3 horizontalMove = new Vector3(toDesired.x, 0f, toDesired.z);
            float horizDist = horizontalMove.magnitude;

            Vector3 horizVel = horizDist > 0.5f
                ? horizontalMove.normalized * speed
                : horizontalMove * (speed / 0.5f);

            float heightDiff = desiredPos.y - motor.TransientPosition.y;
            _verticalVelocity += (heightDiff * 6f - _verticalVelocity * 4f) * deltaTime;

            currentVelocity = new Vector3(horizVel.x, _verticalVelocity, horizVel.z);
        }

        #region 공격

        /// <summary>
        /// 공중 공격 스킬 선택 + 모션 재생.
        /// 투사체 발사는 AnimEvent에서 처리 — 여기서는 모션만 제어한다.
        /// </summary>
        private void TryAerialAttack()
        {
            float dist = _brain.Detection.DistanceToTarget;
            if (!_brain.Combat.TrySelectAerialAbility(
                    dist,
                    false,
                    out var ability))
            {
                _attackCooldown = 0.5f;
                return;
            }

            // Combat에 Ability를 설정 — AnimEvent가 Payload의 공격 정보를 참조해 투사체를 발사한다.
            if (!_brain.Combat.SetCurrentAbility(ability))
                return;
            _isAttacking = true;

            AbilityAttackInfo currentSkill = _brain.Combat.CurrentSkill;
            var animState = currentSkill != null
                ? gameActor.Animator.PlayAbilityMotion(currentSkill.motionKey, 0.1f)
                : null;
            if (animState != null)
            {
                gameActor.Animator.OnMotionSetCompleted += OnAttackMotionEnd;
            }
            else
            {
                // 모션 없으면 즉시 완료 처리
                _isAttacking = false;
                _attackCooldown = Cfg_ShotInterval;
                _brain.Combat.CancelCurrentAbility();
                _brain.OnAirAttackFinished();
            }
        }

        private void OnAttackMotionEnd()
        {
            gameActor.Animator.OnMotionSetCompleted -= OnAttackMotionEnd;

            if (!_isAttacking) return;
            _isAttacking = false;
            _attackCooldown = Cfg_ShotInterval;
            _brain.Combat.CompleteCurrentAbility();

            // 선회 모션 복귀 + 방향 반전
            gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Fly_Move, 0.2f);
            _orbitDirection *= -1f;
            _dirChangeTimer = 0f;
            _nextDirChangeTime = Random.Range(Cfg_DirChangeMin, Cfg_DirChangeMax);

            _brain.OnAirAttackFinished();
        }

        /// <summary>
        /// 안전장치 하강 — Brain.TransitionToDescend를 호출하여
        /// 데이터 기반(Dive/Land) 분기를 타게 한다.
        /// </summary>
        private void ForceDescend()
        {
            // 강제 하강은 모션 완료 콜백을 버리는 경로이므로 활성 Ability도 함께 취소해야 한다.
            // _isAttacking만 먼저 내리면 OnExit의 정리도 건너뛰어 RejectNew 실행이 영구 잔류한다.
            CancelActiveAttack();
            _brain.OnAirCircleForceDescend();
        }

        private void CancelActiveAttack()
        {
            gameActor.Animator.OnMotionSetCompleted -= OnAttackMotionEnd;
            if (_isAttacking)
                _brain.Combat.CancelCurrentAbility();
            _isAttacking = false;
        }

        #endregion
    }
}
