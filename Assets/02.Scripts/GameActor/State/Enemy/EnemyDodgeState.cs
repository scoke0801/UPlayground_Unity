using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 액션성 회피 상태.
    /// 짧은 무적 시간 동안 타겟 공격 축에서 벗어나며, 방향성 Dodge(Dodge_F/B/L/R) → Dodge 순으로 모션을 선택한다.
    /// 둘 다 정의되지 않은 액터는 이 상태로 진입할 수 없다(EnemyActionResolver에서 차단).
    /// </summary>
    public class EnemyDodgeState : GameActorState
    {
        public override string StateName => "Dodge";
        public override bool BlocksBehaviorTree => true;
        public override bool GrantsInvincibility => true;
        public override bool SuppressesHitReaction => true;

        private readonly EnemyAIContext _context;
        private readonly EnemyDetection _detection;

        private Vector3 _dodgeDirection;
        private float _dodgeTimer;
        private bool _motionEnded;
        private float _motionLockDuration;
        private float _movementDuration;

        private const float DODGE_DURATION = 0.35f;
        private const float FALLBACK_DODGE_LOCK_DURATION = 0.45f;
        private const float DODGE_SPEED_RATIO = 1.85f;
        private const float WALL_REDIRECT_MIN_DOT = -0.35f;

        /// <summary>
        /// 이 액터가 Dodge 상태를 실행할 수 있는지 — Dodge_F/B/L/R 또는 Dodge 모션이 하나라도 있어야 한다.
        /// </summary>
        public static bool CanExecute(GameActor actor)
        {
            var animator = actor?.Animator;
            if (animator == null) return false;
            return animator.HasMotion(AnimKey.Dodge_F)
                || animator.HasMotion(AnimKey.Dodge_B)
                || animator.HasMotion(AnimKey.Dodge_L)
                || animator.HasMotion(AnimKey.Dodge_R)
                || animator.HasMotion(AnimKey.Dodge);
        }

        public EnemyDodgeState(
            ActorMovementController controller,
            EnemyAIContext context,
            EnemyDetection detection) : base(controller)
        {
            _context = context;
            _detection = detection;
        }

        public override bool CanTransitionState(string stateName)
        {
            return stateName is not ("Death" or "Grabbed");
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _dodgeTimer = 0f;
            _motionEnded = false;
            _movementDuration = DODGE_DURATION;
            _motionLockDuration = FALLBACK_DODGE_LOCK_DURATION;
            _dodgeDirection = CalculateDodgeDirection();

            // 방향성 키 우선 → Dodge → 가용한 다른 방향성 순으로 폴백.
            // CanExecute가 최소 1개 보유를 보장하므로 None이 반환되지 않는다.
            AnimKey directionalKey = EnemyLocomotionHelper.ResolveDirectionalKey(
                _dodgeDirection,
                gameActor.transform,
                AnimKey.Dodge_F,
                AnimKey.Dodge_B,
                AnimKey.Dodge_L,
                AnimKey.Dodge_R);

            AnimKey motionKey = EnemyLocomotionHelper.PickFirstAvailable(
                gameActor.Animator,
                directionalKey,
                AnimKey.Dodge,
                AnimKey.Dodge_B,
                AnimKey.Dodge_L,
                AnimKey.Dodge_R,
                AnimKey.Dodge_F);

            if (motionKey != AnimKey.None)
            {
                var duration = gameActor.Animator.GetMotionSetDuration(motionKey);
                _motionLockDuration = duration > 0f
                    ? Mathf.Max(DODGE_DURATION, duration)
                    : FALLBACK_DODGE_LOCK_DURATION;

                gameActor.Animator.OnMotionSetCompleted += OnDodgeMotionCompleted;
                gameActor.Animator.PlayMotion(motionKey, 0.05f);
            }
            else
            {
                _motionEnded = true;
            }
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetCompleted -= OnDodgeMotionCompleted;
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }

            _dodgeTimer += deltaTime;

            if (_dodgeTimer < _movementDuration)
                return;

            if (!_motionEnded && _dodgeTimer < _motionLockDuration)
                return;

            if (!_detection.HasTarget)
            {
                controller.TransitionToState(new EnemyIdleState(controller));
                return;
            }

            var distance = _detection.DistanceToTarget;
            if (distance <= _context.OptimalCombatDistance)
            {
                controller.TransitionToState(
                    new EnemyCircleState(controller, _context, _detection, _context.CircleDuration * 0.5f));
            }
            else
            {
                controller.TransitionToState(new EnemyChaseState(controller, _context, _detection));
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (!_detection.HasTarget)
                return;

            var dirToTarget = _detection.CurrentTarget.position - motor.TransientPosition;
            dirToTarget.y = 0f;
            if (dirToTarget.sqrMagnitude <= 0.01f)
                return;

            currentRotation = Quaternion.Slerp(
                currentRotation,
                Quaternion.LookRotation(dirToTarget.normalized),
                1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity += controller.Gravity * deltaTime;
                return;
            }

            var normalizedTime = Mathf.Clamp01(_dodgeTimer / _movementDuration);
            var speedScale = 1f - normalizedTime * normalizedTime;
            var targetVelocity = _dodgeDirection * (controller.MaxRunMoveSpeed * DODGE_SPEED_RATIO * speedScale);

            targetVelocity = motor.GetDirectionTangentToSurface(
                targetVelocity,
                motor.GroundingStatus.GroundNormal) * targetVelocity.magnitude;

            currentVelocity = Vector3.Lerp(
                currentVelocity,
                targetVelocity,
                1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }

        public override void OnMovementHit(
            Collider hitCollider,
            Vector3 hitNormal,
            Vector3 hitPoint,
            ref KinematicCharacterController.HitStabilityReport hitStabilityReport)
        {
            var dot = Vector3.Dot(_dodgeDirection, hitNormal);
            if (dot >= WALL_REDIRECT_MIN_DOT)
                return;

            _dodgeDirection = Vector3.ProjectOnPlane(_dodgeDirection, hitNormal).normalized;
            if (_dodgeDirection.sqrMagnitude <= 0.01f)
                _dodgeDirection = CalculateDodgeDirection();
        }

        private Vector3 CalculateDodgeDirection()
        {
            if (!_detection.HasTarget)
                return -gameActor.transform.forward;

            var toTarget = _detection.CurrentTarget.position - motor.TransientPosition;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.01f)
                return -gameActor.transform.forward;

            var dirToTarget = toTarget.normalized;
            var away = -dirToTarget;
            var side = Random.value > 0.5f ? 1f : -1f;
            var lateral = Vector3.Cross(Vector3.up, dirToTarget).normalized * side;

            var tooClose = _detection.DistanceToTarget <= _context.PersonalSpaceDistance + 0.4f;
            var direction = tooClose
                ? away * 0.75f + lateral * 0.25f
                : lateral * 0.8f + away * 0.2f;

            direction.y = 0f;
            return direction.sqrMagnitude > 0.01f ? direction.normalized : away;
        }

        private void OnDodgeMotionCompleted()
        {
            if (controller.CurrentState == this)
                _motionEnded = true;
        }
    }
}
