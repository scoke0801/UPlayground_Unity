#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Party;

namespace UPlayGround.Data.Editor.Party
{
    /// <summary>
    /// 기존 PartyMemberGrowthSO의 고정 스탯 투자 정의를 최초 스킬 트리 저작 초안으로 변환한다.
    /// 기존 에셋을 덮어쓰거나 삭제하지 않으며, 생성 도중 실패하면 이번 실행에서 만든 에셋만 롤백한다.
    /// </summary>
    public static class CharacterSkillTreeMigrationTool
    {
        private const string OutputFolder = "Assets/10.Datas/Party/SkillTree";

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/데이터/성장/누락 Character Skill Tree 초안 생성")]
        public static void CreateMissingDrafts()
        {
            var createdPaths = new List<string>();
            var originalTreeLists = new Dictionary<PartyConfigSO, List<CharacterSkillTreeSO>>();
            try
            {
                EnsureFolder(OutputFolder);
                foreach (PartyConfigSO config in LoadAll<PartyConfigSO>())
                {
                    if (config == null || config.growthData == null)
                        continue;
                    originalTreeLists[config] = config.characterSkillTrees == null
                        ? null
                        : new List<CharacterSkillTreeSO>(config.characterSkillTrees);
                    config.characterSkillTrees ??= new List<CharacterSkillTreeSO>();
                    for (int i = 0; i < config.growthData.Count; i++)
                    {
                        PartyMemberGrowthSO growth = config.growthData[i];
                        if (growth == null || Find(config, growth.characterType) != null)
                            continue;

                        string path = $"{OutputFolder}/CharacterSkillTree_{growth.characterType}.asset";
                        CharacterSkillTreeSO tree = AssetDatabase.LoadAssetAtPath<CharacterSkillTreeSO>(path);
                        if (tree == null)
                        {
                            tree = ScriptableObject.CreateInstance<CharacterSkillTreeSO>();
                            tree.characterType = growth.characterType;
                            tree.nodes = BuildNodes(growth);
                            AssetDatabase.CreateAsset(tree, path);
                            createdPaths.Add(path);
                        }
                        config.characterSkillTrees.Add(tree);
                        EditorUtility.SetDirty(config);
                    }
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[CharacterSkillTreeMigrationTool] 신규 초안 {createdPaths.Count}개 생성 완료. " +
                          "Ability 해금/패시브/선행 그래프는 캐릭터별로 저작한 뒤 검증기를 실행하세요.");
            }
            catch (Exception exception)
            {
                foreach (KeyValuePair<PartyConfigSO, List<CharacterSkillTreeSO>> pair
                         in originalTreeLists)
                {
                    pair.Key.characterSkillTrees = pair.Value;
                    EditorUtility.SetDirty(pair.Key);
                }
                for (int i = createdPaths.Count - 1; i >= 0; i--)
                    AssetDatabase.DeleteAsset(createdPaths[i]);
                AssetDatabase.SaveAssets();
                Debug.LogException(exception);
                throw;
            }
        }

        private static List<SkillNodeDefinition> BuildNodes(PartyMemberGrowthSO growth)
        {
            var result = new List<SkillNodeDefinition>();
            if (growth.investmentRules == null)
                return result;
            for (int i = 0; i < growth.investmentRules.Count; i++)
            {
                GrowthInvestmentRule rule = growth.investmentRules[i];
                if (!rule.AttributeId.IsValid)
                    continue;
                result.Add(new SkillNodeDefinition
                {
                    nodeId = $"Stat.{rule.AttributeId.Value}",
                    displayNameKey = GrowthAttributeCatalog.GetDisplayName(rule.AttributeId),
                    descriptionKey = "기존 고정 스탯 투자에서 생성된 초안",
                    cost = 1,
                    maxRank = Mathf.Max(1, rule.maxRank),
                    layoutPosition = new Vector2(i * 240f, 0f),
                    effects = new List<SkillNodeEffect>
                    {
                        new StatDeltaEffect
                        {
                            attributeId = rule.AttributeId.Value,
                            operation = AttributeModifierOperation.Add,
                            valuePerRank = rule.flatPerRank,
                        },
                    },
                });
            }
            return result;
        }

        private static CharacterSkillTreeSO Find(
            PartyConfigSO config,
            UPlayGround.Data.EnumType.CharacterActorType type)
        {
            if (config.characterSkillTrees == null)
                return null;
            for (int i = 0; i < config.characterSkillTrees.Count; i++)
                if (config.characterSkillTrees[i] != null
                    && config.characterSkillTrees[i].characterType == type)
                    return config.characterSkillTrees[i];
            return null;
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

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            string[] parts = path.Split('/');
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
