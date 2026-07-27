using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Editor.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround;
using UPlayGround.Tool.Editor;

namespace UPlayGround.Actor.Editor
{
    /// <summary>
    /// ActorDatabase에 등록된 ActorDefinitionSO를 관리하는 UI Toolkit 에디터 창.
    /// 메뉴: UPlayGround/캐릭터/액터/액터 데이터베이스 에디터
    ///
    /// 목록/상세 분할선은 드래그로 폭을 조절할 수 있고, 조절한 폭은 EditorPrefs에 유지된다.
    /// 목록은 ↑/↓ (및 Home/End) 키로 선택을 이동할 수 있다.
    /// </summary>
    public class ActorDatabaseEditorWindow : EditorWindow
    {
        // ── 참조 ─────────────────────────────────────────────────────
        private ActorDatabase _database;

        // ── 선택 상태 ─────────────────────────────────────────────────
        private ActorDefinitionSO _selected;
        private SerializedObject  _selectedSO;
        private bool _hasUnsavedChanges;

        /// <summary>목록에 표시 중인 항목. 필터가 없을 때는 Database 인덱스와 1:1 대응하며 Missing(null)도 포함한다.</summary>
        private readonly List<ActorDefinitionSO> _visible = new();

        /// <summary>필터가 없어 드래그 순서 변경이 가능한 상태인지.</summary>
        private bool _canReorder = true;

        // ── UI 요소 ───────────────────────────────────────────────────
        private ObjectField    _databaseField;
        private ToolbarButton  _newActorButton;
        private ToolbarButton  _syncButton;
        private ToolbarButton  _enumButton;
        private ToolbarButton  _prefabSyncButton;
        private ToolbarButton  _cleanupButton;
        private ToolbarButton  _saveButton;

        private VisualElement  _bodyRoot;
        private HelpBox        _noDatabaseHelp;
        private TwoPaneSplitView _split;
        private VisualElement  _listPane;
        private ToolbarSearchField _searchField;
        private EnumFlagsField _typeFilterField;
        private Label          _countLabel;
        private ListView       _listView;

        private Label          _headerLabel;
        private ScrollView     _detailScroll;
        private Button         _detailOpenInspectorButton;
        private Button         _detailSaveButton;

        // ── 아이콘 캐시 ───────────────────────────────────────────────
        private Texture2D _iconSO;

        // ── 색상 ─────────────────────────────────────────────────────
        private static readonly Color ColorHeader   = new(0.15f, 0.15f, 0.20f);
        private static readonly Color ColorUnsaved  = new(0.85f, 0.60f, 0.10f);
        private static readonly Color ColorMissing  = new(0.90f, 0.35f, 0.35f);

        // ── 레이아웃 상수 ────────────────────────────────────────────
        private const float ItemHeight       = 40f;
        private const float DefaultListWidth = 280f;
        private const float MinListWidth     = 190f;
        private const float MaxListWidth     = 900f;
        private const float MinDetailWidth   = 320f;

        private const string PrefKeyListWidth  = "UPlayGround.ActorDatabaseEditor.ListWidth";
        private const string DefaultSavePath   = "Assets/10.Datas/Actor/DataBase";
        private const string EnumOutputPath    = "Assets/02.Scripts/Data/Actor/ActorIdType.cs";

        // ── 메뉴 ─────────────────────────────────────────────────────
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/캐릭터/액터/액터 데이터베이스 에디터", priority = 101)]
        public static void Open()
        {
            var window = GetWindow<ActorDatabaseEditorWindow>();
            window.titleContent = new GUIContent("Actor Database", EditorGUIUtility.IconContent("d_ScriptableObject Icon").image);
            window.minSize = new Vector2(720f, 440f);
            window.Show();
        }

        // ── 라이프사이클 ──────────────────────────────────────────────
        private void OnEnable()
        {
            _iconSO = EditorGUIUtility.IconContent("d_ScriptableObject Icon").image as Texture2D;
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            BuildToolbar();
            BuildBody();

            // Ctrl+S 저장 — 어떤 필드에 포커스가 있어도 동작하도록 캡처 단계에서 처리
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnRootKeyDown, TrickleDown.TrickleDown);

            TryAutoLoadDatabase();
            RefreshAll();
        }

        private void OnFocus()
        {
            // 외부(Project 창 등)에서 에셋이 바뀌었을 수 있으므로 목록 라벨을 갱신한다.
            if (_listView != null)
                RefreshList();
        }

        // ── 툴바 ─────────────────────────────────────────────────────
        private void BuildToolbar()
        {
            var toolbar = new Toolbar();

            _databaseField = new ObjectField
            {
                objectType = typeof(ActorDatabase),
                allowSceneObjects = false,
                tooltip = "편집할 ActorDatabase 에셋",
            };
            _databaseField.style.width = 220f;
            _databaseField.style.marginRight = 4f;
            _databaseField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue as ActorDatabase == _database) return;
                SetDatabase(evt.newValue as ActorDatabase);
                RefreshAll();
            });
            toolbar.Add(_databaseField);

            toolbar.Add(new ToolbarButton(CreateNewDatabase) { text = "새 Database 생성" });

            toolbar.Add(new ToolbarSpacer { flex = true });

            _newActorButton = new ToolbarButton(CreateNewDefinition) { text = "새 Actor 추가" };
            _syncButton = new ToolbarButton(SyncActorDefinitionsFromProject)
            {
                text = "SO 자동 동기화",
                tooltip = "프로젝트의 모든 ActorDefinitionSO 중 미등록 항목을 Database에 추가합니다.",
            };
            _enumButton = new ToolbarButton(GenerateActorIdEnum)
            {
                text = "Enum 생성",
                tooltip = EnumOutputPath + " 를 덮어씁니다.",
            };
            _prefabSyncButton = new ToolbarButton(SyncPrefabActorIds)
            {
                text = "프리팹 ID 동기화",
                tooltip = "각 actorId를 연결된 프리팹의 GameActor._actorId에 반영합니다.",
            };
            _cleanupButton = new ToolbarButton(CleanupMissingDefinitions) { text = "Missing 정리" };

            toolbar.Add(_newActorButton);
            toolbar.Add(_syncButton);
            toolbar.Add(_enumButton);
            toolbar.Add(_prefabSyncButton);
            toolbar.Add(_cleanupButton);

            _saveButton = new ToolbarButton(SaveAll) { text = "저장  Ctrl+S" };
            _saveButton.style.width = 100f;
            _saveButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            toolbar.Add(_saveButton);

            rootVisualElement.Add(toolbar);
        }

        // ── 본문 ─────────────────────────────────────────────────────
        private void BuildBody()
        {
            _bodyRoot = new VisualElement();
            _bodyRoot.style.flexGrow = 1f;
            _bodyRoot.style.flexDirection = FlexDirection.Column;
            rootVisualElement.Add(_bodyRoot);

            _noDatabaseHelp = new HelpBox(
                "ActorDatabase가 선택되지 않았습니다.\n툴바에서 기존 Database를 연결하거나 새로 생성하세요.",
                HelpBoxMessageType.Info);
            _noDatabaseHelp.style.marginLeft = 8f;
            _noDatabaseHelp.style.marginRight = 8f;
            _noDatabaseHelp.style.marginTop = 8f;
            _bodyRoot.Add(_noDatabaseHelp);

            float initialWidth = Mathf.Clamp(
                EditorPrefs.GetFloat(PrefKeyListWidth, DefaultListWidth),
                MinListWidth, MaxListWidth);

            _split = new TwoPaneSplitView(0, initialWidth, TwoPaneSplitViewOrientation.Horizontal);
            _split.style.flexGrow = 1f;
            _bodyRoot.Add(_split);

            _split.Add(BuildListPane());
            _split.Add(BuildDetailPane());
        }

        private VisualElement BuildListPane()
        {
            _listPane = new VisualElement();
            _listPane.style.flexDirection = FlexDirection.Column;
            _listPane.style.minWidth = MinListWidth;

            // 분할선 드래그로 바뀐 폭을 유지한다.
            _listPane.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                float width = evt.newRect.width;
                if (width > 1f)
                    EditorPrefs.SetFloat(PrefKeyListWidth, width);
            });

            // 검색 행
            var searchRow = new Toolbar();
            _searchField = new ToolbarSearchField { tooltip = "표시 이름 또는 actorId로 검색" };
            _searchField.style.flexGrow = 1f;
            _searchField.RegisterValueChangedCallback(_ => RefreshList());
            searchRow.Add(_searchField);
            _listPane.Add(searchRow);

            // 타입 필터 행
            var filterRow = new Toolbar();
            _typeFilterField = new EnumFlagsField(ActorType.None) { tooltip = "ActorType 필터 (Nothing = 전체 표시)" };
            _typeFilterField.style.flexGrow = 1f;
            _typeFilterField.RegisterValueChangedCallback(_ => RefreshList());
            filterRow.Add(_typeFilterField);
            filterRow.Add(new ToolbarButton(() =>
            {
                _searchField.value = string.Empty;
                _typeFilterField.value = ActorType.None;
                RefreshList();
            })
            { text = "✕", tooltip = "검색·타입 필터 초기화" });
            _listPane.Add(filterRow);

            _countLabel = new Label();
            _countLabel.style.fontSize = 10f;
            _countLabel.style.opacity = 0.65f;
            _countLabel.style.paddingLeft = 6f;
            _countLabel.style.paddingTop = 3f;
            _countLabel.style.paddingBottom = 3f;
            _listPane.Add(_countLabel);

            _listView = new ListView
            {
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                fixedItemHeight = ItemHeight,
                reorderMode = ListViewReorderMode.Animated,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                itemsSource = _visible,
                makeItem = MakeListItem,
                bindItem = BindListItem,
            };
            _listView.style.flexGrow = 1f;
            _listView.selectionChanged += objs => SelectDefinition(objs.FirstOrDefault() as ActorDefinitionSO);
            _listView.itemIndexChanged += OnItemIndexChanged;
            _listPane.Add(_listView);

            // ↑/↓ 로 목록 선택 이동. 검색창에 포커스가 있어도 동작하도록 캡처 단계에서 가로챈다.
            _listPane.RegisterCallback<KeyDownEvent>(OnListKeyDown, TrickleDown.TrickleDown);

            return _listPane;
        }

        private VisualElement BuildDetailPane()
        {
            var detailPane = new VisualElement();
            detailPane.style.flexDirection = FlexDirection.Column;
            detailPane.style.minWidth = MinDetailWidth;

            _headerLabel = new Label();
            _headerLabel.style.backgroundColor = ColorHeader;
            _headerLabel.style.color = Color.white;
            _headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _headerLabel.style.fontSize = 13f;
            _headerLabel.style.paddingLeft = 10f;
            _headerLabel.style.paddingTop = 6f;
            _headerLabel.style.paddingBottom = 6f;
            detailPane.Add(_headerLabel);

            _detailScroll = new ScrollView();
            _detailScroll.style.flexGrow = 1f;
            _detailScroll.style.paddingLeft = 6f;
            _detailScroll.style.paddingRight = 6f;
            _detailScroll.style.paddingTop = 4f;
            detailPane.Add(_detailScroll);

            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.paddingLeft = 6f;
            footer.style.paddingRight = 6f;
            footer.style.paddingTop = 4f;
            footer.style.paddingBottom = 6f;

            _detailOpenInspectorButton = new Button(() =>
            {
                if (_selected == null) return;
                Selection.activeObject = _selected;
                EditorGUIUtility.PingObject(_selected);
            })
            { text = "Inspector에서 열기" };
            _detailOpenInspectorButton.style.flexGrow = 1f;
            _detailOpenInspectorButton.style.height = 24f;
            footer.Add(_detailOpenInspectorButton);

            _detailSaveButton = new Button(SaveAll) { text = "저장  Ctrl+S" };
            _detailSaveButton.style.width = 130f;
            _detailSaveButton.style.height = 24f;
            footer.Add(_detailSaveButton);

            detailPane.Add(footer);
            return detailPane;
        }

        // ── 목록 아이템 ───────────────────────────────────────────────
        private VisualElement MakeListItem()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 4f;
            row.style.paddingRight = 4f;

            var texts = new VisualElement { name = "texts" };
            texts.style.flexDirection = FlexDirection.Column;
            texts.style.flexGrow = 1f;
            texts.style.overflow = Overflow.Hidden;

            var nameLabel = new Label { name = "display-name" };
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            texts.Add(nameLabel);

            var idLabel = new Label { name = "actor-id" };
            idLabel.style.fontSize = 10f;
            idLabel.style.opacity = 0.65f;
            texts.Add(idLabel);

            row.Add(texts);

            var typeLabel = new Label { name = "actor-type" };
            typeLabel.style.fontSize = 10f;
            typeLabel.style.opacity = 0.55f;
            typeLabel.style.marginRight = 4f;
            typeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(typeLabel);

            var duplicateButton = new Button { name = "duplicate", text = "복제", tooltip = "이 정의를 복제해 새 에셋으로 저장" };
            duplicateButton.style.width = 42f;
            duplicateButton.clicked += () =>
            {
                var def = ResolveRowDefinition(duplicateButton);
                if (def != null) DuplicateDefinition(def);
            };
            row.Add(duplicateButton);

            var removeButton = new Button { name = "remove", text = "삭제", tooltip = "Database 목록에서 제거 (에셋 파일은 삭제되지 않음)" };
            removeButton.style.width = 42f;
            removeButton.clicked += () =>
            {
                var def = ResolveRowDefinition(removeButton);
                if (def != null) RemoveDefinition(def);
            };
            row.Add(removeButton);

            return row;
        }

        private void BindListItem(VisualElement element, int index)
        {
            element.userData = index;

            var def = index >= 0 && index < _visible.Count ? _visible[index] : null;

            var nameLabel = element.Q<Label>("display-name");
            var idLabel   = element.Q<Label>("actor-id");
            var typeLabel = element.Q<Label>("actor-type");
            var duplicateButton = element.Q<Button>("duplicate");
            var removeButton    = element.Q<Button>("remove");

            if (def == null)
            {
                nameLabel.text = "(Missing)";
                nameLabel.style.color = ColorMissing;
                idLabel.text = "툴바의 'Missing 정리'로 제거할 수 있습니다.";
                typeLabel.text = string.Empty;
                duplicateButton.SetEnabled(false);
                removeButton.SetEnabled(false);
                return;
            }

            nameLabel.text = string.IsNullOrEmpty(def.displayName) ? def.actorId : def.displayName;
            nameLabel.style.color = StyleKeyword.Null;
            idLabel.text = def.actorId;
            typeLabel.text = def.actorType == ActorType.None ? string.Empty : def.actorType.ToString();
            duplicateButton.SetEnabled(true);
            removeButton.SetEnabled(true);
        }

        /// <summary>목록 행의 버튼에서 해당 행이 가리키는 정의를 역추적한다.</summary>
        private ActorDefinitionSO ResolveRowDefinition(VisualElement rowChild)
        {
            for (VisualElement e = rowChild; e != null; e = e.parent)
            {
                if (e.userData is int index)
                    return index >= 0 && index < _visible.Count ? _visible[index] : null;
            }
            return null;
        }

        // ── 키보드 ───────────────────────────────────────────────────
        private void OnRootKeyDown(KeyDownEvent evt)
        {
            if (!(evt.ctrlKey || evt.commandKey) || evt.keyCode != KeyCode.S)
                return;

            SaveAll();
            evt.StopPropagation();
        }

        private void OnListKeyDown(KeyDownEvent evt)
        {
            int count = _visible.Count;
            if (count == 0) return;

            // ListView가 포커스를 가지고 있으면 내장 내비게이션이 처리한다.
            // 내장 내비게이션은 KeyDownEvent가 아닌 NavigationMoveEvent로 동작해서
            // StopPropagation으로 막을 수 없다. 여기서 함께 처리하면 두 칸씩 이동한다.
            if (IsListFocused()) return;

            int current = _listView.selectedIndex;
            int next;

            switch (evt.keyCode)
            {
                case KeyCode.UpArrow:   next = current < 0 ? count - 1 : current - 1; break;
                case KeyCode.DownArrow: next = current < 0 ? 0         : current + 1; break;
                case KeyCode.Home:      next = 0; break;
                case KeyCode.End:       next = count - 1; break;
                default: return;
            }

            next = Mathf.Clamp(next, 0, count - 1);
            evt.StopPropagation();

            if (next == current) return;

            _listView.SetSelection(next);
            _listView.ScrollToItem(next);
        }

        /// <summary>ListView 또는 그 하위 요소가 현재 포커스를 가지고 있는지.</summary>
        private bool IsListFocused()
        {
            var focused = _listView?.panel?.focusController?.focusedElement as VisualElement;
            if (focused == null) return false;
            return focused == _listView || _listView.Contains(focused);
        }

        // ── 순서 변경 ─────────────────────────────────────────────────
        private void OnItemIndexChanged(int fromIndex, int toIndex)
        {
            if (_database == null || !_canReorder || fromIndex == toIndex)
                return;

            var dbSO = new SerializedObject(_database);
            var actorsProp = dbSO.FindProperty("_actors");
            if (actorsProp == null || !actorsProp.isArray)
            {
                Debug.LogError("[ActorDatabase] _actors 배열을 찾을 수 없어 순서 변경을 적용하지 못했습니다.");
                RefreshList();
                return;
            }

            // 필터가 없을 때 _visible은 Database 인덱스와 1:1이므로 인덱스를 그대로 사용한다.
            actorsProp.MoveArrayElement(fromIndex, toIndex);
            dbSO.ApplyModifiedProperties();

            _database.InvalidateLookup();
            EditorUtility.SetDirty(_database);
            MarkUnsaved();
            RefreshList();
        }

        // ── 갱신 ─────────────────────────────────────────────────────
        private void RefreshAll()
        {
            _databaseField.SetValueWithoutNotify(_database);
            UpdateToolbarState();
            RefreshList();
            RefreshDetail();
        }

        private void RefreshList()
        {
            _visible.Clear();

            string search = _searchField != null ? _searchField.value : string.Empty;
            var typeFilter = _typeFilterField != null ? (ActorType)_typeFilterField.value : ActorType.None;
            _canReorder = string.IsNullOrEmpty(search) && typeFilter == ActorType.None;

            int total = 0;
            if (_database != null)
            {
                var all = _database.All;
                total = all.Count;

                for (int i = 0; i < all.Count; i++)
                {
                    var def = all[i];

                    // 필터가 없을 때는 Missing까지 그대로 노출해 인덱스를 Database와 일치시킨다.
                    if (_canReorder)
                    {
                        _visible.Add(def);
                        continue;
                    }

                    if (def == null) continue;

                    string label = string.IsNullOrEmpty(def.displayName) ? def.actorId : def.displayName;
                    if (!string.IsNullOrEmpty(search) &&
                        label.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0 &&
                        (def.actorId == null || def.actorId.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0))
                        continue;

                    if (typeFilter != ActorType.None && (def.actorType & typeFilter) == 0)
                        continue;

                    _visible.Add(def);
                }
            }

            _listView.reorderable = _canReorder;
            _listView.itemsSource = _visible;
            _listView.Rebuild();

            int selectedIndex = _selected != null ? _visible.IndexOf(_selected) : -1;
            _listView.SetSelectionWithoutNotify(selectedIndex >= 0 ? new[] { selectedIndex } : System.Array.Empty<int>());

            _countLabel.text = _canReorder
                ? $"{total}개 · 드래그로 순서 변경, ↑/↓로 이동"
                : $"{_visible.Count} / {total}개 (필터 적용 중 — 순서 변경 불가)";

            UpdateToolbarState();
        }

        private void RefreshDetail()
        {
            _detailScroll.Clear();

            if (_selected == null || _selectedSO == null)
            {
                _headerLabel.text = "선택 없음";
                var hint = new Label("← 좌측에서 Actor를 선택하세요");
                hint.style.opacity = 0.6f;
                hint.style.marginTop = 12f;
                hint.style.unityTextAlign = TextAnchor.MiddleCenter;
                _detailScroll.Add(hint);
                _detailOpenInspectorButton.SetEnabled(false);
                return;
            }

            _detailOpenInspectorButton.SetEnabled(true);
            UpdateDetailHeader();

            // 섹션 구성·디자인은 ActorDefinitionDetailView가 단일 소스다.
            // Inspector, 데이터 저작 허브와 동일한 화면을 공유한다.
            // TrackSerializedObjectValue는 요소당 SerializedObject 하나만 추적하므로 매번 새 컨테이너에 붙인다.
            var container = new VisualElement();
            container.Add(ActorDefinitionDetailView.Build(_selectedSO, new ActorDefinitionDetailOptions
            {
                ShowOpenHubButton = true,
                ShowAssetHeader   = false,
                ShowHubLinks      = true,
            }));
            container.TrackSerializedObjectValue(_selectedSO, _ => OnSelectedDefinitionChanged());
            _detailScroll.Add(container);
        }

        private void UpdateDetailHeader()
        {
            if (_selected == null) return;
            string name = string.IsNullOrEmpty(_selected.displayName) ? _selected.actorId : _selected.displayName;
            _headerLabel.text = $"{name}  [{_selected.actorId}]";
        }

        private void OnSelectedDefinitionChanged()
        {
            if (_selected == null) return;

            _database?.InvalidateLookup();
            MarkUnsaved();
            UpdateDetailHeader();

            int index = _visible.IndexOf(_selected);
            if (index >= 0)
                _listView.RefreshItem(index);
        }

        private void UpdateToolbarState()
        {
            bool hasDatabase = _database != null;

            _noDatabaseHelp.style.display = hasDatabase ? DisplayStyle.None : DisplayStyle.Flex;
            _split.style.display = hasDatabase ? DisplayStyle.Flex : DisplayStyle.None;

            _newActorButton.SetEnabled(hasDatabase);
            _syncButton.SetEnabled(hasDatabase);
            _enumButton.SetEnabled(hasDatabase);
            _prefabSyncButton.SetEnabled(hasDatabase);

            int missingCount = CountMissingDefinitions();
            _cleanupButton.SetEnabled(hasDatabase);
            _cleanupButton.text = missingCount > 0 ? $"Missing 정리 ({missingCount})" : "Missing 정리";

            UpdateSaveButtons();
        }

        private void UpdateSaveButtons()
        {
            if (_saveButton == null) return;

            _saveButton.text = _hasUnsavedChanges ? "● 저장  Ctrl+S" : "저장  Ctrl+S";
            _saveButton.SetEnabled(_hasUnsavedChanges);
            _saveButton.style.backgroundColor = _hasUnsavedChanges ? ColorUnsaved : StyleKeyword.Null;

            if (_detailSaveButton == null) return;

            _detailSaveButton.text = _hasUnsavedChanges ? "● 저장  Ctrl+S" : "저장  Ctrl+S";
            _detailSaveButton.SetEnabled(_hasUnsavedChanges);
            _detailSaveButton.style.backgroundColor = _hasUnsavedChanges ? ColorUnsaved : StyleKeyword.Null;
        }

        // ── 저장 ─────────────────────────────────────────────────────

        /// <summary>미저장 에셋을 모두 디스크에 기록하고 dirty 상태를 해제한다.</summary>
        private void SaveAll()
        {
            AssetDatabase.SaveAssets();
            _hasUnsavedChanges = false;
            UpdateTitle();
            UpdateSaveButtons();
        }

        private void MarkUnsaved()
        {
            if (_hasUnsavedChanges) return;
            _hasUnsavedChanges = true;
            UpdateTitle();
            UpdateSaveButtons();
        }

        private void UpdateTitle()
        {
            titleContent = new GUIContent(
                _hasUnsavedChanges ? "Actor Database ●" : "Actor Database",
                _iconSO);
        }

        // ── 선택/데이터베이스 ─────────────────────────────────────────
        private void TryAutoLoadDatabase()
        {
            if (_database != null) return;

            var guids = AssetDatabase.FindAssets("t:ActorDatabase");
            if (guids.Length > 0)
                SetDatabase(AssetDatabase.LoadAssetAtPath<ActorDatabase>(AssetDatabase.GUIDToAssetPath(guids[0])));
        }

        private void SetDatabase(ActorDatabase db)
        {
            _database = db;
            _hasUnsavedChanges = false;
            ClearSelection();
            UpdateTitle();
        }

        private void SelectDefinition(ActorDefinitionSO def)
        {
            _selected   = def;
            _selectedSO = def != null ? new SerializedObject(def) : null;
            RefreshDetail();
        }

        private void ClearSelection()
        {
            _selected   = null;
            _selectedSO = null;
        }

        // ── CRUD ─────────────────────────────────────────────────────
        private void CreateNewDatabase()
        {
            EnsureSavePath(DefaultSavePath);
            string path = EditorUtility.SaveFilePanelInProject(
                "ActorDatabase 저장", "ActorDatabase", "asset",
                "저장할 위치를 선택하세요", DefaultSavePath);
            if (string.IsNullOrEmpty(path)) return;

            var db = CreateInstance<ActorDatabase>();
            AssetDatabase.CreateAsset(db, path);
            AssetDatabase.SaveAssets();
            SetDatabase(db);
            RefreshAll();
        }

        private void CreateNewDefinition()
        {
            if (_database == null) return;

            EnsureSavePath(DefaultSavePath);
            string path = EditorUtility.SaveFilePanelInProject(
                "ActorDefinition 저장", "ActorDef_New", "asset",
                "저장할 위치를 선택하세요", DefaultSavePath);
            if (string.IsNullOrEmpty(path)) return;

            var def = CreateInstance<ActorDefinitionSO>();
            def.actorId     = Path.GetFileNameWithoutExtension(path);
            def.displayName = def.actorId;

            AssetDatabase.CreateAsset(def, path);
            AssetDatabase.SaveAssets();

            _database.AddDefinition(def);
            SelectDefinition(def);
            MarkUnsaved();
            RefreshList();
        }

        private void DuplicateDefinition(ActorDefinitionSO source)
        {
            if (_database == null || source == null) return;

            EnsureSavePath(DefaultSavePath);
            string sourcePath = AssetDatabase.GetAssetPath(source);
            string path = EditorUtility.SaveFilePanelInProject(
                "ActorDefinition 복제", source.actorId + "_Copy", "asset",
                "저장할 위치를 선택하세요", DefaultSavePath);
            if (string.IsNullOrEmpty(path)) return;

            AssetDatabase.CopyAsset(sourcePath, path);

            var copy = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
            copy.actorId     = Path.GetFileNameWithoutExtension(path);
            copy.displayName = copy.actorId;
            EditorUtility.SetDirty(copy);
            AssetDatabase.SaveAssets();

            _database.AddDefinition(copy);
            SelectDefinition(copy);
            MarkUnsaved();
            RefreshList();
        }

        private void RemoveDefinition(ActorDefinitionSO def)
        {
            if (_database == null || def == null) return;

            if (!EditorUtility.DisplayDialog("삭제 확인",
                $"'{def.actorId}' 를 Database에서 제거하시겠습니까?\n(에셋 파일은 삭제되지 않습니다)", "제거", "취소"))
                return;

            _database.RemoveDefinition(def);
            if (_selected == def)
            {
                ClearSelection();
                RefreshDetail();
            }

            MarkUnsaved();
            RefreshList();
        }

        private void SyncActorDefinitionsFromProject()
        {
            if (_database == null) return;

            Undo.RecordObject(_database, "Sync ActorDefinitions From Project");

            var registeredDefinitions = new HashSet<ActorDefinitionSO>();
            var registeredIds = new HashSet<string>();
            foreach (var def in _database.All)
            {
                if (def == null) continue;

                registeredDefinitions.Add(def);
                if (!string.IsNullOrEmpty(def.actorId))
                    registeredIds.Add(def.actorId);
            }

            int added = 0;
            int skippedDuplicateId = 0;
            int filledEmptyId = 0;

            string[] guids = AssetDatabase.FindAssets("t:ActorDefinitionSO");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
                if (definition == null || registeredDefinitions.Contains(definition))
                    continue;

                if (string.IsNullOrEmpty(definition.actorId))
                {
                    Undo.RecordObject(definition, "Fill ActorDefinition ActorId");
                    definition.actorId = definition.name;
                    EditorUtility.SetDirty(definition);
                    filledEmptyId++;
                }

                if (!registeredIds.Add(definition.actorId))
                {
                    skippedDuplicateId++;
                    Debug.LogWarning($"[ActorDatabase] actorId 중복으로 자동 동기화 건너뜀: '{definition.actorId}' ({path})");
                    continue;
                }

                _database.AddDefinition(definition);
                registeredDefinitions.Add(definition);
                added++;
            }

            if (added > 0 || filledEmptyId > 0)
            {
                _database.InvalidateLookup();
                EditorUtility.SetDirty(_database);
                MarkUnsaved();
                RefreshList();
            }

            string message = $"ActorDefinitionSO 자동 동기화 완료\n추가: {added}개";
            if (filledEmptyId > 0)
                message += $"\n비어있는 actorId 자동 설정: {filledEmptyId}개";
            if (skippedDuplicateId > 0)
                message += $"\n중복 actorId로 건너뜀: {skippedDuplicateId}개";

            EditorUtility.DisplayDialog("SO 자동 동기화", message, "확인");
            Debug.Log($"[ActorDatabase] ActorDefinitionSO 자동 동기화 완료: 추가 {added}개, actorId 설정 {filledEmptyId}개, 중복 건너뜀 {skippedDuplicateId}개");
        }

        // ── Missing 정리 ─────────────────────────────────────────────
        private int CountMissingDefinitions()
        {
            if (_database == null) return 0;

            int count = 0;
            foreach (var def in _database.All)
            {
                if (def == null)
                    count++;
            }
            return count;
        }

        private void CleanupMissingDefinitions()
        {
            if (_database == null) return;

            int missingCount = CountMissingDefinitions();
            if (missingCount == 0)
            {
                EditorUtility.DisplayDialog("Missing 정리", "정리할 Missing 항목이 없습니다.", "확인");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "Missing 항목 정리",
                $"ActorDatabase에서 Missing 항목 {missingCount}개를 제거하시겠습니까?\n(ActorDefinitionSO 에셋 파일은 삭제하지 않습니다)",
                "정리", "취소"))
                return;

            Undo.RecordObject(_database, "Cleanup Missing Actor Definitions");

            var dbSO = new SerializedObject(_database);
            var actorsProp = dbSO.FindProperty("_actors");
            if (actorsProp == null || !actorsProp.isArray)
            {
                EditorUtility.DisplayDialog("Missing 정리 실패", "ActorDatabase의 _actors 배열을 찾을 수 없습니다.", "확인");
                return;
            }

            int removed = 0;
            for (int i = actorsProp.arraySize - 1; i >= 0; i--)
            {
                var element = actorsProp.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != null)
                    continue;

                int beforeSize = actorsProp.arraySize;
                actorsProp.DeleteArrayElementAtIndex(i);
                if (actorsProp.arraySize == beforeSize)
                    actorsProp.DeleteArrayElementAtIndex(i);

                removed++;
            }

            dbSO.ApplyModifiedProperties();
            _database.InvalidateLookup();
            EditorUtility.SetDirty(_database);

            MarkUnsaved();
            RefreshList();

            Debug.Log($"[ActorDatabase] Missing 항목 정리 완료: {removed}개 제거");
            EditorUtility.DisplayDialog("Missing 정리 완료", $"{removed}개 Missing 항목을 제거했습니다.", "확인");
        }

        private static void EnsureSavePath(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            // "Assets/10.Datas/Actor" → 상위 폴더부터 순서대로 생성
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        // ── 프리팹 ID 동기화 ──────────────────────────────────────────

        /// <summary>
        /// Database의 각 actorId를 연결된 프리팹의 GameActor._actorId에 반영한다.
        /// </summary>
        private void SyncPrefabActorIds()
        {
            if (_database == null) return;

            var all = _database.All;

            int synced  = 0;
            int skipped = 0;

            try
            {
                for (int i = 0; i < all.Count; i++)
                {
                    var def = all[i];

                    EditorUtility.DisplayProgressBar(
                        "프리팹 ID 동기화",
                        $"처리 중: {def?.actorId ?? "(null)"} ({i + 1}/{all.Count})",
                        (float)(i + 1) / all.Count);

                    if (def == null || def.prefab == null || string.IsNullOrEmpty(def.actorId))
                    {
                        skipped++;
                        continue;
                    }

                    string prefabPath = AssetDatabase.GetAssetPath(def.prefab);
                    if (string.IsNullOrEmpty(prefabPath))
                    {
                        Debug.LogWarning($"[ActorDatabase] '{def.actorId}': 프리팹 경로를 찾을 수 없습니다.");
                        skipped++;
                        continue;
                    }

                    // 프리팹 내용 로드 (임시 씬에 언패킹)
                    var prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
                    try
                    {
                        var gameActor = prefabContents.GetComponent<GameActor>();
                        if (gameActor == null)
                        {
                            Debug.LogWarning($"[ActorDatabase] '{def.actorId}': 프리팹 루트에 GameActor 컴포넌트가 없습니다.");
                            skipped++;
                            continue;
                        }

                        var so   = new SerializedObject(gameActor);
                        var prop = so.FindProperty("_actorId");
                        if (prop == null)
                        {
                            Debug.LogWarning($"[ActorDatabase] '{def.actorId}': GameActor에서 _actorId 프로퍼티를 찾을 수 없습니다.");
                            skipped++;
                            continue;
                        }

                        if (prop.stringValue == def.actorId)
                        {
                            skipped++; // 이미 일치 — 변경 불필요
                            continue;
                        }

                        prop.stringValue = def.actorId;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                        synced++;
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(prefabContents);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            Debug.Log($"[ActorDatabase] 프리팹 ID 동기화 완료: {synced}개 갱신, {skipped}개 건너뜀");

            if (synced > 0)
                EditorUtility.DisplayDialog("프리팹 ID 동기화 완료",
                    $"{synced}개 프리팹의 _actorId를 갱신했습니다.\n" +
                    $"({skipped}개는 이미 일치하거나 프리팹 없음)",
                    "확인");
            else
                EditorUtility.DisplayDialog("프리팹 ID 동기화",
                    $"변경이 필요한 항목이 없습니다. ({skipped}개 확인)",
                    "확인");
        }

        // ── Enum 코드 생성 ────────────────────────────────────────────

        /// <summary>
        /// ActorDatabase의 모든 actorId를 읽어 ActorIdType.cs를 덮어씁니다.
        /// 공통 IdEnumGeneratorUtility를 사용합니다.
        /// </summary>
        private void GenerateActorIdEnum()
        {
            if (_database == null) return;

            var raw = new List<(string, string)>();
            bool hasDuplicate = false;

            foreach (var def in _database.All)
            {
                if (def == null || string.IsNullOrEmpty(def.actorId)) continue;
                string id = IdEnumGeneratorUtility.SanitizeToIdentifier(def.actorId);
                if (raw.Exists(e => e.Item1 == id))
                {
                    Debug.LogWarning($"[ActorIdEnum] 식별자 충돌: '{def.actorId}' → '{id}'. 중복 항목은 제외됩니다.");
                    hasDuplicate = true;
                }
                else
                {
                    raw.Add((def.actorId, def.actorId));
                }
            }

            if (hasDuplicate &&
                !EditorUtility.DisplayDialog("식별자 충돌 경고",
                    "하나 이상의 actorId가 동일한 enum 이름으로 변환됩니다.\n" +
                    "충돌 항목은 제외됩니다. 계속하시겠습니까?",
                    "계속", "취소"))
                return;

            var entries = IdEnumGeneratorUtility.DeduplicateEntries(raw);

            bool ok = IdEnumGeneratorUtility.GenerateStringKeyEnum(
                "ActorIdType", "ToActorId", "Actor",
                EnumOutputPath, "UPlayGround.Data.Actor", entries);

            if (ok)
            {
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Enum 생성 완료",
                    $"{entries.Count}개 항목으로 ActorIdType.cs가 생성되었습니다.\n{EnumOutputPath}",
                    "확인");
            }
        }
    }
}
