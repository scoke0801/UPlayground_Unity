using UnityEngine;
using UPlayGround.Component;
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
        public override string StateName => "GroundMove";
        
        private float _runTimer;
        private float _sprintAutoChangeDealy = 0f;

        private BaseMoveAnimType _cachedAnimType = BaseMoveAnimType.Run;
        
        public PlayerGroundMoveState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            gameActor.Tags?.AddTag(GameplayTagId.State_Move);

            _runTimer = Time.realtimeSinceStartup;

            _sprintAutoChangeDealy = playerActor.PlayerController.SprintAutoStartDelay;

            _cachedAnimType = gameActor.MoveAnimType;
            gameActor.Animator.PlayMotion(GetMoveAnimKey(), 0.25f);
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Tags?.RemoveTag(GameplayTagId.State_Move);
            gameActor.Tags?.RemoveTag(GameplayTagId.State_Sprint);
            gameActor.MoveAnimType = BaseMoveAnimType.Run;
            base.OnExit(toState);
        }
        public override void UpdateState(float deltaTime)
        {
            // 점프 입력이 있으면 Airborne 상태로 전환
            if (playerController.HasJumpInput())
            {
                playerController.TransitionToState(new PlayerAirborneState(controller));
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
                playerController.TransitionToState(new PlayerAirborneState(controller));
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
                    playerController.TransitionToState(new PlayerIdleState(controller));
                }
                return;
            }

            if (playerController.HasGuardInput())
            {
                playerController.TransitionToState(new PlayerGuardState(playerController));
                return;
            }

            if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Attack) != null)
            {
                playerController.TransitionToState(new PlayerAttackState(playerController));
                
                return;
            }

            // 차지 공격: 홀드 임계값 초과 시 우선 진입 (HeavyAttack 버퍼 체크보다 앞)
            if (playerController.IsChargeAttackHeld())
            {
                playerController.TransitionToState(new PlayerChargeState(playerController));
                return;
            }

            if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.HeavyAttack) != null)
            {
                if (gameActor.MoveAnimType == BaseMoveAnimType.Sprint)
                {
                    playerController.TransitionToState(new PlayerDashAttackState(playerController));
                }
                else
                {
                    playerController.TransitionToState(new PlayerAttackState(playerController));
                }

                return;
            }

           
            var skillGauge = playerActor.SkillGauge;
            for (int i = 0; i < 10; i++)
            {
                if (skillGauge == null) break;
                if (!playerController.HasSkillInput(i)) continue;
                if (skillGauge.CanUseSkill(i) == false) continue; 

                playerController.TransitionToState(new PlayerAttackState(playerController));
                return;
            }
            
            if (_cachedAnimType != gameActor.MoveAnimType)
            {
                _cachedAnimType = gameActor.MoveAnimType;
                gameActor.Animator.PlayMotion(GetMoveAnimKey(), 0.25f);
            }

            if (_runTimer + _sprintAutoChangeDealy < Time.realtimeSinceStartup)
            {
                gameActor.MoveAnimType = BaseMoveAnimType.Sprint;
                gameActor.Tags?.AddTag(GameplayTagId.State_Sprint);
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
        private AnimKey GetMoveAnimKey()
        {
            switch (gameActor.MoveAnimType)
            {
                case BaseMoveAnimType.Walk: return AnimKey.Walk;
                case BaseMoveAnimType.Sprint: return AnimKey.Sprint;
                case BaseMoveAnimType.Run: return AnimKey.Run;          
                default: break;
            }

            // 기본은 달리기
            return AnimKey.Run;
        }
    }
}