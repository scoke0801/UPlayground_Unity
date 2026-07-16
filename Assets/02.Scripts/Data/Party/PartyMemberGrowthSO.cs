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

    public enum GrowthAttributeType
    {
        Health,
        Defense,
        Critical,
        AttackSpeed,
        AttackPower
    }

    public enum GrowthUnlockType
    {
        Combo,
        Skill
    }

    public enum GrowthComboType
    {
        Light,
        Heavy
    }

    public enum GrowthSkillType
    {
        Ability,
        Ultimate
    }

    /// <summary>성장 데이터와 전투 코드가 공유하는 해금 식별자 규칙.</summary>
    public static class GrowthUnlockIds
    {
        public static string Combo(GrowthComboType comboType, int step)
            => $"Combo.{comboType}.{Mathf.Max(1, step)}";

        public static string Skill(GrowthSkillType skillType)
            => $"Skill.{skillType}";
    }

    [Serializable]
    public struct GrowthUnlockMilestone
    {
        [Min(1)] public int requiredRank;
        public GrowthUnlockType unlockType;
        [Tooltip("런타임 식별자. 예: Combo.Light.3, Combo.Heavy.2, Skill.Ability, Skill.Ultimate")]
        public string unlockId;
        public string displayName;
        [TextArea] public string description;
    }

    [Serializable]
    public struct GrowthInvestmentRule
    {
        public GrowthAttributeType attributeType;
        public StatType statType;
        [Min(1)] public int maxRank;
        [Tooltip("랭크 1당 기본 스탯에 더할 값. 체력 20, 방어 0.02, 크리티컬 0.01, 공속 0.03, 공격력 0.05 권장.")]
        public float flatPerRank;
        public List<GrowthUnlockMilestone> milestones;
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
    [CreateAssetMenu(fileName = "PartyMemberGrowth_", menuName = "UPlayGround/파티/Member Growth")]
    public class PartyMemberGrowthSO : ScriptableObject
    {
        public CharacterActorType characterType;
        public ActorStatSO baseStat;

        [Tooltip("레벨업 필요 경험치 곡선. null이면 PartyManager의 기본 폴백 곡선을 사용한다.")]
        public LevelCurveSO levelCurve;

        [Min(1)] public int initialLevel = 1;
        [Min(1)] public int levelCap = 100;

        [Header("휴식지점 선택 성장")]
        [Tooltip("켜면 기존 레벨별 growthRules도 함께 적용한다. 기본은 꺼서 실제 능력치는 포인트 투자로만 상승한다.")]
        public bool useAutomaticLevelGrowth;
        [Min(1)] public int growthPointsPerLevel = 1;
        public List<GrowthInvestmentRule> investmentRules = new();

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

        public bool TryGetInvestmentRule(GrowthAttributeType type, out GrowthInvestmentRule rule)
        {
            for (int i = 0; i < investmentRules.Count; i++)
            {
                if (investmentRules[i].attributeType != type) continue;
                rule = investmentRules[i];
                return true;
            }

            rule = GetDefaultInvestmentRule(type);
            return true;
        }

        public static GrowthInvestmentRule GetDefaultInvestmentRule(GrowthAttributeType type) => type switch
        {
            GrowthAttributeType.Health => new GrowthInvestmentRule { attributeType = type, statType = StatType.MaxHealth, maxRank = 20, flatPerRank = 20f, milestones = new List<GrowthUnlockMilestone>() },
            GrowthAttributeType.Defense => new GrowthInvestmentRule { attributeType = type, statType = StatType.Defense, maxRank = 20, flatPerRank = 0.02f, milestones = new List<GrowthUnlockMilestone>() },
            GrowthAttributeType.Critical => new GrowthInvestmentRule { attributeType = type, statType = StatType.CritRate, maxRank = 20, flatPerRank = 0.01f, milestones = new List<GrowthUnlockMilestone>() },
            GrowthAttributeType.AttackSpeed => new GrowthInvestmentRule { attributeType = type, statType = StatType.AttackSpeed, maxRank = 20, flatPerRank = 0.03f, milestones = new List<GrowthUnlockMilestone>() },
            _ => new GrowthInvestmentRule { attributeType = type, statType = StatType.AttackPower, maxRank = 20, flatPerRank = 0.05f, milestones = new List<GrowthUnlockMilestone>() },
        };
    }
}
