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
        public const string BossAssist = "BossAssist";
        
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

        public const string QuickSlot_Left = "QuickSlot_Left";
        public const string QuickSlot_Right = "QuickSlot_Right";
        public const string QuickSlot_Up = "QuickSlot_Up";
        public const string QuickSlot_Down = "QuickSlot_Down";

        public const string ElementBuff = "ElementBuff";
    }

    /// <summary>전투 입력별 선입력 유지 시간을 한 곳에서 제공한다.</summary>
    public static class PlayerInputBufferPolicy
    {
        public const float AttackDuration = 0.24f;
        public const float StandardDuration = 0.15f;
        public const float MovementDuration = 0.12f;
        public const float SkillDuration = 0.20f;
        public const float DefaultDuration = StandardDuration;

        /// <summary>
        /// performed 단계에서 공용 버퍼에 즉시 적재해도 되는지 반환한다.
        /// 강공격은 같은 버튼의 탭/차지를 릴리스에서 판정하므로, performed 입력을 먼저 적재하면
        /// 대시 공격이 그 임시 입력을 소비한 뒤 릴리스가 새 강공격으로 중복 확정될 수 있다.
        /// </summary>
        public static bool ShouldBufferOnPerformed(string actionName)
        {
            return actionName != PlayerAction.HeavyAttack;
        }

        /// <summary>액션에 맞는 선입력 유지 시간을 반환한다.</summary>
        public static float GetDuration(string actionName)
        {
            return actionName switch
            {
                PlayerAction.Attack => AttackDuration,
                PlayerAction.HeavyAttack => AttackDuration,
                PlayerAction.Dodge => StandardDuration,
                PlayerAction.Jump => MovementDuration,
                PlayerAction.Dash => MovementDuration,
                PlayerAction.SkillAbility => SkillDuration,
                PlayerAction.SkillUltimate => SkillDuration,
                PlayerAction.ElementBuff => SkillDuration,
                PlayerAction.CharacterSwap_1 => StandardDuration,
                PlayerAction.CharacterSwap_2 => StandardDuration,
                PlayerAction.CharacterSwap_3 => StandardDuration,
                PlayerAction.CharacterSwap_4 => StandardDuration,
                _ => DefaultDuration,
            };
        }
    }

    public static class SystemAction
    {   
        public const string ShowCursor = "ShowCursor";
        public const string Back = "Back";
    }
    
    public static class UIAction
    {   
        public const string Navigate = "Navigate";
        public const string Inventory = "Inventory";
        public const string EquipInventory = "EquipInventory";
        public const string Map = "Map";
        public const string Party = "Party";
        public const string MenuPanel = "MenuPanel";
        public const string CheatPanel = "CheatPanel";
        
        public const string Submit = "Submit";
        public const string Cancel = "Cancel";
        public const string Point = "Point";
        public const string Click = "Click";
        public const string RightClick = "RightClick";
        public const string MiddleClick = "MiddleClick";
        public const string ScrollWheel = "ScrollWheel";
        public const string DialogueNext = "DialogueNext";
        public const string DialogueSkip = "DialogueSkip";
        public const string DialogueToggleAuto = "DialogueToggleAuto";
        public const string DialogueBacklog = "DialogueBacklog";
        public const string MainTabPrevious = "MainTabPrevious";
        public const string MainTabNext = "MainTabNext";
        public const string SubTabPrevious = "SubTabPrevious";
        public const string SubTabNext = "SubTabNext";
        public const string VirtualCursorMove = "VirtualCursorMove";

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
