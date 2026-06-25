using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.UI;

namespace UPlayGround.Combat
{
    /// <summary>
    /// 전투 피해량 계산 전용 유틸리티. 결과 적용과 피드백 실행은 수행하지 않는다.
    /// </summary>
    public static class DamageResolver
    {
        public static DamageResult ResolvePlayerDamage(PlayerActor target, in HitContext hit, bool includeCritical = true)
        {
            float baseDamage = Mathf.Max(0f, hit.Damage);
            float attackerPower = hit.Attacker != null
                ? hit.Attacker.Stats.AttackPower
                : 1f;
            float defenseRate = target != null
                ? Mathf.Clamp01(target.Stats.Defense)
                : 0f;
            float criticalMultiplier = includeCritical ? ResolveCriticalMultiplier(hit.CriticalMultiplier) : 1f;
            float finalDamage = baseDamage
                                * attackerPower
                                * (1f - defenseRate)
                                * criticalMultiplier;

            // 기본 피해가 있는 공격은 방어율이 높아도 최소 1은 들어가게 한다(칩 데미지 보장).
            if (baseDamage > 0f)
                finalDamage = Mathf.Max(1f, finalDamage);

            return new DamageResult(
                baseDamage,
                finalDamage,
                attackerPower,
                defenseRate,
                damageTakenMultiplier: 1f,
                criticalMultiplier,
                criticalMultiplier > 1f,
                FloatStyle.PlayerDamage);
        }

        public static DamageResult ResolveMonsterDamage(
            MonsterActor target,
            in HitContext hit,
            MonsterBreakGauge breakGauge)
        {
            float baseDamage = Mathf.Max(0f, hit.Damage);
            float attackerPower = hit.Attacker != null
                ? hit.Attacker.Stats.AttackPower
                : 1f;
            float defenseRate = target != null
                ? Mathf.Clamp01(target.Stats.Defense)
                : 0f;
            // 통합 취약 배율: 리액션 상태(Stun/Knockdown/Airborne/Grabbed) 배율과 Break 노출 배율을 단일 채널로 묶어
            // 동시 성립 시 더 큰 하나만 적용한다(max-wins).
            float breakExposedMultiplier = breakGauge != null ? breakGauge.DamageTakenMultiplier : 1f;
            float damageTakenMultiplier = CombatPolicyResolver.GetVulnerabilityMultiplier(
                target != null ? target.Definition?.combatReactionPolicy : null,
                target != null ? target.Grade : MonsterActorGrade.Normal,
                target != null ? target.CurrentReactionState : CombatReactionState.None,
                breakExposedMultiplier);
            float criticalMultiplier = ResolveCriticalMultiplier(hit.CriticalMultiplier);
            float finalDamage = baseDamage
                                * attackerPower
                                * (1f - defenseRate)
                                * damageTakenMultiplier
                                * criticalMultiplier;

            // 기본 피해가 있는 공격은 방어율이 높아도 최소 1은 들어가게 한다(칩 데미지 보장).
            if (baseDamage > 0f)
                finalDamage = Mathf.Max(1f, finalDamage);

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
            float fixedDamage,
            float minReferenceHealth)
        {
            // 최대 HP가 기준 HP보다 낮으면 기준 HP를 가진 것처럼 계산해 비율 피해의 하한을 보장한다.
            float effectiveHealth = Mathf.Max(Mathf.Max(0f, maxHealth), Mathf.Max(0f, minReferenceHealth));
            float rateDamage = effectiveHealth * Mathf.Max(0f, damageByMaxHpRate);
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

        private static float ResolveCriticalMultiplier(float multiplier)
            => multiplier > 1f ? multiplier : 1f;
    }
}
