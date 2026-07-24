using System;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Animation;
using UPlayGround.Data.Actor.Animation;

namespace UPlayGround.Gameplay.Ability
{
    [CreateAssetMenu(
        fileName = "AbilityTask_WaitMotionSetEnd",
        menuName = "UPlayGround/Ability/Task/Wait MotionSet End")]
    public sealed class WaitMotionSetEndAbilityTask : AbilityTaskDefinitionSO
    {
        public override AbilityTaskInstance CreateRuntime(AbilityTaskContext context) =>
            new WaitMotionSetEndAbilityTaskInstance(context);
    }

    /// <summary>
    /// 액터 상태가 시작한 MotionSet 종료를 Ability Task 수명주기에 연결한다.
    /// 부모 Ability 취소 시 Motion 이벤트 구독을 즉시 정리한다.
    /// </summary>
    public sealed class WaitMotionSetEndAbilityTaskInstance : AbilityTaskInstance
    {
        private ActorAnimator _animator;

        public WaitMotionSetEndAbilityTaskInstance(AbilityTaskContext context) : base(context) { }

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
