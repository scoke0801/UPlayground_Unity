using KinematicCharacterController;
using UnityEngine;

namespace Game.FSM
{
    public class PlayerCharacterController : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor;
        
        [Header("Stable Movement")]
        public float walkSpeed = 5f;
        public float runSpeed = 9f;
        public float sprintSpeed = 12f;
        public float StableMovementSharpness = 15f;
        public float OrientationSharpness = 10f;

        [Header("Air Movement")]
        public float MaxAirMoveSpeed = 15f;
        public float AirAccelerationSpeed = 15f;
        public float Drag = 0.1f;

        [Header("Jumping")]
        public float JumpUpSpeed = 10f;
        
        [Header("Misc")]
        public Vector3 Gravity = new Vector3(0, -30f, 0);

        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;
        private bool _jumpRequested = false;
        private bool _isSprinting = false;
        private Vector3 _internalVelocityAdd = Vector3.zero;

        private void Awake()
        {
            Motor.CharacterController = this;
        }

        public void AddVelocity(Vector3 velocity)
        {
            _internalVelocityAdd += velocity;
        }

        public void SetInputs(Vector3 moveInput, Vector3 lookInput, bool jumpRequested, bool isSprinting)
        {
            _moveInputVector = moveInput;
            _lookInputVector = lookInput;
            _jumpRequested = jumpRequested;
            _isSprinting = isSprinting;
        }

        public void BeforeCharacterUpdate(float deltaTime) { }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_lookInputVector.sqrMagnitude > 0f && OrientationSharpness > 0f)
            {
                Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
            }
            
            Vector3 currentUp = (currentRotation * Vector3.up);
            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-OrientationSharpness * deltaTime));
            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // Ground movement
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                float targetSpeed = walkSpeed;
                if (_isSprinting)
                {
                    targetSpeed = sprintSpeed;
                }
                else if (_moveInputVector.sqrMagnitude > 0.8f)
                {
                    targetSpeed = runSpeed;
                }

                Vector3 targetMovementVelocity = _moveInputVector * targetSpeed;

                currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-StableMovementSharpness * deltaTime));
            }
            // Air movement
            else
            {
                if (_moveInputVector.sqrMagnitude > 0f)
                {
                    Vector3 addedVelocity = _moveInputVector * AirAccelerationSpeed * deltaTime;
                    Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                    if (currentVelocityOnInputsPlane.magnitude < MaxAirMoveSpeed)
                    {
                        Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, MaxAirMoveSpeed);
                        addedVelocity = newTotal - currentVelocityOnInputsPlane;
                    }

                    currentVelocity += addedVelocity;
                }

                currentVelocity += Gravity * deltaTime;
                currentVelocity *= (1f / (1f + (Drag * deltaTime)));
            }

            if (_jumpRequested)
            {
                if (Motor.GroundingStatus.IsStableOnGround)
                {
                    Motor.ForceUnground();
                    currentVelocity += Motor.CharacterUp * JumpUpSpeed - Vector3.Project(currentVelocity, Motor.CharacterUp);
                }
                _jumpRequested = false;
            }

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
