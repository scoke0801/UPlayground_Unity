using System.Collections.Generic;
using UPlayGround.Ability.Core;

namespace UPlayGround.Data.Stat
{
    /// <summary>프로젝트 Attribute의 Profile 미지정 기본값.</summary>
    public static class UPlayGroundAttributeDefaults
    {
        /// <summary>
        /// Actor의 AttributeProfile에 반드시 직렬화하는 캐릭터 기본 스탯.
        /// 현재 자원(Vital.Health 등), Ability별 자원, Meta 실행값은 Registry에는 존재하지만
        /// Profile 원본값이 아니므로 포함하지 않는다.
        /// </summary>
        public static AttributeId[] ProfileAttributes => new AttributeId[]
        {
            Attributes.Vital.MaxHealth,
            Attributes.Vital.HealthRegenRate,
            Attributes.Combat.AttackPower,
            Attributes.Combat.Defense,
            Attributes.Combat.CritRate,
            Attributes.Combat.CritMultiplier,
            Attributes.Combat.AttackSpeed,
            Attributes.Movement.MoveSpeed,
            Attributes.Movement.DashDistance,
            Attributes.Vital.MaxPoise,
            Attributes.Vital.PoiseRecoveryRate,
            Attributes.Vital.PoiseRecoveryDelay,
            Attributes.Resource.GenerationMultiplier,
            Attributes.Combat.InvincibleDurationMultiplier,
            Attributes.Life.GatheringPower,
        };

        /// <summary>Registry에 등록된 런타임 Attribute 전체.</summary>
        public static AttributeId[] All
        {
            get
            {
                IReadOnlyList<AttributeRegistryEntry> definitions =
                    AttributeRegistry.Definitions;
                var result = new AttributeId[definitions.Count];
                for (int i = 0; i < definitions.Count; i++)
                    result[i] = new AttributeId(definitions[i].attributeId);
                return result;
            }
        }

        public static float Get(AttributeId attributeId)
        {
            return AttributeRegistry.TryGetDefinition(
                attributeId,
                out AttributeRegistryEntry definition)
                ? definition.defaultBaseValue
                : 0f;
        }
    }
}
