using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 액션성 회피 상태.
    /// 짧은 무적 시간 동안 타겟 공격 축에서 벗어난다.
    /// 계산된 방향의 Dodge_F/B/L/R 모션을 우선 사용하고, 없으면 기본 Dodge 모션으로 실행한다.
    /// </summary>
    public class EnemyDodgeState : EnemyActorState
    {
        public override string StateName => "Dodge";
        public override bool BlocksBehaviorTree => true;
        public override bool GrantsInvincibility => true;
        public override bool SuppressesHitReaction => true;

        private readonly EnemyAIContext _context;
        private readonly EnemyDetection _detection;
        private readonly AnimKey _motionKey;

        private Vector3 _dodgeDirection;
        private float _dodgeTimer;
        private bool _motionEnded;
        private float _motionLockDuration;
        private float _movementDuration;

        private const float DODGE_DURATION = 0.32f;
        private const float FALLBACK_DODGE_LOCK_DURATION = 0.45f;
        private const float DODGE_SPEED_RATIO = 1.55f;
        private const float WALL_REDIRECT_MIN_DOT = -0.35f;

        /// <summary>
        /// 이 액터가 Dodge 상태를 실행할 수 있는지 — Dodge_F/B/L/R 또는 기본 Dodge 모션이 있어야 한다.
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

        public static bool TryResolveDodgeMotion(
            GameActor actor,
            EnemyAIContext context,
            EnemyDetection detection,
            Vector3 actorPosition,
            out Vector3 dodgeDirection,
            out AnimKey motionKey)
        {
            dodgeDirection = CalculateDodgeDirection(actor, context, detection, actorPosition);
            motionKey = EnemyLocomotionHelper.ResolveDirectionalKey(
                dodgeDirection,
                actor.transform,
                AnimKey.Dodge_F,
                AnimKey.Dodge_B,
                AnimKey.Dodge_L,
                AnimKey.Dodge_R);

            if (actor.Animator == null)
                return false;

            if (actor.Animator.HasMotion(motionKey))
                return true;

            motionKey = AnimKey.Dodge;
            return actor.Animator.HasMotion(motionKey);
        }

        public EnemyDodgeState(
            ActorMovementController controller,
            EnemyAIContext context,
            EnemyDetection detection,
            Vector3 dodgeDirection,
            AnimKey motionKey) : base(controller)
        {
            _context = context;
            _detection = detection;
            _dodgeDirection = dodgeDirection.sqrMagnitude > 0.01f
                ? dodgeDirection.normalized
                : -controller.Actor.transform.forward;
            _motionKey = motionKey;
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

            if (_motionKey != AnimKey.None)
            {
                var duration = gameActor.Animator.GetMotionSetDuration(_motionKey);
                _motionLockDuration = duration > 0f
                    ? Mathf.Max(DODGE_DURATION, duration)
                    : FALLBACK_DODGE_LOCK_DURATION;

                gameActor.Animator.OnMotionSetCompleted += OnDodgeMotionCompleted;
                gameActor.Animator.PlayMotion(_motionKey, 0.05f);
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
            if (ShouldTransitionToAirborne(deltaTime))
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

            var verticalVelocity = currentVelocity.y;
            currentVelocity = targetVelocity;
            currentVelocity.y = verticalVelocity;
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
                _dodgeDirection = CalculateDodgeDirection(gameActor, _context, _detection, motor.TransientPosition);
        }

        private static Vector3 CalculateDodgeDirection(
            GameActor actor,
            EnemyAIContext context,
            EnemyDetection detection,
            Vector3 actorPosition)
        {
            if (actor == null || detection == null || !detection.HasTarget)
                return actor != null ? -actor.transform.forward : Vector3.back;

            var toTarget = detection.CurrentTarget.position - actorPosition;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.01f)
                return -actor.transform.forward;

            var dirToTarget = toTarget.normalized;
            var away = -dirToTarget;
            var side = Random.value > 0.5f ? 1f : -1f;
            var lateral = Vector3.Cross(Vector3.up, dirToTarget).normalized * side;

            var tooClose = context != null && detection.DistanceToTarget <= context.PersonalSpaceDistance + 0.4f;
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
