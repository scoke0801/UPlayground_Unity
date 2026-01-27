using UnityEngine;
using UPlayGround.Data.Enum;

namespace UPlayGround.GameActor.MovementController.State
{
    /// <summary>
    /// 구르기 상태
    /// </summary>
    public class PlayerAttackState : PlayerActorState
    {
        public override string StateName => "Attack";
        
        public PlayerAttackState(ActorMovementController controller) : base(controller)
        {
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            var animState = gameActor.Animator.PlayAnimation(GetAnimKey(), 0.25f);
            if (animState != null)
            {
                animState.OwnedEvents.OnEnd = ChangeToNextState;
            }
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

        private AnimKey GetAnimKey()
        {
            bool attackInput = playerController.HasAttackInput();
            bool heavyAttackInput = playerController.HasHeavyAttackInput();

            if (attackInput && heavyAttackInput)
            {
                return AnimKey.Skill_1;
            }

            if (heavyAttackInput)
            {
                return AnimKey.HeavyAttack;
            }

            return AnimKey.Attack;
        }
    }
}