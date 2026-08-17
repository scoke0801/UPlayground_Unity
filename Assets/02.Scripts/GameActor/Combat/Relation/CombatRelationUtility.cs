using UPlayGround.Data.Combat;
using UPlayGround.Manager;

namespace UPlayGround.Combat
{
    /// <summary>Actor 계층의 모든 관계 판정이 서비스와 동일한 폴백 규칙을 사용하게 한다.</summary>
    public static class CombatRelationUtility
    {
        public static CombatRelation GetRelation(
            ICombatAffiliationView source,
            ICombatAffiliationView target)
        {
            if (source == null || target == null)
                return CombatRelation.Neutral;

            if (Services.TryGet<ICombatRelationService>(out var service))
                return service.GetRelation(source, target);

            return CombatFactionRules.ResolveDefaultRelation(
                source.CombatFactionId,
                target.CombatFactionId);
        }

        public static bool CanTarget(
            ICombatAffiliationView source,
            ICombatAffiliationView target)
        {
            if (source == null || target == null || !target.IsCombatAvailable)
                return false;

            if (Services.TryGet<ICombatRelationService>(out var service))
                return service.CanTarget(source, target);

            return source.CombatantRuntimeId != target.CombatantRuntimeId
                   && GetRelation(source, target) == CombatRelation.Hostile;
        }

        public static bool CanDamage(
            ICombatAffiliationView source,
            ICombatAffiliationView target,
            CombatTargetPolicy policy = CombatTargetPolicy.Hostile)
        {
            if (source == null || target == null)
                return true;

            if (Services.TryGet<ICombatRelationService>(out var service))
                return service.CanDamage(source, target, policy);

            bool isSelf = source.CombatantRuntimeId == target.CombatantRuntimeId;
            return CombatFactionRules.MatchesPolicy(
                GetRelation(source, target),
                isSelf,
                policy);
        }

        public static CombatCreditOwner GetCreditOwner(ICombatAffiliationView actor)
        {
            if (actor == null)
                return CombatCreditOwner.None;
            if (Services.TryGet<ICombatRelationService>(out var service))
                return service.GetCreditOwner(actor);
            return actor.CombatCreditOwner;
        }
    }
}
