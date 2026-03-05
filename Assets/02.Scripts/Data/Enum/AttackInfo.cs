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

    /// <summary>
    /// 공격 종류 — 스킬 게이지 충전량 구분용
    /// </summary>
    public enum AttackKind
    {
        NormalAttack  = 0,  // 약 공격 콤보
        HeavyAttack   = 1,  // 강 공격 콤보
        JumpAttack    = 2,  // 점프 공격
        DashAttack    = 3,  // 대시 공격
        FinishAttack  = 4,  // 마무리(처형) 공격
        SkillAttack   = 5,  // 스킬 (게이지 충전 없음)
    }

    public enum AttackReactionType
    {
        None = 0,       // 반응 없음 (Poise로 버팀)
        Light,          // 가벼운 경직 (짧음, 캔슬 빠름)
        Hit,            // 일반 경직
        Heavy,          // 무거운 경직 (긴 경직, 후퇴)
        KnockBack,      // 넉백 (공격 방향으로 밀림)
        Stun,           // 스턴 (장시간)
        Pull,           // 끌어당기기 (공격자 방향으로 당겨옴)
        Airborne,       // 공중으로 띄움
        Knockdown,      // 넘어뜨리기. Knockback 애니 있으면 재생, 없으면 Hit으로 폴백
        Grab,           // (애니메이션이 없다)잡기 — 대상의 행동을 일정 시간 제한
    }

    public enum CombatSkillType
    {
        None = 0,
     
    }
    
}