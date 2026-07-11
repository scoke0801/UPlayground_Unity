using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UnityEngine;

namespace UPlayGround.Combat
{
    /// <summary>
    /// 모든 피격의 실행 진입점. 계산 후 대상에게 적용을 위임하고 최종 결과를 한 번만 기록한다.
    /// </summary>
    public static class CombatResolutionPipeline
    {
        public static CombatResult Execute(IDamageable victim, in HitRequest request)
        {
            if (victim == null)
                return default;

            CombatResult result = victim switch
            {
                PlayerActor player => ExecutePlayerHit(player, request),
                MonsterActor monster => ExecuteMonsterHit(monster, request),
                _ => default,
            };

            RecordIfObservable(result);
            return result;
        }

        private static CombatResult ExecutePlayerHit(PlayerActor victim, in HitRequest request)
        {
            CombatResult resolved = ResolvePlayerHit(
                victim,
                request,
                victim.BuildCombatDefenseQuery());
            return victim.ApplyResolvedHit(request, resolved);
        }

        private static CombatResult ExecuteMonsterHit(MonsterActor victim, in HitRequest request)
        {
            if (!victim.CanResolveHit(request))
                return default;

            CombatResult resolved = ResolveMonsterHit(victim, request, victim.BreakGauge);
            return victim.ApplyResolvedHit(request, resolved);
        }

        public static CombatResult ResolvePlayerHit(
            PlayerActor victim,
            in HitRequest request,
            in PlayerDefenseQuery defenseQuery)
        {
            HitContext hit = HitContext.Create(request, victim);
            DefenseResult defense = DefenseResolver.ResolvePlayerDefense(defenseQuery, hit);

            if (!defense.ShouldApplyDamage)
                return CombatResult.Build(hit, defense, default, ReactionDecision.None, ResourceChangeSet.Empty);

            DamageResult damage = DamageResolver.ResolvePlayerDamage(victim, hit);
            return BuildDamageResult(hit, defense, damage);
        }

        public static CombatResult ResolvePlayerGuardBreakDamage(
            PlayerActor victim,
            in HitRequest request)
        {
            HitContext hit = HitContext.Create(request, victim);
            DamageResult damage = DamageResolver.ResolvePlayerDamage(victim, hit, includeCritical: false);
            return BuildDamageResult(hit, new DefenseResult(DefenseOutcome.GuardBreak, true), damage);
        }

        public static CombatResult ResolveMonsterHit(
            MonsterActor victim,
            in HitRequest request,
            MonsterBreakGauge breakGauge)
        {
            HitContext hit = HitContext.Create(request, victim);
            DamageResult damage = request.IsSpecialBreak
                ? DamageResolver.ResolveSpecialBreakDamage(
                    victim.MaxHealth,
                    request.SpecialDamageByMaxHpRate,
                    request.SpecialFixedDamage,
                    request.SpecialMinReferenceHealth)
                : DamageResolver.ResolveMonsterDamage(victim, hit, breakGauge);
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
