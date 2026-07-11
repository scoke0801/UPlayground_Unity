using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UnityEngine;

namespace UPlayGround.Data.Actor
{
    /// <summary>
    /// 몬스터 전용 정적 프로필.
    /// ActorDefinitionSO의 공통 식별/프리팹/스탯과 분리해 몬스터 전용 데이터만 묶는다.
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterProfile_", menuName = "UPlayGround/액터/Monster Profile")]
    public sealed class MonsterActorProfileSO : ScriptableObject
    {
        [Header("레벨/등급")]
        public MonsterActorGrade grade = MonsterActorGrade.Normal;

        [Min(1)]
        public int level = 1;

        public MonsterScalingSO monsterScaling;

        [Header("강인도/브레이크")]
        public MonsterBreakGaugeSO breakGaugeData;

        [Header("전투/AI")]
        public EnemyAttackDataSO attackData;
        public CombatDefensePolicySO combatDefensePolicy;
        public CombatReactionPolicySO combatReactionPolicy;
        public EnemyBehaviorSO behaviorData;

        [Header("드랍/합류")]
        public EnemyDropTableSO dropTable;
        public CharacterActorType recruitableAs = CharacterActorType.None;

        [Header("성장 보상")]
        [Min(0)]
        public long expReward;

        [Min(0)]
        public int goldReward;
    }
}
