using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    public class PlayerJumpDashAttackState : PlayerActorState
    {
        public override string StateName  => "JumpDashAttack";
        public override bool AdjustGravity => false;

        private PlayerCombat _combat;
        private AttackData   _attackData;
        private bool         _changingState;

        public PlayerJumpDashAttackState(ActorMovementController controller) : base(controller) { }

        public override bool CanTransitionState(string stateName)
        {
            if (stateName == "Hit") return false;
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _changingState = false;
            _combat        = playerActor.GetCombat();
            _attackData    = _combat?.ExecuteJumpDashAttack();

            AnimKey animKey = _attackData?.animKey ?? AnimKey.JumpDashAttack_1;
            var state = gameActor.Animator.PlayMotion(animKey, 0.1f);
            if (state != null)
                gameActor.Animator.OnMotionSetCompleted += ChangeToNextState;
            else
                ChangeToNextState();
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetCompleted -= ChangeToNextState;
            _combat?.ClearHitTargets();
            base.OnExit(toState);
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            base.UpdateVelocity(ref currentVelocity, deltaTime);

            Vector3 rootMotionVel = gameActor.Animator.DeltaPosition / deltaTime;
            currentVelocity = rootMotionVel;

            if (!motor.GroundingStatus.IsStableOnGround)
                currentVelocity += controller.Gravity * controller.FallGravityMultiplier * deltaTime;
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation *= gameActor.Animator.DeltaRotation;
            currentRotation  = currentRotation.normalized;
        }

        private void ChangeToNextState()
        {
            if (_changingState) return;
            _changingState = true;

            _combat?.ResetCombo();
            _combat?.ClearHitTargets();

            if (!motor.GroundingStatus.IsStableOnGround)
            {
                playerController.TransitionToState(new PlayerAirborneState(playerController));
                return;
            }

            if (playerController.HasMoveInput())
                controller.TransitionToState(new PlayerGroundMoveState(controller));
            else
                controller.TransitionToState(new PlayerIdleState(controller));
        }
    }
}
