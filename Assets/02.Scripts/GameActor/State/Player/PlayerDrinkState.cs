using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 소모품 사용 모션 상태.
    /// Idle에서만 진입하며 Drink MotionSet 재생이 끝나면 다시 Idle로 돌아간다.
    /// </summary>
    public sealed class PlayerDrinkState : PlayerActorState
    {
        public override string StateName => "Drink";

        protected override AnimKey? RequiredMotionKey => AnimKey.Drink;
        private PlayerEquipment _equipment;

        public PlayerDrinkState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(string stateName)
        {
            return stateName == "Idle" && HasRequiredMotion();
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _equipment = playerActor.GetPlayerEquipment();
            _equipment?.BeginConsumableUseEquipment();

            var animState = gameActor.Animator.PlayMotion(AnimKey.Drink, 0.15f);
            if (animState == null)
            {
                TransitionToIdle();
                return;
            }

            gameActor.Animator.OnMotionSetCompleted += TransitionToIdle;
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetCompleted -= TransitionToIdle;
            _equipment?.EndConsumableUseEquipment();
            _equipment = null;
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            if (ShouldTransitionToAirborne(deltaTime))
            {
                playerController.TransitionToState(new PlayerAirborneState(playerController));
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                return;
            }

            currentVelocity = Vector3.Lerp(
                currentVelocity,
                Vector3.zero,
                1f - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }

        private void TransitionToIdle()
        {
            if (controller.CurrentState == this)
            {
                playerController.TransitionToState(new PlayerIdleState(playerController));
            }
        }
    }
}
