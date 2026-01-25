using KinematicCharacterController;
using UnityEngine;

namespace UPlayGround.GameActor.MovementController
{
    public class ActorMovementController : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor;

        public Base.GameActor<ActorMovementController> Actor;
        
        [Header("Stable Movement")]
        public float MaxStableMoveSpeed = 10f;
        public float StableMovementSharpness = 15;
        public float OrientationSharpness = 10;

        [Header("Air Movement")]
        public float MaxAirMoveSpeed = 3f;
        public float AirAccelerationSpeed = 5f;
        public float Drag = 0.1f;

        [Header("Jumping")]
        public float JumpSpeed = 10f;
        public float JumpPreGroundingGraceTime = 0.1f; // 땅에 닿기 직전 점프 입력 허용 시간
        public float JumpPostGroundingGraceTime = 0.15f; // 낭떠러지에서 떨어진 후 점프 허용 시간 (코요테 타임)
        
        [Header("Misc")]
        public bool RotationObstruction;
        public Vector3 Gravity = new Vector3(0, -30f, 0);
        public Transform MeshRoot;

        private void Start()
        {
            // Assign to motor
            Motor.CharacterController = this;
        }
        
        public virtual void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
        }

        public virtual void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
        }

        public virtual void BeforeCharacterUpdate(float deltaTime)
        {
        }

        public virtual void PostGroundingUpdate(float deltaTime)
        {
        }

        public virtual void AfterCharacterUpdate(float deltaTime)
        {
        }

        public virtual bool IsColliderValidForCollisions(Collider coll)
        {
            return false;
        }

        public virtual void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public virtual void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
        }

        public virtual void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition,
            Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        public virtual void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
    }
}