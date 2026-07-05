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
        
        public const string SkillAbility = "SkillAbility";
        public const string SkillUltimate = "SkillUltimate";
        
        public const string Interact = "Interact";
        
        public const string Equip = "Equip";
        
        public const string LockOn = "LockOn";
        public const string LockOnSwitchLeft = "LockOnSwitchLeft";
        public const string LockOnSwitchRight = "LockOnSwitchRight";
        
        public const string Guard = "Guard";

        public const string CharacterSwap_1 = "CharacterSwap_1";
        public const string CharacterSwap_2 = "CharacterSwap_2";
        public const string CharacterSwap_3 = "CharacterSwap_3";
        public const string CharacterSwap_4 = "CharacterSwap_4";
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
        public const string Map = "Map";
        public const string Party = "Party";
        public const string MenuPanel = "MenuPanel";
        public const string CheatPanel = "CheatPanel";
        
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

    /// <summary>
    /// 현재 활성화된 입력 디바이스 분류.
    /// 키 프롬프트 UI가 표시할 글리프(키보드/마우스 vs 게임패드)를 결정하는 단일 기준.
    /// 키보드와 마우스는 PC에서 항상 함께 쓰이므로 하나로 묶는다(명조/원신 관례).
    /// </summary>
    public enum ActiveInputDevice
    {
        KeyboardMouse,
        Gamepad,
    }

    /// <summary>
    /// 게임패드 브랜드. 같은 물리 버튼이라도 표기가 다르다(buttonSouth = Xbox A / PS ✕ / Switch B).
    /// 브랜드별 글리프가 비어 있으면 Generic 세트로 폴백한다.
    /// </summary>
    public enum GamepadBrand
    {
        Generic,
        Xbox,
        PlayStation,
        Switch,
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