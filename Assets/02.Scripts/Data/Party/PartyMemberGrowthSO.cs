using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Party
{
    /// <summary>
    /// 장비 옵션과 구형 저장 데이터가 사용하는 안정 Attribute ID 카탈로그.
    /// </summary>
    public static class GrowthAttributeCatalog
    {
        public const string HealthId = "Vital.MaxHealth";
        public const string DefenseId = "Combat.Defense";
        public const string CriticalId = "Combat.CritRate";
        public const string AttackSpeedId = "Combat.AttackSpeed";
        public const string AttackPowerId = "Combat.AttackPower";
        public const string StaminaId = "Resource.MaxStamina";

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
        public static readonly AttributeId Stamina =
            new(StaminaId);

        public static readonly AttributeId[] LegacyOrderedIds =
        {
            Health,
            Defense,
            Critical,
            AttackSpeed,
            AttackPower,
        };

        public static readonly AttributeId[] DefaultEquipmentRollIds =
        {
            Health,
            Stamina,
            Defense,
            Critical,
            AttackSpeed,
            AttackPower,
        };

        public static bool TryGetEquipmentFlatValuePerRank(
            AttributeId attributeId,
            out float value)
        {
            if (attributeId == Health) value = 20f;
            else if (attributeId == Stamina) value = 5f;
            else if (attributeId == Defense) value = 0.02f;
            else if (attributeId == Critical) value = 0.01f;
            else if (attributeId == AttackSpeed) value = 0.03f;
            else if (attributeId == AttackPower) value = 0.05f;
            else
            {
                value = 0f;
                return false;
            }

            return true;
        }

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
                "Stamina" => Stamina,
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

    /// <summary>
    /// 파티 캐릭터 한 명의 기본 Attribute 프로필과 레벨 범위를 정의한다.
    /// 능력치와 스킬 성장은 CharacterSkillTreeSO가 단독으로 소유한다.
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
    }
}
