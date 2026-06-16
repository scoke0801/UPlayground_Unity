#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Crafting;
using UPlayGround.Data.Path;

/// <summary>
/// 레시피 비주얼 에디터 윈도우.
/// 메뉴: UPlayGround / Crafting / Recipe Editor
///
/// 기능:
///   - 좌우 2패널 레이아웃 (목록 / 상세 편집)
///   - ItemDatabase 연동: 아이템 ID 옆에 이름 실시간 표시
///   - 아이템 피커 팝업: 이름으로 검색해서 선택
///   - 카테고리 탭 + 검색 필터
///   - 인라인 재료·언락 조건 편집
///   - 실시간 유효성 검사 (중복 ID, 없는 아이템 ID 경고)
///   - 레시피 추가 / 복제 / 삭제
///   - CSV 내보내기 (기존 워크플로우 호환)
/// </summary>
public class RecipeEditorWindow : EditorWindow
{
    // ──── 데이터 ────
    private RecipeDatabase _db;
    private ItemDatabase   _itemDb;
    private Dictionary<int, ItemSO> _itemCache = new Dictionary<int, ItemSO>();

    // 에디터에서 직접 관리하는 작업 목록
    private List<RecipeData>            _recipes          = new List<RecipeData>();
    private List<IngredientData>        _ingredients      = new List<IngredientData>();
    private List<RecipeUnlockCondition> _unlockConditions = new List<RecipeUnlockCondition>();

    // ──── 선택 & 필터 상태 ────
    private int               _selectedIndex   = -1;
    private string            _searchText      = "";
    private CraftingCategory? _filterCategory  = null;

    // ──── 스크롤 ────
    private Vector2 _listScroll;
    private Vector2 _detailScroll;

    // ──── 검증 ────
    private HashSet<int>       _duplicateIDs   = new HashSet<int>();
    private Dictionary<int, int> _recipeIndexMap = new Dictionary<int, int>(); // recipeID → _recipes 인덱스
    private bool               _isDirty        = false;

    // ──── 아이템 피커 팝업 ────
    private bool        _showItemPicker       = false;
    private string      _itemPickerSearch     = "";
    private Action<int> _itemPickerCallback;
    private Vector2     _itemPickerScroll;
    private bool        _itemPickerFocusRequested = false;

    private const string ITEM_PICKER_SEARCH_CONTROL = "ItemPickerSearchField";

    // ──── 상수 ────
    private const float LIST_PANEL_WIDTH = 270f;
    private const float ROW_HEIGHT       = 52f;

    // ──── 스타일 캐시 (Repaint마다 new GUIStyle 방지) ────
    private GUIStyle _titleStyle;

    // ──────────────────────────────────────────────────────────

    [MenuItem("UPlayGround/게임플레이/제작/레시피 에디터")]
    public static void ShowWindow()
    {
        var win = GetWindow<RecipeEditorWindow>("Recipe Editor");
        win.minSize = new Vector2(720, 520);
    }

    // ──────────────────────────────────────────────────────────
    #region 초기화

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        // 플레이모드 중에는 AssetDatabase 접근을 피한다
        // (씬 전환 시 OnEnable이 재호출되어 ArgumentException 발생 방지)
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            LoadAllDatabases();
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // 플레이모드 종료 후 에디트모드로 돌아왔을 때 DB 재로드
        if (state == PlayModeStateChange.EnteredEditMode)
            LoadAllDatabases();
    }

    private void LoadAllDatabases()
    {
        // RecipeDatabase 탐색
        string[] rGuids = AssetDatabase.FindAssets("t:RecipeDatabase");
        if (rGuids.Length > 0)
            _db = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(AssetDatabase.GUIDToAssetPath(rGuids[0]));

        // ItemDatabase 탐색
        string[] iGuids = AssetDatabase.FindAssets("t:ItemDatabase");
        if (iGuids.Length > 0)
        {
            _itemDb = AssetDatabase.LoadAssetAtPath<ItemDatabase>(AssetDatabase.GUIDToAssetPath(iGuids[0]));
            if (_itemDb != null)
            {
                _itemDb.Initialize();
                _itemCache = new Dictionary<int, ItemSO>();
                foreach (var item in _itemDb.AllItems.Where(i => i != null))
                {
                    if (!_itemCache.ContainsKey(item.itemId))
                        _itemCache[item.itemId] = item;
                    else
                        Debug.LogWarning($"[RecipeEditorWindow] 중복 itemId 발견: {item.itemId} ({item.name}) — 첫 번째 항목이 사용됩니다.");
                }
            }
        }

        RefreshWorkingCopy();
    }

    // SO의 현재 데이터를 작업 복사본으로 읽어 온다.
    private void RefreshWorkingCopy()
    {
        if (_db == null)
        {
            _recipes.Clear();
            _ingredients.Clear();
            _unlockConditions.Clear();
            _isDirty = false;
            return;
        }

        _recipes          = new List<RecipeData>(_db.AllRecipes);
        _ingredients      = new List<IngredientData>(_db.AllIngredients);
        _unlockConditions = new List<RecipeUnlockCondition>(_db.AllUnlockConditions);

        ValidateIDs();
        _isDirty = false;
    }

    private void ValidateIDs()
    {
        _duplicateIDs.Clear();
        _recipeIndexMap.Clear();
        var seen = new HashSet<int>();
        for (int i = 0; i < _recipes.Count; i++)
        {
            int id = _recipes[i].recipeID;
            if (!seen.Add(id))
                _duplicateIDs.Add(id);
            else
                _recipeIndexMap[id] = i;
        }
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region OnGUI 루트

    private void OnGUI()
    {
        DrawToolbar();

        if (_db == null)
        {
            DrawNoDatabaseMessage();
            return;
        }

        EditorGUILayout.BeginHorizontal();

        DrawLeftPanel();
        GUILayout.Box(GUIContent.none, GUILayout.Width(2), GUILayout.ExpandHeight(true));
        DrawRightPanel();

        EditorGUILayout.EndHorizontal();

        // 팝업은 다른 UI 위에 그린다
        if (_showItemPicker)
            DrawItemPickerPopup();
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region 툴바

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // DB 이름 표시 / 선택
        GUI.color = (_db == null) ? Color.red : Color.white;
        if (GUILayout.Button(_db != null ? _db.name : "DB 없음", EditorStyles.toolbarDropDown, GUILayout.Width(160)))
            ShowDBContextMenu();
        GUI.color = Color.white;

        GUILayout.FlexibleSpace();

        // 검색창
        GUILayout.Label("검색:", EditorStyles.miniLabel, GUILayout.Width(35));
        string newSearch = GUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.Width(160));
        if (newSearch != _searchText)
        {
            _searchText    = newSearch;
            _selectedIndex = -1;
        }
        if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)) && _searchText.Length > 0)
        {
            _searchText = "";
            GUI.FocusControl(null);
        }

        GUILayout.Space(8);

        // 저장 버튼 (변경 있으면 주황)
        GUI.color = _isDirty ? new Color(1f, 0.65f, 0f) : Color.white;
        if (GUILayout.Button(_isDirty ? "● 저장" : "저장됨", EditorStyles.toolbarButton, GUILayout.Width(75)))
            SaveDatabase();
        GUI.color = Color.white;

        if (GUILayout.Button("CSV 내보내기", EditorStyles.toolbarButton, GUILayout.Width(90)))
            ExportToCSV();

        if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
            LoadAllDatabases();

        EditorGUILayout.EndHorizontal();
    }

    private void ShowDBContextMenu()
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("DB 선택..."), false, SelectDatabase);
        menu.AddItem(new GUIContent("새 DB 생성..."), false, CreateNewDatabase);
        if (_db != null)
            menu.AddItem(new GUIContent("현재 DB 선택"), false, () => Selection.activeObject = _db);
        menu.ShowAsContext();
    }

    private void SelectDatabase()
    {
        string path = EditorUtility.OpenFilePanel("RecipeDatabase 선택", "Assets", "asset");
        if (string.IsNullOrEmpty(path)) return;
        path = "Assets" + path.Substring(Application.dataPath.Length);
        var db = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(path);
        if (db == null) { EditorUtility.DisplayDialog("오류", "선택한 파일이 RecipeDatabase가 아닙니다.", "확인"); return; }
        _db = db;
        _selectedIndex = -1;
        RefreshWorkingCopy();
    }

    private void CreateNewDatabase()
    {
        string path = EditorUtility.SaveFilePanelInProject("새 RecipeDatabase 생성", "RecipeDatabase", "asset", "저장 위치 선택");
        if (string.IsNullOrEmpty(path)) return;
        var db = ScriptableObject.CreateInstance<RecipeDatabase>();
        AssetDatabase.CreateAsset(db, path);
        AssetDatabase.SaveAssets();
        _db = db;
        RefreshWorkingCopy();
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region 좌측 패널 (레시피 목록)

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(LIST_PANEL_WIDTH));

        DrawCategoryTabs();

        // 레시피 목록
        _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.ExpandHeight(true));

        var filtered = GetFilteredRecipes();
        if (filtered.Count == 0)
        {
            GUILayout.Space(20);
            GUILayout.Label("레시피 없음", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            // IndexOf(O(n)) 대신 recipeID → index 역매핑으로 O(1) 조회
            foreach (var (recipe, realIdx) in filtered.Select(r => (r, _recipeIndexMap.GetValueOrDefault(r.recipeID, -1))))
                DrawRecipeListItem(recipe, realIdx);
        }

        EditorGUILayout.EndScrollView();

        DrawListBottomBar();

        EditorGUILayout.EndVertical();
    }

    private void DrawCategoryTabs()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        DrawCategoryTab(null,                        "전체");
        DrawCategoryTab(CraftingCategory.Consumable, "소비");
        DrawCategoryTab(CraftingCategory.Equipment,  "장비");
        DrawCategoryTab(CraftingCategory.Material,   "재료");
        DrawCategoryTab(CraftingCategory.Special,    "특수");
        EditorGUILayout.EndHorizontal();
    }

    private void DrawCategoryTab(CraftingCategory? cat, string label)
    {
        bool active = _filterCategory == cat;
        GUI.color = active ? new Color(0.45f, 0.75f, 1f) : Color.white;
        if (GUILayout.Button(label, EditorStyles.toolbarButton))
        {
            _filterCategory = cat;
            _selectedIndex  = -1;
        }
        GUI.color = Color.white;
    }

    private void DrawRecipeListItem(RecipeData recipe, int realIndex)
    {
        bool isSelected      = (realIndex == _selectedIndex);
        bool hasDupID        = _duplicateIDs.Contains(recipe.recipeID);
        bool hasUnlock       = _unlockConditions.Any(u => u.recipeID == recipe.recipeID);
        int  ingredientCount = _ingredients.Count(i => i.recipeID == recipe.recipeID);

        Rect rowRect = EditorGUILayout.BeginVertical(GUILayout.Height(ROW_HEIGHT));

        // 배경색 (선택 시)
        if (Event.current.type == EventType.Repaint)
        {
            if (isSelected)
                EditorGUI.DrawRect(rowRect, new Color(0.2f, 0.4f, 0.85f, 0.35f));
            else if (rowRect.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.05f));
        }

        // 클릭 → 선택
        if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
        {
            _selectedIndex = realIndex;
            GUI.FocusControl(null);
            Repaint();
        }

        // 카테고리 색상 세로 바 (왼쪽 4px)
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y + 3, 4, rowRect.height - 6),
                GetCategoryColor(recipe.category));

        // ── 내용 ──
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10);

        EditorGUILayout.BeginVertical();

        // 1행: 이름 + 뱃지
        EditorGUILayout.BeginHorizontal();

        if (hasDupID)
        {
            GUI.color = Color.red;
            GUILayout.Label("⚠", GUILayout.Width(16));
            GUI.color = Color.white;
        }

        GUILayout.Label(
            string.IsNullOrEmpty(recipe.recipeName) ? "(이름 없음)" : recipe.recipeName,
            EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

        if (recipe.isDebugUnlocked)
        {
            GUI.color = new Color(0.4f, 1f, 0.4f);
            GUILayout.Label("[D]", EditorStyles.miniLabel, GUILayout.Width(22));
            GUI.color = Color.white;
        }
        else if (hasUnlock)
        {
            GUI.color = new Color(1f, 0.8f, 0.2f);
            GUILayout.Label("[L]", EditorStyles.miniLabel, GUILayout.Width(22));
            GUI.color = Color.white;
        }

        EditorGUILayout.EndHorizontal();

        // 2행: ID / 카테고리 / 재료 수
        EditorGUILayout.BeginHorizontal();
        GUI.color = new Color(0.65f, 0.65f, 0.65f);
        GUILayout.Label($"#{recipe.recipeID}", EditorStyles.miniLabel, GUILayout.Width(40));
        GUILayout.Label(GetCategoryName(recipe.category), EditorStyles.miniLabel, GUILayout.Width(36));
        GUILayout.Label($"재료 {ingredientCount}종", EditorStyles.miniLabel);
        if (recipe.castTimeSeconds > 0f)
            GUILayout.Label($"{recipe.castTimeSeconds:F1}s", EditorStyles.miniLabel);
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        // 구분선
        Rect sep = EditorGUILayout.GetControlRect(GUILayout.Height(1));
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(sep, new Color(0.3f, 0.3f, 0.3f, 0.4f));
    }

    private void DrawListBottomBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUI.color = new Color(0.7f, 0.7f, 0.7f);
        GUILayout.Label($"총 {_recipes.Count}개", EditorStyles.miniLabel, GUILayout.Width(55));
        GUI.color = Color.white;

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("+ 추가", EditorStyles.toolbarButton, GUILayout.Width(55)))
            AddNewRecipe();

        GUI.enabled = (_selectedIndex >= 0 && _selectedIndex < _recipes.Count);

        if (GUILayout.Button("복제", EditorStyles.toolbarButton, GUILayout.Width(40)))
            DuplicateRecipe(_selectedIndex);

        GUI.color = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("삭제", EditorStyles.toolbarButton, GUILayout.Width(40)))
            DeleteRecipe(_selectedIndex);
        GUI.color = Color.white;

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    private List<RecipeData> GetFilteredRecipes()
    {
        IEnumerable<RecipeData> query = _recipes;

        if (_filterCategory.HasValue)
            query = query.Where(r => r.category == _filterCategory.Value);

        if (!string.IsNullOrEmpty(_searchText))
        {
            string lower = _searchText.ToLower();
            query = query.Where(r =>
                r.recipeName.ToLower().Contains(lower) ||
                r.recipeID.ToString().Contains(lower) ||
                (r.description != null && r.description.ToLower().Contains(lower)));
        }

        return query.ToList();
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region 우측 패널 (상세 편집)

    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

        if (_selectedIndex < 0 || _selectedIndex >= _recipes.Count)
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("← 좌측에서 레시피를 선택하세요", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            return;
        }

        var recipe = _recipes[_selectedIndex];

        // 카테고리 색상 헤더 바
        Rect headerBar = EditorGUILayout.GetControlRect(GUILayout.Height(5));
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(headerBar, GetCategoryColor(recipe.category));

        _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

        EditorGUI.BeginChangeCheck();

        DrawDetailHeader(recipe);
        EditorGUILayout.Space(6);

        DrawSection("기본 정보",        () => DrawBasicInfo(recipe));
        DrawSection("결과물",           () => DrawResultItem(recipe));
        DrawSection("필요 재료",        () => DrawIngredients(recipe));
        DrawSection("비용 & 제작 시간", () => DrawCostAndTime(recipe));
        DrawSection("언락 조건",        () => DrawUnlockCondition(recipe));

        if (EditorGUI.EndChangeCheck())
        {
            ValidateIDs();
            _isDirty = true;
        }

        EditorGUILayout.Space(20);
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawDetailHeader(RecipeData recipe)
    {
        EditorGUILayout.BeginHorizontal();

        _titleStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 };
        GUILayout.Label(string.IsNullOrEmpty(recipe.recipeName) ? "(이름 없음)" : recipe.recipeName, _titleStyle);

        GUILayout.FlexibleSpace();

        if (_duplicateIDs.Contains(recipe.recipeID))
        {
            GUI.color = Color.red;
            GUILayout.Label("⚠ 중복 ID!", EditorStyles.boldLabel);
            GUI.color = Color.white;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSection(string title, Action drawContent)
    {
        EditorGUILayout.BeginVertical("helpBox");
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        drawContent();
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(2);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(3);
    }

    // ──── 섹션: 기본 정보 ────

    private void DrawBasicInfo(RecipeData recipe)
    {
        int newID = EditorGUILayout.IntField("레시피 ID", recipe.recipeID);
        if (newID != recipe.recipeID)
        {
            // ID 변경 시 재료·언락 조건의 recipeID도 동기화
            int oldID = recipe.recipeID;
            recipe.recipeID = newID;
            foreach (var i in _ingredients.Where(x => x.recipeID == oldID)) i.recipeID = newID;
            foreach (var u in _unlockConditions.Where(x => x.recipeID == oldID)) u.recipeID = newID;
        }

        recipe.recipeName = EditorGUILayout.TextField("이름", recipe.recipeName);

        EditorGUILayout.LabelField("설명");
        recipe.description = EditorGUILayout.TextArea(
            recipe.description ?? "", GUILayout.MinHeight(40), GUILayout.MaxHeight(80));

        recipe.category = (CraftingCategory)EditorGUILayout.EnumPopup("카테고리", recipe.category);

        recipe.isDebugUnlocked = EditorGUILayout.Toggle(
            new GUIContent("디버그 언락", "true면 조건 없이 처음부터 해금"), recipe.isDebugUnlocked);
    }

    // ──── 섹션: 결과물 ────

    private void DrawResultItem(RecipeData recipe)
    {
        EditorGUILayout.BeginHorizontal();
        recipe.resultItemID = EditorGUILayout.IntField("결과 아이템 ID", recipe.resultItemID);
        if (GUILayout.Button("선택", GUILayout.Width(52)))
            OpenItemPicker(id => { recipe.resultItemID = id; _isDirty = true; Repaint(); });
        EditorGUILayout.EndHorizontal();

        DrawItemNameHint(recipe.resultItemID);

        recipe.resultQuantity = Mathf.Max(1, EditorGUILayout.IntField("결과 수량", recipe.resultQuantity));
    }

    // ──── 섹션: 필요 재료 ────

    private void DrawIngredients(RecipeData recipe)
    {
        var myIngredients = _ingredients.Where(i => i.recipeID == recipe.recipeID).ToList();

        if (myIngredients.Count == 0)
        {
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            EditorGUILayout.LabelField("재료 없음. 아래 버튼으로 추가하세요.", EditorStyles.miniLabel);
            GUI.color = Color.white;
        }

        bool removed = false;
        for (int i = 0; i < myIngredients.Count; i++)
        {
            if (removed) break;
            var ingr = myIngredients[i];

            EditorGUILayout.BeginVertical("box");

            // 헤더 행
            EditorGUILayout.BeginHorizontal();
            EditorGUI.indentLevel--;
            GUILayout.Label($"  재료 {i + 1}", EditorStyles.boldLabel, GUILayout.Width(55));
            GUILayout.FlexibleSpace();
            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18)))
            {
                _ingredients.Remove(ingr);
                _isDirty = true;
                removed  = true;
            }
            GUI.color = Color.white;
            EditorGUI.indentLevel++;
            EditorGUILayout.EndHorizontal();

            if (removed) { EditorGUILayout.EndVertical(); break; }

            // 아이템 ID + 선택
            EditorGUILayout.BeginHorizontal();
            ingr.ingredientItemID = EditorGUILayout.IntField("아이템 ID", ingr.ingredientItemID);
            if (GUILayout.Button("선택", GUILayout.Width(52)))
            {
                var captured = ingr;
                OpenItemPicker(id => { captured.ingredientItemID = id; _isDirty = true; Repaint(); });
            }
            EditorGUILayout.EndHorizontal();

            DrawItemNameHint(ingr.ingredientItemID);

            ingr.requiredQuantity = Mathf.Max(1, EditorGUILayout.IntField("필요 수량", ingr.requiredQuantity));

            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        GUILayout.Space(4);
        if (GUILayout.Button("+ 재료 추가", GUILayout.Height(26)))
        {
            _ingredients.Add(new IngredientData { recipeID = recipe.recipeID, requiredQuantity = 1 });
            _isDirty = true;
        }
    }

    // ──── 섹션: 비용 & 제작 시간 ────

    private void DrawCostAndTime(RecipeData recipe)
    {
        recipe.costType = (CostType)EditorGUILayout.EnumPopup("비용 유형", recipe.costType);

        if (recipe.costType != CostType.Free)
            recipe.costAmount = Mathf.Max(0, EditorGUILayout.IntField("골드 비용", recipe.costAmount));

        recipe.castTimeSeconds = Mathf.Max(0f, EditorGUILayout.FloatField("제작 시간 (초)", recipe.castTimeSeconds));
    }

    // ──── 섹션: 언락 조건 ────

    private void DrawUnlockCondition(RecipeData recipe)
    {
        var cond    = _unlockConditions.FirstOrDefault(u => u.recipeID == recipe.recipeID);
        bool hasCond = cond != null;

        bool wantCond = EditorGUILayout.Toggle("언락 조건 사용", hasCond);

        if (wantCond && !hasCond)
        {
            cond = new RecipeUnlockCondition { recipeID = recipe.recipeID };
            _unlockConditions.Add(cond);
            _isDirty = true;
        }
        else if (!wantCond && hasCond)
        {
            _unlockConditions.Remove(cond);
            _isDirty = true;
            cond     = null;
        }

        if (cond == null)
        {
            if (!recipe.isDebugUnlocked)
            {
                EditorGUILayout.HelpBox(
                    "언락 조건이 없으면 None으로 처리되어 즉시 해금됩니다.\n" +
                    "조건을 걸려면 위 토글을 켜거나, '디버그 언락'을 사용하세요.",
                    MessageType.Info);
            }
            return;
        }

        EditorGUILayout.Space(2);
        cond.conditionType = (UnlockConditionType)EditorGUILayout.EnumPopup("조건 유형", cond.conditionType);

        switch (cond.conditionType)
        {
            case UnlockConditionType.None:
                EditorGUILayout.HelpBox("None → 조건 없이 즉시 언락됩니다.", MessageType.Info);
                break;

            case UnlockConditionType.MonsterKill:
                cond.conditionStringValue = EditorGUILayout.TextField("Actor ID", cond.conditionStringValue);
                cond.conditionValue  = EditorGUILayout.IntField("레거시 숫자 ID", cond.conditionValue);
                cond.conditionValue2 = Mathf.Max(1, EditorGUILayout.IntField("처치 횟수", cond.conditionValue2));
                break;

            case UnlockConditionType.ItemCollect:
                EditorGUILayout.BeginHorizontal();
                cond.conditionValue = EditorGUILayout.IntField("아이템 ID", cond.conditionValue);
                if (GUILayout.Button("선택", GUILayout.Width(52)))
                {
                    var captured = cond;
                    OpenItemPicker(id => { captured.conditionValue = id; _isDirty = true; Repaint(); });
                }
                EditorGUILayout.EndHorizontal();
                DrawItemNameHint(cond.conditionValue);
                cond.conditionValue2 = Mathf.Max(1, EditorGUILayout.IntField("수집 수량", cond.conditionValue2));
                break;

            case UnlockConditionType.ItemHave:
                EditorGUILayout.BeginHorizontal();
                cond.conditionValue = EditorGUILayout.IntField("아이템 ID", cond.conditionValue);
                if (GUILayout.Button("선택", GUILayout.Width(52)))
                {
                    var captured = cond;
                    OpenItemPicker(id => { captured.conditionValue = id; _isDirty = true; Repaint(); });
                }
                EditorGUILayout.EndHorizontal();
                DrawItemNameHint(cond.conditionValue);
                cond.conditionValue2 = Mathf.Max(1, EditorGUILayout.IntField("소지 수량", cond.conditionValue2));
                break;

            case UnlockConditionType.RecipeCraft:
                DrawRecipeIDField(ref cond.conditionValue, "레시피 ID");
                cond.conditionValue2 = Mathf.Max(1, EditorGUILayout.IntField("제작 횟수", cond.conditionValue2));
                break;
        }
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region 아이템 피커 팝업

    private void OpenItemPicker(Action<int> callback)
    {
        _showItemPicker          = true;
        _itemPickerSearch        = "";
        _itemPickerCallback      = callback;
        _itemPickerScroll        = Vector2.zero;
        _itemPickerFocusRequested = true;
    }

    private void DrawItemPickerPopup()
    {
        // 팝업 위치: 우측 하단 기준
        float pw = 310f;
        float ph = 400f;
        Rect popupRect = new Rect(position.width - pw - 4, 30, pw, ph);

        // 팝업 외부 클릭 시 닫기
        if (Event.current.type == EventType.MouseDown && !popupRect.Contains(Event.current.mousePosition))
        {
            _showItemPicker = false;
            Repaint();
            return;
        }

        // 배경 박스
        GUI.Box(popupRect, GUIContent.none, "window");

        GUILayout.BeginArea(popupRect);

        // 헤더
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("아이템 선택", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(22)))
        {
            _showItemPicker = false;
            EditorGUILayout.EndHorizontal(); // BeginHorizontal 반드시 먼저 닫기
            GUILayout.EndArea();
            Repaint();
            return;
        }
        EditorGUILayout.EndHorizontal();

        if (_itemDb == null)
        {
            EditorGUILayout.HelpBox("ItemDatabase를 찾을 수 없습니다.\nID를 직접 입력하세요.", MessageType.Warning);
            GUILayout.EndArea();
            return;
        }

        // 검색창 — SetNextControlName으로 포커스 제어 (한글 IME 입력 지원)
        GUI.SetNextControlName(ITEM_PICKER_SEARCH_CONTROL);
        _itemPickerSearch = EditorGUILayout.TextField(_itemPickerSearch, EditorStyles.toolbarSearchField);

        if (_itemPickerFocusRequested)
        {
            EditorGUI.FocusTextInControl(ITEM_PICKER_SEARCH_CONTROL);
            _itemPickerFocusRequested = false;
        }

        // 아이템 목록
        _itemPickerScroll = EditorGUILayout.BeginScrollView(_itemPickerScroll);

        var items = _itemDb.AllItems
            .Where(i => i != null)
            .Where(i => string.IsNullOrEmpty(_itemPickerSearch)
                     || i.itemName.IndexOf(_itemPickerSearch, System.StringComparison.CurrentCultureIgnoreCase) >= 0
                     || i.itemId.ToString().Contains(_itemPickerSearch))
            .OrderBy(i => i.itemId)
            .ToList();

        if (items.Count == 0)
        {
            GUILayout.Label("검색 결과 없음", EditorStyles.centeredGreyMiniLabel);
        }

        foreach (var item in items)
        {
            EditorGUILayout.BeginHorizontal("helpBox");

            // 아이콘
            if (item.icon != null)
            {
                var preview = AssetPreview.GetAssetPreview(item.icon);
                if (preview != null)
                    GUILayout.Label(preview, GUILayout.Width(28), GUILayout.Height(28));
                else
                    GUILayout.Label("□", GUILayout.Width(28), GUILayout.Height(28));
            }
            else
            {
                GUILayout.Label("□", GUILayout.Width(28), GUILayout.Height(28));
            }

            EditorGUILayout.BeginVertical();
            GUILayout.Label(item.itemName, EditorStyles.boldLabel);
            GUI.color = new Color(0.65f, 0.65f, 0.65f);
            GUILayout.Label($"ID: {item.itemId}  |  {item.itemType}", EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("선택", GUILayout.Width(44), GUILayout.Height(32)))
            {
                _itemPickerCallback?.Invoke(item.itemId);
                _showItemPicker = false;
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(1);
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();

        Repaint();
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region 공통 드로우 헬퍼

    /// <summary> 아이템 ID 아래에 초록/빨강으로 이름 힌트 표시 </summary>
    private void DrawItemNameHint(int itemID)
    {
        if (itemID == 0) return;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUI.indentLevel * 15f + 18f);

        if (_itemCache.TryGetValue(itemID, out var item))
        {
            GUI.color = new Color(0.4f, 1f, 0.4f);
            GUILayout.Label($"→ {item.itemName}  [{item.itemType}]", EditorStyles.miniLabel);
        }
        else
        {
            GUI.color = new Color(1f, 0.4f, 0.4f);
            GUILayout.Label($"⚠ ID {itemID} — 등록된 아이템 없음", EditorStyles.miniLabel);
        }
        GUI.color = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    /// <summary> 레시피 ID 필드 + 레시피 이름 힌트 </summary>
    private void DrawRecipeIDField(ref int recipeID, string label)
    {
        EditorGUILayout.BeginHorizontal();
        recipeID = EditorGUILayout.IntField(label, recipeID);
        int recipeIDCopy = recipeID;
        var targetRecipe = _recipes.FirstOrDefault(r => r.recipeID == recipeIDCopy);
        if (targetRecipe != null)
        {
            GUI.color = new Color(0.4f, 1f, 0.4f);
            GUILayout.Label($"→ {targetRecipe.recipeName}", EditorStyles.miniLabel);
            GUI.color = Color.white;
        }
        EditorGUILayout.EndHorizontal();
    }

    private static Color GetCategoryColor(CraftingCategory cat) => cat switch
    {
        CraftingCategory.Consumable => new Color(0.3f, 0.85f, 0.3f),
        CraftingCategory.Equipment  => new Color(0.3f, 0.55f, 1.0f),
        CraftingCategory.Material   => new Color(0.95f, 0.75f, 0.2f),
        CraftingCategory.Special    => new Color(0.85f, 0.3f, 0.85f),
        _                           => Color.gray,
    };

    private static string GetCategoryName(CraftingCategory cat) => cat switch
    {
        CraftingCategory.Consumable => "소비",
        CraftingCategory.Equipment  => "장비",
        CraftingCategory.Material   => "재료",
        CraftingCategory.Special    => "특수",
        _                           => "?",
    };

    private void DrawNoDatabaseMessage()
    {
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginVertical();
        GUILayout.Space(20);
        GUILayout.Label("RecipeDatabase를 찾을 수 없습니다.", EditorStyles.boldLabel);
        GUILayout.Space(12);
        if (GUILayout.Button("DB 파일 선택...", GUILayout.Width(150), GUILayout.Height(30))) SelectDatabase();
        GUILayout.Space(6);
        if (GUILayout.Button("새 DB 생성...",  GUILayout.Width(150), GUILayout.Height(30))) CreateNewDatabase();
        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region 레시피 CRUD

    private void AddNewRecipe()
    {
        int newID = _recipes.Count > 0 ? _recipes.Max(r => r.recipeID) + 1 : 1;
        _recipes.Add(new RecipeData
        {
            recipeID        = newID,
            recipeName      = $"새 레시피 {newID}",
            description     = "",
            resultQuantity  = 1,
            castTimeSeconds = 2f,
            costType        = CostType.Free,
        });
        _selectedIndex = _recipes.Count - 1;
        _isDirty = true;
        ValidateIDs();
    }

    private void DuplicateRecipe(int index)
    {
        if (index < 0 || index >= _recipes.Count) return;

        var src   = _recipes[index];
        int newID = _recipes.Max(r => r.recipeID) + 1;

        _recipes.Add(new RecipeData
        {
            recipeID        = newID,
            recipeName      = src.recipeName + " (복사)",
            description     = src.description,
            resultItemID    = src.resultItemID,
            resultQuantity  = src.resultQuantity,
            costType        = src.costType,
            costAmount      = src.costAmount,
            castTimeSeconds = src.castTimeSeconds,
            category        = src.category,
            isDebugUnlocked = src.isDebugUnlocked,
        });

        // 재료도 복사
        foreach (var ingr in _ingredients.Where(i => i.recipeID == src.recipeID).ToList())
        {
            _ingredients.Add(new IngredientData
            {
                recipeID         = newID,
                ingredientItemID = ingr.ingredientItemID,
                requiredQuantity = ingr.requiredQuantity,
            });
        }

        // 언락 조건은 의도적으로 복사하지 않음.
        // 복사본은 보통 별도 언락 조건이 필요하므로 사용자가 직접 설정하도록 함.

        _selectedIndex = _recipes.Count - 1;
        _isDirty = true;
        ValidateIDs();
    }

    private void DeleteRecipe(int index)
    {
        if (index < 0 || index >= _recipes.Count) return;

        var recipe = _recipes[index];
        if (!EditorUtility.DisplayDialog("삭제 확인",
            $"'{recipe.recipeName}' 레시피를 삭제하겠습니까?\n연결된 재료 및 언락 조건도 함께 삭제됩니다.",
            "삭제", "취소")) return;

        int id = recipe.recipeID;
        _recipes.RemoveAt(index);
        _ingredients.RemoveAll(i => i.recipeID == id);
        _unlockConditions.RemoveAll(u => u.recipeID == id);

        _selectedIndex = Mathf.Clamp(_selectedIndex, -1, _recipes.Count - 1);
        _isDirty = true;
        ValidateIDs();
    }

    #endregion

    // ──────────────────────────────────────────────────────────
    #region 저장 & 내보내기

    private void SaveDatabase()
    {
        if (_db == null) return;

        _db.SetRecipes(_recipes);
        _db.SetIngredients(_ingredients);
        _db.SetUnlockConditions(_unlockConditions);

        EditorUtility.SetDirty(_db);
        AssetDatabase.SaveAssets();

        _isDirty = false;
        Debug.Log($"[RecipeEditor] 저장 완료 — 레시피 {_recipes.Count}개 / 재료 행 {_ingredients.Count}개 / 언락 {_unlockConditions.Count}개");
    }

    private void ExportToCSV()
    {
        string dir = EditorUtility.SaveFolderPanel("CSV 내보낼 폴더 선택", "Assets", "");
        if (string.IsNullOrEmpty(dir)) return;

        // recipe_master.csv
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("recipeID,recipeName,resultItemID,resultQuantity,costType,costAmount,castTimeSeconds,category,description,isDebugUnlocked");
        foreach (var r in _recipes)
            sb.AppendLine($"{r.recipeID},\"{r.recipeName}\",{r.resultItemID},{r.resultQuantity},{r.costType},{r.costAmount},{r.castTimeSeconds},{r.category},\"{r.description}\",{r.isDebugUnlocked.ToString().ToUpper()}");
        File.WriteAllText(Path.Combine(dir, "recipe_master.csv"), sb.ToString(), System.Text.Encoding.UTF8);

        // recipe_ingredients.csv
        sb.Clear();
        sb.AppendLine("recipeID,ingredientItemID,requiredQuantity");
        foreach (var i in _ingredients)
            sb.AppendLine($"{i.recipeID},{i.ingredientItemID},{i.requiredQuantity}");
        File.WriteAllText(Path.Combine(dir, "recipe_ingredients.csv"), sb.ToString(), System.Text.Encoding.UTF8);

        // recipe_unlocks.csv
        sb.Clear();
        sb.AppendLine("recipeID,conditionType,conditionValue,conditionValue2,conditionStringValue");
        foreach (var u in _unlockConditions)
            sb.AppendLine($"{u.recipeID},{u.conditionType},{u.conditionValue},{u.conditionValue2},{u.conditionStringValue}");
        File.WriteAllText(Path.Combine(dir, "recipe_unlocks.csv"), sb.ToString(), System.Text.Encoding.UTF8);

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("내보내기 완료", $"CSV 3개 파일 저장:\n{dir}", "확인");
    }

    #endregion
}
#endif
