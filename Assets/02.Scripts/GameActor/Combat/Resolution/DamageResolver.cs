using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.UI;

namespace UPlayGround.Combat
{
    /// <summary>
    /// 전투 피해량 계산 전용 유틸리티. 결과 적용과 피드백 실행은 수행하지 않는다.
    /// </summary>
    public static class DamageResolver
    {
        public static DamageResult ResolvePlayerDamage(AttackData attackData, bool includeCritical = true)
        {
            float baseDamage = Mathf.Max(0f, attackData?.damage ?? 0f);
            float criticalMultiplier = includeCritical ? ResolveCriticalMultiplier(attackData) : 1f;
            float finalDamage = baseDamage * criticalMultiplier;

            return new DamageResult(
                baseDamage,
                finalDamage,
                attackerPower: 1f,
                defenseRate: 0f,
                damageTakenMultiplier: 1f,
                criticalMultiplier,
                criticalMultiplier > 1f,
                FloatStyle.PlayerDamage);
        }

        public static DamageResult ResolveMonsterDamage(
            MonsterActor target,
            AttackData attackData,
            MonsterBreakGauge breakGauge)
        {
            float baseDamage = Mathf.Max(0f, attackData?.damage ?? 0f);
            float attackerPower = attackData?.attacker != null
                ? attackData.attacker.Stats.AttackPower
                : 1f;
            float defenseRate = target != null
                ? Mathf.Clamp01(target.Stats.Defense)
                : 0f;
            float damageTakenMultiplier = breakGauge != null
                ? breakGauge.DamageTakenMultiplier
                : 1f;
            float criticalMultiplier = ResolveCriticalMultiplier(attackData);
            float finalDamage = baseDamage
                                * attackerPower
                                * (1f - defenseRate)
                                * damageTakenMultiplier
                                * criticalMultiplier;

            return new DamageResult(
                baseDamage,
                finalDamage,
                attackerPower,
                defenseRate,
                damageTakenMultiplier,
                criticalMultiplier,
                criticalMultiplier > 1f,
                criticalMultiplier > 1f ? FloatStyle.Critical : FloatStyle.Normal);
        }

        public static DamageResult ResolveSpecialBreakDamage(
            float maxHealth,
            float damageByMaxHpRate,
            float fixedDamage)
        {
            float rateDamage = Mathf.Max(0f, maxHealth) * Mathf.Max(0f, damageByMaxHpRate);
            float baseDamage = Mathf.Max(0f, fixedDamage) + rateDamage;

            return new DamageResult(
                baseDamage,
                baseDamage,
                attackerPower: 1f,
                defenseRate: 0f,
                damageTakenMultiplier: 1f,
                criticalMultiplier: 1f,
                isCritical: true,
                FloatStyle.Critical);
        }

        private static float ResolveCriticalMultiplier(AttackData attackData)
        {
            if (attackData == null || attackData.criticalMultiplier <= 1f)
                return 1f;

            return attackData.criticalMultiplier;
        }
    }
}
