namespace UPlayGround.InputDefine
{
    public static class InputMapNames
    {
        public const string PlayerAction = "PlayerAction";
        public const string UI = "UI";
        public const string System = "System";
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
        
        public const string Attack = "Attack";
        public const string HeavyAttack = "HeavyAttack";
        
        public const string Skill_1 = "Skill_1";
        public const string Skill_2 = "Skill_2";
        public const string Skill_3 = "Skill_3";
        public const string Skill_4 = "Skill_4";
        
        public const string Interact = "Interact";
        
        public const string Equip = "Equip";
    }

    public static class SystemAction
    {   
        public const string ShowCursor = "ShowCursor";
        public const string Pause = "Pause";
    }
    
    public static class UIAction
    {   
        public const string Inventory = "Inventory";
        
        public const string Submit = "Submit";
        public const string Cancel = "Cancel";
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