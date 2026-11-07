namespace UPlayGround.Combat
{
    /// <summary>단일 피격에서 확정되는 상호 배타적인 최종 방어 결과.</summary>
    public enum DefenseOutcome
    {
        None = 0,
        Block = 1,
        PerfectGuard = 2,
        GuardBreak = 3,
        AttackClash = 4,
        PerfectDodge = 5,
        Invincible = 6,
        UnblockableHit = 7,
    }

    /// <summary>최종 방어 결과와 피해 계산 진행 여부를 함께 전달한다.</summary>
    public readonly struct DefenseResult
    {
        public readonly DefenseOutcome Outcome;
        public readonly bool ShouldApplyDamage;

        public DefenseResult(DefenseOutcome outcome, bool shouldApplyDamage)
        {
            Outcome = outcome;
            ShouldApplyDamage = shouldApplyDamage;
        }

        public static DefenseResult None => new DefenseResult(DefenseOutcome.None, true);
    }
}
