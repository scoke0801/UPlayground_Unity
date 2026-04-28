using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.State
{
    /// <summary>
    /// 공중 상태 - 점프/낙하
    /// </summary>
    public class PlayerAirborneState : PlayerActorState
    {
        public override string StateName => "Airborne";
        public override bool AdjustGravity => false;
        
        private int _remainingJumps;
        private bool _hasLanded = false;
        private bool _landStarted = false;
        private bool _jumpAnimPlayed = false;
        private bool _fallAnimPlayed = false;
        private bool _hasLeftGround; // 한 번이라도 실제로 지면을 떠났는지

        private float _timeSinceJumpRequested = 0f;
        private float _timeSinceLastAbleToJump = 0f;

        private float _dragSpeed = 0.1f;
        
        public PlayerAirborneState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);
            gameActor.Tags?.AddTag(GameplayTagId.State_Airborne);

            if (playerActor.FootIK != null) playerActor.FootIK.ForceDisabled = true;

            _dragSpeed = controller.Drag;
            _remainingJumps = playerController.MaxJumpCount;
            _jumpAnimPlayed = false;

            if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Jump) == null)
            {
                _remainingJumps -= 1;
                gameActor.Animator.PlayMotion(AnimKey.Fall, 0.2f);
                _fallAnimPlayed = true;
            }
            else
            {
                gameActor.Tags?.AddTag(GameplayTagId.State_Jump);
                // 점프 입력으로 진입: UpdateVelocity(HandleJump) 실행을 기다리지 않고 즉시 재생
                PlayJumpAnimation(true);
                _jumpAnimPlayed = true;
            }
        }

        public override void OnExit(GameActorState state)
        {
            gameActor.Tags?.RemoveTag(GameplayTagId.State_Airborne);
            gameActor.Tags?.RemoveTag(GameplayTagId.State_Jump);
            playerActor.ClearJumpInput();
            if (playerActor.FootIK != null) playerActor.FootIK.ForceDisabled = false;

            base.OnExit(state);
        }

        public override void UpdateState(float deltaTime)
        {
            _timeSinceLastAbleToJump += deltaTime;
            _timeSinceJumpRequested += deltaTime;

            if (InputManager.Instance.InputBuffer.ConsumeInput(PlayerAction.Dash) != null)
            {
                if (controller.TryTransitionToState(new PlayerDashState(controller)))
                {
                    return;
                }
            }
            if (InputManager.Instance.InputBuffer.HasInput(PlayerAction.Attack))
            {
                if (playerController.TryTransitionToState(new PlayerJumpAttackState(playerController, startAsFinish: false)))
                    return;
            }
            if (InputManager.Instance.InputBuffer.HasInput(PlayerAction.HeavyAttack))
            {
                if (playerController.TryTransitionToState(new PlayerJumpAttackState(playerController, startAsFinish: true)))
                    return;
            }
            
            if (_hasLanded)
            {
                ChangeToNextState();
                return;
            }

            // 한 번이라도 실제로 공중에 다녀온 후에만 조기 종료 가능.
            // 점프 직후 1프레임은 KCC의 grounding 상태가 아직 갱신되지 않아
            // IsStableOnGround가 true로 남아있을 수 있어 잘못 발화하는 것을 방지.
            if (_hasLeftGround
                && playerController.HasJumpInput() == false
                && motor.GroundingStatus.IsStableOnGround
                && _landStarted == false)
            {
                ChangeToNextState();
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
                
                // 가변 중력 적용
                float verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                float gravityMultiplier = verticalSpeed < 0f
                    ? controller.FallGravityMultiplier   // 하강 중
                    : controller.RiseGravityMultiplier;  // 상승 중
                // Gravity
                
                currentVelocity += gravityMultiplier * deltaTime * controller.Gravity;
            }
                
            // Drag
            currentVelocity *= (1f / (1f + (_dragSpeed * deltaTime)));

            HandleJump(ref currentVelocity, deltaTime);

            // 정점 통과(수직 속도 ≤ 0) 시점에 Fall 애니메이션으로 자연스럽게 페이드.
            // Jump 클립의 OnEnd에 의존하지 않도록 물리 기반으로 트리거.
            if (_jumpAnimPlayed && !_fallAnimPlayed)
            {
                float verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                if (verticalSpeed <= 0f)
                {
                    gameActor.Animator.PlayMotion(AnimKey.Fall, 0.2f);
                    _fallAnimPlayed = true;
                }
            }
        }


        public override void PostGroundingUpdate(float deltaTime)
        {
            // 실제로 지면을 떠난 시점 기록 (조기 종료 가드용)
            if (!_hasLeftGround && !motor.GroundingStatus.IsStableOnGround)
            {
                _hasLeftGround = true;
            }

            // 착지 감지
            if (motor.GroundingStatus.IsStableOnGround && !motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLanded();
            }
        }
        
        private void HandleJump(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!playerController.HasJumpInput() || _remainingJumps <= 0)
                return;
            
            bool isFirstJump = _remainingJumps == playerController.MaxJumpCount;

            // 1단 점프: 코요테 타임 체크
            if (isFirstJump)
            {
                bool withinPreBuffer  = _timeSinceJumpRequested <= controller.JumpPreGroundingGraceTime;
                bool withinCoyoteTime = _timeSinceLastAbleToJump <= controller.JumpPostGroundingGraceTime;

                if (!withinPreBuffer || !withinCoyoteTime)
                    return;
            }

            // 점프 실행
            playerActor.ClearJumpInput();

            float jumpSpeed = isFirstJump ? controller.JumpSpeed : playerController.DoubleJumpSpeed;

            // 수직 속도 초기화 후 점프
            currentVelocity = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);
            currentVelocity += motor.CharacterUp * jumpSpeed;

            _remainingJumps--;
            _timeSinceJumpRequested = 0f;
            motor.ForceUnground();

            // 첫 점프는 OnEnter에서 이미 재생했을 수 있으므로 중복 방지.
            // 더블 점프는 항상 새로 재생해야 함 (DoubleJump 모션).
            if (!_jumpAnimPlayed || !isFirstJump)
            {
                PlayJumpAnimation(isFirstJump);
                _jumpAnimPlayed = true;
            }
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
            _remainingJumps  = 0;
            _timeSinceLastAbleToJump = 0f;
            
            var state = gameActor.Animator.PlayMotion(AnimKey.Land, 0.2f);
            if (state != null)
            {
                _landStarted = true;
                _dragSpeed = controller.LandDrag;
        
                state.OwnedEvents.OnEnd += ChangeToNextState;
            }
            else
            {
                // 애니메이션이 없거나 찾을 수 없는 경우 즉시 다음 상태로
                ChangeToNextState();
            }
        }

        private void PlayJumpAnimation(bool isFirstJump)
        {
            AnimKey jumpKey = AnimKey.Jump;

            if (gameActor.Animator.HasMotion(AnimKey.DoubleJump, true) == true
                && isFirstJump == false)
            {
                jumpKey = AnimKey.DoubleJump;
            }

            // 즉발 액션이므로 페이드를 짧게 — 0.2f는 도약 시작이 한 박자 늦어보임
            gameActor.Animator.PlayMotion(jumpKey, 0.05f);
            // Jump → Fall 전이는 UpdateVelocity의 수직 속도 체크에서 처리 (정점 통과 시점)
            _fallAnimPlayed = false;
        }
    }
}