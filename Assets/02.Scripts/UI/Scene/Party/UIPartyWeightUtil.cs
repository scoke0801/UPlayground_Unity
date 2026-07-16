using UPlayGround.Components;
using UPlayGround.Data.Cycle;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 파티 UI에서 캐릭터 무게 프로필(사이클 03 스펙)을 조회·표기하기 위한 공용 헬퍼.
    /// 프로필의 원본은 Player 프리팹 하위 CharacterModelData.weightProfile이다.
    /// </summary>
    public static class UIPartyWeightUtil
    {
        public static CharacterWeightProfileSO FindProfile(CharacterActorType type)
        {
            PlayerActor player = GameObjectManager.Instance?.Player;
            PlayerSwapBehaviour swap = player != null ? player.GetComponent<PlayerSwapBehaviour>() : null;
            CharacterModelData model = swap != null ? swap.GetModelData(type) : null;
            return model != null ? model.weightProfile : null;
        }

        public static string ClassLabel(CharacterWeightClass weightClass) => weightClass switch
        {
            CharacterWeightClass.Light => "경량",
            CharacterWeightClass.Heavy => "중량",
            _ => "표준",
        };
    }
}
