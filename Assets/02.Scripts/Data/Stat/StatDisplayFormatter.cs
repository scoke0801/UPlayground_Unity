using UnityEngine;

namespace UPlayGround.Data.Stat
{
    public static class StatDisplayFormatter
    {
        public static string GetDisplayName(StatType type)
        {
            return type switch
            {
                StatType.MaxHealth => "최대 체력",
                StatType.HealthRegenRate => "체력 회복",
                StatType.AttackPower => "공격력",
                StatType.Defense => "방어력",
                StatType.CritRate => "치명타 확률",
                StatType.CritMultiplier => "치명타 피해",
                StatType.MoveSpeed => "이동 속도",
                StatType.DashDistance => "대시 거리",
                StatType.MaxPoise => "강인도",
                StatType.PoiseRecoveryRate => "강인도 회복",
                StatType.PoiseRecoveryDelay => "강인도 회복 대기",
                StatType.SkillGaugeRate => "스킬 게이지",
                StatType.InvincibleDuration => "무적 시간",
                _ => type.ToString()
            };
        }

        public static string FormatValue(StatType type, float value)
        {
            return type switch
            {
                StatType.Defense or StatType.CritRate => $"{Mathf.Clamp01(value) * 100f:0.#}%",
                StatType.CritMultiplier => $"{value * 100f:0.#}%",
                StatType.AttackPower or StatType.MoveSpeed or StatType.DashDistance or
                    StatType.SkillGaugeRate or StatType.InvincibleDuration => $"{value:0.##}",
                _ => $"{value:0.#}"
            };
        }

        public static string FormatModifier(StatModifier modifier)
        {
            string name = GetDisplayName(modifier.statType);
            float value = modifier.value;

            return modifier.modifierType switch
            {
                ModifierType.Flat => $"{name} {FormatSignedFlat(modifier.statType, value)}",
                ModifierType.Percent => $"{name} {FormatSignedPercent(value)}",
                ModifierType.Multiply => $"{name} x{value:0.##}",
                _ => $"{name} {value:0.##}"
            };
        }

        private static string FormatSignedFlat(StatType type, float value)
        {
            string sign = value >= 0f ? "+" : string.Empty;
            return type switch
            {
                StatType.Defense or StatType.CritRate or StatType.CritMultiplier =>
                    $"{sign}{value * 100f:0.#}%",
                _ => $"{sign}{value:0.#}"
            };
        }

        private static string FormatSignedPercent(float value)
        {
            string sign = value >= 0f ? "+" : string.Empty;
            return $"{sign}{value * 100f:0.#}%";
        }
    }
}
