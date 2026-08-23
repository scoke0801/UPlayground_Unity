using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;

namespace UPlayGround.UI
{
    /// <summary>
    /// 파티 UI에서 캐릭터 무게 프로필을 조회·표기하기 위한 공용 헬퍼.
    /// 프로필의 원본은 모델과 분리된 PlayerCharacterDefinitionSO이다.
    /// </summary>
    public static class UIPartyWeightUtil
    {
        public static CharacterWeightProfileSO FindProfile(CharacterActorType type)
        {
            return UISvc.Party?.GetCharacterDefinition(type)?.weightProfile;
        }

        public static string ClassLabel(CharacterWeightClass weightClass) => weightClass switch
        {
            CharacterWeightClass.Light => "경량",
            CharacterWeightClass.Heavy => "중량",
            _ => "표준",
        };
    }
}
