namespace UPlayGround.AI.CombatDecision
{
    /// <summary>
    /// Intent 점수에 가산되는 0~1 범위 연속값 식별자.
    /// base 또는 bonus.amount에 계수와 함께 곱해진다.
    /// </summary>
    public enum ContinuousValueId
    {
        None = 0,
        Aggression,
        ReactionChance,
        PunishChance,
        CounterChance,
        RetreatChance,
        GuardChance,
        CircleWeight,
        PredictionConfidence
    }
}
