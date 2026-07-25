using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Gameplay.Tag
{
    /// <summary>런타임 코드가 직접 참조하는 프로젝트 표준 태그.</summary>
    public static class GameplayTags
    {
        public static readonly GameplayTag None = default;
        public static readonly GameplayTag State_Move = GameplayTag.CreateCodeDefined("State.Move");
        public static readonly GameplayTag State_Sprint = GameplayTag.CreateCodeDefined("State.Sprint");
        public static readonly GameplayTag State_Dash = GameplayTag.CreateCodeDefined("State.Dash");
        public static readonly GameplayTag State_Jump = GameplayTag.CreateCodeDefined("State.Jump");
        public static readonly GameplayTag State_Airborne = GameplayTag.CreateCodeDefined("State.Airborne");
        public static readonly GameplayTag State_Crouching = GameplayTag.CreateCodeDefined("State.Crouching");
        public static readonly GameplayTag State_Dodge = GameplayTag.CreateCodeDefined("State.Dodge");
        public static readonly GameplayTag State_Combat = GameplayTag.CreateCodeDefined("State.Combat");
        public static readonly GameplayTag State_Combat_Attack = GameplayTag.CreateCodeDefined("State.Combat.Attack");
        public static readonly GameplayTag State_Combat_Guard = GameplayTag.CreateCodeDefined("State.Combat.Guard");
        public static readonly GameplayTag State_Combat_Charge = GameplayTag.CreateCodeDefined("State.Combat.Charge");
        public static readonly GameplayTag State_Combat_DashAttack = GameplayTag.CreateCodeDefined("State.Combat.DashAttack");
        public static readonly GameplayTag State_Combat_JumpAttack = GameplayTag.CreateCodeDefined("State.Combat.JumpAttack");
        public static readonly GameplayTag State_Hit = GameplayTag.CreateCodeDefined("State.Hit");
        public static readonly GameplayTag State_Death = GameplayTag.CreateCodeDefined("State.Death");
        public static readonly GameplayTag State_Grabbed = GameplayTag.CreateCodeDefined("State.Grabbed");
        public static readonly GameplayTag State_Interaction = GameplayTag.CreateCodeDefined("State.Interaction");
        public static readonly GameplayTag Combo_Light = GameplayTag.CreateCodeDefined("Combo.Light");
        public static readonly GameplayTag Combo_Heavy = GameplayTag.CreateCodeDefined("Combo.Heavy");
        public static readonly GameplayTag State_Combat_Counter = GameplayTag.CreateCodeDefined("State.Combat.Counter");
    }
}

namespace UPlayGround.Data.Actor.Animation
{
    /// <summary>런타임 코드가 직접 참조하는 공용 모션 의미 슬롯.</summary>
    public static class MotionTags
    {
        public static readonly GameplayTag None = default;
        public static readonly GameplayTag Dash = GameplayTag.CreateCodeDefined("Motion.Action.Dash");
        public static readonly GameplayTag Dash_B = GameplayTag.CreateCodeDefined("Motion.Action.Dash.B");
        public static readonly GameplayTag Dash_F = GameplayTag.CreateCodeDefined("Motion.Action.Dash.F");
        public static readonly GameplayTag Dash_L = GameplayTag.CreateCodeDefined("Motion.Action.Dash.L");
        public static readonly GameplayTag Dash_R = GameplayTag.CreateCodeDefined("Motion.Action.Dash.R");
        public static readonly GameplayTag Dodge = GameplayTag.CreateCodeDefined("Motion.Action.Dodge");
        public static readonly GameplayTag Dodge_B = GameplayTag.CreateCodeDefined("Motion.Action.Dodge.B");
        public static readonly GameplayTag Dodge_F = GameplayTag.CreateCodeDefined("Motion.Action.Dodge.F");
        public static readonly GameplayTag Dodge_L = GameplayTag.CreateCodeDefined("Motion.Action.Dodge.L");
        public static readonly GameplayTag Dodge_R = GameplayTag.CreateCodeDefined("Motion.Action.Dodge.R");
        public static readonly GameplayTag Step_B = GameplayTag.CreateCodeDefined("Motion.Action.Step.B");
        public static readonly GameplayTag Step_F = GameplayTag.CreateCodeDefined("Motion.Action.Step.F");
        public static readonly GameplayTag Step_L = GameplayTag.CreateCodeDefined("Motion.Action.Step.L");
        public static readonly GameplayTag Step_R = GameplayTag.CreateCodeDefined("Motion.Action.Step.R");
        public static readonly GameplayTag DoubleJump = GameplayTag.CreateCodeDefined("Motion.Air.DoubleJump");
        public static readonly GameplayTag Fall = GameplayTag.CreateCodeDefined("Motion.Air.Fall");
        public static readonly GameplayTag Jump = GameplayTag.CreateCodeDefined("Motion.Air.Jump");
        public static readonly GameplayTag Land = GameplayTag.CreateCodeDefined("Motion.Air.Land");
        public static readonly GameplayTag Idle_To_Crouch = GameplayTag.CreateCodeDefined("Motion.Crouch.Enter");
        public static readonly GameplayTag Crouch_To_Idle = GameplayTag.CreateCodeDefined("Motion.Crouch.Exit");
        public static readonly GameplayTag Crouch_Idle = GameplayTag.CreateCodeDefined("Motion.Crouch.Idle");
        public static readonly GameplayTag Crouch_Walk = GameplayTag.CreateCodeDefined("Motion.Crouch.Walk");
        public static readonly GameplayTag Equip_Arrow = GameplayTag.CreateCodeDefined("Motion.Equipment.Arrow.Equip");
        public static readonly GameplayTag Equip_Bow = GameplayTag.CreateCodeDefined("Motion.Equipment.Bow.Equip");
        public static readonly GameplayTag Equip_GreatSword = GameplayTag.CreateCodeDefined("Motion.Equipment.GreatSword.Equip");
        public static readonly GameplayTag Equip_Katana = GameplayTag.CreateCodeDefined("Motion.Equipment.Katana.Equip");
        public static readonly GameplayTag UnEquip_Katana = GameplayTag.CreateCodeDefined("Motion.Equipment.Katana.Unequip");
        public static readonly GameplayTag Equip_LeftWeapon = GameplayTag.CreateCodeDefined("Motion.Equipment.Left.Equip");
        public static readonly GameplayTag Equip_Weapon = GameplayTag.CreateCodeDefined("Motion.Equipment.Main.Equip");
        public static readonly GameplayTag UnEquip_Weapon = GameplayTag.CreateCodeDefined("Motion.Equipment.Main.Unequip");
        public static readonly GameplayTag Equip_RightWeapon = GameplayTag.CreateCodeDefined("Motion.Equipment.Right.Equip");
        public static readonly GameplayTag Equip_Shield = GameplayTag.CreateCodeDefined("Motion.Equipment.Shield.Equip");
        public static readonly GameplayTag Equip_Staff = GameplayTag.CreateCodeDefined("Motion.Equipment.Staff.Equip");
        public static readonly GameplayTag Equip_SubWeapon = GameplayTag.CreateCodeDefined("Motion.Equipment.Sub.Equip");
        public static readonly GameplayTag UnEquip_SubWeapon = GameplayTag.CreateCodeDefined("Motion.Equipment.Sub.Unequip");
        public static readonly GameplayTag Equip_Sword = GameplayTag.CreateCodeDefined("Motion.Equipment.Sword.Equip");
        public static readonly GameplayTag Fly_Attack = GameplayTag.CreateCodeDefined("Motion.Fly.Attack");
        public static readonly GameplayTag Fly_Idle = GameplayTag.CreateCodeDefined("Motion.Fly.Idle");
        public static readonly GameplayTag Fly_Landing = GameplayTag.CreateCodeDefined("Motion.Fly.Landing");
        public static readonly GameplayTag Fly_Move = GameplayTag.CreateCodeDefined("Motion.Fly.Move");
        public static readonly GameplayTag Fly_Start = GameplayTag.CreateCodeDefined("Motion.Fly.Start");
        public static readonly GameplayTag Drink = GameplayTag.CreateCodeDefined("Motion.Interaction.Drink");
        public static readonly GameplayTag Fishing_Catch = GameplayTag.CreateCodeDefined("Motion.Interaction.Fishing.Catch");
        public static readonly GameplayTag Fishing_CatchEnd = GameplayTag.CreateCodeDefined("Motion.Interaction.Fishing.Catch.End");
        public static readonly GameplayTag Fishing_CatchLoop = GameplayTag.CreateCodeDefined("Motion.Interaction.Fishing.Catch.Loop");
        public static readonly GameplayTag Fishing_CatchStart = GameplayTag.CreateCodeDefined("Motion.Interaction.Fishing.Catch.Start");
        public static readonly GameplayTag Fishing_End = GameplayTag.CreateCodeDefined("Motion.Interaction.Fishing.End");
        public static readonly GameplayTag Fishing_Idle = GameplayTag.CreateCodeDefined("Motion.Interaction.Fishing.Idle");
        public static readonly GameplayTag Fishing_Throw = GameplayTag.CreateCodeDefined("Motion.Interaction.Fishing.Throw");
        public static readonly GameplayTag HandGathering = GameplayTag.CreateCodeDefined("Motion.Interaction.Gathering.Hand");
        public static readonly GameplayTag GroundWork_End = GameplayTag.CreateCodeDefined("Motion.Interaction.GroundWork.End");
        public static readonly GameplayTag GroundWork_Loop = GameplayTag.CreateCodeDefined("Motion.Interaction.GroundWork.Loop");
        public static readonly GameplayTag GroundWork_Start = GameplayTag.CreateCodeDefined("Motion.Interaction.GroundWork.Start");
        public static readonly GameplayTag ItemPickup = GameplayTag.CreateCodeDefined("Motion.Interaction.ItemPickup");
        public static readonly GameplayTag Mining_Ground = GameplayTag.CreateCodeDefined("Motion.Interaction.Mining.Ground");
        public static readonly GameplayTag Mining_Wall = GameplayTag.CreateCodeDefined("Motion.Interaction.Mining.Wall");
        public static readonly GameplayTag Woodcutting = GameplayTag.CreateCodeDefined("Motion.Interaction.Woodcutting");
        public static readonly GameplayTag Idle = GameplayTag.CreateCodeDefined("Motion.Locomotion.Idle");
        public static readonly GameplayTag Run = GameplayTag.CreateCodeDefined("Motion.Locomotion.Run");
        public static readonly GameplayTag Run_B = GameplayTag.CreateCodeDefined("Motion.Locomotion.Run.B");
        public static readonly GameplayTag Run_B_L45 = GameplayTag.CreateCodeDefined("Motion.Locomotion.Run.B.L45");
        public static readonly GameplayTag Run_B_R45 = GameplayTag.CreateCodeDefined("Motion.Locomotion.Run.B.R45");
        public static readonly GameplayTag Run_F_L45 = GameplayTag.CreateCodeDefined("Motion.Locomotion.Run.F.L45");
        public static readonly GameplayTag Run_F_L90 = GameplayTag.CreateCodeDefined("Motion.Locomotion.Run.F.L90");
        public static readonly GameplayTag Run_F_R45 = GameplayTag.CreateCodeDefined("Motion.Locomotion.Run.F.R45");
        public static readonly GameplayTag Run_F_R90 = GameplayTag.CreateCodeDefined("Motion.Locomotion.Run.F.R90");
        public static readonly GameplayTag Sprint = GameplayTag.CreateCodeDefined("Motion.Locomotion.Sprint");
        public static readonly GameplayTag Walk = GameplayTag.CreateCodeDefined("Motion.Locomotion.Walk");
        public static readonly GameplayTag Walk_B = GameplayTag.CreateCodeDefined("Motion.Locomotion.Walk.B");
        public static readonly GameplayTag Walk_B_L45 = GameplayTag.CreateCodeDefined("Motion.Locomotion.Walk.B.L45");
        public static readonly GameplayTag Walk_B_R45 = GameplayTag.CreateCodeDefined("Motion.Locomotion.Walk.B.R45");
        public static readonly GameplayTag Walk_F_L45 = GameplayTag.CreateCodeDefined("Motion.Locomotion.Walk.F.L45");
        public static readonly GameplayTag Walk_F_L90 = GameplayTag.CreateCodeDefined("Motion.Locomotion.Walk.F.L90");
        public static readonly GameplayTag Walk_F_R45 = GameplayTag.CreateCodeDefined("Motion.Locomotion.Walk.F.R45");
        public static readonly GameplayTag Walk_F_R90 = GameplayTag.CreateCodeDefined("Motion.Locomotion.Walk.F.R90");
        public static readonly GameplayTag Walk_Slow = GameplayTag.CreateCodeDefined("Motion.Locomotion.Walk.Slow");
        public static readonly GameplayTag Walk_Slow_B = GameplayTag.CreateCodeDefined("Motion.Locomotion.Walk.Slow.B");
        public static readonly GameplayTag Walk_Slow_B_L45 = GameplayTag.CreateCodeDefined("Motion.Locomotion.Walk.Slow.B.L45");
        public static readonly GameplayTag Walk_Slow_B_R45 = GameplayTag.CreateCodeDefined("Motion.Locomotion.Walk.Slow.B.R45");
        public static readonly GameplayTag Walk_Slow_F_L45 = GameplayTag.CreateCodeDefined("Motion.Locomotion.Walk.Slow.F.L45");
        public static readonly GameplayTag Walk_Slow_F_L90 = GameplayTag.CreateCodeDefined("Motion.Locomotion.Walk.Slow.F.L90");
        public static readonly GameplayTag Walk_Slow_F_R45 = GameplayTag.CreateCodeDefined("Motion.Locomotion.Walk.Slow.F.R45");
        public static readonly GameplayTag Walk_Slow_F_R90 = GameplayTag.CreateCodeDefined("Motion.Locomotion.Walk.Slow.F.R90");
        public static readonly GameplayTag Talk_1 = GameplayTag.CreateCodeDefined("Motion.Npc.Talk");
        public static readonly GameplayTag Block = GameplayTag.CreateCodeDefined("Motion.Reaction.Block");
        public static readonly GameplayTag Die = GameplayTag.CreateCodeDefined("Motion.Reaction.Death");
        public static readonly GameplayTag Getup = GameplayTag.CreateCodeDefined("Motion.Reaction.Getup");
        public static readonly GameplayTag Grabbed = GameplayTag.CreateCodeDefined("Motion.Reaction.Grabbed");
        public static readonly GameplayTag Grabbed_End = GameplayTag.CreateCodeDefined("Motion.Reaction.Grabbed.End");
        public static readonly GameplayTag Guard = GameplayTag.CreateCodeDefined("Motion.Reaction.Guard");
        public static readonly GameplayTag GuardBreak = GameplayTag.CreateCodeDefined("Motion.Reaction.Guard.Break");
        public static readonly GameplayTag Hit_B = GameplayTag.CreateCodeDefined("Motion.Reaction.Hit.B");
        public static readonly GameplayTag Hit_F = GameplayTag.CreateCodeDefined("Motion.Reaction.Hit.F");
        public static readonly GameplayTag Hit_L = GameplayTag.CreateCodeDefined("Motion.Reaction.Hit.L");
        public static readonly GameplayTag Hit_R = GameplayTag.CreateCodeDefined("Motion.Reaction.Hit.R");
        public static readonly GameplayTag Knockback = GameplayTag.CreateCodeDefined("Motion.Reaction.Knockback");
        public static readonly GameplayTag Knockdown = GameplayTag.CreateCodeDefined("Motion.Reaction.Knockdown");
        public static readonly GameplayTag Knockdown_Getup = GameplayTag.CreateCodeDefined("Motion.Reaction.Knockdown.Getup");
        public static readonly GameplayTag Stun = GameplayTag.CreateCodeDefined("Motion.Reaction.Stun");
        public static readonly GameplayTag Move_Stop_Running = GameplayTag.CreateCodeDefined("Motion.Stop.Running.F");
        public static readonly GameplayTag Move_Stop_Running_L45 = GameplayTag.CreateCodeDefined("Motion.Stop.Running.L45");
        public static readonly GameplayTag Move_Stop_Running_R45 = GameplayTag.CreateCodeDefined("Motion.Stop.Running.R45");
        public static readonly GameplayTag Move_Stop_Sprinting = GameplayTag.CreateCodeDefined("Motion.Stop.Sprinting.F");
        public static readonly GameplayTag Move_Stop_Sprinting_L45 = GameplayTag.CreateCodeDefined("Motion.Stop.Sprinting.L45");
        public static readonly GameplayTag Move_Stop_Sprinting_R45 = GameplayTag.CreateCodeDefined("Motion.Stop.Sprinting.R45");
        public static readonly GameplayTag Move_Stop_Walking = GameplayTag.CreateCodeDefined("Motion.Stop.Walking.F");
        public static readonly GameplayTag Move_Stop_Walking_L45 = GameplayTag.CreateCodeDefined("Motion.Stop.Walking.L45");
        public static readonly GameplayTag Move_Stop_Walking_R45 = GameplayTag.CreateCodeDefined("Motion.Stop.Walking.R45");
        public static readonly GameplayTag Stand_Idle_Turn_180 = GameplayTag.CreateCodeDefined("Motion.Turn.Idle.180");
        public static readonly GameplayTag Stand_Idle_Turn_L45 = GameplayTag.CreateCodeDefined("Motion.Turn.Idle.L45");
        public static readonly GameplayTag Stand_Idle_Turn_L90 = GameplayTag.CreateCodeDefined("Motion.Turn.Idle.L90");
        public static readonly GameplayTag Stand_Idle_Turn_R45 = GameplayTag.CreateCodeDefined("Motion.Turn.Idle.R45");
        public static readonly GameplayTag Stand_Idle_Turn_R90 = GameplayTag.CreateCodeDefined("Motion.Turn.Idle.R90");
        public static readonly GameplayTag Run_Turn_180 = GameplayTag.CreateCodeDefined("Motion.Turn.Run.180");
        public static readonly GameplayTag Run_Turn_L45 = GameplayTag.CreateCodeDefined("Motion.Turn.Run.L45");
        public static readonly GameplayTag Run_Turn_L90 = GameplayTag.CreateCodeDefined("Motion.Turn.Run.L90");
        public static readonly GameplayTag Run_Turn_R45 = GameplayTag.CreateCodeDefined("Motion.Turn.Run.R45");
        public static readonly GameplayTag Run_Turn_R90 = GameplayTag.CreateCodeDefined("Motion.Turn.Run.R90");
        public static readonly GameplayTag Sprint_Turn_180 = GameplayTag.CreateCodeDefined("Motion.Turn.Sprint.180");
        public static readonly GameplayTag Sprint_Turn_L45 = GameplayTag.CreateCodeDefined("Motion.Turn.Sprint.L45");
        public static readonly GameplayTag Sprint_Turn_L90 = GameplayTag.CreateCodeDefined("Motion.Turn.Sprint.L90");
        public static readonly GameplayTag Sprint_Turn_R45 = GameplayTag.CreateCodeDefined("Motion.Turn.Sprint.R45");
        public static readonly GameplayTag Sprint_Turn_R90 = GameplayTag.CreateCodeDefined("Motion.Turn.Sprint.R90");
        public static readonly GameplayTag Walk_Turn_180 = GameplayTag.CreateCodeDefined("Motion.Turn.Walk.180");
        public static readonly GameplayTag Walk_Turn_L45 = GameplayTag.CreateCodeDefined("Motion.Turn.Walk.L45");
        public static readonly GameplayTag Walk_Turn_L90 = GameplayTag.CreateCodeDefined("Motion.Turn.Walk.L90");
        public static readonly GameplayTag Walk_Turn_R45 = GameplayTag.CreateCodeDefined("Motion.Turn.Walk.R45");
        public static readonly GameplayTag Walk_Turn_R90 = GameplayTag.CreateCodeDefined("Motion.Turn.Walk.R90");
        public static readonly GameplayTag BreakAttack = GameplayTag.CreateCodeDefined("Motion.Action.BreakAttack");
        public static readonly GameplayTag FinishAttack = GameplayTag.CreateCodeDefined("Motion.Action.FinishAttack");
        public static readonly GameplayTag Locomotion = GameplayTag.CreateCodeDefined("Motion.Locomotion");
        public static readonly GameplayTag Stop = GameplayTag.CreateCodeDefined("Motion.Stop");
        public static readonly GameplayTag Turn = GameplayTag.CreateCodeDefined("Motion.Turn");
        public static readonly GameplayTag Air = GameplayTag.CreateCodeDefined("Motion.Air");
        public static readonly GameplayTag Crouch = GameplayTag.CreateCodeDefined("Motion.Crouch");
        public static readonly GameplayTag Equipment = GameplayTag.CreateCodeDefined("Motion.Equipment");
    }
}

