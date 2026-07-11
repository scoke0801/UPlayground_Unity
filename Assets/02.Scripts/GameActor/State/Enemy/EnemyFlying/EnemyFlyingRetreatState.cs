using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 비행 몬스터 후퇴. 타겟 반대 방향으로 거리 확보 후 Chase 복귀.
    /// 이륙/공격 전환 판단은 BT가 담당한다.
    /// </summary>
    public class EnemyFlyingRetreatState : EnemyActorState
    {
        public override string StateName => "Flying_Retreat";
        public override bool BlocksBehaviorTree => true;

        private readonly EnemyFlyingAIContext _brain;
        private float _retreatSpeed;
        private float _timer;

        private const float Timeout = 2.0f;

        public EnemyFlyingRetreatState(ActorMovementController controller, EnemyFlyingAIContext brain)
            : base(controller) { _brain = brain; }

        public override bool CanTransitionState(string stateName) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            motor.SetGroundSolvingActivation(true);
            _timer = 0f;
            _retreatSpeed = controller.MaxRunMoveSpeed * 0.65f;
            gameActor.Animator.PlayMotion(AnimKey.Walk, 0.2f);
        }

        public override void UpdateState(float deltaTime)
        {
            if (ShouldTransitionToAirborne(deltaTime))
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }

            _timer += deltaTime;

            if (!_brain.Detection.HasTarget)
            {
                controller.TransitionToState(new EnemyIdleState(controller));
                return;
            }

            bool reached = _brain.Detection.DistanceToTarget >= _brain.RetreatDistance;
            if (reached || _timer >= Timeout)
            {
                controller.TransitionToState(new EnemyFlyingChaseState(controller, _brain));
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (!_brain.Detection.HasTarget) return;
            // 타겟을 바라보면서 뒷걸음
            Vector3 dir = (_brain.Detection.CurrentTarget.position - motor.TransientPosition);
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                currentRotation = Quaternion.Slerp(currentRotation,
                    Quaternion.LookRotation(dir.normalized),
                    1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime));
            }
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!_brain.Detection.HasTarget || !motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
                return;
            }

            Vector3 away = (motor.TransientPosition - _brain.Detection.CurrentTarget.position);
            away.y = 0;
            Vector3 vel = away.normalized * _retreatSpeed;
            vel = motor.GetDirectionTangentToSurface(vel, motor.GroundingStatus.GroundNormal) * vel.magnitude;
            currentVelocity = Vector3.Lerp(currentVelocity, vel,
                1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }

        public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref KinematicCharacterController.HitStabilityReport hitStabilityReport)
        {
            // 벽에 막히면 바로 Chase로
            Vector3 retreatDir = (motor.TransientPosition - _brain.Detection.CurrentTarget.position).normalized;
            retreatDir.y = 0;
            if (Vector3.Dot(retreatDir, hitNormal) < -0.35f)
                controller.TransitionToState(new EnemyFlyingChaseState(controller, _brain));
        }
    }
}
