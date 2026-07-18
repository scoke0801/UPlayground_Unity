using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;

namespace UPlayGround.Data.Editor.Ability
{
    public sealed class GameplayAbilityEditorWindow : EditorWindow
    {
        private readonly List<UnityEngine.Object> _assets = new();
        private readonly List<UnityEngine.Object> _filtered = new();
        private ListView _assetList;
        private VisualElement _detail;
        private VisualElement _summary;
        private VisualElement _validation;
        private ToolbarSearchField _search;
        private ToolbarMenu _filterMenu;
        private Label _pathLabel;
        private UnityEngine.Object _selected;
        private string _filter = "전체";
        private string _activeTab = "기본 정보";
        private ObjectField _legacySource;
        private VisualElement _main;
        private VisualElement _assetColumn;
        private Label _toolbarPathLabel;
        private VisualElement _toolbarRow;
        private VisualElement _tabsRow;
        private readonly Dictionary<string, Button> _tabButtons = new();

        private static readonly Color Bg0 = new(0.055f, 0.075f, 0.10f);
        private static readonly Color Bg1 = new(0.08f, 0.10f, 0.13f);
        private static readonly Color Bg2 = new(0.11f, 0.13f, 0.16f);
        private static readonly Color Border = new(0.22f, 0.27f, 0.32f);
        private static readonly Color Accent = new(0.18f, 0.52f, 0.92f);

        [MenuItem("UPlayGround/Ability/Ability & Effect 데이터 툴")]
        public static void Open()
        {
            GameplayAbilityEditorWindow window = GetWindow<GameplayAbilityEditorWindow>();
            window.titleContent = new GUIContent("Ability Editor");
            window.minSize = new Vector2(1050f, 650f);
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += HandleUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedo;
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.backgroundColor = Bg0;
            rootVisualElement.style.color = new Color(0.88f, 0.9f, 0.94f);
            BuildToolbar();
            BuildTabs();
            BuildMain();
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            RefreshAssets();
        }

        private void BuildToolbar()
        {
            var toolbarScroll = new ScrollView(ScrollViewMode.Horizontal);
            toolbarScroll.style.height = 35f;
            toolbarScroll.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            toolbarScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            toolbarScroll.style.flexShrink = 0f;

            var toolbar = new Toolbar();
            _toolbarRow = toolbar;
            toolbar.style.height = 34f;
            toolbar.style.flexShrink = 0f;
            toolbar.style.backgroundColor = Bg2;
            toolbar.style.borderBottomColor = Border;
            toolbar.style.borderBottomWidth = 1f;

            var title = new Label("Gameplay Ability / Effect Editor");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginLeft = 8f;
            toolbar.Add(title);

            _pathLabel = new Label("에셋을 선택하세요");
            _toolbarPathLabel = _pathLabel;
            _pathLabel.style.color = new Color(0.55f, 0.6f, 0.68f);
            _pathLabel.style.marginLeft = 14f;
            _pathLabel.style.minWidth = 160f;
            _pathLabel.style.flexGrow = 1f;
            toolbar.Add(_pathLabel);

            toolbar.Add(MakeToolbarButton("새 Ability", () => CreateAsset<GameplayAbilitySO>("GA_")));
            toolbar.Add(MakeToolbarButton("새 Effect", () => CreateAsset<GameplayEffectSO>("GE_")));
            toolbar.Add(MakeToolbarButton("새 Set", () => CreateAsset<AbilitySetSO>("AbilitySet_")));
            toolbar.Add(MakeToolbarButton(
                "일괄 변환",
                AbilityBatchMigrationWindow.Open));
            toolbar.Add(MakeToolbarButton("전체 검증", ValidateAll));

            var delete = MakeToolbarButton("선택 삭제", DeleteSelected);
            delete.style.backgroundColor = new Color(0.45f, 0.12f, 0.12f);
            delete.style.color = new Color(1f, 0.82f, 0.82f);
            toolbar.Add(delete);

            var save = MakeToolbarButton("저장", SaveSelected);
            save.style.backgroundColor = Accent;
            save.style.color = Color.white;
            toolbar.Add(save);
            toolbarScroll.Add(toolbar);
            rootVisualElement.Add(toolbarScroll);
        }

        private void ConvertSelectedPayload()
        {
            if (_selected is not GameplayAbilitySO ability)
            {
                EditorUtility.DisplayDialog(
                    "Payload 변환",
                    "변환할 GameplayAbilitySO를 에셋 목록에서 선택하세요.",
                    "확인");
                return;
            }

            int converted = AbilityPayloadMigration.ConvertLegacyVariants(ability);
            EditorUtility.DisplayDialog(
                "Payload 변환",
                converted > 0
                    ? $"{converted}개 Variant를 비파괴 변환했습니다."
                    : "변환할 레거시 Variant가 없습니다.",
                "확인");
            RebuildDetail();
        }

        private void BuildTabs()
        {
            var tabScroll = new ScrollView(ScrollViewMode.Horizontal);
            tabScroll.style.height = 33f;
            tabScroll.style.flexShrink = 0f;
            tabScroll.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            tabScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;

            var tabs = new VisualElement();
            _tabsRow = tabs;
            _tabButtons.Clear();
            tabs.style.flexDirection = FlexDirection.Row;
            tabs.style.height = 32f;
            tabs.style.flexShrink = 0f;
            tabs.style.backgroundColor = Bg1;
            tabs.style.borderBottomColor = Border;
            tabs.style.borderBottomWidth = 1f;

            string[] labels =
            {
                "기본 정보", "활성화 조건", "비용/쿨다운", "Variant",
                "Effect", "Cue", "저장/교체 정책", "정적 밸런스", "검증 결과",
            };
            for (int i = 0; i < labels.Length; i++)
            {
                string tab = labels[i];
                var button = new Button(() =>
                {
                    _activeTab = tab;
                    UpdateTabStyles();
                    RebuildDetail();
                }) { text = tab };
                // 클릭 후 파란 포커스 테두리가 선택 표시처럼 남지 않게 한다.
                // 활성 상태는 아래 UpdateTabStyles의 단일 표시만 사용한다.
                button.focusable = false;
                button.style.height = 27f;
                button.style.marginTop = 4f;
                button.style.marginLeft = 2f;
                button.style.flexShrink = 0f;
                _tabButtons[tab] = button;
                tabs.Add(button);
            }
            UpdateTabStyles();
            tabScroll.Add(tabs);
            rootVisualElement.Add(tabScroll);
        }

        private void UpdateTabStyles()
        {
            foreach (KeyValuePair<string, Button> pair in _tabButtons)
            {
                bool active = string.Equals(pair.Key, _activeTab, StringComparison.Ordinal);
                Button button = pair.Value;
                button.style.backgroundColor = active
                    ? new Color(0.12f, 0.22f, 0.32f)
                    : Bg2;
                button.style.color = active
                    ? Color.white
                    : new Color(0.72f, 0.76f, 0.82f);
                button.style.borderBottomColor = active ? Accent : Color.clear;
                button.style.borderBottomWidth = active ? 3f : 0f;
            }
        }

        private void BuildMain()
        {
            _main = new VisualElement();
            _main.style.flexDirection = FlexDirection.Row;
            _main.style.flexGrow = 1f;
            _main.style.minWidth = 0f;
            _main.style.minHeight = 0f;
            _main.style.overflow = Overflow.Hidden;
            rootVisualElement.Add(_main);

            _assetColumn = BuildAssetColumn();
            _main.Add(_assetColumn);
            _detail = new ScrollView();
            _detail.style.flexGrow = 1f;
            _detail.style.minWidth = 0f;
            _detail.style.minHeight = 0f;
            _detail.style.paddingLeft = 12f;
            _detail.style.paddingRight = 12f;
            _detail.style.paddingTop = 8f;
            _main.Add(_detail);

            _summary = new ScrollView();
            _summary.style.width = 245f;
            _summary.style.flexShrink = 0f;
            _summary.style.backgroundColor = Bg1;
            _summary.style.borderLeftColor = Border;
            _summary.style.borderLeftWidth = 1f;
            _summary.style.paddingLeft = 10f;
            _summary.style.paddingRight = 10f;
            _main.Add(_summary);
        }

        private VisualElement BuildAssetColumn()
        {
            var column = new VisualElement();
            column.style.width = 270f;
            column.style.flexShrink = 0f;
            column.style.minWidth = 0f;
            column.style.minHeight = 0f;
            column.style.backgroundColor = Bg1;
            column.style.borderRightColor = Border;
            column.style.borderRightWidth = 1f;

            var header = SectionHeader("에셋 목록");
            column.Add(header);

            var filters = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            filters.style.paddingLeft = 6f;
            filters.style.paddingRight = 6f;
            filters.style.paddingTop = 6f;
            filters.style.overflow = Overflow.Hidden;
            filters.style.flexShrink = 0f;
            _search = new ToolbarSearchField();
            _search.style.flexGrow = 1f;
            _search.style.flexShrink = 1f;
            _search.style.flexBasis = 0f;
            _search.style.minWidth = 0f;
            _search.style.width = StyleKeyword.Auto;
            _search.RegisterValueChangedCallback(_ => ApplyFilter());
            filters.Add(_search);

            _filterMenu = new ToolbarMenu { text = "전체" };
            _filterMenu.style.width = 62f;
            _filterMenu.style.minWidth = 62f;
            _filterMenu.style.maxWidth = 62f;
            _filterMenu.style.flexShrink = 0f;
            _filterMenu.style.marginLeft = 4f;
            foreach (string filter in new[] { "전체", "Ability", "Effect", "Set" })
            {
                string captured = filter;
                _filterMenu.menu.AppendAction(filter, _ =>
                {
                    _filter = captured;
                    _filterMenu.text = captured;
                    ApplyFilter();
                });
            }
            filters.Add(_filterMenu);
            column.Add(filters);

            _assetList = new ListView(_filtered, 48f, MakeAssetRow, BindAssetRow)
            {
                selectionType = SelectionType.Multiple,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                reorderable = false,
                showBorder = false,
            };
            _assetList.style.flexGrow = 1f;
            _assetList.selectionChanged += OnSelectionChanged;
            column.Add(_assetList);

            // 마이그레이션 UI는 기능 완성 후 다시 노출한다.
            // 변환 구현은 보존하되 현재 저작 화면에서는 CRUD와 검증에 집중한다.
            return column;
        }

        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            if (_main == null || _assetColumn == null || _detail == null || _summary == null)
                return;

            float width = evt.newRect.width;
            bool verticalCompact = width < 560f;
            bool hideSummary = width < 920f;
            bool hideToolbarPath = width < 760f;

            _summary.style.display = hideSummary ? DisplayStyle.None : DisplayStyle.Flex;
            if (_toolbarPathLabel != null)
                _toolbarPathLabel.style.display =
                    hideToolbarPath ? DisplayStyle.None : DisplayStyle.Flex;
            if (_toolbarRow != null)
                _toolbarRow.style.minWidth = width;
            if (_tabsRow != null)
                _tabsRow.style.minWidth = width;

            if (verticalCompact)
            {
                _main.style.flexDirection = FlexDirection.Column;
                _assetColumn.style.width = StyleKeyword.Auto;
                _assetColumn.style.height = 210f;
                _assetColumn.style.flexGrow = 0f;
                _detail.style.flexGrow = 1f;
                _detail.style.width = StyleKeyword.Auto;
            }
            else
            {
                _main.style.flexDirection = FlexDirection.Row;
                _assetColumn.style.width = width < 920f ? 230f : 270f;
                _assetColumn.style.height = StyleKeyword.Auto;
                _assetColumn.style.flexGrow = 0f;
                _detail.style.flexGrow = 1f;
                _detail.style.width = StyleKeyword.Auto;
            }
        }

        private VisualElement MakeAssetRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 7f;
            row.style.paddingRight = 5f;
            row.style.borderBottomColor = new Color(0.15f, 0.18f, 0.22f);
            row.style.borderBottomWidth = 1f;

            var icon = new Image { name = "icon", scaleMode = ScaleMode.ScaleToFit };
            icon.style.width = 34f;
            icon.style.height = 34f;
            icon.style.marginRight = 7f;
            row.Add(icon);

            var labels = new VisualElement();
            labels.style.flexGrow = 1f;
            var name = new Label { name = "name" };
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            labels.Add(name);
            var type = new Label { name = "type" };
            type.style.fontSize = 10f;
            type.style.color = new Color(0.55f, 0.6f, 0.68f);
            labels.Add(type);
            row.Add(labels);

            var badge = new Label { name = "badge" };
            badge.style.unityTextAlign = TextAnchor.MiddleCenter;
            badge.style.minWidth = 20f;
            row.Add(badge);
            return row;
        }

        private void BindAssetRow(VisualElement row, int index)
        {
            if ((uint)index >= (uint)_filtered.Count) return;
            UnityEngine.Object asset = _filtered[index];
            row.Q<Label>("name").text = GetStableId(asset);
            row.Q<Label>("type").text = asset.GetType().Name;
            row.Q<Image>("icon").image = GetIcon(asset);

            List<AbilityValidationIssue> issues = AbilityDataValidator.Validate(asset);
            int errors = issues.Count(x => x.Severity == AbilityValidationSeverity.Error);
            int warnings = issues.Count(x => x.Severity == AbilityValidationSeverity.Warning);
            Label badge = row.Q<Label>("badge");
            badge.text = errors > 0 ? $"✕ {errors}" : warnings > 0 ? $"⚠ {warnings}" : "✓";
            badge.style.color = errors > 0
                ? new Color(1f, 0.35f, 0.35f)
                : warnings > 0 ? new Color(1f, 0.75f, 0.2f) : new Color(0.35f, 0.85f, 0.55f);
        }

        private void OnSelectionChanged(IEnumerable<object> selected)
        {
            _selected = selected.OfType<UnityEngine.Object>().FirstOrDefault();
            _pathLabel.text = _selected != null
                ? AssetDatabase.GetAssetPath(_selected)
                : "에셋을 선택하세요";
            RebuildDetail();
        }

        private void RebuildDetail()
        {
            if (_detail == null || _summary == null) return;
            _detail.Clear();
            _summary.Clear();
            if (_selected == null)
            {
                var empty = new Label("왼쪽 목록에서 Ability, Effect 또는 Set을 선택하세요.");
                empty.style.marginTop = 40f;
                empty.style.unityTextAlign = TextAnchor.MiddleCenter;
                _detail.Add(empty);
                return;
            }

            var serialized = new SerializedObject(_selected);
            // TrackSerializedObjectValue는 한 VisualElement가 수명 동안 하나의
            // SerializedObject만 추적할 수 있다. 탭 전환마다 재사용되는 _detail에
            // 직접 등록하지 않고, Rebuild 시 함께 폐기되는 컨테이너를 사용한다.
            var bindingRoot = new VisualElement();
            bindingRoot.Add(SectionHeader($"{_activeTab} · {_selected.name}"));
            _detail.Add(bindingRoot);

            string[] properties = GetPropertiesForTab(_selected, _activeTab);
            if (properties.Length == 0)
            {
                bindingRoot.Add(new HelpBox(
                    "이 에셋 타입에는 현재 탭에서 편집할 항목이 없습니다.",
                    HelpBoxMessageType.Info));
            }
            for (int i = 0; i < properties.Length; i++)
            {
                SerializedProperty property = serialized.FindProperty(properties[i]);
                if (property == null) continue;
                var field = new PropertyField(property);
                field.style.marginTop = 5f;
                field.Bind(serialized);
                bindingRoot.Add(field);
            }

            bindingRoot.TrackSerializedObjectValue(serialized, _ =>
            {
                EditorUtility.SetDirty(_selected);
                RebuildSummary();
                RebuildValidation();
                _assetList?.RefreshItems();
            });
            RebuildSummary();
            RebuildValidation();
        }

        private void RebuildSummary()
        {
            _summary.Clear();
            _summary.Add(SectionHeader("요약 정보"));
            AddSummary("에셋 타입", _selected?.GetType().Name ?? "-");
            AddSummary("안정 ID", GetStableId(_selected));

            if (_selected is GameplayAbilitySO ability)
            {
                AddSummary("분류", ability.presentation?.category.ToString());
                AddSummary("Variant", (ability.variants?.Count ?? 0).ToString());
                AddSummary("비용", $"{ability.cost?.resourceType} / {ability.cost?.policy}");
                AddSummary("쿨다운", $"{ability.cooldown?.durationSeconds:0.##}s");
                AddSummary("공유 그룹", ability.cooldown?.ResolveGroupId(ability.abilityId));
            }
            else if (_selected is GameplayEffectSO effect)
            {
                AddSummary("지속 타입", effect.durationType.ToString());
                AddSummary("지속 시간", $"{effect.durationSeconds:0.##}s");
                AddSummary("주기", effect.IsPeriodic ? $"{effect.periodSeconds:0.##}s" : "없음");
                AddSummary("최대 스택", effect.maxStackCount.ToString());
            }

            _summary.Add(SectionHeader("현재 상태"));
            _validation = new VisualElement();
            _summary.Add(_validation);
        }

        private void RebuildValidation()
        {
            if (_validation == null) return;
            _validation.Clear();
            List<AbilityValidationIssue> issues = AbilityDataValidator.Validate(_selected);
            int errors = issues.Count(x => x.Severity == AbilityValidationSeverity.Error);
            int warnings = issues.Count(x => x.Severity == AbilityValidationSeverity.Warning);
            AddValidationLine($"✕ 오류 {errors}", new Color(1f, 0.35f, 0.35f));
            AddValidationLine($"⚠ 경고 {warnings}", new Color(1f, 0.75f, 0.2f));
            AddValidationLine($"ⓘ 정보 {issues.Count - errors - warnings}", new Color(0.35f, 0.65f, 1f));

            for (int i = 0; i < issues.Count; i++)
            {
                AbilityValidationIssue issue = issues[i];
                var box = new HelpBox(issue.Message, issue.Severity switch
                {
                    AbilityValidationSeverity.Error => HelpBoxMessageType.Error,
                    AbilityValidationSeverity.Warning => HelpBoxMessageType.Warning,
                    _ => HelpBoxMessageType.Info,
                });
                box.style.marginTop = 4f;
                _validation.Add(box);
            }
        }

        private void RefreshAssets()
        {
            _assets.Clear();
            LoadAssets<GameplayAbilitySO>();
            LoadAssets<GameplayEffectSO>();
            LoadAssets<AbilitySetSO>();
            _assets.Sort((a, b) => string.Compare(GetStableId(a), GetStableId(b), StringComparison.Ordinal));
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            _filtered.Clear();
            string query = _search?.value?.Trim() ?? string.Empty;
            for (int i = 0; i < _assets.Count; i++)
            {
                UnityEngine.Object asset = _assets[i];
                if (!MatchesType(asset)) continue;
                if (!string.IsNullOrEmpty(query)
                    && GetStableId(asset).IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0
                    && asset.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                _filtered.Add(asset);
            }
            _assetList?.RefreshItems();
            RestoreListSelection();
        }

        private void RestoreListSelection()
        {
            if (_assetList == null) return;

            if (_filtered.Count == 0)
            {
                _assetList.ClearSelection();
                _selected = null;
                _pathLabel.text = "에셋을 선택하세요";
                RebuildDetail();
                return;
            }

            int selectedIndex = _selected != null ? _filtered.IndexOf(_selected) : -1;
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
                _selected = _filtered[0];
            }

            _assetList.SetSelection(selectedIndex);
            _assetList.ScrollToItem(selectedIndex);
            _pathLabel.text = AssetDatabase.GetAssetPath(_selected);
            RebuildDetail();
        }

        private bool MatchesType(UnityEngine.Object asset) => _filter switch
        {
            "Ability" => asset is GameplayAbilitySO,
            "Effect" => asset is GameplayEffectSO,
            "Set" => asset is AbilitySetSO,
            _ => true,
        };

        private void LoadAssets<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            for (int i = 0; i < guids.Length; i++)
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (asset != null) _assets.Add(asset);
            }
        }

        private void CreateAsset<T>(string prefix) where T : ScriptableObject
        {
            string path = EditorUtility.SaveFilePanelInProject(
                $"{typeof(T).Name} 생성", prefix, "asset", "저장 위치를 선택하세요.",
                "Assets/10.Datas/Ability");
            if (string.IsNullOrEmpty(path)) return;
            T asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            Undo.RegisterCreatedObjectUndo(asset, $"{typeof(T).Name} 생성");
            AssetDatabase.SaveAssets();
            RefreshAssets();
            Selection.activeObject = asset;
            _selected = asset;
            RebuildDetail();
        }

        private void SaveSelected()
        {
            if (_selected != null) EditorUtility.SetDirty(_selected);
            AssetDatabase.SaveAssets();
            RebuildValidation();
            ShowNotification(new GUIContent("저장 및 검증 완료"));
        }

        private void DeleteSelected()
        {
            if (_selected == null)
            {
                ShowNotification(new GUIContent("삭제할 에셋을 선택하세요."));
                return;
            }

            string path = AssetDatabase.GetAssetPath(_selected);
            if (string.IsNullOrWhiteSpace(path) || !AssetDatabase.IsMainAsset(_selected))
            {
                EditorUtility.DisplayDialog(
                    "삭제할 수 없음",
                    "프로젝트의 메인 Ability/Effect/Set 에셋만 삭제할 수 있습니다.",
                    "확인");
                return;
            }

            List<string> references = FindReferencingAssetPaths(path);
            string referenceText = references.Count == 0
                ? "이 에셋을 영구 삭제합니다. 이 작업은 Undo로 복원할 수 없습니다."
                : $"이 에셋을 참조하는 에셋이 {references.Count}개 있습니다.\n\n"
                  + string.Join("\n", references.Take(6))
                  + (references.Count > 6 ? "\n…" : string.Empty)
                  + "\n\n삭제하면 해당 참조가 Missing 상태가 될 수 있습니다.";

            int choice;
            if (references.Count == 0)
            {
                choice = EditorUtility.DisplayDialog(
                    $"{_selected.GetType().Name} 삭제",
                    $"'{_selected.name}'\n{path}\n\n{referenceText}",
                    "삭제",
                    "취소")
                    ? 0
                    : 1;
            }
            else
            {
                choice = EditorUtility.DisplayDialogComplex(
                    $"{_selected.GetType().Name} 삭제",
                    $"'{_selected.name}'\n{path}\n\n{referenceText}",
                    "참조 무시하고 삭제",
                    "취소",
                    "첫 참조 선택");
            }
            if (choice == 2 && references.Count > 0)
            {
                Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(references[0]);
                EditorGUIUtility.PingObject(Selection.activeObject);
                return;
            }
            if (choice != 0) return;

            string deletedName = _selected.name;
            _selected = null;
            _assetList?.ClearSelection();
            if (!AssetDatabase.DeleteAsset(path))
            {
                EditorUtility.DisplayDialog(
                    "삭제 실패",
                    $"에셋을 삭제하지 못했습니다.\n{path}",
                    "확인");
                RefreshAssets();
                return;
            }

            AssetDatabase.SaveAssets();
            RefreshAssets();
            ShowNotification(new GUIContent($"'{deletedName}' 삭제 완료"));
        }

        private static List<string> FindReferencingAssetPaths(string targetPath)
        {
            var result = new List<string>();
            string[] allPaths = AssetDatabase.GetAllAssetPaths();
            for (int i = 0; i < allPaths.Length; i++)
            {
                string candidate = allPaths[i];
                if (candidate == targetPath
                    || !candidate.StartsWith("Assets/", StringComparison.Ordinal)
                    || AssetDatabase.IsValidFolder(candidate))
                    continue;

                string extension = System.IO.Path.GetExtension(candidate);
                if (extension is not (".asset" or ".prefab" or ".unity"))
                    continue;

                string[] dependencies = AssetDatabase.GetDependencies(candidate, false);
                for (int j = 0; j < dependencies.Length; j++)
                {
                    if (!string.Equals(
                            dependencies[j], targetPath, StringComparison.Ordinal))
                        continue;
                    result.Add(candidate);
                    break;
                }
            }
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private void ValidateAll()
        {
            List<AbilityValidationIssue> issues = AbilityDataValidator.ValidateAll();
            int errors = issues.Count(x => x.Severity == AbilityValidationSeverity.Error);
            int warnings = issues.Count(x => x.Severity == AbilityValidationSeverity.Warning);
            Debug.Log($"[AbilityValidator] 완료: 오류 {errors}, 경고 {warnings}, 전체 {issues.Count}");
            for (int i = 0; i < issues.Count; i++)
            {
                AbilityValidationIssue issue = issues[i];
                if (issue.Severity == AbilityValidationSeverity.Error)
                    Debug.LogError(issue.Message, issue.Context);
                else if (issue.Severity == AbilityValidationSeverity.Warning)
                    Debug.LogWarning(issue.Message, issue.Context);
            }
            RefreshAssets();
            RebuildValidation();
        }

        private void ConvertLegacy()
        {
            if (_legacySource.value is not PlayerAttackDataSO source)
            {
                ShowNotification(new GUIContent("PlayerAttackDataSO를 선택하세요."));
                return;
            }
            string absolute = EditorUtility.OpenFolderPanel(
                "변환 데이터 저장 폴더", Application.dataPath + "/10.Datas/Ability", "");
            if (string.IsNullOrEmpty(absolute)) return;
            string normalized = absolute.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (!normalized.StartsWith(dataPath, StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("잘못된 경로", "Assets 내부 폴더를 선택하세요.", "확인");
                return;
            }
            string folder = "Assets" + normalized.Substring(dataPath.Length);
            AbilityMigrationUtility.Convert(source, folder);
            RefreshAssets();
        }

        private void HandleUndoRedo()
        {
            RefreshAssets();
            RebuildDetail();
        }

        private static string[] GetPropertiesForTab(UnityEngine.Object target, string tab)
        {
            if (target is GameplayAbilitySO)
            {
                return tab switch
                {
                    "기본 정보" => new[] { "abilityId", "schemaVersion", "presentation", "abilityTagIds", "concurrency" },
                    "활성화 조건" => new[] { "activation" },
                    "비용/쿨다운" => new[] { "cost", "cooldown" },
                    "Variant" => new[] { "variants" },
                    "Effect" => new[] { "commitEffects", "endEffects" },
                    "Cue" => new[] { "cues" },
                    "저장/교체 정책" => new[] { "persistence" },
                    "정적 밸런스" => new[] { "balance" },
                    _ => Array.Empty<string>(),
                };
            }
            if (target is GameplayEffectSO)
            {
                return tab switch
                {
                    "기본 정보" => new[] { "effectId", "schemaVersion", "durationType", "durationSeconds", "periodSeconds" },
                    "Effect" => new[] { "stackingKey", "stackPolicy", "maxStackCount", "modifiers", "resourceOperations", "grantedTagIds" },
                    "저장/교체 정책" => new[] { "removalPolicy", "savePolicy" },
                    _ => Array.Empty<string>(),
                };
            }
            if (target is AbilitySetSO)
                return tab == "기본 정보"
                    ? new[] { "playerSlots", "additionalAbilities" }
                    : Array.Empty<string>();
            return Array.Empty<string>();
        }

        private static string GetStableId(UnityEngine.Object asset) => asset switch
        {
            GameplayAbilitySO ability => string.IsNullOrWhiteSpace(ability.abilityId) ? ability.name : ability.abilityId,
            GameplayEffectSO effect => string.IsNullOrWhiteSpace(effect.effectId) ? effect.name : effect.effectId,
            _ => asset != null ? asset.name : "-",
        };

        private static Texture GetIcon(UnityEngine.Object asset)
        {
            if (asset is GameplayAbilitySO ability && ability.presentation?.icon != null)
                return ability.presentation.icon.texture;
            return AssetPreview.GetMiniThumbnail(asset);
        }

        private static ToolbarButton MakeToolbarButton(string text, Action action)
        {
            var button = new ToolbarButton(action) { text = text };
            button.style.marginLeft = 3f;
            return button;
        }

        private static VisualElement SectionHeader(string text)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.backgroundColor = Bg2;
            label.style.paddingLeft = 8f;
            label.style.paddingTop = 6f;
            label.style.paddingBottom = 6f;
            label.style.borderBottomColor = Border;
            label.style.borderBottomWidth = 1f;
            return label;
        }

        private void AddSummary(string label, string value)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            row.style.paddingTop = 4f;
            row.style.paddingBottom = 4f;
            var key = new Label(label);
            key.style.width = 78f;
            key.style.color = new Color(0.55f, 0.6f, 0.68f);
            row.Add(key);
            var val = new Label(value ?? "-");
            val.style.flexGrow = 1f;
            val.style.whiteSpace = WhiteSpace.Normal;
            row.Add(val);
            _summary.Add(row);
        }

        private void AddValidationLine(string text, Color color)
        {
            var label = new Label(text);
            label.style.color = color;
            label.style.marginTop = 4f;
            _validation.Add(label);
        }
    }
}
