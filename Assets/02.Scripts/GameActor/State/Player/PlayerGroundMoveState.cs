using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.State
{
    /// <summary>
    /// 지면 이동 상태 - 걷기/달리기
    /// </summary>
    public class PlayerGroundMoveState : PlayerActorState
    {
        public override ActorStateId StateId => ActorStateId.GroundMove;
        
        private float _runTimer;
        private float _sprintAutoChangeDealy = 0f;

        private BaseMoveAnimType _cachedAnimType = BaseMoveAnimType.Run;
        
        internal PlayerGroundMoveState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(ActorStateId fromState)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            gameActor.Tags?.AddTag(GameplayTags.State_Move);

            _runTimer = gameActor.ActorTime;

            _sprintAutoChangeDealy = playerActor.PlayerController.SprintAutoStartDelay;

            _cachedAnimType = gameActor.MoveAnimType;
            gameActor.Animator.PlayMotion(GetMoveAnimKey(), 0.25f);
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Tags?.RemoveTag(GameplayTags.State_Move);
            gameActor.Tags?.RemoveTag(GameplayTags.State_Sprint);
            gameActor.MoveAnimType = BaseMoveAnimType.Run;
            base.OnExit(toState);
        }
        public override void UpdateState(float deltaTime)
        {
            // 점프 입력이 있으면 Airborne 상태로 전환
            if (playerController.HasJumpInput())
            {
                playerController.TransitionToState(ActorStateId.Airborne);
                return;
            }

            if (playerController.HasDodgeInput())
            {
                playerController.TransitionToState(new PlayerDodgeState(playerController));
                return;
            }

            if (playerController.HasDashInput())
            {
                if (controller.TryTransitionToState(new PlayerDashState(controller)))
                {
                    return;
                }
            }

            // 웅크리기 입력이 있으면 Crouching 상태로 전환
            if (playerController.HasCrouchInput())
            {
                playerController.TransitionToState(new PlayerCrouchingState(controller));
                return;
            }

            // 지면에서 떨어지면 Airborne 상태로 전환 (유예 시간 적용)
            if (ShouldTransitionToAirborne(deltaTime))
            {
                playerController.TransitionToState(ActorStateId.Airborne);
                return;
            }

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

            // 이동 입력이 없으면 Stop 전환 시도 — 전방 기본 클립이 등록된 경우에만 Stop 상태 사용
            if (!playerController.HasMoveInput())
            {
                var forwardStopKey = PlayerStopState.GetStopAnimKeyForward(gameActor.MoveAnimType);
                bool hasStop = gameActor.Animator.HasMotion(forwardStopKey, true);
                if (hasStop && gameActor.MoveAnimType == BaseMoveAnimType.Sprint)
                {
                    playerController.TransitionToState(
                        new PlayerStopState(controller, gameActor.MoveAnimType, playerController.LookInputVector));
                }
                else
                {
                    playerController.TransitionToState(ActorStateId.Idle);
                }
                return;
            }

            if (playerController.HasGuardInput())
            {
                playerController.TransitionToState(new PlayerGuardState(playerController));
                return;
            }

            // 공격 입력 소비는 PlayerAttackState.OnEnter의 중재기가 한 번만 담당한다.
            // 여기서 먼저 소비하면 빠른 연타의 두 번째 입력까지 OnEnter에서 연달아 소비되어
            // 콤보 선입력이 사라진다.
            if (Svc.Input.InputBuffer.HasInput(PlayerAction.Attack))
            {
                if (PlayerAttackState.TryEnter(playerController))
                    return;
            }

            // 차지 공격: 홀드 임계값 초과 시 우선 진입 (HeavyAttack 버퍼 체크보다 앞)
            if (playerController.IsChargeAttackHeld())
            {
                playerController.TransitionToState(new PlayerChargeState(playerController));
                return;
            }

            if (Svc.Input.InputBuffer.HasInput(PlayerAction.HeavyAttack))
            {
                if (gameActor.MoveAnimType == BaseMoveAnimType.Sprint
                    && playerController.TryTransitionToState(new PlayerDashAttackState(playerController)))
                {
                    Svc.Input.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack);
                    return;
                }

                // TryEnter 가 OnEnter 안에서 HeavyAttack 입력을 소비한다.
                if (PlayerAttackState.TryEnter(playerController))
                    return;
            }


            var skillGauge = playerActor.SkillGauge;
            for (int i = 0; i < PlayerAbilityResourceView.SkillSlotCount; i++)
            {
                if (skillGauge == null) break;
                if (!playerController.HasSkillInput(i)) continue;
                if (skillGauge.CanUseSkill(i) == false) continue;

                if (PlayerAttackState.TryEnter(playerController)) return;
            }
            
            if (_cachedAnimType != gameActor.MoveAnimType)
            {
                _cachedAnimType = gameActor.MoveAnimType;
                gameActor.Animator.PlayMotion(GetMoveAnimKey(), 0.25f);
            }

            if (_runTimer + _sprintAutoChangeDealy < gameActor.ActorTime)
            {
                gameActor.MoveAnimType = BaseMoveAnimType.Sprint;
                gameActor.Tags?.AddTag(GameplayTags.State_Sprint);
                _runTimer = float.MaxValue; // 자동 전환은 상태 진입 후 1회만 발동
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            Vector3 lookDirection = playerController.LookInputVector;
            
            if (lookDirection != Vector3.zero && controller.OrientationSharpness > 0f)
            {
                // 부드럽게 이동 방향으로 회전
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
            if (motor.GroundingStatus.IsStableOnGround)
            {
                // 경사로 이동 보정: 현재 속도를 지면 기울기에 맞게 재지향
                currentVelocity = motor.GetDirectionTangentToSurface(
                    currentVelocity, 
                    motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;
            
                // 지면 노멀을 고려한 타겟 속도 계산
                Vector3 moveInputVector = playerController.MoveInputVector;
                Vector3 inputRight = Vector3.Cross(moveInputVector, motor.CharacterUp);
                Vector3 reorientedInput = Vector3.Cross(
                    motor.GroundingStatus.GroundNormal, 
                    inputRight).normalized * moveInputVector.magnitude;
            
                Vector3 targetMovementVelocity = reorientedInput * GetMaxMovementSpeed();
            
                // 부드럽게 목표 속도로 이동
                currentVelocity = Vector3.Lerp(
                    currentVelocity, 
                    targetMovementVelocity, 
                    1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
            }
        }

        private float GetMaxMovementSpeed()
        {
            switch (gameActor.MoveAnimType)
            {
                case BaseMoveAnimType.Walk: return controller.MaxWalkMoveSpeed;
                case BaseMoveAnimType.Sprint: return controller.MaxSprintMoveSpeed;
                case BaseMoveAnimType.Run: return controller.MaxRunMoveSpeed;        
                default: break;
            }

            return controller.MaxRunMoveSpeed;
        }
        private UPlayGround.Gameplay.Tag.GameplayTag GetMoveAnimKey()
        {
            switch (gameActor.MoveAnimType)
            {
                case BaseMoveAnimType.Walk: return UPlayGround.Data.Actor.Animation.MotionTags.Walk;
                case BaseMoveAnimType.Sprint: return UPlayGround.Data.Actor.Animation.MotionTags.Sprint;
                case BaseMoveAnimType.Run: return UPlayGround.Data.Actor.Animation.MotionTags.Run;
                default: break;
            }

            // 기본은 달리기
            return UPlayGround.Data.Actor.Animation.MotionTags.Run;
        }
    }
}
