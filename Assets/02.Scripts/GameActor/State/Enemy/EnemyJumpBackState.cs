using UnityEngine;
using Animancer;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 타겟과 너무 붙었거나 압박 루프를 끊어야 할 때 쓰는 장거리 후방 점프.
    /// 일반 Retreat보다 빠르게 거리를 벌리고 착지 후 BT가 다음 패턴을 고르게 한다.
    /// </summary>
    public class EnemyJumpBackState : GameActorState
    {
        public override string StateName => "JumpBack";
        public override bool BlocksBehaviorTree => true;
        public override bool AdjustGravity => false;

        private readonly EnemyAIContext _context;
        private readonly EnemyDetection _detection;
        private readonly EnemyTacticalMemory _memory;

        private Vector3 _jumpDirection;
        private float _timer;
        private bool _hasLeftGround;
        private bool _landing;
        private AnimancerState _landState;

        private const float MIN_DURATION = 0.32f;
        private const float MAX_DURATION = 1.15f;
        private const float HORIZONTAL_SPEED_RATIO = 2.25f;
        private const float JUMP_SPEED_RATIO = 0.62f;
        private const float WALL_REDIRECT_MIN_DOT = -0.35f;

        public EnemyJumpBackState(
            ActorMovementController controller,
            EnemyAIContext context,
            EnemyDetection detection,
            EnemyTacticalMemory memory = null) : base(controller)
        {
            _context = context;
            _detection = detection;
            _memory = memory;
        }

        public override bool CanTransitionState(string stateName)
        {
            return stateName is not ("Death" or "Grabbed");
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _timer = 0f;
            _hasLeftGround = false;
            _landing = false;
            _jumpDirection = CalculateJumpDirection();

            _memory?.NotifyRetreated();
            gameActor.Animator.PlayMotion(
                gameActor.Animator.HasMotion(AnimKey.Jump) ? AnimKey.Jump : AnimKey.Dodge,
                0.05f);

            motor.ForceUnground();
        }

        public override void OnExit(GameActorState toState)
        {
            if (_landState != null)
            {
                _landState.OwnedEvents.OnEnd = null;
                _landState = null;
            }

            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            _timer += deltaTime;

            if (_landing)
                return;

            if (_timer >= MAX_DURATION)
            {
                ChangeToNextState();
                return;
            }

            if (_timer >= MIN_DURATION && _hasLeftGround && motor.GroundingStatus.IsStableOnGround)
                OnLanded();
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
            var normalizedTime = Mathf.Clamp01(_timer / MAX_DURATION);
            var horizontalScale = Mathf.Lerp(1f, 0.35f, normalizedTime);

            var horizontalVelocity = _jumpDirection
                                     * (controller.MaxRunMoveSpeed * HORIZONTAL_SPEED_RATIO * horizontalScale);
            var verticalVelocity = currentVelocity.y;

            if (!_hasLeftGround && _timer <= 0.08f)
                verticalVelocity = Mathf.Max(verticalVelocity, controller.JumpSpeed * JUMP_SPEED_RATIO);
            else
                verticalVelocity += controller.Gravity.y * controller.RiseGravityMultiplier * deltaTime;

            currentVelocity = horizontalVelocity;
            currentVelocity.y = verticalVelocity;
        }

        public override void PostGroundingUpdate(float deltaTime)
        {
            if (!_hasLeftGround && !motor.GroundingStatus.IsStableOnGround)
                _hasLeftGround = true;

            if (_timer >= MIN_DURATION
                && _hasLeftGround
                && motor.GroundingStatus.IsStableOnGround
                && !motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLanded();
            }
        }

        public override void OnMovementHit(
            Collider hitCollider,
            Vector3 hitNormal,
            Vector3 hitPoint,
            ref KinematicCharacterController.HitStabilityReport hitStabilityReport)
        {
            var dot = Vector3.Dot(_jumpDirection, hitNormal);
            if (dot >= WALL_REDIRECT_MIN_DOT)
                return;

            _jumpDirection = Vector3.ProjectOnPlane(_jumpDirection, hitNormal).normalized;
            if (_jumpDirection.sqrMagnitude <= 0.01f)
                _jumpDirection = CalculateJumpDirection();
        }

        private Vector3 CalculateJumpDirection()
        {
            if (!_detection.HasTarget)
                return -gameActor.transform.forward;

            var away = motor.TransientPosition - _detection.CurrentTarget.position;
            away.y = 0f;
            if (away.sqrMagnitude <= 0.01f)
                away = -gameActor.transform.forward;

            var awayDir = away.normalized;
            var side = Random.value > 0.5f ? 1f : -1f;
            var lateral = Vector3.Cross(Vector3.up, -awayDir).normalized * side;
            var direction = (awayDir * 0.86f + lateral * 0.14f).normalized;
            return direction.sqrMagnitude > 0.01f ? direction : awayDir;
        }

        private void OnLanded()
        {
            if (_landing)
                return;

            _landing = true;
            var land = gameActor.Animator.PlayMotion(AnimKey.Land, 0.12f);
            if (land != null)
            {
                _landState = land;
                _landState.OwnedEvents.OnEnd = ChangeToNextState;
            }
            else
                ChangeToNextState();
        }

        private void ChangeToNextState()
        {
            if (controller.CurrentState != this)
                return;

            if (_landState != null)
            {
                _landState.OwnedEvents.OnEnd = null;
                _landState = null;
            }

            if (!_detection.HasTarget)
            {
                controller.TransitionToState(new EnemyIdleState(controller));
                return;
            }

            var distance = _detection.DistanceToTarget;
            if (distance > _context.OptimalCombatDistance + 0.8f)
                controller.TransitionToState(new EnemyChaseState(controller, _context, _detection));
            else
                controller.TransitionToState(new EnemyIdleState(controller));
        }
    }
}
