// ============================================================
// AUTO-GENERATED — GameplayTagRegistry Editor (Motion 슬롯 포함)
// UPlayGround/GameplayTag/Tag Registry Editor 에서 관리하세요.
// 직접 편집하지 마세요. 저장 후 에디터에서 "코드 생성"을 눌러야 반영됩니다.
// Generated: 2026-04-16 20:47
// ============================================================

using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Gameplay.Tag
{
    /// <summary>
    /// GameplayTag 식별자 열거형.
    /// GameplayTagRegistrySO + Tag Registry Editor 에서 관리하며 코드가 자동 생성된다.
    /// 코드에서는 반드시 이 enum을 사용하고, 문자열을 직접 쓰지 않는다.
    /// </summary>
    public enum GameplayTagId
    {
        None = 0,
        State_Move              = 1,
        State_Sprint            = 2,
        State_Dash              = 3,
        State_Jump              = 4,
        State_Airborne          = 5,
        State_Crouching         = 6,
        State_Dodge             = 7,
        State_Combat            = 8,
        State_Combat_Attack     = 9,
        State_Combat_Guard      = 10,
        State_Combat_Charge     = 11,
        State_Combat_DashAttack = 12,
        State_Combat_JumpAttack = 13,
        State_Hit               = 14,
        State_Death             = 15,
        State_Grabbed           = 16,
        State_Interaction       = 17,
        Combo_Light             = 18,
        Combo_Heavy             = 19,
        State_Combat_Counter      = 20,
        State_Combat_ParryCounter = 21,
    }

    /// <summary>
    /// GameplayTagId → GameplayTag 변환 확장 메서드.
    /// </summary>
    public static class GameplayTagIdExtensions
    {
        private static readonly string[] s_TagNames = new string[]
        {
            "",  // None = 0
            "State.Move",                 // State_Move = 1  (이동 중)
            "State.Sprint",               // State_Sprint = 2  (전력 질주 중)
            "State.Dash",                 // State_Dash = 3  (대시 중)
            "State.Jump",                 // State_Jump = 4  (점프 입력 진입)
            "State.Airborne",             // State_Airborne = 5  (공중 상태)
            "State.Crouching",            // State_Crouching = 6  (웅크리는 중)
            "State.Dodge",                // State_Dodge = 7  (회피 중)
            "State.Combat",               // State_Combat = 8  (전투 상태 (부모))
            "State.Combat.Attack",        // State_Combat_Attack = 9  (공격 중)
            "State.Combat.Guard",         // State_Combat_Guard = 10  (가드 중)
            "State.Combat.Charge",        // State_Combat_Charge = 11  (차지 중)
            "State.Combat.DashAttack",    // State_Combat_DashAttack = 12  (대시 공격 중)
            "State.Combat.JumpAttack",    // State_Combat_JumpAttack = 13  (점프 공격 중)
            "State.Hit",                  // State_Hit = 14  (피격 중)
            "State.Death",                // State_Death = 15  (사망)
            "State.Grabbed",              // State_Grabbed = 16  (잡힌 상태)
            "State.Interaction",          // State_Interaction = 17  (상호작용 중)
            "Combo.Light",                // Combo_Light = 18  (콤보: 약 공격 입력됨)
            "Combo.Heavy",                // Combo_Heavy = 19  (콤보: 강 공격 입력됨)
            "State.Combat.Counter",       // State_Combat_Counter = 20
            "State.Combat.ParryCounter",  // State_Combat_ParryCounter = 21
        };

        /// <summary>GameplayTagId를 GameplayTag 구조체로 변환한다.</summary>
        public static GameplayTag ToTag(this GameplayTagId id)
        {
            int i = (int)id;
            return new GameplayTag(i >= 0 && i < s_TagNames.Length ? s_TagNames[i] : string.Empty);
        }

        /// <summary>GameplayTagId의 태그 이름 문자열을 반환한다.</summary>
        public static string TagName(this GameplayTagId id)
        {
            int i = (int)id;
            return i >= 0 && i < s_TagNames.Length ? s_TagNames[i] : string.Empty;
        }
    }
}

namespace UPlayGround.Data.Actor.Animation
{
    /// <summary>
    /// 코드가 요구하는 공용 모션 의미 슬롯.
    /// 콘텐츠 공격 모션은 이 목록에 추가하지 않고 MotionReferenceSO를 직접 사용한다.
    /// </summary>
    public static class MotionTags
    {
        public static readonly GameplayTag None = default;

        public static readonly GameplayTag FinishAttack = "Motion.Action.FinishAttack";
        public static readonly GameplayTag BreakAttack = "Motion.Action.BreakAttack";

        public static readonly GameplayTag Idle = "Motion.Locomotion.Idle";
        public static readonly GameplayTag Walk = "Motion.Locomotion.Walk";
        public static readonly GameplayTag Run = "Motion.Locomotion.Run";
        public static readonly GameplayTag Sprint = "Motion.Locomotion.Sprint";
        public static readonly GameplayTag Walk_Slow = "Motion.Locomotion.Walk.Slow";

        public static readonly GameplayTag Dodge = "Motion.Action.Dodge";
        public static readonly GameplayTag Dodge_F = "Motion.Action.Dodge.F";
        public static readonly GameplayTag Dodge_B = "Motion.Action.Dodge.B";
        public static readonly GameplayTag Dodge_L = "Motion.Action.Dodge.L";
        public static readonly GameplayTag Dodge_R = "Motion.Action.Dodge.R";
        public static readonly GameplayTag Dash = "Motion.Action.Dash";
        public static readonly GameplayTag Dash_F = "Motion.Action.Dash.F";
        public static readonly GameplayTag Dash_B = "Motion.Action.Dash.B";
        public static readonly GameplayTag Dash_L = "Motion.Action.Dash.L";
        public static readonly GameplayTag Dash_R = "Motion.Action.Dash.R";
        public static readonly GameplayTag Step_F = "Motion.Action.Step.F";
        public static readonly GameplayTag Step_B = "Motion.Action.Step.B";
        public static readonly GameplayTag Step_L = "Motion.Action.Step.L";
        public static readonly GameplayTag Step_R = "Motion.Action.Step.R";

        public static readonly GameplayTag Jump = "Motion.Air.Jump";
        public static readonly GameplayTag Fall = "Motion.Air.Fall";
        public static readonly GameplayTag Land = "Motion.Air.Land";
        public static readonly GameplayTag DoubleJump = "Motion.Air.DoubleJump";
        public static readonly GameplayTag Fly_Start = "Motion.Fly.Start";
        public static readonly GameplayTag Fly_Move = "Motion.Fly.Move";
        public static readonly GameplayTag Fly_Landing = "Motion.Fly.Landing";
        public static readonly GameplayTag Fly_Attack = "Motion.Fly.Attack";
        public static readonly GameplayTag Fly_Idle = "Motion.Fly.Idle";

        public static readonly GameplayTag Crouch_Idle = "Motion.Crouch.Idle";
        public static readonly GameplayTag Crouch_Walk = "Motion.Crouch.Walk";
        public static readonly GameplayTag Idle_To_Crouch = "Motion.Crouch.Enter";
        public static readonly GameplayTag Crouch_To_Idle = "Motion.Crouch.Exit";

        public static readonly GameplayTag Hit_F = "Motion.Reaction.Hit.F";
        public static readonly GameplayTag Hit_B = "Motion.Reaction.Hit.B";
        public static readonly GameplayTag Hit_L = "Motion.Reaction.Hit.L";
        public static readonly GameplayTag Hit_R = "Motion.Reaction.Hit.R";
        public static readonly GameplayTag Die = "Motion.Reaction.Death";
        public static readonly GameplayTag Getup = "Motion.Reaction.Getup";
        public static readonly GameplayTag Guard = "Motion.Reaction.Guard";
        public static readonly GameplayTag GuardBreak = "Motion.Reaction.Guard.Break";
        public static readonly GameplayTag Stun = "Motion.Reaction.Stun";
        public static readonly GameplayTag Block = "Motion.Reaction.Block";
        public static readonly GameplayTag Knockback = "Motion.Reaction.Knockback";
        public static readonly GameplayTag Knockdown = "Motion.Reaction.Knockdown";
        public static readonly GameplayTag Knockdown_Getup = "Motion.Reaction.Knockdown.Getup";
        public static readonly GameplayTag Grabbed = "Motion.Reaction.Grabbed";
        public static readonly GameplayTag Grabbed_End = "Motion.Reaction.Grabbed.End";

        public static readonly GameplayTag HandGathering = "Motion.Interaction.Gathering.Hand";
        public static readonly GameplayTag Woodcutting = "Motion.Interaction.Woodcutting";
        public static readonly GameplayTag Mining_Ground = "Motion.Interaction.Mining.Ground";
        public static readonly GameplayTag Mining_Wall = "Motion.Interaction.Mining.Wall";
        public static readonly GameplayTag ItemPickup = "Motion.Interaction.ItemPickup";
        public static readonly GameplayTag Drink = "Motion.Interaction.Drink";
        public static readonly GameplayTag GroundWork_Start = "Motion.Interaction.GroundWork.Start";
        public static readonly GameplayTag GroundWork_Loop = "Motion.Interaction.GroundWork.Loop";
        public static readonly GameplayTag GroundWork_End = "Motion.Interaction.GroundWork.End";
        public static readonly GameplayTag Fishing_Throw = "Motion.Interaction.Fishing.Throw";
        public static readonly GameplayTag Fishing_Idle = "Motion.Interaction.Fishing.Idle";
        public static readonly GameplayTag Fishing_End = "Motion.Interaction.Fishing.End";
        public static readonly GameplayTag Fishing_CatchStart = "Motion.Interaction.Fishing.Catch.Start";
        public static readonly GameplayTag Fishing_CatchLoop = "Motion.Interaction.Fishing.Catch.Loop";
        public static readonly GameplayTag Fishing_CatchEnd = "Motion.Interaction.Fishing.Catch.End";
        public static readonly GameplayTag Fishing_Catch = "Motion.Interaction.Fishing.Catch";

        public static readonly GameplayTag Equip_LeftWeapon = "Motion.Equipment.Left.Equip";
        public static readonly GameplayTag Equip_RightWeapon = "Motion.Equipment.Right.Equip";
        public static readonly GameplayTag Equip_Sword = "Motion.Equipment.Sword.Equip";
        public static readonly GameplayTag Equip_Shield = "Motion.Equipment.Shield.Equip";
        public static readonly GameplayTag Equip_GreatSword = "Motion.Equipment.GreatSword.Equip";
        public static readonly GameplayTag Equip_Staff = "Motion.Equipment.Staff.Equip";
        public static readonly GameplayTag Equip_Bow = "Motion.Equipment.Bow.Equip";
        public static readonly GameplayTag Equip_Arrow = "Motion.Equipment.Arrow.Equip";
        public static readonly GameplayTag Equip_Katana = "Motion.Equipment.Katana.Equip";
        public static readonly GameplayTag UnEquip_Katana = "Motion.Equipment.Katana.Unequip";
        public static readonly GameplayTag Equip_Weapon = "Motion.Equipment.Main.Equip";
        public static readonly GameplayTag Equip_SubWeapon = "Motion.Equipment.Sub.Equip";
        public static readonly GameplayTag UnEquip_Weapon = "Motion.Equipment.Main.Unequip";
        public static readonly GameplayTag UnEquip_SubWeapon = "Motion.Equipment.Sub.Unequip";
        public static readonly GameplayTag Talk_1 = "Motion.Npc.Talk";

        public static readonly GameplayTag Move_Stop_Walking = "Motion.Stop.Walking.F";
        public static readonly GameplayTag Move_Stop_Running = "Motion.Stop.Running.F";
        public static readonly GameplayTag Move_Stop_Sprinting = "Motion.Stop.Sprinting.F";
        public static readonly GameplayTag Move_Stop_Walking_L45 = "Motion.Stop.Walking.L45";
        public static readonly GameplayTag Move_Stop_Walking_R45 = "Motion.Stop.Walking.R45";
        public static readonly GameplayTag Move_Stop_Running_L45 = "Motion.Stop.Running.L45";
        public static readonly GameplayTag Move_Stop_Running_R45 = "Motion.Stop.Running.R45";
        public static readonly GameplayTag Move_Stop_Sprinting_L45 = "Motion.Stop.Sprinting.L45";
        public static readonly GameplayTag Move_Stop_Sprinting_R45 = "Motion.Stop.Sprinting.R45";

        public static readonly GameplayTag Stand_Idle_Turn_L45 = "Motion.Turn.Idle.L45";
        public static readonly GameplayTag Stand_Idle_Turn_R45 = "Motion.Turn.Idle.R45";
        public static readonly GameplayTag Stand_Idle_Turn_L90 = "Motion.Turn.Idle.L90";
        public static readonly GameplayTag Stand_Idle_Turn_R90 = "Motion.Turn.Idle.R90";
        public static readonly GameplayTag Stand_Idle_Turn_180 = "Motion.Turn.Idle.180";
        public static readonly GameplayTag Walk_Turn_L45 = "Motion.Turn.Walk.L45";
        public static readonly GameplayTag Walk_Turn_R45 = "Motion.Turn.Walk.R45";
        public static readonly GameplayTag Walk_Turn_L90 = "Motion.Turn.Walk.L90";
        public static readonly GameplayTag Walk_Turn_R90 = "Motion.Turn.Walk.R90";
        public static readonly GameplayTag Walk_Turn_180 = "Motion.Turn.Walk.180";
        public static readonly GameplayTag Run_Turn_L45 = "Motion.Turn.Run.L45";
        public static readonly GameplayTag Run_Turn_R45 = "Motion.Turn.Run.R45";
        public static readonly GameplayTag Run_Turn_L90 = "Motion.Turn.Run.L90";
        public static readonly GameplayTag Run_Turn_R90 = "Motion.Turn.Run.R90";
        public static readonly GameplayTag Run_Turn_180 = "Motion.Turn.Run.180";
        public static readonly GameplayTag Sprint_Turn_L45 = "Motion.Turn.Sprint.L45";
        public static readonly GameplayTag Sprint_Turn_R45 = "Motion.Turn.Sprint.R45";
        public static readonly GameplayTag Sprint_Turn_L90 = "Motion.Turn.Sprint.L90";
        public static readonly GameplayTag Sprint_Turn_R90 = "Motion.Turn.Sprint.R90";
        public static readonly GameplayTag Sprint_Turn_180 = "Motion.Turn.Sprint.180";

        public static readonly GameplayTag Walk_Slow_B = "Motion.Locomotion.Walk.Slow.B";
        public static readonly GameplayTag Walk_Slow_B_L45 = "Motion.Locomotion.Walk.Slow.B.L45";
        public static readonly GameplayTag Walk_Slow_B_R45 = "Motion.Locomotion.Walk.Slow.B.R45";
        public static readonly GameplayTag Walk_Slow_F_L45 = "Motion.Locomotion.Walk.Slow.F.L45";
        public static readonly GameplayTag Walk_Slow_F_R45 = "Motion.Locomotion.Walk.Slow.F.R45";
        public static readonly GameplayTag Walk_Slow_F_L90 = "Motion.Locomotion.Walk.Slow.F.L90";
        public static readonly GameplayTag Walk_Slow_F_R90 = "Motion.Locomotion.Walk.Slow.F.R90";
        public static readonly GameplayTag Walk_B = "Motion.Locomotion.Walk.B";
        public static readonly GameplayTag Walk_B_L45 = "Motion.Locomotion.Walk.B.L45";
        public static readonly GameplayTag Walk_B_R45 = "Motion.Locomotion.Walk.B.R45";
        public static readonly GameplayTag Walk_F_L45 = "Motion.Locomotion.Walk.F.L45";
        public static readonly GameplayTag Walk_F_R45 = "Motion.Locomotion.Walk.F.R45";
        public static readonly GameplayTag Walk_F_L90 = "Motion.Locomotion.Walk.F.L90";
        public static readonly GameplayTag Walk_F_R90 = "Motion.Locomotion.Walk.F.R90";
        public static readonly GameplayTag Run_B = "Motion.Locomotion.Run.B";
        public static readonly GameplayTag Run_B_L45 = "Motion.Locomotion.Run.B.L45";
        public static readonly GameplayTag Run_B_R45 = "Motion.Locomotion.Run.B.R45";
        public static readonly GameplayTag Run_F_L45 = "Motion.Locomotion.Run.F.L45";
        public static readonly GameplayTag Run_F_R45 = "Motion.Locomotion.Run.F.R45";
        public static readonly GameplayTag Run_F_L90 = "Motion.Locomotion.Run.F.L90";
        public static readonly GameplayTag Run_F_R90 = "Motion.Locomotion.Run.F.R90";
    }
}
