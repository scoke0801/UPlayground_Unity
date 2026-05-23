// 자동 생성 파일입니다. 직접 수정하지 마세요.
// UPlayGround/ID Enum Generator 창에서 재생성하세요.
// Generated: 2026-05-23 00:22
namespace UPlayGround.Data.Quest
{
    /// <summary>QuestIdType — Quest ID 키 열거형 (자동 생성)</summary>
    public enum QuestIdType
    {
        None = 0,
        TestQuest_001 = 1,
        main_001 = 2,
        quest_main_001 = 3,
        quest_main_002 = 4,
        quest_main_003 = 5,
        quest_main_004 = 6,
        quest_main_005 = 7,
        quest_sub_guide_broken_lantern = 8,
        quest_sub_herbalist_lake_herb = 9,
        quest_sub_highland_golem_trace = 10,
        quest_sub_hunter_skeleton_patrol = 11,
        quest_sub_hunter_spider_web = 12,
        quest_sub_survivor_lost_pack = 13,
    }

    public static class QuestIdTypeExtensions
    {
        /// <summary>enum 값을 Quest ID 키 문자열로 변환한다.</summary>
        public static string ToQuestId(this QuestIdType type) => type switch
        {
            QuestIdType.TestQuest_001 => "TestQuest_001",
            QuestIdType.main_001 => "main_001",
            QuestIdType.quest_main_001 => "quest_main_001",
            QuestIdType.quest_main_002 => "quest_main_002",
            QuestIdType.quest_main_003 => "quest_main_003",
            QuestIdType.quest_main_004 => "quest_main_004",
            QuestIdType.quest_main_005 => "quest_main_005",
            QuestIdType.quest_sub_guide_broken_lantern => "quest_sub_guide_broken_lantern",
            QuestIdType.quest_sub_herbalist_lake_herb => "quest_sub_herbalist_lake_herb",
            QuestIdType.quest_sub_highland_golem_trace => "quest_sub_highland_golem_trace",
            QuestIdType.quest_sub_hunter_skeleton_patrol => "quest_sub_hunter_skeleton_patrol",
            QuestIdType.quest_sub_hunter_spider_web => "quest_sub_hunter_spider_web",
            QuestIdType.quest_sub_survivor_lost_pack => "quest_sub_survivor_lost_pack",
            _ => string.Empty,
        };
    }
}
