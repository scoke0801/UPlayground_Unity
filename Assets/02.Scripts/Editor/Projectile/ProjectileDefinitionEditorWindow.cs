using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Projectile;

namespace UPlayGround.Editor.Projectile
{
    /// <summary>
    /// ProjectileDefinition을 목록, 조합, 검증, 궤적 순서로 저작하는 UI Toolkit 창.
    /// SerializeReference 변경은 SerializedProperty를 통해서만 수행한다.
    /// </summary>
    public sealed class ProjectileDefinitionEditorWindow : EditorWindow
    {
        private const string CommonStylePath =
            "Assets/02.Scripts/Editor/UIToolkit/Styles/UPlayGroundEditor.uss";
        private const string ProjectileStylePath =
            "Assets/02.Scripts/Editor/Projectile/ProjectileEditor.uss";
        private const string DefaultAssetFolder = "Assets/10.Datas/Projectile";
        private const string SidebarWidthPrefs = "ProjectileEditor.SidebarWidth";

        private sealed class DefinitionItem
        {
            public ProjectileDefinitionSO Asset;
            public string Path;
        }

        private static readonly (string Label, Type Type)[] MotionTypes =
        {
            ("직선 Linear", typeof(LinearProjectileMotion)),
            ("포물선 Arc", typeof(ArcProjectileMotion)),
            ("유도 Homing", typeof(HomingProjectileMotion)),
            ("고정 Stationary", typeof(StationaryProjectileMotion)),
            ("궤도 Orbit", typeof(OrbitProjectileMotion)),
            ("히트스캔 Hitscan", typeof(HitscanProjectileMotion)),
        };

        private static readonly (string Label, Type Type)[] BehaviorTypes =
        {
            ("관통 Pierce", typeof(PierceProjectileBehavior)),
            ("튕김 Bounce", typeof(BounceProjectileBehavior)),
            ("분열 Split", typeof(SplitProjectileBehavior)),
            ("기폭 Detonate", typeof(DetonateProjectileBehavior)),
            ("범위 틱 Area Tick", typeof(AreaTickProjectileBehavior)),
            ("부착 Attach", typeof(AttachProjectileBehavior)),
            ("반사 가능 Reflectable", typeof(ReflectableProjectileBehavior)),
        };

        private readonly List<DefinitionItem> _allItems = new();
        private readonly List<DefinitionItem> _visibleItems = new();
        private readonly List<string> _validationErrors = new();

        private ToolbarSearchField _search;
        private ListView _list;
        private VisualElement _detailRoot;
        private VisualElement _validationRoot;
        private Label _statusLabel;
        private Label _analysisLabel;
        private ProjectileTrajectoryPreviewElement _preview;
        private ProjectileDefinitionSO _selected;
        private SerializedObject _serializedDefinition;
        private bool _suppressListSelection;
        private bool _analysisRefreshScheduled;

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/게임플레이/투사체 에디터",
            priority = 160)]
        public static void Open()
        {
            GetWindow<ProjectileDefinitionEditorWindow>("투사체 에디터");
        }

        public static void Open(ProjectileDefinitionSO definition)
        {
            ProjectileDefinitionEditorWindow window =
                GetWindow<ProjectileDefinitionEditorWindow>("투사체 에디터");
            window.SelectDefinition(definition);
            window.Focus();
        }

        [OnOpenAsset]
        private static bool OpenDefinitionAsset(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not ProjectileDefinitionSO definition)
                return false;
            Open(definition);
            return true;
        }

        private void OnEnable()
        {
            minSize = new Vector2(900f, 560f);
            Undo.undoRedoPerformed += HandleUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedo;
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();
            root.AddToClassList("up-editor-root");
            root.AddToClassList("up-projectile-editor");
            root.AddToClassList(EditorGUIUtility.isProSkin
                ? "up-theme-dark"
                : "up-theme-light");
            AddStyle(root, CommonStylePath);
            AddStyle(root, ProjectileStylePath);

            root.Add(BuildToolbar());

            float sidebarWidth = Mathf.Clamp(
                EditorPrefs.GetFloat(SidebarWidthPrefs, 310f),
                260f,
                480f);
            var split = new TwoPaneSplitView(
                0,
                sidebarWidth,
                TwoPaneSplitViewOrientation.Horizontal);
            split.AddToClassList("up-projectile-main-split");

            VisualElement sidebar = BuildSidebar();
            sidebar.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (evt.newRect.width > 0f)
                    EditorPrefs.SetFloat(SidebarWidthPrefs, evt.newRect.width);
            });
            split.Add(sidebar);

            _detailRoot = new VisualElement();
            _detailRoot.AddToClassList("up-projectile-detail-root");
            split.Add(_detailRoot);
            root.Add(split);

            _statusLabel = new Label();
            _statusLabel.AddToClassList("up-projectile-status");
            root.Add(_statusLabel);

            RefreshAssetList();
            if (_selected == null && Selection.activeObject is ProjectileDefinitionSO selected)
                _selected = selected;
            if (_selected == null && _allItems.Count > 0)
                _selected = _allItems[0].Asset;
            SelectDefinition(_selected);
        }

        private VisualElement BuildToolbar()
        {
            var toolbar = new Toolbar();
            toolbar.AddToClassList("up-projectile-toolbar");

            var title = new Label("PROJECTILE AUTHORING");
            title.AddToClassList("up-projectile-toolbar-title");
            toolbar.Add(title);
            toolbar.Add(new ToolbarSpacer { flex = true });
            toolbar.Add(new ToolbarButton(CreateDefinition) { text = "+ 새 Definition" });
            toolbar.Add(new ToolbarButton(DuplicateSelected) { text = "복제" });
            toolbar.Add(new ToolbarButton(FindReferences) { text = "사용처" });
            toolbar.Add(new ToolbarButton(ValidateAllDefinitions) { text = "전체 검증" });
            toolbar.Add(new ToolbarButton(RefreshAssetList) { text = "새로고침" });
            return toolbar;
        }

        private VisualElement BuildSidebar()
        {
            var sidebar = new VisualElement();
            sidebar.AddToClassList("up-projectile-sidebar");

            sidebar.Add(BuildPanelHeader("DEFINITIONS", "투사체 정의"));

            _search = new ToolbarSearchField();
            _search.AddToClassList("up-projectile-search");
            _search.RegisterValueChangedCallback(_ => RebuildVisibleList());
            sidebar.Add(_search);

            _list = new ListView
            {
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                showAlternatingRowBackgrounds = AlternatingRowBackground.None,
                reorderable = false,
                makeItem = MakeListItem,
                bindItem = BindListItem,
            };
            _list.AddToClassList("up-projectile-list");
            _list.selectionChanged += HandleListSelection;
            sidebar.Add(_list);
            return sidebar;
        }

        private static VisualElement MakeListItem()
        {
            var row = new VisualElement();
            row.AddToClassList("up-projectile-list-row");
            var name = new Label { name = "name" };
            name.AddToClassList("up-projectile-list-name");
            row.Add(name);
            var subtitle = new Label { name = "subtitle" };
            subtitle.AddToClassList("up-projectile-list-subtitle");
            row.Add(subtitle);
            return row;
        }

        private void BindListItem(VisualElement element, int index)
        {
            DefinitionItem item = _visibleItems[index];
            element.Q<Label>("name").text = item.Asset != null ? item.Asset.name : "(Missing)";
            element.Q<Label>("subtitle").text = BuildItemSubtitle(item.Asset);
            element.EnableInClassList(
                "up-projectile-list-row-selected",
                item.Asset == _selected);
        }

        private static string BuildItemSubtitle(ProjectileDefinitionSO definition)
        {
            if (definition == null)
                return "참조 누락";
            string motion = GetFriendlyTypeName(definition.motion);
            int behaviorCount = definition.behaviors?.Count ?? 0;
            return $"{motion}  ·  Behavior {behaviorCount}  ·  Pool {definition.prewarmCount}/{definition.maxPoolSize}";
        }

        private void HandleListSelection(IEnumerable<object> selection)
        {
            if (_suppressListSelection)
                return;
            DefinitionItem item = selection.OfType<DefinitionItem>().FirstOrDefault();
            if (item?.Asset != null)
                SelectDefinition(item.Asset);
        }

        private void SelectDefinition(ProjectileDefinitionSO definition)
        {
            _selected = definition;
            Selection.activeObject = definition;
            SyncListSelection();
            BuildDetail();
        }

        private void SyncListSelection()
        {
            if (_list == null)
                return;
            int index = _visibleItems.FindIndex(item => item.Asset == _selected);
            _suppressListSelection = true;
            if (index >= 0)
            {
                _list.SetSelectionWithoutNotify(new[] { index });
                _list.ScrollToItem(index);
            }
            else
            {
                _list.ClearSelection();
            }
            _list.RefreshItems();
            _suppressListSelection = false;
        }

        private void BuildDetail()
        {
            if (_detailRoot == null)
                return;
            _detailRoot.Unbind();
            _detailRoot.Clear();

            if (_selected == null)
            {
                var empty = new Label("왼쪽 목록에서 ProjectileDefinition을 선택하세요.");
                empty.AddToClassList("up-empty-hint");
                _detailRoot.Add(empty);
                UpdateStatus();
                return;
            }

            _serializedDefinition = new SerializedObject(_selected);
            _serializedDefinition.Update();

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("up-projectile-detail-scroll");
            scroll.RegisterCallback<SerializedPropertyChangeEvent>(_ => ScheduleAnalysisRefresh());

            scroll.Add(BuildSelectionHeader());
            scroll.Add(BuildVisualSection());
            scroll.Add(BuildSimulationSection());
            scroll.Add(BuildMotionSection());
            scroll.Add(BuildBehaviorSection());
            scroll.Add(BuildPoolSection());
            scroll.Add(BuildAnalysisSection());

            _detailRoot.Add(scroll);
            _detailRoot.Bind(_serializedDefinition);
            RefreshAnalysis();
            UpdateStatus();
        }

        private VisualElement BuildSelectionHeader()
        {
            var header = new VisualElement();
            header.AddToClassList("up-projectile-selection");

            var title = new Label(_selected.name);
            title.AddToClassList("up-projectile-selection-title");
            header.Add(title);

            var path = new Label(AssetDatabase.GetAssetPath(_selected));
            path.AddToClassList("up-projectile-selection-path");
            header.Add(path);
            return header;
        }

        private VisualElement BuildVisualSection()
        {
            VisualElement body = BuildSection("VISUAL", "비주얼과 공통 피드백");
            AddField(body, "visualPrefab", "비주얼 프리팹");
            AddField(body, "hitEffectKey", "착탄 FX 키");
            AddField(body, "detachTrailOnReturn", "반환 전 트레일 유지");
            return body.parent;
        }

        private VisualElement BuildSimulationSection()
        {
            VisualElement body = BuildSection("SIMULATION", "수명과 충돌 정책");
            AddField(body, "lifetime", "수명");
            AddField(body, "collisionRadius", "충돌 반경");
            AddField(body, "destroyOnHit", "충돌 시 소멸");
            AddField(body, "inheritOwnerTimeScale", "소유자 시간축 상속");
            return body.parent;
        }

        private VisualElement BuildMotionSection()
        {
            VisualElement body = BuildSection("MOTION", "이동 전략");
            SerializedProperty motion = _serializedDefinition.FindProperty("motion");
            Type currentType = motion.managedReferenceValue?.GetType();
            int currentIndex = Math.Max(
                0,
                Array.FindIndex(MotionTypes, entry => entry.Type == currentType));
            var choices = MotionTypes.Select(entry => entry.Label).ToList();
            var popup = new PopupField<string>("이동 전략", choices, currentIndex);
            popup.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            popup.RegisterValueChangedCallback(evt =>
            {
                int index = choices.IndexOf(evt.newValue);
                if (index >= 0)
                    SetManagedReference(
                        _serializedDefinition.FindProperty("motion"),
                        MotionTypes[index].Type,
                        "투사체 이동 전략 변경");
            });
            body.Add(popup);

            if (motion.managedReferenceValue != null)
            {
                var details = new PropertyField(motion.Copy(), "이동 세부 설정");
                details.AddToClassList("up-projectile-managed-field");
                body.Add(details);
            }
            return body.parent;
        }

        private VisualElement BuildBehaviorSection()
        {
            VisualElement body = BuildSection("BEHAVIORS", "충돌·명중·만료 동작 조합");
            SerializedProperty behaviors = _serializedDefinition.FindProperty("behaviors");

            for (int i = 0; i < behaviors.arraySize; i++)
            {
                int index = i;
                SerializedProperty element = behaviors.GetArrayElementAtIndex(i);
                var card = new VisualElement();
                card.AddToClassList("up-projectile-behavior-card");

                var header = new VisualElement();
                header.AddToClassList("up-projectile-behavior-header");
                var title = new Label($"{i + 1:00}  {GetManagedReferenceDisplayName(element)}");
                title.AddToClassList("up-projectile-behavior-title");
                header.Add(title);
                header.Add(new VisualElement { style = { flexGrow = 1f } });
                header.Add(new Button(() => MoveBehavior(index, index - 1)) { text = "↑" });
                header.Add(new Button(() => MoveBehavior(index, index + 1)) { text = "↓" });
                var remove = new Button(() => RemoveBehavior(index)) { text = "삭제" };
                remove.AddToClassList("up-projectile-danger-button");
                header.Add(remove);
                card.Add(header);

                if (element.managedReferenceValue != null)
                    card.Add(new PropertyField(element.Copy(), null));
                body.Add(card);
            }

            var add = new Button { text = "+ Behavior 추가" };
            add.clicked += () => ShowBehaviorMenu(add);
            add.AddToClassList("up-projectile-add-behavior");
            body.Add(add);
            return body.parent;
        }

        private VisualElement BuildPoolSection()
        {
            VisualElement body = BuildSection("POOL & SAFETY", "프리워밍과 폭증 제한");
            AddField(body, "prewarmCount", "프리워밍");
            AddField(body, "maxPoolSize", "최대 풀 크기");
            AddField(body, "maxGeneration", "분열 최대 세대");
            return body.parent;
        }

        private VisualElement BuildAnalysisSection()
        {
            VisualElement body = BuildSection("ANALYSIS", "즉시 검증과 궤적 미리보기");
            _analysisLabel = new Label();
            _analysisLabel.AddToClassList("up-projectile-analysis");
            body.Add(_analysisLabel);

            _preview = new ProjectileTrajectoryPreviewElement();
            body.Add(_preview);

            _validationRoot = new VisualElement();
            _validationRoot.AddToClassList("up-projectile-validation");
            body.Add(_validationRoot);
            return body.parent;
        }

        private VisualElement BuildSection(string kicker, string title)
        {
            var section = new VisualElement();
            section.AddToClassList("up-inspector-section");
            var heading = new VisualElement();
            heading.AddToClassList("up-inspector-section-heading");
            var kickerLabel = new Label(kicker);
            kickerLabel.AddToClassList("up-inspector-section-kicker");
            heading.Add(kickerLabel);
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("up-inspector-section-title");
            heading.Add(titleLabel);
            section.Add(heading);
            var body = new VisualElement();
            body.AddToClassList("up-inspector-section-body");
            section.Add(body);
            return body;
        }

        private static VisualElement BuildPanelHeader(string kicker, string title)
        {
            var header = new VisualElement();
            header.AddToClassList("up-panel-header");
            var kickerLabel = new Label(kicker);
            kickerLabel.AddToClassList("up-panel-kicker");
            header.Add(kickerLabel);
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("up-panel-title");
            header.Add(titleLabel);
            return header;
        }

        private void AddField(VisualElement body, string propertyName, string label)
        {
            SerializedProperty property = _serializedDefinition.FindProperty(propertyName);
            if (property != null)
                body.Add(new PropertyField(property, label));
        }

        private void SetManagedReference(
            SerializedProperty property,
            Type type,
            string undoName)
        {
            if (_selected == null || property == null || type == null)
                return;
            Undo.RecordObject(_selected, undoName);
            _serializedDefinition.Update();
            property.managedReferenceValue = Activator.CreateInstance(type);
            _serializedDefinition.ApplyModifiedProperties();
            EditorUtility.SetDirty(_selected);
            BuildDetail();
        }

        private void ShowBehaviorMenu(Button anchor)
        {
            var menu = new GenericMenu();
            foreach ((string label, Type type) in BehaviorTypes)
            {
                string capturedLabel = label;
                Type capturedType = type;
                menu.AddItem(
                    new GUIContent(capturedLabel),
                    false,
                    () => AddBehavior(capturedType));
            }
            menu.DropDown(anchor.worldBound);
        }

        private void AddBehavior(Type type)
        {
            if (_selected == null)
                return;
            Undo.RecordObject(_selected, "투사체 Behavior 추가");
            _serializedDefinition.Update();
            SerializedProperty behaviors = _serializedDefinition.FindProperty("behaviors");
            int index = behaviors.arraySize;
            behaviors.arraySize++;
            behaviors.GetArrayElementAtIndex(index).managedReferenceValue =
                Activator.CreateInstance(type);
            _serializedDefinition.ApplyModifiedProperties();
            EditorUtility.SetDirty(_selected);
            BuildDetail();
        }

        private void RemoveBehavior(int index)
        {
            if (_selected == null)
                return;
            Undo.RecordObject(_selected, "투사체 Behavior 삭제");
            _serializedDefinition.Update();
            SerializedProperty behaviors = _serializedDefinition.FindProperty("behaviors");
            if (index >= 0 && index < behaviors.arraySize)
                behaviors.DeleteArrayElementAtIndex(index);
            _serializedDefinition.ApplyModifiedProperties();
            EditorUtility.SetDirty(_selected);
            BuildDetail();
        }

        private void MoveBehavior(int from, int to)
        {
            if (_selected == null)
                return;
            _serializedDefinition.Update();
            SerializedProperty behaviors = _serializedDefinition.FindProperty("behaviors");
            if (from < 0 || from >= behaviors.arraySize || to < 0 || to >= behaviors.arraySize)
                return;
            Undo.RecordObject(_selected, "투사체 Behavior 순서 변경");
            behaviors.MoveArrayElement(from, to);
            _serializedDefinition.ApplyModifiedProperties();
            EditorUtility.SetDirty(_selected);
            BuildDetail();
        }

        private void ScheduleAnalysisRefresh()
        {
            if (_analysisRefreshScheduled)
                return;
            _analysisRefreshScheduled = true;
            _detailRoot.schedule.Execute(() =>
            {
                _analysisRefreshScheduled = false;
                if (_selected != null)
                    EditorUtility.SetDirty(_selected);
                RefreshAnalysis();
                _list?.RefreshItems();
                UpdateStatus();
            });
        }

        private void RefreshAnalysis()
        {
            if (_selected == null)
                return;
            _validationErrors.Clear();
            _selected.CollectValidationErrors(_validationErrors);

            if (_analysisLabel != null)
            {
                string motion = GetFriendlyTypeName(_selected.motion);
                float estimatedDistance = EstimateTravelDistance(_selected);
                int splitPeak = EstimateSplitPeak(_selected);
                _analysisLabel.text =
                    $"전략  {motion}\n"
                    + $"예상 이동 거리  {estimatedDistance:0.##} m\n"
                    + $"풀  {_selected.prewarmCount} prewarm / {_selected.maxPoolSize} max\n"
                    + $"분열 트리 최대  약 {splitPeak}개";
            }

            _preview?.SetDefinition(_selected);
            if (_validationRoot == null)
                return;
            _validationRoot.Clear();
            if (_validationErrors.Count == 0)
            {
                _validationRoot.Add(new HelpBox(
                    "저장 가능한 조합입니다.",
                    HelpBoxMessageType.Info));
                return;
            }
            foreach (string error in _validationErrors)
                _validationRoot.Add(new HelpBox(error, HelpBoxMessageType.Error));
        }

        private static float EstimateTravelDistance(ProjectileDefinitionSO definition)
        {
            float lifetime = Mathf.Max(0f, definition.lifetime);
            return definition.motion switch
            {
                LinearProjectileMotion linear => linear.speed * lifetime
                    + 0.5f * linear.acceleration * lifetime * lifetime,
                ArcProjectileMotion arc => arc.speed * lifetime,
                HomingProjectileMotion homing => homing.speed * lifetime,
                OrbitProjectileMotion orbit => 2f * Mathf.PI * orbit.radius
                    * Mathf.Abs(orbit.angularSpeed) / 360f * lifetime,
                HitscanProjectileMotion hitscan => hitscan.range,
                _ => 0f,
            };
        }

        private static int EstimateSplitPeak(ProjectileDefinitionSO root)
        {
            int total = 1;
            ProjectileDefinitionSO current = root;
            int multiplier = 1;
            var visited = new HashSet<ProjectileDefinitionSO>();
            for (int generation = 0;
                 current != null && generation < Mathf.Max(0, root.maxGeneration);
                 generation++)
            {
                if (!visited.Add(current))
                    break;
                SplitProjectileBehavior split = current.GetBehavior<SplitProjectileBehavior>();
                if (split?.childDefinition == null)
                    break;
                multiplier *= Mathf.Max(1, split.count);
                total += multiplier;
                current = split.childDefinition;
            }
            return total;
        }

        private void RefreshAssetList()
        {
            ProjectileDefinitionSO previous = _selected;
            _allItems.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:ProjectileDefinitionSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ProjectileDefinitionSO asset =
                    AssetDatabase.LoadAssetAtPath<ProjectileDefinitionSO>(path);
                if (asset != null)
                    _allItems.Add(new DefinitionItem { Asset = asset, Path = path });
            }
            _allItems.Sort((a, b) =>
                string.Compare(a.Asset.name, b.Asset.name, StringComparison.OrdinalIgnoreCase));
            RebuildVisibleList();
            if (previous != null && _allItems.Any(item => item.Asset == previous))
                _selected = previous;
            UpdateStatus();
        }

        private void RebuildVisibleList()
        {
            _visibleItems.Clear();
            string query = _search?.value?.Trim() ?? string.Empty;
            foreach (DefinitionItem item in _allItems)
            {
                if (string.IsNullOrEmpty(query)
                    || item.Asset.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                    || BuildItemSubtitle(item.Asset).IndexOf(
                        query,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    _visibleItems.Add(item);
            }
            if (_list != null)
            {
                _list.itemsSource = _visibleItems;
                _list.Rebuild();
                SyncListSelection();
            }
            UpdateStatus();
        }

        private void CreateDefinition()
        {
            EnsureDefaultFolder();
            string path = EditorUtility.SaveFilePanelInProject(
                "ProjectileDefinition 생성",
                "ProjectileDefinition",
                "asset",
                "저장할 위치를 선택하세요.",
                DefaultAssetFolder);
            if (string.IsNullOrWhiteSpace(path))
                return;
            var definition = CreateInstance<ProjectileDefinitionSO>();
            AssetDatabase.CreateAsset(definition, path);
            AssetDatabase.SaveAssets();
            RefreshAssetList();
            SelectDefinition(definition);
        }

        private void DuplicateSelected()
        {
            if (_selected == null)
                return;
            string source = AssetDatabase.GetAssetPath(_selected);
            string target = AssetDatabase.GenerateUniqueAssetPath(
                $"{System.IO.Path.GetDirectoryName(source)}/{_selected.name}_Copy.asset"
                    .Replace('\\', '/'));
            if (!AssetDatabase.CopyAsset(source, target))
                return;
            AssetDatabase.SaveAssets();
            RefreshAssetList();
            SelectDefinition(AssetDatabase.LoadAssetAtPath<ProjectileDefinitionSO>(target));
        }

        private void FindReferences()
        {
            if (_selected == null)
                return;
            string selectedPath = AssetDatabase.GetAssetPath(_selected);
            var references = new List<UnityEngine.Object>();
            string[] candidates = AssetDatabase
                .FindAssets("t:UPlayGroundMotionAbilityPayloadSO")
                .Concat(AssetDatabase.FindAssets("t:MotionSetAsset"))
                .Distinct()
                .ToArray();
            foreach (string guid in candidates)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.GetDependencies(path, false).Contains(selectedPath))
                {
                    UnityEngine.Object asset =
                        AssetDatabase.LoadMainAssetAtPath(path);
                    if (asset != null)
                        references.Add(asset);
                }
            }

            Selection.objects = references.ToArray();
            Debug.Log(references.Count == 0
                ? $"[ProjectileEditor] {_selected.name} 사용처가 없습니다."
                : $"[ProjectileEditor] {_selected.name} 사용처 {references.Count}개를 선택했습니다.");
        }

        private void ValidateAllDefinitions()
        {
            var errors = new List<string>();
            foreach (DefinitionItem item in _allItems)
                item.Asset?.CollectValidationErrors(errors);
            EditorUtility.DisplayDialog(
                "ProjectileDefinition 전체 검증",
                errors.Count == 0
                    ? $"{_allItems.Count}개 Definition이 모두 유효합니다."
                    : $"오류 {errors.Count}개\n\n{string.Join("\n", errors.Take(20))}"
                      + (errors.Count > 20 ? "\n…" : string.Empty),
                "확인");
        }

        private static void EnsureDefaultFolder()
        {
            if (AssetDatabase.IsValidFolder(DefaultAssetFolder))
                return;
            string current = "Assets";
            foreach (string part in DefaultAssetFolder.Split('/').Skip(1))
            {
                string next = $"{current}/{part}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private void HandleUndoRedo()
        {
            if (_selected == null)
                return;
            BuildDetail();
            _list?.RefreshItems();
        }

        private void UpdateStatus()
        {
            if (_statusLabel == null)
                return;
            _statusLabel.text =
                $"Definitions {_allItems.Count}  ·  표시 {_visibleItems.Count}"
                + (_selected != null ? $"  ·  선택 {_selected.name}" : string.Empty);
        }

        private static string GetManagedReferenceDisplayName(SerializedProperty property)
        {
            object value = property?.managedReferenceValue;
            return value != null ? GetFriendlyTypeName(value) : "(비어 있음)";
        }

        private static string GetFriendlyTypeName(object value)
        {
            if (value == null)
                return "None";
            string name = value.GetType().Name;
            return name
                .Replace("ProjectileMotion", string.Empty)
                .Replace("ProjectileBehavior", string.Empty);
        }

        private static void AddStyle(VisualElement root, string path)
        {
            StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (style != null)
                root.styleSheets.Add(style);
            else
                Debug.LogWarning($"Projectile Editor 스타일을 찾을 수 없습니다: {path}");
        }
    }
}
