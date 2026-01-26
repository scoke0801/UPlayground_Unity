using UnityEngine;

namespace UPlayGround.GameActor.MovementController.State
{
    /// <summary>
    /// 웅크리기 상태
    /// </summary>
    public class PlayerCrouchingState : PlayerActorState
    {
        public override string StateName => "Crouching";
        
        private Collider[] _probedColliders = new Collider[8];
        private const float CrouchSpeedMultiplier = 0.5f;
        
        public PlayerCrouchingState(ActorMovementController controller) : base(controller)
        {
        }
        
        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            
            // 캡슐 크기 축소
            motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
            controller.MeshRoot.localScale = new Vector3(1f, 0.5f, 1f);
        }
        
        public override void OnExit(GameActorState toState)
        {
            base.OnExit(toState);
            
            // 캡슐 크기 복원 (안전하게 체크 후)
            TryStandUp();
        }
        
        public override void UpdateState(float deltaTime)
        {
            // 웅크리기 해제 입력이 있으면 일어서기 시도
            if (!playerController.HasCrouchInput())
            {
                if (CanStandUp())
                {
                    // 이동 입력이 있으면 GroundMove, 없으면 Idle
                    if (playerController.HasMoveInput())
                    {
                        playerController.TransitionToState(new PlayerGroundMoveState(controller));
                    }
                    else
                    {
                        playerController.TransitionToState(new PlayerIdleState(controller));
                    }
                    return;
                }
            }
            
            // 점프 입력 (웅크린 상태에서는 점프 불가능하게 할 수도 있음)
            // if (controller.HasJumpInput)
            // {
            //     controller.TransitionToState(new AirborneState(controller));
            //     return;
            // }
            
            // 지면에서 떨어지면 Airborne 상태로 전환
            if (!motor.GroundingStatus.IsStableOnGround)
            {
                controller.TransitionToState(new PlayerAirborneState(controller));
                return;
            }
        }
        
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            Vector3 lookDirection = playerController.LookInputVector;
            
            if (lookDirection != Vector3.zero && controller.OrientationSharpness > 0f)
            {
                // 웅크린 상태에서는 회전이 조금 느림
                float crouchRotationSharpness = controller.OrientationSharpness * 0.7f;
                Vector3 smoothedLookInputDirection = Vector3.Slerp(
                    motor.CharacterForward, 
                    lookDirection, 
                    1 - Mathf.Exp(-crouchRotationSharpness * deltaTime)).normalized;
                
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, motor.CharacterUp);
            }

            currentRotation = currentRotation.normalized;
        }
        
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 경사로 이동 보정
            currentVelocity = motor.GetDirectionTangentToSurface(
                currentVelocity, 
                motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;
            
            // 지면 노멀을 고려한 타겟 속도 계산 (웅크린 상태는 느림)
            Vector3 moveInputVector = playerController.MoveInputVector;
            Vector3 inputRight = Vector3.Cross(moveInputVector, motor.CharacterUp);
            Vector3 reorientedInput = Vector3.Cross(
                motor.GroundingStatus.GroundNormal, 
                inputRight).normalized * moveInputVector.magnitude;
            
            Vector3 targetMovementVelocity = reorientedInput * controller.MaxSprintMoveSpeed * CrouchSpeedMultiplier;
            
            // 부드럽게 목표 속도로 이동
            currentVelocity = Vector3.Lerp(
                currentVelocity, 
                targetMovementVelocity, 
                1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }
        
        /// <summary>
        /// 일어설 수 있는지 체크 (천장 확인)
        /// </summary>
        private bool CanStandUp()
        {
            // 일어선 크기로 임시 변경하여 충돌 체크
            motor.SetCapsuleDimensions(0.5f, 2f, 1f);
            
            int hitCount = motor.CharacterCollisionsOverlap(
                motor.TransientPosition,
                motor.TransientRotation,
                _probedColliders);
            
            // 원래 크기로 되돌림
            motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
            
            return hitCount == 0;
        }
        
        /// <summary>
        /// 안전하게 일어서기 시도
        /// </summary>
        private void TryStandUp()
        {
            if (CanStandUp())
            {
                motor.SetCapsuleDimensions(0.5f, 2f, 1f);
                controller.MeshRoot.localScale = new Vector3(1f, 1f, 1f);
            }
            else
            {
                // 일어설 수 없으면 웅크린 상태 유지
                motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
            }
        }
    }
}