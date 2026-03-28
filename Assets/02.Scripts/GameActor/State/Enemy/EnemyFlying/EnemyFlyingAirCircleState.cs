using UnityEngine;
using UPlayGround.Component;
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
    public class EnemyFlyingAirCircleState : GameActorState
    {
        public override string StateName => "Flying_AirCircle";
        public override bool AdjustGravity => false;

        private readonly EnemyFlyingBrain _brain;

        // 선회
        private float _orbitAngle;
        private float _orbitDirection;

        // 공격 타이밍
        private float _attackCooldown;
        private bool _isAttacking;
        private float _attackMotionTimer; // 모션 미완료 대비 타임아웃

        // 고도 유지
        private float _verticalVelocity;

        // 전체 체류 타임아웃 — 어떤 이유로든 빠져나가지 못하는 상황 방지
        private float _totalTimer;

        private const float FirstShotDelay = 1.0f;
        private const float ShotInterval = 1.5f;
        private const float AttackMotionTimeout = 3.0f; // 모션이 이 시간 안에 안 끝나면 강제 완료
        private const float MaxStayDuration = 15.0f;    // 최대 체류 시간

        public EnemyFlyingAirCircleState(ActorMovementController controller, EnemyFlyingBrain brain)
            : base(controller)
        {
            _brain = brain;
        }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _attackCooldown = FirstShotDelay;
            _isAttacking = false;
            _attackMotionTimer = 0f;
            _verticalVelocity = 0f;
            _totalTimer = 0f;
            _orbitDirection = Random.value > 0.5f ? 1f : -1f;

            if (_brain.Detection.HasTarget)
            {
                Vector3 offset = motor.TransientPosition - _brain.Detection.CurrentTarget.position;
                offset.y = 0;
                _orbitAngle = Mathf.Atan2(offset.z, offset.x);
            }

            motor.SetGroundSolvingActivation(false);
            gameActor.Animator.PlayMotion(AnimKey.Fly_Move, 0.2f);

            Debug.Log($"[AirCircle] OnEnter — airAttackCount={_brain.AirAttackCount}/{_brain.AirAttackLimit}");
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            gameActor.Animator.OnMotionSetCompleted -= OnAttackMotionEnd;
            _isAttacking = false;
            Debug.Log($"[AirCircle] OnExit → {toState.StateName}");
        }

        public override void UpdateState(float deltaTime)
        {
            _totalTimer += deltaTime;

            // ── 최대 체류 시간 초과 → 강제 Dive ──
            if (_totalTimer >= MaxStayDuration)
            {
                Debug.LogWarning("[AirCircle] 최대 체류 시간 초과, 강제 Dive");
                ForceDive();
                return;
            }

            if (!_brain.Detection.HasTarget) return;

            // ── 공격 모션 중 ──
            if (_isAttacking)
            {
                _attackMotionTimer += deltaTime;

                // 모션이 일정 시간 안에 안 끝나면 강제 완료
                if (_attackMotionTimer >= AttackMotionTimeout)
                {
                    Debug.LogWarning("[AirCircle] 공격 모션 타임아웃, 강제 완료");
                    ForceEndAttackMotion();
                }
                return;
            }

            // ── 발사 횟수 도달 → Dive ──
            if (_brain.AirAttackCount >= _brain.AirAttackLimit)
            {
                Debug.Log($"[AirCircle] 발사 완료 ({_brain.AirAttackCount}/{_brain.AirAttackLimit}), Dive 전환");
                ForceDive();
                return;
            }

            // ── 쿨다운 후 발사 ──
            _attackCooldown -= deltaTime;
            if (_attackCooldown <= 0f)
            {
                TryFireProjectile();
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
            float radius = _brain.AirCircleRadius;
            float speed = _brain.AirMoveSpeed;

            float angularSpeed = speed / radius;
            _orbitAngle += angularSpeed * _orbitDirection * deltaTime;

            Vector3 desiredPos = targetPos + new Vector3(
                Mathf.Cos(_orbitAngle) * radius,
                0f,
                Mathf.Sin(_orbitAngle) * radius);
            desiredPos.y = targetPos.y + _brain.AirHoverHeight;

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

        private void TryFireProjectile()
        {
            float dist = _brain.Detection.DistanceToTarget;
            var aerialSkills = _brain.Combat.AttackData?.GetAvailableAerialSkills(dist);
            if (aerialSkills == null || aerialSkills.Count == 0)
            {
                _attackCooldown = 0.5f;
                Debug.LogWarning("[AirCircle] 공중 스킬 없음 — 0.5초 후 재시도");
                return;
            }

            var skill = _brain.Combat.AttackData.SelectRandomAerialSkill(aerialSkills);
            if (skill == null) return;

            _brain.Combat.SetCurrentSkill(skill);
            _isAttacking = true;
            _attackMotionTimer = 0f;

            Debug.Log($"[AirCircle] 공격 모션 시작: {skill.baseInfo.animKey}");

            var animState = gameActor.Animator.PlayMotion(skill.baseInfo.animKey, 0.1f);
            if (animState != null)
            {
                gameActor.Animator.OnMotionSetCompleted += OnAttackMotionEnd;
            }
            else
            {
                Debug.LogWarning("[AirCircle] 공격 모션 재생 실패, 즉시 완료 처리");
                _isAttacking = false;
                _attackCooldown = ShotInterval;
                _brain.OnAirAttackFinished();
            }
        }

        private void OnAttackMotionEnd()
        {
            gameActor.Animator.OnMotionSetCompleted -= OnAttackMotionEnd;

            if (!_isAttacking) return; // 이미 타임아웃으로 처리됨
            _isAttacking = false;
            _attackCooldown = ShotInterval;

            Debug.Log($"[AirCircle] 공격 모션 완료, Brain에 알림 (현재 count={_brain.AirAttackCount})");

            gameActor.Animator.PlayMotion(AnimKey.Fly_Move, 0.2f);
            _brain.OnAirAttackFinished();
        }

        /// <summary> 모션 타임아웃 시 강제 완료 </summary>
        private void ForceEndAttackMotion()
        {
            gameActor.Animator.OnMotionSetCompleted -= OnAttackMotionEnd;
            _isAttacking = false;
            _attackCooldown = ShotInterval;

            gameActor.Animator.PlayMotion(AnimKey.Fly_Move, 0.2f);
            _brain.OnAirAttackFinished();
        }

        /// <summary> 체류 한계 시 직접 Dive 전환 (Brain 콜백 우회) </summary>
        private void ForceDive()
        {
            gameActor.Animator.OnMotionSetCompleted -= OnAttackMotionEnd;
            _isAttacking = false;

            controller.TransitionToState(
                new EnemyFlyingDiveState(controller, _brain));
        }

        #endregion
    }
}
