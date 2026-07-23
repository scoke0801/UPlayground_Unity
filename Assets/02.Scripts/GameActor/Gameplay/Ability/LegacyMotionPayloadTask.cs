using System;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Animation;
using UPlayGround.Data.Actor.Animation;

namespace UPlayGround.Gameplay.Ability
{
    [CreateAssetMenu(
        fileName = "AbilityTask_LegacyMotionPayload",
        menuName = "UPlayGround/Ability/Task/Legacy Motion Payload")]
    public sealed class LegacyMotionPayloadTask : AbilityTaskDefinitionSO
    {
        public override AbilityTaskInstance CreateRuntime(AbilityTaskContext context) =>
            new LegacyMotionPayloadTaskInstance(context);
    }

    /// <summary>
    /// 기존 상태가 시작한 MotionSet의 종료를 Ability Task 수명주기에 연결하는 전환기 어댑터.
    /// 모든 기존 Ability가 Task 부모 취소와 Motion 구독 정리를 보장받도록 사용한다.
    /// </summary>
    public sealed class LegacyMotionPayloadTaskInstance : AbilityTaskInstance
    {
        private ActorAnimator _animator;

        public LegacyMotionPayloadTaskInstance(AbilityTaskContext context) : base(context) { }

        protected override void OnActivate()
        {
            if (!AbilitySystemComponent.TryResolve(Context.Owner.Handle, out var component))
            {
                Fail("AbilitySystemComponent를 찾을 수 없습니다.");
                return;
            }

            GameActor owner = component.GetComponent<GameActor>();
            _animator = owner != null ? owner.Animator : null;
            if (_animator == null)
            {
                Fail("ActorAnimator를 찾을 수 없습니다.");
                return;
            }
            _animator.OnMotionSetEnded += OnMotionSetEnded;
        }

        private void OnMotionSetEnded(MotionSet _, bool completed)
        {
            if (completed) Succeed("MotionCompleted");
            else Fail("MotionInterrupted");
        }

        protected override void OnEnd()
        {
            if (_animator != null)
                _animator.OnMotionSetEnded -= OnMotionSetEnded;
            _animator = null;
        }
    }
}
