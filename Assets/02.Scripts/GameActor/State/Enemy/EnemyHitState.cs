using System.Collections;
using UnityEngine;
using UPlayGround.Data.Enum;
using UPlayGround.Component;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 몬스터 피격 상태
    /// </summary>
    public class EnemyHitState : GameActorState
    {
        public override string StateName => "Hit";
        
        private PlayerEquipment _equipment;
        public EnemyHitState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            var state = gameActor.Animator.PlayMotion(AnimKey.Hit_F, 0.25f);

            if (state != null)
            {
                state.OwnedEvents.OnEnd = () => { controller.TransitionToState(new EnemyIdleState(controller)); };
            }
            // [TODO] 파티클 출력
        }

        public override void UpdateState(float deltaTime)
        {
            // 지면에서 떨어지면 Airborne 상태로 전환
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                controller.TransitionToState(new EnemyAirborneState(controller));
                return;
            }
        }
        
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
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
