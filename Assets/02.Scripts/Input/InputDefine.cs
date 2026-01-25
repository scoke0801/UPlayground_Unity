namespace UPlayGround.InputDefine
{
    public static class InputMapNames
    {
        public const string PlayerAction = "PlayerAction";
        public const string UI = "UI";
    }

    public static class PlayerAction
    {
        public const string Move = "Move";
        public const string Look = "Look";
        public const string Zoom = "Zoom";
        public const string Jump = "Jump";
        public const string Run = "Run";
        public const string Roll = "Roll";
        public const string Attack = "Attack";
        public const string HeavyAttack = "HeavyAttack";
        public const string Sprint = "Sprint";
        public const string Interact = "Interact";
    }

    public static class System
    {   
        public const string ShowCursor = "ShowCursor";
        public const string Navigate = "Navigate";
        public const string Submit = "Submit";
        public const string Cancel = "Cancel";
    }
    
    public static class UI
    {   
        public const string Inventory = "Inventory";
        public const string Pause = "Pause";
    }

    public enum InputLayer
    {
        //  == CanvasLayer
        None = -1,
        
        Level_0 = 0,        // == HUD
        Level_1 = 1000,    // == Scene
        Level_2 = 2000,    // == Popup
        Level_3 = 3000,    // == System
    }
}