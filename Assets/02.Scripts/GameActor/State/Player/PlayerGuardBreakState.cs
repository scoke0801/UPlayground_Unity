using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.Animation;

namespace UPlayGround.State
{
    /// <summary>
    /// 가드 브레이크 상태.
    /// 애니메이션 재생 → 경직 대기 → Idle 복귀까지 전담한다.
    /// 이 State에 머무는 동안은 가드 입력을 포함한 모든 행동이 불가하다.
    /// </summary>
    public class PlayerGuardBreakState : PlayerActorState
    {
        public override ActorStateId StateId => ActorStateId.GuardBreak;

        // 애니메이션이 없는 경우의 강제 경직 시간 (폴백용)
        private const float FALLBACK_STUN_DURATION = 1.2f;

        // 모션 완료 이벤트가 끝내 오지 않을 때의 상한. 모션 총 길이에 얹는 여유분.
        private const float MOTION_TIMEOUT_MARGIN = 0.5f;

        private bool  _animFinished;
        private bool  _motionStarted;
        private float _motionTimeoutTime;
        private MotionSet _breakMotionSet;

        public PlayerGuardBreakState(ActorMovementController controller) : base(controller) { }

        // 가드 브레이크 중에는 어떤 State로도 전환 불가
        public override bool CanTransitionState(ActorStateId fromState) => false;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _animFinished       = false;
            _motionStarted      = false;
            _motionTimeoutTime  = 0f;
            _breakMotionSet     = null;

            // GuardBreak 전용 모션이 있으면 재생, 없으면 Knockback으로 폴백
            UPlayGround.Gameplay.Tag.GameplayTag animKey = playerActor.Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.GuardBreak, true)
                ? UPlayGround.Data.Actor.Animation.MotionTags.GuardBreak
                : UPlayGround.Data.Actor.Animation.MotionTags.Knockback;

            var animState = playerActor.Animator.PlayMotion(animKey, 0.1f, 0);
            if (animState != null)
            {
                // 모션 완료 판정은 MotionSet 디렉터 이벤트로 한다.
                // AnimancerState.OwnedEvents.OnEnd는 완료 시점에 재생이 마지막 포즈에서
                // 정지(Speed = 0)하면 발화하지 않아, 이 상태가 영구 잠길 수 있다.
                _breakMotionSet = gameActor.Animator.CurrentMotionSet;
                gameActor.Animator.OnMotionSetEndedWithReason += OnMotionSetEnded;
                _motionStarted = true;

                // 재생 중인 MotionSet이 InfiniteLoop/Hold Section을 포함하면 완료 이벤트가 발화하지 않고
                // IsPlayingMotionSet도 계속 true다. 이 State는 스스로 나가는 것 외에 복구 수단이 없으므로
                // 모션 총 길이 기준 상한을 함께 건다.
                float motionDuration = gameActor.Animator.CurrentMotionSet?.TotalDuration ?? 0f;
                _motionTimeoutTime = Time.time
                    + Mathf.Max(FALLBACK_STUN_DURATION, motionDuration + MOTION_TIMEOUT_MARGIN);
            }
            else
            {
                // 애니메이션 자체가 없으면 폴백 타이머로 처리
                controller.StartCoroutine(ReturnToIdleAfterDelay(FALLBACK_STUN_DURATION));
            }
        }

        public override void OnExit(GameActorState toState)
        {
            gameActor.Animator.OnMotionSetEndedWithReason -= OnMotionSetEnded;
            base.OnExit(toState);
        }

        private void OnMotionSetEnded(MotionSet motionSet, MotionSetEndReason _)
        {
            if (ReferenceEquals(motionSet, _breakMotionSet))
                _animFinished = true;
        }

        public override void UpdateState(float deltaTime)
        {
            if (controller.CurrentState != this)
                return;

            // 모션이 중간에 교체·중단되어 완료 이벤트가 오지 않는 경우까지 복구한다.
            // (모션 없이 폴백 타이머로 진입한 경우는 코루틴이 복귀를 담당하므로 제외)
            bool motionStalled = _motionStarted
                && (!gameActor.Animator.IsPlayingMotionSet || Time.time >= _motionTimeoutTime);
            if (_animFinished || motionStalled)
            {
                controller.TransitionToState(ActorStateId.Idle);
                return;
            }
        }

        public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // 경직 중 회전 고정
            currentRotation = currentRotation.normalized;
        }

        public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!motor.GroundingStatus.IsStableOnGround) return;

            // 경직 중 빠르게 감속
            currentVelocity = Vector3.Lerp(
                currentVelocity,
                Vector3.zero,
                1 - Mathf.Exp(-controller.StableMovementSharpness * deltaTime));
        }

        private System.Collections.IEnumerator ReturnToIdleAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (controller.CurrentState != this)
                yield break;

            controller.TransitionToState(ActorStateId.Idle);
        }
    }
}
