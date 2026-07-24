using UnityEngine;
using UPlayGround.Ability.Core;

namespace UPlayGround.Data.Stat
{
    public static class StatDisplayFormatter
    {
        public static string GetDisplayName(AttributeId attributeId)
        {
            if (attributeId == AttributeIds.Vital.MaxHealth) return "최대 체력";
            if (attributeId == AttributeIds.Vital.HealthRegenRate) return "체력 회복";
            if (attributeId == AttributeIds.Combat.AttackPower) return "공격력";
            if (attributeId == AttributeIds.Combat.Defense) return "방어력";
            if (attributeId == AttributeIds.Combat.CritRate) return "치명타 확률";
            if (attributeId == AttributeIds.Combat.CritMultiplier) return "치명타 피해";
            if (attributeId == AttributeIds.Combat.AttackSpeed) return "공격 속도";
            if (attributeId == AttributeIds.Movement.MoveSpeed) return "이동 속도";
            if (attributeId == AttributeIds.Movement.DashDistance) return "대시 거리";
            if (attributeId == AttributeIds.Vital.MaxPoise) return "강인도";
            if (attributeId == AttributeIds.Vital.PoiseRecoveryRate) return "강인도 회복";
            if (attributeId == AttributeIds.Vital.PoiseRecoveryDelay) return "강인도 회복 대기";
            if (attributeId == AttributeIds.Resource.GenerationMultiplier) return "스킬 게이지";
            if (attributeId == AttributeIds.Combat.InvincibleDurationMultiplier) return "무적 시간";
            if (attributeId == AttributeIds.Life.GatheringPower) return "채집력";
            return attributeId.Value;
        }

        public static string FormatValue(AttributeId attributeId, float value)
        {
            if (attributeId == AttributeIds.Combat.Defense
                || attributeId == AttributeIds.Combat.CritRate
                || attributeId == AttributeIds.Combat.CritMultiplier)
                return $"{Mathf.Clamp01(value) * 100f:0.#}%";
            return $"{value:0.##}";
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
            if (attributeId == AttributeIds.Combat.Defense
                || attributeId == AttributeIds.Combat.CritRate
                || attributeId == AttributeIds.Combat.CritMultiplier)
                return $"{sign}{value * 100f:0.#}%";
            return $"{sign}{value:0.#}";
        }

        private static string FormatSignedPercent(float value)
        {
            string sign = value >= 0f ? "+" : string.Empty;
            return $"{sign}{value * 100f:0.#}%";
        }
    }
}
