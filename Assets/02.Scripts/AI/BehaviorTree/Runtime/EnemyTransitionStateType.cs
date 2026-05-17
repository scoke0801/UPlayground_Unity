namespace UPlayGround.AI.BehaviorTree
{
    public enum EnemyTransitionStateType
    {
        Idle = 0,
        Patrol = 1,
        Chase = 2,
        Attack = 3,
        Retreat = 4,
        Circle = 5,
        Guard = 6,
        Charge = 7,
        Flank = 8,
        Counter = 9,
        Dodge = 10
    }

    public static class EnemyBlackboardKeys
    {
        public const string HasTarget = "HasTarget";
        public const string Target = "Target";
        public const string DistanceToTarget = "DistanceToTarget";
        public const string CurrentState = "CurrentState";
        public const string HpPercent = "HpPercent";
        public const string CurrentPhaseName = "CurrentPhaseName";
        public const string PhaseIndex = "PhaseIndex";
        public const string AllowCharge = "AllowCharge";
        public const string AllowFlank = "AllowFlank";
        public const string MaxConsecutiveAttacks = "MaxConsecutiveAttacks";
        public const string ContinueAttackChance = "ContinueAttackChance";
        public const string GuardChance = "GuardChance";
        public const string RetreatChance = "RetreatChance";
        public const string IsPlayerAttacking = "IsPlayerAttacking";
        public const string IsPlayerGuarding = "IsPlayerGuarding";
        public const string IsPlayerStaggered = "IsPlayerStaggered";
        public const string IsPlayerRecovering = "IsPlayerRecovering";
        public const string IsPlayerDodgingFrequently = "IsPlayerDodgingFrequently";
        public const string CanUseSkill = "CanUseSkill";
        public const string HasAttackSlot = "HasAttackSlot";
        public const string NextActionAllowedTime = "NextActionAllowedTime";

        public const string Aggression = "aggression";
        public const string ReactionChance = "reactionChance";
        public const string CounterChance = "counterChance";
        public const string DodgeChance = "dodgeChance";
        public const string PunishRecoveryChance = "punishRecoveryChance";
        public const string AntiGuardChance = "antiGuardChance";
        public const string MinRetreatCooldown = "minRetreatCooldown";
        public const string MaxComboPressureCount = "maxComboPressureCount";
        public const string PreferredRange = "preferredRange";

        public const string RecentlyHitByPlayer = "RecentlyHitByPlayer";
        public const string RecentHitCount = "recentHitCount";
        public const string LastHitReactionType = "lastHitReactionType";
        public const string PoiseRatio = "poiseRatio";
        public const string IsPoiseBroken = "isPoiseBroken";
        public const string HitReactionLockTime = "hitReactionLockTime";
        public const string RevengeChance = "revengeChance";
    }
}
