using UPlayGround.Data.Actor.Animation;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Animation
{
    /// <summary>
    /// 대화 제스처 슬롯을 액터가 실제로 재생할 수 있는 슬롯으로 낮춰준다.
    /// 카탈로그는 휴머노이드 공용 제스처를 가리키지만 모든 액터가 그 모션을 갖는다는 보장은 없으므로,
    /// 폴백 사다리(요청 → 기본 대화 → Idle)를 상태마다 다시 쓰지 않도록 한곳에 모았다.
    /// </summary>
    public static class DialogueMotionPlayback
    {
        /// <summary>재생 가능한 대화 모션 슬롯을 고른다. 해석 실패 시 무효 태그를 반환한다.</summary>
        public static GameplayTag Resolve(ActorAnimator animator, GameplayTag requested)
        {
            if (animator == null)
                return default;

            if (requested.IsValid() && animator.HasMotion(requested))
                return requested;

            if (animator.HasMotion(MotionTags.Talk_1))
                return MotionTags.Talk_1;

            return MotionTags.Idle;
        }
    }
}
