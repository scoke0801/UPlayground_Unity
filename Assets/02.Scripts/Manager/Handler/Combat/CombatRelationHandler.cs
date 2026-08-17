using System;
using System.Collections.Generic;
using UPlayGround.Data.Combat;

namespace UPlayGround.Manager.Handler
{
    /// <summary>진영 관계와 제한된 수명의 런타임 소속 덮어쓰기를 관리한다.</summary>
    public sealed class CombatRelationHandler
    {
        private sealed class AffiliationOverride
        {
            public long Token;
            public string FactionId;
            public CombatCreditOwner CreditOwner;
        }

        private sealed class OverrideLease : IDisposable
        {
            private CombatRelationHandler _owner;
            private readonly int _combatantId;
            private readonly long _token;

            public OverrideLease(CombatRelationHandler owner, int combatantId, long token)
            {
                _owner = owner;
                _combatantId = combatantId;
                _token = token;
            }

            public void Dispose()
            {
                _owner?.RemoveOverride(_combatantId, _token);
                _owner = null;
            }
        }

        private readonly Dictionary<int, List<AffiliationOverride>> _overrides = new();
        private readonly CombatFactionRelationTableSO _relationTable;
        private long _nextToken;

        public CombatRelationHandler(CombatFactionRelationTableSO relationTable)
        {
            _relationTable = relationTable;
        }

        public CombatRelation GetRelation(
            ICombatAffiliationView source,
            ICombatAffiliationView target)
        {
            if (source == null || target == null)
                return CombatRelation.Neutral;

            string sourceFaction = ResolveFactionId(source);
            string targetFaction = ResolveFactionId(target);
            return _relationTable != null
                ? _relationTable.Resolve(sourceFaction, targetFaction)
                : CombatFactionRules.ResolveDefaultRelation(sourceFaction, targetFaction);
        }

        public bool CanTarget(
            ICombatAffiliationView source,
            ICombatAffiliationView target)
        {
            return source != null
                   && target != null
                   && source.CombatantRuntimeId != target.CombatantRuntimeId
                   && target.IsCombatAvailable
                   && GetRelation(source, target) == CombatRelation.Hostile;
        }

        public bool CanDamage(
            ICombatAffiliationView source,
            ICombatAffiliationView target,
            CombatTargetPolicy policy)
        {
            if (source == null || target == null)
                return true;

            bool isSelf = source.CombatantRuntimeId == target.CombatantRuntimeId;
            return CombatFactionRules.MatchesPolicy(
                GetRelation(source, target),
                isSelf,
                policy);
        }

        public CombatCreditOwner GetCreditOwner(ICombatAffiliationView actor)
        {
            if (actor == null)
                return CombatCreditOwner.None;
            return TryGetCurrentOverride(actor.CombatantRuntimeId, out var current)
                ? current.CreditOwner
                : actor.CombatCreditOwner;
        }

        public IDisposable OverrideAffiliation(
            ICombatAffiliationView actor,
            CombatFactionSO faction,
            CombatCreditOwner creditOwner)
        {
            if (actor == null || faction == null || string.IsNullOrWhiteSpace(faction.FactionId))
                return null;

            int combatantId = actor.CombatantRuntimeId;
            if (!_overrides.TryGetValue(combatantId, out var entries))
            {
                entries = new List<AffiliationOverride>();
                _overrides.Add(combatantId, entries);
            }

            long token = ++_nextToken;
            entries.Add(new AffiliationOverride
            {
                Token = token,
                FactionId = faction.FactionId,
                CreditOwner = creditOwner,
            });
            return new OverrideLease(this, combatantId, token);
        }

        public void Clear()
        {
            _overrides.Clear();
        }

        private string ResolveFactionId(ICombatAffiliationView actor)
        {
            return TryGetCurrentOverride(actor.CombatantRuntimeId, out var current)
                ? current.FactionId
                : actor.CombatFactionId;
        }

        private bool TryGetCurrentOverride(int combatantId, out AffiliationOverride current)
        {
            current = null;
            if (!_overrides.TryGetValue(combatantId, out var entries) || entries.Count == 0)
                return false;

            current = entries[entries.Count - 1];
            return true;
        }

        private void RemoveOverride(int combatantId, long token)
        {
            if (!_overrides.TryGetValue(combatantId, out var entries))
                return;

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].Token != token)
                    continue;
                entries.RemoveAt(i);
                break;
            }

            if (entries.Count == 0)
                _overrides.Remove(combatantId);
        }
    }
}
