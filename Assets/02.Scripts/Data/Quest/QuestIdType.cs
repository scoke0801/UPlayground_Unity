// 자동 생성 파일입니다. 직접 수정하지 마세요.
// UPlayGround/ID Enum Generator 창에서 재생성하세요.
// Generated: 2026-06-05 14:29
namespace UPlayGround.Data.Quest
{
    /// <summary>QuestIdType — Quest ID 키 열거형 (자동 생성)</summary>
    public enum QuestIdType
    {
        None = 0,
        TestQuest_001 = 1,
        main_001 = 2,
        main_002 = 3,
        quest_sub_guide_broken_lantern = 9,
        quest_sub_herbalist_lake_herb = 10,
        quest_sub_highland_golem_trace = 11,
        quest_sub_hunter_skeleton_patrol = 12,
        quest_sub_hunter_spider_web = 13,
        quest_sub_survivor_lost_pack = 14,
    }

    public static class QuestIdTypeExtensions
    {
        /// <summary>enum 값을 Quest ID 키 문자열로 변환한다.</summary>
        public static string ToQuestId(this QuestIdType type) => type switch
        {
            QuestIdType.TestQuest_001 => "TestQuest_001",
            QuestIdType.main_001 => "main_001",
            QuestIdType.main_002 => "main_002",
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
