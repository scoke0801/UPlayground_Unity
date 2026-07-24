using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;
using UPlayGround.Manager;
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
                ? GetAttribute(hit.Attacker, AttributeIds.Combat.AttackPower, 1f)
                : 1f;
            float defenseRate = target != null
                ? Mathf.Clamp01(GetAttribute(target, AttributeIds.Combat.Defense, 0f))
                : 0f;
            float criticalMultiplier = includeCritical ? ResolveCriticalMultiplier(hit.CriticalMultiplier) : 1f;
            float elementMultiplier = ResolveElementMultiplier(hit.Attacker, target);
            float damageTakenMultiplier = hit.Attacker is MonsterActor monsterAttacker
                ? Svc.MonsterCodexReader?.GetDamageTakenMultiplier(monsterAttacker.ActorId) ?? 1f
                : 1f;
            float finalDamage = DamageExecution.Calculate(
                baseDamage,
                attackerPower,
                defenseRate,
                damageTakenMultiplier,
                elementMultiplier,
                criticalMultiplier);

            return new DamageResult(
                baseDamage,
                finalDamage,
                attackerPower,
                defenseRate,
                damageTakenMultiplier,
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
                ? GetAttribute(hit.Attacker, AttributeIds.Combat.AttackPower, 1f)
                : 1f;
            float defenseRate = target != null
                ? Mathf.Clamp01(GetAttribute(target, AttributeIds.Combat.Defense, 0f))
                : 0f;
            // 통합 취약 배율: 리액션 상태(Stun/Knockdown/Airborne/Grabbed) 배율과 Break 노출 배율을 단일 채널로 묶어
            // 동시 성립 시 더 큰 하나만 적용한다(max-wins).
            float breakExposedMultiplier = breakGauge != null ? breakGauge.DamageTakenMultiplier : 1f;
            float damageTakenMultiplier = CombatPolicyResolver.GetVulnerabilityMultiplier(
                target != null ? target.Definition?.combatReactionPolicy : null,
                target != null ? target.Grade : MonsterActorGrade.Normal,
                target != null ? target.CurrentReactionState : CombatReactionState.None,
                breakExposedMultiplier);
            if (target != null && hit.Attacker is PlayerActor)
            {
                damageTakenMultiplier *=
                    Svc.MonsterCodexReader?.GetDamageDealtMultiplier(target.ActorId) ?? 1f;
            }
            float criticalMultiplier = ResolveCriticalMultiplier(hit.CriticalMultiplier);
            float elementMultiplier = ResolveElementMultiplier(hit.Attacker, target);
            float finalDamage = DamageExecution.Calculate(
                baseDamage,
                attackerPower,
                defenseRate,
                damageTakenMultiplier,
                elementMultiplier,
                criticalMultiplier);

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

        public static DamageResult ResolveSpecialBreakDamage(
            MonsterActor target,
            float damageByMaxHpRate,
            float fixedDamage,
            float minReferenceHealth)
        {
            DamageResult baseResult = ResolveSpecialBreakDamage(
                target != null ? target.MaxHealth : 0f,
                damageByMaxHpRate,
                fixedDamage,
                minReferenceHealth);
            float codexMultiplier = target != null
                ? Svc.MonsterCodexReader?.GetDamageDealtMultiplier(target.ActorId) ?? 1f
                : 1f;

            return new DamageResult(
                baseResult.BaseDamage,
                baseResult.FinalDamage * codexMultiplier,
                baseResult.AttackerPower,
                baseResult.DefenseRate,
                codexMultiplier,
                baseResult.CriticalMultiplier,
                baseResult.IsCritical,
                baseResult.FloaterStyle);
        }

        private static float ResolveCriticalMultiplier(float multiplier)
            => multiplier > 1f ? multiplier : 1f;

        private static float GetAttribute(
            GameActor actor,
            AttributeId attributeId,
            float fallback)
        {
            return actor?.AbilitySystem != null
                   && actor.AbilitySystem.TryGetAttribute(
                       attributeId, current: true, out float value)
                ? value
                : fallback;
        }

        private static float ResolveElementMultiplier(
            GameActor attacker,
            GameActor victim)
        {
            if (attacker == null || victim == null)
                return 1f;

            return CombatElementRules.ResolveDamageMultiplier(
                attacker.CurrentElement,
                victim.CurrentElement,
                attacker.ElementalAdvantageMultiplier);
        }
    }
}
