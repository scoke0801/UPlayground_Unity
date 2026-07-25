using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Party
{
    public enum GrowthFormula
    {
        Flat,
        Percent,
        Curve
    }

    /// <summary>
    /// 기존 enum 기반 성장 데이터를 안정 Attribute ID로 변환하는 호환·표시 도우미.
    /// 신규 성장 후보의 단일 원본은 각 PartyMemberGrowthSO.investmentRules다.
    /// </summary>
    public static class GrowthAttributeCatalog
    {
        public const string HealthId = "Vital.MaxHealth";
        public const string DefenseId = "Combat.Defense";
        public const string CriticalId = "Combat.CritRate";
        public const string AttackSpeedId = "Combat.AttackSpeed";
        public const string AttackPowerId = "Combat.AttackPower";

        public static readonly AttributeId Health =
            new(HealthId);
        public static readonly AttributeId Defense =
            new(DefenseId);
        public static readonly AttributeId Critical =
            new(CriticalId);
        public static readonly AttributeId AttackSpeed =
            new(AttackSpeedId);
        public static readonly AttributeId AttackPower =
            new(AttackPowerId);

        public static readonly AttributeId[] LegacyOrderedIds =
        {
            Health,
            Defense,
            Critical,
            AttackSpeed,
            AttackPower,
        };

        public static readonly string[] LegacyOrderedIdValues =
        {
            HealthId,
            DefenseId,
            CriticalId,
            AttackSpeedId,
            AttackPowerId,
        };

        public static bool TryResolveLegacy(
            string value,
            out AttributeId attributeId)
        {
            if (global::UPlayGround.Data.Stat.AttributeRegistry.TryResolve(
                    value,
                    out global::UPlayGround.Data.Stat.AttributeReference reference))
            {
                attributeId = reference.ToCoreId();
                return true;
            }

            attributeId = value switch
            {
                "Health" => Health,
                "Defense" => Defense,
                "Critical" => Critical,
                "AttackSpeed" => AttackSpeed,
                "AttackPower" => AttackPower,
                _ => default,
            };
            return attributeId.IsValid;
        }

        public static bool TryResolveLegacy(
            int legacyValue,
            out AttributeId attributeId)
        {
            if (legacyValue >= 0
                && legacyValue < LegacyOrderedIds.Length)
            {
                attributeId = LegacyOrderedIds[legacyValue];
                return true;
            }

            attributeId = default;
            return false;
        }

        public static string GetDisplayName(AttributeId attributeId)
        {
            return global::UPlayGround.Data.Stat.AttributeRegistry
                .TryGetDefinition(
                    attributeId,
                    out global::UPlayGround.Data.Stat.AttributeRegistryEntry entry)
                && !string.IsNullOrWhiteSpace(entry.displayName)
                    ? entry.displayName
                    : attributeId.Value;
        }

        public static string GetDisplayName(string attributeId) =>
            GetDisplayName(new AttributeId(attributeId));
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
        Ultimate,
        ElementalImbue
    }

    /// <summary>성장 데이터와 전투 코드가 공유하는 해금 식별자 규칙.</summary>
    public static class GrowthUnlockIds
    {
        public const string RoutePrefix = "Route.";

        public static string Combo(GrowthComboType comboType, int step)
            => $"Combo.{comboType}.{Mathf.Max(1, step)}";

        public static string Skill(GrowthSkillType skillType)
            => $"Skill.{skillType}";

        /// <summary>약+강 조합(ComboRoute) 개별 해금 식별자. routeId를 그대로 감싼다.</summary>
        public static string Route(string routeId)
            => RoutePrefix + (string.IsNullOrEmpty(routeId) ? string.Empty : routeId);
    }

    [Serializable]
    public struct GrowthUnlockMilestone
    {
        [Min(1)] public int requiredRank;
        public GrowthUnlockType unlockType;
        [Tooltip("런타임 식별자. 예: Combo.Light.3, Combo.Heavy.2, Skill.Ability, Skill.Ultimate, Skill.ElementalImbue")]
        public string unlockId;
        public string displayName;
        [TextArea] public string description;
    }

    [Serializable]
    public struct GrowthInvestmentRule
    {
        [Tooltip("런타임에서 사용하는 안정 Attribute ID")]
        [AttributeIdSelector]
        public string attributeId;
        [Min(1)] public int maxRank;
        [Tooltip("랭크 1당 기본 스탯에 더할 값. 체력 20, 방어 0.02, 크리티컬 0.01, 공속 0.03, 공격력 0.05 권장.")]
        public float flatPerRank;
        public List<GrowthUnlockMilestone> milestones;

        public AttributeId AttributeId => new(attributeId);
    }

    [Serializable]
    public struct StatGrowthRule
    {
        [Tooltip("성장 대상 안정 Attribute ID")]
        [AttributeIdSelector]
        public string attributeId;
        public GrowthFormula formula;
        public float flatPerLevel;
        public float percentPerLevel;
        public AnimationCurve curve;

        public AttributeId AttributeId => new(attributeId);
    }

    /// <summary>
    /// 파티 캐릭터 한 명의 레벨 성장 규칙.
    /// baseProfile은 레벨 1 기준값이며, growthRules가 레벨에 따른 증가량을 정의한다.
    /// </summary>
    [CreateAssetMenu(fileName = "PartyMemberGrowth_", menuName = "UPlayGround/파티/Member Growth")]
    public class PartyMemberGrowthSO : ScriptableObject
    {
        public CharacterActorType characterType;
        public AttributeProfileSO baseProfile;

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

        public bool TryGetRule(AttributeId attributeId, out StatGrowthRule rule)
        {
            for (int i = 0; i < growthRules.Count; i++)
            {
                if (growthRules[i].AttributeId != attributeId) continue;
                rule = growthRules[i];
                return true;
            }

            rule = default;
            return false;
        }

        public bool TryGetInvestmentRule(
            AttributeId attributeId,
            out GrowthInvestmentRule rule)
        {
            for (int i = 0; i < investmentRules.Count; i++)
            {
                if (investmentRules[i].AttributeId != attributeId) continue;
                rule = investmentRules[i];
                return true;
            }

            rule = default;
            return false;
        }

        public static GrowthInvestmentRule GetDefaultInvestmentRule(
            AttributeId attributeId)
        {
            float flatPerRank =
                attributeId == GrowthAttributeCatalog.Health ? 20f :
                attributeId == GrowthAttributeCatalog.Defense ? 0.02f :
                attributeId == GrowthAttributeCatalog.Critical ? 0.01f :
                attributeId == GrowthAttributeCatalog.AttackSpeed ? 0.03f :
                0.05f;
            return new GrowthInvestmentRule
            {
                attributeId = attributeId.Value,
                maxRank = 20,
                flatPerRank = flatPerRank,
                milestones = new List<GrowthUnlockMilestone>(),
            };
        }

        public static GrowthInvestmentRule GetDefaultInvestmentRule(
            string attributeId) =>
            GetDefaultInvestmentRule(new AttributeId(attributeId));
    }
}
