namespace UPlayGround.BehaviorTree
{
    public static class BBKey
    {
        // ── Perception ──────────────────────────────
        public const string HasTarget        = "HasTarget";
        public const string DistanceToTarget = "DistanceToTarget";
        public const string CurrentStateName = "CurrentStateName";

        // ── Action Timing ───────────────────────────
        public const string LastActionTime  = "LastActionTime";
        public const string NextActionDelay = "NextActionDelay";

        // ── Phase ───────────────────────────────────
        public const string PhaseAllowCharge          = "PhaseAllowCharge";
        public const string PhaseAllowFlank           = "PhaseAllowFlank";
        public const string PhaseChargeChance         = "PhaseChargeChance";
        public const string PhaseFlankChance          = "PhaseFlankChance";
        public const string PhaseMaxConsecutiveAttacks = "PhaseMaxConsecutiveAttacks";

        // ── Combat Distance ─────────────────────────
        public const string OptimalCombatDistance = "OptimalCombatDistance";
        public const string MaxAttackRange        = "MaxAttackRange";
        public const string PersonalSpaceDistance = "PersonalSpaceDistance";
        public const string MinCombatDistance     = "MinCombatDistance";
        public const string RetreatDistance       = "RetreatDistance";

        // ── State ───────────────────────────────────
        public const string ConsecutiveDefensiveCount = "ConsecutiveDefensiveCount";
        public const string HasGuardMotion            = "HasGuardMotion";
    }
}
