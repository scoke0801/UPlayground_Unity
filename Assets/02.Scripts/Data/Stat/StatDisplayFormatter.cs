using UnityEngine;
using UPlayGround.Ability.Core;

namespace UPlayGround.Data.Stat
{
    public static class StatDisplayFormatter
    {
        public static string GetDisplayName(AttributeId attributeId)
        {
            return AttributeRegistry.TryGetDefinition(
                    attributeId,
                    out AttributeRegistryEntry definition)
                && !string.IsNullOrWhiteSpace(definition.displayName)
                    ? definition.displayName
                    : attributeId.Value;
        }

        public static string FormatValue(AttributeId attributeId, float value)
        {
            if (!AttributeRegistry.TryGetDefinition(
                    attributeId,
                    out AttributeRegistryEntry definition))
                return $"{value:0.##}";
            return definition.format == AttributeValueFormat.Percent01
                ? $"{Mathf.Clamp01(value) * 100f:0.#}%"
                : $"{value:0.##}{definition.unit}";
        }

        public static string FormatModifier(
            AttributeId attributeId,
            AttributeModifierOperation operation,
            float value)
        {
            string name = GetDisplayName(attributeId);
            return operation switch
            {
                AttributeModifierOperation.Add =>
                    $"{name} {FormatSignedFlat(attributeId, value)}",
                AttributeModifierOperation.Percent =>
                    $"{name} {FormatSignedPercent(value)}",
                AttributeModifierOperation.Multiply =>
                    $"{name} x{value:0.##}",
                AttributeModifierOperation.Override =>
                    $"{name} = {FormatValue(attributeId, value)}",
                _ => $"{name} {value:0.##}",
            };
        }

        private static string FormatSignedFlat(
            AttributeId attributeId,
            float value)
        {
            string sign = value >= 0f ? "+" : string.Empty;
            if (AttributeRegistry.TryGetDefinition(
                    attributeId,
                    out AttributeRegistryEntry definition)
                && definition.format == AttributeValueFormat.Percent01)
                return $"{sign}{value * 100f:0.#}%";
            string unit = definition?.unit ?? string.Empty;
            return $"{sign}{value:0.#}{unit}";
        }

        private static string FormatSignedPercent(float value)
        {
            string sign = value >= 0f ? "+" : string.Empty;
            return $"{sign}{value * 100f:0.#}%";
        }
    }
}
