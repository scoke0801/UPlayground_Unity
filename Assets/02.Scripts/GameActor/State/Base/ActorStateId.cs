namespace UPlayGround.State
{
    /// <summary>
    /// 액터 상태 전이 계약에 사용하는 안정적인 식별자.
    /// 문자열 표현은 디버그 및 BT 블랙보드 표시 용도로만 사용한다.
    /// </summary>
    public enum ActorStateId
    {
        None = 0,

        // 액터 공통 의미 상태
        Idle = 1,
        Airborne = 2,
        Attack = 3,
        Charge = 4,
        Dash = 5,
        Death = 6,
        Dodge = 7,
        Grabbed = 8,
        Guard = 9,
        Hit = 10,
        Knockdown = 11,
        Stun = 12,
        Land = 13,

        // 플레이어 상태
        GroundMove = 100,
        Crouching = 101,
        DashAttack = 102,
        Drink = 103,
        FinishAttack = 104,
        GuardBreak = 105,
        Interaction = 106,
        JumpAttack = 107,
        JumpDashAttack = 108,
        SpecialBreakAttack = 109,
        Stop = 110,
        TurnInPlace = 111,

        // 지상 몬스터 상태
        Chase = 200,
        Circle = 201,
        Counter = 202,
        Flank = 203,
        JumpBack = 204,
        Patrol = 205,
        Retreat = 206,
        SpecialBreakVictim = 207,

        // 비행 몬스터 상태
        Flying_AirCircle = 300,
        Flying_Chase = 301,
        Flying_Circle = 302,
        Flying_Dive = 303,
        Flying_GroundAttack = 304,
        Flying_Land = 305,
        Flying_Patrol = 306,
        Flying_Retreat = 307,
        Flying_TakeOff = 308,

        // NPC 상태
        Talk = 400,
        Wander = 401,
    }
}
