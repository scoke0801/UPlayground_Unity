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
        public const string TargetHas = "Target.Has";
        public const string TargetObject = "Target.Object";
        public const string TargetDistance = "Target.Distance";
        public const string SelfStateId = "Self.StateId";
        public const string SelfStateTags = "Self.StateTags";
        public const string SelfHpPercent = "Self.HpPercent";
        public const string SelfPhaseName = "Self.PhaseName";
        public const string SelfPhaseIndex = "Self.PhaseIndex";
        public const string AllowCharge = "AllowCharge";
        public const string AllowFlank = "AllowFlank";
        public const string MaxConsecutiveAttacks = "MaxConsecutiveAttacks";
        public const string ContinueAttackChance = "ContinueAttackChance";
        public const string GuardChance = "GuardChance";
        public const string RetreatChance = "RetreatChance";
        public const string CanUseSkill = "CanUseSkill";
        public const string HasAttackSlot = "HasAttackSlot";
        public const string NextActionAllowedTime = "NextActionAllowedTime";

        public const string AIAggression = "AI.Aggression";
        public const string AIReactionChance = "AI.ReactionChance";
        public const string AICounterChance = "AI.CounterChance";
        public const string AIDodgeChance = "AI.DodgeChance";
        public const string AIPunishRecoveryChance = "AI.PunishRecoveryChance";
        public const string AIAntiGuardChance = "AI.AntiGuardChance";
        public const string AIMinRetreatCooldown = "AI.MinRetreatCooldown";
        public const string AIMaxComboPressureCount = "AI.MaxComboPressureCount";
        public const string AIPreferredRange = "AI.PreferredRange";

        public const string HitReactionLockTime = "hitReactionLockTime";
        public const string RevengeChance = "revengeChance";

        public const string MemoryPlayerIsAttacking = "Memory.Player.IsAttacking";
        public const string MemoryPlayerIsGuarding = "Memory.Player.IsGuarding";
        public const string MemoryPlayerIsStaggered = "Memory.Player.IsStaggered";
        public const string MemoryPlayerIsRecovering = "Memory.Player.IsRecovering";
        public const string MemoryPlayerIsDodgingFrequently = "Memory.Player.IsDodgingFrequently";
        public const string MemoryPlayerIsAttackingFrequently = "Memory.Player.IsAttackingFrequently";
        public const string MemoryPlayerIsGuardingFrequently = "Memory.Player.IsGuardingFrequently";
        public const string MemoryPlayerIsRecoveringFrequently = "Memory.Player.IsRecoveringFrequently";
        public const string MemoryPlayerDodgeCount = "Memory.Player.DodgeCount";
        public const string MemoryPlayerGuardCount = "Memory.Player.GuardCount";
        public const string MemoryPlayerAttackCount = "Memory.Player.AttackCount";
        public const string MemoryPlayerRecoverCount = "Memory.Player.RecoverCount";
        public const string MemoryHitRecentlyByPlayer = "Memory.Hit.RecentlyByPlayer";
        public const string MemoryHitRecentCount = "Memory.Hit.RecentCount";
        public const string MemoryHitLastReactionType = "Memory.Hit.LastReactionType";
        public const string SelfPoiseRatio = "Self.PoiseRatio";
        public const string SelfIsPoiseBroken = "Self.IsPoiseBroken";

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

        public const string DecisionSelectedIntent = "Decision.SelectedIntent";
        public const string DecisionLastIntent = "Decision.LastIntent";
        public const string DecisionConsecutiveIntentCount = "Decision.ConsecutiveIntentCount";
        public const string DecisionIntentScoreAttack = "Decision.IntentScore.Attack";
        public const string DecisionIntentScorePunish = "Decision.IntentScore.Punish";
        public const string DecisionIntentScoreCounter = "Decision.IntentScore.Counter";
        public const string DecisionIntentScorePressure = "Decision.IntentScore.Pressure";
        public const string DecisionIntentScoreChase = "Decision.IntentScore.Chase";
        public const string DecisionIntentScoreRetreat = "Decision.IntentScore.Retreat";
        public const string DecisionIntentScoreKeepDistance = "Decision.IntentScore.KeepDistance";
        public const string DecisionIntentScoreDefend = "Decision.IntentScore.Defend";
        public const string DecisionIntentScoreRecover = "Decision.IntentScore.Recover";
        public const string DecisionCombatRhythmPhase = "Decision.CombatRhythmPhase";

        public static string CooldownReadyTime(string cooldownId)
        {
            return $"Cooldown.{cooldownId}.ReadyTime";
        }
    }
}
