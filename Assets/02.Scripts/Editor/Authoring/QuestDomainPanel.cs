#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Editor.Authoring;
using UPlayGround.Data.Item;
using UPlayGround.Data.Quest;
using UPlayGround.Tool.Editor;

namespace UPlayGround.Editor.Authoring
{
    [InitializeOnLoad]
    internal static class QuestDomainRegistration
    {
        static QuestDomainRegistration()
        {
            DataAuthoringDomainRegistry.Register(
                QuestDomainPanel.DomainKey,
                "퀘스트",
                () => new QuestDomainPanel(),
                300);
        }
    }

    public sealed partial class QuestDomainPanel : DataDomainPanel<QuestSO>
    {
        public const string DomainKey = "quests";
        private const string DefaultQuestPath = "Assets/10.Datas/Quest";
        private const string QuestIdOutputPath = "Assets/02.Scripts/Data/Quest/QuestIdType.cs";

        private readonly Dictionary<int, ItemSO> _itemsById = new Dictionary<int, ItemSO>();
        private QuestDatabase _questDatabase;
        private VisualElement _createPopup;
        private string _newQuestId = "quest_new";
        private string _newQuestName = "새 퀘스트";
        private string _newSavePath = DefaultQuestPath;

        public override string DomainId => DomainKey;
        public override string DisplayName => "퀘스트";
        public override Texture2D Icon => EditorGUIUtility.IconContent("ScriptableObject Icon").image as Texture2D;
        protected override float ListPanelWidth => 330f;
        protected override string CreateButtonLabel => "+ 새 퀘스트";
        protected override bool CanCreate => true;
        protected override bool CanDuplicate(QuestSO asset) => asset != null;
        protected override bool CanDelete(QuestSO asset) => asset != null;

        protected override IEnumerable<QuestSO> LoadAssets()
        {
            LoadQuestDatabase();
            RebuildItemIndex();
            return AssetDatabase.FindAssets("t:QuestSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<QuestSO>)
                .Where(quest => quest != null)
                .OrderBy(quest => quest.questId, StringComparer.Ordinal);
        }

        protected override string KeyOf(QuestSO asset)
        {
            return asset?.questId;
        }

        protected override string LabelOf(QuestSO asset)
        {
            if (asset == null)
                return "(Missing Quest)";

            string type = asset.questType == QuestType.Main ? "메인" : "서브";
            return $"{asset.questName}  ·  {asset.questId}  ·  {type}  ·  목표 {asset.objectives?.Count ?? 0}";
        }

        protected override IEnumerable<DataDomainFilter<QuestSO>> CreateFilters()
        {
            yield return new DataDomainFilter<QuestSO>("메인", quest => quest.questType == QuestType.Main);
            yield return new DataDomainFilter<QuestSO>("서브", quest => quest.questType == QuestType.Sub);
            yield return new DataDomainFilter<QuestSO>("반복", quest => quest.isRepeatable);
            yield return new DataDomainFilter<QuestSO>("자동완료", quest => quest.autoComplete);
        }

        protected override void AddToolbarActions(Toolbar toolbar)
        {
            toolbar.Add(new ToolbarButton(RefreshDatabase) { text = "DB 갱신" });
            toolbar.Add(new ToolbarButton(GenerateQuestIdEnum) { text = "ID Enum 생성" });
        }

        protected override void CreateNew()
        {
            if (_createPopup == null)
            {
                _createPopup = BuildCreatePopup();
                Root.Add(_createPopup);
            }

            _createPopup.style.display = _createPopup.style.display == DisplayStyle.None
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        protected override QuestSO Duplicate(QuestSO asset)
        {
            string duplicateId = MakeUniqueDuplicateId(asset.questId);
            QuestSO copy = AssetCrudService.DuplicateAsset(
                asset,
                duplicated =>
                {
                    duplicated.questId = duplicateId;
                    duplicated.questName = $"{duplicated.questName} 복사본";
                },
                "퀘스트 복제");
            if (copy != null)
                EditorGUIUtility.PingObject(copy);
            return copy;
        }

        protected override bool Delete(QuestSO asset)
        {
            if (!EditorUtility.DisplayDialog(
                    "퀘스트 삭제",
                    $"'{asset.questName}' (ID: {asset.questId})을 삭제합니다.\nUndo로 복구할 수 있습니다.",
                    "삭제",
                    "취소"))
            {
                return false;
            }

            return AssetCrudService.DeleteAsset(asset, "퀘스트 삭제");
        }

        protected override IEnumerable<DataAuthoringIssue> GetIssues(QuestSO asset)
        {
            if (HasDuplicateKey(asset))
            {
                yield return new DataAuthoringIssue(
                    DataAuthoringIssueSeverity.Error,
                    $"Quest ID '{asset.questId}'가 중복됩니다.",
                    asset);
            }

            if (asset.reward?.items == null)
                yield break;

            foreach (QuestItemReward reward in asset.reward.items)
            {
                if (reward != null && reward.itemId != 0 && !_itemsById.ContainsKey(reward.itemId))
                {
                    yield return new DataAuthoringIssue(
                        DataAuthoringIssueSeverity.Warning,
                        $"보상 아이템 ID {reward.itemId}를 찾을 수 없습니다.",
                        asset);
                }
            }
        }

        private VisualElement BuildCreatePopup()
        {
            var popup = new VisualElement();
            popup.style.position = Position.Absolute;
            popup.style.left = 4f;
            popup.style.top = 22f;
            popup.style.width = 380f;
            popup.style.display = DisplayStyle.None;
            popup.style.paddingLeft = 8f;
            popup.style.paddingRight = 8f;
            popup.style.paddingBottom = 8f;
            popup.style.backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f)
                : new Color(0.82f, 0.82f, 0.82f);
            SetBorder(popup);

            var header = new Toolbar();
            var title = new Label("새 퀘스트 생성");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);
            var flexibleSpace = new VisualElement();
            flexibleSpace.style.flexGrow = 1f;
            header.Add(flexibleSpace);
            header.Add(new ToolbarButton(() => popup.style.display = DisplayStyle.None) { text = "×" });
            popup.Add(header);

            var idField = new TextField("Quest ID") { value = _newQuestId };
            idField.style.marginTop = 4f;
            popup.Add(idField);
            var nameField = new TextField("퀘스트 이름") { value = _newQuestName };
            popup.Add(nameField);

            var pathRow = new VisualElement();
            pathRow.style.flexDirection = FlexDirection.Row;
            var pathField = new TextField("저장 경로") { value = _newSavePath };
            pathField.style.flexGrow = 1f;
            pathRow.Add(pathField);
            pathRow.Add(new Button(() => SelectSaveFolder(pathField)) { text = "..." });
            popup.Add(pathRow);

            var duplicateWarning = new HelpBox("이미 존재하는 Quest ID입니다.", HelpBoxMessageType.Warning);
            popup.Add(duplicateWarning);
            var createButton = new Button { text = "생성" };
            createButton.style.height = 25f;
            createButton.style.marginTop = 5f;
            popup.Add(createButton);

            void Validate()
            {
                _newQuestId = idField.value?.Trim();
                _newQuestName = nameField.value?.Trim();
                _newSavePath = pathField.value?.Trim();
                bool duplicated = Assets.Any(quest => quest.questId == _newQuestId);
                duplicateWarning.style.display = duplicated ? DisplayStyle.Flex : DisplayStyle.None;
                createButton.SetEnabled(
                    !string.IsNullOrWhiteSpace(_newQuestId)
                    && !string.IsNullOrWhiteSpace(_newQuestName)
                    && !string.IsNullOrWhiteSpace(_newSavePath)
                    && !duplicated);
            }

            idField.RegisterValueChangedCallback(_ => Validate());
            nameField.RegisterValueChangedCallback(_ => Validate());
            pathField.RegisterValueChangedCallback(_ => Validate());
            createButton.clicked += () =>
            {
                Validate();
                if (!createButton.enabledSelf)
                    return;
                CreateQuestFromPopup(popup);
            };
            Validate();
            return popup;
        }

        private void CreateQuestFromPopup(VisualElement popup)
        {
            if (!AssetCrudService.IsAssetPathWithin(_newSavePath, DefaultQuestPath))
            {
                EditorUtility.DisplayDialog(
                    "잘못된 저장 경로",
                    $"퀘스트는 QuestDatabase 검색 루트 아래에 저장해야 합니다.\n{DefaultQuestPath}",
                    "확인");
                return;
            }

            QuestSO quest = AssetCrudService.CreateAsset<QuestSO>(
                _newSavePath,
                _newQuestId,
                created =>
                {
                    created.questId = _newQuestId;
                    created.questName = _newQuestName;
                },
                "퀘스트 생성");

            popup.style.display = DisplayStyle.None;
            RefreshAssets(quest);
            EditorGUIUtility.PingObject(quest);
            Debug.Log($"[DataAuthoringHub] 퀘스트 생성 완료: {AssetDatabase.GetAssetPath(quest)}");
        }

        private void SelectSaveFolder(TextField pathField)
        {
            string selected = EditorUtility.OpenFolderPanel("퀘스트 저장 폴더", _newSavePath, string.Empty);
            if (string.IsNullOrEmpty(selected))
                return;

            if (!AssetCrudService.TryConvertAbsoluteFolderToAssetPath(selected, out string assetPath)
                || !AssetCrudService.IsAssetPathWithin(assetPath, DefaultQuestPath))
            {
                EditorUtility.DisplayDialog(
                    "잘못된 저장 경로",
                    $"퀘스트는 QuestDatabase 검색 루트 아래에 저장해야 합니다.\n{DefaultQuestPath}",
                    "확인");
                return;
            }

            _newSavePath = assetPath;
            pathField.value = _newSavePath;
        }

        private string MakeUniqueDuplicateId(string sourceId)
        {
            string baseId = string.IsNullOrWhiteSpace(sourceId) ? "quest_copy" : $"{sourceId}_copy";
            string candidate = baseId;
            int suffix = 2;
            while (Assets.Any(quest => quest != null && quest.questId == candidate))
                candidate = $"{baseId}{suffix++}";
            return candidate;
        }

        private void RefreshDatabase()
        {
            LoadQuestDatabase();
            if (_questDatabase == null)
            {
                EditorUtility.DisplayDialog("QuestDatabase 없음", "프로젝트에서 QuestDatabase를 찾을 수 없습니다.", "확인");
                return;
            }

            Undo.RecordObject(_questDatabase, "QuestDatabase 갱신");
            _questDatabase.RefreshDatabase(DefaultQuestPath);
            RefreshAssets(Selected);
        }

        private void GenerateQuestIdEnum()
        {
            LoadQuestDatabase();
            if (_questDatabase == null)
            {
                EditorUtility.DisplayDialog("QuestDatabase 없음", "DB 갱신을 먼저 실행하세요.", "확인");
                return;
            }

            var rawEntries = new List<(string, string)>();
            foreach (QuestSO quest in _questDatabase.QuestList)
            {
                if (quest != null && !string.IsNullOrEmpty(quest.questId))
                    rawEntries.Add((quest.questId, quest.questId));
            }

            var entries = IdEnumGeneratorUtility.DeduplicateEntries(rawEntries);
            bool generated = IdEnumGeneratorUtility.GenerateStringKeyEnum(
                "QuestIdType",
                "ToQuestId",
                "Quest ID",
                QuestIdOutputPath,
                "UPlayGround.Data.Quest",
                entries);
            if (!generated)
                return;

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Enum 생성 완료",
                $"QuestIdType 생성 완료 ({entries.Count}개)\n→ {QuestIdOutputPath}",
                "확인");
        }

        private void LoadQuestDatabase()
        {
            if (_questDatabase != null)
                return;

            string guid = AssetDatabase.FindAssets("t:QuestDatabase").FirstOrDefault();
            if (!string.IsNullOrEmpty(guid))
                _questDatabase = AssetDatabase.LoadAssetAtPath<QuestDatabase>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private void RebuildItemIndex()
        {
            _itemsById.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:ItemSO"))
            {
                ItemSO item = AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (item != null && !_itemsById.ContainsKey(item.itemId))
                    _itemsById.Add(item.itemId, item);
            }
        }

        private ItemSO FindItem(int itemId)
        {
            _itemsById.TryGetValue(itemId, out ItemSO item);
            return item;
        }

        private static void AddProperty(VisualElement parent, string bindingPath, string label)
        {
            parent.Add(new PropertyField { bindingPath = bindingPath, label = label });
        }

        private static VisualElement MakeSection(string title)
        {
            var section = new VisualElement();
            section.style.marginTop = 6f;
            section.style.paddingLeft = 8f;
            section.style.paddingRight = 8f;
            section.style.paddingTop = 6f;
            section.style.paddingBottom = 6f;
            section.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.08f);
            SetBorder(section);

            var label = new Label(title);
            label.AddToClassList("section-title");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 3f;
            section.Add(label);
            return section;
        }

        private static void SetBorder(VisualElement element)
        {
            Color color = new Color(0f, 0f, 0f, 0.3f);
            element.style.borderLeftWidth = 1f;
            element.style.borderRightWidth = 1f;
            element.style.borderTopWidth = 1f;
            element.style.borderBottomWidth = 1f;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
        }
    }
}
#endif
