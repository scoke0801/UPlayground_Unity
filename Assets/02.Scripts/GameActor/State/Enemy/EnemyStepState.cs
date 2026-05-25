using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 짧은 스텝 회피 상태.
    /// Dodge보다 짧고 빠른 방향성 이동으로 압박 라인에서 살짝 빠지거나 옆걸음으로 각을 만든다.
    /// 계산된 방향의 Dash F/B/L/R 모션이 있을 때만 실행한다.
    /// 무적은 부여하지 않고, BT는 차단한다.
    /// </summary>
    public class EnemyStepState : EnemyActorState
    {
        public override string StateName => "Dash";
        public override bool BlocksBehaviorTree => true;

        private readonly EnemyAIContext _context;
        private readonly EnemyDetection _detection;
        private readonly AnimKey _motionKey;

        private Vector3 _stepDirection;
        private float _stepTimer;
        private bool _motionEnded;
        private float _motionLockDuration;
        private float _movementDuration;

        private const float STEP_DURATION = 0.22f;
        private const float FALLBACK_STEP_LOCK_DURATION = 0.32f;
        private const float STEP_SPEED_RATIO = 1.2f;
        private const float WALL_REDIRECT_MIN_DOT = -0.35f;

        public EnemyStepState(
            ActorMovementController controller,
            EnemyAIContext context,
            EnemyDetection detection,
            Vector3 stepDirection,
            AnimKey motionKey) : base(controller)
        {
            _context = context;
            _detection = detection;
            _stepDirection = stepDirection.sqrMagnitude > 0.01f
                ? stepDirection.normalized
                : -controller.Actor.transform.forward;
            _motionKey = motionKey;
        }

        /// <summary>
        /// 이 액터가 Step 상태를 실행할 수 있는지 — Dash_F/B/L/R 중 하나라도 있어야 한다.
        /// </summary>
        public static bool CanExecute(GameActor actor)
        {
            var animator = actor?.Animator;
            if (animator == null) return false;
            return animator.HasMotion(AnimKey.Dash_F)
                || animator.HasMotion(AnimKey.Dash_B)
                || animator.HasMotion(AnimKey.Dash_L)
                || animator.HasMotion(AnimKey.Dash_R);
        }

        public static bool TryResolveStepMotion(
            GameActor actor,
            EnemyAIContext context,
            EnemyDetection detection,
            Vector3 actorPosition,
            out Vector3 stepDirection,
            out AnimKey motionKey)
        {
            stepDirection = CalculateStepDirection(actor, context, detection, actorPosition);
            motionKey = EnemyLocomotionHelper.ResolveDirectionalKey(
                stepDirection,
                actor.transform,
                AnimKey.Dash_F,
                AnimKey.Dash_B,
                AnimKey.Dash_L,
                AnimKey.Dash_R);

            return actor.Animator != null && actor.Animator.HasMotion(motionKey);
        }

        public override bool CanTransitionState(string stateName)
        {
            return stateName is not ("Death" or "Grabbed");
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _stepTimer = 0f;
            _motionEnded = false;
            _movementDuration = STEP_DURATION;
            _motionLockDuration = FALLBACK_STEP_LOCK_DURATION;

            if (_motionKey != AnimKey.None)
            {
                var duration = gameActor.Animator.GetMotionSetDuration(_motionKey);
                _motionLockDuration = duration > 0f
                    ? Mathf.Max(_movementDuration, duration)
                    : FALLBACK_STEP_LOCK_DURATION;

                gameActor.Animator.OnMotionSetCompleted += OnStepMotionCompleted;
                gameActor.Animator.PlayMotion(_motionKey, 0.05f);
            }
            else
            {
                _motionEnded = true;
            }
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetCompleted -= OnStepMotionCompleted;
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            if (ShouldTransitionToAirborne(deltaTime))
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }

            _stepTimer += deltaTime;

            if (_stepTimer < _movementDuration)
                return;

            if (!_motionEnded && _stepTimer < _motionLockDuration)
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

            var normalizedTime = Mathf.Clamp01(_stepTimer / _movementDuration);
            var speedScale = 1f - normalizedTime * normalizedTime;
            var targetVelocity = _stepDirection * (controller.MaxRunMoveSpeed * STEP_SPEED_RATIO * speedScale);

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
            var dot = Vector3.Dot(_stepDirection, hitNormal);
            if (dot >= WALL_REDIRECT_MIN_DOT)
                return;

            _stepDirection = Vector3.ProjectOnPlane(_stepDirection, hitNormal).normalized;
            if (_stepDirection.sqrMagnitude <= 0.01f)
                _stepDirection = CalculateStepDirection(gameActor, _context, _detection, motor.TransientPosition);
        }

        private static Vector3 CalculateStepDirection(
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
            var side = Random.value > 0.5f ? 1f : -1f;
            var lateral = Vector3.Cross(Vector3.up, dirToTarget).normalized * side;

            // Step은 Dodge보다 측면 비중을 더 키운다 (짧게 옆걸음 위주)
            var tooClose = context != null && detection.DistanceToTarget <= context.PersonalSpaceDistance + 0.4f;
            var away = -dirToTarget;
            var direction = tooClose
                ? away * 0.5f + lateral * 0.5f
                : lateral * 0.9f + away * 0.1f;

            direction.y = 0f;
            return direction.sqrMagnitude > 0.01f ? direction.normalized : lateral;
        }

        private void OnStepMotionCompleted()
        {
            if (controller.CurrentState == this)
                _motionEnded = true;
        }
    }
}
