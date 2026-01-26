using UnityEngine;
using UPlayGround.Data.Enum;

namespace UPlayGround.GameActor.MovementController.State
{
    /// <summary>
    /// 공중 상태 - 점프/낙하
    /// </summary>
    public class PlayerAirborneState : PlayerActorState
    {
        public override string StateName => "Airborne";
        
        private bool _hasJumped = false;
        private bool _hasLanded = false;
        private bool _landStarted = false;
        
        private float _timeSinceJumpRequested = 0f;
        private float _timeSinceLastAbleToJump = 0f;

        private float _dragSpeed = 0.1f;
        
        public PlayerAirborneState(ActorMovementController controller) : base(controller)
        {
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _dragSpeed = controller.Drag;
            
            if (playerController.HasJumpInput() == false)
            {
                gameActor.Animator.PlayAnimation(AnimKey.Fall);
            }
        }
        
        public override void UpdateState(float deltaTime)
        {
            _timeSinceLastAbleToJump += deltaTime;
            _timeSinceJumpRequested += deltaTime;
            
            // 지면에 착지하면 상태 전환
            if (_hasLanded 
                || (motor.GroundingStatus.IsStableOnGround && _landStarted == false))
            {
                ChangeToNextState();
                return;
            }
        }
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            Vector3 lookDirection = playerController.LookInputVector;
            
            if (lookDirection != Vector3.zero && controller.OrientationSharpness > 0f)
            {
                // 공중에서도 회전 가능 (약간 느리게)
                float airRotationSharpness = controller.OrientationSharpness * 0.5f;
                Vector3 smoothedLookInputDirection = Vector3.Slerp(
                    motor.CharacterForward, 
                    lookDirection, 
                    1 - Mathf.Exp(-airRotationSharpness * deltaTime)).normalized;
                
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, motor.CharacterUp);
            }

            currentRotation = currentRotation.normalized;
        }
        
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (motor.GroundingStatus.IsStableOnGround == false)
            {
                Vector3 moveInputVector = playerController.MoveInputVector;
                
                Vector3 inputDir = moveInputVector.normalized;
                // 경사면 방지 로직
                if (motor.GroundingStatus.FoundAnyGround)
                {
                    // 캐릭터의 Up 벡터와 지면의 법선(Normal)을 이용해 
                    // 경사면의 기울기 방향을 나타내는 수평 법선을 계산합니다.
                    Vector3 perpendicularObstructionNormal = Vector3.Cross(Vector3.Cross(motor.CharacterUp, motor.GroundingStatus.GroundNormal), motor.CharacterUp).normalized;
                    
                    // 입력 방향을 이 법선 평면에 투영하여 경사면 방향으로 가속되지 않도록 제한합니다.
                    inputDir = Vector3.ProjectOnPlane(inputDir, perpendicularObstructionNormal).normalized;
                }
                // 가속도 계산용, 누른 방향으로의 속도 성분 추출
                float currentSpeedInInputDirection = Vector3.Dot(currentVelocity, inputDir);
                
                // 목표 속도까지 얼마나 더 가속할 수 있는지 여유분 계산
                float speedToGain = controller.MaxAirMoveSpeed - currentSpeedInInputDirection;
                
                if (speedToGain > 0)
                {
                    // 설정된 가속도와 여유분 중 작은 값을 선택하여 가속
                    float accelAmount = controller.AirAccelerationSpeed * deltaTime;
                    float finalAccel = Mathf.Min(accelAmount, speedToGain);
                
                    // 현재 속도에 입력 방향으로의 힘만 더함 (기존 속도는 깎지 않음)
                    currentVelocity += inputDir * finalAccel;
                }
                
                // Gravity
                currentVelocity += controller.Gravity * deltaTime;
            }
                
            // Drag
            currentVelocity *= (1f / (1f + (_dragSpeed * deltaTime)));
            
            HandleJump(ref currentVelocity, deltaTime);
        }


        public override void PostGroundingUpdate(float deltaTime)
        {
            // 착지 감지
            if (motor.GroundingStatus.IsStableOnGround && !motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLanded();
            }
        }
        
        private void HandleJump(ref Vector3 currentVelocity, float deltaTime)
        {
            _timeSinceLastAbleToJump += deltaTime;

            // 점프 실행 판정
            if (playerController.HasJumpInput() && _hasJumped == false)
            {
                // 점프 예약 시간(Pre-buffer) 내에 있고, 점프 가능 시간(Coyote time) 내에 있는 경우
                if (_timeSinceJumpRequested <= controller.JumpPreGroundingGraceTime 
                    && _timeSinceLastAbleToJump <= controller.JumpPostGroundingGraceTime)
                {
                    // 수직 속도 초기화 후 점프 속도 적용
                    currentVelocity = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);
                    currentVelocity += motor.CharacterUp * controller.JumpSpeed;
                    
                    _hasJumped = true;
                    _timeSinceJumpRequested = 0f;
                    motor.ForceUnground(); // 모터를 강제로 공중 상태로 전환
                    
                    PlayJumpAnimation();
                }
            }
            
            _timeSinceJumpRequested += deltaTime;
        }
        
        private void ChangeToNextState()
        {
            // 이동 입력이 있으면 GroundMove, 없으면 Idle
            if (playerController.HasMoveInput())
            {
                controller.TransitionToState(new PlayerGroundMoveState(controller));
            }
            else
            {
                controller.TransitionToState(new PlayerIdleState(controller));
            }
        }

        private void OnLanded()
        {
            Debug.Log("Landed on ground");
            _hasJumped = false;
            _timeSinceLastAbleToJump = 0f;
            
            var state = gameActor.Animator.PlayAnimation(AnimKey.Land, 0.2f);
            if (state != null)
            {
                _landStarted = true;
                _dragSpeed = controller.LandDrag;
                
                state.OwnedEvents.OnEnd += () =>
                {
                    _hasLanded = true;
                };
            }
        }

        private void PlayJumpAnimation()
        {
            var state = gameActor.Animator.PlayAnimation(AnimKey.Jump, 0.2f);
            if (state != null)
            {
                state.OwnedEvents.OnEnd += () =>
                {
                    gameActor.Animator.PlayAnimation(AnimKey.Fall, 0.2f);
                };
            }
        }
    }
}