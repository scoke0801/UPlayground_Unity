using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Combat
{
    /// <summary>등급별 처치 시간과 소탕 무리 목표를 함께 저작하는 기준.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/전투/Balance Targets")]
    public sealed class CombatBalanceTargetsSO : ScriptableObject
    {
        public const string ResourceKey = "CombatBalanceTargets";
        [Min(0.1f)] public float weakSeconds = 1.5f;
        [Min(0.1f)] public float normalSeconds = 4.5f;
        [Min(0.1f)] public float eliteSeconds = 45f;
        [Min(0.1f)] public float bossSeconds = 120f;
        public Vector2 sweepGroupSeconds = new(25f, 45f);

        /// <summary>시나리오에 별도 목표가 없을 때 적용할 등급 기준을 반환한다.</summary>
        public float GetTargetSeconds(MonsterActorGrade grade) => grade switch
        {
            MonsterActorGrade.Weak => weakSeconds,
            MonsterActorGrade.Elite => eliteSeconds,
            MonsterActorGrade.Boss => bossSeconds,
            _ => normalSeconds,
        };
    }
}
