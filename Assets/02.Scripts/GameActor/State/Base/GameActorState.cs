using KinematicCharacterController;
using UnityEngine;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 모든 Actor 이동 상태의 베이스 클래스
    /// </summary>
    public abstract class GameActorState
    {
        protected GameActor gameActor;
        protected ActorMovementController controller;
        protected KinematicCharacterMotor motor;
        
        public GameActorState(ActorMovementController controller)
        {
            this.gameActor = controller.Actor;
            this.controller = controller;
            this.motor = controller.Motor;
        }
        
        /// <summary>
        /// 상태 이름 (디버깅용)
        /// </summary>
        public abstract string StateName { get; }
        
        /// <summary>
        /// 상태 진입 시 호출
        /// </summary>
        public virtual void OnEnter(GameActorState fromState)
        {
            Debug.Log($"[State] Enter: {StateName} (from {fromState?.StateName ?? "None"})");
        }
        
        /// <summary>
        /// 상태 퇴장 시 호출
        /// </summary>
        public virtual void OnExit(GameActorState toState)
        {
            Debug.Log($"[State] Exit: {StateName} (to {toState?.StateName ?? "None"})");
        }
        
        /// <summary>
        /// 매 프레임 상태 업데이트 - 상태 전환 로직 포함
        /// </summary>
        public virtual void UpdateState(float deltaTime)
        {
        }
        
        /// <summary>
        /// 캐릭터 회전 업데이트
        /// </summary>
        public virtual void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
        }
        
        /// <summary>
        /// 캐릭터 속도 업데이트
        /// </summary>
        public virtual void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
        }
        
        /// <summary>
        /// 모터 업데이트 전 호출
        /// </summary>
        public virtual void BeforeCharacterUpdate(float deltaTime)
        {
        }
        
        /// <summary>
        /// 모터 업데이트 후 호출
        /// </summary>
        public virtual void AfterCharacterUpdate(float deltaTime)
        {
        }
        
        /// <summary>
        /// 접지 상태 업데이트 후 호출
        /// </summary>
        public virtual void PostGroundingUpdate(float deltaTime)
        {
        }
        
        /// <summary>
        /// 지면 충돌 시 호출
        /// </summary>
        public virtual void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, 
            ref HitStabilityReport hitStabilityReport)
        {
        }
        
        /// <summary>
        /// 이동 중 충돌 시 호출
        /// </summary>
        public virtual void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, 
            ref HitStabilityReport hitStabilityReport)
        {
        }
    }
}