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
    /// 상체(Drink 모션)는 상체 마스크 오버레이 레이어에 얹고, 하체는 Layer 0 로코모션(Idle/Walk)이
    /// 담당한다. 덕분에 걷기 속도로 이동하면서 마실 수 있다. 상체 모션이 끝나면 Idle로 돌아간다.
    /// </summary>
    public sealed class PlayerDrinkState : PlayerActorState
    {
        public override string StateName => "Drink";

        protected override AnimKey? RequiredMotionKey => AnimKey.Drink;

        private const float LegFadeDuration = 0.2f;
        private const float OverlayFadeDuration = 0.15f;

        private PlayerEquipment _equipment;
        private float _drinkRemaining;      // 상체 오버레이 남은 재생 시간(초)
        private AnimKey _legAnimKey = AnimKey.None; // 현재 Layer 0에 올라간 로코모션 키

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

            // 하체: 진입 시 이동 입력 여부에 따라 Idle/Walk 로코모션을 Layer 0에 재생
            UpdateLegLocomotion();

            // 상체: Drink 모션을 상체 마스크 오버레이 레이어에 1회 재생.
            // 디렉터를 사용하지 않으므로 OnMotionSetCompleted 대신 재생 길이 타이머로 완료를 판정한다.
            _drinkRemaining = gameActor.Animator.PlayUpperBodyOverlay(AnimKey.Drink, OverlayFadeDuration);
            if (_drinkRemaining <= 0f)
            {
                TransitionToIdle();
            }
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.StopUpperBodyOverlay(OverlayFadeDuration);
            _equipment?.EndConsumableUseEquipment();
            _equipment = null;
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            // 상체 오버레이 남은 시간 소모 (실제 종료는 아래 우선순위 처리 뒤 판정)
            _drinkRemaining -= deltaTime;

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
                return;
            }

            // 이동 입력은 상태 전이를 유발하지 않는다. 대신 하체 로코모션만 갱신해
            // 걷기 속도로 이동하면서 상체 마시기 모션을 유지한다.
            UpdateLegLocomotion();

            // 상체 모션이 끝났으면 Idle로 복귀한다.
            if (_drinkRemaining <= 0f)
            {
                TransitionToIdle();
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            Vector3 lookDirection = playerController.LookInputVector;

            if (lookDirection != Vector3.zero && controller.OrientationSharpness > 0f)
            {
                Vector3 smoothedLookInputDirection = Vector3.Slerp(
                    motor.CharacterForward,
                    lookDirection,
                    1 - Mathf.Exp(-controller.OrientationSharpness * deltaTime)).normalized;

                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, motor.CharacterUp);
            }

            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                return;
            }

            // 경사면 보정
            currentVelocity = motor.GetDirectionTangentToSurface(
                currentVelocity,
                motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

            // 이동 입력을 지면 노멀 기준으로 재지향해 걷기 속도로 이동한다(마시는 중엔 걷기 속도로 제한).
            Vector3 moveInputVector = playerController.MoveInputVector;
            Vector3 inputRight = Vector3.Cross(moveInputVector, motor.CharacterUp);
            Vector3 reorientedInput = Vector3.Cross(
                motor.GroundingStatus.GroundNormal,
                inputRight).normalized * moveInputVector.magnitude;

            Vector3 targetMovementVelocity = reorientedInput * controller.MaxWalkMoveSpeed;

            currentVelocity = Vector3.Lerp(
                currentVelocity,
                targetMovementVelocity,
                1f - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }

        /// <summary>
        /// 이동 입력 여부에 따라 하체 로코모션(Layer 0)을 Idle/Walk로 전환한다.
        /// 키가 바뀔 때만 재생하므로 매 프레임 호출해도 안전하다.
        /// </summary>
        private void UpdateLegLocomotion()
        {
            AnimKey desired = playerController.HasMoveInput() ? AnimKey.Walk : AnimKey.Idle;
            if (desired == _legAnimKey)
                return;

            _legAnimKey = desired;
            gameActor.Animator.PlayMotion(desired, LegFadeDuration);
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
