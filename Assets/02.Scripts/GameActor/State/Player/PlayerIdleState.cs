using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Components;
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

        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            _equipment = playerActor.GetPlayerEquipment();
            gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Idle, 0.25f);
        }

        public override void UpdateState(float deltaTime)
        {
            // 점프 입력이 있으면 Airborne 상태로 전환 (낙하 판정보다 먼저 체크)
            if (Svc.Input.InputBuffer.HasInput(PlayerAction.Jump))
            {
                playerController.TransitionToState(new PlayerAirborneState(playerController));
                return;
            }

            // 지면에서 떨어지면 Airborne 상태로 전환 (유예 시간 적용)
            if (ShouldTransitionToAirborne(deltaTime))
            {
                playerController.TransitionToState(new PlayerAirborneState(playerController));
                return;
            }

            // 인터렉션 상태로 전환
            if (playerController.HasInteractInput())
            {
                PlayerCombat combat = playerActor.GetCombat();
                Transform breakTarget = combat != null ? combat.FindSpecialBreakAttackTarget() : null;
                if (breakTarget != null)
                {
                    Svc.Input.InputBuffer.ConsumeInput(PlayerAction.Interact);
                    playerController.TransitionToState(new PlayerSpecialBreakAttackState(playerController, breakTarget));
                    return;
                }

                if (!playerActor.CanStartInteraction())
                    return;

                playerController.TransitionToState(new PlayerInteractionState(playerController));
                return;
            }

            // 이동 입력이 있으면 GroundMove 상태로 전환
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
            
            if (Svc.Input.InputBuffer.HasInput(PlayerAction.Dash))
            {
                if (controller.TryTransitionToState(new PlayerDashState(controller)))
                {
                    return;
                }
            }
            
            // 웅크리기 입력이 있으면 Crouching 상태로 전환
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

            //if (playerActor.IsEquippedRightWeapon || playerActor.IsEquippedLeftWeapon)
            {
                if (Svc.Input.InputBuffer.HasInput(PlayerAction.Attack))
                {
                    if (PlayerAttackState.TryEnter(playerController)) return;
                }

                // 차지 공격: 홀드 임계값 초과 시 우선 진입 (HeavyAttack 버퍼 체크보다 앞)
                if (playerController.IsChargeAttackHeld())
                {
                    playerController.TransitionToState(new PlayerChargeState(playerController));
                    return;
                }

                if (Svc.Input.InputBuffer.HasInput(PlayerAction.HeavyAttack))
                {
                    if (PlayerAttackState.TryEnter(playerController)) return;
                }

               
                var skillGauge = playerActor.SkillGauge;
                for (int i = 0; i < PlayerAbilityResourceView.SkillSlotCount; i++)
                {
                    if (skillGauge == null) break;
                    if (!playerController.HasSkillInput(i)) continue;
                    if (skillGauge.CanUseSkill(i) == false) continue;

                    if (PlayerAttackState.TryEnter(playerController)) return;
                }
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
                var animState = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Equip_Weapon, 0.25f);
                if (animState != null)
                {
                    if (playerActor.CharacterType == CharacterActorType.Bokusei)
                    {
                        playerActor.GetPlayerEquipment().SetWeaponType(WeaponType.Katana);
                    }
                    else if (playerActor.CharacterType == CharacterActorType.Honoka)
                    {
                        playerActor.GetPlayerEquipment().SetWeaponType(WeaponType.DoubleAxe);
                    }

                    animState.OwnedEvents.OnEnd += () =>
                    {    
                        gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Idle, 0.1f);
                    };
                }
            }
            else
            {
                var animState = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.UnEquip_Weapon, 0.25f);
                if (animState != null)
                {
                    animState.OwnedEvents.OnEnd += () =>
                    {
                        playerActor.GetPlayerEquipment().SetWeaponType(WeaponType.NoWeapon);
                        gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Idle, 0.1f);
                    };
                }
            }
        }
    
        private void PlayEquip_Left()
        {
            if (playerActor.IsEquippedLeftWeapon == false)
            {
                var animState = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Equip_LeftWeapon, 0.25f);
                if (animState != null)
                {
                    animState.OwnedEvents.OnEnd += () =>
                    {
                        gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Idle, 0.1f);
                    };
                }
            }
            else
            {
                var animState = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Equip_LeftWeapon, 0.25f);
                if (animState != null)
                {
                    animState.OwnedEvents.OnEnd += () =>
                    {
                        gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Idle, 0.1f);
                    };
                }
            }
        }
    }
}
