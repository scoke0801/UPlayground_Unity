using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
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
            // 점프 입력은 자연 낙하 판정보다 먼저 처리한다.
            if (Svc.Input.InputBuffer.HasInput(PlayerAction.Jump))
            {
                playerController.TransitionToState(new PlayerAirborneState(playerController));
                return;
            }

            if (ShouldTransitionToAirborne(deltaTime))
            {
                playerController.TransitionToState(new PlayerAirborneState(playerController));
                return;
            }

            if (playerController.HasInteractInput())
            {
                PlayerCombat combat = playerActor.GetCombat();
                Transform breakTarget = combat != null ? combat.FindSpecialBreakAttackTarget() : null;
                if (breakTarget != null)
                {
                    Svc.Input.InputBuffer.ConsumeInput(PlayerAction.Interact);
                    playerController.TransitionToState(
                        new PlayerSpecialBreakAttackState(playerController, breakTarget));
                    return;
                }

                if (playerActor.CanStartInteraction())
                {
                    playerController.TransitionToState(new PlayerInteractionState(playerController));
                    return;
                }
            }

            if (playerController.HasMoveInput())
            {
                playerController.TransitionToState(new PlayerGroundMoveState(playerController));
                return;
            }

            if (Svc.Input.InputBuffer.HasInput(PlayerAction.Dodge))
            {
                playerController.TransitionToState(new PlayerDodgeState(playerController));
                return;
            }

            if (Svc.Input.InputBuffer.HasInput(PlayerAction.Dash)
                && controller.TryTransitionToState(new PlayerDashState(controller)))
            {
                return;
            }

            if (playerController.HasCrouchInput())
            {
                playerController.TransitionToState(new PlayerCrouchingState(playerController));
                return;
            }

            if (playerController.HasGuardInput())
            {
                playerController.TransitionToState(new PlayerGuardState(playerController));
                return;
            }

            if (Svc.Input.InputBuffer.HasInput(PlayerAction.Attack))
            {
                if (PlayerAttackState.TryEnter(playerController))
                    return;
            }

            if (playerController.IsChargeAttackHeld())
            {
                playerController.TransitionToState(new PlayerChargeState(playerController));
                return;
            }

            if (Svc.Input.InputBuffer.HasInput(PlayerAction.HeavyAttack))
            {
                if (PlayerAttackState.TryEnter(playerController))
                    return;
            }

            PlayerSkillGauge skillGauge = playerActor.SkillGauge;
            for (int i = 0; i < PlayerSkillGauge.SkillSlotCount; i++)
            {
                if (skillGauge == null)
                    break;
                if (!playerController.HasSkillInput(i) || !skillGauge.CanUseSkill(i))
                    continue;

                if (PlayerAttackState.TryEnter(playerController))
                    return;
            }

            // 행동 입력과 같은 프레임에 전투 상태가 켜졌다면 해당 행동 전이를 우선한다.
            // 외부 요인만으로 전투 상태가 활성화된 경우에는 Drink를 즉시 종료한다.
            if (playerActor.IsInCombat)
            {
                TransitionToIdle();
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
