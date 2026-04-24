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

        // ── Self Stats ──────────────────────────────
        public const string SelfHPPercent = "SelfHPPercent";

        // ── Flying ──────────────────────────────────
        public const string ShouldTakeOff    = "ShouldTakeOff";     // Bool: 지상 공격 한도/체류 초과
        public const string ShouldDescend    = "ShouldDescend";     // Bool: 공중 공격 한도 초과
        public const string IsAirState       = "IsAirState";        // Bool
        public const string GroundAttackCount = "GroundAttackCount"; // Int
        public const string AirAttackCount   = "AirAttackCount";    // Int
    }
}
