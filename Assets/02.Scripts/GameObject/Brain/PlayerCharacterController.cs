using KinematicCharacterController;
using UnityEngine;

namespace Game.FSM
{
    public class PlayerCharacterController : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor;

        private CharacterBrain _brain;
        public Vector3 MoveInputVector { get; private set; }
        
        private Vector3 _internalVelocityAdd = Vector3.zero;

        private void Awake()
        {
            Motor.CharacterController = this;
        }

        public void AddVelocity(Vector3 velocity)
        {
            _internalVelocityAdd += velocity;
        }

        public void SetBrain(CharacterBrain brain)
        {
            _brain = brain;
        }
        
        public void SetInputs(Vector3 moveInput)
        {
            MoveInputVector = moveInput;
        }

        public void BeforeCharacterUpdate(float deltaTime) { }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_brain.CurrentState is IMovementState movementState)
            {
                movementState.UpdateRotation(ref currentRotation, deltaTime, _brain);
            }
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_brain.CurrentState is IMovementState movementState)
            {
                movementState.UpdateVelocity(ref currentVelocity, deltaTime, _brain);
            }
            else
            {
                DoDefaultUpdateVelocity(ref currentVelocity, deltaTime);
            }
            //
            // // 지면에 없을 때만 중력을 적용하거나, 
            // // 지면일 때는 아주 최소한의 힘만 주어 바닥에 붙어있게 합니다.
            if (!Motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity += _brain.MovementData.Gravity * deltaTime;
            }
            else
            {
                // 지면에서는 중력이 속도 벡터에 누적되지 않도록 제어
                // 대신 Motor의 GroundSnapping이 캐릭터를 바닥에 고정시킵니다.
            }
            //
            // // 마찰력(Drag) 적용
            // float drag = Motor.GroundingStatus.IsStableOnGround ? _brain.Drag : _brain.AirDrag; 
            // currentVelocity *= (1f / (1f + (drag * deltaTime)));
            //
            if (_internalVelocityAdd.sqrMagnitude > 0f)
            {
                currentVelocity += _internalVelocityAdd;
                _internalVelocityAdd = Vector3.zero;
            }
        }
        
        public void DoDefaultUpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 1. 현재 속도를 수평(Horizontal)과 수직(Vertical) 성분으로 분리
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);
            Vector3 verticalVelocity = Vector3.Project(currentVelocity, Motor.CharacterUp);
            
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                // 2. 입력 확인 및 목표 속도 결정
                float targetSpeed = 0f;
                // 아주 작은 입력은 무시하도록 데드존 설정 (0.01f)
                if (_brain.InputDirection.sqrMagnitude > 0.01f)
                {
                    if (_brain.IsSprintPressed) targetSpeed = _brain.MovementData.SprintSpeed;
                    else if (_brain.InputDirection.sqrMagnitude > 0.8f) targetSpeed = _brain.MovementData.RunSpeed;
                    else targetSpeed = _brain.MovementData.WalkSpeed;
                }
            
                // 3. 지면의 경사(Normal)를 고려한 이동 방향 계산
                // 단순히 InputDirection을 쓰는게 아니라 지면을 타고 흐르도록 합니다.
                Vector3 targetMovementDirection = Motor.GetDirectionTangentToSurface(_brain.InputDirection, Motor.GroundingStatus.GroundNormal);
                Vector3 targetMovementVelocity = targetMovementDirection * targetSpeed;
                
                float currentSharpness = (targetSpeed > 0.01f) ? _brain.MovementData.AccelerationSharpness : _brain.MovementData.DecelerationSharpness;
            
                horizontalVelocity = Vector3.Lerp(
                    horizontalVelocity, 
                    targetMovementVelocity, 
                    1f - Mathf.Exp(-currentSharpness * deltaTime)
                );
            
                verticalVelocity = Vector3.zero; 
            }
            
            //최종 속도 재조합
            currentVelocity = horizontalVelocity + verticalVelocity;
        }
        
        private void OnAnimatorMove()
        {
            // 현재 상태가 TurnInPlaceStateSO인지 확인
            if (_brain.CurrentState is TurnInPlaceStateSO turnState)
            {
                // TurnInPlaceStateSO에 Root Motion 데이터 전달
                turnState.OnAnimatorMoveCallback(_brain);
            }
            // 다른 상태들도 필요하면 추가 가능
        }

        public void AfterCharacterUpdate(float deltaTime) { }
        public void PostGroundingUpdate(float deltaTime) { }
        public bool IsColliderValidForCollisions(Collider coll) => true;
        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
        public void OnDiscreteCollisionDetected(Collider hitCollider) { }

    }
}
