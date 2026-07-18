#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Crafting;
using UPlayGround.Data.Path;
using UPlayGround.Data.Item;

namespace UPlayGround.Data.Crafting.Editor
{
    /// <summary>
    /// 레시피 비주얼 에디터 윈도우 (UIToolkit).
    /// 메뉴: UPlayGround / 게임플레이 / 제작 / 레시피 에디터
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
        private RecipeData        _selected;
        private List<RecipeData>  _filtered       = new List<RecipeData>();
        private string            _searchText     = "";
        private CraftingCategory? _filterCategory = null;

        // ──── 검증 ────
        private HashSet<int> _duplicateIDs = new HashSet<int>();
        private bool         _isDirty      = false;

        // ──── UI 요소 ────
        private VisualElement _body;
        private VisualElement _noDbPane;
        private ListView      _listView;
        private Label         _countLabel;
        private VisualElement _detailPane;
        private VisualElement _itemPickerPopup;
        private ToolbarMenu   _dbMenu;
        private ToolbarButton _saveButton;
        private ToolbarButton _duplicateButton;
        private ToolbarButton _deleteButton;
        private readonly List<ToolbarToggle> _categoryToggles = new List<ToolbarToggle>();

        // ──── 상수 ────
        private const float LIST_PANEL_WIDTH = 270f;

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
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 플레이모드 종료 후 에디트모드로 돌아왔을 때 DB 재로드
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                LoadAllDatabases();
                RefreshAll();
            }
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
            _selected = null;

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
            var seen = new HashSet<int>();
            foreach (var r in _recipes)
            {
                if (!seen.Add(r.recipeID))
                    _duplicateIDs.Add(r.recipeID);
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region UI 구성

        private void CreateGUI()
        {
            // 플레이모드 중에는 AssetDatabase 접근을 피한다
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                LoadAllDatabases();

            var root = rootVisualElement;
            root.Clear();

            root.Add(BuildToolbar());

            _noDbPane = BuildNoDatabasePane();
            root.Add(_noDbPane);

            _body = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            _body.Add(BuildListPanel());
            _detailPane = new VisualElement { style = { flexGrow = 1 } };
            _body.Add(_detailPane);
            root.Add(_body);

            RefreshAll();
        }

        private Toolbar BuildToolbar()
        {
            var toolbar = new Toolbar();

            _dbMenu = new ToolbarMenu { text = "DB 없음", style = { width = 170 } };
            _dbMenu.menu.AppendAction("DB 선택...", _ => SelectDatabase());
            _dbMenu.menu.AppendAction("새 DB 생성...", _ => CreateNewDatabase());
            _dbMenu.menu.AppendAction("현재 DB 선택", _ =>
            {
                if (_db != null) Selection.activeObject = _db;
            }, _ => _db != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            toolbar.Add(_dbMenu);

            toolbar.Add(new VisualElement { style = { flexGrow = 1 } });

            var search = new ToolbarSearchField { style = { width = 180 } };
            search.RegisterValueChangedCallback(evt =>
            {
                _searchText = evt.newValue;
                RefreshList();
            });
            toolbar.Add(search);

            _saveButton = new ToolbarButton(SaveDatabase) { text = "저장됨", style = { width = 75 } };
            toolbar.Add(_saveButton);

            toolbar.Add(new ToolbarButton(ExportToCSV) { text = "CSV 내보내기" });
            toolbar.Add(new ToolbarButton(() =>
            {
                LoadAllDatabases();
                RefreshAll();
            }) { text = "새로고침" });

            return toolbar;
        }

        private VisualElement BuildNoDatabasePane()
        {
            var pane = new VisualElement
            {
                style = { flexGrow = 1, justifyContent = Justify.Center, alignItems = Align.Center }
            };
            pane.Add(new Label("RecipeDatabase를 찾을 수 없습니다.")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 12 }
            });
            pane.Add(new Button(SelectDatabase) { text = "DB 파일 선택...", style = { width = 150, height = 30, marginBottom = 6 } });
            pane.Add(new Button(CreateNewDatabase) { text = "새 DB 생성...", style = { width = 150, height = 30 } });
            return pane;
        }

        private VisualElement BuildListPanel()
        {
            var panel = new VisualElement
            {
                style =
                {
                    width = LIST_PANEL_WIDTH,
                    flexShrink = 0,
                    borderRightWidth = 1,
                    borderRightColor = new Color(0f, 0f, 0f, 0.35f),
                }
            };

            // 카테고리 탭
            var tabBar = new Toolbar();
            var categories = new (CraftingCategory? cat, string label)[]
            {
                (null, "전체"),
                (CraftingCategory.Consumable, "소비"),
                (CraftingCategory.Equipment,  "장비"),
                (CraftingCategory.Material,   "재료"),
                (CraftingCategory.Special,    "특수"),
            };
            _categoryToggles.Clear();
            foreach (var (cat, label) in categories)
            {
                var captured = cat;
                var toggle = new ToolbarToggle { text = label, value = _filterCategory == cat, style = { flexGrow = 1 } };
                toggle.RegisterValueChangedCallback(evt =>
                {
                    if (!evt.newValue)
                    {
                        toggle.SetValueWithoutNotify(_filterCategory == captured);
                        return;
                    }
                    _filterCategory = captured;
                    foreach (var t in _categoryToggles)
                        t.SetValueWithoutNotify(t == toggle);
                    RefreshList();
                });
                _categoryToggles.Add(toggle);
                tabBar.Add(toggle);
            }
            panel.Add(tabBar);

            _listView = new ListView
            {
                fixedItemHeight = 52,
                selectionType = SelectionType.Single,
                style = { flexGrow = 1 },
                makeItem = MakeListRow,
                bindItem = BindListRow,
            };
            _listView.selectionChanged += _ =>
            {
                _selected = _listView.selectedItem as RecipeData;
                RebuildDetail();
                UpdateSelectionButtons();
            };
            panel.Add(_listView);

            // 하단 바
            var bottom = new Toolbar();
            _countLabel = new Label { style = { color = new Color(0.7f, 0.7f, 0.7f), fontSize = 10, unityTextAlign = TextAnchor.MiddleLeft } };
            bottom.Add(_countLabel);
            bottom.Add(new VisualElement { style = { flexGrow = 1 } });
            bottom.Add(new ToolbarButton(AddNewRecipe) { text = "+ 추가" });
            _duplicateButton = new ToolbarButton(() => DuplicateRecipe(_selected)) { text = "복제" };
            bottom.Add(_duplicateButton);
            _deleteButton = new ToolbarButton(() => DeleteRecipe(_selected)) { text = "삭제" };
            _deleteButton.style.color = new Color(1f, 0.5f, 0.5f);
            bottom.Add(_deleteButton);
            panel.Add(bottom);

            return panel;
        }

        private static VisualElement MakeListRow()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            row.Add(new VisualElement
            {
                name = "catbar",
                style = { width = 4, height = 44, flexShrink = 0, marginLeft = 2, marginRight = 6 }
            });

            var info = new VisualElement { style = { flexGrow = 1, justifyContent = Justify.Center } };

            var nameRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            nameRow.Add(new Label("⚠") { name = "dup", style = { color = Color.red, marginRight = 2 } });
            nameRow.Add(new Label { name = "name", style = { unityFontStyleAndWeight = FontStyle.Bold, flexGrow = 1 } });
            nameRow.Add(new Label("[D]") { name = "debug", style = { color = new Color(0.4f, 1f, 0.4f), fontSize = 10, marginRight = 4 } });
            nameRow.Add(new Label("[L]") { name = "lock", style = { color = new Color(1f, 0.8f, 0.2f), fontSize = 10, marginRight = 4 } });
            info.Add(nameRow);

            info.Add(new Label { name = "sub", style = { color = new Color(0.65f, 0.65f, 0.65f), fontSize = 10 } });
            row.Add(info);

            return row;
        }

        private void BindListRow(VisualElement row, int index)
        {
            if (index < 0 || index >= _filtered.Count) return;
            var recipe = _filtered[index];

            bool hasDupID  = _duplicateIDs.Contains(recipe.recipeID);
            bool hasUnlock = _unlockConditions.Any(u => u.recipeID == recipe.recipeID);
            int  ingrCount = _ingredients.Count(i => i.recipeID == recipe.recipeID);

            row.Q("catbar").style.backgroundColor = GetCategoryColor(recipe.category);
            row.Q<Label>("dup").style.display = hasDupID ? DisplayStyle.Flex : DisplayStyle.None;
            row.Q<Label>("name").text = string.IsNullOrEmpty(recipe.recipeName) ? "(이름 없음)" : recipe.recipeName;
            row.Q<Label>("debug").style.display = recipe.isDebugUnlocked ? DisplayStyle.Flex : DisplayStyle.None;
            row.Q<Label>("lock").style.display = !recipe.isDebugUnlocked && hasUnlock ? DisplayStyle.Flex : DisplayStyle.None;

            string sub = $"#{recipe.recipeID}  {GetCategoryName(recipe.category)}  재료 {ingrCount}종";
            if (recipe.castTimeSeconds > 0f)
                sub += $"  {recipe.castTimeSeconds:F1}s";
            row.Q<Label>("sub").text = sub;
        }

        private void RefreshAll()
        {
            if (_body == null) return; // CreateGUI 이전 호출 가드

            bool hasDb = _db != null;
            if (_dbMenu != null)
            {
                _dbMenu.text = hasDb ? _db.name : "DB 없음";
                _dbMenu.style.color = hasDb ? StyleKeyword.Null : (StyleColor)Color.red;
            }
            _noDbPane.style.display = hasDb ? DisplayStyle.None : DisplayStyle.Flex;
            _body.style.display     = hasDb ? DisplayStyle.Flex : DisplayStyle.None;

            UpdateSaveButton();
            if (hasDb)
                RefreshList();
        }

        private void RefreshList(bool rebuildDetail = true)
        {
            _filtered = GetFilteredRecipes();
            _listView.itemsSource = _filtered;
            _listView.RefreshItems();
            _countLabel.text = $"총 {_recipes.Count}개";

            int idx = _selected != null ? _filtered.IndexOf(_selected) : -1;
            _listView.SetSelectionWithoutNotify(idx >= 0 ? new[] { idx } : Array.Empty<int>());
            bool selectionCleared = _selected != null && idx < 0;
            if (selectionCleared)
                _selected = null;
            if (rebuildDetail || selectionCleared)
                RebuildDetail();
            UpdateSelectionButtons();
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

        private void UpdateSelectionButtons()
        {
            bool has = _selected != null;
            _duplicateButton?.SetEnabled(has);
            _deleteButton?.SetEnabled(has);
        }

        private void MarkDirty()
        {
            _isDirty = true;
            ValidateIDs();
            UpdateSaveButton();
            RefreshList(false);
        }

        private void UpdateSaveButton()
        {
            if (_saveButton == null) return;
            _saveButton.text = _isDirty ? "● 저장" : "저장됨";
            _saveButton.style.color = _isDirty ? (StyleColor)new Color(1f, 0.65f, 0f) : StyleKeyword.Null;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region DB 선택 / 생성

        private void SelectDatabase()
        {
            string path = EditorUtility.OpenFilePanel("RecipeDatabase 선택", "Assets", "asset");
            if (string.IsNullOrEmpty(path)) return;
            path = "Assets" + path.Substring(Application.dataPath.Length);
            var db = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(path);
            if (db == null) { EditorUtility.DisplayDialog("오류", "선택한 파일이 RecipeDatabase가 아닙니다.", "확인"); return; }
            _db = db;
            RefreshWorkingCopy();
            RefreshAll();
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
            RefreshAll();
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 상세 패널 (우)

        private void RebuildDetail()
        {
            CloseItemPicker();
            _detailPane.Clear();

            if (_selected == null)
            {
                var hint = new VisualElement
                {
                    style = { flexGrow = 1, justifyContent = Justify.Center, alignItems = Align.Center }
                };
                hint.Add(new Label("← 좌측에서 레시피를 선택하세요") { style = { color = new Color(0.55f, 0.55f, 0.55f) } });
                _detailPane.Add(hint);
                return;
            }

            var recipe = _selected;

            // 카테고리 색상 헤더 바
            var headerBar = new VisualElement
            {
                style = { height = 5, backgroundColor = GetCategoryColor(recipe.category), flexShrink = 0 }
            };
            _detailPane.Add(headerBar);

            var scroll = new ScrollView { style = { flexGrow = 1, paddingLeft = 8, paddingRight = 8, paddingTop = 6 } };
            _detailPane.Add(scroll);

            // ── 상세 헤더 ────────────────────────────────────────
            var titleRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var titleLabel = new Label(string.IsNullOrEmpty(recipe.recipeName) ? "(이름 없음)" : recipe.recipeName)
            {
                style = { fontSize = 15, unityFontStyleAndWeight = FontStyle.Bold }
            };
            titleRow.Add(titleLabel);
            titleRow.Add(new VisualElement { style = { flexGrow = 1 } });
            var dupLabel = new Label("⚠ 중복 ID!")
            {
                style = { color = Color.red, unityFontStyleAndWeight = FontStyle.Bold }
            };
            dupLabel.style.display = _duplicateIDs.Contains(recipe.recipeID) ? DisplayStyle.Flex : DisplayStyle.None;
            titleRow.Add(dupLabel);
            scroll.Add(titleRow);

            // ── 기본 정보 ────────────────────────────────────────
            var basic = MakeSection("기본 정보");

            var idField = new IntegerField("레시피 ID") { value = recipe.recipeID };
            idField.RegisterValueChangedCallback(evt =>
            {
                // ID 변경 시 재료·언락 조건의 recipeID도 동기화
                int oldID = recipe.recipeID;
                recipe.recipeID = evt.newValue;
                foreach (var i in _ingredients.Where(x => x.recipeID == oldID)) i.recipeID = evt.newValue;
                foreach (var u in _unlockConditions.Where(x => x.recipeID == oldID)) u.recipeID = evt.newValue;
                MarkDirty();
                dupLabel.style.display = _duplicateIDs.Contains(recipe.recipeID) ? DisplayStyle.Flex : DisplayStyle.None;
            });
            basic.Add(idField);

            var nameField = new TextField("이름") { value = recipe.recipeName };
            nameField.RegisterValueChangedCallback(evt =>
            {
                recipe.recipeName = evt.newValue;
                titleLabel.text = string.IsNullOrEmpty(evt.newValue) ? "(이름 없음)" : evt.newValue;
                MarkDirty();
            });
            basic.Add(nameField);

            basic.Add(new Label("설명"));
            var descField = new TextField { value = recipe.description ?? "", multiline = true, style = { minHeight = 40, maxHeight = 80 } };
            descField.RegisterValueChangedCallback(evt => { recipe.description = evt.newValue; MarkDirty(); });
            basic.Add(descField);

            var catField = new EnumField("카테고리", recipe.category);
            catField.RegisterValueChangedCallback(evt =>
            {
                recipe.category = (CraftingCategory)evt.newValue;
                headerBar.style.backgroundColor = GetCategoryColor(recipe.category);
                MarkDirty();
            });
            basic.Add(catField);

            var debugToggle = new Toggle("디버그 언락") { value = recipe.isDebugUnlocked, tooltip = "true면 조건 없이 처음부터 해금" };
            debugToggle.RegisterValueChangedCallback(evt => { recipe.isDebugUnlocked = evt.newValue; MarkDirty(); });
            basic.Add(debugToggle);

            scroll.Add(basic);

            // ── 결과물 ───────────────────────────────────────────
            var result = MakeSection("결과물");
            var resultHint = MakeItemHintLabel();
            var resultRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var resultIdField = new IntegerField("결과 아이템 ID") { value = recipe.resultItemID, style = { flexGrow = 1 } };
            resultIdField.RegisterValueChangedCallback(evt =>
            {
                recipe.resultItemID = evt.newValue;
                UpdateItemHint(resultHint, evt.newValue);
                MarkDirty();
            });
            resultRow.Add(resultIdField);
            resultRow.Add(new Button(() => OpenItemPicker(id =>
            {
                recipe.resultItemID = id;
                resultIdField.SetValueWithoutNotify(id);
                UpdateItemHint(resultHint, id);
                MarkDirty();
            })) { text = "선택", style = { width = 52 } });
            result.Add(resultRow);
            result.Add(resultHint);
            UpdateItemHint(resultHint, recipe.resultItemID);

            var resultQtyField = new IntegerField("결과 수량") { value = recipe.resultQuantity };
            resultQtyField.RegisterValueChangedCallback(evt =>
            {
                int v = Mathf.Max(1, evt.newValue);
                resultQtyField.SetValueWithoutNotify(v);
                recipe.resultQuantity = v;
                MarkDirty();
            });
            result.Add(resultQtyField);
            scroll.Add(result);

            // ── 필요 재료 ────────────────────────────────────────
            var ingrSection = MakeSection("필요 재료");
            BuildIngredientRows(ingrSection, recipe);
            scroll.Add(ingrSection);

            // ── 비용 & 제작 시간 ─────────────────────────────────
            var cost = MakeSection("비용 & 제작 시간");
            var goldField = new IntegerField("골드 비용") { value = recipe.costAmount };
            var costTypeField = new EnumField("비용 유형", recipe.costType);
            costTypeField.RegisterValueChangedCallback(evt =>
            {
                recipe.costType = (CostType)evt.newValue;
                goldField.style.display = recipe.costType != CostType.Free ? DisplayStyle.Flex : DisplayStyle.None;
                MarkDirty();
            });
            cost.Add(costTypeField);
            goldField.RegisterValueChangedCallback(evt =>
            {
                int v = Mathf.Max(0, evt.newValue);
                goldField.SetValueWithoutNotify(v);
                recipe.costAmount = v;
                MarkDirty();
            });
            goldField.style.display = recipe.costType != CostType.Free ? DisplayStyle.Flex : DisplayStyle.None;
            cost.Add(goldField);

            var castField = new FloatField("제작 시간 (초)") { value = recipe.castTimeSeconds };
            castField.RegisterValueChangedCallback(evt =>
            {
                float v = Mathf.Max(0f, evt.newValue);
                castField.SetValueWithoutNotify(v);
                recipe.castTimeSeconds = v;
                MarkDirty();
            });
            cost.Add(castField);
            scroll.Add(cost);

            // ── 언락 조건 ────────────────────────────────────────
            var unlockSection = MakeSection("언락 조건");
            BuildUnlockCondition(unlockSection, recipe);
            scroll.Add(unlockSection);
        }

        // ──── 섹션: 필요 재료 ────

        private void BuildIngredientRows(VisualElement section, RecipeData recipe)
        {
            // 타이틀(첫 요소)만 남기고 재구축
            while (section.childCount > 1)
                section.RemoveAt(section.childCount - 1);

            var myIngredients = _ingredients.Where(i => i.recipeID == recipe.recipeID).ToList();

            if (myIngredients.Count == 0)
            {
                section.Add(new Label("재료 없음. 아래 버튼으로 추가하세요.")
                {
                    style = { color = new Color(0.7f, 0.7f, 0.7f), fontSize = 10 }
                });
            }

            for (int i = 0; i < myIngredients.Count; i++)
            {
                var ingr = myIngredients[i];

                var box = new VisualElement
                {
                    style =
                    {
                        marginTop = 2, paddingLeft = 6, paddingRight = 6, paddingTop = 4, paddingBottom = 4,
                        backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.1f),
                        borderTopLeftRadius = 3, borderTopRightRadius = 3,
                        borderBottomLeftRadius = 3, borderBottomRightRadius = 3,
                    }
                };

                // 헤더 행
                var headRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                headRow.Add(new Label($"재료 {i + 1}") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
                headRow.Add(new VisualElement { style = { flexGrow = 1 } });
                headRow.Add(new Button(() =>
                {
                    _ingredients.Remove(ingr);
                    MarkDirty();
                    BuildIngredientRows(section, recipe);
                }) { text = "✕", style = { width = 22, color = new Color(1f, 0.5f, 0.5f) } });
                box.Add(headRow);

                // 아이템 ID + 선택
                var hint = MakeItemHintLabel();
                var idRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                var idField = new IntegerField("아이템 ID") { value = ingr.ingredientItemID, style = { flexGrow = 1 } };
                idField.RegisterValueChangedCallback(evt =>
                {
                    ingr.ingredientItemID = evt.newValue;
                    UpdateItemHint(hint, evt.newValue);
                    MarkDirty();
                });
                idRow.Add(idField);
                idRow.Add(new Button(() => OpenItemPicker(id =>
                {
                    ingr.ingredientItemID = id;
                    idField.SetValueWithoutNotify(id);
                    UpdateItemHint(hint, id);
                    MarkDirty();
                })) { text = "선택", style = { width = 52 } });
                box.Add(idRow);
                box.Add(hint);
                UpdateItemHint(hint, ingr.ingredientItemID);

                var qtyField = new IntegerField("필요 수량") { value = ingr.requiredQuantity };
                qtyField.RegisterValueChangedCallback(evt =>
                {
                    int v = Mathf.Max(1, evt.newValue);
                    qtyField.SetValueWithoutNotify(v);
                    ingr.requiredQuantity = v;
                    MarkDirty();
                });
                box.Add(qtyField);

                section.Add(box);
            }

            section.Add(new Button(() =>
            {
                _ingredients.Add(new IngredientData { recipeID = recipe.recipeID, requiredQuantity = 1 });
                MarkDirty();
                BuildIngredientRows(section, recipe);
            }) { text = "+ 재료 추가", style = { height = 26, marginTop = 4 } });
        }

        // ──── 섹션: 언락 조건 ────

        private void BuildUnlockCondition(VisualElement section, RecipeData recipe)
        {
            while (section.childCount > 1)
                section.RemoveAt(section.childCount - 1);

            var cond    = _unlockConditions.FirstOrDefault(u => u.recipeID == recipe.recipeID);
            bool hasCond = cond != null;

            var useToggle = new Toggle("언락 조건 사용") { value = hasCond };
            useToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue && cond == null)
                {
                    _unlockConditions.Add(new RecipeUnlockCondition { recipeID = recipe.recipeID });
                    MarkDirty();
                }
                else if (!evt.newValue && cond != null)
                {
                    _unlockConditions.Remove(cond);
                    MarkDirty();
                }
                BuildUnlockCondition(section, recipe);
            });
            section.Add(useToggle);

            if (cond == null)
            {
                if (!recipe.isDebugUnlocked)
                {
                    section.Add(new HelpBox(
                        "언락 조건이 없으면 None으로 처리되어 즉시 해금됩니다.\n" +
                        "조건을 걸려면 위 토글을 켜거나, '디버그 언락'을 사용하세요.",
                        HelpBoxMessageType.Info));
                }
                return;
            }

            var typeField = new EnumField("조건 유형", cond.conditionType);
            typeField.RegisterValueChangedCallback(evt =>
            {
                cond.conditionType = (UnlockConditionType)evt.newValue;
                MarkDirty();
                BuildUnlockCondition(section, recipe);
            });
            section.Add(typeField);

            switch (cond.conditionType)
            {
                case UnlockConditionType.None:
                    section.Add(new HelpBox("None → 조건 없이 즉시 언락됩니다.", HelpBoxMessageType.Info));
                    break;

                case UnlockConditionType.MonsterKill:
                {
                    var actorField = new TextField("Actor ID") { value = cond.conditionStringValue };
                    actorField.RegisterValueChangedCallback(evt => { cond.conditionStringValue = evt.newValue; MarkDirty(); });
                    section.Add(actorField);

                    var legacyField = new IntegerField("레거시 숫자 ID") { value = cond.conditionValue };
                    legacyField.RegisterValueChangedCallback(evt => { cond.conditionValue = evt.newValue; MarkDirty(); });
                    section.Add(legacyField);

                    section.Add(MakeMinClampedIntField("처치 횟수", cond.conditionValue2, 1, v => cond.conditionValue2 = v));
                    break;
                }

                case UnlockConditionType.ItemCollect:
                case UnlockConditionType.ItemHave:
                {
                    var hint = MakeItemHintLabel();
                    var idRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                    var idField = new IntegerField("아이템 ID") { value = cond.conditionValue, style = { flexGrow = 1 } };
                    idField.RegisterValueChangedCallback(evt =>
                    {
                        cond.conditionValue = evt.newValue;
                        UpdateItemHint(hint, evt.newValue);
                        MarkDirty();
                    });
                    idRow.Add(idField);
                    idRow.Add(new Button(() => OpenItemPicker(id =>
                    {
                        cond.conditionValue = id;
                        idField.SetValueWithoutNotify(id);
                        UpdateItemHint(hint, id);
                        MarkDirty();
                    })) { text = "선택", style = { width = 52 } });
                    section.Add(idRow);
                    section.Add(hint);
                    UpdateItemHint(hint, cond.conditionValue);

                    string qtyLabel = cond.conditionType == UnlockConditionType.ItemCollect ? "수집 수량" : "소지 수량";
                    section.Add(MakeMinClampedIntField(qtyLabel, cond.conditionValue2, 1, v => cond.conditionValue2 = v));
                    break;
                }

                case UnlockConditionType.RecipeCraft:
                {
                    var recipeHint = new Label
                    {
                        style = { color = new Color(0.4f, 1f, 0.4f), fontSize = 10, marginLeft = 18 }
                    };
                    void UpdateRecipeHint(int id)
                    {
                        var target = _recipes.FirstOrDefault(r => r.recipeID == id);
                        recipeHint.text = target != null ? $"→ {target.recipeName}" : "";
                        recipeHint.style.display = target != null ? DisplayStyle.Flex : DisplayStyle.None;
                    }
                    var idField = new IntegerField("레시피 ID") { value = cond.conditionValue };
                    idField.RegisterValueChangedCallback(evt =>
                    {
                        cond.conditionValue = evt.newValue;
                        UpdateRecipeHint(evt.newValue);
                        MarkDirty();
                    });
                    section.Add(idField);
                    section.Add(recipeHint);
                    UpdateRecipeHint(cond.conditionValue);

                    section.Add(MakeMinClampedIntField("제작 횟수", cond.conditionValue2, 1, v => cond.conditionValue2 = v));
                    break;
                }
            }
        }

        private IntegerField MakeMinClampedIntField(string label, int value, int min, Action<int> setter)
        {
            var field = new IntegerField(label) { value = value };
            field.RegisterValueChangedCallback(evt =>
            {
                int v = Mathf.Max(min, evt.newValue);
                field.SetValueWithoutNotify(v);
                setter(v);
                MarkDirty();
            });
            return field;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 아이템 피커 팝업

        private void OpenItemPicker(Action<int> callback)
        {
            CloseItemPicker();

            var popup = new VisualElement
            {
                style =
                {
                    position = Position.Absolute, right = 4, top = 26, width = 310, height = 400,
                    backgroundColor = EditorGUIUtility.isProSkin
                        ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.8f, 0.8f, 0.8f),
                    borderLeftWidth = 1, borderRightWidth = 1, borderTopWidth = 1, borderBottomWidth = 1,
                    borderLeftColor = Color.black, borderRightColor = Color.black,
                    borderTopColor = Color.black, borderBottomColor = Color.black,
                }
            };

            var header = new Toolbar();
            header.Add(new Label("아이템 선택")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, unityTextAlign = TextAnchor.MiddleLeft }
            });
            header.Add(new VisualElement { style = { flexGrow = 1 } });
            header.Add(new ToolbarButton(CloseItemPicker) { text = "✕" });
            popup.Add(header);

            if (_itemDb == null)
            {
                popup.Add(new HelpBox("ItemDatabase를 찾을 수 없습니다.\nID를 직접 입력하세요.", HelpBoxMessageType.Warning));
                _itemPickerPopup = popup;
                rootVisualElement.Add(popup);
                return;
            }

            var search = new ToolbarSearchField { style = { width = Length.Percent(98) } };
            popup.Add(search);

            var allItems = _itemDb.AllItems
                .Where(i => i != null)
                .OrderBy(i => i.itemId)
                .ToList();
            var filteredItems = new List<ItemSO>(allItems);

            var pickerList = new ListView
            {
                fixedItemHeight = 36,
                selectionType = SelectionType.None,
                style = { flexGrow = 1 },
                itemsSource = filteredItems,
                makeItem = () =>
                {
                    var row = new VisualElement
                    {
                        style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, paddingLeft = 4, paddingRight = 4 }
                    };
                    row.Add(new Image { name = "icon", scaleMode = ScaleMode.ScaleToFit, style = { width = 28, height = 28, flexShrink = 0, marginRight = 4 } });
                    var info = new VisualElement { style = { flexGrow = 1, justifyContent = Justify.Center } };
                    info.Add(new Label { name = "name", style = { unityFontStyleAndWeight = FontStyle.Bold } });
                    info.Add(new Label { name = "sub", style = { color = new Color(0.65f, 0.65f, 0.65f), fontSize = 10 } });
                    row.Add(info);
                    row.Add(new Button { name = "pick", text = "선택", style = { width = 44 } });
                    return row;
                },
            };
            pickerList.bindItem = (row, i) =>
            {
                if (i < 0 || i >= filteredItems.Count) return;
                var item = filteredItems[i];
                row.Q<Image>("icon").sprite = item.icon;
                row.Q<Label>("name").text = item.itemName;
                row.Q<Label>("sub").text = $"ID: {item.itemId}  |  {item.itemType}";
                row.Q<Button>("pick").clickable = new Clickable(() =>
                {
                    callback?.Invoke(item.itemId);
                    CloseItemPicker();
                });
            };
            popup.Add(pickerList);

            search.RegisterValueChangedCallback(evt =>
            {
                string s = evt.newValue ?? "";
                filteredItems.Clear();
                filteredItems.AddRange(allItems.Where(i =>
                    string.IsNullOrEmpty(s)
                    || i.itemName.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) >= 0
                    || i.itemId.ToString().Contains(s)));
                pickerList.RefreshItems();
            });

            _itemPickerPopup = popup;
            rootVisualElement.Add(popup);
            search.Focus();
        }

        private void CloseItemPicker()
        {
            if (_itemPickerPopup != null)
            {
                _itemPickerPopup.RemoveFromHierarchy();
                _itemPickerPopup = null;
            }
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 공통 헬퍼

        private static VisualElement MakeSection(string title)
        {
            var section = new VisualElement
            {
                style =
                {
                    marginTop = 4, paddingLeft = 8, paddingRight = 8, paddingTop = 6, paddingBottom = 6,
                    backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.08f),
                    borderTopLeftRadius = 3, borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3, borderBottomRightRadius = 3,
                    borderLeftWidth = 1, borderRightWidth = 1, borderTopWidth = 1, borderBottomWidth = 1,
                    borderLeftColor = new Color(0f, 0f, 0f, 0.25f), borderRightColor = new Color(0f, 0f, 0f, 0.25f),
                    borderTopColor = new Color(0f, 0f, 0f, 0.25f), borderBottomColor = new Color(0f, 0f, 0f, 0.25f),
                }
            };
            section.Add(new Label(title) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } });
            return section;
        }

        private static Label MakeItemHintLabel()
        {
            return new Label { style = { fontSize = 10, marginLeft = 18 } };
        }

        /// <summary> 아이템 ID 아래에 초록/빨강으로 이름 힌트 표시 </summary>
        private void UpdateItemHint(Label hint, int itemID)
        {
            if (itemID == 0)
            {
                hint.style.display = DisplayStyle.None;
                return;
            }

            hint.style.display = DisplayStyle.Flex;
            if (_itemCache.TryGetValue(itemID, out var item))
            {
                hint.text = $"→ {item.itemName}  [{item.itemType}]";
                hint.style.color = new Color(0.4f, 1f, 0.4f);
            }
            else
            {
                hint.text = $"⚠ ID {itemID} — 등록된 아이템 없음";
                hint.style.color = new Color(1f, 0.4f, 0.4f);
            }
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

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 레시피 CRUD

        private void AddNewRecipe()
        {
            int newID = _recipes.Count > 0 ? _recipes.Max(r => r.recipeID) + 1 : 1;
            var recipe = new RecipeData
            {
                recipeID        = newID,
                recipeName      = $"새 레시피 {newID}",
                description     = "",
                resultQuantity  = 1,
                castTimeSeconds = 2f,
                costType        = CostType.Free,
            };
            _recipes.Add(recipe);
            _selected = recipe;
            _isDirty = true;
            ValidateIDs();
            UpdateSaveButton();
            RefreshList();
        }

        private void DuplicateRecipe(RecipeData src)
        {
            if (src == null) return;

            int newID = _recipes.Max(r => r.recipeID) + 1;

            var copy = new RecipeData
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
            };
            _recipes.Add(copy);

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

            _selected = copy;
            _isDirty = true;
            ValidateIDs();
            UpdateSaveButton();
            RefreshList();
        }

        private void DeleteRecipe(RecipeData recipe)
        {
            if (recipe == null) return;

            if (!EditorUtility.DisplayDialog("삭제 확인",
                $"'{recipe.recipeName}' 레시피를 삭제하겠습니까?\n연결된 재료 및 언락 조건도 함께 삭제됩니다.",
                "삭제", "취소")) return;

            int id = recipe.recipeID;
            _recipes.Remove(recipe);
            _ingredients.RemoveAll(i => i.recipeID == id);
            _unlockConditions.RemoveAll(u => u.recipeID == id);

            _selected = null;
            _isDirty = true;
            ValidateIDs();
            UpdateSaveButton();
            RefreshList();
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
            UpdateSaveButton();
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
            File.WriteAllText(System.IO.Path.Combine(dir, "recipe_master.csv"), sb.ToString(), System.Text.Encoding.UTF8);

            // recipe_ingredients.csv
            sb.Clear();
            sb.AppendLine("recipeID,ingredientItemID,requiredQuantity");
            foreach (var i in _ingredients)
                sb.AppendLine($"{i.recipeID},{i.ingredientItemID},{i.requiredQuantity}");
            File.WriteAllText(System.IO.Path.Combine(dir, "recipe_ingredients.csv"), sb.ToString(), System.Text.Encoding.UTF8);

            // recipe_unlocks.csv
            sb.Clear();
            sb.AppendLine("recipeID,conditionType,conditionValue,conditionValue2,conditionStringValue");
            foreach (var u in _unlockConditions)
                sb.AppendLine($"{u.recipeID},{u.conditionType},{u.conditionValue},{u.conditionValue2},{u.conditionStringValue}");
            File.WriteAllText(System.IO.Path.Combine(dir, "recipe_unlocks.csv"), sb.ToString(), System.Text.Encoding.UTF8);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("내보내기 완료", $"CSV 3개 파일 저장:\n{dir}", "확인");
        }

        #endregion
    }
}
#endif
