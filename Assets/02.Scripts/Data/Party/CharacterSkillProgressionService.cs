using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Party
{
    /// <summary>
    /// PartyManager가 소유하는 캐릭터별 고정 스킬 트리 런타임.
    /// 레벨을 주입받아 순수한 포인트/노드 규칙을 한곳에서 처리한다.
    /// </summary>
    public sealed class CharacterSkillProgressionService
    {
        private readonly Dictionary<CharacterActorType, CharacterSkillTreeSO> _trees = new();
        private readonly Dictionary<CharacterActorType, CharacterSkillProgressState> _states = new();
        private SkillPointRule _pointRule = new();
        private Func<CharacterActorType, int> _levelProvider;

        public event Action<CharacterActorType> OnSkillProgressChanged;

        public void Configure(
            IEnumerable<CharacterSkillTreeSO> trees,
            SkillPointRule pointRule,
            Func<CharacterActorType, int> levelProvider)
        {
            _trees.Clear();
            if (trees != null)
            {
                foreach (CharacterSkillTreeSO tree in trees)
                {
                    if (tree == null
                        || tree.characterType == CharacterActorType.None
                        || _trees.ContainsKey(tree.characterType))
                        continue;
                    _trees.Add(tree.characterType, tree);
                }
            }

            _pointRule = pointRule ?? new SkillPointRule();
            _levelProvider = levelProvider;
            ReconcileAllLevels(notify: false);
            // 트리가 교체되면 기존 상태의 노드/포인트 회계가 트리와 어긋날 수 있으므로 재정합한다.
            ReconcileAllAccounting();
        }

        public CharacterSkillTreeSO GetTree(CharacterActorType type) =>
            _trees.TryGetValue(type, out CharacterSkillTreeSO tree) ? tree : null;

        public CharacterSkillProgressState GetState(CharacterActorType type) =>
            EnsureState(type);

        public int GetAvailablePoints(CharacterActorType type)
        {
            CharacterSkillProgressState state = EnsureState(type);
            return state == null
                ? 0
                : Mathf.Max(0, state.totalPoints - state.spentPoints);
        }

        public int GetNodeRank(CharacterActorType type, string nodeId)
        {
            CharacterSkillProgressState state = EnsureState(type);
            return FindRank(state, nodeId);
        }

        public bool CanTakeNode(
            CharacterActorType type,
            string nodeId,
            out SkillNodeBlockReason reason)
        {
            CharacterSkillTreeSO tree = GetTree(type);
            if (tree == null)
            {
                reason = SkillNodeBlockReason.MissingTree;
                return false;
            }

            SkillNodeDefinition node = tree.FindNode(nodeId);
            if (node == null)
            {
                reason = SkillNodeBlockReason.MissingNode;
                return false;
            }

            CharacterSkillProgressState state = EnsureState(type);
            int rank = FindRank(state, node.NormalizedId);
            if (rank >= Mathf.Max(1, node.maxRank))
            {
                reason = SkillNodeBlockReason.MaxRank;
                return false;
            }

            int level = Mathf.Max(1, _levelProvider?.Invoke(type) ?? 1);
            if (level < Mathf.Max(0, node.requiredLevel))
            {
                reason = SkillNodeBlockReason.LevelTooLow;
                return false;
            }

            if (node.requiredNodeIds != null)
            {
                for (int i = 0; i < node.requiredNodeIds.Count; i++)
                {
                    if (FindRank(state, node.requiredNodeIds[i]) > 0)
                        continue;
                    reason = SkillNodeBlockReason.MissingPrerequisite;
                    return false;
                }
            }

            if (GetAvailablePoints(type) < Mathf.Max(1, node.cost))
            {
                reason = SkillNodeBlockReason.InsufficientPoints;
                return false;
            }

            reason = SkillNodeBlockReason.None;
            return true;
        }

        public bool TryTakeNode(CharacterActorType type, string nodeId)
        {
            if (!CanTakeNode(type, nodeId, out _))
                return false;

            CharacterSkillTreeSO tree = GetTree(type);
            SkillNodeDefinition node = tree.FindNode(nodeId);
            CharacterSkillProgressState state = EnsureState(type);
            SkillNodeRankEntry entry = FindEntry(state, node.NormalizedId);
            if (entry == null)
            {
                entry = new SkillNodeRankEntry
                {
                    nodeId = node.NormalizedId,
                    rank = 0,
                };
                state.takenNodes.Add(entry);
            }

            entry.rank++;
            state.spentPoints += Mathf.Max(1, node.cost);
            OnSkillProgressChanged?.Invoke(type);
            return true;
        }

        public bool TryRespec(CharacterActorType type)
        {
            CharacterSkillProgressState state = EnsureState(type);
            if (state == null)
                return false;
            state.takenNodes.Clear();
            state.spentPoints = 0;
            OnSkillProgressChanged?.Invoke(type);
            return true;
        }

        public void ReconcileLevel(CharacterActorType type, bool notify = true)
        {
            if (type == CharacterActorType.None)
                return;
            CharacterSkillProgressState state = EnsureState(type, reconcile: false);
            int level = Mathf.Max(1, _levelProvider?.Invoke(type) ?? 1);
            int oldGrantedLevel = Mathf.Max(1, state.grantedUpToLevel);
            if (level <= oldGrantedLevel)
                return;

            int grant = Mathf.Max(
                0,
                _pointRule.TotalPointsAtLevel(level)
                - _pointRule.TotalPointsAtLevel(oldGrantedLevel));
            state.totalPoints += grant;
            state.grantedUpToLevel = level;
            if (notify && grant > 0)
                OnSkillProgressChanged?.Invoke(type);
        }

        public void GrantBonusPoints(CharacterActorType type, int amount)
        {
            if (type == CharacterActorType.None || amount <= 0)
                return;
            EnsureState(type).totalPoints += amount;
            OnSkillProgressChanged?.Invoke(type);
        }

        public IReadOnlyList<SkillStatModifierEntry> GetStatModifiers(
            CharacterActorType type)
        {
            var totals = new Dictionary<(AttributeId, AttributeModifierOperation), float>();
            VisitTakenEffects(type, (effect, rank) =>
            {
                if (effect is not StatDeltaEffect stat || !stat.AttributeId.IsValid)
                    return;
                var key = (stat.AttributeId, stat.operation);
                float value = stat.valuePerRank * rank;
                if (stat.operation == AttributeModifierOperation.Multiply)
                {
                    float factor = Mathf.Pow(stat.valuePerRank, rank);
                    totals[key] = totals.TryGetValue(key, out float current)
                        ? current * factor
                        : factor;
                }
                else
                {
                    totals[key] = totals.TryGetValue(key, out float current)
                        ? current + value
                        : value;
                }
            });

            var result = new List<SkillStatModifierEntry>(totals.Count);
            foreach (KeyValuePair<(AttributeId, AttributeModifierOperation), float> pair in totals)
                result.Add(new SkillStatModifierEntry(pair.Key.Item1, pair.Key.Item2, pair.Value));
            result.Sort((left, right) =>
            {
                int id = string.CompareOrdinal(left.AttributeId.Value, right.AttributeId.Value);
                return id != 0 ? id : left.Operation.CompareTo(right.Operation);
            });
            return result;
        }

        public float GetAbilityScalar(
            CharacterActorType type,
            string abilityId,
            AbilityScalarKind kind)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
                return 1f;
            float flat = 0f;
            float percent = 0f;
            float multiply = 1f;
            VisitTakenEffects(type, (effect, rank) =>
            {
                if (effect is not AbilityScalarEffect scalar
                    || scalar.kind != kind
                    || !string.Equals(
                        scalar.abilityId?.Trim(),
                        abilityId.Trim(),
                        StringComparison.Ordinal))
                    return;
                float value = scalar.valuePerRank * rank;
                switch (scalar.operation)
                {
                    case ModifierType.Flat:
                        flat += value;
                        break;
                    case ModifierType.Percent:
                        percent += value;
                        break;
                    case ModifierType.Multiply:
                        multiply *= Mathf.Pow(scalar.valuePerRank, rank);
                        break;
                }
            });
            return Mathf.Max(0f, (1f + flat) * (1f + percent) * multiply);
        }

        public bool IsAbilityUnlocked(CharacterActorType type, string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
                return true;
            bool gated = false;
            bool unlocked = false;
            CharacterSkillTreeSO tree = GetTree(type);
            if (tree?.nodes == null)
                return true;
            for (int i = 0; i < tree.nodes.Count; i++)
            {
                SkillNodeDefinition node = tree.nodes[i];
                if (node?.effects == null)
                    continue;
                for (int j = 0; j < node.effects.Count; j++)
                {
                    if (node.effects[j] is not AbilityUnlockEffect effect
                        || !string.Equals(
                            effect.abilityId?.Trim(),
                            abilityId.Trim(),
                            StringComparison.Ordinal))
                        continue;
                    gated = true;
                    unlocked |= GetNodeRank(type, node.NormalizedId) > 0;
                }
            }
            return !gated || unlocked;
        }

        public float GetDodgeCooldownMultiplier(CharacterActorType type)
        {
            float reduction = 0f;
            VisitTakenEffects(type, (effect, rank) =>
            {
                if (effect is DodgeCooldownEffect dodge)
                    reduction += dodge.reductionPerRank * rank;
            });
            return Mathf.Clamp(1f - reduction, 0.2f, 1f);
        }

        public IReadOnlyList<PassiveAbilitySO> GetGrantedPassives(
            CharacterActorType type)
        {
            var result = new List<PassiveAbilitySO>();
            var seen = new HashSet<PassiveAbilitySO>();
            VisitTakenEffects(type, (effect, _) =>
            {
                if (effect is PassiveGrantEffect grant
                    && grant.passive != null
                    && seen.Add(grant.passive))
                    result.Add(grant.passive);
            });
            return result;
        }

        public List<CharacterSkillProgressState> ExportStates()
        {
            var result = new List<CharacterSkillProgressState>(_states.Count);
            foreach (CharacterSkillProgressState state in _states.Values)
                result.Add(Clone(state));
            result.Sort((left, right) => left.characterType.CompareTo(right.characterType));
            return result;
        }

        public void ImportStates(IEnumerable<CharacterSkillProgressState> states)
        {
            _states.Clear();
            if (states != null)
            {
                foreach (CharacterSkillProgressState source in states)
                {
                    if (source == null
                        || source.characterType == CharacterActorType.None
                        || _states.ContainsKey(source.characterType))
                        continue;
                    CharacterSkillProgressState state = Clone(source);
                    state.grantedUpToLevel = Mathf.Max(1, state.grantedUpToLevel);
                    state.totalPoints = Mathf.Max(0, state.totalPoints);
                    SanitizeRanks(state);
                    RecalculateSpent(state);
                    _states.Add(state.characterType, state);
                }
            }
            ReconcileAllLevels(notify: false);
        }

        public void Clear() => _states.Clear();

        private void ReconcileAllLevels(bool notify)
        {
            var types = new HashSet<CharacterActorType>(_trees.Keys);
            foreach (CharacterActorType type in _states.Keys)
                types.Add(type);
            foreach (CharacterActorType type in types)
                ReconcileLevel(type, notify);
        }

        private void ReconcileAllAccounting()
        {
            foreach (CharacterSkillProgressState state in _states.Values)
            {
                SanitizeRanks(state);
                RecalculateSpent(state);
            }
        }

        private CharacterSkillProgressState EnsureState(
            CharacterActorType type,
            bool reconcile = true)
        {
            if (type == CharacterActorType.None)
                return null;
            if (!_states.TryGetValue(type, out CharacterSkillProgressState state))
            {
                int level = Mathf.Max(1, _levelProvider?.Invoke(type) ?? 1);
                state = new CharacterSkillProgressState
                {
                    characterType = type,
                    grantedUpToLevel = level,
                    totalPoints = _pointRule.TotalPointsAtLevel(level),
                    spentPoints = 0,
                    takenNodes = new List<SkillNodeRankEntry>(),
                };
                _states.Add(type, state);
            }
            if (reconcile)
                ReconcileLevel(type, notify: false);
            return state;
        }

        private void VisitTakenEffects(
            CharacterActorType type,
            Action<SkillNodeEffect, int> visitor)
        {
            CharacterSkillTreeSO tree = GetTree(type);
            CharacterSkillProgressState state = EnsureState(type);
            if (tree?.nodes == null || state == null || visitor == null)
                return;
            for (int i = 0; i < tree.nodes.Count; i++)
            {
                SkillNodeDefinition node = tree.nodes[i];
                int rank = node == null ? 0 : FindRank(state, node.NormalizedId);
                if (rank <= 0 || node.effects == null)
                    continue;
                for (int j = 0; j < node.effects.Count; j++)
                    if (node.effects[j] != null)
                        visitor(node.effects[j], rank);
            }
        }

        private void SanitizeRanks(CharacterSkillProgressState state)
        {
            state.takenNodes ??= new List<SkillNodeRankEntry>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = state.takenNodes.Count - 1; i >= 0; i--)
            {
                SkillNodeRankEntry entry = state.takenNodes[i];
                string id = entry?.nodeId?.Trim();
                SkillNodeDefinition node = GetTree(state.characterType)?.FindNode(id);
                if (entry == null
                    || string.IsNullOrEmpty(id)
                    || node == null
                    || !seen.Add(id))
                {
                    state.takenNodes.RemoveAt(i);
                    continue;
                }
                entry.nodeId = id;
                entry.rank = Mathf.Clamp(entry.rank, 0, Mathf.Max(1, node?.maxRank ?? entry.rank));
            }
        }

        private void RecalculateSpent(CharacterSkillProgressState state)
        {
            int spent = 0;
            CharacterSkillTreeSO tree = GetTree(state.characterType);
            if (state.takenNodes != null)
            {
                for (int i = 0; i < state.takenNodes.Count; i++)
                {
                    SkillNodeRankEntry entry = state.takenNodes[i];
                    SkillNodeDefinition node = tree?.FindNode(entry?.nodeId);
                    if (node != null)
                        spent += Mathf.Max(0, entry.rank) * Mathf.Max(1, node.cost);
                }
            }
            state.spentPoints = Mathf.Clamp(spent, 0, Mathf.Max(spent, state.totalPoints));
        }

        private static SkillNodeRankEntry FindEntry(
            CharacterSkillProgressState state,
            string nodeId)
        {
            if (state?.takenNodes == null || string.IsNullOrWhiteSpace(nodeId))
                return null;
            string normalized = nodeId.Trim();
            for (int i = 0; i < state.takenNodes.Count; i++)
                if (state.takenNodes[i] != null
                    && string.Equals(
                        state.takenNodes[i].nodeId?.Trim(),
                        normalized,
                        StringComparison.Ordinal))
                    return state.takenNodes[i];
            return null;
        }

        private static int FindRank(
            CharacterSkillProgressState state,
            string nodeId) =>
            Mathf.Max(0, FindEntry(state, nodeId)?.rank ?? 0);

        private static CharacterSkillProgressState Clone(
            CharacterSkillProgressState source)
        {
            var clone = new CharacterSkillProgressState
            {
                characterType = source.characterType,
                grantedUpToLevel = source.grantedUpToLevel,
                totalPoints = source.totalPoints,
                spentPoints = source.spentPoints,
                takenNodes = new List<SkillNodeRankEntry>(),
            };
            if (source.takenNodes != null)
                for (int i = 0; i < source.takenNodes.Count; i++)
                {
                    SkillNodeRankEntry entry = source.takenNodes[i];
                    if (entry != null)
                        clone.takenNodes.Add(new SkillNodeRankEntry
                        {
                            nodeId = entry.nodeId,
                            rank = entry.rank,
                        });
                }
            return clone;
        }
    }
}
