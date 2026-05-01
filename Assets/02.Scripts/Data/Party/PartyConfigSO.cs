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
        [Header("Roster")]
        [Tooltip("게임 시작 시 보유한 캐릭터 전체 목록(=초기 Roster). 처치 보상으로 추가될 캐릭터는 런타임에 합류한다.")]
        public List<CharacterActorType> partyOrder = new();

        [Header("Battle Order")]
        [Tooltip("출전(BattleOrder) 슬롯 상한. 신규 합류 시 이 수보다 적게 차있으면 자동 편입된다.")]
        [Min(1)]
        public int maxBattleSize = 4;

        [Tooltip("게임 시작 시 출전 슬롯에 배치할 캐릭터. 비어있으면 partyOrder의 앞 maxBattleSize 명을 사용.")]
        public List<CharacterActorType> defaultBattleOrder = new();

        [Tooltip("게임 시작 시 조작할 캐릭터의 BattleOrder 인덱스 (0부터 시작)")]
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
