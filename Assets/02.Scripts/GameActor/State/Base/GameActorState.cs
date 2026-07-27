using KinematicCharacterController;
using System;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    [Flags]
    public enum ActorStateTag
    {
        None = 0,
        Locomotion = 1 << 0,
        Combat = 1 << 1,
        Defensive = 1 << 2,
        Airborne = 1 << 3,
        InterruptLocked = 1 << 4,
        Recovery = 1 << 5
    }

    public enum GravityOwnership
    {
        Controller,
        State,
        None
    }

    /// <summary>
    /// 모든 Actor 이동 상태의 베이스 클래스
    /// </summary>
    public abstract class GameActorState
    {
        protected GameActor gameActor;
        protected ActorMovementController controller;
        protected KinematicCharacterMotor motor;

        public virtual GravityOwnership GravityOwner => GravityOwnership.Controller;

        public virtual float GetGravityMultiplier(float verticalSpeed)
            => verticalSpeed < 0f
                ? controller.FallGravityMultiplier
                : controller.RiseGravityMultiplier;

        /// <summary>
        /// true이면 이 상태 동안 피격 무적. PlayerActor.CanTakeDamage()에서 참조.
        /// </summary>
        public virtual bool GrantsInvincibility => false;

        /// <summary>
        /// true이면 데미지는 받지만 피격 리액션/경직 전환은 무시한다.
        /// </summary>
        public virtual bool SuppressesHitReaction => false;

        /// <summary>
        /// 이 상태에서 일반 피격 리액션 애니메이션을 재생할 수 있는지 여부.
        /// Poise Break 같은 강제 무력화 판정은 이 값과 별도로 처리한다.
        /// </summary>
        public virtual bool CanPlayHitReaction(in HitContext hit) => !SuppressesHitReaction;

        /// <summary>
        /// true이면 BT가 이 상태를 중간에 다른 판단으로 덮지 않고 상태 자체의 종료 로직을 기다린다.
        /// </summary>
        public virtual bool BlocksBehaviorTree => false;

        /// <summary>
        /// 같은 구체 상태 타입의 새 인스턴스로 재진입할 수 있는지 여부.
        /// 기본 상태는 중복 전환을 막고, 입력에 따라 실행 컨텍스트를 새로 만들어야 하는 상태만 허용한다.
        /// </summary>
        public virtual bool AllowsSameTypeReentry => false;

        /// <summary>
        /// 동일 타입 재진입이 허용된 상태에서 현재 실행 컨텍스트를 새 인스턴스로 교체할 수 있는지 판정한다.
        /// 공격 타입처럼 인스턴스별 실행 종류가 다른 상태가 교차 전환을 제한할 때 재정의한다.
        /// </summary>
        public virtual bool CanReenterFrom(GameActorState currentState) => AllowsSameTypeReentry;

        /// <summary>
        /// 서브클래스가 켤 추가 상태 태그. InterruptLocked는 BlocksBehaviorTree로부터 자동 합성되므로
        /// 여기서 다시 켤 필요 없음.
        /// </summary>
        protected virtual ActorStateTag StateTagsCore => ActorStateTag.None;

        /// <summary>
        /// BT 조건/동기화에서 구체 상태명 대신 사용할 상태 태그.
        /// BlocksBehaviorTree가 true면 InterruptLocked가 항상 포함된다.
        /// </summary>
        public ActorStateTag StateTags =>
            StateTagsCore | (BlocksBehaviorTree ? ActorStateTag.InterruptLocked : ActorStateTag.None);
        
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
        
        public abstract bool CanTransitionState(string stateName);
        
        /// <summary>
        /// 상태 진입 시 호출
        /// </summary>
        public virtual void OnEnter(GameActorState fromState)
        {
        }
        
        /// <summary>
        /// 상태 퇴장 시 호출
        /// </summary>
        public virtual void OnExit(GameActorState toState)
        {
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

        /// <summary>컨트롤러 중력 적분 뒤 상태별 종단 속도 같은 최종 제약을 적용한다.</summary>
        public virtual void ConstrainVelocityAfterGravity(ref Vector3 currentVelocity, float deltaTime)
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

        /// <summary>
        /// 들어온 공격 방향으로 즉시 1회 스냅 회전한다(가드 블록 시점 등).
        /// 공격자 위치를 우선 사용하고, 없으면 -attackDirection을 사용한다.
        /// </summary>
        protected void FaceIncomingAttack(AttackData incomingAttack)
        {
            Vector3 direction = Vector3.zero;

            if (incomingAttack?.attacker != null)
                direction = incomingAttack.attacker.transform.position - motor.TransientPosition;
            else if (incomingAttack != null && incomingAttack.attackDirection != Vector3.zero)
                direction = -incomingAttack.attackDirection;

            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f) return;

            motor.SetRotation(Quaternion.LookRotation(direction.normalized, motor.CharacterUp));
        }
    }
}
