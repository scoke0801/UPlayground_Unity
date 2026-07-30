using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UPlayGround.Animation;
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
        public override string StateName => "Death";
        public override bool BlocksBehaviorTree => true;
        public override GravityOwnership GravityOwner => GravityOwnership.State;
        
        private bool _isDestroyCalled;
        private MotionSet _playedMotionSet;
        private float _despawnTimeout;
        private float _elapsedUnscaled;
        private PlayerEquipment _equipment;
        public EnemyDeathState(ActorMovementController controller) : base(controller)
        {
        }

        public override bool CanTransitionState(string stateName)
        {
            return true;
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            // 워프 진행 중이면 즉시 clear (사망 모션이 우선).
            controller.MotionWarp?.ClearTarget();

            MonsterActor owner = gameActor as MonsterActor;

            if (owner == null)
            {
                return;
            }
            var state = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Die, 0.25f);
            if (state != null)
            {
                _playedMotionSet = gameActor.Animator.CurrentMotionSet;
                _despawnTimeout = Mathf.Max(
                    0.25f,
                    (_playedMotionSet?.TotalDuration ?? 0f) + 0.25f);
                gameActor.Animator.OnMotionSetEndedWithReason += OnMotionSetEnded;
            }
            else
            {
                BeginDespawn();
            }
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetEndedWithReason -= OnMotionSetEnded;
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            if (_isDestroyCalled || _playedMotionSet == null)
                return;

            // 사망 모션 종료 알림은 MotionSet 교체/종료 순서에 따라 유실될 수 있다.
            // 월드 액터가 시체 상태로 영구 잔류하지 않도록 비스케일 시간으로 보장한다.
            _elapsedUnscaled += Time.unscaledDeltaTime;
            if (_elapsedUnscaled >= _despawnTimeout)
                BeginDespawn();
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

        private void OnMotionSetEnded(MotionSet motionSet, MotionSetEndReason reason)
        {
            if (!ReferenceEquals(motionSet, _playedMotionSet))
                return;

            BeginDespawn();
        }

        private void BeginDespawn()
        {
            if (_isDestroyCalled)
                return;

            _isDestroyCalled = true;
            gameActor.Animator.OnMotionSetEndedWithReason -= OnMotionSetEnded;

            if (gameActor is MonsterActor owner)
                owner.PlayDissolveAndDestroy(3f);
        }
    }
}
