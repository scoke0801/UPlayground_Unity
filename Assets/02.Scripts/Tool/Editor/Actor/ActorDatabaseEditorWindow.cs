using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround;
using UPlayGround.Tool.Editor;

namespace UPlayGround.Actor.Editor
{
    /// <summary>
    /// ActorDatabase에 등록된 ActorDefinitionSO를 관리하는 에디터 창.
    /// 메뉴: UPlayGround/Actor/Actor Database Editor
    /// </summary>
    public class ActorDatabaseEditorWindow : EditorWindow
    {
        // ── 참조 ─────────────────────────────────────────────────────
        private ActorDatabase _database;

        // ── UI 상태 ───────────────────────────────────────────────────
        private ActorDefinitionSO _selected;
        private SerializedObject  _selectedSO;
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private string   _searchFilter    = "";
        private ActorType _filterActorType = ActorType.None;
        private bool     _hasUnsavedChanges;

        // ── 드래그 순서 변경 ──────────────────────────────────────────
        private int _dragIndex       = -1;
        private int _dropTargetIndex = -1;
        private readonly List<float> _itemTopYs = new();
        private readonly List<float> _itemMidYs = new();

        // ── 스타일 캐시 ───────────────────────────────────────────────
        private GUIStyle _styleHeader;
        private GUIStyle _styleListItem;
        private GUIStyle _styleListItemSelected;
        private bool     _stylesInitialized;

        // ── 아이콘 캐시 ───────────────────────────────────────────────
        private Texture2D _iconSO;

        // ── 색상 ─────────────────────────────────────────────────────
        private static readonly Color ColorHeader    = new(0.15f, 0.15f, 0.20f);
        private static readonly Color ColorSelected  = new(0.22f, 0.44f, 0.72f);
        private static readonly Color ColorSeparator = new(0.25f, 0.25f, 0.28f);
        private static readonly Color ColorUnsaved   = new(0.85f, 0.60f, 0.10f);
        private static readonly Color ColorDragLine  = new(0.35f, 0.65f, 1.00f);

        // ── 레이아웃 상수 ────────────────────────────────────────────
        private const float ListWidth    = 240f;
        private const float ItemHeight   = 36f;
        private const float DividerWidth = 2f;
        private const float ToolbarHeight = 21f;
        private const string DefaultSavePath   = "Assets/10.Datas/Actor/DataBase";
        private const string EnumOutputPath    = "Assets/02.Scripts/Data/Actor/ActorIdType.cs";

        private readonly struct VisibleActorEntry
        {
            public readonly int Index;
            public readonly ActorDefinitionSO Definition;
            public readonly string Label;

            public VisibleActorEntry(int index, ActorDefinitionSO definition, string label)
            {
                Index      = index;
                Definition = definition;
                Label      = label;
            }
        }

        // ── 메뉴 ─────────────────────────────────────────────────────
        [MenuItem("UPlayGround/캐릭터/액터/액터 데이터베이스 에디터", priority =  101)]
        public static void Open()
        {
            var window = GetWindow<ActorDatabaseEditorWindow>();
            window.titleContent = new GUIContent("Actor Database", EditorGUIUtility.IconContent("d_ScriptableObject Icon").image);
            window.minSize = new Vector2(640f, 420f);
            window.Show();
        }

        // ── 라이프사이클 ──────────────────────────────────────────────
        private void OnEnable()
        {
            _iconSO = EditorGUIUtility.IconContent("d_ScriptableObject Icon").image as Texture2D;
            TryAutoLoadDatabase();
        }

        private void OnGUI()
        {
            InitStyles();
            HandleKeyboardShortcuts();

            // 스크롤 영역 밖에서 마우스를 놓은 경우 드래그 확정
            if (_dragIndex >= 0 && Event.current.type == EventType.MouseUp)
            {
                ApplyReorder();
                Repaint();
            }

            var toolbarRect = new Rect(0f, 0f, position.width, ToolbarHeight);
            GUILayout.BeginArea(toolbarRect);
            DrawToolbar();
            GUILayout.EndArea();

            var contentRect = new Rect(
                0f,
                toolbarRect.yMax,
                position.width,
                Mathf.Max(0f, position.height - toolbarRect.yMax));

            if (_database == null)
            {
                GUILayout.BeginArea(contentRect);
                DrawNoDatabaseMessage();
                GUILayout.EndArea();
                return;
            }

            var listRect = new Rect(contentRect.x, contentRect.y, ListWidth, contentRect.height);
            var dividerRect = new Rect(listRect.xMax, contentRect.y, DividerWidth, contentRect.height);
            var detailRect = new Rect(
                dividerRect.xMax,
                contentRect.y,
                Mathf.Max(0f, contentRect.width - ListWidth - DividerWidth),
                contentRect.height);

            DrawListPanel(listRect);
            DrawDivider(dividerRect);

            GUILayout.BeginArea(detailRect);
            DrawDetailPanel();
            GUILayout.EndArea();
        }

        private void HandleKeyboardShortcuts()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && (e.control || e.command) && e.keyCode == KeyCode.S)
            {
                SaveAll();
                e.Use();
            }
        }

        // ── 툴바 ─────────────────────────────────────────────────────
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();
            var newDb = (ActorDatabase)EditorGUILayout.ObjectField(
                _database, typeof(ActorDatabase), false,
                GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck())
                SetDatabase(newDb);

            if (GUILayout.Button("새 Database 생성", EditorStyles.toolbarButton, GUILayout.Width(110)))
                CreateNewDatabase();

            GUILayout.FlexibleSpace();

            if (_database != null && GUILayout.Button("새 Actor 추가", EditorStyles.toolbarButton, GUILayout.Width(100)))
                CreateNewDefinition();

            if (_database != null && GUILayout.Button("SO 자동 동기화", EditorStyles.toolbarButton, GUILayout.Width(100)))
                SyncActorDefinitionsFromProject();

            if (_database != null && GUILayout.Button("Enum 생성", EditorStyles.toolbarButton, GUILayout.Width(76)))
                GenerateActorIdEnum();

            if (_database != null && GUILayout.Button("프리팹 ID 동기화", EditorStyles.toolbarButton, GUILayout.Width(100)))
                SyncPrefabActorIds();

            if (_database != null && GUILayout.Button(GetMissingCleanupButtonLabel(), EditorStyles.toolbarButton, GUILayout.Width(96)))
                CleanupMissingDefinitions();

            // 저장 버튼 — 미저장 변경이 있을 때 주황색 강조
            if (_hasUnsavedChanges)
            {
                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = ColorUnsaved;
                if (GUILayout.Button("● 저장  Ctrl+S", EditorStyles.toolbarButton, GUILayout.Width(108)))
                    SaveAll();
                GUI.backgroundColor = prevBg;
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                    GUILayout.Button("저장  Ctrl+S", EditorStyles.toolbarButton, GUILayout.Width(108));
            }

            EditorGUILayout.EndHorizontal();
        }

        // ── 목록 패널 ─────────────────────────────────────────────────
        private void DrawListPanel(Rect panelRect)
        {
            var searchRect = new Rect(panelRect.x, panelRect.y, panelRect.width, ToolbarHeight);
            var typeRect = new Rect(panelRect.x, searchRect.yMax, panelRect.width, ToolbarHeight);
            var scrollRect = new Rect(
                panelRect.x,
                typeRect.yMax,
                panelRect.width,
                Mathf.Max(0f, panelRect.yMax - typeRect.yMax));

            DrawListSearchRow(searchRect);
            DrawListTypeFilterRow(typeRect);

            // 필터 없을 때만 드래그 순서 변경 허용
            bool canReorder = string.IsNullOrEmpty(_searchFilter) && _filterActorType == ActorType.None;

            _itemTopYs.Clear();
            _itemMidYs.Clear();

            var all = _database.All;
            var visibleEntries = new List<VisibleActorEntry>();
            for (int i = 0; i < all.Count; i++)
            {
                var def = all[i];
                if (def == null) continue;

                string label = string.IsNullOrEmpty(def.displayName) ? def.actorId : def.displayName;

                // 텍스트 필터
                if (!string.IsNullOrEmpty(_searchFilter) &&
                    label.IndexOf(_searchFilter, System.StringComparison.OrdinalIgnoreCase) < 0 &&
                    def.actorId.IndexOf(_searchFilter, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                // ActorType 필터 (None = 전체 표시, 복합 Flags 조합 지원)
                if (_filterActorType != ActorType.None && (def.actorType & _filterActorType) == 0)
                    continue;

                visibleEntries.Add(new VisibleActorEntry(i, def, label));
            }

            float scrollbarWidth = GUI.skin.verticalScrollbar.fixedWidth;
            float contentWidth = Mathf.Max(1f, scrollRect.width - scrollbarWidth - 4f);
            float contentHeight = Mathf.Max(scrollRect.height + 1f, visibleEntries.Count * ItemHeight);
            var viewRect = new Rect(0f, 0f, contentWidth, contentHeight);

            HandleListScrollWheel(scrollRect, contentHeight);
            _listScroll = GUI.BeginScrollView(scrollRect, _listScroll, viewRect, false, true);

            for (int entryIndex = 0; entryIndex < visibleEntries.Count; entryIndex++)
            {
                var entry = visibleEntries[entryIndex];
                int i = entry.Index;
                var def = entry.Definition;
                string label = entry.Label;
                bool isSelected = _selected == def;

                Rect itemRect = new Rect(0f, entryIndex * ItemHeight, contentWidth, ItemHeight);

                if (canReorder)
                {
                    _itemTopYs.Add(itemRect.y);
                    _itemMidYs.Add(itemRect.center.y);
                }

                // 배경
                if (_dragIndex == i)
                    EditorGUI.DrawRect(itemRect, new Color(ColorSelected.r, ColorSelected.g, ColorSelected.b, 0.35f));
                else if (isSelected)
                    EditorGUI.DrawRect(itemRect, ColorSelected);

                // 드래그 핸들 (필터 없을 때만)
                if (canReorder)
                {
                    var handleRect = new Rect(itemRect.x + 2, itemRect.y, 14, itemRect.height);
                    GUI.Label(handleRect, "≡", EditorStyles.centeredGreyMiniLabel);

                    if (Event.current.type == EventType.MouseDown &&
                        handleRect.Contains(Event.current.mousePosition))
                    {
                        _dragIndex       = i;
                        _dropTargetIndex = i;
                        Event.current.Use();
                    }
                }

                float lx = canReorder ? itemRect.x + 18 : itemRect.x + 8;
                float lw = canReorder ? itemRect.width - 118 : itemRect.width - 108;

                GUI.Label(new Rect(lx, itemRect.y + 4, lw, 16), label, EditorStyles.boldLabel);
                GUI.Label(new Rect(lx, itemRect.y + 20, lw, 14), def.actorId, EditorStyles.miniLabel);

                // 복제 버튼
                var dupRect = new Rect(itemRect.xMax - 104, itemRect.y + 8, 48, 20);
                if (GUI.Button(dupRect, "복제", EditorStyles.miniButton))
                {
                    DuplicateDefinition(def);
                    GUIUtility.ExitGUI();
                    return;
                }

                // 삭제 버튼
                var delRect = new Rect(itemRect.xMax - 52, itemRect.y + 8, 48, 20);
                if (GUI.Button(delRect, "삭제", EditorStyles.miniButton))
                {
                    if (EditorUtility.DisplayDialog("삭제 확인",
                        $"'{def.actorId}' 를 Database에서 제거하시겠습니까?\n(에셋 파일은 삭제되지 않습니다)", "제거", "취소"))
                    {
                        _database.RemoveDefinition(def);
                        if (_selected == def) ClearSelection();
                        _dragIndex = -1;
                        GUIUtility.ExitGUI();
                        return;
                    }
                }

                // 클릭으로 선택 (드래그 핸들 영역 제외)
                if (Event.current.type == EventType.MouseDown &&
                    itemRect.Contains(Event.current.mousePosition))
                {
                    SelectDefinition(def);
                    Event.current.Use();
                }
            }

            if (visibleEntries.Count == 0)
                GUI.Label(new Rect(0f, 4f, contentWidth, 20f), "항목 없음", EditorStyles.centeredGreyMiniLabel);

            // ── 드래그 드롭 위치 표시 ──────────────────────────────────
            if (_dragIndex >= 0 && canReorder && _itemMidYs.Count > 0)
            {
                // MouseDrag에서만 삽입 위치 계산
                // Repaint/Layout은 mousePosition이 스크롤 콘텐츠 좌표로 변환되지 않아 항상 마지막 위치로 계산됨
                if (Event.current.type == EventType.MouseDrag)
                {
                    float mouseY = Event.current.mousePosition.y;
                    _dropTargetIndex = _itemMidYs.Count; // 기본: 맨 뒤
                    for (int j = 0; j < _itemMidYs.Count; j++)
                    {
                        if (mouseY < _itemMidYs[j])
                        {
                            _dropTargetIndex = j;
                            break;
                        }
                    }
                    Repaint();
                }

                // 삽입 위치 구분선은 Repaint에서만 그림
                if (Event.current.type == EventType.Repaint &&
                    _dropTargetIndex >= 0 && _dropTargetIndex <= _itemTopYs.Count)
                {
                    float lineY = _dropTargetIndex < _itemTopYs.Count
                        ? _itemTopYs[_dropTargetIndex]
                        : _itemTopYs[_itemTopYs.Count - 1] + ItemHeight;
                    EditorGUI.DrawRect(new Rect(2, lineY - 1, ListWidth - 8, 2), ColorDragLine);
                }
            }

            GUI.EndScrollView();
        }

        private void DrawListSearchRow(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.toolbar);

            var labelRect = new Rect(rect.x, rect.y, 36f, rect.height);
            var clearRect = new Rect(rect.xMax - 20f, rect.y, 20f, rect.height);
            var fieldRect = new Rect(labelRect.xMax, rect.y + 2f, Mathf.Max(1f, rect.width - 56f), rect.height - 4f);

            GUI.Label(labelRect, "검색", EditorStyles.toolbarButton);
            _searchFilter = EditorGUI.TextField(fieldRect, _searchFilter, EditorStyles.toolbarSearchField);
            if (GUI.Button(clearRect, "✕", EditorStyles.toolbarButton))
                _searchFilter = "";
        }

        private void DrawListTypeFilterRow(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.toolbar);

            var labelRect = new Rect(rect.x, rect.y, 36f, rect.height);
            var clearRect = new Rect(rect.xMax - 20f, rect.y, 20f, rect.height);
            var fieldRect = new Rect(labelRect.xMax, rect.y + 1f, Mathf.Max(1f, rect.width - 56f), rect.height - 2f);

            GUI.Label(labelRect, "타입", EditorStyles.toolbarButton);
            _filterActorType = (ActorType)EditorGUI.EnumFlagsField(fieldRect, _filterActorType, EditorStyles.toolbarPopup);
            if (GUI.Button(clearRect, "✕", EditorStyles.toolbarButton))
                _filterActorType = ActorType.None;
        }

        private void HandleListScrollWheel(Rect scrollRect, float contentHeight)
        {
            var e = Event.current;
            if (e.type != EventType.ScrollWheel || !scrollRect.Contains(e.mousePosition))
                return;

            float maxScrollY = Mathf.Max(0f, contentHeight - scrollRect.height);
            _listScroll.y = Mathf.Clamp(_listScroll.y + e.delta.y * ItemHeight * 0.5f, 0f, maxScrollY);
            e.Use();
            Repaint();
        }

        // ── 구분선 ────────────────────────────────────────────────────
        private void DrawDivider(Rect rect)
        {
            EditorGUI.DrawRect(rect, ColorSeparator);
        }

        // ── 상세 패널 ─────────────────────────────────────────────────
        private void DrawDetailPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            if (_selected == null || _selectedSO == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("← 좌측에서 Actor를 선택하세요", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            _selectedSO.Update();

            // 헤더
            DrawColorBox(ColorHeader, 28);
            Rect headerRect = GUILayoutUtility.GetLastRect();
            GUI.Label(new Rect(headerRect.x + 10, headerRect.y + 5, headerRect.width, 18),
                $"  {_selected.displayName}  [{_selected.actorId}]", _styleHeader);

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUILayout.ExpandHeight(true));
            EditorGUILayout.Space(4);

            // SerializedObject 기반 인스펙터
            var iterator = _selectedSO.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyPath == "m_Script") continue;
                EditorGUILayout.PropertyField(iterator, true);
            }

            EditorGUILayout.EndScrollView();

            // 변경 감지 후 적용 (디스크 저장은 SaveAll에서)
            if (_selectedSO.hasModifiedProperties)
            {
                _selectedSO.ApplyModifiedProperties();
                _database.InvalidateLookup();
                MarkUnsaved();
            }

            EditorGUILayout.Space(4);

            // 하단 버튼 행
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Inspector에서 열기", GUILayout.Height(24)))
                Selection.activeObject = _selected;

            var prevBg = GUI.backgroundColor;
            if (_hasUnsavedChanges)
            {
                GUI.backgroundColor = ColorUnsaved;
                if (GUILayout.Button("● 저장  Ctrl+S", GUILayout.Height(24), GUILayout.Width(130)))
                    SaveAll();
                GUI.backgroundColor = prevBg;
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                    GUILayout.Button("저장  Ctrl+S", GUILayout.Height(24), GUILayout.Width(130));
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ── 저장 ─────────────────────────────────────────────────────

        /// <summary>
        /// 미저장 에셋을 모두 디스크에 기록하고 dirty 상태를 해제한다.
        /// </summary>
        private void SaveAll()
        {
            AssetDatabase.SaveAssets();
            _hasUnsavedChanges = false;
            UpdateTitle();
        }

        private void MarkUnsaved()
        {
            if (_hasUnsavedChanges) return;
            _hasUnsavedChanges = true;
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            titleContent = new GUIContent(
                _hasUnsavedChanges ? "Actor Database ●" : "Actor Database",
                _iconSO);
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────
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
        }

        private void ClearSelection()
        {
            _selected   = null;
            _selectedSO = null;
        }

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
        }

        private void CreateNewDefinition()
        {
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
                Repaint();
            }

            string message = $"ActorDefinitionSO 자동 동기화 완료\n추가: {added}개";
            if (filledEmptyId > 0)
                message += $"\n비어있는 actorId 자동 설정: {filledEmptyId}개";
            if (skippedDuplicateId > 0)
                message += $"\n중복 actorId로 건너뜀: {skippedDuplicateId}개";

            EditorUtility.DisplayDialog("SO 자동 동기화", message, "확인");
            Debug.Log($"[ActorDatabase] ActorDefinitionSO 자동 동기화 완료: 추가 {added}개, actorId 설정 {filledEmptyId}개, 중복 건너뜀 {skippedDuplicateId}개");
        }

        private void ApplyReorder()
        {
            if (_dragIndex >= 0 && _dropTargetIndex >= 0)
            {
                if (_database.MoveDefinition(_dragIndex, _dropTargetIndex))
                    MarkUnsaved();
            }
            _dragIndex       = -1;
            _dropTargetIndex = -1;
        }

        private string GetMissingCleanupButtonLabel()
        {
            int missingCount = CountMissingDefinitions();
            return missingCount > 0 ? $"Missing 정리 ({missingCount})" : "Missing 정리";
        }

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

            if (_selected == null)
                ClearSelection();

            MarkUnsaved();
            Repaint();

            Debug.Log($"[ActorDatabase] Missing 항목 정리 완료: {removed}개 제거");
            EditorUtility.DisplayDialog("Missing 정리 완료", $"{removed}개 Missing 항목을 제거했습니다.", "확인");
        }

        private void DuplicateDefinition(ActorDefinitionSO source)
        {
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

        private void DrawColorBox(Color color, float height)
        {
            var rect = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, color);
        }

        private void DrawNoDatabaseMessage()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical();
            GUILayout.Label("ActorDatabase가 선택되지 않았습니다.", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label("툴바에서 기존 Database를 연결하거나 새로 생성하세요.", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
        }

        // ── 프리팹 ID 동기화 ──────────────────────────────────────────

        /// <summary>
        /// Database의 각 actorId를 연결된 프리팹의 GameActor._actorId에 반영한다.
        /// </summary>
        private void SyncPrefabActorIds()
        {
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

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _styleHeader = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 13,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = Color.white },
            };

            _styleListItem = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding   = new RectOffset(8, 4, 4, 4),
            };

            _styleListItemSelected = new GUIStyle(_styleListItem)
            {
                normal = { textColor = Color.white },
            };
        }
    }
}
