using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Components;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 몬스터 사망 상태
    /// </summary>
    public class EnemyDeathState : EnemyActorState
    {
        public override string StateName => "Death";
        public override bool BlocksBehaviorTree => true;
        
        private bool _isDestoryCalled = false;
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

            // 워프 진행 중이면 즉시 clear (사망 모션이 우선).
            controller.MotionWarp?.ClearTarget();

            MonsterActor owner = gameActor as MonsterActor;

            if (owner == null)
            {
                return;
            }
            var state = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Die, 0.25f);
            if (state != null)
            {
                state.OwnedEvents.OnEnd = () =>
                {
                    if (_isDestoryCalled == false)
                    {
                        _isDestoryCalled = true;
                        owner.PlayDissolveAndDestroy(3f);
                    }
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
            if (motor.GroundingStatus.IsStableOnGround == false)
            {
                currentVelocity += controller.Gravity * deltaTime;
            }
            
        }
    }
}
