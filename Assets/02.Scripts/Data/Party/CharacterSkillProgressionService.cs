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
        private const string LegacyBonusPointSourceId = "Legacy.SaveTotal";

        private readonly Dictionary<CharacterActorType, CharacterSkillTreeSO> _trees = new();
        private readonly Dictionary<CharacterActorType, CharacterSkillProgressState> _states = new();
        private readonly Dictionary<CharacterActorType, ResolvedCharacterGrowth> _resolvedGrowth = new();
        private readonly CharacterGrowthResolver _growthResolver = new();
        private SkillPointRule _pointRule = new();
        private Func<CharacterActorType, int> _levelProvider;
        private int _resolvedGrowthVersion;

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
            _resolvedGrowth.Clear();
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
                : Mathf.Max(0, GetTotalPoints(type, state) - state.spentPoints);
        }

        /// <summary>레벨 포인트와 별도 출처로 지급된 포인트를 합산한다.</summary>
        public int GetTotalPoints(CharacterActorType type)
        {
            CharacterSkillProgressState state = EnsureState(type);
            return state == null ? 0 : GetTotalPoints(type, state);
        }

        public int GetNodeRank(CharacterActorType type, string nodeId)
        {
            CharacterSkillTreeSO tree = GetTree(type);
            SkillNodeDefinition node = tree?.FindNode(nodeId);
            CharacterSkillProgressState state = EnsureState(type);
            return GetEffectiveRank(state, node);
        }

        /// <summary>실제로 포인트를 소비해 취득한 노드가 하나라도 있는지 반환한다.</summary>
        public bool HasSpentNodes(CharacterActorType type)
        {
            CharacterSkillProgressState state = EnsureState(type);
            if (state?.takenNodes == null)
                return false;
            for (int i = 0; i < state.takenNodes.Count; i++)
                if (state.takenNodes[i]?.rank > 0)
                    return true;
            return false;
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
            int rank = GetEffectiveRank(state, node);
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
                    SkillNodeDefinition requiredNode =
                        tree.FindNode(node.requiredNodeIds[i]);
                    if (GetEffectiveRank(state, requiredNode) > 0)
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
            InvalidateResolvedGrowth(type);
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
            InvalidateResolvedGrowth(type);
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
            if (level == oldGrantedLevel)
                return;

            int grantedPoints = Mathf.Max(
                0,
                _pointRule.TotalPointsAtLevel(level)
                - _pointRule.TotalPointsAtLevel(oldGrantedLevel));
            state.grantedUpToLevel = level;
            state.totalPoints = GetTotalPoints(type, state);
            if (notify && grantedPoints > 0)
                OnSkillProgressChanged?.Invoke(type);
        }

        public void GrantBonusPoints(
            CharacterActorType type,
            string sourceId,
            int amount)
        {
            string normalizedSourceId = sourceId?.Trim();
            if (type == CharacterActorType.None
                || string.IsNullOrEmpty(normalizedSourceId)
                || amount <= 0)
            {
                return;
            }
            CharacterSkillProgressState state = EnsureState(type);
            SkillPointGrantEntry grant = FindGrant(
                state.bonusPointGrants,
                normalizedSourceId);
            if (grant == null)
            {
                grant = new SkillPointGrantEntry
                {
                    sourceId = normalizedSourceId,
                };
                state.bonusPointGrants.Add(grant);
            }
            grant.amount += amount;
            state.totalPoints = GetTotalPoints(type, state);
            OnSkillProgressChanged?.Invoke(type);
        }

        public IReadOnlyList<SkillStatModifierEntry> GetStatModifiers(
            CharacterActorType type) =>
            GetResolvedGrowth(type).AttributeModifiers;

        public float GetAbilityScalar(
            CharacterActorType type,
            string abilityId,
            AbilityScalarKind kind) =>
            GetResolvedGrowth(type).GetAbilityScalar(abilityId, kind);

        public bool IsAbilityUnlocked(CharacterActorType type, string abilityId) =>
            GetResolvedGrowth(type).IsAbilityUnlocked(abilityId);

        /// <summary>기본 타수 이후에는 스킬 트리 해금 구간만 순서대로 연다.</summary>
        public int GetUnlockedComboCount(
            CharacterActorType type,
            PlayerCombatAbilitySlot slot,
            IReadOnlyList<GameplayAbilitySO> abilities)
        {
            int abilityCount = abilities?.Count ?? 0;
            if (abilityCount == 0)
                return 0;

            CharacterSkillTreeSO tree = GetTree(type);
            if (tree == null)
                return abilityCount;

            int unlockedCount = Mathf.Clamp(
                tree.GetInitiallyUnlockedComboCount(slot),
                0,
                abilityCount);
            CharacterSkillProgressState state = EnsureState(type);
            bool hasOpenedExtension = false;
            for (int i = unlockedCount; i < abilityCount; i++)
            {
                GameplayAbilitySO ability = abilities[i];
                if (TryGetProgressionUnlockState(
                        tree,
                        state,
                        ability?.abilityId,
                        out bool isUnlocked))
                {
                    if (!isUnlocked)
                        return unlockedCount;
                    hasOpenedExtension = true;
                }

                if (hasOpenedExtension)
                    unlockedCount = i + 1;
            }
            return unlockedCount;
        }

        public float GetDodgeCooldownMultiplier(CharacterActorType type) =>
            GetResolvedGrowth(type).DodgeCooldownMultiplier;

        public IReadOnlyList<PassiveAbilitySO> GetGrantedPassives(
            CharacterActorType type) =>
            GetResolvedGrowth(type).GrantedPassives;

        /// <summary>전투와 UI가 공유하는 캐릭터 성장 최종 결과를 반환한다.</summary>
        public ResolvedCharacterGrowth GetResolvedGrowth(CharacterActorType type)
        {
            if (_resolvedGrowth.TryGetValue(type, out ResolvedCharacterGrowth resolved))
                return resolved;

            CharacterSkillTreeSO tree = GetTree(type);
            CharacterSkillProgressState state = EnsureState(type);
            resolved = _growthResolver.Resolve(
                type,
                tree,
                node => GetEffectiveRank(state, node),
                ++_resolvedGrowthVersion);
            _resolvedGrowth[type] = resolved;
            return resolved;
        }

        /// <summary>레벨에서 재계산할 수 없는 보너스 포인트와 노드 랭크만 저장한다.</summary>
        public CharacterSkillProgressSaveData ExportSaveState(CharacterActorType type)
        {
            CharacterSkillProgressState state = EnsureState(type);
            return new CharacterSkillProgressSaveData
            {
                skillTreeVersion = Mathf.Max(1, GetTree(type)?.skillTreeVersion ?? 1),
                bonusPointGrants = CloneGrants(state?.bonusPointGrants),
                takenNodes = CloneRanks(state?.takenNodes),
            };
        }

        /// <summary>원인 기반 저장값을 현재 레벨·트리 비용 규칙으로 다시 계산한다.</summary>
        public void ImportSaveState(
            CharacterActorType type,
            CharacterSkillProgressSaveData source)
        {
            if (type == CharacterActorType.None || source == null)
                return;

            int level = GetLevel(type);
            var state = new CharacterSkillProgressState
            {
                characterType = type,
                grantedUpToLevel = level,
                bonusPointGrants = CloneGrants(source.bonusPointGrants),
                takenNodes = CloneRanks(source.takenNodes),
            };
            MigrateRanks(
                GetTree(type),
                source.skillTreeVersion,
                state.takenNodes);
            state.totalPoints = GetTotalPoints(type, state);
            SanitizeRanks(state);
            RecalculateSpent(state);
            _states[type] = state;
            InvalidateResolvedGrowth(type);
        }

        /// <summary>3.3 이하 세이브의 결과값에서 보너스 포인트 원인을 복원한다.</summary>
        public void ImportLegacyStates(IEnumerable<CharacterSkillProgressState> states)
        {
            _states.Clear();
            _resolvedGrowth.Clear();
            if (states != null)
            {
                foreach (CharacterSkillProgressState source in states)
                    ImportLegacyState(source);
            }
            ReconcileAllLevels(notify: false);
        }

        /// <summary>혼합 또는 부분 세이브에서 누락된 캐릭터 한 명의 구형 진행도를 보완한다.</summary>
        public void ImportLegacyState(CharacterSkillProgressState source)
        {
            if (source == null
                || source.characterType == CharacterActorType.None
                || _states.ContainsKey(source.characterType))
            {
                return;
            }

            CharacterSkillProgressState state = Clone(source);
            int savedLevel = Mathf.Max(1, state.grantedUpToLevel);
            int migratedBonusPoints = Mathf.Max(
                0,
                state.totalPoints - _pointRule.TotalPointsAtLevel(savedLevel));
            state.bonusPointGrants.Clear();
            if (migratedBonusPoints > 0)
            {
                state.bonusPointGrants.Add(new SkillPointGrantEntry
                {
                    sourceId = LegacyBonusPointSourceId,
                    amount = migratedBonusPoints,
                });
            }
            state.grantedUpToLevel = GetLevel(state.characterType);
            state.totalPoints = GetTotalPoints(state.characterType, state);
            SanitizeRanks(state);
            RecalculateSpent(state);
            _states.Add(state.characterType, state);
            InvalidateResolvedGrowth(state.characterType);
        }

        public void Clear()
        {
            _states.Clear();
            _resolvedGrowth.Clear();
        }

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
                state.totalPoints = GetTotalPoints(state.characterType, state);
                SanitizeRanks(state);
                RecalculateSpent(state);
            }
        }

        private int GetTotalPoints(
            CharacterActorType type,
            CharacterSkillProgressState state) =>
            _pointRule.TotalPointsAtLevel(GetLevel(type))
            + SumBonusPoints(state?.bonusPointGrants);

        private int GetLevel(CharacterActorType type) =>
            Mathf.Max(1, _levelProvider?.Invoke(type) ?? 1);

        private void InvalidateResolvedGrowth(CharacterActorType type)
        {
            _resolvedGrowth.Remove(type);
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
                    bonusPointGrants = new List<SkillPointGrantEntry>(),
                    takenNodes = new List<SkillNodeRankEntry>(),
                };
                _states.Add(type, state);
            }
            if (reconcile)
                ReconcileLevel(type, notify: false);
            return state;
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
            state.spentPoints = Mathf.Max(0, spent);
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

        private static int GetEffectiveRank(
            CharacterSkillProgressState state,
            SkillNodeDefinition node)
        {
            if (node == null)
                return 0;
            int acquiredRank = FindRank(state, node.NormalizedId);
            return node.IsGrantedByDefault
                ? Mathf.Max(1, acquiredRank)
                : acquiredRank;
        }

        private bool TryGetProgressionUnlockState(
            CharacterSkillTreeSO tree,
            CharacterSkillProgressState state,
            string abilityId,
            out bool isUnlocked)
        {
            isUnlocked = false;
            if (string.IsNullOrWhiteSpace(abilityId))
                return false;

            bool isGated = false;
            if (tree?.nodes == null)
                return false;
            string normalizedAbilityId = abilityId.Trim();
            for (int i = 0; i < tree.nodes.Count; i++)
            {
                SkillNodeDefinition node = tree.nodes[i];
                if (node?.effects == null)
                    continue;
                for (int j = 0; j < node.effects.Count; j++)
                {
                    if (node.effects[j] is not AbilityUnlockEffect effect
                        || effect.grantedByDefault
                        || !string.Equals(
                            effect.abilityId?.Trim(),
                            normalizedAbilityId,
                            StringComparison.Ordinal))
                        continue;
                    isGated = true;
                    isUnlocked |= GetEffectiveRank(state, node) > 0;
                }
            }
            return isGated;
        }

        private static CharacterSkillProgressState Clone(
            CharacterSkillProgressState source)
        {
            var clone = new CharacterSkillProgressState
            {
                characterType = source.characterType,
                grantedUpToLevel = source.grantedUpToLevel,
                totalPoints = source.totalPoints,
                spentPoints = source.spentPoints,
                bonusPointGrants = CloneGrants(source.bonusPointGrants),
                takenNodes = CloneRanks(source.takenNodes),
            };
            return clone;
        }

        private static List<SkillNodeRankEntry> CloneRanks(
            IReadOnlyList<SkillNodeRankEntry> source)
        {
            var result = new List<SkillNodeRankEntry>(source?.Count ?? 0);
            for (int i = 0; i < (source?.Count ?? 0); i++)
            {
                SkillNodeRankEntry entry = source[i];
                if (entry != null)
                    result.Add(new SkillNodeRankEntry
                    {
                        nodeId = entry.nodeId,
                        rank = entry.rank,
                    });
            }
            return result;
        }

        private static List<SkillPointGrantEntry> CloneGrants(
            IReadOnlyList<SkillPointGrantEntry> source)
        {
            var result = new List<SkillPointGrantEntry>(source?.Count ?? 0);
            for (int i = 0; i < (source?.Count ?? 0); i++)
            {
                SkillPointGrantEntry grant = source[i];
                string sourceId = grant?.sourceId?.Trim();
                if (string.IsNullOrEmpty(sourceId) || grant.amount <= 0)
                    continue;

                SkillPointGrantEntry existing = FindGrant(result, sourceId);
                if (existing != null)
                {
                    existing.amount += grant.amount;
                    continue;
                }

                result.Add(new SkillPointGrantEntry
                {
                    sourceId = sourceId,
                    amount = grant.amount,
                });
            }
            return result;
        }

        private static SkillPointGrantEntry FindGrant(
            IReadOnlyList<SkillPointGrantEntry> grants,
            string sourceId)
        {
            for (int i = 0; i < (grants?.Count ?? 0); i++)
            {
                SkillPointGrantEntry grant = grants[i];
                if (grant != null
                    && string.Equals(
                        grant.sourceId,
                        sourceId,
                        StringComparison.Ordinal))
                {
                    return grant;
                }
            }
            return null;
        }

        private static int SumBonusPoints(
            IReadOnlyList<SkillPointGrantEntry> grants)
        {
            int total = 0;
            for (int i = 0; i < (grants?.Count ?? 0); i++)
                total += Mathf.Max(0, grants[i]?.amount ?? 0);
            return total;
        }

        private static void MigrateRanks(
            CharacterSkillTreeSO tree,
            int savedVersion,
            IReadOnlyList<SkillNodeRankEntry> ranks)
        {
            if (tree == null)
                return;
            for (int i = 0; i < (ranks?.Count ?? 0); i++)
            {
                SkillNodeRankEntry rank = ranks[i];
                if (rank != null)
                    rank.nodeId = tree.MigrateNodeId(rank.nodeId, savedVersion);
            }
        }
    }
}
