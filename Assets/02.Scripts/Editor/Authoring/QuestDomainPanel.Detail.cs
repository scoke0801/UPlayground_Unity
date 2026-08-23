#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Editor.Authoring;
using UPlayGround.Data.Item;
using UPlayGround.Data.Quest;

namespace UPlayGround.Editor.Authoring
{
    public sealed partial class QuestDomainPanel
    {
        private static readonly Color[] ObjectiveColors =
        {
            new Color(0.35f, 0.75f, 0.35f),
            new Color(0.35f, 0.65f, 0.95f),
            new Color(0.90f, 0.75f, 0.20f),
            new Color(0.90f, 0.35f, 0.35f),
            new Color(0.75f, 0.45f, 0.90f),
            new Color(0.95f, 0.60f, 0.20f),
            new Color(0.30f, 0.80f, 0.80f),
            new Color(0.80f, 0.80f, 0.80f),
        };

        private static readonly string[] ObjectiveLabels =
        {
            "아이템 수집", "아이템 전달", "아이템 사용", "몬스터 처치",
            "스토리 진행", "아이템 제작", "아이템 강화", "위치 도달"
        };

        private static readonly string[] ObjectiveHints =
        {
            "NotifyItemCollected(itemId, count)",
            "NotifyItemDelivered(npcId, itemId, count)",
            "NotifyItemUsed(itemId, count)",
            "NotifyMonsterKill(actorId)",
            "NotifyStoryProgress(progress)",
            "NotifyItemCrafted(recipeId, quantity)",
            "NotifyItemEnhanced(itemId)",
            "NotifyLocationReached(locationId)"
        };

        protected override VisualElement BuildDetail(QuestSO quest)
        {
            var detail = new VisualElement();
            var serializedObject = new SerializedObject(quest);

            var header = new Toolbar();
            var title = new Label(quest.questName) { name = "detail-title" };
            title.style.fontSize = 13f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);
            var flexibleSpace = new VisualElement();
            flexibleSpace.style.flexGrow = 1f;
            header.Add(flexibleSpace);
            var path = new Label(AssetDatabase.GetAssetPath(quest));
            path.style.fontSize = 10f;
            header.Add(path);
            header.Add(new ToolbarButton(() => EditorGUIUtility.PingObject(quest)) { text = "Project에서 열기" });
            detail.Add(header);

            var duplicateWarning = new HelpBox(string.Empty, HelpBoxMessageType.Error);
            duplicateWarning.style.marginTop = 5f;
            detail.Add(duplicateWarning);

            var basicSection = MakeSection("기본 정보");
            AddProperty(basicSection, "questId", "퀘스트 ID");
            AddProperty(basicSection, "questName", "퀘스트 이름");
            AddProperty(basicSection, "questType", "분류(메인/서브)");
            AddProperty(basicSection, "shortSummary", "짧은 부제");
            AddProperty(basicSection, "questDescription", "설명");
            detail.Add(basicSection);

            var prerequisiteSection = MakeSection("선행 조건");
            AddProperty(prerequisiteSection, "requiredQuestIds", "완료 필요 퀘스트 ID");
            AddProperty(prerequisiteSection, "requiredStoryProgress", "필요 스토리 진행도");
            detail.Add(prerequisiteSection);

            var autoLinkSection = MakeSection("자동 연계");
            AddProperty(autoLinkSection, "autoAcceptOnNewGame", "새 게임 시작 시 자동 수락");
            AddProperty(autoLinkSection, "autoAcceptNextQuestIds", "완료 후 자동 수락 퀘스트 ID");
            detail.Add(autoLinkSection);

            BuildObjectivesSection(detail, serializedObject, quest);
            BuildRewardSection(detail, serializedObject, quest);

            var settingsSection = MakeSection("설정");
            AddProperty(settingsSection, "isRepeatable", "반복 퀘스트");
            AddProperty(settingsSection, "autoComplete", "자동 완료");
            var autoCompleteInfo = new HelpBox("목표를 모두 달성하면 즉시 자동 완료됩니다.", HelpBoxMessageType.Info);
            settingsSection.Add(autoCompleteInfo);
            detail.Add(settingsSection);

            SerializedProperty autoCompleteProperty = serializedObject.FindProperty("autoComplete");
            void UpdateAutoCompleteInfo(SerializedProperty property)
            {
                autoCompleteInfo.style.display = property.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
            }
            UpdateAutoCompleteInfo(autoCompleteProperty);
            settingsSection.TrackPropertyValue(autoCompleteProperty, UpdateAutoCompleteInfo);

            void RefreshDuplicateWarning()
            {
                bool duplicated = HasDuplicateKey(quest);
                duplicateWarning.text = $"Quest ID '{quest.questId}'가 다른 퀘스트와 중복됩니다.";
                duplicateWarning.style.display = duplicated ? DisplayStyle.Flex : DisplayStyle.None;
            }

            RefreshDuplicateWarning();
            detail.TrackSerializedObjectValue(serializedObject, _ =>
            {
                NotifyAssetChanged(quest);
                RefreshDuplicateWarning();
                title.text = quest.questName;
            });
            detail.Bind(serializedObject);
            return detail;
        }

        private void BuildObjectivesSection(
            VisualElement detail,
            SerializedObject serializedObject,
            QuestSO quest)
        {
            var section = MakeSection("목표");
            Label heading = section.Q<Label>(className: "section-title");
            var cardList = new VisualElement();
            var actionRow = new VisualElement();
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.justifyContent = Justify.FlexEnd;
            actionRow.Add(new Button(() =>
            {
                Undo.RecordObject(quest, "퀘스트 목표 추가");
                serializedObject.Update();
                AddNewObjective(serializedObject.FindProperty("objectives"));
                serializedObject.ApplyModifiedProperties();
                RebuildObjectiveCards(serializedObject, quest, cardList, heading);
                NotifyAssetChanged(quest);
            }) { text = "+ 목표 추가" });
            section.Insert(1, actionRow);
            section.Add(cardList);
            detail.Add(section);
            RebuildObjectiveCards(serializedObject, quest, cardList, heading);
        }

        private void RebuildObjectiveCards(
            SerializedObject serializedObject,
            QuestSO quest,
            VisualElement cardList,
            Label heading)
        {
            serializedObject.UpdateIfRequiredOrScript();
            cardList.Unbind();
            cardList.Clear();
            SerializedProperty objectives = serializedObject.FindProperty("objectives");
            if (heading != null)
                heading.text = $"목표 ({objectives.arraySize}개)";

            if (objectives.arraySize == 0)
            {
                cardList.Add(new HelpBox("목표가 없습니다.", HelpBoxMessageType.Info));
                return;
            }

            for (int i = 0; i < objectives.arraySize; i++)
                cardList.Add(BuildObjectiveCard(serializedObject, quest, cardList, heading, i));
            cardList.Bind(serializedObject);
        }

        private VisualElement BuildObjectiveCard(
            SerializedObject serializedObject,
            QuestSO quest,
            VisualElement cardList,
            Label heading,
            int index)
        {
            SerializedProperty objectives = serializedObject.FindProperty("objectives");
            SerializedProperty element = objectives.GetArrayElementAtIndex(index);
            string path = element.propertyPath;
            SerializedProperty typeProperty = element.FindPropertyRelative("type");
            int typeIndex = typeProperty.enumValueIndex;
            Color color = typeIndex >= 0 && typeIndex < ObjectiveColors.Length
                ? ObjectiveColors[typeIndex]
                : Color.gray;

            var card = new VisualElement();
            card.style.marginTop = 3f;
            card.style.marginBottom = 3f;
            card.style.paddingLeft = 8f;
            card.style.paddingRight = 8f;
            card.style.paddingTop = 6f;
            card.style.paddingBottom = 6f;
            card.style.backgroundColor = new Color(color.r, color.g, color.b, 0.12f);
            card.style.borderLeftWidth = 3f;
            card.style.borderLeftColor = color;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            var badge = new Label(typeIndex >= 0 && typeIndex < ObjectiveLabels.Length
                ? ObjectiveLabels[typeIndex]
                : ((QuestObjectiveType)typeIndex).ToString());
            badge.style.width = 76f;
            badge.style.height = 18f;
            badge.style.backgroundColor = color;
            badge.style.color = Color.white;
            badge.style.unityTextAlign = TextAnchor.MiddleCenter;
            badge.style.unityFontStyleAndWeight = FontStyle.Bold;
            badge.style.fontSize = 10f;
            header.Add(badge);

            var description = new Label();
            description.style.marginLeft = 5f;
            description.style.fontSize = 10f;
            SerializedProperty descriptionProperty = element.FindPropertyRelative("description");
            void UpdateDescription(SerializedProperty property)
            {
                description.text = string.IsNullOrEmpty(property.stringValue) ? "—" : property.stringValue;
            }
            UpdateDescription(descriptionProperty);
            card.TrackPropertyValue(descriptionProperty, UpdateDescription);
            header.Add(description);

            var flexibleSpace = new VisualElement();
            flexibleSpace.style.flexGrow = 1f;
            header.Add(flexibleSpace);

            void ApplyAndRebuild()
            {
                serializedObject.ApplyModifiedProperties();
                RebuildObjectiveCards(serializedObject, quest, cardList, heading);
                NotifyAssetChanged(quest);
            }

            var upButton = new Button(() =>
            {
                Undo.RecordObject(quest, "퀘스트 목표 순서 변경");
                serializedObject.Update();
                serializedObject.FindProperty("objectives").MoveArrayElement(index, index - 1);
                ApplyAndRebuild();
            }) { text = "▲" };
            upButton.style.width = 24f;
            upButton.SetEnabled(index > 0);
            header.Add(upButton);

            var downButton = new Button(() =>
            {
                Undo.RecordObject(quest, "퀘스트 목표 순서 변경");
                serializedObject.Update();
                serializedObject.FindProperty("objectives").MoveArrayElement(index, index + 1);
                ApplyAndRebuild();
            }) { text = "▼" };
            downButton.style.width = 24f;
            downButton.SetEnabled(index < objectives.arraySize - 1);
            header.Add(downButton);

            var removeButton = new Button(() =>
            {
                Undo.RecordObject(quest, "퀘스트 목표 삭제");
                serializedObject.Update();
                serializedObject.FindProperty("objectives").DeleteArrayElementAtIndex(index);
                ApplyAndRebuild();
            }) { text = "×" };
            removeButton.style.width = 24f;
            removeButton.style.color = new Color(1f, 0.5f, 0.5f);
            header.Add(removeButton);
            card.Add(header);

            AddProperty(card, $"{path}.objectiveId", "목표 ID");
            AddProperty(card, $"{path}.description", "설명");
            AddProperty(card, $"{path}.type", "타입");
            AddProperty(card, $"{path}.revealAfterObjectiveIds", "표시 선행 목표 ID");

            AddProperty(card, $"{path}.markerLocationId", "마커 위치 ID");
            AddProperty(card, $"{path}.markerIntent", "마커 성격");

            var conditionalFields = new VisualElement();
            card.Add(conditionalFields);
            BuildObjectiveConditionalFields(conditionalFields, serializedObject, quest, path, (QuestObjectiveType)typeIndex);
            card.TrackPropertyValue(typeProperty, _ =>
                cardList.schedule.Execute(() => RebuildObjectiveCards(serializedObject, quest, cardList, heading)));

            string hint = typeIndex >= 0 && typeIndex < ObjectiveHints.Length ? ObjectiveHints[typeIndex] : string.Empty;
            var hintLabel = new Label($"▶ QuestManager.{hint}");
            hintLabel.style.fontSize = 10f;
            hintLabel.style.marginTop = 2f;
            card.Add(hintLabel);
            return card;
        }

        private void BuildObjectiveConditionalFields(
            VisualElement container,
            SerializedObject serializedObject,
            QuestSO quest,
            string path,
            QuestObjectiveType type)
        {
            container.Clear();
            switch (type)
            {
                case QuestObjectiveType.ItemCollect:
                case QuestObjectiveType.ItemUse:
                case QuestObjectiveType.ItemEnhance:
                    AddItemTargetField(container, serializedObject, quest, path, "아이템 ID");
                    AddProperty(container, $"{path}.requiredCount", "필요 수량");
                    break;
                case QuestObjectiveType.ItemDeliver:
                    AddItemTargetField(container, serializedObject, quest, path, "아이템 ID");
                    AddProperty(container, $"{path}.npcId", "NPC ID");
                    AddProperty(container, $"{path}.requiredCount", "전달 수량");
                    break;
                case QuestObjectiveType.MonsterKill:
                    AddProperty(container, $"{path}.targetStringId", "Actor ID");
                    AddProperty(container, $"{path}.targetId", "레거시 숫자 ID");
                    AddProperty(container, $"{path}.requiredCount", "처치 수");
                    break;
                case QuestObjectiveType.StoryProgress:
                    AddProperty(container, $"{path}.targetId", "필요 진행도");
                    break;
                case QuestObjectiveType.ItemCraft:
                    AddProperty(container, $"{path}.targetId", "레시피 ID");
                    AddProperty(container, $"{path}.requiredCount", "제작 횟수");
                    break;
                case QuestObjectiveType.ReachLocation:
                    AddProperty(container, $"{path}.targetStringId", "위치 ID");
                    break;
            }
        }

        private void AddItemTargetField(
            VisualElement container,
            SerializedObject serializedObject,
            QuestSO quest,
            string objectivePath,
            string label)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            var propertyField = new PropertyField
            {
                bindingPath = $"{objectivePath}.targetId",
                label = label
            };
            propertyField.style.flexGrow = 1f;
            row.Add(propertyField);

            Button pickerButton = null;
            pickerButton = new Button(() =>
            {
                serializedObject.Update();
                int currentId = serializedObject.FindProperty($"{objectivePath}.targetId").intValue;
                SharedItemPicker.Show(pickerButton, FindItem(currentId), selectedItem =>
                {
                    Undo.RecordObject(quest, "퀘스트 목표 아이템 변경");
                    serializedObject.Update();
                    serializedObject.FindProperty($"{objectivePath}.targetId").intValue =
                        selectedItem != null ? selectedItem.itemId : 0;
                    serializedObject.ApplyModifiedProperties();
                    NotifyAssetChanged(quest);
                });
            }) { text = "아이템 선택" };
            row.Add(pickerButton);
            container.Add(row);
        }

        private static void AddNewObjective(SerializedProperty objectives)
        {
            objectives.InsertArrayElementAtIndex(objectives.arraySize);
            SerializedProperty element = objectives.GetArrayElementAtIndex(objectives.arraySize - 1);
            element.FindPropertyRelative("objectiveId").stringValue = $"obj_{objectives.arraySize}";
            element.FindPropertyRelative("description").stringValue = string.Empty;
            element.FindPropertyRelative("type").enumValueIndex = 0;
            element.FindPropertyRelative("targetId").intValue = 0;
            element.FindPropertyRelative("npcId").intValue = 0;
            element.FindPropertyRelative("targetStringId").stringValue = string.Empty;
            element.FindPropertyRelative("requiredCount").intValue = 1;
            SerializedProperty revealConditions = element.FindPropertyRelative("revealAfterObjectiveIds");
            if (revealConditions != null && revealConditions.isArray)
                revealConditions.ClearArray();
        }

        private void BuildRewardSection(
            VisualElement detail,
            SerializedObject serializedObject,
            QuestSO quest)
        {
            var section = MakeSection("보상");
            AddProperty(section, "reward.gold", "골드");
            AddProperty(section, "reward.exp", "경험치");

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginTop = 4f;
            var countLabel = new Label();
            countLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(countLabel);
            var flexibleSpace = new VisualElement();
            flexibleSpace.style.flexGrow = 1f;
            header.Add(flexibleSpace);
            var rows = new VisualElement();
            header.Add(new Button(() =>
            {
                Undo.RecordObject(quest, "퀘스트 보상 아이템 추가");
                serializedObject.Update();
                SerializedProperty items = serializedObject.FindProperty("reward.items");
                items.InsertArrayElementAtIndex(items.arraySize);
                SerializedProperty element = items.GetArrayElementAtIndex(items.arraySize - 1);
                element.FindPropertyRelative("itemId").intValue = 0;
                element.FindPropertyRelative("count").intValue = 1;
                serializedObject.ApplyModifiedProperties();
                RebuildRewardRows(serializedObject, quest, rows, countLabel);
                NotifyAssetChanged(quest);
            }) { text = "+ 아이템 추가" });
            section.Add(header);
            section.Add(rows);
            detail.Add(section);
            RebuildRewardRows(serializedObject, quest, rows, countLabel);
        }

        private void RebuildRewardRows(
            SerializedObject serializedObject,
            QuestSO quest,
            VisualElement rows,
            Label countLabel)
        {
            serializedObject.UpdateIfRequiredOrScript();
            rows.Unbind();
            rows.Clear();
            SerializedProperty items = serializedObject.FindProperty("reward.items");
            countLabel.text = $"보상 아이템 ({items.arraySize}개)";

            for (int i = 0; i < items.arraySize; i++)
            {
                int capturedIndex = i;
                SerializedProperty element = items.GetArrayElementAtIndex(i);
                int itemId = element.FindPropertyRelative("itemId").intValue;
                ItemSO item = FindItem(itemId);

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginTop = 2f;
                row.style.paddingLeft = 4f;
                row.style.paddingRight = 4f;
                row.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.1f);

                var icon = new Image { sprite = item != null ? item.icon : null, scaleMode = ScaleMode.ScaleToFit };
                icon.style.width = 26f;
                icon.style.height = 26f;
                icon.style.marginRight = 4f;
                row.Add(icon);

                var itemLabel = new Label(item != null ? $"[{item.itemId}] {item.itemName}" : $"미해결 ID: {itemId}");
                itemLabel.style.minWidth = 150f;
                if (item == null && itemId != 0)
                    itemLabel.style.color = new Color(1f, 0.5f, 0.4f);
                row.Add(itemLabel);

                Button pickerButton = null;
                pickerButton = new Button(() => SharedItemPicker.Show(pickerButton, item, selectedItem =>
                {
                    Undo.RecordObject(quest, "퀘스트 보상 아이템 변경");
                    serializedObject.Update();
                    SerializedProperty currentItems = serializedObject.FindProperty("reward.items");
                    if (capturedIndex < currentItems.arraySize)
                    {
                        currentItems.GetArrayElementAtIndex(capturedIndex)
                            .FindPropertyRelative("itemId").intValue = selectedItem != null ? selectedItem.itemId : 0;
                    }
                    serializedObject.ApplyModifiedProperties();
                    RebuildRewardRows(serializedObject, quest, rows, countLabel);
                    NotifyAssetChanged(quest);
                })) { text = "선택" };
                row.Add(pickerButton);

                var countField = new IntegerField("수량")
                {
                    value = element.FindPropertyRelative("count").intValue
                };
                countField.style.width = 105f;
                countField.RegisterValueChangedCallback(evt =>
                {
                    int value = Mathf.Max(1, evt.newValue);
                    countField.SetValueWithoutNotify(value);
                    Undo.RecordObject(quest, "퀘스트 보상 수량 변경");
                    serializedObject.Update();
                    SerializedProperty currentItems = serializedObject.FindProperty("reward.items");
                    if (capturedIndex < currentItems.arraySize)
                    {
                        currentItems.GetArrayElementAtIndex(capturedIndex)
                            .FindPropertyRelative("count").intValue = value;
                    }
                    serializedObject.ApplyModifiedProperties();
                });
                row.Add(countField);

                var rowSpace = new VisualElement();
                rowSpace.style.flexGrow = 1f;
                row.Add(rowSpace);
                var removeButton = new Button(() =>
                {
                    Undo.RecordObject(quest, "퀘스트 보상 아이템 삭제");
                    serializedObject.Update();
                    SerializedProperty currentItems = serializedObject.FindProperty("reward.items");
                    if (capturedIndex < currentItems.arraySize)
                        currentItems.DeleteArrayElementAtIndex(capturedIndex);
                    serializedObject.ApplyModifiedProperties();
                    RebuildRewardRows(serializedObject, quest, rows, countLabel);
                    NotifyAssetChanged(quest);
                }) { text = "×" };
                removeButton.style.width = 24f;
                removeButton.style.color = new Color(1f, 0.5f, 0.5f);
                row.Add(removeButton);
                rows.Add(row);
            }
        }
    }
}
#endif
