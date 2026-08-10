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

}
