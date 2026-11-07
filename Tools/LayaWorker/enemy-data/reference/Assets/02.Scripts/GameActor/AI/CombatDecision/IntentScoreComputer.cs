namespace UPlayGround.AI.CombatDecision
{
    /// <summary>
    /// IntentWeightEntry + IntentEvaluationContext 조합으로 단일 Intent 점수를 계산한다.
    /// 계산 순서: base + Σ(baseContinuous) → +Σ(만족된 bonuses) → ×Π(만족된 multipliers).
    /// Phase/Role 가중치와 Last Intent 페널티는 호출자가 별도로 적용한다.
    /// </summary>
    public static class IntentScoreComputer
    {
        public static float Compute(IntentWeightEntry entry, in IntentEvaluationContext ctx)
        {
            if (entry == null) return 0f;

            var score = entry.baseScore;

            if (entry.baseContinuous != null)
            {
                for (var i = 0; i < entry.baseContinuous.Count; i++)
                {
                    var c = entry.baseContinuous[i];
                    score += c.coefficient * IntentConditionEvaluator.GetContinuous(c.valueId, in ctx);
                }
            }

            if (entry.bonuses != null)
            {
                for (var i = 0; i < entry.bonuses.Count; i++)
                {
                    var bonus = entry.bonuses[i];
                    if (bonus == null) continue;
                    if (!IntentConditionEvaluator.EvaluateTerms(bonus.mode, bonus.terms, in ctx))
                        continue;

                    var amount = bonus.amount;
                    if (bonus.continuous != null)
                    {
                        for (var k = 0; k < bonus.continuous.Count; k++)
                        {
                            var c = bonus.continuous[k];
                            amount += c.coefficient * IntentConditionEvaluator.GetContinuous(c.valueId, in ctx);
                        }
                    }
                    score += amount;
                }
            }

            if (entry.multipliers != null)
            {
                for (var i = 0; i < entry.multipliers.Count; i++)
                {
                    var mul = entry.multipliers[i];
                    if (mul == null) continue;
                    if (!IntentConditionEvaluator.EvaluateTerms(mul.mode, mul.terms, in ctx))
                        continue;
                    score *= mul.factor;
                }
            }

            return score;
        }
    }
}
