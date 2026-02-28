namespace UPlayGround.Data.EnumType
{

    /// <summary>
    /// 스킬 타입 정의
    /// </summary>
    public enum SkillType
    {
        None = 0,
        Attack,     // 일반 공격
        Heal,       // 자가 치유
        Spawn,      // 몬스터 소환
        Buff,       // 버프
        Debuff      // 디버프
    }  
    
    /// <summary>
    /// AI 전투 스타일
    /// </summary>
    public enum EnemyCombatStyle
    {
        Melee,      // 근접 - 계속 접근
        Ranged,     // 원거리 - 거리 유지
        Balanced,   // 균형 - 중거리 유지
        Support     // 서포터 - 멀리서 스킬 사용
    }
    
    public enum AttackType { Melee, Ranged } // 공격 유형 정의

    public enum AttackReactionType
    {
        None = 0,       // 반응 없음 (Poise로 버팀)
        Light,          // 가벼운 경직 (짧음, 캔슬 빠름)
        Hit,            // 일반 경직
        Heavy,          // 무거운 경직 (긴 경직, 후퇴)
        KnockBack,      // 넉백
        Stun,           // 스턴 (장시간)
    }

    public enum CombatSkillType
    {
        None = 0,
     
    }
    
}