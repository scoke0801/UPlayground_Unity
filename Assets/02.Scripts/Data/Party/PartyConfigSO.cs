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

        [Header("Entry Attack Defaults")]
        [Tooltip("CharacterModelData.entryAttackRange 가 0 이하일 때 사용할 기본 검출 반경.")]
        [Min(0f)]
        public float defaultEntryAttackRange = 6f;

        [Tooltip("등장 공격의 적 검출 레이어. 락온/공격 레이어와 동일 권장.")]
        public LayerMask entryAttackTargetLayer = ~0;

        [Tooltip("LOS 검사 시 시야를 가로막는 레이어 (지형 등). requireLineOfSight=true 인 캐릭터에만 사용.")]
        public LayerMask entryAttackLineOfSightBlocker = 0;
    }
}
