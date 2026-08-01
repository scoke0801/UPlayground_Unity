using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 타겟과 너무 붙었거나 압박 루프를 끊어야 할 때 쓰는 짧은 후방 점프.
    /// 일반 Retreat보다 빠르게 압박만 끊고 착지 후 BT가 다음 패턴을 고르게 한다.
    /// </summary>
    public class EnemyJumpBackState : EnemyActorState
    {
        public override ActorStateId StateId => ActorStateId.JumpBack;
        public override bool BlocksBehaviorTree => true;
        public override GravityOwnership GravityOwner => GravityOwnership.State;

        private readonly EnemyAIContext _context;
        private readonly EnemyDetection _detection;
        private readonly EnemyTacticalMemory _memory;

        private Vector3 _jumpDirection;
        private float _timer;
        private bool _hasLeftGround;
        private bool _canJumpBack;
        private bool _launchStarted;
        private bool _usesMotionFallback;
        private bool _motionEnded;
        private float _motionLockDuration;
        private float _maxSafeTargetDistance;

        private const float HORIZONTAL_SPEED_RATIO = 1.75f;
        private const float JUMP_SPEED_RATIO = 0.52f;
        private const float WALL_REDIRECT_MIN_DOT = -0.35f;
        private const float LOCK_ON_SAFE_DISTANCE_FALLBACK = 12f;
        private const float TARGET_DISTANCE_SAFETY_MARGIN = 0.75f;
        private const float TARGET_DISTANCE_EXTRA_MARGIN = 1.1f;
        private const float MIN_RETREAT_ROOM = 0.35f;
        private const float MIN_HORIZONTAL_LAUNCH_SPEED = 1.2f;
        private const float HORIZONTAL_DAMPING = 2.2f;
        private const float FALLBACK_RETREAT_DURATION = 0.38f;
        private const float FALLBACK_SPEED_RATIO = 1.25f;
        private const float MOTION_COMPLETION_GRACE = 0.1f;

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

        public override bool CanTransitionState(ActorStateId fromState)
        {
            return fromState is not (ActorStateId.Death or ActorStateId.Grabbed);
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _timer = 0f;
            _hasLeftGround = false;
            _launchStarted = false;
            _usesMotionFallback = false;
            _motionEnded = false;
            _jumpDirection = CalculateJumpDirection();
            _maxSafeTargetDistance = ResolveMaxSafeTargetDistance();
            _canJumpBack = HasRetreatRoom();

            _memory?.NotifyRetreated();
            var motionKey = ResolveMotionKey();
            var duration = gameActor.Animator.GetMotionSetDuration(motionKey);
            _motionLockDuration = duration > 0f
                ? Mathf.Max(FALLBACK_RETREAT_DURATION, duration + MOTION_COMPLETION_GRACE)
                : FALLBACK_RETREAT_DURATION;

            gameActor.Animator.OnMotionSetCompleted += OnMotionCompleted;
            if (gameActor.Animator.PlayMotion(motionKey, 0.05f) == null)
            {
                gameActor.Animator.OnMotionSetCompleted -= OnMotionCompleted;

                // Jump/Dodge 모션이 없는 몬스터도 상태 전환 성공 직후 Idle로 끝나지 않고
                // 짧은 물리 후퇴를 수행한다. 원거리 몬스터의 최소 거리 확보를 보장한다.
                _usesMotionFallback = true;
                gameActor.Animator.PlayMotion(
                    UPlayGround.Data.Actor.Animation.MotionTags.Idle,
                    0.05f);
            }
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetCompleted -= OnMotionCompleted;

            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            _timer += deltaTime;

            if (_usesMotionFallback && _timer >= FALLBACK_RETREAT_DURATION)
            {
                ChangeToNextState();
                return;
            }

            // MotionSet 디렉터는 마지막 포즈를 샘플링한 뒤 개별 AnimancerState의
            // OnEnd 전에 속도를 0으로 만들 수 있다. 완료 신호가 유실되더라도
            // 상태가 BT를 영구 차단하지 않도록 재생 길이를 최종 안전장치로 사용한다.
            if (_motionEnded || _timer >= _motionLockDuration)
                ChangeToNextState();
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
            if (!_canJumpBack)
            {
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                return;
            }

            if (_usesMotionFallback)
            {
                if (!motor.GroundingStatus.IsStableOnGround)
                {
                    currentVelocity += controller.Gravity * deltaTime;
                    return;
                }

                float fallbackScale = 1f - Mathf.Clamp01(_timer / FALLBACK_RETREAT_DURATION);
                var fallbackVelocity =
                    _jumpDirection
                    * (controller.MaxRunMoveSpeed * FALLBACK_SPEED_RATIO * fallbackScale);
                fallbackVelocity = motor.GetDirectionTangentToSurface(
                    fallbackVelocity,
                    motor.GroundingStatus.GroundNormal) * fallbackVelocity.magnitude;

                var fallbackVerticalVelocity = currentVelocity.y;
                currentVelocity = fallbackVelocity;
                currentVelocity.y = fallbackVerticalVelocity;
                return;
            }

            float horizontalScale = Mathf.Lerp(1f, 0.35f, 1f - Mathf.Exp(-HORIZONTAL_DAMPING * _timer));

            var horizontalVelocity = _jumpDirection
                                     * (controller.MaxRunMoveSpeed * HORIZONTAL_SPEED_RATIO * horizontalScale);
            ClampHorizontalVelocityToSafeDistance(ref horizontalVelocity, deltaTime);

            var verticalVelocity = currentVelocity.y;
            bool hasRetreatVelocity =
                horizontalVelocity.sqrMagnitude >= MIN_HORIZONTAL_LAUNCH_SPEED * MIN_HORIZONTAL_LAUNCH_SPEED;

            if (!_hasLeftGround && _timer <= 0.08f && hasRetreatVelocity)
            {
                _launchStarted = true;
                motor.ForceUnground();
                verticalVelocity = Mathf.Max(verticalVelocity, controller.JumpSpeed * JUMP_SPEED_RATIO);
            }
            else if (!_hasLeftGround && !_launchStarted)
            {
                verticalVelocity = Mathf.Min(verticalVelocity, 0f);
            }
            else
            {
                verticalVelocity += controller.Gravity.y * controller.RiseGravityMultiplier * deltaTime;
            }

            currentVelocity = horizontalVelocity;
            currentVelocity.y = verticalVelocity;
        }

        public override void PostGroundingUpdate(float deltaTime)
        {
            if (!_hasLeftGround && !motor.GroundingStatus.IsStableOnGround)
                _hasLeftGround = true;

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

        private UPlayGround.Gameplay.Tag.GameplayTag ResolveMotionKey()
        {
            if (_canJumpBack && gameActor.Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.Jump))
                return UPlayGround.Data.Actor.Animation.MotionTags.Jump;

            return UPlayGround.Data.Actor.Animation.MotionTags.Dodge;
        }

        private float ResolveMaxSafeTargetDistance()
        {
            var safeDistance = LOCK_ON_SAFE_DISTANCE_FALLBACK;
            if (_context != null)
            {
                var tacticalDistance = Mathf.Max(
                    _context.OptimalCombatDistance + TARGET_DISTANCE_EXTRA_MARGIN,
                    _context.PersonalSpaceDistance + TARGET_DISTANCE_EXTRA_MARGIN);
                safeDistance = Mathf.Min(safeDistance, tacticalDistance);
            }

            if (_detection != null)
                safeDistance = Mathf.Min(
                    safeDistance,
                    Mathf.Max(0f, _detection.LostTargetRadius - TARGET_DISTANCE_SAFETY_MARGIN));

            var cameraManager = CameraManager.Instance;
            if (cameraManager != null)
                safeDistance = Mathf.Min(
                    safeDistance,
                    Mathf.Max(0f, cameraManager.GetLockOnRange() - TARGET_DISTANCE_SAFETY_MARGIN));

            return safeDistance > 0.1f ? safeDistance : LOCK_ON_SAFE_DISTANCE_FALLBACK;
        }

        private bool HasRetreatRoom()
        {
            if (_detection == null || !_detection.HasTarget)
                return true;

            var away = motor.TransientPosition - _detection.CurrentTarget.position;
            away.y = 0f;
            return _maxSafeTargetDistance - away.magnitude >= MIN_RETREAT_ROOM;
        }

        private void ClampHorizontalVelocityToSafeDistance(ref Vector3 horizontalVelocity, float deltaTime)
        {
            if (_detection == null || !_detection.HasTarget || deltaTime <= 0f)
                return;

            var away = motor.TransientPosition - _detection.CurrentTarget.position;
            away.y = 0f;
            var currentDistance = away.magnitude;
            if (currentDistance <= 0.001f)
                return;

            var awayDir = away / currentDistance;
            var outwardSpeed = Vector3.Dot(horizontalVelocity, awayDir);
            if (outwardSpeed <= 0f)
                return;

            var remainingDistance = _maxSafeTargetDistance - currentDistance;
            var maxOutwardSpeed = remainingDistance > 0f
                ? remainingDistance / deltaTime
                : 0f;

            if (outwardSpeed <= maxOutwardSpeed)
                return;

            horizontalVelocity -= awayDir * (outwardSpeed - maxOutwardSpeed);
            if (horizontalVelocity.sqrMagnitude <= 0.01f)
                horizontalVelocity = Vector3.zero;
        }

        private void ChangeToNextState()
        {
            if (controller.CurrentState != this)
                return;

            gameActor.Animator.OnMotionSetCompleted -= OnMotionCompleted;

            if (!_detection.HasTarget)
            {
                controller.TransitionToState(ActorStateId.Idle);
                return;
            }

            var distance = _detection.DistanceToTarget;
            if (distance > _context.OptimalCombatDistance + 0.8f)
                controller.TransitionToState(new EnemyChaseState(controller, _context, _detection));
            else
                controller.TransitionToState(ActorStateId.Idle);
        }

        private void OnMotionCompleted()
        {
            if (controller.CurrentState == this)
                _motionEnded = true;
        }
    }
}
