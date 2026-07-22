using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Codex;

namespace UPlayGround.Data.Editor.Codex
{
    /// <summary>ActorDatabase와 연결된 몬스터 도감 항목을 일괄 편집하는 UI Toolkit 창.</summary>
    public sealed class MonsterCodexEditorWindow : EditorWindow
    {
        private const string WindowTitle = "몬스터 도감 편집기";

        private readonly List<MonsterCodexEntrySO> _filteredEntries = new();

        private ActorDatabase _actorDatabase;
        private MonsterCodexDatabaseSO _codexDatabase;
        private ListView _entryList;
        private VisualElement _detail;
        private ToolbarSearchField _search;
        private Label _summary;

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/도감/몬스터 도감 편집기")]
        public static void Open()
        {
            MonsterCodexEditorWindow window = GetWindow<MonsterCodexEditorWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(920f, 620f);
            window.Show();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            BuildToolbar();
            BuildContent();
            ReloadDatabases();
        }

        private void BuildToolbar()
        {
            Toolbar toolbar = new();

            _search = new ToolbarSearchField();
            _search.style.width = 260f;
            _search.RegisterValueChangedCallback(_ => RefreshList());
            toolbar.Add(_search);

            toolbar.Add(new ToolbarSpacer { flex = true });
            toolbar.Add(new ToolbarButton(BuildDatabase)
            {
                text = "데이터 생성/갱신",
                tooltip = "ActorDatabase의 모든 Monster 정의를 기준으로 누락 항목을 생성합니다.",
            });
            toolbar.Add(new ToolbarButton(ValidateDatabase) { text = "검증" });
            toolbar.Add(new ToolbarButton(SaveAssets) { text = "저장" });
            toolbar.Add(new ToolbarButton(ReloadDatabases) { text = "새로고침" });
            rootVisualElement.Add(toolbar);
        }

        private void BuildContent()
        {
            _summary = new Label();
            _summary.style.paddingLeft = 8f;
            _summary.style.paddingTop = 5f;
            _summary.style.paddingBottom = 5f;
            rootVisualElement.Add(_summary);

            TwoPaneSplitView split = new(
                0,
                330f,
                TwoPaneSplitViewOrientation.Horizontal);
            split.style.flexGrow = 1f;

            VisualElement listPane = new();
            listPane.style.flexGrow = 1f;
            listPane.style.paddingLeft = 6f;
            listPane.style.paddingRight = 6f;
            listPane.style.paddingBottom = 6f;

            _entryList = new ListView
            {
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                fixedItemHeight = 42f,
                makeItem = () =>
                {
                    VisualElement row = new();
                    row.style.flexDirection = FlexDirection.Column;
                    row.style.paddingLeft = 7f;
                    row.style.paddingTop = 3f;
                    row.Add(new Label { name = "display-name" });
                    Label actorId = new() { name = "actor-id" };
                    actorId.style.fontSize = 10f;
                    actorId.style.opacity = 0.65f;
                    row.Add(actorId);
                    return row;
                },
                bindItem = (element, index) =>
                {
                    MonsterCodexEntrySO entry = _filteredEntries[index];
                    ActorDefinitionSO actor = FindActor(entry.actorId);
                    string displayName = !string.IsNullOrWhiteSpace(entry.displayNameOverride)
                        ? entry.displayNameOverride
                        : actor != null && !string.IsNullOrWhiteSpace(actor.displayName)
                            ? actor.displayName
                            : entry.name;
                    element.Q<Label>("display-name").text =
                        $"{(entry.includeInCodex ? "●" : "○")} {displayName}";
                    element.Q<Label>("actor-id").text = entry.actorId;
                },
            };
            _entryList.selectionChanged += selection =>
                ShowDetail(selection.FirstOrDefault() as MonsterCodexEntrySO);
            listPane.Add(_entryList);

            _detail = new ScrollView();
            _detail.style.flexGrow = 1f;
            _detail.style.paddingLeft = 14f;
            _detail.style.paddingRight = 14f;
            _detail.style.paddingBottom = 14f;

            split.Add(listPane);
            split.Add(_detail);
            rootVisualElement.Add(split);
        }

        private void ReloadDatabases()
        {
            _actorDatabase = FindFirst<ActorDatabase>();
            _codexDatabase = FindFirst<MonsterCodexDatabaseSO>();
            RefreshList();
        }

        private void RefreshList()
        {
            MonsterCodexEntrySO selected = _entryList?.selectedItem as MonsterCodexEntrySO;
            _filteredEntries.Clear();

            if (_codexDatabase != null)
            {
                string query = _search?.value?.Trim();
                foreach (MonsterCodexEntrySO entry in _codexDatabase.Entries)
                {
                    if (entry == null || !MatchesSearch(entry, query))
                        continue;
                    _filteredEntries.Add(entry);
                }
            }

            _filteredEntries.Sort((left, right) =>
                string.CompareOrdinal(left.actorId, right.actorId));

            if (_entryList != null)
            {
                _entryList.itemsSource = _filteredEntries;
                _entryList.Rebuild();
            }

            int actorCount = _actorDatabase?.All.Count ?? 0;
            int codexCount = _codexDatabase?.Entries.Count ?? 0;
            _summary.text = _codexDatabase == null
                ? "MonsterCodexDatabase가 없습니다. '데이터 생성/갱신'을 실행하세요."
                : $"도감 {codexCount}개 · 검색 결과 {_filteredEntries.Count}개 · Actor 정의 {actorCount}개";

            if (selected != null && _filteredEntries.Contains(selected))
                _entryList.SetSelection(_filteredEntries.IndexOf(selected));
            else if (_filteredEntries.Count > 0)
                _entryList.SetSelection(0);
            else
                ShowDetail(null);
        }

        private void ShowDetail(MonsterCodexEntrySO entry)
        {
            _detail.Clear();
            if (entry == null)
            {
                _detail.Add(new HelpBox("편집할 도감 항목을 선택하세요.", HelpBoxMessageType.Info));
                return;
            }

            ActorDefinitionSO actor = FindActor(entry.actorId);
            Label title = new(actor != null ? actor.displayName : entry.name);
            title.style.fontSize = 22f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginTop = 10f;
            title.style.marginBottom = 8f;
            _detail.Add(title);

            ObjectField actorField = new("연결된 Actor Definition")
            {
                objectType = typeof(ActorDefinitionSO),
                value = actor,
                allowSceneObjects = false,
            };
            actorField.SetEnabled(false);
            _detail.Add(actorField);

            if (actor == null)
            {
                _detail.Add(new HelpBox(
                    $"ActorDatabase에서 actorId '{entry.actorId}'를 찾지 못했습니다.",
                    HelpBoxMessageType.Error));
            }

            SerializedObject serialized = new(entry);
            AddProperty(serialized, "actorId", "Actor ID", false);
            AddProperty(serialized, "includeInCodex", "도감에 표시");
            AddProperty(serialized, "portrait", "초상화");
            AddProperty(serialized, "displayNameOverride", "표시명 재정의");
            AddProperty(serialized, "descriptionOverride", "설명 재정의");
            AddProperty(serialized, "fullRecordKillCount", "100% 목표 처치 수");

            Label bonusTitle = new("최대 기록 보정");
            bonusTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            bonusTitle.style.marginTop = 12f;
            _detail.Add(bonusTitle);
            AddProperty(serialized, "bonus", "보정 수치");

            Button ping = new(() =>
            {
                Selection.activeObject = entry;
                EditorGUIUtility.PingObject(entry);
            })
            {
                text = "Project에서 항목 찾기",
            };
            ping.style.marginTop = 12f;
            _detail.Add(ping);
            _detail.Bind(serialized);
        }

        private void AddProperty(
            SerializedObject serialized,
            string propertyName,
            string label,
            bool enabled = true)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                return;

            PropertyField field = new(property, label);
            field.SetEnabled(enabled);
            _detail.Add(field);
        }

        private bool MatchesSearch(MonsterCodexEntrySO entry, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            ActorDefinitionSO actor = FindActor(entry.actorId);
            return entry.actorId.Contains(query, System.StringComparison.OrdinalIgnoreCase) ||
                   entry.name.Contains(query, System.StringComparison.OrdinalIgnoreCase) ||
                   (!string.IsNullOrWhiteSpace(entry.displayNameOverride) &&
                    entry.displayNameOverride.Contains(
                        query,
                        System.StringComparison.OrdinalIgnoreCase)) ||
                   (actor != null &&
                    !string.IsNullOrWhiteSpace(actor.displayName) &&
                    actor.displayName.Contains(query, System.StringComparison.OrdinalIgnoreCase));
        }

        private ActorDefinitionSO FindActor(string actorId) =>
            _actorDatabase?.GetDefinition(actorId);

        private void BuildDatabase()
        {
            MonsterCodexDatabaseBuilder.Build();
            ReloadDatabases();
        }

        private void ValidateDatabase() =>
            MonsterCodexDatabaseBuilder.Validate();

        private void SaveAssets()
        {
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("도감 데이터를 저장했습니다."));
        }

        private static T FindFirst<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids.Length == 0
                ? null
                : AssetDatabase.LoadAssetAtPath<T>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
