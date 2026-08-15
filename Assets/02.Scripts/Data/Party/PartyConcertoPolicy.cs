namespace UPlayGround.Data.Party
{
    /// <summary>
    /// 교체 특수 공격의 준비·우선순위 규칙. UI와 런타임이 같은 판정을 사용하도록
    /// Unity 오브젝트와 분리한 순수 정책으로 유지한다.
    /// </summary>
    public static class PartyConcertoPolicy
    {
        public static bool IsReady(float current, float maximum)
            => maximum > 0f && current >= maximum;

        public static bool CanTriggerSwapSpecial(
            float current,
            float maximum,
            bool hasAuthoredAbility,
            bool hasHigherPrioritySwapReaction)
        {
            return hasAuthoredAbility
                   && !hasHigherPrioritySwapReaction
                   && IsReady(current, maximum);
        }
    }
}
