
namespace UPlayGround.Data.Enum
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
        
        Dodge = 10,
        
        Jump = 20,
        Fall,
        Land,

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
        
        JumpAttack_1 = 400,
        JumpAttack_2,
        JumpAttack_3,
        JumpAttack_4,
        JumpAttack_5,
        
        Skill_1 = 500,
        Skill_2,
        Skill_3,
        Skill_4,
        
        Hit_F = 700,
        Hit_B,
        Hit_L,
        Hit_R,
        
        Die = 800,
        
        Getup = 820,
        
        Guard = 840,
        Block = 860,
        
        Knockback = 900,
        
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
        
        // 정지 (Stop)
        Move_Stop_Walking = 5000,
        Move_Stop_Running,
        Move_Stop_Sprinting,

        #region Trun

        Walk_Turn_L45,
        Walk_Turn_R45,
        Walk_Turn_L90,
        Walk_Turn_R90,
        Walk_Turn_180,

        Run_Turn_L45,
        Run_Turn_R45,
        Run_Turn_L90,
        Run_Turn_R90,
        Run_Turn_180,

        Sprint_Turn_L45,
        Sprint_Turn_R45,
        Sprint_Turn_L90,
        Sprint_Turn_R90,
        Sprint_Turn_180,

        #endregion


        Mixer_Locomotion,
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