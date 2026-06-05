// 자동 생성 파일입니다. 직접 수정하지 마세요.
// UPlayGround/ID Enum Generator 창에서 재생성하세요.
// Generated: 2026-06-05 14:29
namespace UPlayGround.Data.Crafting
{
    /// <summary>RecipeIdType — Recipe int ID 열거형 (자동 생성). 값 자체가 ID이므로 (int)type으로 변환한다.</summary>
    public enum RecipeIdType
    {
        None = 0,
        하급_생명_물약 = 1,
        중급_생명_물약 = 2,
        고급_생명_물약 = 3,
    }

    public static class RecipeIdTypeExtensions
    {
        /// <summary>enum 값을 Recipe int ID로 변환한다. (int)type과 동일.</summary>
        public static int ToRecipeId(this RecipeIdType type) => (int)type;
    }
}
