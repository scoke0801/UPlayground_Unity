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
        Dodge = 10,
        JumpBack = 11
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
        public const string IsPlayerAttackingFrequently = "IsPlayerAttackingFrequently";
        public const string IsPlayerGuardingFrequently = "IsPlayerGuardingFrequently";
        public const string IsPlayerRecoveringFrequently = "IsPlayerRecoveringFrequently";
        public const string PlayerDodgeCount = "PlayerDodgeCount";
        public const string PlayerGuardCount = "PlayerGuardCount";
        public const string PlayerAttackCount = "PlayerAttackCount";
        public const string PlayerRecoverCount = "PlayerRecoverCount";
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

        public const string SelectedIntent = "SelectedIntent";
        public const string LastIntent = "LastIntent";
        public const string ConsecutiveIntentCount = "ConsecutiveIntentCount";
        public const string IntentScoreAttack = "IntentScore_Attack";
        public const string IntentScorePunish = "IntentScore_Punish";
        public const string IntentScoreCounter = "IntentScore_Counter";
        public const string IntentScorePressure = "IntentScore_Pressure";
        public const string IntentScoreChase = "IntentScore_Chase";
        public const string IntentScoreRetreat = "IntentScore_Retreat";
        public const string IntentScoreKeepDistance = "IntentScore_KeepDistance";
        public const string IntentScoreDefend = "IntentScore_Defend";
        public const string IntentScoreRecover = "IntentScore_Recover";
        public const string CombatRhythmPhase = "CombatRhythmPhase";
        public const string EnemyAIRole = "EnemyAIRole";
        public const string IntentWeightAttack = "IntentWeight_Attack";
        public const string IntentWeightPunish = "IntentWeight_Punish";
        public const string IntentWeightCounter = "IntentWeight_Counter";
        public const string IntentWeightPressure = "IntentWeight_Pressure";
        public const string IntentWeightChase = "IntentWeight_Chase";
        public const string IntentWeightRetreat = "IntentWeight_Retreat";
        public const string IntentWeightKeepDistance = "IntentWeight_KeepDistance";
        public const string IntentWeightDefend = "IntentWeight_Defend";
        public const string IntentWeightRecover = "IntentWeight_Recover";
    }
}
