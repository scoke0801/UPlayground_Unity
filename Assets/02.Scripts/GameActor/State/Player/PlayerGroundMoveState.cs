using UnityEngine;
using UPlayGround.Data.Enum;

namespace UPlayGround.GameActor.MovementController.State
{
    /// <summary>
    /// 지면 이동 상태 - 걷기/달리기
    /// </summary>
    public class PlayerGroundMoveState : PlayerActorState
    {
        public override string StateName => "GroundMove";

        private BaseMoveAnimType _cachedAnimType = BaseMoveAnimType.Run;
        
        public PlayerGroundMoveState(ActorMovementController controller) : base(controller)
        {
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _cachedAnimType = gameActor.MoveAnimType;
            gameActor.Animator.PlayAnimation(GetMoveAnimKey(), 0.25f);            
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
            
            // 웅크리기 입력이 있으면 Crouching 상태로 전환
            if (playerController.HasCrouchInput())
            {
                playerController.TransitionToState(new PlayerCrouchingState(controller));
                return;
            }
            
            // 지면에서 떨어지면 Airborne 상태로 전환
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                playerController.TransitionToState(new PlayerAirborneState(controller));
                return;
            }
            
            // 이동 입력이 없으면 Idle 상태로 전환
            if (!playerController.HasMoveInput())
            {
                playerController.TransitionToState(new PlayerIdleState(controller));
                return;
            }

            if (_cachedAnimType != gameActor.MoveAnimType)
            {
                _cachedAnimType = gameActor.MoveAnimType;
                gameActor.Animator.PlayAnimation(GetMoveAnimKey(), 0.25f);         
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