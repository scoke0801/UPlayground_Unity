using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Party
{
    /// <summary>한 Ability에 적용할 스킬 트리 배율을 종류별 최종값으로 묶는다.</summary>
    public readonly struct AbilityScalarSet
    {
        public AbilityScalarSet(
            float damage,
            float breakDamage,
            float cooldown,
            float cost)
        {
            Damage = damage;
            BreakDamage = breakDamage;
            Cooldown = cooldown;
            Cost = cost;
        }

        public float Damage { get; }
        public float BreakDamage { get; }
        public float Cooldown { get; }
        public float Cost { get; }

        public float Get(AbilityScalarKind kind) =>
            kind switch
            {
                AbilityScalarKind.Damage => Damage,
                AbilityScalarKind.BreakDamage => BreakDamage,
                AbilityScalarKind.Cooldown => Cooldown,
                AbilityScalarKind.Cost => Cost,
                _ => 1f,
            };
    }

    /// <summary>스킬 트리의 모든 전투 효과를 소비자가 함께 읽는 불변 결과로 제공한다.</summary>
    public sealed class ResolvedCharacterGrowth
    {
        private readonly HashSet<string> _gatedAbilityIds;
        private readonly HashSet<string> _unlockedAbilityIds;

        internal ResolvedCharacterGrowth(
            CharacterActorType characterType,
            IReadOnlyList<SkillStatModifierEntry> attributeModifiers,
            HashSet<string> gatedAbilityIds,
            HashSet<string> unlockedAbilityIds,
            IReadOnlyDictionary<string, AbilityScalarSet> abilityScalars,
            IReadOnlyList<PassiveAbilitySO> grantedPassives,
            float dodgeCooldownMultiplier,
            int version)
        {
            CharacterType = characterType;
            AttributeModifiers = attributeModifiers;
            _gatedAbilityIds = gatedAbilityIds;
            _unlockedAbilityIds = unlockedAbilityIds;
            AbilityScalars = abilityScalars;
            GrantedPassives = grantedPassives;
            DodgeCooldownMultiplier = dodgeCooldownMultiplier;
            Version = version;
        }

        public CharacterActorType CharacterType { get; }
        public IReadOnlyList<SkillStatModifierEntry> AttributeModifiers { get; }
        public IReadOnlyCollection<string> UnlockedAbilityIds => _unlockedAbilityIds;
        public IReadOnlyDictionary<string, AbilityScalarSet> AbilityScalars { get; }
        public IReadOnlyList<PassiveAbilitySO> GrantedPassives { get; }
        public float DodgeCooldownMultiplier { get; }
        public int Version { get; }

        public bool IsAbilityUnlocked(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
                return true;

            string normalized = abilityId.Trim();
            return !_gatedAbilityIds.Contains(normalized)
                   || _unlockedAbilityIds.Contains(normalized);
        }

        public float GetAbilityScalar(string abilityId, AbilityScalarKind kind)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
                return 1f;

            return AbilityScalars.TryGetValue(
                    abilityId.Trim(),
                    out AbilityScalarSet scalars)
                ? scalars.Get(kind)
                : 1f;
        }
    }

    /// <summary>취득 노드에서 Attribute·Ability·Passive·회피 규칙을 한 번에 해석한다.</summary>
    public sealed class CharacterGrowthResolver
    {
        private readonly struct ScalarKey : IEquatable<ScalarKey>
        {
            public ScalarKey(string abilityId, AbilityScalarKind kind)
            {
                AbilityId = abilityId;
                Kind = kind;
            }

            public string AbilityId { get; }
            public AbilityScalarKind Kind { get; }

            public bool Equals(ScalarKey other) =>
                Kind == other.Kind
                && string.Equals(AbilityId, other.AbilityId, StringComparison.Ordinal);

            public override bool Equals(object obj) =>
                obj is ScalarKey other && Equals(other);

            public override int GetHashCode() =>
                HashCode.Combine(AbilityId, (int)Kind);
        }

        private struct ScalarAccumulator
        {
            public float Flat;
            public float Percent;
            public float Multiply;

            public static ScalarAccumulator Identity =>
                new() { Multiply = 1f };

            public float Resolve() =>
                Mathf.Max(0f, (1f + Flat) * (1f + Percent) * Multiply);
        }

        public ResolvedCharacterGrowth Resolve(
            CharacterActorType characterType,
            CharacterSkillTreeSO tree,
            Func<SkillNodeDefinition, int> rankProvider,
            int version)
        {
            var statTotals =
                new Dictionary<(AttributeId, AttributeModifierOperation), float>();
            var scalarTotals = new Dictionary<ScalarKey, ScalarAccumulator>();
            var gatedAbilityIds = new HashSet<string>(StringComparer.Ordinal);
            var unlockedAbilityIds = new HashSet<string>(StringComparer.Ordinal);
            var passives = new List<PassiveAbilitySO>();
            var seenPassives = new HashSet<PassiveAbilitySO>();
            float dodgeCooldownReduction = 0f;

            for (int nodeIndex = 0;
                 nodeIndex < (tree?.nodes?.Count ?? 0);
                 nodeIndex++)
            {
                SkillNodeDefinition node = tree.nodes[nodeIndex];
                if (node?.effects == null)
                    continue;

                int rank = Mathf.Max(0, rankProvider?.Invoke(node) ?? 0);
                for (int effectIndex = 0;
                     effectIndex < node.effects.Count;
                     effectIndex++)
                {
                    SkillNodeEffect effect = node.effects[effectIndex];
                    if (effect is AbilityUnlockEffect unlock)
                        ResolveUnlock(unlock, rank, gatedAbilityIds, unlockedAbilityIds);
                    if (rank <= 0 || effect == null)
                        continue;

                    switch (effect)
                    {
                        case StatDeltaEffect stat when stat.AttributeId.IsValid:
                            AddStat(statTotals, stat, rank);
                            break;
                        case AbilityScalarEffect scalar:
                            AddScalar(scalarTotals, scalar, rank);
                            break;
                        case DodgeCooldownEffect dodge:
                            dodgeCooldownReduction += dodge.reductionPerRank * rank;
                            break;
                        case PassiveGrantEffect grant
                            when grant.passive != null && seenPassives.Add(grant.passive):
                            passives.Add(grant.passive);
                            break;
                    }
                }
            }

            return new ResolvedCharacterGrowth(
                characterType,
                BuildStatModifiers(statTotals),
                gatedAbilityIds,
                unlockedAbilityIds,
                BuildAbilityScalars(scalarTotals),
                passives,
                Mathf.Clamp(1f - dodgeCooldownReduction, 0.2f, 1f),
                version);
        }

        private static void ResolveUnlock(
            AbilityUnlockEffect unlock,
            int rank,
            HashSet<string> gatedAbilityIds,
            HashSet<string> unlockedAbilityIds)
        {
            string abilityId = unlock.abilityId?.Trim();
            if (string.IsNullOrEmpty(abilityId))
                return;

            gatedAbilityIds.Add(abilityId);
            if (unlock.grantedByDefault || rank > 0)
                unlockedAbilityIds.Add(abilityId);
        }

        private static void AddStat(
            Dictionary<(AttributeId, AttributeModifierOperation), float> totals,
            StatDeltaEffect stat,
            int rank)
        {
            var key = (stat.AttributeId, stat.operation);
            if (stat.operation == AttributeModifierOperation.Multiply)
            {
                float factor = Mathf.Pow(stat.valuePerRank, rank);
                totals[key] = totals.TryGetValue(key, out float current)
                    ? current * factor
                    : factor;
                return;
            }

            float value = stat.valuePerRank * rank;
            totals[key] = totals.TryGetValue(key, out float currentValue)
                ? currentValue + value
                : value;
        }

        private static void AddScalar(
            Dictionary<ScalarKey, ScalarAccumulator> totals,
            AbilityScalarEffect scalar,
            int rank)
        {
            string abilityId = scalar.abilityId?.Trim();
            if (string.IsNullOrEmpty(abilityId))
                return;

            var key = new ScalarKey(abilityId, scalar.kind);
            ScalarAccumulator accumulator = totals.TryGetValue(key, out var current)
                ? current
                : ScalarAccumulator.Identity;
            float value = scalar.valuePerRank * rank;
            switch (scalar.operation)
            {
                case ModifierType.Flat:
                    accumulator.Flat += value;
                    break;
                case ModifierType.Percent:
                    accumulator.Percent += value;
                    break;
                case ModifierType.Multiply:
                    accumulator.Multiply *= Mathf.Pow(scalar.valuePerRank, rank);
                    break;
            }
            totals[key] = accumulator;
        }

        private static IReadOnlyList<SkillStatModifierEntry> BuildStatModifiers(
            Dictionary<(AttributeId, AttributeModifierOperation), float> totals)
        {
            var result = new List<SkillStatModifierEntry>(totals.Count);
            foreach (KeyValuePair<(AttributeId, AttributeModifierOperation), float> pair
                     in totals)
            {
                result.Add(new SkillStatModifierEntry(
                    pair.Key.Item1,
                    pair.Key.Item2,
                    pair.Value));
            }
            result.Sort((left, right) =>
            {
                int id = string.CompareOrdinal(
                    left.AttributeId.Value,
                    right.AttributeId.Value);
                return id != 0 ? id : left.Operation.CompareTo(right.Operation);
            });
            return result;
        }

        private static IReadOnlyDictionary<string, AbilityScalarSet> BuildAbilityScalars(
            Dictionary<ScalarKey, ScalarAccumulator> totals)
        {
            var byAbility =
                new Dictionary<string, float[]>(StringComparer.Ordinal);
            foreach (KeyValuePair<ScalarKey, ScalarAccumulator> pair in totals)
            {
                if (!byAbility.TryGetValue(pair.Key.AbilityId, out float[] values))
                {
                    values = new[] { 1f, 1f, 1f, 1f };
                    byAbility.Add(pair.Key.AbilityId, values);
                }
                values[(int)pair.Key.Kind] = pair.Value.Resolve();
            }

            var result =
                new Dictionary<string, AbilityScalarSet>(byAbility.Count, StringComparer.Ordinal);
            foreach (KeyValuePair<string, float[]> pair in byAbility)
            {
                float[] values = pair.Value;
                result.Add(pair.Key, new AbilityScalarSet(
                    values[(int)AbilityScalarKind.Damage],
                    values[(int)AbilityScalarKind.BreakDamage],
                    values[(int)AbilityScalarKind.Cooldown],
                    values[(int)AbilityScalarKind.Cost]));
            }
            return result;
        }
    }
}
