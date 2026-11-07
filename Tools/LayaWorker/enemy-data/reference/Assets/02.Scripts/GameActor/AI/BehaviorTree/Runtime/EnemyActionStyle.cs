namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 같은 의도 안에서 선호하는 실행 방식.
    /// None이면 Resolver가 현재 몬스터 능력과 컨텍스트에 맞는 기본 상태를 고른다.
    /// </summary>
    public enum EnemyActionStyle
    {
        None = 0,
        Dodge = 1,
        JumpBack = 2,
        Guard = 3,
        Circle = 4,
        Flank = 5,
        Charge = 6,
        Dive = 7,
        Land = 8,
        TakeOff = 9,
        Patrol = 10,
        Idle = 11,
        Step = 12,

        /// <summary>
        /// 비행형 전용 공중 선회. Circle(지상 견제)과 다른 State이며 대응 Intent가 없어
        /// 스코어러 경로로는 진입할 수 없었다.
        /// </summary>
        AirCircle = 13
    }
}
