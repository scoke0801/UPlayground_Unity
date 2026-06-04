#if UNITY_EDITOR
namespace UPlayGround.Tool.Editor
{
    /// <summary>
    /// UPlayGround 에디터 메뉴 우선순위 구간.
    /// Unity 메뉴는 숫자가 낮을수록 위에 표시된다.
    /// </summary>
    public static class UPlaygroundMenuPriority
    {
        public const int Launcher = -100;

        public const int Generator = 10;

        public const int Character = 100;
        public const int CharacterData = 130;

        public const int BehaviorTree = 200;
        public const int BehaviorTreeJson = 220;

        public const int GameplayCombat = 300;
        public const int GameplayCombatTools = 330;
        public const int GameplayBalance = 360;
        public const int GameplayItem = 400;
        public const int GameplayCrafting = 430;
        public const int GameplayStat = 460;
        public const int GameplayQuest = 490;
        public const int GameplayTag = 520;

        public const int WorldMap = 600;
        public const int WorldCamera = 630;
        public const int WorldMinimap = 660;

        public const int NarrativeDialogue = 700;
        public const int NarrativeStory = 730;

        public const int Util = 900;
        public const int UtilValidation = 910;
        public const int UtilViewer = 930;
        public const int UtilConverter = 950;
    }
}
#endif
