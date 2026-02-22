using System.Collections;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Component;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 몬스터 사망 상태
    /// </summary>
    public class EnemyDeathState : GameActorState
    {
        public override string StateName => "Death";
        
        private PlayerEquipment _equipment;
        public EnemyDeathState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            var state = gameActor.Animator.PlayMotion(AnimKey.Die, 0.25f);
            if (state != null)
            {
                state.OwnedEvents.OnEnd = () =>
                {
                    
                };
            }
        }

        public override void UpdateState(float deltaTime)
        {
        }
        
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // Idle 상태에서는 회전 유지 (또는 부드럽게 정면으로)
            currentRotation = currentRotation.normalized;
        }
        
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity.x = 0;
            currentVelocity.z = 0;
            if (motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity.y = controller.Gravity.y * deltaTime;
            }
        }
    }
}