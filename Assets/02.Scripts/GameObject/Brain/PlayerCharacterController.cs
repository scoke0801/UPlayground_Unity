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
    
            // 지면에 없을 때만 중력을 적용하거나, 
            // 지면일 때는 아주 최소한의 힘만 주어 바닥에 붙어있게 합니다.
            if (!Motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity += _brain.Gravity * deltaTime;
            }
            else
            {
                // 지면에서는 중력이 속도 벡터에 누적되지 않도록 제어
                // 대신 Motor의 GroundSnapping이 캐릭터를 바닥에 고정시킵니다.
            }
    
            // 마찰력(Drag) 적용
            float drag = Motor.GroundingStatus.IsStableOnGround ? _brain.Drag : _brain.AirDrag; 
            currentVelocity *= (1f / (1f + (drag * deltaTime)));
    
            if (_internalVelocityAdd.sqrMagnitude > 0f)
            {
                currentVelocity += _internalVelocityAdd;
                _internalVelocityAdd = Vector3.zero;
            }
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
