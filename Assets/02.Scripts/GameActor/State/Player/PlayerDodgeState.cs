using UnityEngine;
using UPlayGround.Data.Enum;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 구르기 상태
    /// </summary>
    public class PlayerDodgeState : PlayerActorState
    {
        public override string StateName => "Dodge";
        
        public PlayerDodgeState(ActorMovementController controller) : base(controller)
        {
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            gameActor.Animator.ApplyRootMotion(true);
            //controller.AddVelocity(motor.CharacterForward * controller.DodgePower);
            
            var animState = gameActor.Animator.PlayAnimation(AnimKey.Dodge, 0.25f);
            if (animState != null)
            {
                animState.OwnedEvents.OnEnd = ChangeToNextState;
            }
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.ApplyRootMotion(false);
            
            base.OnExit(toState);
        }
        
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // // Drag
            // currentVelocity *= (1f / (1f + (controller.LandDrag * deltaTime)));
            currentVelocity = gameActor.Animator.DeltaPosition / deltaTime;
        }
        private void ChangeToNextState()
        {
            // 이동 입력이 있으면 GroundMove, 없으면 Idle
            if (playerController.HasMoveInput())
            {
                controller.TransitionToState(new PlayerGroundMoveState(controller));
            }
            else
            {
                controller.TransitionToState(new PlayerIdleState(controller));
            }
        }
    }
}