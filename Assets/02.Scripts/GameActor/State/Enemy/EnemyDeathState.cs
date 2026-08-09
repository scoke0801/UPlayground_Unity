using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Components;
using UPlayGround.Manager;
using UPlayGround.MovementController;

namespace UPlayGround.State
{
    /// <summary>
    /// 몬스터 사망 상태
    /// </summary>
    public class EnemyDeathState : EnemyActorState
    {
        public override ActorStateId StateId => ActorStateId.Death;
        public override bool BlocksBehaviorTree => true;
        public override GravityOwnership GravityOwner => GravityOwnership.State;
        
        private bool _isDestoryCalled = false;
        private bool _completionSubscribed = false;
        private MonsterActor _owner;
        private PlayerEquipment _equipment;
        public EnemyDeathState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(ActorStateId fromState)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            // 워프 진행 중이면 즉시 clear (사망 모션이 우선).
            controller.MotionWarp?.ClearTarget();

            _owner = gameActor as MonsterActor;

            if (_owner == null)
            {
                return;
            }
            var state = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Die, 0.25f);
            if (state != null)
            {
                // MotionSet 타임라인은 Animancer OnEnd를 쓰지 않는다(ActorAnimator가 매 클립 전환마다 null로 지운다).
                // 종료 신호는 다른 상태와 동일하게 OnMotionSetCompleted로 받는다.
                gameActor.Animator.OnMotionSetCompleted += OnDeathMotionEnd;
                _completionSubscribed = true;
            }
            else
            {
                // Die 모션 미등록 → 시체가 영구히 남지 않도록 즉시 디졸브한다.
                OnDeathMotionEnd();
            }
        }

        private void OnDeathMotionEnd()
        {
            if (_completionSubscribed)
            {
                _completionSubscribed = false;
                if (gameActor != null && gameActor.Animator != null)
                    gameActor.Animator.OnMotionSetCompleted -= OnDeathMotionEnd;
            }

            if (_isDestoryCalled || _owner == null) return;
            _isDestoryCalled = true;
            _owner.PlayDissolveAndDestroy(3f);
        }

        public override void OnExit(GameActorState toState)
        {
            if (_completionSubscribed)
            {
                _completionSubscribed = false;
                if (gameActor != null && gameActor.Animator != null)
                    gameActor.Animator.OnMotionSetCompleted -= OnDeathMotionEnd;
            }

            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // Idle 상태에서는 회전 유지 (또는 부드럽게 정면으로)
            currentRotation = currentRotation.normalized;
        }
        
        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            currentVelocity.x = 0;
            currentVelocity.z = 0;
            if (motor.GroundingStatus.IsStableOnGround == false)
            {
                currentVelocity += controller.Gravity * deltaTime;
            }
            
        }
    }
}
