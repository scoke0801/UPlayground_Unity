using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Components;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Animation;

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

        private const float FALLBACK_DEATH_TIMEOUT = 4f;
        private const float MINIMUM_PLAY_RATE = 0.5f;
        private const float MOTION_COMPLETION_GRACE = 0.25f;

        private bool _isDestoryCalled = false;
        private bool _completionSubscribed = false;
        private float _deathTimer;
        private float _deathTimeout;
        private MotionSet _deathMotionSet;
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

            _isDestoryCalled = false;
            _completionSubscribed = false;
            _deathTimer = 0f;
            _deathTimeout = FALLBACK_DEATH_TIMEOUT;
            _deathMotionSet = null;
            _owner = gameActor as MonsterActor;

            if (_owner == null)
            {
                return;
            }
            var state = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Die, 0.25f);
            if (state != null)
            {
                _deathMotionSet = gameActor.Animator.CurrentMotionSet;
                float motionDuration = _deathMotionSet?.TotalDuration ?? 0f;
                if (motionDuration > 0f)
                {
                    _deathTimeout = Mathf.Max(
                        FALLBACK_DEATH_TIMEOUT,
                        motionDuration / MINIMUM_PLAY_RATE + MOTION_COMPLETION_GRACE);
                }

                gameActor.Animator.OnMotionSetEndedWithReason += OnDeathMotionEnded;
                _completionSubscribed = true;
            }
            else
            {
                // Die 모션 미등록 → 시체가 영구히 남지 않도록 곧바로 잔존 처리로 넘긴다.
                OnDeathMotionEnd();
            }
        }

        private void OnDeathMotionEnded(MotionSet motionSet, MotionSetEndReason _)
        {
            if (_deathMotionSet != null && ReferenceEquals(motionSet, _deathMotionSet))
                OnDeathMotionEnd();
        }

        private void OnDeathMotionEnd()
        {
            if (_completionSubscribed)
            {
                _completionSubscribed = false;
                if (gameActor != null && gameActor.Animator != null)
                    gameActor.Animator.OnMotionSetEndedWithReason -= OnDeathMotionEnded;
            }

            _deathMotionSet = null;

            if (_isDestoryCalled || _owner == null) return;
            _isDestoryCalled = true;
            _owner.BeginDeathRemains();
        }

        public override void OnExit(GameActorState toState)
        {
            if (_completionSubscribed)
            {
                _completionSubscribed = false;
                if (gameActor != null && gameActor.Animator != null)
                    gameActor.Animator.OnMotionSetEndedWithReason -= OnDeathMotionEnded;
            }

            _deathMotionSet = null;

            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            if (_isDestoryCalled || !_completionSubscribed)
                return;

            _deathTimer += deltaTime;
            if (_deathTimer < _deathTimeout)
                return;

            Debug.LogWarning(
                $"[EnemyDeathState] 사망 Motion 종료 신호가 없어 디졸브를 강제 시작합니다. " +
                $"actor={gameActor.name}, timeout={_deathTimeout:0.00}s",
                gameActor);
            OnDeathMotionEnd();
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
