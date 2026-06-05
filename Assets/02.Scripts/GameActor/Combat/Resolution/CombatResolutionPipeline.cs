using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UnityEngine;

namespace UPlayGround.Combat
{
    /// <summary>
    /// 방어, 피해, 리소스 변화, 로그 기록의 표준 순서를 묶는 전환 단계 pipeline.
    /// P2에서는 상태 전환/물리 힘 적용을 아직 Actor에 남기고, 결과 객체 조립과 기록만 중앙화한다.
    /// </summary>
    public static class CombatResolutionPipeline
    {
        public static CombatResult ResolvePlayerHit(
            PlayerActor victim,
            AttackData attackData,
            in PlayerDefenseQuery defenseQuery)
        {
            HitContext hit = HitContext.FromAttackData(attackData, victim);
            DefenseResult defense = DefenseResolver.ResolvePlayerDefense(defenseQuery, attackData);

            if (!defense.ShouldApplyDamage)
                return CombatResult.Build(hit, defense, default, ReactionDecision.None, ResourceChangeSet.Empty);

            DamageResult damage = DamageResolver.ResolvePlayerDamage(victim, attackData);
            return BuildDamageResult(hit, defense, damage);
        }

        public static CombatResult ResolvePlayerGuardBreakDamage(
            PlayerActor victim,
            AttackData attackData)
        {
            HitContext hit = HitContext.FromAttackData(attackData, victim);
            DamageResult damage = DamageResolver.ResolvePlayerDamage(victim, attackData, includeCritical: false);
            return BuildDamageResult(hit, new DefenseResult(DefenseOutcome.GuardBreak, true), damage);
        }

        public static CombatResult ResolveMonsterHit(
            MonsterActor victim,
            AttackData attackData,
            MonsterBreakGauge breakGauge)
        {
            HitContext hit = HitContext.FromAttackData(attackData, victim);
            DamageResult damage = DamageResolver.ResolveMonsterDamage(victim, attackData, breakGauge);
            return BuildDamageResult(hit, DefenseResult.None, damage);
        }

        public static CombatResult ResolveSpecialBreakHit(
            GameActor attacker,
            MonsterActor victim,
            DamageResult damage,
            Vector3 hitPoint)
        {
            var hit = new HitContext(
                attacker,
                victim,
                AnimKey.None,
                0,
                AttackKind.HeavyAttack,
                AttackReactionType.Heavy,
                AttackDefenseType.Unblockable,
                damage.BaseDamage,
                0f,
                0f,
                false,
                hitPoint,
                Vector3.zero,
                victim != null ? victim.gameObject : null,
                null,
                null);

            return BuildDamageResult(hit, DefenseResult.None, damage);
        }

        public static CombatResult WithReaction(in CombatResult result, in ReactionDecision reaction)
        {
            return CombatResult.Build(
                result.Hit,
                result.Defense,
                result.Damage,
                reaction,
                result.Resources);
        }

        public static CombatResult WithReactionAndResources(
            in CombatResult result,
            in ReactionDecision reaction,
            in ResourceChangeSet resources)
        {
            return CombatResult.Build(
                result.Hit,
                result.Defense,
                result.Damage,
                reaction,
                resources);
        }

        public static CombatResult WithMonsterAppliedResources(
            in CombatResult result,
            in ReactionDecision reaction,
            float poiseDamageApplied,
            float breakDamageApplied)
        {
            return WithReactionAndResources(
                result,
                reaction,
                result.Resources.WithPoiseAndBreak(poiseDamageApplied, breakDamageApplied));
        }

        public static void RecordIfDamageApplied(in CombatResult result)
        {
            if (!result.DamageApplied || result.FinalDamage <= 0f)
                return;

            CombatLogRecorder.Record(result);
        }

        public static void RecordIfObservable(in CombatResult result)
        {
            if (result.DamageApplied)
            {
                if (result.FinalDamage > 0f)
                    CombatLogRecorder.Record(result);
                return;
            }

            if (result.DefenseOutcome != DefenseOutcome.None)
                CombatLogRecorder.Record(result);
        }

        private static CombatResult BuildDamageResult(
            in HitContext hit,
            in DefenseResult defense,
            in DamageResult damage)
        {
            return CombatResult.Build(
                hit,
                defense,
                damage,
                ReactionDecision.None,
                ResourceChangeSet.FromDamage(damage.FinalDamage));
        }
    }
}
