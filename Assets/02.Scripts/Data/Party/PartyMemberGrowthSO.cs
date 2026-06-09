using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Party
{
    public enum GrowthFormula
    {
        Flat,
        Percent,
        Curve
    }

    [Serializable]
    public struct StatGrowthRule
    {
        public StatType statType;
        public GrowthFormula formula;
        public float flatPerLevel;
        public float percentPerLevel;
        public AnimationCurve curve;
    }

    /// <summary>
    /// 파티 캐릭터 한 명의 레벨 성장 규칙.
    /// baseStat은 레벨 1 기준값이며, growthRules가 레벨에 따른 증가량을 정의한다.
    /// </summary>
    [CreateAssetMenu(fileName = "PartyMemberGrowth_", menuName = "UPlayGround/Party/Party Member Growth")]
    public class PartyMemberGrowthSO : ScriptableObject
    {
        public CharacterActorType characterType;
        public ActorStatSO baseStat;

        [Tooltip("레벨업 필요 경험치 곡선. null이면 PartyManager의 기본 폴백 곡선을 사용한다.")]
        public LevelCurveSO levelCurve;

        [Min(1)] public int initialLevel = 1;
        [Min(1)] public int levelCap = 100;

        public List<StatGrowthRule> growthRules = new();

        public bool TryGetRule(StatType type, out StatGrowthRule rule)
        {
            for (int i = 0; i < growthRules.Count; i++)
            {
                if (growthRules[i].statType != type) continue;
                rule = growthRules[i];
                return true;
            }

            rule = default;
            return false;
        }
    }
}
