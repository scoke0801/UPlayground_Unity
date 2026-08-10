// 자동 생성 파일입니다. 직접 수정하지 마세요.
// UPlayGround/ID Enum Generator 창에서 재생성하세요.
// Generated: 2026-08-09
namespace UPlayGround.Data.Crafting
{
    /// <summary>RecipeIdType — Recipe int ID 열거형 (자동 생성). 값 자체가 ID이므로 (int)type으로 변환한다.</summary>
    public enum RecipeIdType
    {
        None = 0,
        저급_회복물약 = 1,
        중급_회복물약 = 2,
        고급_회복물약 = 3,
        특수_회복물약 = 4,
        완전_회복물약 = 5,
        시련_강화_모자_I = 10,
        시련_강화_상의_I = 11,
        시련_강화_하의_I = 12,
        시련_강화_장갑_I = 13,
        시련_강화_신발_I = 14,
        시련_강화_모자_II = 20,
        시련_강화_상의_II = 21,
        시련_강화_하의_II = 22,
        시련_강화_장갑_II = 23,
        시련_강화_신발_II = 24,
        시련_강화_모자_III = 30,
        시련_강화_상의_III = 31,
        시련_강화_하의_III = 32,
        시련_강화_장갑_III = 33,
        시련_강화_신발_III = 34,
    }

    public static class RecipeIdTypeExtensions
    {
        /// <summary>enum 값을 Recipe int ID로 변환한다. (int)type과 동일.</summary>
        public static int ToRecipeId(this RecipeIdType type) => (int)type;
    }
}
