using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.MovementController;

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

        private bool  _animFinished;

        public PlayerGuardBreakState(ActorMovementController controller) : base(controller) { }

        // 가드 브레이크 중에는 어떤 State로도 전환 불가
        public override bool CanTransitionState(ActorStateId fromState) => false;

        public override void OnEnter(GameActorState fromState)
        {
            base.OnEnter(fromState);

            _animFinished = false;

            // GuardBreak 전용 모션이 있으면 재생, 없으면 Knockback으로 폴백
            UPlayGround.Gameplay.Tag.GameplayTag animKey = playerActor.Animator.HasMotion(UPlayGround.Data.Actor.Animation.MotionTags.GuardBreak, true)
                ? UPlayGround.Data.Actor.Animation.MotionTags.GuardBreak
                : UPlayGround.Data.Actor.Animation.MotionTags.Knockback;

            var animState = playerActor.Animator.PlayMotion(animKey, 0.1f, 0);
            if (animState != null)
            {
                animState.OwnedEvents.OnEnd = () => _animFinished = true;
            }
            else
            {
                // 애니메이션 자체가 없으면 폴백 타이머로 처리
                controller.StartCoroutine(ReturnToIdleAfterDelay(FALLBACK_STUN_DURATION));
            }
        }

        public override void UpdateState(float deltaTime)
        {
            if (controller.CurrentState != this)
                return;

            if (_animFinished)
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
