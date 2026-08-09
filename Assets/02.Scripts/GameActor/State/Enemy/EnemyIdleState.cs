using System.Collections;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Components;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 대기 상태 - 지면에 서있고 움직이지 않는 상태
    /// </summary>
    public class EnemyIdleState : EnemyActorState
    {
        public override ActorStateId StateId => ActorStateId.Idle;
        
        private PlayerEquipment _equipment;

        // 정지형(이동 불가) 액터의 대기 중 조준 회전용. OnEnter에서 1회 캐싱.
        private EnemyAIContext _context;
        private EnemyDetection _detection;

        internal EnemyIdleState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(ActorStateId fromState)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Idle, 0.25f);

            _context ??= gameActor.GetComponent<EnemyAIContext>();
            _detection ??= gameActor.GetComponent<EnemyDetection>();
        }

        public override void UpdateState(float deltaTime)
        {
            // 지면에서 떨어지면 Airborne 상태로 전환
            if (ShouldTransitionToAirborne(deltaTime))
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }
        }
        
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 정지형 액터는 이동하지 않는 대신 대기 중에도 타겟을 향해 몸을 돌린다.
            if (_context != null && _context.FaceTargetWhileIdle
                && _detection != null && _detection.HasTarget)
            {
                Vector3 dir = _detection.CurrentTarget.position - motor.TransientPosition;
                dir.y = 0f;

                if (dir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                    currentRotation = Quaternion.Slerp(
                        currentRotation, targetRot,
                        1f - Mathf.Exp(-_context.IdleFaceTargetSharpness * deltaTime));
                    return;
                }
            }

            // Idle 상태에서는 회전 유지 (또는 부드럽게 정면으로)
            currentRotation = currentRotation.normalized;
        }
        
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (motor.GroundingStatus.IsStableOnGround)
            {
                // 지면에 있으므로 경사면에 맞춰 속도 조정
                currentVelocity = motor.GetDirectionTangentToSurface(
                    currentVelocity,
                    motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

                // 정지 상태로 부드럽게 감속
                Vector3 targetVelocity = Vector3.zero;
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    targetVelocity,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
            }
        }
    }
}
