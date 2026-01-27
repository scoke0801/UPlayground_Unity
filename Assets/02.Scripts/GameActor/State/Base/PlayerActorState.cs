using KinematicCharacterController;
using UnityEngine;

namespace UPlayGround.GameActor.MovementController.State
{
    /// <summary>
    /// 모든 Actor 이동 상태의 베이스 클래스
    /// </summary>
    public abstract class PlayerActorState : GameActorState
    {
        protected PlayerMovementController playerController;
        protected PlayerActor playerActor;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
         
            playerActor = gameActor as PlayerActor;
        }
        
        protected PlayerActorState(ActorMovementController controller) : base(controller)
        {
            playerController = controller as PlayerMovementController;
        }
    }
}