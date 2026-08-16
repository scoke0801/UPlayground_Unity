using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 비행 몬스터 지상 선회. 기존 EnemyCircleState의 경량 버전.
    /// 타겟 주변을 배회하다 duration 만료 시 Chase로 복귀.
    /// 이륙/공격 전환 판단은 BT가 담당한다.
    /// </summary>
    public class EnemyFlyingCircleState : EnemyActorState
    {
        public override ActorStateId StateId => ActorStateId.Flying_Circle;
        public override bool BlocksBehaviorTree => true;

        private readonly EnemyFlyingAIContext _brain;
        private float _duration;
        private float _timer;
        private float _circleDir;
        private float _baseSpeed;

        public EnemyFlyingCircleState(ActorMovementController controller, EnemyFlyingAIContext brain, float duration)
            : base(controller)
        {
            _brain = brain;
            _duration = duration;
        }

        public override bool CanTransitionState(ActorStateId fromState) => true;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            motor.SetGroundSolvingActivation(true);
            _timer = 0f;
            _circleDir = Random.value > 0.5f ? 1f : -1f;
            _baseSpeed = controller.MaxRunMoveSpeed * 0.5f;
            gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Walk, 0.25f);
        }

        public override void UpdateState(float deltaTime)
        {
            if (ShouldTransitionToAirborne(deltaTime))
            {
                controller.TransitionToState(
                    ActorStateId.Airborne,
                    EnemyAirborneContext.Natural);
                return;
            }
            if (!_brain.Detection.HasTarget)
            {
                controller.TransitionToState(ActorStateId.Idle);
                return;
            }

            _timer += deltaTime;
            if (_timer >= _duration)
            {
                // Circle 종료 → Chase 복귀 (Brain이 다음 판단)
                controller.TransitionToState(ActorStateId.Flying_Chase);
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (!_brain.Detection.HasTarget) return;
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

            Vector3 toTarget = _brain.Detection.CurrentTarget.position - motor.TransientPosition;
            toTarget.y = 0;
            float dist = toTarget.magnitude;
            if (dist < 0.1f) return;

            Vector3 dirToTarget = toTarget / dist;
            // 접선 방향 이동
            Vector3 tangent = Vector3.Cross(Vector3.up, dirToTarget) * _circleDir;
            // 거리 보정: 멀면 접근, 가까우면 후퇴
            float radialCorrection = Mathf.Clamp((dist - _brain.OptimalCombatDistance) / _brain.OptimalCombatDistance, -0.5f, 0.5f);
            Vector3 moveDir = (tangent + dirToTarget * radialCorrection).normalized;

            Vector3 vel = moveDir * _baseSpeed;
            vel = motor.GetDirectionTangentToSurface(vel, motor.GroundingStatus.GroundNormal) * vel.magnitude;
            currentVelocity = Vector3.Lerp(currentVelocity, vel,
                1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }
    }
}
