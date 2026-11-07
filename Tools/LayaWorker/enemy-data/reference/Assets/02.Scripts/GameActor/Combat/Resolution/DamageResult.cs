using UPlayGround.UI;

namespace UPlayGround.Combat
{
    /// <summary>
    /// 전투 피해 계산 결과. HP 적용, UI 표시, 상태 전환은 호출자가 담당한다.
    /// </summary>
    public readonly struct DamageResult
    {
        public readonly float BaseDamage;
        public readonly float FinalDamage;
        public readonly float AttackerPower;
        public readonly float DefenseRate;
        public readonly float DamageTakenMultiplier;
        public readonly float CriticalMultiplier;
        public readonly bool IsCritical;
        public readonly FloatStyle FloaterStyle;

        public DamageResult(
            float baseDamage,
            float finalDamage,
            float attackerPower,
            float defenseRate,
            float damageTakenMultiplier,
            float criticalMultiplier,
            bool isCritical,
            FloatStyle floaterStyle)
        {
            BaseDamage = baseDamage;
            FinalDamage = finalDamage;
            AttackerPower = attackerPower;
            DefenseRate = defenseRate;
            DamageTakenMultiplier = damageTakenMultiplier;
            CriticalMultiplier = criticalMultiplier;
            IsCritical = isCritical;
            FloaterStyle = floaterStyle;
        }
    }
}
