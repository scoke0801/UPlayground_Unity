namespace UPlayGround.AI.CombatDecision
{
    /// <summary>
    /// Intent 점수 계산에 사용되는 조건 식별자.
    /// 새 조건이 필요할 때 enum에 추가하고 IntentConditionEvaluator.Evaluate 분기에 매핑한다.
    /// </summary>
    public enum IntentConditionId
    {
        None = 0,

        // 거리 조건
        InAttackRange,                       // Distance <= Optimal AND HasAvailableAttack
        TooClose,                            // Distance <= PersonalSpace
        UnderPreferredRange,                 // Distance < max(PersonalSpace, Preferred - 0.45)
        OverPreferredRange,                  // Distance > Preferred + 0.75
        IsDistanceWithinOptimal,             // Distance <= Optimal
        IsDistanceWithinPreferredPlusBuffer, // Distance <= Preferred + 1.5
        IsDistanceWithinMinDistance,         // Distance <= MinDistance
        IsDistanceFarFromOptimal,            // Distance > Optimal + 1.5

        // 자기 상태
        LowHealth,                           // HealthPercent <= 0.35
        IsPoiseBroken,
        TimeSinceRetreatBelowMinCooldown,    // TimeSinceRetreat < MinRetreatCooldown

        // 행동 가능성
        ActionDelayElapsed,
        CanUseSkill,
        HasAvailableAttack,
        HasGuardMotion,

        // 플레이어 즉시 상태
        IsPlayerAttacking,
        IsPlayerGuarding,
        IsPlayerStaggered,
        IsPlayerRecovering,

        // 플레이어 빈도 관찰
        IsPlayerDodgingFrequently,
        IsPlayerAttackingFrequently,
        IsPlayerGuardingFrequently,
        IsPlayerRecoveringFrequently,

        // 피격 기록
        WasHitRecently
    }
}
