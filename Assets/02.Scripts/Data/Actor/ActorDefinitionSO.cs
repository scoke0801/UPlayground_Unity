using UnityEngine;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Enemy;
using UPlayGround.Ability.Core;

namespace UPlayGround.Data.Actor
{
    /// <summary>
    /// 하나의 Actor 종류를 정의하는 ScriptableObject.
    /// ActorID를 키로 ActorDatabase에 등록해 런타임 스폰 및 스탯 조회에 활용한다.
    /// </summary>
    [CreateAssetMenu(fileName = "ActorDef_", menuName = "UPlayGround/액터/Definition")]
    public class ActorDefinitionSO : ScriptableObject
    {
        [Tooltip("런타임에서 사용하는 고유 문자열 ID (중복 불가)")]
        public string actorId = "";

        [Tooltip("에디터/UI에 표시할 이름")]
        public string displayName = "";

        [TextArea(2, 4)]
        public string description = "";

        public ActorType actorType = ActorType.Monster;
        public CharacterActorType characterType = CharacterActorType.None;
        [Tooltip("전투 진영. 비워두면 ActorType 기반 기본 진영을 사용한다.")]
        public CombatFactionSO combatFaction;
        [Tooltip("이 ActorID가 공격 판정을 켤 때 대상으로 삼을 레이어. 비워두면 ActorType 기본 규칙을 사용한다.")]
        public LayerMask targetLayerMask = 0;

        [Tooltip("런타임 스폰에 사용할 프리팹. GameActor 컴포넌트를 포함해야 함.")]
        public GameObject prefab;

        [Tooltip("GAS Attribute 기본값 Profile.")]
        public AttributeProfileSO attributeProfile;

        [Tooltip("Poise 데이터. null이면 프리팹에 설정된 값 사용.")]
        public PoiseSO poiseData;

        [Tooltip("몬스터 전용 정적 프로필. Monster 타입이 아니면 비워둔다.")]
        public MonsterActorProfileSO monsterProfile;

        [Tooltip("몬스터 브레이크 게이지 데이터. null이면 프리팹에 설정된 값 사용.")]
        public MonsterBreakGaugeSO breakGaugeData;

        [Tooltip("몬스터 레벨/등급 성장 기준. 몬스터 statData 재생성 시 우선 사용한다.")]
        public MonsterScalingSO monsterScaling;

        [Tooltip("몬스터 등급. 킬캠/브레이크 게이지/일부 전투 규칙에서 사용.")]
        public MonsterActorGrade grade = MonsterActorGrade.Normal;

        [Min(1)]
        [Tooltip("생성/밸런싱 기준 레벨. 공격 데이터 레벨 스케일링 등에 사용.")]
        public int level = 1;

        [Tooltip("이 액터의 기본 전투 속성. GameplayEffect가 적용되면 런타임 동안 덮어쓸 수 있습니다.")]
        public CombatElement combatElement = CombatElement.None;

        [Tooltip("고정 속성 또는 새 게임마다 actorId 기반으로 다시 추첨할지 결정합니다.")]
        public CombatElementAssignmentMode elementAssignmentMode =
            CombatElementAssignmentMode.Fixed;

        [Min(1f)]
        [Tooltip("유리한 속성으로 공격할 때 적용할 피해 배율.")]
        public float elementalAdvantageMultiplier =
            CombatElementRules.DefaultAdvantageMultiplier;

        [Tooltip("액터에게 부여할 공용 AbilitySet. 몬스터 프로필에 값이 있으면 프로필 값이 우선합니다.")]
        public AbilitySetSO abilitySet;

        [Tooltip("방어 판정 정책. null이면 기존 기본 방어 규칙을 사용한다.")]
        public CombatDefensePolicySO combatDefensePolicy;

        [Tooltip("피격 리액션 정책. null이면 기존 기본 리액션 규칙을 사용한다.")]
        public CombatReactionPolicySO combatReactionPolicy;

        [Tooltip("적 행동(AI) 프로필. null이면 프리팹의 EnemyAIController에 설정된 값 사용.")]
        public EnemyBehaviorSO behaviorData;

        [Tooltip("NpcActor에 주입할 NPC 전용 대화/상호작용 데이터. NPC가 아니면 비워둔다.")]
        public NpcActorSO npcData;

        [Tooltip("사망 시 드랍 테이블. null이면 프리팹에 설정된 값 사용.")]
        public EnemyDropTableSO dropTable;

        [Tooltip("처치 시 파티에 합류시킬 캐릭터 타입. None이면 합류 없음.")]
        public CharacterActorType recruitableAs = CharacterActorType.None;

        [Min(0)]
        [Tooltip("처치 시 출전 파티 전원에게 지급할 경험치. 0이면 지급 없음.")]
        public long expReward = 0;

        [Min(0)]
        [Tooltip("처치 시 지급할 골드. 0이면 지급 없음. 재스폰 레벨 스케일링 시 경험치와 같은 공식으로 증가한다.")]
        public int goldReward = 0;

        public MonsterBreakGaugeSO EffectiveBreakGaugeData => monsterProfile != null ? monsterProfile.breakGaugeData : breakGaugeData;
        public MonsterScalingSO EffectiveMonsterScaling => monsterProfile != null ? monsterProfile.monsterScaling : monsterScaling;
        public MonsterActorGrade EffectiveGrade => monsterProfile != null ? monsterProfile.grade : grade;
        public int EffectiveLevel => monsterProfile != null ? Mathf.Max(1, monsterProfile.level) : Mathf.Max(1, level);
        public AbilitySetSO EffectiveAbilitySet
        {
            get
            {
                AbilitySetSO shared = monsterProfile?.abilitySet;
                if (shared == null)
                    return abilitySet;
                if (abilitySet == null)
                    return shared;
                // 기존 데이터는 Profile Set 우선 계약을 보존한다.
                // Definition Set이 명시적으로 Profile Set에서 파생된 경우에만
                // 특수 몬스터용 합성 Set으로 사용한다.
                return ReferenceEquals(abilitySet, shared)
                       || abilitySet.IsDerivedFrom(shared)
                    ? abilitySet
                    : shared;
            }
        }
        public CombatDefensePolicySO EffectiveCombatDefensePolicy => monsterProfile != null ? monsterProfile.combatDefensePolicy : combatDefensePolicy;
        public CombatReactionPolicySO EffectiveCombatReactionPolicy => monsterProfile != null ? monsterProfile.combatReactionPolicy : combatReactionPolicy;
        public EnemyBehaviorSO EffectiveBehaviorData => monsterProfile != null ? monsterProfile.behaviorData : behaviorData;
        public EnemyDropTableSO EffectiveDropTable => monsterProfile != null ? monsterProfile.dropTable : dropTable;
        public CharacterActorType EffectiveRecruitableAs => monsterProfile != null ? monsterProfile.recruitableAs : recruitableAs;
        public long EffectiveExpReward => monsterProfile != null ? System.Math.Max(0, monsterProfile.expReward) : System.Math.Max(0, expReward);
        public int EffectiveGoldReward => monsterProfile != null ? Mathf.Max(0, monsterProfile.goldReward) : goldReward;

        public CombatElement ResolveCombatElement(int newGameSeed) =>
            elementAssignmentMode == CombatElementAssignmentMode.RandomPerNewGame
                ? CombatElementRules.ResolveRandomElement(newGameSeed, actorId)
                : combatElement;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(actorId))
                actorId = name;
        }
#endif
    }
}
