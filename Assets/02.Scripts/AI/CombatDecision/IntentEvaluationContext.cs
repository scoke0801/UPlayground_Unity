namespace UPlayGround.AI.CombatDecision
{
    /// <summary>
    /// Intent 점수 계산에 필요한 모든 입력값을 한 번에 모은 컨텍스트.
    /// EnemyCombatDecisionEvaluator가 매 Tick 채우고 IntentConditionEvaluator가 읽는다.
    /// 임시 구조체이므로 readonly로 만들지 않고 field setter를 허용한다.
    /// </summary>
    public struct IntentEvaluationContext
    {
        // 거리 관련 절대값
        public float Distance;
        public float OptimalDistance;
        public float MinDistance;
        public float PersonalSpace;
        public float PreferredRange;

        // 자기 자신
        public float HealthPercent;

        // 연속값 (0~1 또는 양수)
        public float Aggression;
        public float ReactionChance;
        public float PunishChance;
        public float CounterChance;
        public float RetreatChance;
        public float GuardChance;
        public float CircleWeight;

        // 후퇴 쿨다운
        public float MinRetreatCooldown;
        public float TimeSinceRetreat;

        // 행동 가능성
        public bool ActionDelayElapsed;
        public bool CanUseSkill;
        public bool HasAvailableAttack;
        public bool HasGuardMotion;

        // 플레이어 즉시 상태
        public bool IsPlayerAttacking;
        public bool IsPlayerGuarding;
        public bool IsPlayerStaggered;
        public bool IsPlayerRecovering;

        // 플레이어 빈도 관찰
        public bool IsPlayerDodgingFrequently;
        public bool IsPlayerAttackingFrequently;
        public bool IsPlayerGuardingFrequently;
        public bool IsPlayerRecoveringFrequently;

        // 플레이어 행동 예측
        public PlayerActionToken PredictedNextPlayerAction;
        public float PredictionConfidence;

        // 피격 기록
        public bool WasHitRecently;
        public bool IsPoiseBroken;
    }
}
