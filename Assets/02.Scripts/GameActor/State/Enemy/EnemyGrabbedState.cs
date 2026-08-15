using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.Animation;

namespace UPlayGround.State
{
    /// <summary>
    /// 적 잡힘 상태.
    /// Grab 공격에 피격 시 진입. 일정 시간 행동 불능.
    /// 공격자가 FireForcedMotionReleased()를 호출하면 즉시 해제되며,
    /// 호출 없이 grabDuration이 만료되면 자동 탈출한다.
    /// </summary>
    public class EnemyGrabbedState : EnemyActorState
    {
        public override ActorStateId StateId => ActorStateId.Grabbed;
        public override bool BlocksBehaviorTree => true;

        private readonly HitContext _hit;

        private const float FALLBACK_RELEASE_TIMEOUT = 2f;
        private const float MINIMUM_PLAY_RATE = 0.5f;
        private const float MOTION_COMPLETION_GRACE = 0.25f;

        private bool _isActive;
        private bool _releaseStarted;
        private float _remainingDuration;
        private float _releaseTimer;
        private float _releaseTimeout;
        private MotionSet _releaseMotionSet;

        public EnemyGrabbedState(ActorMovementController controller, in HitContext hit)
            : base(controller)
        {
            _hit = hit;
        }

        public override bool CanTransitionState(ActorStateId fromState)
        {
            return fromState is not (ActorStateId.Death
                or ActorStateId.Grabbed
                or ActorStateId.SpecialBreakVictim);
        }

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _isActive = true;
            _releaseStarted = false;
            _remainingDuration = _hit.GrabDuration;
            _releaseTimer = 0f;
            _releaseTimeout = FALLBACK_RELEASE_TIMEOUT;
            _releaseMotionSet = null;

            if (_hit.Attacker != null)
                _hit.Attacker.OnForcedMotionReleased += Escape;

            UPlayGround.Gameplay.Tag.GameplayTag animKey;
            if (_hit.VictimForcedMotionSlot != default &&
                gameActor.Animator.HasMotion(_hit.VictimForcedMotionSlot))
                animKey = _hit.VictimForcedMotionSlot;
            else if (gameActor.Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.Grabbed))
                animKey = UPlayGround.Data.Actor.Animation.MotionTags.Grabbed;
            else
                animKey = UPlayGround.Data.Actor.Animation.MotionTags.Hit_F;

            gameActor.Animator.PlayMotion(animKey, 0.1f);
        }

        public override void OnExit(GameActorState toState)
        {
            if (_hit.Attacker != null)
                _hit.Attacker.OnForcedMotionReleased -= Escape;

            gameActor.Animator.OnMotionSetEndedWithReason -= OnReleaseMotionEnded;
            _isActive = false;
            _releaseMotionSet = null;
            base.OnExit(toState);
        }

        public override void UpdateState(float deltaTime)
        {
            if (!_isActive || controller.CurrentState != this)
                return;

            if (_releaseStarted)
            {
                _releaseTimer += deltaTime;
                if (_releaseTimer >= _releaseTimeout)
                {
                    Debug.LogWarning(
                        $"[EnemyGrabbedState] 잡기 해제 Motion 종료 신호가 없어 강제 복귀합니다. " +
                        $"actor={gameActor.name}, timeout={_releaseTimeout:0.00}s",
                        gameActor);
                    TransitionOut();
                }

                return;
            }

            _remainingDuration -= deltaTime;

            if (_remainingDuration <= 0f)
                Escape();
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity = Vector3.Lerp(
                    currentVelocity, Vector3.zero,
                    1 - Mathf.Exp(-controller.StableMovementSharpness * 2f * deltaTime));
            }
        }

        private void Escape()
        {
            if (!_isActive || _releaseStarted || controller.CurrentState != this)
                return;

            _releaseStarted = true;
            _releaseTimer = 0f;

            if (_hit.Attacker != null)
                _hit.Attacker.OnForcedMotionReleased -= Escape;

            if (gameActor.Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.Grabbed_End))
            {
                var state = gameActor.Animator.PlayMotion(UPlayGround.Data.Actor.Animation.MotionTags.Grabbed_End, 0.1f);
                if (state != null)
                {
                    _releaseMotionSet = gameActor.Animator.CurrentMotionSet;
                    float motionDuration = _releaseMotionSet?.TotalDuration ?? 0f;
                    if (motionDuration > 0f)
                    {
                        _releaseTimeout = Mathf.Max(
                            FALLBACK_RELEASE_TIMEOUT,
                            motionDuration / MINIMUM_PLAY_RATE + MOTION_COMPLETION_GRACE);
                    }

                    gameActor.Animator.OnMotionSetEndedWithReason += OnReleaseMotionEnded;
                }
                else
                    TransitionOut();
            }
            else
            {
                TransitionOut();
            }
        }

        private void OnReleaseMotionEnded(MotionSet motionSet, MotionSetEndReason _)
        {
            if (_releaseMotionSet != null && ReferenceEquals(motionSet, _releaseMotionSet))
                TransitionOut();
        }

        private void TransitionOut()
        {
            if (!_isActive || controller.CurrentState != this)
                return;

            _isActive = false;
            gameActor.Animator.OnMotionSetEndedWithReason -= OnReleaseMotionEnded;
            controller.TransitionToState(ActorStateId.Idle);
        }
    }
}
