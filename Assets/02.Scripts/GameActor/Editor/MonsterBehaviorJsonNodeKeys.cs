#if UNITY_EDITOR
namespace UPlayGround.AI.BehaviorTree.Editor
{
    /// <summary>
    /// Monster Behavior JSON 스키마에서 사용하는 condition/action 키와 select 키워드를
    /// 단일 출처로 모은 카탈로그. Importer, Validator, Exporter는 모두 이 상수를 참조해야 한다.
    /// JSON 파일에 직렬화되는 wire format이므로 값 자체를 변경하면 기존 SourceJson/Generated JSON과 호환되지 않음에 주의.
    /// EnemyBlackboardKeys는 런타임 블랙보드 키 도메인이고, 이 타입은 JSON schema condition/action 키 도메인이다.
    /// </summary>
    internal static class MonsterBehaviorJsonNodeKeys
    {
        internal static class Conditions
        {
            public const string HasTarget = "HasTarget";
            public const string IsBlockedEnemyState = "IsBlockedEnemyState";
            public const string HasStateTag = "HasStateTag";
            public const string BlackboardCompare = "BlackboardCompare";
            public const string IsEnemyPhase = "IsEnemyPhase";
            public const string DistanceLessOrEqual = "DistanceLessOrEqual";
            public const string DistanceGreater = "DistanceGreater";
            public const string ActionDelayElapsed = "ActionDelayElapsed";
            public const string CanUseSkill = "CanUseSkill";
            public const string HasAttackInRange = "HasAttackInRange";
            public const string HasLineOfSight = "HasLineOfSight";
            public const string IsPlayerAttacking = "IsPlayerAttacking";
            public const string IsPlayerGuarding = "IsPlayerGuarding";
            public const string IsPlayerStaggered = "IsPlayerStaggered";
            public const string IsPlayerRecovering = "IsPlayerRecovering";
            public const string IsPlayerDodgingFrequently = "IsPlayerDodgingFrequently";
            public const string IsPlayerAttackingFrequently = "IsPlayerAttackingFrequently";
            public const string IsPlayerGuardingFrequently = "IsPlayerGuardingFrequently";
            public const string IsPlayerRecoveringFrequently = "IsPlayerRecoveringFrequently";
            public const string RecentlyHitByPlayer = "RecentlyHitByPlayer";
            public const string HasAttackSlot = "HasAttackSlot";
            public const string CooldownReady = "CooldownReady";
            public const string IsSelfLowHealth = "IsSelfLowHealth";
            public const string WasLastHitHeavy = "WasLastHitHeavy";
            public const string IsPoiseBroken = "IsPoiseBroken";
            public const string RecentHitCountGreaterOrEqual = "RecentHitCountGreaterOrEqual";
            public const string ConsecutiveAttackCountLessThan = "ConsecutiveAttackCountLessThan";
            public const string ConsecutiveAttackCountGreaterOrEqual = "ConsecutiveAttackCountGreaterOrEqual";
            public const string CanIgnoreLightHit = "CanIgnoreLightHit";
            public const string CanRevengeAfterHit = "CanRevengeAfterHit";
            public const string SelectedIntent = "SelectedIntent";
            public const string IsCurrentState = "IsCurrentState";
            public const string IsFlyingAirState = "IsFlyingAirState";
            public const string IsFlyingGroundCombatState = "IsFlyingGroundCombatState";
            public const string IsAirAttackLimitReached = "IsAirAttackLimitReached";
            public const string ShouldFlyingTakeOff = "ShouldFlyingTakeOff";
            public const string FlyingCanUseSkill = "FlyingCanUseSkill";
            public const string HasDiveSkillAvailable = "HasDiveSkillAvailable";
            public const string RollDiveChance = "RollDiveChance";
        }

        internal static class Actions
        {
            public const string KeepCurrentState = "KeepCurrentState";
            public const string PatrolOrIdle = "PatrolOrIdle";
            public const string Transition = "Transition";
            public const string RequestAction = "RequestAction";
            public const string RequestAttackSlot = "RequestAttackSlot";
            public const string ExecuteAttack = "ExecuteAttack";
            public const string Wait = "Wait";
            public const string FlyingTransition = "FlyingTransition";
            public const string FlyingPatrolOrIdle = "FlyingPatrolOrIdle";
            public const string ResetFlyingCounters = "ResetFlyingCounters";
            public const string ResetFlyingAirCounters = "ResetFlyingAirCounters";
            public const string DescendFlying = "DescendFlying";
            public const string RequestFlyingAttackSlot = "RequestFlyingAttackSlot";
            public const string SelectFlyingDiveSkill = "SelectFlyingDiveSkill";
        }

        internal static class SelectKinds
        {
            public const string WeightedRandom = "WeightedRandom";
        }

        internal static class ActorKinds
        {
            public const string Ground = "Ground";
            public const string Flying = "Flying";
        }
    }
}
#endif
