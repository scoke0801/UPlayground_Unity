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
    /// 적 AI가 BT에서 요청하는 공격 선택 카테고리.
    /// None은 기존 동작처럼 전체 사용 가능 공격 풀을 의미한다.
    /// </summary>
    public enum EnemyAttackCategory
    {
        None = 0,
        Basic = 1,
        Heavy = 2,
        Skill = 3,
    }

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
        ChargeAttack  = 6,  // 차지 공격 (홀드 후 릴리즈)
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

    /// <summary>
    /// 플레이어가 이 공격에 대해 취할 수 있는 방어 대응 분류.
    /// Danger Ring 색(패링 가능=노랑 / 불가=빨강)과 퍼펙트 가드 카운터 성립 여부를 결정한다.
    /// 기존 동작(모든 공격 카운터 가능) 보존을 위해 기본값은 Parryable.
    /// </summary>
    public enum AttackDefenseType
    {
        Parryable,     // 퍼펙트 가드 시 패링/카운터 성립 (기본) — 노란 링
        GuardableOnly, // 막을 수는 있으나 카운터 불가 — 노란 링(카운터 표시 없음)
        Unblockable,   // 가드 불가, 회피 필수 (명조 Red Warning / 세키로 危) — 붉은 링
    }
    
}
