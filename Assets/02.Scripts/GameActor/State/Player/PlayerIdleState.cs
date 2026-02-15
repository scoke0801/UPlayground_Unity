using System.Collections;
using UnityEngine;
using UPlayGround.Data.Enum;
using UPlayGround.Component;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 대기 상태 - 지면에 서있고 움직이지 않는 상태
    /// </summary>
    public class PlayerIdleState : PlayerActorState
    {
        public override string StateName => "Idle";
        
        private PlayerEquipment _equipment;
        public PlayerIdleState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionToState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _equipment = playerActor.GetPlayerEquipment();
            gameActor.Animator.PlayMotion(AnimKey.Idle, 0.25f);
        }

        public override void UpdateState(float deltaTime)
        {
            // 지면에서 떨어지면 Airborne 상태로 전환
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                playerController.TransitionToState(new PlayerAirborneState(playerController));
                return;
            }

            // 인터렉션 상태로 전환
            if (playerController.HasInteractInput())
            {
                playerController.TransitionToState(new PlayerInteractionState(playerController));
                return;
            }
            
            // 이동 입력이 있으면 GroundMove 상태로 전환
            if (playerController.HasMoveInput())
            {
                playerController.TransitionToState(new PlayerGroundMoveState(playerController));
                return;
            }
            
            // 점프 입력이 있으면 Airborne 상태로 전환
            if (playerController.HasJumpInput())
            {
                playerController.TransitionToState(new PlayerAirborneState(playerController));
                return;
            }

            if (playerController.HasDodgeInput())
            {
                playerController.TransitionToState(new PlayerDodgeState(playerController));
                return;
            }
            
            // 웅크리기 입력이 있으면 Crouching 상태로 전환
            if (playerController.HasCrouchInput())
            {
                playerController.TransitionToState(new PlayerCrouchingState(playerController));
                return;
            }

            //if (playerActor.IsEquippedRightWeapon || playerActor.IsEquippedLeftWeapon)
            {
                if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Attack) != null)
                {
                    playerController.TransitionToState(new PlayerAttackState(playerController));
                    return;
                }
                if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null)
                {
                    playerController.TransitionToState(new PlayerAttackState(playerController));
                    return;
                }

                for (int i = 0; i < 4; ++i)
                {
                    if (!playerController.HasSkillInput(i)) continue;

                    playerController.TransitionToState(new PlayerAttackState(playerController));
                    return;
                }
            }

            if (playerController.HasEquipInput() )
            {
                PlayEquipItem();
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

        private void PlayEquipItem()
        {
            if(playerActor.GetPlayerEquipment().GetMainWeaponType() == WeaponType.NoWeapon)
            {
                var animState = gameActor.Animator.PlayMotion(AnimKey.Equip_Katana, 0.25f);
                if (animState != null)
                {
                    playerActor.GetPlayerEquipment().TEST_SetWeaponType(WeaponType.Katana);

                    animState.OwnedEvents.OnEnd += () =>
                    {    
                        gameActor.Animator.PlayMotion(AnimKey.Idle, 0.1f);
                    };
                }
            }
            else
            {
                var animState = gameActor.Animator.PlayMotion(AnimKey.UnEquip_Katana, 0.25f);
                if (animState != null)
                {
                    animState.OwnedEvents.OnEnd += () =>
                    {
                        playerActor.GetPlayerEquipment().TEST_SetWeaponType(WeaponType.NoWeapon);
                        gameActor.Animator.PlayMotion(AnimKey.Idle, 0.1f);
                    };
                }
            }
        }
    
        private void PlayEquip_Left()
        {
            if (playerActor.IsEquippedLeftWeapon == false)
            {
                var animState = gameActor.Animator.PlayMotion(AnimKey.Equip_LeftWeapon, 0.25f);
                if (animState != null)
                {
                    animState.OwnedEvents.OnEnd += () =>
                    {
                        gameActor.Animator.PlayMotion(AnimKey.Idle, 0.1f);
                    };
                }
            }
            else
            {
                var animState = gameActor.Animator.PlayMotion(AnimKey.Equip_LeftWeapon, 0.25f);
                if (animState != null)
                {
                    animState.OwnedEvents.OnEnd += () =>
                    {
                        gameActor.Animator.PlayMotion(AnimKey.Idle, 0.1f);
                    };
                }
            }
        }
    }
}
