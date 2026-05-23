// 자동 생성 파일입니다. 직접 수정하지 마세요.
// UPlayGround/ID Enum Generator 창에서 재생성하세요.
// Generated: 2026-05-23 00:22
namespace UPlayGround.Data.Path
{
    /// <summary>UIKeyType — UI Prefab 키 열거형 (자동 생성)</summary>
    public enum UIKeyType
    {
        None = 0,
        InteractionKeyUI = 1,
        InteractionHPBoard = 2,
        PauseMenu = 3,
        Inventory = 4,
        Cursor = 5,
        ItemAcquisitionList = 6,
        GamePlay = 7,
        ItemPopup = 8,
        HudPlayerInfo = 9,
        ActorHpBar = 10,
        TitleMenu = 11,
        MainDialogue = 12,
        SystemDialogue = 13,
        MonologueDialogue = 14,
        DamageFloater = 15,
        Minimap = 16,
        Map = 17,
        RespawnPopup = 18,
        Party = 19,
        Craft = 20,
        Quest = 21,
        MenuPanel = 22,
        Config = 23,
        HudParty = 24,
        HudQuest = 25,
    }

    public static class UIKeyTypeExtensions
    {
        /// <summary>enum 값을 UI Prefab 키 문자열로 변환한다.</summary>
        public static string ToKey(this UIKeyType type) => type switch
        {
            UIKeyType.InteractionKeyUI => "InteractionKeyUI",
            UIKeyType.InteractionHPBoard => "InteractionHPBoard",
            UIKeyType.PauseMenu => "PauseMenu",
            UIKeyType.Inventory => "Inventory",
            UIKeyType.Cursor => "Cursor",
            UIKeyType.ItemAcquisitionList => "ItemAcquisitionList",
            UIKeyType.GamePlay => "GamePlay",
            UIKeyType.ItemPopup => "ItemPopup",
            UIKeyType.HudPlayerInfo => "HudPlayerInfo",
            UIKeyType.ActorHpBar => "ActorHpBar",
            UIKeyType.TitleMenu => "TitleMenu",
            UIKeyType.MainDialogue => "MainDialogue",
            UIKeyType.SystemDialogue => "SystemDialogue",
            UIKeyType.MonologueDialogue => "MonologueDialogue",
            UIKeyType.DamageFloater => "DamageFloater",
            UIKeyType.Minimap => "Minimap",
            UIKeyType.Map => "Map",
            UIKeyType.RespawnPopup => "RespawnPopup",
            UIKeyType.Party => "Party",
            UIKeyType.Craft => "Craft",
            UIKeyType.Quest => "Quest",
            UIKeyType.MenuPanel => "MenuPanel",
            UIKeyType.Config => "Config",
            UIKeyType.HudParty => "HudParty",
            UIKeyType.HudQuest => "HudQuest",
            _ => string.Empty,
        };
    }
}
