#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.Item;

namespace UPlayGround.Data.Editor.Authoring
{
    [InitializeOnLoad]
    internal static class DropDomainRegistration
    {
        static DropDomainRegistration()
        {
            DataAuthoringDomainRegistry.Register(
                DropDomainPanel.DomainKey,
                "드랍",
                () => new DropDomainPanel(),
                400);
        }
    }

    /// <summary>
    /// 몬스터 드랍 테이블과 일반 상호작용 오브젝트의 드랍 목록을 함께 편집합니다.
    /// </summary>
    public sealed class DropDomainPanel : DataDomainPanel<ScriptableObject>
    {
        public const string DomainKey = "drops";
        private const string DefaultPath = "Assets/10.Datas/Actor/Enemy/DropTables";

        public override string DomainId => DomainKey;
        public override string DisplayName => "드랍";
        public override Texture2D Icon => EditorGUIUtility.IconContent("d_Profiler.NetworkOperations").image as Texture2D;
        protected override string CreateButtonLabel => "+ 몬스터 드랍 테이블";
        protected override bool CanCreate => true;
        protected override bool CanDuplicate(ScriptableObject asset) => IsSupported(asset);
        protected override bool CanDelete(ScriptableObject asset) => IsSupported(asset);

        protected override IEnumerable<ScriptableObject> LoadAssets()
        {
            IEnumerable<EnemyDropTableSO> monsterTables = AssetDatabase.FindAssets("t:EnemyDropTableSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<EnemyDropTableSO>)
                .Where(asset => asset != null);

            IEnumerable<InteractableActorSO> interactables = AssetDatabase.FindAssets("t:InteractableActorSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<InteractableActorSO>)
                .Where(asset => asset != null && asset.GetType() == typeof(InteractableActorSO));

            return monsterTables.Cast<ScriptableObject>()
                .Concat(interactables)
                .OrderBy(LabelOf, StringComparer.CurrentCulture);
        }

        protected override string KeyOf(ScriptableObject asset) => AssetDatabase.GetAssetPath(asset);

        protected override string LabelOf(ScriptableObject asset)
        {
            return asset switch
            {
                EnemyDropTableSO table => $"{table.name}  ·  몬스터  ·  독립 {table.dropItems?.Count ?? 0}개 / 그룹 {table.weightedGroups?.Count ?? 0}개",
                InteractableActorSO interactable => $"{DisplayActorName(interactable)}  ·  상호작용  ·  {interactable.dropItems?.Count ?? 0}개",
                _ => asset != null ? asset.name : string.Empty
            };
        }

        protected override IEnumerable<DataDomainFilter<ScriptableObject>> CreateFilters()
        {
            yield return new DataDomainFilter<ScriptableObject>("몬스터", asset => asset is EnemyDropTableSO);
            yield return new DataDomainFilter<ScriptableObject>("상호작용", asset => asset is InteractableActorSO);
        }

        protected override IEnumerable<DataAuthoringIssue> GetIssues(ScriptableObject asset)
        {
            IReadOnlyList<ItemDropList> drops = asset switch
            {
                EnemyDropTableSO table => table.dropItems,
                InteractableActorSO interactable => interactable.dropItems,
                _ => Array.Empty<ItemDropList>()
            };

            for (int i = 0; i < drops.Count; i++)
            {
                ItemDropList drop = drops[i];
                if (drop == null || drop.itemData == null)
                {
                    yield return new DataAuthoringIssue(
                        DataAuthoringIssueSeverity.Error,
                        $"드랍 항목 {i + 1}의 아이템이 비어 있습니다.",
                        asset);
                    continue;
                }

                if (drop.rate <= 0f)
                {
                    yield return new DataAuthoringIssue(
                        DataAuthoringIssueSeverity.Warning,
                        $"'{drop.itemData.itemName}'의 드랍 확률이 0%입니다.",
                        asset);
                }
                if (drop.maximumDropCount <= 0)
                {
                    yield return new DataAuthoringIssue(
                        DataAuthoringIssueSeverity.Warning,
                        $"'{drop.itemData.itemName}'의 최대 수량이 0 이하입니다.",
                        asset);
                }
                else if (drop.minimumDropCount > drop.maximumDropCount)
                {
                    yield return new DataAuthoringIssue(
                        DataAuthoringIssueSeverity.Error,
                        $"'{drop.itemData.itemName}'의 최소 수량이 최대 수량보다 큽니다.",
                        asset);
                }
            }

            if (asset is not EnemyDropTableSO enemyTable || enemyTable.weightedGroups == null)
                yield break;

            var groupIds = new HashSet<string>(StringComparer.Ordinal);
            for (int groupIndex = 0; groupIndex < enemyTable.weightedGroups.Count; groupIndex++)
            {
                WeightedItemDropGroup group = enemyTable.weightedGroups[groupIndex];
                if (group == null)
                {
                    yield return new DataAuthoringIssue(
                        DataAuthoringIssueSeverity.Error,
                        $"가중 그룹 {groupIndex + 1}이 비어 있습니다.",
                        asset);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(group.groupId) || !groupIds.Add(group.groupId))
                {
                    yield return new DataAuthoringIssue(
                        DataAuthoringIssueSeverity.Error,
                        $"가중 그룹 {groupIndex + 1}의 ID가 비어 있거나 중복됩니다.",
                        asset);
                }

                if (group.entries == null || group.entries.Count == 0)
                {
                    yield return new DataAuthoringIssue(
                        DataAuthoringIssueSeverity.Warning,
                        $"'{group.groupId}' 가중 그룹에 후보가 없습니다.",
                        asset);
                    continue;
                }

                for (int entryIndex = 0; entryIndex < group.entries.Count; entryIndex++)
                {
                    WeightedItemDropEntry entry = group.entries[entryIndex];
                    if (entry?.itemData == null)
                    {
                        yield return new DataAuthoringIssue(
                            DataAuthoringIssueSeverity.Error,
                            $"'{group.groupId}' 그룹 후보 {entryIndex + 1}의 아이템이 비어 있습니다.",
                            asset);
                    }
                    else if (entry.minimumDropCount > entry.maximumDropCount)
                    {
                        yield return new DataAuthoringIssue(
                            DataAuthoringIssueSeverity.Error,
                            $"'{group.groupId}' 그룹의 '{entry.itemData.itemName}' 최소 수량이 최대 수량보다 큽니다.",
                            asset);
                    }
                }
            }
        }

        protected override void CreateNew()
        {
            EnemyDropTableSO created = AssetCrudService.CreateAsset<EnemyDropTableSO>(
                DefaultPath,
                "DropTable_New",
                undoName: "드랍 테이블 생성");
            EditorGUIUtility.PingObject(created);
            RefreshAssets(created);
        }

        protected override ScriptableObject Duplicate(ScriptableObject asset)
        {
            ScriptableObject copy = AssetCrudService.DuplicateAsset(asset, undoName: "드랍 데이터 복제");
            EditorGUIUtility.PingObject(copy);
            return copy;
        }

        protected override bool Delete(ScriptableObject asset)
        {
            if (!EditorUtility.DisplayDialog(
                    "드랍 데이터 삭제",
                    $"'{LabelOf(asset)}' 자산을 삭제할까요?\n연결된 프리팹·ActorDefinition 참조를 먼저 확인하세요.",
                    "삭제",
                    "취소"))
            {
                return false;
            }

            return AssetCrudService.DeleteAsset(asset, "드랍 데이터 삭제");
        }

        protected override VisualElement BuildDetail(ScriptableObject asset)
        {
            var detail = new VisualElement();
            var serializedObject = new SerializedObject(asset);
            detail.Add(BuildHeader(asset));

            if (asset is InteractableActorSO)
            {
                var actorSection = MakeSection("상호작용 오브젝트");
                AddProperty(actorSection, serializedObject, "actorName", "이름");
                AddProperty(actorSection, serializedObject, "interactionObjectType", "상호작용 유형");
                AddProperty(actorSection, serializedObject, "description", "설명");
                AddProperty(actorSection, serializedObject, "hp", "HP");
                detail.Add(actorSection);
            }

            var dropSection = MakeSection("드랍 아이템");
            RebuildDropRows(dropSection, asset, serializedObject);
            detail.Add(dropSection);

            if (asset is EnemyDropTableSO)
            {
                var weightedSection = MakeSection("가중 선택 그룹");
                AddProperty(weightedSection, serializedObject, "weightedGroups", "그룹 목록");
                weightedSection.Add(new HelpBox(
                    "장비처럼 동시에 여러 개가 나오면 안 되는 후보를 한 그룹에 넣습니다. 미드랍 가중치로 그룹 전체 드랍률을 조절할 수 있습니다.",
                    HelpBoxMessageType.Info));
                detail.Add(weightedSection);
            }

            var summary = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            detail.Add(summary);

            void RefreshSummary()
            {
                SerializedProperty drops = serializedObject.FindProperty("dropItems");
                float expected = 0f;
                for (int i = 0; i < drops.arraySize; i++)
                {
                    SerializedProperty row = drops.GetArrayElementAtIndex(i);
                    float rate = row.FindPropertyRelative("rate").floatValue / 100f;
                    int minimum = Mathf.Max(1, row.FindPropertyRelative("minimumDropCount").intValue);
                    int maximum = row.FindPropertyRelative("maximumDropCount").intValue;
                    expected += rate * (minimum + Mathf.Max(minimum, maximum)) * 0.5f;
                }
                SerializedProperty groups = serializedObject.FindProperty("weightedGroups");
                string groupSummary = groups != null ? $" · 가중 그룹 {groups.arraySize}개" : string.Empty;
                summary.text = $"독립 항목 {drops.arraySize}개{groupSummary} · 독립 최대 {ExpectedMax(drops)}개 · 독립 기대 수량 {expected:0.##}개";
            }

            RefreshSummary();
            detail.TrackSerializedObjectValue(serializedObject, _ =>
            {
                NotifyAssetChanged(asset);
                RefreshSummary();
            });
            return detail;
        }

        private void RebuildDropRows(VisualElement section, ScriptableObject asset, SerializedObject serializedObject)
        {
            ClearSectionBody(section);
            serializedObject.Update();
            SerializedProperty drops = serializedObject.FindProperty("dropItems");

            if (drops == null)
            {
                section.Add(new HelpBox("dropItems 직렬화 필드를 찾지 못했습니다.", HelpBoxMessageType.Error));
                return;
            }

            if (drops.arraySize == 0)
                section.Add(MutedLabel("등록된 드랍 아이템이 없습니다."));

            for (int index = 0; index < drops.arraySize; index++)
            {
                int capturedIndex = index;
                SerializedProperty row = drops.GetArrayElementAtIndex(index);
                string itemPath = row.FindPropertyRelative("itemData").propertyPath;

                var box = MakeBox();
                var heading = new VisualElement();
                heading.style.flexDirection = FlexDirection.Row;
                var title = new Label($"드랍 {index + 1}");
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.flexGrow = 1f;
                heading.Add(title);
                heading.Add(new Button(() =>
                {
                    serializedObject.Update();
                    SerializedProperty currentDrops = serializedObject.FindProperty("dropItems");
                    currentDrops.DeleteArrayElementAtIndex(capturedIndex);
                    serializedObject.ApplyModifiedProperties();
                    RebuildDropRows(section, asset, serializedObject);
                    NotifyAssetChanged(asset);
                }) { text = "삭제" });
                box.Add(heading);

                var itemRow = new VisualElement();
                itemRow.style.flexDirection = FlexDirection.Row;
                var itemField = new PropertyField(row.FindPropertyRelative("itemData"), "아이템");
                itemField.style.flexGrow = 1f;
                itemRow.Add(itemField);
                var pickerButton = new Button { text = "검색" };
                pickerButton.clicked += () =>
                {
                    serializedObject.Update();
                    ItemSO current = serializedObject.FindProperty(itemPath).objectReferenceValue as ItemSO;
                    SharedItemPicker.Show(pickerButton, current, selected =>
                    {
                        serializedObject.Update();
                        serializedObject.FindProperty(itemPath).objectReferenceValue = selected;
                        serializedObject.ApplyModifiedProperties();
                        NotifyAssetChanged(asset);
                    });
                };
                itemRow.Add(pickerButton);
                box.Add(itemRow);

                var rate = new PropertyField(row.FindPropertyRelative("rate"), "확률 (%)");
                var minimum = new PropertyField(row.FindPropertyRelative("minimumDropCount"), "최소 수량");
                var maximum = new PropertyField(row.FindPropertyRelative("maximumDropCount"), "최대 수량");
                box.Add(rate);
                box.Add(minimum);
                box.Add(maximum);
                box.Bind(serializedObject);
                section.Add(box);
            }

            section.Add(new Button(() =>
            {
                serializedObject.Update();
                SerializedProperty currentDrops = serializedObject.FindProperty("dropItems");
                int newIndex = currentDrops.arraySize;
                currentDrops.InsertArrayElementAtIndex(newIndex);
                SerializedProperty added = currentDrops.GetArrayElementAtIndex(newIndex);
                added.FindPropertyRelative("itemData").objectReferenceValue = null;
                added.FindPropertyRelative("rate").floatValue = 100f;
                added.FindPropertyRelative("minimumDropCount").intValue = 1;
                added.FindPropertyRelative("maximumDropCount").intValue = 1;
                serializedObject.ApplyModifiedProperties();
                RebuildDropRows(section, asset, serializedObject);
                NotifyAssetChanged(asset);
            }) { text = "+ 드랍 아이템 추가" });
        }

        private static VisualElement BuildHeader(ScriptableObject asset)
        {
            var header = new Toolbar();
            var title = new Label(asset.name);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            header.Add(spacer);
            var path = new Label(AssetDatabase.GetAssetPath(asset));
            path.style.fontSize = 10f;
            header.Add(path);
            header.Add(new ToolbarButton(() => EditorGUIUtility.PingObject(asset)) { text = "Project에서 열기" });
            return header;
        }

        private static void AddProperty(VisualElement parent, SerializedObject serializedObject, string path, string label)
        {
            SerializedProperty property = serializedObject.FindProperty(path);
            if (property != null)
            {
                var field = new PropertyField(property, label);
                field.Bind(serializedObject);
                parent.Add(field);
            }
        }

        private static VisualElement MakeSection(string title)
        {
            var section = new VisualElement();
            section.style.marginTop = 10f;
            section.style.paddingLeft = 8f;
            section.style.paddingRight = 8f;
            section.style.paddingTop = 7f;
            section.style.paddingBottom = 7f;
            var heading = new Label(title) { name = "section-title" };
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 5f;
            section.Add(heading);
            return section;
        }

        private static VisualElement MakeBox()
        {
            var box = new VisualElement();
            box.style.marginBottom = 5f;
            box.style.paddingLeft = 6f;
            box.style.paddingRight = 6f;
            box.style.paddingTop = 5f;
            box.style.paddingBottom = 5f;
            box.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.1f);
            return box;
        }

        private static Label MutedLabel(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 10f;
            label.style.color = new Color(0.65f, 0.65f, 0.65f);
            return label;
        }

        private static void ClearSectionBody(VisualElement section)
        {
            while (section.childCount > 1)
                section.RemoveAt(section.childCount - 1);
        }

        private static int ExpectedMax(SerializedProperty drops)
        {
            int result = 0;
            for (int i = 0; i < drops.arraySize; i++)
                result += drops.GetArrayElementAtIndex(i).FindPropertyRelative("maximumDropCount").intValue;
            return result;
        }

        private static bool IsSupported(ScriptableObject asset)
            => asset is EnemyDropTableSO || asset is InteractableActorSO;

        private static string DisplayActorName(InteractableActorSO asset)
            => string.IsNullOrWhiteSpace(asset.actorName) ? asset.name : asset.actorName;
    }
}
#endif
