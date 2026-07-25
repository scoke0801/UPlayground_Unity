using System.Collections.Generic;
using UPlayGround.Ability.Core;

namespace UPlayGround.Data.Stat
{
    /// <summary>프로젝트 Attribute의 Profile 미지정 기본값.</summary>
    public static class UPlayGroundAttributeDefaults
    {
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
