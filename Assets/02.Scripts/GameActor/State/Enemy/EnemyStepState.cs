using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 짧은 스텝 회피 상태.
    /// Dodge보다 짧고 빠른 방향성 이동으로 압박 라인에서 살짝 빠지거나 옆걸음으로 각을 만든다.
    /// 모션 우선순위: Step_F/B/L/R → Dodge_F/B/L/R → Dodge. 모두 없으면 EnemyActionResolver에서 진입을 차단한다.
    /// 무적은 부여하지 않고, BT는 차단한다.
    /// </summary>
    public class EnemyStepState : GameActorState
    {
        public override string StateName => "Step";
        public override bool BlocksBehaviorTree => true;

        private readonly EnemyAIContext _context;
        private readonly EnemyDetection _detection;

        private Vector3 _stepDirection;
        private float _stepTimer;

        private const float STEP_DURATION = 0.22f;
        private const float STEP_SPEED_RATIO = 1.2f;
        private const float WALL_REDIRECT_MIN_DOT = -0.35f;

        public EnemyStepState(
            ActorMovementController controller,
            EnemyAIContext context,
            EnemyDetection detection) : base(controller)
        {
            _context = context;
            _detection = detection;
        }

        /// <summary>
        /// 이 액터가 Step 상태를 실행할 수 있는지 — Step_F/B/L/R 또는 Dodge 계열 모션이 하나라도 있어야 한다.
        /// </summary>
        public static bool CanExecute(GameActor actor)
        {
            var animator = actor?.Animator;
            if (animator == null) return false;
            if (animator.HasMotion(AnimKey.Step_F)
                || animator.HasMotion(AnimKey.Step_B)
                || animator.HasMotion(AnimKey.Step_L)
                || animator.HasMotion(AnimKey.Step_R))
            {
                return true;
            }
            return EnemyDodgeState.CanExecute(actor);
        }

        public override bool CanTransitionState(string stateName)
        {
            return stateName is not ("Death" or "Grabbed");
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _stepTimer = 0f;
            _stepDirection = CalculateStepDirection();

            // Step 방향성 → Dodge 방향성 → Dodge → 가용한 다른 방향성 순으로 폴백.
            // CanExecute가 최소 1개 보유를 보장하므로 None이 반환되지 않는다.
            AnimKey stepKey = EnemyLocomotionHelper.ResolveDirectionalKey(
                _stepDirection,
                gameActor.transform,
                AnimKey.Step_F,
                AnimKey.Step_B,
                AnimKey.Step_L,
                AnimKey.Step_R);

            AnimKey dodgeKey = EnemyLocomotionHelper.ResolveDirectionalKey(
                _stepDirection,
                gameActor.transform,
                AnimKey.Dodge_F,
                AnimKey.Dodge_B,
                AnimKey.Dodge_L,
                AnimKey.Dodge_R);

            AnimKey motionKey = EnemyLocomotionHelper.PickFirstAvailable(
                gameActor.Animator,
                stepKey,
                dodgeKey,
                AnimKey.Dodge,
                AnimKey.Step_B, AnimKey.Step_L, AnimKey.Step_R, AnimKey.Step_F,
                AnimKey.Dodge_B, AnimKey.Dodge_L, AnimKey.Dodge_R, AnimKey.Dodge_F);

            if (motionKey != AnimKey.None)
                gameActor.Animator.PlayMotion(motionKey, 0.05f);
        }

        public override void UpdateState(float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }

            _stepTimer += deltaTime;

            if (_stepTimer < STEP_DURATION)
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

            var normalizedTime = Mathf.Clamp01(_stepTimer / STEP_DURATION);
            var speedScale = 1f - normalizedTime * normalizedTime;
            var targetVelocity = _stepDirection * (controller.MaxRunMoveSpeed * STEP_SPEED_RATIO * speedScale);

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
            var dot = Vector3.Dot(_stepDirection, hitNormal);
            if (dot >= WALL_REDIRECT_MIN_DOT)
                return;

            _stepDirection = Vector3.ProjectOnPlane(_stepDirection, hitNormal).normalized;
            if (_stepDirection.sqrMagnitude <= 0.01f)
                _stepDirection = CalculateStepDirection();
        }

        private Vector3 CalculateStepDirection()
        {
            if (!_detection.HasTarget)
                return -gameActor.transform.forward;

            var toTarget = _detection.CurrentTarget.position - motor.TransientPosition;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.01f)
                return -gameActor.transform.forward;

            var dirToTarget = toTarget.normalized;
            var side = Random.value > 0.5f ? 1f : -1f;
            var lateral = Vector3.Cross(Vector3.up, dirToTarget).normalized * side;

            // Step은 Dodge보다 측면 비중을 더 키운다 (짧게 옆걸음 위주)
            var tooClose = _detection.DistanceToTarget <= _context.PersonalSpaceDistance + 0.4f;
            var away = -dirToTarget;
            var direction = tooClose
                ? away * 0.5f + lateral * 0.5f
                : lateral * 0.9f + away * 0.1f;

            direction.y = 0f;
            return direction.sqrMagnitude > 0.01f ? direction.normalized : lateral;
        }
    }
}
