using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Party
{
    /// <summary>
    /// 파티 구성 정보를 정의하는 ScriptableObject.
    /// Resources/Data/PartyConfig.asset 에 배치해 PartyManager가 로드한다.
    /// </summary>
    [CreateAssetMenu(fileName = "PartyConfig", menuName = "UPlayGround/Party/Party Config")]
    public class PartyConfigSO : ScriptableObject
    {
        [Tooltip("파티 슬롯 순서. 씬에 해당 CharacterType의 PlayerActor가 있으면 자동으로 포함된다.")]
        public List<CharacterActorType> partyOrder = new();

        [Tooltip("게임 시작 시 조작할 캐릭터의 슬롯 인덱스 (0부터 시작)")]
        [Min(0)]
        public int startActiveIndex = 0;
    }
}
