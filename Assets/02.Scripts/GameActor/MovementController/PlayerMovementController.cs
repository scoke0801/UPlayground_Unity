using KinematicCharacterController;
using UnityEngine;

namespace UPlayGround.GameActor.MovementController
{
    public partial class PlayerMovementController : ActorMovementController
    {
        private Vector3 _moveInputVector; // 입력값 캐싱
        private Vector3 _lookInputVector;
        
        // 점프 관련 상태 변수
        private bool _jumpRequested = false;
        private bool _jumpConsumed = false;
        
        private float _timeSinceJumpRequested = Mathf.Infinity;
        private float _timeSinceLastAbleToJump = 0f;
        
        // PlayerActor에서 호출하여 입력 전달
        public void SetInputs(Vector2 moveInput, Quaternion cameraRotation, bool jumpDown)
        {
            // 1. 기본적인 이동 입력 벡터 (X, Z)
            Vector3 rawMoveInput = new Vector3(moveInput.x, 0f, moveInput.y);

            // 2. 카메라가 바라보는 방향을 지면(CharacterUp)에 투영하여 기준 방향 설정
            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(cameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(cameraRotation * Vector3.up, Motor.CharacterUp).normalized;
            }
            
            // 3. 카메라 기준의 회전값 생성
            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);

            // 4. 입력 벡터를 카메라 회전에 맞춰 변환 (카메라 앞방향이 캐릭터의 이동 앞방향이 됨)
            _moveInputVector = cameraPlanarRotation * rawMoveInput;
            
            // 5. 캐릭터가 바라볼 방향 설정 (이동 중일 때만 업데이트하거나 카메라 정면 유지)
            if (_moveInputVector.sqrMagnitude > 0f)
            {
                _lookInputVector = _moveInputVector.normalized;
            }
            
            // 점프 입력 처리
            if (jumpDown)
            {
                _timeSinceJumpRequested = 0f;
                _jumpRequested = true;
            }
        }
        
        #region ICharacterController
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_lookInputVector != Vector3.zero && OrientationSharpness > 0f)
            {
                // 부드럽게 이동 방향으로 캐릭터 회전
                Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
            }
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 지면 상태 업데이트
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                _timeSinceLastAbleToJump = 0f;
                _jumpConsumed = false;

                // 경사로 이동 보정: 현재 속도를 지면 기울기에 맞게 재지향
                currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;
                
                // 지면 노멀을 고려한 타겟 속도 계산
                Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                Vector3 reorientedInput = Vector3.Cross(Motor.GroundingStatus.GroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                Vector3 targetMovementVelocity = reorientedInput * MaxStableMoveSpeed;
                currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-StableMovementSharpness * deltaTime));
            }
            else
            {
                Vector3 inputDir = _moveInputVector.normalized;
                float currentSpeedInInputDirection = Vector3.Dot(currentVelocity, inputDir);

                // 목표 속도까지 얼마나 더 가속할 수 있는지 여유분 계산
                float speedToGain = MaxAirMoveSpeed - currentSpeedInInputDirection;

                if (speedToGain > 0)
                {
                    // 설정된 가속도와 여유분 중 작은 값을 선택하여 가속
                    float accelAmount = AirAccelerationSpeed * deltaTime;
                    float finalAccel = Mathf.Min(accelAmount, speedToGain);

                    // 현재 속도에 입력 방향으로의 힘만 더함 (기존 속도는 깎지 않음)
                    currentVelocity += inputDir * finalAccel;
                }
                
                // Gravity
                currentVelocity += Gravity * deltaTime;

                // Drag
                currentVelocity *= (1f / (1f + (Drag * deltaTime)));
            }

            HandleJump(ref currentVelocity, deltaTime);
        }

        public override void BeforeCharacterUpdate(float deltaTime)
        {
        }

        public override void PostGroundingUpdate(float deltaTime)
        {
        }

        public override void AfterCharacterUpdate(float deltaTime)
        {
        }

        public override bool IsColliderValidForCollisions(Collider coll)
        {
            // 자기 자신의 콜라이더가 아니면 충돌 허용
            return coll != Motor.Capsule;
        }

        public override void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
        }

        public override void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition,
            Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        public override void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
        #endregion
    }

    public partial class PlayerMovementController : ActorMovementController
    {
        // [TODO] 별도 상태로 구분해서 처리할 수 있도록 구조를 분리할 지 정리 필요
        private void HandleJump(ref Vector3 currentVelocity, float deltaTime)
        {
            _timeSinceLastAbleToJump += deltaTime;

            // 점프 실행 판정
            if (_jumpRequested)
            {
                // 점프 예약 시간(Pre-buffer) 내에 있고, 점프 가능 시간(Coyote time) 내에 있는 경우
                if (!_jumpConsumed && _timeSinceJumpRequested <= JumpPreGroundingGraceTime && _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime)
                {
                    // 수직 속도 초기화 후 점프 속도 적용
                    currentVelocity = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);
                    currentVelocity += Motor.CharacterUp * JumpSpeed;
                    
                    _jumpRequested = false;
                    _jumpConsumed = true;
                    
                    Motor.ForceUnground(); // 모터를 강제로 공중 상태로 전환
                }
            }
            
            _timeSinceJumpRequested += deltaTime;
        }
    }
}