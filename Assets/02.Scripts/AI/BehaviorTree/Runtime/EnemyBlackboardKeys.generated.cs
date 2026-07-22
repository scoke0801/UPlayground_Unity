// 자동 생성 파일입니다. 직접 수정하지 마세요.
// UPlayGround/생성 도구/Enemy Blackboard Keys 생성 메뉴에서 재생성하세요.
// Source: Assets/10.Datas/AI/BehaviorTree/BehaviorTreeEditorRegistry.json
// Identifier rule: key를 PascalCase로 자동 변환하며, 충돌/가독성 문제가 있으면 JSON identifier 필드를 사용합니다.

namespace UPlayGround.AI.BehaviorTree
{
    public static partial class EnemyBlackboardKeys
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
        public const string PredictionPlayerNextAction = "Prediction.Player.NextAction";
        public const string PredictionConfidence = "Prediction.Confidence";
        public const string PredictionPlayerLastToken = "Prediction.Player.LastToken";
        public const string PredictionPlayerTimeSinceLast = "Prediction.Player.TimeSinceLast";
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
        public const string EnablePatrol = "enablePatrol";
        public const string OptimalCombatDistance = "optimalCombatDistance";
        public const string MinCombatDistance = "minCombatDistance";
        public const string PersonalSpaceDistance = "personalSpaceDistance";
        public const string CircleWeight = "circleWeight";
        public const string GroupIntentAttackMultiplier = "Group.Intent.AttackMultiplier";
        public const string GroupIntentPunishMultiplier = "Group.Intent.PunishMultiplier";
        public const string GroupIntentCounterMultiplier = "Group.Intent.CounterMultiplier";
        public const string GroupIntentPressureBonus = "Group.Intent.PressureBonus";
        public const string GroupIntentKeepDistanceBonus = "Group.Intent.KeepDistanceBonus";
        public const string GroupIntentRetreatBonus = "Group.Intent.RetreatBonus";
        public const string GroupBreatherRemainingTime = "Group.BreatherRemainingTime";
        public const string GroupFormationSlotIndex = "Group.FormationSlotIndex";
        public const string GroupAggroFitness = "Group.AggroFitness";
    }
}
