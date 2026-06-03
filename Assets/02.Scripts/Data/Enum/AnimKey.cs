
namespace UPlayGround.Data.EnumType
{
    /// <summary>
    /// 애니메이션 클립 구분하기 위한 기본 Key
    /// </summary>
    public enum AnimKey
    {
        None = 0,

        Idle,
        Walk,
        Run,
        Sprint,
        
        // 단순 (방향 무관)
        Dodge = 10,
        Dash = 11,
        
        // 방향성
        Dodge_F = 12,
        Dodge_B = 13,
        Dodge_L = 14,
        Dodge_R = 15,
        
        Dash_F = 16,
        Dash_B = 17,
        Dash_L = 18,
        Dash_R = 19,

        // 우선 안씀, Dash를 사용
        Step_F = 35,
        Step_B = 36,
        Step_L = 37,
        Step_R = 38,

        Jump = 20,
        Fall,
        Land,
        DoubleJump,

        // 공중 이동 몬스터 전용
        Fly_Start = 25,  // 지상 → 공중 전환 (이륙)
        Fly_Move,        // 공중 순항 루프
        Fly_Landing,     // 공중 → 지상 착지
        Fly_Attack,      // 공중 공격 (1종)
        Fly_Idle,

        // [TODO] 이런 경우는 하나의 묶음이 되어야 하지 않을까?
        Crouch_Idle = 30,
        Crouch_Walk,
        Idle_To_Crouch,
        Crouch_To_Idle,
        
        Attack_1 = 100,
        Attack_2,
        Attack_3,
        Attack_4,
        Attack_5,
        Attack_6,
        Attack_7,
        Attack_8,
        Attack_9,
        Attack_10,
        
        HeavyAttack_1 = 200,
        HeavyAttack_2,
        HeavyAttack_3,
        HeavyAttack_4,
        HeavyAttack_5,
        HeavyAttack_6,
        HeavyAttack_7,
        HeavyAttack_8,
        HeavyAttack_9,
        HeavyAttack_10,
        
        DashAttack_1 = 300,
        DashAttack_2,
        DashAttack_3,
        DashAttack_4,
        DashAttack_5,
        
        JumpDashAttack_1 = 310,
        
        JumpAttack_1 = 400,
        JumpAttack_2,
        JumpAttack_3,
        JumpAttack_4,
        JumpAttack_5,
        JumpAttack_6,
        JumpAttack_7,
        
        Skill_1 = 500,
        Skill_2,
        Skill_3,
        Skill_4,
        Skill_5,
        Skill_6,
        Skill_7,
        Skill_8,
        Skill_9,
        
        Counter_Attack_1 = 530,
        Counter_Attack_2,
        
        Player_SwapAttack_1 = 550,
        Player_SwapAttack_2,
        Player_SwapAttack_3,
        Player_SwapAttack_4,
        Player_SwapAttack_5,
        Player_SwapSpecialAttack_1,
        Player_SwapEvadeCounterAttack_1,
        
        // 차지 공격 — 하나의 애니메이션 안에 InfiniteLoop LoopEvent로 차지 구간을 정의
        ChargeAttack_1 = 620,
        ChargeAttack_2,
        ChargeAttack_3,
        ChargeAttack_4,
        ChargeAttack_5,

        FinishAttack = 690,
        
        Hit_F = 700,
        Hit_B,
        Hit_L,
        Hit_R,
        
        Die = 800,
        
        Getup = 820,
        
        Guard = 840,
        GuardBreak = 841,
        Stun = 850, 
        Block = 860,
        
        Knockback = 900,
        Knockdown,          // 넘어뜨리기
        Knockdown_Getup,    // 넘어진 후 일어서기
        
        Grabbed = 920,      // 잡힘 — 행동 불능 루프
        Grabbed_End,        // 잡힘 해제 (탈출 또는 시간 만료)
        
        HandGathering = 1000,
        
        Woodcutting = 1500,
        
        Mining_Ground = 1600,
        Mining_Wall,
        
        GroundWork_Start = 1800,
        GroundWork_Loop,
        GroundWork_End,
        
        Fishing_Throw = 1900,
        Fishing_Idle,
        Fishing_End,
        Fishing_CatchStart,
        Fishing_CatchLoop,
        Fishing_CatchEnd,
        Fishing_Catch,

        Equip_LeftWeapon = 2000,
        Equip_RightWeapon,
        Equip_Sword,
        Equip_Shield,
        Equip_GreatSword,
        Equip_Staff,
        Equip_Bow,
        Equip_Arrow,
        Equip_Katana = 2100,
        UnEquip_Katana,
        
        Equip_Weapon = 2110,
        Equip_SubWeapon,
        UnEquip_Weapon,
        UnEquip_SubWeapon,
        
        // NpcAction
        Talk_1 = 3000,
        
        // 정지 (Stop) — 방향별로 세분화: F / F_L45 / F_R45
        Move_Stop_Walking = 5000,
        Move_Stop_Running,
        Move_Stop_Sprinting,
        Move_Stop_Walking_L45,
        Move_Stop_Walking_R45,
        Move_Stop_Running_L45,
        Move_Stop_Running_R45,
        Move_Stop_Sprinting_L45,
        Move_Stop_Sprinting_R45,

        #region Turn

        // Idle TurnInPlace — Stand_Idle_Turn_*
        Stand_Idle_Turn_L45 = 5100,
        Stand_Idle_Turn_R45,
        Stand_Idle_Turn_L90,
        Stand_Idle_Turn_R90,
        Stand_Idle_Turn_180,

        // Walk TurnInPlace — Walk_F_Turn_*
        Walk_Turn_L45 = 5110,
        Walk_Turn_R45,
        Walk_Turn_L90,
        Walk_Turn_R90,
        Walk_Turn_180,

        // Run TurnInPlace — Run_F_Turn_*
        Run_Turn_L45 = 5120,
        Run_Turn_R45,
        Run_Turn_L90,
        Run_Turn_R90,
        Run_Turn_180,

        // Sprint TurnInPlace — Sprint_F_Turn_*
        Sprint_Turn_L45 = 5130,
        Sprint_Turn_R45,
        Sprint_Turn_L90,
        Sprint_Turn_R90,
        Sprint_Turn_180,

        #endregion

        #region 방향성 이동 (몬스터 로코모션)

        // Walk Slow (순찰·경계 속도) — Walk_Slow_F = 전진
        Walk_Slow       = 6000,
        Walk_Slow_B,
        Walk_Slow_B_L45,
        Walk_Slow_B_R45,
        Walk_Slow_F_L45,
        Walk_Slow_F_R45,
        Walk_Slow_F_L90,
        Walk_Slow_F_R90,

        // Walk (전투 보행) — Walk(기존) = 전진, 이하 나머지 방향
        Walk_B          = 6010,
        Walk_B_L45,
        Walk_B_R45,
        Walk_F_L45,
        Walk_F_R45,
        Walk_F_L90,
        Walk_F_R90,

        // Run (전투 질주) — Run(기존) = 전진, 이하 나머지 방향
        Run_B           = 6020,
        Run_B_L45,
        Run_B_R45,
        Run_F_L45,
        Run_F_R45,
        Run_F_L90,
        Run_F_R90,

        #endregion
    }

    /// <summary>
    /// 애니메이션 레이어 구분용
    /// </summary>
    public enum AnimLayer
    {
        FullBody = 0,
        UpperBody = 1,
        LowerBody = 2,

        Head = 3,
        Eye = 4,

        LeftHand = 10,
        RightHand = 11,
        LeftFoot = 20,
        RightFoot = 21,
    }

    public enum InteractionAnimEvent
    {
        OnHit,
        CatchFish
    }
    /// <summary>
    /// 이동 애니메이션 재생 시, 어떤 유형의 애니메이션을 재생할 지
    /// </summary>
    public enum BaseMoveAnimType
    {
        // Run을 기본으로, 그 외 상태는 토글
        Walk = 0,
        Run,        // 기본
        Sprint,
        Crouching
    }
}
