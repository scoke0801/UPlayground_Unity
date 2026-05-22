namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// BT가 상태 이름 대신 요청하는 전술 의도.
    /// 실제 GameActorState 선택은 EnemyActionResolver가 담당한다.
    /// </summary>
    public enum EnemyActionIntent
    {
        None = 0,
        Attack = 1,
        Punish = 2,
        Counter = 3,
        Pressure = 4,
        Chase = 5,
        Retreat = 6,
        KeepDistance = 7,
        Defend = 8,
        Evade = 9,
        Recover = 10
    }
}
