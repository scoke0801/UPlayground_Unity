using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;

namespace UPlayGround.Data.Party
{
    /// <summary>모델과 분리해 상주시키는 플레이어 캐릭터 게임플레이 정의.</summary>
    [CreateAssetMenu(
        fileName = "PlayerCharacterDefinition_",
        menuName = "UPlayGround/파티/Player Character Definition")]
    public sealed class PlayerCharacterDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public CharacterActorType characterType;

        [Header("Streaming")]
        [Tooltip("CharacterModelData 루트 프리팹의 Addressable 주소입니다.")]
        public string modelAddress;

        [Header("Equipment")]
        public WeaponType defaultWeaponType = WeaponType.NoWeapon;
        [Tooltip("캐릭터 최초 장비 레지스트리를 시딩할 아이템입니다.")]
        public List<EquipmentSO> startingEquipment = new();

        [Header("Combat")]
        [Tooltip("일반 공격, 스킬, 차지, 연계 라우트의 단일 소스입니다.")]
        public AbilitySetSO abilitySet;
        [Tooltip("Forte/Concerto 등 캐릭터별 Ability 자원 축적 규칙입니다.")]
        public AbilityResourceRuleSO abilityResourceRules;

        [Header("Character Weight")]
        public CharacterWeightProfileSO weightProfile;

        [Header("Entry Attack")]
        [Tooltip("0 이하이면 PartyConfigSO.defaultEntryAttackRange를 사용합니다.")]
        public float entryAttackRange;
        [Tooltip("벽 너머의 적을 등장 공격 대상으로 인정하지 않습니다.")]
        public bool requireEntryAttackLineOfSight;
    }
}
