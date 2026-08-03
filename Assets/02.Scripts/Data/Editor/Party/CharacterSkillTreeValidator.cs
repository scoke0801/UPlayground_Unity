#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;

namespace UPlayGround.Data.Editor.Party
{
    public static class CharacterSkillTreeValidator
    {
        private static readonly CharacterActorType[] RequiredP0Characters =
        {
            CharacterActorType.Honoka,
            CharacterActorType.Bokusei,
            CharacterActorType.Hichi,
        };

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/검증/Character Skill Trees")]
        public static void ValidateAllMenu()
        {
            List<string> issues = ValidateAll();
            if (issues.Count == 0)
            {
                Debug.Log("[CharacterSkillTreeValidator] 검증 성공");
                return;
            }

            for (int i = 0; i < issues.Count; i++)
                Debug.LogError($"[CharacterSkillTreeValidator] {issues[i]}");
            Debug.LogError($"[CharacterSkillTreeValidator] 오류 {issues.Count}건");
        }

        public static List<string> ValidateAll()
        {
            var issues = new List<string>();
            List<CharacterSkillTreeSO> trees = LoadAll<CharacterSkillTreeSO>();
            var byCharacter = new Dictionary<CharacterActorType, CharacterSkillTreeSO>();
            var abilityIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (GameplayAbilitySO ability in LoadAll<GameplayAbilitySO>())
                if (ability != null && !string.IsNullOrWhiteSpace(ability.abilityId))
                    abilityIds.Add(ability.abilityId.Trim());

            for (int i = 0; i < trees.Count; i++)
            {
                CharacterSkillTreeSO tree = trees[i];
                if (tree.characterType == CharacterActorType.None)
                    issues.Add($"{AssetDatabase.GetAssetPath(tree)}: characterType이 None입니다.");
                else if (!byCharacter.TryAdd(tree.characterType, tree))
                    issues.Add($"{tree.characterType}: 스킬 트리가 중복입니다.");
                ValidateTree(tree, abilityIds, issues);
            }

            for (int i = 0; i < RequiredP0Characters.Length; i++)
                if (!byCharacter.ContainsKey(RequiredP0Characters[i]))
                    issues.Add($"P0 캐릭터 {RequiredP0Characters[i]}의 CharacterSkillTreeSO가 없습니다.");

            ValidatePartyConfigs(byCharacter, issues);
            return issues;
        }

        private static void ValidateTree(
            CharacterSkillTreeSO tree,
            HashSet<string> abilityIds,
            List<string> issues)
        {
            string path = AssetDatabase.GetAssetPath(tree);
            var nodes = new Dictionary<string, SkillNodeDefinition>(StringComparer.Ordinal);
            if (tree.nodes == null || tree.nodes.Count == 0)
            {
                issues.Add($"{path}: 노드가 없습니다.");
                return;
            }

            for (int i = 0; i < tree.nodes.Count; i++)
            {
                SkillNodeDefinition node = tree.nodes[i];
                string id = node?.NormalizedId;
                if (node == null || string.IsNullOrWhiteSpace(id))
                {
                    issues.Add($"{path}: {i}번 nodeId가 비어 있습니다.");
                    continue;
                }
                if (!nodes.TryAdd(id, node))
                    issues.Add($"{path}: nodeId '{id}'가 중복입니다.");
                if (node.cost <= 0)
                    issues.Add($"{path}/{id}: cost는 1 이상이어야 합니다.");
                if (node.maxRank <= 0)
                    issues.Add($"{path}/{id}: maxRank는 1 이상이어야 합니다.");
                ValidateEffects(path, id, node.effects, abilityIds, issues);
            }

            foreach (KeyValuePair<string, SkillNodeDefinition> pair in nodes)
            {
                List<string> prerequisites = pair.Value.requiredNodeIds;
                if (prerequisites == null) continue;
                for (int i = 0; i < prerequisites.Count; i++)
                {
                    string required = prerequisites[i]?.Trim();
                    if (string.IsNullOrEmpty(required) || !nodes.ContainsKey(required))
                        issues.Add($"{path}/{pair.Key}: 존재하지 않는 선행 노드 '{prerequisites[i]}'.");
                }
            }

            var visiting = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in nodes.Keys)
                DetectCycle(path, id, nodes, visiting, visited, issues);
        }

        private static void ValidateEffects(
            string path,
            string nodeId,
            List<SkillNodeEffect> effects,
            HashSet<string> abilityIds,
            List<string> issues)
        {
            if (effects == null) return;
            for (int i = 0; i < effects.Count; i++)
            {
                switch (effects[i])
                {
                    case null:
                        issues.Add($"{path}/{nodeId}: {i}번 효과가 null입니다.");
                        break;
                    case StatDeltaEffect stat when !stat.AttributeId.IsValid:
                        issues.Add($"{path}/{nodeId}: StatDeltaEffect Attribute ID가 비어 있습니다.");
                        break;
                    case StatDeltaEffect stat when stat.operation == AttributeModifierOperation.Override:
                        issues.Add($"{path}/{nodeId}: 스킬 트리는 base 오염 방지를 위해 Override를 사용할 수 없습니다.");
                        break;
                    case AbilityScalarEffect scalar when string.IsNullOrWhiteSpace(scalar.abilityId)
                                                       || !abilityIds.Contains(scalar.abilityId.Trim()):
                        issues.Add($"{path}/{nodeId}: AbilityScalarEffect abilityId '{scalar.abilityId}'를 해석할 수 없습니다.");
                        break;
                    case AbilityUnlockEffect unlock when string.IsNullOrWhiteSpace(unlock.abilityId)
                                                       || !abilityIds.Contains(unlock.abilityId.Trim()):
                        issues.Add($"{path}/{nodeId}: AbilityUnlockEffect abilityId '{unlock.abilityId}'를 해석할 수 없습니다.");
                        break;
                    case PassiveGrantEffect grant when grant.passive == null:
                        issues.Add($"{path}/{nodeId}: PassiveGrantEffect passive가 비어 있습니다.");
                        break;
                }
            }
        }

        private static void DetectCycle(
            string path,
            string id,
            Dictionary<string, SkillNodeDefinition> nodes,
            HashSet<string> visiting,
            HashSet<string> visited,
            List<string> issues)
        {
            if (visited.Contains(id)) return;
            if (!visiting.Add(id))
            {
                issues.Add($"{path}: '{id}'에서 선행 노드 순환 참조가 발견됐습니다.");
                return;
            }

            List<string> required = nodes[id].requiredNodeIds;
            if (required != null)
                for (int i = 0; i < required.Count; i++)
                {
                    string next = required[i]?.Trim();
                    if (!string.IsNullOrEmpty(next) && nodes.ContainsKey(next))
                        DetectCycle(path, next, nodes, visiting, visited, issues);
                }
            visiting.Remove(id);
            visited.Add(id);
        }

        private static void ValidatePartyConfigs(
            Dictionary<CharacterActorType, CharacterSkillTreeSO> trees,
            List<string> issues)
        {
            foreach (PartyConfigSO config in LoadAll<PartyConfigSO>())
            {
                if (config == null) continue;
                var linkedCharacters = new HashSet<CharacterActorType>();
                if (config.characterSkillTrees != null)
                    for (int i = 0; i < config.characterSkillTrees.Count; i++)
                    {
                        CharacterSkillTreeSO linked = config.characterSkillTrees[i];
                        if (linked != null)
                            linkedCharacters.Add(linked.characterType);
                    }
                for (int i = 0; i < RequiredP0Characters.Length; i++)
                    if (!linkedCharacters.Contains(RequiredP0Characters[i]))
                        issues.Add(
                            $"{AssetDatabase.GetAssetPath(config)}: P0 캐릭터 " +
                            $"{RequiredP0Characters[i]} 스킬 트리가 연결되지 않았습니다.");
                int levelCap = 0;
                if (config.growthData != null && config.growthData.Count > 0)
                    for (int i = 0; i < config.growthData.Count; i++)
                        if (config.growthData[i] != null)
                            levelCap = Mathf.Max(levelCap, config.growthData[i].levelCap);
                if (levelCap <= 0)
                    levelCap = 100;
                int totalPoints = (config.skillPointRule ?? new SkillPointRule())
                    .TotalPointsAtLevel(levelCap);
                foreach (CharacterSkillTreeSO tree in trees.Values)
                {
                    int totalCost = 0;
                    int cheapestRootCost = int.MaxValue;
                    if (tree.nodes != null)
                        for (int i = 0; i < tree.nodes.Count; i++)
                            if (tree.nodes[i] != null)
                            {
                                totalCost += Mathf.Max(1, tree.nodes[i].cost)
                                             * Mathf.Max(1, tree.nodes[i].maxRank);
                                if (tree.nodes[i].requiredNodeIds == null
                                    || tree.nodes[i].requiredNodeIds.Count == 0)
                                    cheapestRootCost = Mathf.Min(
                                        cheapestRootCost,
                                        Mathf.Max(1, tree.nodes[i].cost));
                            }
                    if (cheapestRootCost == int.MaxValue
                        || totalPoints < cheapestRootCost)
                        issues.Add(
                            $"{tree.name}: 레벨 상한 포인트({totalPoints})로 취득 가능한 루트 노드가 없습니다.");
                    if (totalCost <= totalPoints)
                        Debug.LogWarning(
                            $"[CharacterSkillTreeValidator] {tree.name}: 레벨 상한 포인트({totalPoints})로 " +
                            $"전체 노드({totalCost})를 취득할 수 있어 선택성이 약해질 수 있습니다.",
                            tree);
                }
            }
        }

        private static List<T> LoadAll<T>() where T : UnityEngine.Object
        {
            var result = new List<T>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            for (int i = 0; i < guids.Length; i++)
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                if (asset != null) result.Add(asset);
            }
            return result;
        }
    }
}
#endif
