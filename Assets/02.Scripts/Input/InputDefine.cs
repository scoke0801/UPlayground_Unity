namespace UPlayGround.InputDefine
{
    public static class InputMapNames
    {
        public const string PlayerAction = "PlayerAction";
        public const string UI = "UI";
        public const string System = "System";
        
        public const string Gamepad = "Gamepad";
    }
    public static class PlayerAction
    {
        public const string Move = "Move";
        public const string Look = "Look";
        public const string Zoom = "Zoom";
        public const string Jump = "Jump";
        public const string Crouching = "Crouching";
        public const string Walk = "Walk";
        public const string Sprint = "Sprint";
        public const string Dodge = "Dodge";
        public const string Dash = "Dash";
        
        public const string Attack = "Attack";
        public const string HeavyAttack = "HeavyAttack";
        
        public const string Skill_1 = "Skill_1";
        public const string Skill_2 = "Skill_2";
        public const string Skill_3 = "Skill_3";
        public const string Skill_4 = "Skill_4";
        public const string Skill_5 = "Skill_5";
        public const string Skill_6 = "Skill_6";
        public const string Skill_7 = "Skill_7";
        public const string Skill_8 = "Skill_8";
        public const string Skill_9 = "Skill_9";
        
        public const string Interact = "Interact";
        
        public const string Equip = "Equip";
        
        public const string LockOn = "LockOn";
        public const string LockOnSwitchLeft = "LockOnSwitchLeft";
        public const string LockOnSwitchRight = "LockOnSwitchRight";
        
        public const string Guard = "Guard";
    }

    public static class SystemAction
    {   
        public const string ShowCursor = "ShowCursor";
        public const string Back = "Back";
    }
    
    public static class UIAction
    {   
        public const string Inventory = "Inventory";
        public const string EquipInventory = "EquipInventory";
        
        public const string Submit = "Submit";
        public const string Cancel = "Cancel";
        public const string DialogueNext = "DialogueNext";
    }

    public static class GamepadAction
    {
        public const string L1 = "L1";
        public const string L2 = "L2";
        public const string L3 = "L3";
        
        public const string R1 = "R1";
        public const string R2 = "R2";
        public const string R3 = "R3";
        
        public const string Up = "Up";
        public const string Down = "Down";
        public const string Left = "Left";
        public const string Right = "Right";
        
        public const string North = "North";
        public const string South = "South";
        public const string East = "East";
        public const string West = "West";
        
        public const string Select = "Select";
        public const string Start = "Start";
        
        public const string Touchpad = "Touchpad";
    }
    public enum InputLayer
    {
        //  == CanvasLayer
        None = -1,
        
        Level_0 = 0,        // == HUD
        Level_1 = 1000,    // == Scene
        Level_2 = 2000,    // == Popup
        Level_3 = 3000,    // == System
        
        Level_Top = 10000  // 어디서든 입력 가능해야 하는 경우
    }
}