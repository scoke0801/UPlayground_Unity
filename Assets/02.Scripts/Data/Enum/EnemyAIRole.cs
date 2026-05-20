namespace UPlayGround.Data.EnumType
{
    /// <summary>
    /// Intent 점수 보정에 사용하는 몬스터 전투 역할.
    /// 실제 행동 실행은 BT와 상태 머신이 담당한다.
    /// </summary>
    public enum EnemyAIRole
    {
        Melee = 0,
        RangedSupport = 1,
        RangedMain = 2,
        Healer = 3,
        Summoner = 4
    }
}
