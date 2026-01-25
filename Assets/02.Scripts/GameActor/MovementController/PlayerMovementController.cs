using KinematicCharacterController;
using UnityEngine;

namespace UPlayGround.GameActor.MovementController
{
    public class PlayerMovementController : ActorMovementController
    {
        private Vector3 _moveInputVector; // 입력값 캐싱
        
        // PlayerActor에서 호출하여 입력 전달
        public void SetMoveInput(Vector2 input)
        {
            _moveInputVector = new Vector3(input.x, 0f, input.y);
        }
        
        #region ICharacterController
        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            Vector3 targetMovementVelocity = Vector3.zero;

            // 지면에 붙어있을 때 (Stable)
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                Vector3 effectiveMoveDir = Vector3.ProjectOnPlane(_moveInputVector, Motor.GroundingStatus.GroundNormal).normalized;
                
                // 입력이 있을 때만 속도 계산 (입력 크기 유지)
                targetMovementVelocity = effectiveMoveDir * (_moveInputVector.magnitude * MaxStableMoveSpeed);

                // 부드러운 가감속
                currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-StableMovementSharpness * deltaTime));
            }
            // 공중에 떠있을 때 (Airborne)
            else
            {
                if (_moveInputVector.sqrMagnitude > 0f)
                {
                    targetMovementVelocity = _moveInputVector * MaxAirMoveSpeed;

                    // 공중 가속도 적용
                    currentVelocity = Vector3.MoveTowards(currentVelocity, targetMovementVelocity, AirAccelerationSpeed * deltaTime);
                }

                // 중력 적용
                currentVelocity += Gravity * deltaTime;

                // 공기 저항 (Drag) 적용
                currentVelocity *= (1f - Drag * deltaTime);
            }
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
}