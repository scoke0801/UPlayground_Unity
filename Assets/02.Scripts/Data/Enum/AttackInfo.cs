namespace UPlayGround.Data.Enum
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
        Hit = 0,
        
        KnockBack,
        
        Stun,
    }

    public enum CombatSkillType
    {
        None = 0,
     
    }
    
}