namespace UPlayGround.AI.BehaviorTree
{
    public enum EnemyTransitionStateType
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Retreat,
        Circle,
        Guard,
        Charge,
        Flank,
        Counter
    }

    public static class EnemyBlackboardKeys
    {
        public const string HasTarget = "HasTarget";
        public const string Target = "Target";
        public const string DistanceToTarget = "DistanceToTarget";
        public const string CurrentState = "CurrentState";
        public const string HpPercent = "HpPercent";
        public const string CurrentPhaseName = "CurrentPhaseName";
        public const string IsPlayerAttacking = "IsPlayerAttacking";
        public const string IsPlayerGuarding = "IsPlayerGuarding";
        public const string IsPlayerStaggered = "IsPlayerStaggered";
        public const string IsPlayerRecovering = "IsPlayerRecovering";
        public const string IsPlayerDodgingFrequently = "IsPlayerDodgingFrequently";
        public const string CanUseSkill = "CanUseSkill";
        public const string HasAttackSlot = "HasAttackSlot";
        public const string NextActionAllowedTime = "NextActionAllowedTime";
    }
}
