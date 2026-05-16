namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 비행형 적의 BT 전환 대상 State.
    /// 지상 전용 <see cref="EnemyTransitionStateType"/>와 분리되어 비행 BT 노드에서만 사용된다.
    /// </summary>
    public enum FlyingEnemyTransitionStateType
    {
        Idle,
        Patrol,
        Chase,
        GroundAttack,
        Circle,
        Retreat,
        TakeOff,
        AirCircle,
        Land,
        Dive,
    }

    /// <summary>
    /// 비행 BT 노드/JSON에서 공유하는 Blackboard 키.
    /// 지상 <see cref="EnemyBlackboardKeys"/>의 일부 키(HasTarget 등)는 그대로 재사용한다.
    /// </summary>
    public static class FlyingEnemyBlackboardKeys
    {
        public const string AirAttackCount = "AirAttackCount";
        public const string AirAttackLimit = "AirAttackLimit";
        public const string GroundTimer = "GroundTimer";
        public const string GroundAttackCount = "GroundAttackCount";
    }
}
