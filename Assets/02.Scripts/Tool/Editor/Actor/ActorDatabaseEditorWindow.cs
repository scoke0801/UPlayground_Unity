using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround;

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
        private const string DefaultSavePath   = "Assets/10.Datas/Actor/DataBase";
        private const string EnumOutputPath    = "Assets/02.Scripts/Data/Actor/ActorIdType.cs";

        // ── 메뉴 ─────────────────────────────────────────────────────
        [MenuItem("UPlayGround/Actor/Actor Database Editor")]
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

            DrawToolbar();

            if (_database == null)
            {
                DrawNoDatabaseMessage();
                return;
            }

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            DrawListPanel();
            DrawDivider();
            DrawDetailPanel();
            EditorGUILayout.EndHorizontal();
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

            if (_database != null && GUILayout.Button("Enum 생성", EditorStyles.toolbarButton, GUILayout.Width(76)))
                GenerateActorIdEnum();

            if (_database != null && GUILayout.Button("프리팹 ID 동기화", EditorStyles.toolbarButton, GUILayout.Width(100)))
                SyncPrefabActorIds();

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
        private void DrawListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(ListWidth), GUILayout.ExpandHeight(true));

            // 텍스트 검색 행
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("검색", EditorStyles.toolbarButton, GUILayout.Width(36));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
                _searchFilter = "";
            EditorGUILayout.EndHorizontal();

            // ActorType 필터 행
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("타입", EditorStyles.toolbarButton, GUILayout.Width(36));
            _filterActorType = (ActorType)EditorGUILayout.EnumFlagsField(
                _filterActorType, EditorStyles.toolbarPopup);
            if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
                _filterActorType = ActorType.None;
            EditorGUILayout.EndHorizontal();

            // 필터 없을 때만 드래그 순서 변경 허용
            bool canReorder = string.IsNullOrEmpty(_searchFilter) && _filterActorType == ActorType.None;

            _itemTopYs.Clear();
            _itemMidYs.Clear();

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

            var all = _database.All;
            bool anyShown = false;
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

                anyShown = true;
                bool isSelected = _selected == def;

                Rect itemRect = GUILayoutUtility.GetRect(ListWidth - 4, ItemHeight);

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

            if (!anyShown)
                GUILayout.Label("항목 없음", EditorStyles.centeredGreyMiniLabel, GUILayout.ExpandWidth(true));

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

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ── 구분선 ────────────────────────────────────────────────────
        private void DrawDivider()
        {
            var rect = GUILayoutUtility.GetRect(2, float.MaxValue, GUILayout.Width(2), GUILayout.ExpandHeight(true));
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
        /// </summary>
        private void GenerateActorIdEnum()
        {
            var all = _database.All;

            // ── 중복 식별자 검출 ──────────────────────────────────────
            var identifierToId  = new Dictionary<string, string>();
            var entries         = new List<(string identifier, string originalId)>();
            bool hasDuplicate   = false;

            foreach (var def in all)
            {
                if (def == null || string.IsNullOrEmpty(def.actorId)) continue;

                string identifier = SanitizeToIdentifier(def.actorId);
                if (identifierToId.TryGetValue(identifier, out var existing))
                {
                    Debug.LogWarning(
                        $"[ActorIdEnum] 식별자 충돌: '{def.actorId}'와 '{existing}' 모두 '{identifier}'로 변환됩니다. " +
                        $"actorId를 수정하거나 Database Editor에서 확인하세요.");
                    hasDuplicate = true;
                    continue;
                }

                identifierToId[identifier] = def.actorId;
                entries.Add((identifier, def.actorId));
            }

            if (hasDuplicate &&
                !EditorUtility.DisplayDialog("식별자 충돌 경고",
                    "하나 이상의 actorId가 동일한 enum 이름으로 변환됩니다.\n" +
                    "충돌 항목은 제외됩니다. 계속하시겠습니까?",
                    "계속", "취소"))
                return;

            // ── 코드 생성 ─────────────────────────────────────────────
            var sb = new StringBuilder();
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            sb.AppendLine("// 자동 생성 파일입니다. 직접 수정하지 마세요.");
            sb.AppendLine("// UPlayGround/Actor/Actor Database Editor → [Enum 생성] 버튼으로 재생성하세요.");
            sb.AppendLine($"// Generated: {timestamp}");
            sb.AppendLine("namespace UPlayGround.Data.Actor");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// ActorDatabase에 등록된 모든 Actor의 타입 열거형.");
            sb.AppendLine("    /// ActorSpawnManager.SpawnActor(ActorIdType, ...) 호출에 사용한다.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public enum ActorIdType");
            sb.AppendLine("    {");
            sb.AppendLine("        None = 0,");

            for (int i = 0; i < entries.Count; i++)
                sb.AppendLine($"        {entries[i].identifier} = {i + 1},");

            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    public static class ActorIdTypeExtensions");
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary>enum 값을 ActorDatabase의 actorId 문자열로 변환한다.</summary>");
            sb.AppendLine("        public static string ToActorId(this ActorIdType type) => type switch");
            sb.AppendLine("        {");

            foreach (var (identifier, originalId) in entries)
                sb.AppendLine($"            ActorIdType.{identifier} => \"{EscapeString(originalId)}\",");

            sb.AppendLine("            _ => string.Empty,");
            sb.AppendLine("        };");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(
                Path.GetFullPath(EnumOutputPath),
                sb.ToString(),
                new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            AssetDatabase.ImportAsset(EnumOutputPath);
            AssetDatabase.Refresh();

            Debug.Log($"[ActorIdEnum] {EnumOutputPath} 생성 완료 ({entries.Count}개 항목)");
            EditorUtility.DisplayDialog("Enum 생성 완료",
                $"{entries.Count}개 항목으로 ActorIdType.cs가 생성되었습니다.\n{EnumOutputPath}",
                "확인");
        }

        /// <summary>actorId 문자열을 유효한 C# 식별자로 변환한다.</summary>
        private static string SanitizeToIdentifier(string id)
        {
            if (string.IsNullOrEmpty(id)) return "_Empty";

            var sb = new StringBuilder(id.Length);
            foreach (char c in id)
                sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');

            // 숫자로 시작하면 앞에 _ 추가
            if (char.IsDigit(sb[0]))
                sb.Insert(0, '_');

            return sb.ToString();
        }

        private static string EscapeString(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

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
