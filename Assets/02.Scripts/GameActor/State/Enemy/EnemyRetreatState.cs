using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Component;
using UPlayGround.MovementController;
using Random = UnityEngine.Random;

namespace UPlayGround.State
{
    /// <summary>
    /// 후퇴 상태 - 타겟 반대 방향으로 뒷걸음치며 거리 확보
    /// 후퇴 완료 후 Circle 상태로 전환하여 대상 주변을 배회
    /// </summary>
    public class EnemyRetreatState : EnemyActorState
    {
        public override string StateName => "Retreat";
        public override bool BlocksBehaviorTree => true;

        private EnemyAIContext _context;
        private EnemyDetection _detection;

        private float _retreatSpeed;
        private float _targetDistance;
        private float _retreatTimer;
        private AnimKey _lastLocoKey = AnimKey.None;

        private const float RETREAT_TIMEOUT = 2.0f;
        private const float RETREAT_SPEED_RATIO = 0.65f;

        public EnemyRetreatState(
            ActorMovementController controller,
            EnemyAIContext context,
            EnemyDetection detection,
            float targetDistance) : base(controller)
        {
            _context = context;
            _detection = detection;
            _targetDistance = targetDistance;
        }

        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _retreatTimer = 0f;
            _retreatSpeed = controller.MaxRunMoveSpeed * RETREAT_SPEED_RATIO;
            _lastLocoKey  = AnimKey.Walk_B;
            gameActor.Animator.PlayMotion(AnimKey.Walk_B, 0.2f);
        }

        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            if (ShouldTransitionToAirborne(deltaTime))
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }

            _retreatTimer += deltaTime;

            bool reachedDistance = _detection.HasTarget &&
                                  _detection.DistanceToTarget >= _targetDistance;
            bool timedOut = _retreatTimer >= RETREAT_TIMEOUT;
            bool lostTarget = !_detection.HasTarget;

            if (lostTarget)
            {
                controller.TransitionToState(new EnemyIdleState(controller));
                return;
            }

            EnemyLocomotionHelper.UpdateAnim(gameActor, motor, ref _lastLocoKey,
                EnemyLocomotionHelper.LocoStyle.Walk);

            if (reachedDistance || timedOut)
            {
                // 후퇴 완료 → Brain이 다음 행동 결정 (항상 Circle이 아님)
                float roll = Random.value;
                if (roll < 0.4f)
                {
                    // 후퇴 후 바로 Chase로 돌아가 압박 (닌자 가이덴 스타일)
                    controller.TransitionToState(
                        new EnemyChaseState(controller, _context, _detection));
                }
                else if (roll < 0.7f)
                {
                    // 짧은 Circle
                    controller.TransitionToState(
                        new EnemyCircleState(controller, _context, _detection, _context.CircleDuration * Random.Range(0.3f, 0.6f)));
                }
                else
                {
                    // 대기 후 Brain의 다음 판단에 맡김
                    controller.TransitionToState(new EnemyIdleState(controller));
                }
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_detection.HasTarget)
            {
                Vector3 dirToTarget = (_detection.CurrentTarget.position - motor.TransientPosition).normalized;
                dirToTarget.y = 0;

                if (dirToTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(dirToTarget);
                    currentRotation = Quaternion.Slerp(
                        currentRotation,
                        targetRotation,
                        1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
                }
            }

            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!_detection.HasTarget)
            {
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                return;
            }

            Vector3 dirAwayFromTarget = (motor.TransientPosition - _detection.CurrentTarget.position).normalized;
            dirAwayFromTarget.y = 0;

            if (motor.GroundingStatus.IsStableOnGround)
            {
                Vector3 targetVelocity = dirAwayFromTarget * _retreatSpeed;

                targetVelocity = motor.GetDirectionTangentToSurface(
                    targetVelocity,
                    motor.GroundingStatus.GroundNormal) * targetVelocity.magnitude;

                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    targetVelocity,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
            }
        }

        /// <summary>
        /// 벽 충돌 시 후퇴 중단 → 바로 Circle로 전환
        /// </summary>
        public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref KinematicCharacterController.HitStabilityReport hitStabilityReport)
        {
            if (_detection.HasTarget)
            {
                Vector3 retreatDir = (motor.TransientPosition - _detection.CurrentTarget.position).normalized;
                retreatDir.y = 0;
                float dot = Vector3.Dot(retreatDir, hitNormal);

                if (dot < -0.35f)
                {
                    controller.TransitionToState(
                        new EnemyCircleState(controller, _context, _detection, _context.CircleDuration));
                }
            }
        }
    }
}
