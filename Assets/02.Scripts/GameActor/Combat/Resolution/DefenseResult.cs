namespace UPlayGround.Combat
{
    public enum DefenseOutcome
    {
        None,
        Guarded,
        GuardBreak,
        Parried,
        PerfectDodged,
        Invincible,
        UnblockableHit,
    }

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
