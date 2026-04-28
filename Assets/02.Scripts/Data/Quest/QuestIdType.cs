// 자동 생성 파일입니다. 직접 수정하지 마세요.
// UPlayGround/ID Enum Generator 창에서 재생성하세요.
// Generated: 2026-04-27 23:18
namespace UPlayGround.Data.Quest
{
    /// <summary>QuestIdType — Quest ID 키 열거형 (자동 생성)</summary>
    public enum QuestIdType
    {
        None = 0,
        main_001 = 1,
    }

    public static class QuestIdTypeExtensions
    {
        /// <summary>enum 값을 Quest ID 키 문자열로 변환한다.</summary>
        public static string ToQuestId(this QuestIdType type) => type switch
        {
            QuestIdType.main_001 => "main_001",
            _ => string.Empty,
        };
    }
}
