#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data;
using UPlayGround.Data.Crafting;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.Path;
using UPlayGround.Data.Quest;
using UPlayGround.Data.Stat;
using UPlayGround.Dialogue;
using UPlayGround.Data.Item;
using UPlayGround.Story;

namespace UPlayGround.Tool.Editor.Validation
{
    public static class GeneralDataValidator
    {
        public static List<EditorValidationIssue> ValidateAll()
        {
            var issues = new List<EditorValidationIssue>();
            var itemIds = BuildItemIdSet(issues);
            var recipeIds = ValidateRecipes(issues, itemIds);
            ValidateQuests(issues, itemIds, recipeIds);
            ValidateDialogue(issues);
            ValidateStory(issues);
            ValidateParty(issues);
            ValidateCamera(issues);
            return issues;
        }

        private static HashSet<int> BuildItemIdSet(List<EditorValidationIssue> issues)
        {
            var ids = new HashSet<int>();
            var owners = new Dictionary<int, UnityEngine.Object>();

            foreach (string guid in AssetDatabase.FindAssets("t:ItemSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<ItemSO>(path);
                if (item == null)
                    continue;

                if (item.itemId <= 0)
                    Add(issues, EditorValidationSeverity.Warning, "Item", path, item, "itemId", "itemId가 0 이하입니다.", "ItemIdType/DB 조회 대상이면 양수 ID를 권장합니다.");

                if (!ids.Add(item.itemId))
                    Add(issues, EditorValidationSeverity.Error, "Item", path, item, "itemId", $"itemId가 중복됩니다: {item.itemId}", $"기존 에셋: {AssetDatabase.GetAssetPath(owners[item.itemId])}");
                else
                    owners[item.itemId] = item;

                if (string.IsNullOrWhiteSpace(item.itemName))
                    Add(issues, EditorValidationSeverity.Warning, "Item", path, item, "itemName", "아이템 이름이 비어 있습니다.", "UI 표시용 이름을 채우세요.");

                if (item is EquipmentSO equipment)
                {
                    if (equipment.itemType != ItemType.EQUIPMENT)
                        Add(issues, EditorValidationSeverity.Warning, "Item", path, item, "itemType", "EquipmentSO인데 itemType이 EQUIPMENT가 아닙니다.", "장비 필터/DB 조회 의도와 맞는지 확인하세요.");
                    if (equipment.weaponType != WeaponType.NoWeapon && equipment.equipmentPrefab == null)
                        Add(issues, EditorValidationSeverity.Warning, "Item", path, item, "equipmentPrefab", "무기 장비인데 equipmentPrefab이 비어 있습니다.", "실제 장착 모델이 필요한 장비인지 확인하세요.");
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:ItemDatabase"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);
                if (database == null)
                    continue;

                var registered = new HashSet<int>();
                foreach (ItemSO item in database.AllItems)
                {
                    if (item == null)
                    {
                        Add(issues, EditorValidationSeverity.Warning, "Item", path, database, "allItems", "ItemDatabase에 Missing 항목이 있습니다.", "ItemDatabase 갱신 또는 수동 정리를 실행하세요.");
                        continue;
                    }

                    registered.Add(item.itemId);
                }

                foreach (int id in ids)
                {
                    if (!registered.Contains(id))
                        Add(issues, EditorValidationSeverity.Warning, "Item", path, database, "allItems", $"ItemSO가 ItemDatabase에 등록되어 있지 않습니다: {id}", "ItemDatabase를 갱신하세요.");
                }
            }

            return ids;
        }

        private static HashSet<int> ValidateRecipes(List<EditorValidationIssue> issues, HashSet<int> itemIds)
        {
            var allRecipeIds = new HashSet<int>();

            foreach (string guid in AssetDatabase.FindAssets("t:RecipeDatabase"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var database = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(path);
                if (database == null)
                    continue;

                var recipeIds = new HashSet<int>();
                foreach (RecipeData recipe in database.AllRecipes)
                {
                    if (recipe == null)
                    {
                        Add(issues, EditorValidationSeverity.Error, "Recipe", path, database, "recipes", "RecipeDatabase에 null RecipeData가 있습니다.", "임포트 데이터를 다시 생성하세요.");
                        continue;
                    }

                    allRecipeIds.Add(recipe.recipeID);
                    if (!recipeIds.Add(recipe.recipeID))
                        Add(issues, EditorValidationSeverity.Error, "Recipe", path, database, "recipeID", $"recipeID가 중복됩니다: {recipe.recipeID}", "RecipeDatabase.Initialize()의 ToDictionary에서 예외가 발생할 수 있습니다.");
                    if (recipe.recipeID <= 0)
                        Add(issues, EditorValidationSeverity.Warning, "Recipe", path, database, "recipeID", "recipeID가 0 이하입니다.", "RecipeIdType 생성/조회 대상이면 양수 ID를 권장합니다.");
                    if (string.IsNullOrWhiteSpace(recipe.recipeName))
                        Add(issues, EditorValidationSeverity.Warning, "Recipe", path, database, "recipeName", $"레시피 이름이 비어 있습니다: {recipe.recipeID}", "제작 UI 표시명을 채우세요.");
                    if (recipe.resultQuantity <= 0)
                        Add(issues, EditorValidationSeverity.Error, "Recipe", path, database, "resultQuantity", $"결과 수량이 0 이하입니다: {recipe.recipeID}", "1 이상의 수량을 사용하세요.");
                    if (recipe.costAmount < 0)
                        Add(issues, EditorValidationSeverity.Error, "Recipe", path, database, "costAmount", $"제작 비용이 음수입니다: {recipe.recipeID}", "0 이상의 비용을 사용하세요.");
                    if (recipe.castTimeSeconds < 0f)
                        Add(issues, EditorValidationSeverity.Error, "Recipe", path, database, "castTimeSeconds", $"제작 시간이 음수입니다: {recipe.recipeID}", "0 이상의 시간을 사용하세요.");
                    if (recipe.resultItemID != 0 && !itemIds.Contains(recipe.resultItemID))
                        Add(issues, EditorValidationSeverity.Warning, "Recipe", path, database, "resultItemID", $"결과 아이템 ID를 찾을 수 없습니다: {recipe.resultItemID}", "ItemSO/ItemDatabase 등록 상태를 확인하세요.");
                }

                var ingredientRecipeIds = new HashSet<int>();
                foreach (IngredientData ingredient in database.AllIngredients)
                {
                    if (ingredient == null)
                        continue;

                    ingredientRecipeIds.Add(ingredient.recipeID);
                    if (!recipeIds.Contains(ingredient.recipeID))
                        Add(issues, EditorValidationSeverity.Error, "Recipe", path, database, "ingredients.recipeID", $"존재하지 않는 recipeID의 재료입니다: {ingredient.recipeID}", "RecipeData를 추가하거나 재료 데이터를 제거하세요.");
                    if (ingredient.requiredQuantity <= 0)
                        Add(issues, EditorValidationSeverity.Error, "Recipe", path, database, "requiredQuantity", $"재료 수량이 0 이하입니다: recipe {ingredient.recipeID}", "1 이상의 수량을 사용하세요.");
                    if (!itemIds.Contains(ingredient.ingredientItemID))
                        Add(issues, EditorValidationSeverity.Warning, "Recipe", path, database, "ingredientItemID", $"재료 아이템 ID를 찾을 수 없습니다: {ingredient.ingredientItemID}", "ItemSO/ItemDatabase 등록 상태를 확인하세요.");
                }

                foreach (RecipeData recipe in database.AllRecipes)
                {
                    if (recipe != null && !ingredientRecipeIds.Contains(recipe.recipeID))
                        Add(issues, EditorValidationSeverity.Info, "Recipe", path, database, "ingredients", $"재료가 없는 레시피입니다: {recipe.recipeID}", "무료/디버그 레시피가 아니라면 재료 데이터를 추가하세요.");
                }

                var unlockIds = new HashSet<int>();
                foreach (RecipeUnlockCondition condition in database.AllUnlockConditions)
                {
                    if (condition == null)
                        continue;

                    if (!recipeIds.Contains(condition.recipeID))
                        Add(issues, EditorValidationSeverity.Error, "Recipe", path, database, "unlockConditions.recipeID", $"존재하지 않는 recipeID의 언락 조건입니다: {condition.recipeID}", "RecipeData를 추가하거나 조건을 제거하세요.");
                    if (!unlockIds.Add(condition.recipeID))
                        Add(issues, EditorValidationSeverity.Warning, "Recipe", path, database, "unlockConditions", $"언락 조건 recipeID가 중복됩니다: {condition.recipeID}", "런타임은 첫 조건만 사용할 수 있습니다.");
                    if (condition.conditionType is UnlockConditionType.ItemCollect or UnlockConditionType.ItemHave
                        && !itemIds.Contains(condition.conditionValue))
                        Add(issues, EditorValidationSeverity.Warning, "Recipe", path, database, "conditionValue", $"언락 조건 아이템 ID를 찾을 수 없습니다: {condition.conditionValue}", "ItemSO/ItemDatabase 등록 상태를 확인하세요.");
                    if (condition.conditionType == UnlockConditionType.RecipeCraft
                        && !recipeIds.Contains(condition.conditionValue))
                        Add(issues, EditorValidationSeverity.Warning, "Recipe", path, database, "conditionValue", $"언락 조건 레시피 ID를 찾을 수 없습니다: {condition.conditionValue}", "선행 레시피 ID를 확인하세요.");
                    if (condition.conditionType == UnlockConditionType.MonsterKill
                        && string.IsNullOrWhiteSpace(condition.conditionStringValue) && condition.conditionValue <= 0)
                        Add(issues, EditorValidationSeverity.Warning, "Recipe", path, database, "conditionStringValue", $"MonsterKill 언락 조건의 Actor ID가 비어 있습니다: {condition.recipeID}", "MonsterActor.ActorId를 conditionStringValue에 지정하세요. 기존 숫자 ID 데이터는 conditionValue로도 동작합니다.");
                    if (condition.conditionType != UnlockConditionType.None && condition.conditionValue2 < 0)
                        Add(issues, EditorValidationSeverity.Warning, "Recipe", path, database, "conditionValue2", $"언락 조건 수량/횟수가 음수입니다: {condition.recipeID}", "0 이상 값을 사용하세요.");
                }
            }

            return allRecipeIds;
        }

        private static void ValidateQuests(List<EditorValidationIssue> issues, HashSet<int> itemIds, HashSet<int> recipeIds)
        {
            var questIds = new Dictionary<string, QuestSO>();

            foreach (string guid in AssetDatabase.FindAssets("t:QuestSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var quest = AssetDatabase.LoadAssetAtPath<QuestSO>(path);
                if (quest == null)
                    continue;

                if (string.IsNullOrWhiteSpace(quest.questId))
                    Add(issues, EditorValidationSeverity.Error, "Quest", path, quest, "questId", "questId가 비어 있습니다.", "QuestDatabase 조회 키를 채우세요.");
                else if (questIds.TryGetValue(quest.questId, out QuestSO existing))
                    Add(issues, EditorValidationSeverity.Error, "Quest", path, quest, "questId", $"questId가 중복됩니다: {quest.questId}", $"기존 에셋: {AssetDatabase.GetAssetPath(existing)}");
                else
                    questIds.Add(quest.questId, quest);

                if (string.IsNullOrWhiteSpace(quest.questName))
                    Add(issues, EditorValidationSeverity.Warning, "Quest", path, quest, "questName", "퀘스트 이름이 비어 있습니다.", "퀘스트 UI 표시명을 채우세요.");

                var objectiveIds = new HashSet<string>();
                for (int i = 0; i < quest.objectives.Count; i++)
                {
                    QuestObjectiveData objective = quest.objectives[i];
                    if (objective == null)
                    {
                        Add(issues, EditorValidationSeverity.Error, "Quest", path, quest, $"objectives[{i}]", "목표 데이터가 null입니다.", "빈 목표를 제거하세요.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(objective.objectiveId))
                        Add(issues, EditorValidationSeverity.Warning, "Quest", path, quest, $"objectives[{i}].objectiveId", "목표 ID가 비어 있습니다.", "QuestSO 안에서 유일한 objectiveId를 지정하세요.");
                    else if (!objectiveIds.Add(objective.objectiveId))
                        Add(issues, EditorValidationSeverity.Error, "Quest", path, quest, $"objectives[{i}].objectiveId", $"목표 ID가 중복됩니다: {objective.objectiveId}", "QuestSO 내부 objectiveId는 유일해야 합니다.");

                    if (objective.requiredCount <= 0)
                        Add(issues, EditorValidationSeverity.Error, "Quest", path, quest, $"objectives[{i}].requiredCount", "목표 요구 수량이 0 이하입니다.", "1 이상의 값을 사용하세요.");

                    ValidateQuestObjectiveTarget(issues, itemIds, recipeIds, path, quest, i, objective);
                }

                if (quest.reward.gold < 0)
                    Add(issues, EditorValidationSeverity.Error, "Quest", path, quest, "reward.gold", "보상 골드가 음수입니다.", "0 이상의 값을 사용하세요.");
                if (quest.reward.exp < 0)
                    Add(issues, EditorValidationSeverity.Error, "Quest", path, quest, "reward.exp", "보상 경험치가 음수입니다.", "0 이상의 값을 사용하세요.");
                for (int i = 0; i < quest.reward.items.Count; i++)
                {
                    QuestItemReward reward = quest.reward.items[i];
                    if (reward == null)
                        continue;
                    if (reward.count <= 0)
                        Add(issues, EditorValidationSeverity.Error, "Quest", path, quest, $"reward.items[{i}].count", "보상 아이템 수량이 0 이하입니다.", "1 이상의 값을 사용하세요.");
                    if (!itemIds.Contains(reward.itemId))
                        Add(issues, EditorValidationSeverity.Warning, "Quest", path, quest, $"reward.items[{i}].itemId", $"보상 아이템 ID를 찾을 수 없습니다: {reward.itemId}", "ItemSO/ItemDatabase 등록 상태를 확인하세요.");
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:QuestDatabase"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var database = AssetDatabase.LoadAssetAtPath<QuestDatabase>(path);
                if (database == null)
                    continue;

                var registered = new HashSet<string>();
                foreach (QuestSO quest in database.GetAllQuests())
                {
                    if (quest == null)
                    {
                        Add(issues, EditorValidationSeverity.Warning, "Quest", path, database, "QuestList", "QuestDatabase에 Missing 항목이 있습니다.", "QuestDatabase를 갱신하세요.");
                        continue;
                    }
                    registered.Add(quest.questId);
                }

                foreach (KeyValuePair<string, QuestSO> pair in questIds)
                {
                    if (pair.Value.isContentEnabled && !registered.Contains(pair.Key))
                        Add(issues, EditorValidationSeverity.Error, "Quest", path, database, "QuestList", $"활성 QuestSO가 QuestDatabase에 등록되어 있지 않습니다: {pair.Key}", "QuestDatabase를 갱신하세요.");
                }
            }

            ValidateQuestReferences(issues, questIds);
        }

        private static void ValidateQuestReferences(
            List<EditorValidationIssue> issues,
            Dictionary<string, QuestSO> questIds)
        {
            foreach (KeyValuePair<string, QuestSO> pair in questIds)
            {
                QuestSO quest = pair.Value;
                string path = AssetDatabase.GetAssetPath(quest);
                ValidateQuestReferenceList(
                    issues,
                    questIds,
                    quest,
                    path,
                    "requiredQuestIds",
                    quest.requiredQuestIds);
                ValidateQuestReferenceList(
                    issues,
                    questIds,
                    quest,
                    path,
                    "autoAcceptNextQuestIds",
                    quest.autoAcceptNextQuestIds);
                ValidateObjectiveRevealDependencies(issues, quest, path);
            }

            ValidateQuestReferenceCycles(
                issues,
                questIds,
                "requiredQuestIds",
                quest => quest.requiredQuestIds);
            ValidateQuestReferenceCycles(
                issues,
                questIds,
                "autoAcceptNextQuestIds",
                quest => quest.autoAcceptNextQuestIds);
        }

        private static void ValidateQuestReferenceList(
            List<EditorValidationIssue> issues,
            Dictionary<string, QuestSO> questIds,
            QuestSO owner,
            string path,
            string field,
            List<string> references)
        {
            if (references == null)
                return;

            EditorValidationSeverity brokenReferenceSeverity = owner.isContentEnabled
                ? EditorValidationSeverity.Error
                : EditorValidationSeverity.Warning;
            var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < references.Count; i++)
            {
                string targetId = references[i];
                if (string.IsNullOrWhiteSpace(targetId))
                {
                    Add(issues, brokenReferenceSeverity, "Quest", path, owner, $"{field}[{i}]", "퀘스트 참조 ID가 비어 있습니다.", "빈 참조를 제거하거나 유효한 Quest ID를 지정하세요.");
                    continue;
                }

                if (!uniqueIds.Add(targetId))
                    Add(issues, EditorValidationSeverity.Warning, "Quest", path, owner, field, $"퀘스트 참조가 중복됩니다: {targetId}", "중복 참조를 하나만 남기세요.");
                if (targetId == owner.questId)
                    Add(issues, brokenReferenceSeverity, "Quest", path, owner, field, "퀘스트가 자기 자신을 참조합니다.", "자기 참조를 제거하세요.");

                if (!questIds.TryGetValue(targetId, out QuestSO target))
                {
                    Add(issues, brokenReferenceSeverity, "Quest", path, owner, $"{field}[{i}]", $"참조한 Quest ID를 찾을 수 없습니다: {targetId}", "기존 ID를 확인하고 올바른 퀘스트를 지정하세요.");
                    continue;
                }

                if (owner.isContentEnabled && !target.isContentEnabled)
                    Add(issues, EditorValidationSeverity.Error, "Quest", path, owner, $"{field}[{i}]", $"활성 퀘스트가 비활성 퀘스트를 참조합니다: {targetId}", "대상 퀘스트를 완성해 활성화하거나 참조를 제거하세요.");
            }
        }

        private static void ValidateObjectiveRevealDependencies(
            List<EditorValidationIssue> issues,
            QuestSO quest,
            string path)
        {
            EditorValidationSeverity brokenReferenceSeverity = quest.isContentEnabled
                ? EditorValidationSeverity.Error
                : EditorValidationSeverity.Warning;
            var objectivesById = new Dictionary<string, QuestObjectiveData>(StringComparer.Ordinal);
            foreach (QuestObjectiveData objective in quest.objectives)
            {
                if (objective != null && !string.IsNullOrWhiteSpace(objective.objectiveId))
                    objectivesById[objective.objectiveId] = objective;
            }

            foreach (QuestObjectiveData objective in quest.objectives)
            {
                if (objective?.revealAfterObjectiveIds == null)
                    continue;

                var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < objective.revealAfterObjectiveIds.Count; i++)
                {
                    string prerequisiteId = objective.revealAfterObjectiveIds[i];
                    string field = $"objectives[{objective.objectiveId}].revealAfterObjectiveIds[{i}]";
                    if (string.IsNullOrWhiteSpace(prerequisiteId))
                        Add(issues, brokenReferenceSeverity, "Quest", path, quest, field, "표시 선행 목표 ID가 비어 있습니다.", "빈 참조를 제거하세요.");
                    else if (prerequisiteId == objective.objectiveId)
                        Add(issues, brokenReferenceSeverity, "Quest", path, quest, field, "목표가 자기 자신을 표시 선행 조건으로 참조합니다.", "자기 참조를 제거하세요.");
                    else if (!objectivesById.ContainsKey(prerequisiteId))
                        Add(issues, brokenReferenceSeverity, "Quest", path, quest, field, $"표시 선행 목표를 찾을 수 없습니다: {prerequisiteId}", "같은 QuestSO의 objectiveId를 지정하세요.");
                    else if (!uniqueIds.Add(prerequisiteId))
                        Add(issues, EditorValidationSeverity.Warning, "Quest", path, quest, field, $"표시 선행 목표가 중복됩니다: {prerequisiteId}", "중복 참조를 하나만 남기세요.");
                }
            }

            var visiting = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (string objectiveId in objectivesById.Keys)
            {
                if (HasObjectiveRevealCycle(objectiveId, objectivesById, visiting, visited))
                {
                    Add(issues, brokenReferenceSeverity, "Quest", path, quest, "objectives.revealAfterObjectiveIds", $"목표 표시 선행 조건에 순환이 있습니다: {objectiveId}", "순환 참조를 끊으세요.");
                    break;
                }
            }
        }

        private static bool HasObjectiveRevealCycle(
            string objectiveId,
            Dictionary<string, QuestObjectiveData> objectivesById,
            HashSet<string> visiting,
            HashSet<string> visited)
        {
            if (visited.Contains(objectiveId))
                return false;
            if (!visiting.Add(objectiveId))
                return true;

            QuestObjectiveData objective = objectivesById[objectiveId];
            foreach (string prerequisiteId in objective.revealAfterObjectiveIds ?? new List<string>())
            {
                if (objectivesById.ContainsKey(prerequisiteId)
                    && HasObjectiveRevealCycle(prerequisiteId, objectivesById, visiting, visited))
                {
                    return true;
                }
            }

            visiting.Remove(objectiveId);
            visited.Add(objectiveId);
            return false;
        }

        private static void ValidateQuestReferenceCycles(
            List<EditorValidationIssue> issues,
            Dictionary<string, QuestSO> questIds,
            string field,
            Func<QuestSO, List<string>> selector)
        {
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, QuestSO> pair in questIds)
            {
                if (!pair.Value.isContentEnabled)
                    continue;
                if (!HasQuestReferenceCycle(pair.Key, questIds, selector, visiting, visited))
                    continue;

                string path = AssetDatabase.GetAssetPath(pair.Value);
                Add(issues, EditorValidationSeverity.Error, "Quest", path, pair.Value, field, $"활성 퀘스트 참조에 순환이 있습니다: {pair.Key}", "선행 또는 자동 연계 순환을 끊으세요.");
                break;
            }
        }

        private static bool HasQuestReferenceCycle(
            string questId,
            Dictionary<string, QuestSO> questIds,
            Func<QuestSO, List<string>> selector,
            HashSet<string> visiting,
            HashSet<string> visited)
        {
            if (visited.Contains(questId))
                return false;
            if (!visiting.Add(questId))
                return true;
            if (!questIds.TryGetValue(questId, out QuestSO quest) || !quest.isContentEnabled)
            {
                visiting.Remove(questId);
                visited.Add(questId);
                return false;
            }

            foreach (string targetId in selector(quest) ?? new List<string>())
            {
                if (questIds.TryGetValue(targetId, out QuestSO target)
                    && target.isContentEnabled
                    && HasQuestReferenceCycle(targetId, questIds, selector, visiting, visited))
                {
                    return true;
                }
            }

            visiting.Remove(questId);
            visited.Add(questId);
            return false;
        }

        private static void ValidateQuestObjectiveTarget(
            List<EditorValidationIssue> issues,
            HashSet<int> itemIds,
            HashSet<int> recipeIds,
            string path,
            QuestSO quest,
            int index,
            QuestObjectiveData objective)
        {
            switch (objective.type)
            {
                case QuestObjectiveType.ItemCollect:
                case QuestObjectiveType.ItemDeliver:
                case QuestObjectiveType.ItemUse:
                case QuestObjectiveType.ItemEnhance:
                    if (!itemIds.Contains(objective.targetId))
                        Add(issues, EditorValidationSeverity.Warning, "Quest", path, quest, $"objectives[{index}].targetId", $"목표 아이템 ID를 찾을 수 없습니다: {objective.targetId}", "ItemSO/ItemDatabase 등록 상태를 확인하세요.");
                    break;
                case QuestObjectiveType.ItemCraft:
                    if (!recipeIds.Contains(objective.targetId))
                        Add(issues, EditorValidationSeverity.Warning, "Quest", path, quest, $"objectives[{index}].targetId", $"목표 레시피 ID를 찾을 수 없습니다: {objective.targetId}", "RecipeDatabase 등록 상태를 확인하세요.");
                    break;
                case QuestObjectiveType.MonsterKill:
                    if (string.IsNullOrWhiteSpace(objective.targetStringId) && objective.targetId <= 0)
                        Add(issues, EditorValidationSeverity.Warning, "Quest", path, quest, $"objectives[{index}].targetStringId", "MonsterKill 목표의 Actor ID가 비어 있습니다.", "MonsterActor.ActorId를 targetStringId에 지정하세요. 기존 숫자 ID 데이터는 targetId로도 동작합니다.");
                    break;
                case QuestObjectiveType.ReachLocation:
                    if (string.IsNullOrWhiteSpace(objective.targetStringId))
                        Add(issues, EditorValidationSeverity.Error, "Quest", path, quest, $"objectives[{index}].targetStringId", "ReachLocation 목표의 targetStringId가 비어 있습니다.", "위치 ID를 지정하세요.");
                    break;
            }
        }

        private static void ValidateDialogue(List<EditorValidationIssue> issues)
        {
            var graphIds = new Dictionary<string, DialogueGraphSO>(StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets("t:DialogueGraphSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var graph = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(path);
                if (graph == null)
                    continue;

                if (string.IsNullOrWhiteSpace(graph.graphId))
                    Add(issues, EditorValidationSeverity.Warning, "Dialogue", path, graph, "graphId", "graphId가 비어 있습니다.", "대화 시작/저장/디버그 조회 키가 필요하면 채우세요.");
                else if (graphIds.TryGetValue(graph.graphId, out DialogueGraphSO existingGraph))
                    Add(issues, EditorValidationSeverity.Error, "Dialogue", path, graph, "graphId", $"graphId가 중복됩니다: {graph.graphId}", $"기존 에셋: {AssetDatabase.GetAssetPath(existingGraph)}");
                else
                    graphIds.Add(graph.graphId, graph);

                var nodeIds = new HashSet<string>();
                for (int i = 0; i < graph.nodes.Count; i++)
                {
                    DialogueNodeSO node = graph.nodes[i];
                    if (node == null)
                    {
                        Add(issues, EditorValidationSeverity.Error, "Dialogue", path, graph, $"nodes[{i}]", "DialogueGraphSO에 Missing 노드가 있습니다.", "Dialogue Graph Editor에서 노드를 정리하세요.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(node.nodeId))
                        Add(issues, EditorValidationSeverity.Error, "Dialogue", AssetDatabase.GetAssetPath(node), node, "nodeId", "DialogueNodeSO.nodeId가 비어 있습니다.", "Dialogue Graph Editor에서 ID를 재부여하세요.");
                    else if (!nodeIds.Add(node.nodeId))
                        Add(issues, EditorValidationSeverity.Error, "Dialogue", AssetDatabase.GetAssetPath(node), node, "nodeId", $"DialogueGraph 내부 nodeId가 중복됩니다: {node.nodeId}", "중복 노드 ID를 재생성하세요.");
                }

                if (string.IsNullOrWhiteSpace(graph.startNodeId))
                    Add(issues, EditorValidationSeverity.Error, "Dialogue", path, graph, "startNodeId", "시작 노드 ID가 비어 있습니다.", "대화 시작 노드를 지정하세요.");
                else if (!nodeIds.Contains(graph.startNodeId))
                    Add(issues, EditorValidationSeverity.Error, "Dialogue", path, graph, "startNodeId", $"startNodeId가 그래프 노드 목록에 없습니다: {graph.startNodeId}", "시작 노드 ID를 현재 그래프의 nodeId로 맞추세요.");

                for (int i = 0; i < graph.nodes.Count; i++)
                {
                    DialogueNodeSO node = graph.nodes[i];
                    if (node == null)
                        continue;
                    ValidateDialogueNodeLinks(issues, nodeIds, node);
                }
            }

            ValidateSpeakerBindings(issues);
        }

        private static void ValidateStory(List<EditorValidationIssue> issues)
        {
            var storyIds = new Dictionary<string, StoryEntrySO>(StringComparer.Ordinal);

            foreach (string guid in AssetDatabase.FindAssets("t:StoryEntrySO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var entry = AssetDatabase.LoadAssetAtPath<StoryEntrySO>(path);
                if (entry == null)
                    continue;

                if (string.IsNullOrWhiteSpace(entry.storyId))
                    Add(issues, EditorValidationSeverity.Error, "Story", path, entry, "storyId", "storyId가 비어 있습니다.", "기존 명명 규칙에 맞는 안정 ID를 지정하세요.");
                else if (storyIds.TryGetValue(entry.storyId, out StoryEntrySO existing))
                    Add(issues, EditorValidationSeverity.Error, "Story", path, entry, "storyId", $"storyId가 중복됩니다: {entry.storyId}", $"기존 에셋: {AssetDatabase.GetAssetPath(existing)}");
                else
                    storyIds.Add(entry.storyId, entry);

                if (entry.requiredProgress < 0)
                    Add(issues, EditorValidationSeverity.Error, "Story", path, entry, "requiredProgress", "requiredProgress가 음수입니다.", "0 이상의 진행도를 사용하세요.");
                if (entry.maxProgressExclusive > 0 && entry.maxProgressExclusive <= entry.requiredProgress)
                    Add(issues, EditorValidationSeverity.Error, "Story", path, entry, "maxProgressExclusive", "진행도 상한이 시작 진행도보다 크지 않습니다.", "requiredProgress보다 큰 상한을 지정하거나 0으로 비우세요.");

                bool hasGraph = entry.dialogueGraph != null;
                var variantProgresses = new HashSet<int>();
                StoryVariant[] variants = entry.variants ?? Array.Empty<StoryVariant>();
                for (int i = 0; i < variants.Length; i++)
                {
                    StoryVariant variant = variants[i];
                    if (variant == null)
                    {
                        Add(issues, EditorValidationSeverity.Error, "Story", path, entry, $"variants[{i}]", "스토리 변형이 null입니다.", "빈 변형을 제거하세요.");
                        continue;
                    }

                    if (variant.requiredProgress < 0)
                        Add(issues, EditorValidationSeverity.Error, "Story", path, entry, $"variants[{i}].requiredProgress", "변형 진행도가 음수입니다.", "0 이상의 진행도를 사용하세요.");
                    if (!variantProgresses.Add(variant.requiredProgress))
                        Add(issues, EditorValidationSeverity.Warning, "Story", path, entry, "variants", $"같은 진행도의 변형이 중복됩니다: {variant.requiredProgress}", "진행도별 변형을 하나만 남기세요.");
                    if (variant.dialogueGraph == null)
                        Add(issues, EditorValidationSeverity.Error, "Story", path, entry, $"variants[{i}].dialogueGraph", "변형의 대화 그래프가 비어 있습니다.", "실행할 DialogueGraphSO를 연결하세요.");
                    else
                        hasGraph = true;
                }

                if (!hasGraph)
                    Add(issues, EditorValidationSeverity.Error, "Story", path, entry, "dialogueGraph", "실행 가능한 대화 그래프가 없습니다.", "기본 그래프 또는 변형 그래프를 연결하세요.");
            }

            foreach (string guid in AssetDatabase.FindAssets("t:StorySequenceSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var sequence = AssetDatabase.LoadAssetAtPath<StorySequenceSO>(path);
                if (sequence == null)
                    continue;

                var sequenceIds = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < sequence.entries.Count; i++)
                {
                    StoryEntrySO entry = sequence.entries[i];
                    if (entry == null)
                    {
                        Add(issues, EditorValidationSeverity.Error, "Story", path, sequence, $"entries[{i}]", "StorySequence에 Missing 항목이 있습니다.", "빈 항목을 제거하거나 StoryEntrySO를 연결하세요.");
                        continue;
                    }

                    if (!sequenceIds.Add(entry.storyId))
                        Add(issues, EditorValidationSeverity.Error, "Story", path, sequence, "entries", $"StorySequence에 storyId가 중복됩니다: {entry.storyId}", "중복 엔트리를 제거하세요.");
                    if (entry.triggerMode != StoryTriggerMode.Auto)
                        Add(issues, EditorValidationSeverity.Warning, "Story", path, sequence, $"entries[{i}]", $"자동 시퀀스에 {entry.triggerMode} 엔트리가 포함되어 있습니다: {entry.storyId}", "자동 재생 대상이면 Auto로 바꾸고, NPC/영역 소유라면 시퀀스에서 제거하세요.");
                }
            }
        }

        private static void ValidateDialogueNodeLinks(List<EditorValidationIssue> issues, HashSet<string> nodeIds, DialogueNodeSO node)
        {
            string path = AssetDatabase.GetAssetPath(node);
            CheckNodeLink(issues, nodeIds, path, node, "nextNodeId", node.nextNodeId);
            CheckNodeLink(issues, nodeIds, path, node, "trueNextNodeId", node.trueNextNodeId);
            CheckNodeLink(issues, nodeIds, path, node, "falseNextNodeId", node.falseNextNodeId);

            if (node.nodeType == NodeType.Condition && node.condition == null)
                Add(issues, EditorValidationSeverity.Error, "Dialogue", path, node, "condition", "Condition 노드인데 condition이 비어 있습니다.", "ConditionSO를 연결하세요.");

            for (int i = 0; i < node.choices.Count; i++)
            {
                ChoiceData choice = node.choices[i];
                if (choice == null)
                    continue;
                if (string.IsNullOrWhiteSpace(choice.choiceText))
                    Add(issues, EditorValidationSeverity.Warning, "Dialogue", path, node, $"choices[{i}].choiceText", "선택지 텍스트가 비어 있습니다.", "플레이어에게 표시될 선택지 문구를 채우세요.");
                CheckNodeLink(issues, nodeIds, path, node, $"choices[{i}].nextNodeId", choice.nextNodeId);
            }
        }

        private static void CheckNodeLink(List<EditorValidationIssue> issues, HashSet<string> nodeIds, string path, UnityEngine.Object asset, string field, string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                return;
            if (!nodeIds.Contains(nodeId))
                Add(issues, EditorValidationSeverity.Error, "Dialogue", path, asset, field, $"연결 대상 nodeId를 찾을 수 없습니다: {nodeId}", "현재 DialogueGraphSO에 존재하는 nodeId로 연결하세요.");
        }

        private static void ValidateSpeakerBindings(List<EditorValidationIssue> issues)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:SpeakerActorBindingTableSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var table = AssetDatabase.LoadAssetAtPath<SpeakerActorBindingTableSO>(path);
                if (table == null)
                    continue;

                var serialized = new SerializedObject(table);
                SerializedProperty entries = serialized.FindProperty("entries");
                var speakerIds = new HashSet<string>();

                if (entries == null || !entries.isArray)
                    continue;

                for (int i = 0; i < entries.arraySize; i++)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                    string speakerId = entry.FindPropertyRelative("speakerId")?.stringValue;
                    string actorId = entry.FindPropertyRelative("actorId")?.stringValue;

                    if (string.IsNullOrWhiteSpace(speakerId) || string.IsNullOrWhiteSpace(actorId))
                        Add(issues, EditorValidationSeverity.Warning, "Dialogue", path, table, $"entries[{i}]", "speakerId 또는 actorId가 비어 있습니다.", "빈 바인딩은 런타임 맵에 등록되지 않습니다.");
                    else if (!speakerIds.Add(speakerId))
                        Add(issues, EditorValidationSeverity.Warning, "Dialogue", path, table, $"entries[{i}].speakerId", $"speakerId가 중복됩니다: {speakerId}", "중복 시 나중 항목이 덮어쓸 수 있습니다.");
                }
            }
        }

        private static void ValidateParty(List<EditorValidationIssue> issues)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:PartyConfigSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<PartyConfigSO>(path);
                if (config == null)
                    continue;

                if (config.maxBattleSize <= 0)
                    Add(issues, EditorValidationSeverity.Error, "Party", path, config, "maxBattleSize", "maxBattleSize가 0 이하입니다.", "1 이상의 값을 사용하세요.");

                var growthSet = new HashSet<CharacterActorType>();
                for (int i = 0; i < config.growthData.Count; i++)
                {
                    PartyMemberGrowthSO growth = config.growthData[i];
                    if (growth == null)
                    {
                        Add(issues, EditorValidationSeverity.Warning, "Party", path, config, $"growthData[{i}]", "growthData에 Missing 항목이 있습니다.", "빈 항목을 제거하세요.");
                        continue;
                    }

                    if (!growthSet.Add(growth.characterType))
                        Add(issues, EditorValidationSeverity.Warning, "Party", AssetDatabase.GetAssetPath(growth), growth, "characterType", $"성장 데이터 characterType이 중복됩니다: {growth.characterType}", "PartyConfigSO.growthData 중복을 확인하세요.");
                }
            }

            ValidatePartyMemberGrowthAssets(issues);
        }

        private static void ValidatePartyMemberGrowthAssets(List<EditorValidationIssue> issues)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:PartyMemberGrowthSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var growth = AssetDatabase.LoadAssetAtPath<PartyMemberGrowthSO>(path);
                if (growth == null)
                    continue;

                if (growth.characterType == CharacterActorType.None)
                    Add(issues, EditorValidationSeverity.Warning, "Party", path, growth, "characterType", "성장 데이터 characterType이 None입니다.", "대상 캐릭터 타입을 지정하세요.");
                if (growth.baseProfile == null)
                    Add(issues, EditorValidationSeverity.Error, "Party", path, growth, "baseProfile", "성장 데이터 baseProfile이 비어 있습니다.", "레벨 1 기준 Attribute Profile을 연결하세요.");
                if (growth.initialLevel > growth.levelCap)
                    Add(issues, EditorValidationSeverity.Error, "Party", path, growth, "initialLevel", "initialLevel이 levelCap보다 큽니다.", "초기 레벨과 레벨 상한을 조정하세요.");

            }
        }

        private static void ValidateCamera(List<EditorValidationIssue> issues)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:CameraShakeData"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<CameraShakeData>(path);
                if (data == null)
                    continue;

                if (string.IsNullOrWhiteSpace(data.key))
                    Add(issues, EditorValidationSeverity.Warning, "Camera", path, data, "key", "CameraShakeData.key가 비어 있습니다.", "CameraShakeDatabase 조회 키를 지정하세요.");
                if (data.Duration < 0f || data.Delay < 0f || data.Frequency < 0f)
                    Add(issues, EditorValidationSeverity.Error, "Camera", path, data, "timing", "Duration/Delay/Frequency 중 음수 값이 있습니다.", "0 이상의 값을 사용하세요.");
            }

            foreach (string guid in AssetDatabase.FindAssets("t:CameraShakeDatabase"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var database = AssetDatabase.LoadAssetAtPath<CameraShakeDatabase>(path);
                if (database == null)
                    continue;

                var keys = new HashSet<string>();
                foreach (CameraShakeData data in database.AllItems)
                {
                    if (data == null)
                    {
                        Add(issues, EditorValidationSeverity.Warning, "Camera", path, database, "AllItems", "CameraShakeDatabase에 Missing 항목이 있습니다.", "DB 항목을 정리하세요.");
                        continue;
                    }

                    if (!string.IsNullOrEmpty(data.key) && !keys.Add(data.key))
                        Add(issues, EditorValidationSeverity.Warning, "Camera", path, database, "AllItems", $"CameraShake key가 중복됩니다: {data.key}", "런타임은 먼저 등록된 항목만 조회합니다.");
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:CombatCameraProfileSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<CombatCameraProfileSO>(path);
                if (profile == null)
                    continue;

                if (!profile.HasPlayableContent())
                    Add(issues, EditorValidationSeverity.Info, "Camera", path, profile, "effects", "재생 가능한 카메라 효과가 없습니다.", "의도한 빈 프로필이 아니라면 effects/shake/punch/snapshot 설정을 추가하세요.");
                if (profile.useSnapshotSequence && profile.snapshotProfile == null)
                    Add(issues, EditorValidationSeverity.Warning, "Camera", path, profile, "snapshotProfile", "Snapshot Sequence를 사용하지만 snapshotProfile이 비어 있습니다.", "CameraSnapshotProfile을 연결하세요.");
            }
        }

        private static void Add(
            List<EditorValidationIssue> issues,
            EditorValidationSeverity severity,
            string domain,
            string path,
            UnityEngine.Object asset,
            string field,
            string message,
            string fixHint)
        {
            issues.Add(new EditorValidationIssue(severity, domain, path, asset, field, message, fixHint));
        }
    }
}
#endif
