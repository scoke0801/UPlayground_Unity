using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 액션성 회피 상태.
    /// 짧은 무적 시간 동안 타겟 공격 축에서 벗어나며, 전용 Dodge 모션이 없으면 Run 계열 방향성 모션으로 대체한다.
    /// </summary>
    public class EnemyDodgeState : GameActorState
    {
        public override string StateName => "Dodge";
        public override bool GrantsInvincibility => true;
        public override bool SuppressesHitReaction => true;

        private readonly EnemyAIContext _context;
        private readonly EnemyDetection _detection;

        private Vector3 _dodgeDirection;
        private float _dodgeTimer;
        private bool _usesDodgeMotion;
        private AnimKey _lastLocoKey = AnimKey.None;

        private const float DODGE_DURATION = 0.35f;
        private const float DODGE_SPEED_RATIO = 1.85f;
        private const float WALL_REDIRECT_MIN_DOT = -0.35f;

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
            _dodgeDirection = CalculateDodgeDirection();

            _usesDodgeMotion = gameActor.Animator.HasMotion(AnimKey.Dodge);
            if (_usesDodgeMotion)
            {
                gameActor.Animator.PlayMotion(AnimKey.Dodge, 0.05f);
            }
            else
            {
                _lastLocoKey = AnimKey.Run;
                gameActor.Animator.PlayMotion(AnimKey.Run, 0.05f);
            }
        }

        public override void UpdateState(float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }

            _dodgeTimer += deltaTime;

            if (!_usesDodgeMotion)
            {
                EnemyLocomotionHelper.UpdateAnim(
                    gameActor,
                    motor,
                    ref _lastLocoKey,
                    EnemyLocomotionHelper.LocoStyle.Run,
                    0.05f);
            }

            if (_dodgeTimer < DODGE_DURATION)
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

            var normalizedTime = Mathf.Clamp01(_dodgeTimer / DODGE_DURATION);
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
    }
}
