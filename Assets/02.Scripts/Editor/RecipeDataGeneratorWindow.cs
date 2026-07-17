#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Crafting;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Tool.Editor;
using UPlayGround.Data.Item;

namespace UPlayGround.Data.Crafting.Editor
{
    /// <summary>
    /// ItemDatabase의 ItemSO를 기준으로 RecipeDatabase에 제작 데이터를 발급하는 에디터 윈도우.
    /// 메뉴: UPlayGround / Crafting / Recipe Data Generator
    /// </summary>
    public class RecipeDataGeneratorWindow : EditorWindow
    {
        private class IngredientDraft
        {
            public int itemID;
            public int quantity = 1;
        }

        private RecipeDatabase _recipeDb;
        private ItemDatabase _itemDb;
        private readonly Dictionary<int, ItemSO> _itemCache = new();
        private readonly List<ItemSO> _items = new();
        private readonly List<IngredientDraft> _ingredients = new();

        private ItemSO _selectedResultItem;
        private string _searchText = "";
        private ItemType? _filterType;
        private Vector2 _itemScroll;
        private Vector2 _detailScroll;

        private int _recipeID;
        private string _recipeName = "";
        private string _description = "";
        private int _resultQuantity = 1;
        private CostType _costType = CostType.Free;
        private int _costAmount;
        private float _castTimeSeconds = 2f;
        private CraftingCategory _category;
        private bool _isDebugUnlocked = true;
        private bool _overwriteExisting = true;
        private bool _generateRecipeEnum = true;

        private bool _useUnlockCondition;
        private UnlockConditionType _unlockConditionType = UnlockConditionType.None;
        private int _unlockConditionValue;
        private int _unlockConditionValue2 = 1;
        private string _unlockConditionStringValue = string.Empty;

        private bool _showItemPicker;
        private string _itemPickerSearch = "";
        private System.Action<int> _itemPickerCallback;
        private Vector2 _itemPickerScroll;

        private const float LIST_WIDTH = 310f;
        private const string RECIPE_ENUM_OUTPUT_PATH = "Assets/02.Scripts/Data/Crafting/RecipeIdType.cs";

        public static void Open()
        {
            var win = GetWindow<RecipeDataGeneratorWindow>("Recipe Data Generator");
            win.minSize = new Vector2(860f, 560f);
            win.Show();
        }

        private void OnEnable()
        {
            LoadDatabases();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_recipeDb == null || _itemDb == null)
            {
                DrawMissingDatabase();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawItemList();
            GUILayout.Box(GUIContent.none, GUILayout.Width(2), GUILayout.ExpandHeight(true));
            DrawGeneratorPanel();
            EditorGUILayout.EndHorizontal();

            if (_showItemPicker)
                DrawItemPickerPopup();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
                LoadDatabases();

            GUILayout.Space(8);

            GUI.color = _recipeDb == null ? Color.red : Color.white;
            GUILayout.Label(_recipeDb != null ? $"RecipeDB: {_recipeDb.name}" : "RecipeDB 없음", EditorStyles.miniLabel, GUILayout.Width(190));
            GUI.color = _itemDb == null ? Color.red : Color.white;
            GUILayout.Label(_itemDb != null ? $"ItemDB: {_itemDb.name}" : "ItemDB 없음", EditorStyles.miniLabel, GUILayout.Width(180));
            GUI.color = Color.white;

            GUILayout.FlexibleSpace();

            _overwriteExisting = GUILayout.Toggle(_overwriteExisting, "기존 결과물 레시피 갱신", EditorStyles.toolbarButton, GUILayout.Width(135));
            _generateRecipeEnum = GUILayout.Toggle(_generateRecipeEnum, "RecipeIdType 생성", EditorStyles.toolbarButton, GUILayout.Width(115));

            EditorGUILayout.EndHorizontal();
        }

        private void LoadDatabases()
        {
            _recipeDb = FindDatabase<RecipeDatabase>();
            _itemDb = FindDatabase<ItemDatabase>();

            _itemCache.Clear();
            _items.Clear();

            if (_itemDb != null)
            {
                _itemDb.Initialize();
                foreach (var item in _itemDb.AllItems.Where(i => i != null).OrderBy(i => i.itemId))
                {
                    _items.Add(item);
                    if (!_itemCache.ContainsKey(item.itemId))
                        _itemCache.Add(item.itemId, item);
                }
            }

            if (_selectedResultItem != null && !_items.Contains(_selectedResultItem))
                _selectedResultItem = null;
        }

        private static T FindDatabase<T>() where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0)
                return null;

            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private void DrawMissingDatabase()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox(
                "RecipeDatabase 또는 ItemDatabase를 찾을 수 없습니다.\n" +
                "RecipeDatabase는 Create > UPlayGround > PathDatabase > Recipe로 생성하고, ItemDatabase는 기존 아이템 DB를 준비하세요.",
                MessageType.Warning);
        }

        private void DrawItemList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(LIST_WIDTH));

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawTypeFilter(null, "전체");
            DrawTypeFilter(ItemType.EQUIPMENT, "장비");
            DrawTypeFilter(ItemType.CONSUMABLE, "소비");
            DrawTypeFilter(ItemType.OTHERS, "기타");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("검색:", EditorStyles.miniLabel, GUILayout.Width(34));
            _searchText = GUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(22)))
            {
                _searchText = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            _itemScroll = EditorGUILayout.BeginScrollView(_itemScroll);
            foreach (var item in GetFilteredItems())
                DrawItemRow(item);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawTypeFilter(ItemType? type, string label)
        {
            bool selected = _filterType == type;
            GUI.color = selected ? new Color(0.55f, 0.8f, 1f) : Color.white;
            if (GUILayout.Button(label, EditorStyles.toolbarButton))
                _filterType = type;
            GUI.color = Color.white;
        }

        private IEnumerable<ItemSO> GetFilteredItems()
        {
            IEnumerable<ItemSO> query = _items;

            if (_filterType.HasValue)
                query = query.Where(i => i.itemType == _filterType.Value);

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                string lower = _searchText.ToLower();
                query = query.Where(i =>
                    (!string.IsNullOrEmpty(i.itemName) && i.itemName.ToLower().Contains(lower)) ||
                    i.itemId.ToString().Contains(lower));
            }

            return query;
        }

        private void DrawItemRow(ItemSO item)
        {
            bool selected = item == _selectedResultItem;
            bool hasRecipe = FindRecipeByResultItem(item.itemId) != null;

            Rect row = EditorGUILayout.BeginHorizontal(selected ? "selectionRect" : "helpBox", GUILayout.Height(46));
            if (UnityEngine.Event.current.type == EventType.MouseDown && row.Contains(UnityEngine.Event.current.mousePosition))
            {
                SelectResultItem(item);
                UnityEngine.Event.current.Use();
            }

            Texture2D preview = item.icon != null ? AssetPreview.GetAssetPreview(item.icon) : null;
            if (preview != null)
                GUILayout.Label(preview, GUILayout.Width(38), GUILayout.Height(38));
            else
                GUILayout.Label("□", GUILayout.Width(38), GUILayout.Height(38));

            EditorGUILayout.BeginVertical();
            GUILayout.Label(string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName, EditorStyles.boldLabel);
            GUI.color = new Color(0.65f, 0.65f, 0.65f);
            GUILayout.Label($"ID: {item.itemId} | {item.itemType} | {item.itemRarity}", EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndVertical();

            if (hasRecipe)
            {
                GUI.color = new Color(0.45f, 1f, 0.5f);
                GUILayout.Label("R", EditorStyles.boldLabel, GUILayout.Width(14));
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(1);
        }

        private void SelectResultItem(ItemSO item)
        {
            _selectedResultItem = item;
            ApplyDefaultsFromItem(item);
            Repaint();
        }

        private void ApplyDefaultsFromItem(ItemSO item)
        {
            var existing = FindRecipeByResultItem(item.itemId);

            if (existing != null)
            {
                _recipeID = existing.recipeID;
                _recipeName = existing.recipeName;
                _description = existing.description;
                _resultQuantity = Mathf.Max(1, existing.resultQuantity);
                _costType = existing.costType;
                _costAmount = existing.costAmount;
                _castTimeSeconds = Mathf.Max(0f, existing.castTimeSeconds);
                _category = existing.category;
                _isDebugUnlocked = existing.isDebugUnlocked;

                _ingredients.Clear();
                foreach (var ingredient in _recipeDb.AllIngredients.Where(i => i.recipeID == existing.recipeID))
                {
                    _ingredients.Add(new IngredientDraft
                    {
                        itemID = ingredient.ingredientItemID,
                        quantity = Mathf.Max(1, ingredient.requiredQuantity)
                    });
                }

                var cond = _recipeDb.AllUnlockConditions.FirstOrDefault(u => u.recipeID == existing.recipeID);
                _useUnlockCondition = cond != null;
                if (cond != null)
                {
                    _unlockConditionType = cond.conditionType;
                    _unlockConditionValue = cond.conditionValue;
                    _unlockConditionValue2 = Mathf.Max(1, cond.conditionValue2);
                    _unlockConditionStringValue = cond.conditionStringValue;
                }
                return;
            }

            _recipeID = GetNextRecipeID();
            _recipeName = $"{item.itemName} 제작";
            _description = $"{item.itemName} 제작 레시피";
            _resultQuantity = 1;
            _category = GetCategoryFromItem(item);
            _costType = item.itemType == ItemType.EQUIPMENT ? CostType.Gold : CostType.Free;
            _costAmount = GetDefaultCost(item);
            _castTimeSeconds = GetDefaultCastTime(item);
            _isDebugUnlocked = true;
            _useUnlockCondition = false;
            _unlockConditionType = UnlockConditionType.None;
            _unlockConditionValue = 0;
            _unlockConditionValue2 = 1;
            _unlockConditionStringValue = string.Empty;

            FillSuggestedIngredients();
        }

        private void DrawGeneratorPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            if (_selectedResultItem == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("좌측에서 제작 결과 아이템을 선택하세요.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

            DrawResultHeader();
            EditorGUILayout.Space(8);
            DrawRecipeFields();
            EditorGUILayout.Space(6);
            DrawIngredientFields();
            EditorGUILayout.Space(6);
            DrawUnlockFields();
            EditorGUILayout.Space(12);
            DrawActionButtons();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawResultHeader()
        {
            EditorGUILayout.BeginHorizontal("helpBox");

            Texture2D preview = _selectedResultItem.icon != null ? AssetPreview.GetAssetPreview(_selectedResultItem.icon) : null;
            if (preview != null)
                GUILayout.Label(preview, GUILayout.Width(56), GUILayout.Height(56));
            else
                GUILayout.Label("□", GUILayout.Width(56), GUILayout.Height(56));

            EditorGUILayout.BeginVertical();
            GUILayout.Label(_selectedResultItem.itemName, EditorStyles.boldLabel);
            GUILayout.Label($"ItemID: {_selectedResultItem.itemId} | {_selectedResultItem.itemType} | {_selectedResultItem.itemRarity}", EditorStyles.miniLabel);

            var existing = FindRecipeByResultItem(_selectedResultItem.itemId);
            if (existing != null)
            {
                GUI.color = new Color(0.4f, 1f, 0.5f);
                GUILayout.Label($"기존 레시피 있음: #{existing.recipeID} {existing.recipeName}", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawRecipeFields()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("레시피 기본 데이터", EditorStyles.boldLabel);

            _recipeID = Mathf.Max(1, EditorGUILayout.IntField("레시피 ID", _recipeID));
            _recipeName = EditorGUILayout.TextField("레시피 이름", _recipeName);
            _description = EditorGUILayout.TextField("설명", _description);
            _resultQuantity = Mathf.Max(1, EditorGUILayout.IntField("결과 수량", _resultQuantity));
            _category = (CraftingCategory)EditorGUILayout.EnumPopup("카테고리", _category);
            _costType = (CostType)EditorGUILayout.EnumPopup("비용 유형", _costType);
            if (_costType == CostType.Gold)
                _costAmount = Mathf.Max(0, EditorGUILayout.IntField("골드 비용", _costAmount));
            else
                _costAmount = 0;
            _castTimeSeconds = Mathf.Max(0f, EditorGUILayout.FloatField("제작 시간", _castTimeSeconds));
            _isDebugUnlocked = EditorGUILayout.Toggle("디버그 언락", _isDebugUnlocked);

            EditorGUILayout.EndVertical();
        }

        private void DrawIngredientFields()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("필요 재료", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("추천 재료", GUILayout.Width(72)))
                FillSuggestedIngredients();
            if (GUILayout.Button("+ 추가", GUILayout.Width(56)))
                _ingredients.Add(new IngredientDraft { quantity = 1 });
            EditorGUILayout.EndHorizontal();

            if (_ingredients.Count == 0)
                EditorGUILayout.HelpBox("재료가 없으면 비용만으로 제작 가능한 레시피가 됩니다.", MessageType.Info);

            for (int i = 0; i < _ingredients.Count; i++)
            {
                var ingredient = _ingredients[i];
                EditorGUILayout.BeginHorizontal("helpBox");
                GUILayout.Label($"{i + 1}", GUILayout.Width(18));
                ingredient.itemID = EditorGUILayout.IntField("아이템 ID", ingredient.itemID);
                if (GUILayout.Button("선택", GUILayout.Width(48)))
                {
                    var captured = ingredient;
                    OpenItemPicker(id => captured.itemID = id);
                }
                ingredient.quantity = Mathf.Max(1, EditorGUILayout.IntField("수량", ingredient.quantity, GUILayout.Width(120)));
                GUI.color = new Color(1f, 0.55f, 0.55f);
                if (GUILayout.Button("x", GUILayout.Width(24)))
                {
                    _ingredients.RemoveAt(i);
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();

                DrawItemHint(ingredient.itemID);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawUnlockFields()
        {
            EditorGUILayout.BeginVertical("box");
            _useUnlockCondition = EditorGUILayout.Toggle("언락 조건 사용", _useUnlockCondition);

            if (_useUnlockCondition)
            {
                _unlockConditionType = (UnlockConditionType)EditorGUILayout.EnumPopup("조건 유형", _unlockConditionType);
                switch (_unlockConditionType)
                {
                    case UnlockConditionType.ItemCollect:
                    case UnlockConditionType.ItemHave:
                        EditorGUILayout.BeginHorizontal();
                        _unlockConditionValue = EditorGUILayout.IntField("조건 아이템 ID", _unlockConditionValue);
                        if (GUILayout.Button("선택", GUILayout.Width(48)))
                            OpenItemPicker(id => _unlockConditionValue = id);
                        EditorGUILayout.EndHorizontal();
                        DrawItemHint(_unlockConditionValue);
                        _unlockConditionValue2 = Mathf.Max(1, EditorGUILayout.IntField("필요 수량", _unlockConditionValue2));
                        break;
                    case UnlockConditionType.RecipeCraft:
                        _unlockConditionValue = Mathf.Max(1, EditorGUILayout.IntField("조건 레시피 ID", _unlockConditionValue));
                        _unlockConditionValue2 = Mathf.Max(1, EditorGUILayout.IntField("제작 횟수", _unlockConditionValue2));
                        break;
                    case UnlockConditionType.MonsterKill:
                        _unlockConditionStringValue = EditorGUILayout.TextField("Actor ID", _unlockConditionStringValue);
                        _unlockConditionValue = EditorGUILayout.IntField("레거시 숫자 ID", _unlockConditionValue);
                        _unlockConditionValue2 = Mathf.Max(1, EditorGUILayout.IntField("처치 횟수", _unlockConditionValue2));
                        break;
                    default:
                        _unlockConditionValue = 0;
                        _unlockConditionValue2 = 1;
                        break;
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            string validation = GetValidationMessage();
            if (!string.IsNullOrEmpty(validation))
                EditorGUILayout.HelpBox(validation, MessageType.Warning);

            using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(validation)))
            {
                if (GUILayout.Button("선택 아이템 제작 데이터 생성/갱신", GUILayout.Height(36)))
                    SaveRecipe();
            }

            EditorGUILayout.Space(4);

            if (GUILayout.Button("현재 설정으로 같은 타입 누락 아이템 일괄 생성", GUILayout.Height(26)))
                GenerateMissingRecipesForCurrentFilter();
        }

        private string GetValidationMessage()
        {
            if (_selectedResultItem == null)
                return "결과 아이템이 선택되지 않았습니다.";
            if (string.IsNullOrWhiteSpace(_recipeName))
                return "레시피 이름이 비어 있습니다.";

            var sameIdRecipe = _recipeDb.AllRecipes.FirstOrDefault(r => r.recipeID == _recipeID);
            var existingByResult = FindRecipeByResultItem(_selectedResultItem.itemId);
            if (sameIdRecipe != null && (existingByResult == null || sameIdRecipe.recipeID != existingByResult.recipeID))
                return $"레시피 ID {_recipeID}가 이미 사용 중입니다.";

            if (existingByResult != null && !_overwriteExisting)
                return $"결과 아이템 {_selectedResultItem.itemName}의 기존 레시피가 있습니다. 갱신 옵션을 켜거나 다른 아이템을 선택하세요.";

            foreach (var ingredient in _ingredients)
            {
                if (ingredient.itemID <= 0)
                    return "재료 아이템 ID가 비어 있습니다.";
                if (!_itemCache.ContainsKey(ingredient.itemID))
                    return $"재료 아이템 ID {ingredient.itemID}를 ItemDatabase에서 찾을 수 없습니다.";
            }

            return "";
        }

        private void SaveRecipe()
        {
            if (_ingredients.Count == 0 && !EditorUtility.DisplayDialog(
                    "재료 없는 레시피",
                    "필요 재료가 없는 제작 데이터를 생성합니다. 계속할까요?",
                    "생성", "취소"))
                return;

            var recipes = _recipeDb.AllRecipes.Select(CloneRecipe).ToList();
            var ingredients = _recipeDb.AllIngredients.Select(CloneIngredient).ToList();
            var unlocks = _recipeDb.AllUnlockConditions.Select(CloneUnlock).ToList();

            var existing = recipes.FirstOrDefault(r => r.resultItemID == _selectedResultItem.itemId);
            int targetRecipeID = existing != null && _overwriteExisting ? existing.recipeID : _recipeID;

            if (existing != null && _overwriteExisting)
                recipes.Remove(existing);

            ingredients.RemoveAll(i => i.recipeID == targetRecipeID);
            unlocks.RemoveAll(u => u.recipeID == targetRecipeID);

            recipes.Add(new RecipeData
            {
                recipeID = targetRecipeID,
                recipeName = _recipeName,
                description = _description,
                resultItemID = _selectedResultItem.itemId,
                resultQuantity = _resultQuantity,
                costType = _costType,
                costAmount = _costAmount,
                castTimeSeconds = _castTimeSeconds,
                category = _category,
                isDebugUnlocked = _isDebugUnlocked,
            });

            foreach (var ingredient in _ingredients)
            {
                ingredients.Add(new IngredientData
                {
                    recipeID = targetRecipeID,
                    ingredientItemID = ingredient.itemID,
                    requiredQuantity = ingredient.quantity,
                });
            }

            if (_useUnlockCondition)
            {
                unlocks.Add(new RecipeUnlockCondition
                {
                    recipeID = targetRecipeID,
                    conditionType = _unlockConditionType,
                    conditionValue = _unlockConditionValue,
                    conditionValue2 = _unlockConditionValue2,
                    conditionStringValue = _unlockConditionType == UnlockConditionType.MonsterKill
                        ? _unlockConditionStringValue
                        : string.Empty,
                });
            }

            SaveDatabase(recipes, ingredients, unlocks);
            _recipeID = targetRecipeID;

            EditorUtility.DisplayDialog("완료", $"제작 데이터 저장 완료\n레시피 ID: {targetRecipeID}", "확인");
        }

        private void GenerateMissingRecipesForCurrentFilter()
        {
            var targets = GetFilteredItems()
                .Where(item => FindRecipeByResultItem(item.itemId) == null)
                .ToList();

            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog("생성 대상 없음", "현재 필터에서 레시피가 없는 아이템이 없습니다.", "확인");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "일괄 생성",
                    $"현재 필터의 누락 아이템 {targets.Count}개에 제작 데이터를 생성합니다.\n" +
                    "재료는 각 아이템 타입/희귀도 기준 추천 재료로 채웁니다.",
                    "생성", "취소"))
                return;

            var recipes = _recipeDb.AllRecipes.Select(CloneRecipe).ToList();
            var ingredients = _recipeDb.AllIngredients.Select(CloneIngredient).ToList();
            var unlocks = _recipeDb.AllUnlockConditions.Select(CloneUnlock).ToList();
            int nextId = recipes.Count > 0 ? recipes.Max(r => r.recipeID) + 1 : 1;

            foreach (var item in targets)
            {
                var recipe = new RecipeData
                {
                    recipeID = nextId++,
                    recipeName = $"{item.itemName} 제작",
                    description = $"{item.itemName} 제작 레시피",
                    resultItemID = item.itemId,
                    resultQuantity = 1,
                    costType = item.itemType == ItemType.EQUIPMENT ? CostType.Gold : CostType.Free,
                    costAmount = GetDefaultCost(item),
                    castTimeSeconds = GetDefaultCastTime(item),
                    category = GetCategoryFromItem(item),
                    isDebugUnlocked = _isDebugUnlocked,
                };
                recipes.Add(recipe);

                foreach (var draft in BuildSuggestedIngredients(item))
                {
                    ingredients.Add(new IngredientData
                    {
                        recipeID = recipe.recipeID,
                        ingredientItemID = draft.itemID,
                        requiredQuantity = draft.quantity,
                    });
                }
            }

            SaveDatabase(recipes, ingredients, unlocks);
            EditorUtility.DisplayDialog("완료", $"제작 데이터 {targets.Count}개 생성 완료", "확인");
        }

        private void SaveDatabase(List<RecipeData> recipes, List<IngredientData> ingredients, List<RecipeUnlockCondition> unlocks)
        {
            _recipeDb.SetRecipes(recipes.OrderBy(r => r.recipeID).ToList());
            _recipeDb.SetIngredients(ingredients.OrderBy(i => i.recipeID).ThenBy(i => i.ingredientItemID).ToList());
            _recipeDb.SetUnlockConditions(unlocks.OrderBy(u => u.recipeID).ToList());
            EditorUtility.SetDirty(_recipeDb);
            AssetDatabase.SaveAssets();

            if (_generateRecipeEnum)
                GenerateRecipeIdType(_recipeDb.AllRecipes);

            AssetDatabase.Refresh();
        }

        private static void GenerateRecipeIdType(IReadOnlyList<RecipeData> recipes)
        {
            var raw = recipes
                .Where(r => r != null)
                .OrderBy(r => r.recipeID)
                .Select(r =>
                {
                    string name = string.IsNullOrEmpty(r.recipeName) ? $"Recipe_{r.recipeID}" : r.recipeName;
                    return (name, r.recipeID);
                });

            var entries = IdEnumGeneratorUtility.DeduplicateEntries(raw);
            IdEnumGeneratorUtility.GenerateIntKeyEnum(
                "RecipeIdType",
                "ToRecipeId",
                "Recipe",
                RECIPE_ENUM_OUTPUT_PATH,
                "UPlayGround.Data.Crafting",
                entries,
                silent: true);
        }

        private void FillSuggestedIngredients()
        {
            _ingredients.Clear();
            if (_selectedResultItem == null)
                return;

            _ingredients.AddRange(BuildSuggestedIngredients(_selectedResultItem));
        }

        private List<IngredientDraft> BuildSuggestedIngredients(ItemSO resultItem)
        {
            var materials = _items
                .Where(i => i != null && i.itemType == ItemType.OTHERS && i.itemId != resultItem.itemId)
                .OrderBy(i => i.itemId)
                .ToList();

            var result = new List<IngredientDraft>();
            if (materials.Count == 0)
                return result;

            int rarityMul = Mathf.Max(1, (int)resultItem.itemRarity);
            if (resultItem.itemType == ItemType.EQUIPMENT)
            {
                ItemSO primary = PickMaterial(materials, "가죽") ?? PickMaterial(materials, "장작") ?? materials[0];
                result.Add(new IngredientDraft { itemID = primary.itemId, quantity = 2 + rarityMul });

                ItemSO secondary = PickMaterial(materials, "수정") ?? PickMaterial(materials, "몬스터") ?? materials.FirstOrDefault(i => i.itemId != primary.itemId);
                if (secondary != null)
                    result.Add(new IngredientDraft { itemID = secondary.itemId, quantity = Mathf.Max(1, rarityMul) });
            }
            else if (resultItem.itemType == ItemType.CONSUMABLE)
            {
                result.Add(new IngredientDraft { itemID = materials[0].itemId, quantity = Mathf.Max(1, rarityMul) });
            }
            else
            {
                ItemSO source = materials.FirstOrDefault(i => i.itemId != resultItem.itemId);
                if (source != null)
                    result.Add(new IngredientDraft { itemID = source.itemId, quantity = 2 });
            }

            return result;
        }

        private static ItemSO PickMaterial(IEnumerable<ItemSO> materials, string keyword)
        {
            return materials.FirstOrDefault(i => !string.IsNullOrEmpty(i.itemName) && i.itemName.Contains(keyword));
        }

        private RecipeData FindRecipeByResultItem(int itemID)
        {
            return _recipeDb == null ? null : _recipeDb.AllRecipes.FirstOrDefault(r => r.resultItemID == itemID);
        }

        private int GetNextRecipeID()
        {
            return _recipeDb != null && _recipeDb.AllRecipes.Count > 0
                ? _recipeDb.AllRecipes.Max(r => r.recipeID) + 1
                : 1;
        }

        private static CraftingCategory GetCategoryFromItem(ItemSO item)
        {
            return item.itemType switch
            {
                ItemType.CONSUMABLE => CraftingCategory.Consumable,
                ItemType.EQUIPMENT => CraftingCategory.Equipment,
                ItemType.OTHERS => CraftingCategory.Material,
                _ => CraftingCategory.Special,
            };
        }

        private static int GetDefaultCost(ItemSO item)
        {
            if (item.itemType != ItemType.EQUIPMENT)
                return 0;

            return Mathf.Max(100, (int)item.itemRarity * 100);
        }

        private static float GetDefaultCastTime(ItemSO item)
        {
            return item.itemType switch
            {
                ItemType.EQUIPMENT => 3f + Mathf.Max(0, (int)item.itemRarity - 1),
                ItemType.CONSUMABLE => 1f,
                ItemType.OTHERS => 1.5f,
                _ => 2f,
            };
        }

        private void DrawItemHint(int itemID)
        {
            if (itemID <= 0)
                return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(24);
            if (_itemCache.TryGetValue(itemID, out var item))
            {
                GUI.color = new Color(0.45f, 1f, 0.5f);
                GUILayout.Label($"→ {item.itemName} [{item.itemType}]", EditorStyles.miniLabel);
            }
            else
            {
                GUI.color = new Color(1f, 0.45f, 0.45f);
                GUILayout.Label($"등록된 아이템 없음: {itemID}", EditorStyles.miniLabel);
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void OpenItemPicker(System.Action<int> callback)
        {
            _showItemPicker = true;
            _itemPickerSearch = "";
            _itemPickerCallback = callback;
            _itemPickerScroll = Vector2.zero;
        }

        private void DrawItemPickerPopup()
        {
            float width = 330f;
            float height = 420f;
            Rect rect = new Rect(position.width - width - 8f, 28f, width, height);

            if (UnityEngine.Event.current.type == EventType.MouseDown && !rect.Contains(UnityEngine.Event.current.mousePosition))
            {
                _showItemPicker = false;
                Repaint();
                return;
            }

            GUI.Box(rect, GUIContent.none, "window");
            GUILayout.BeginArea(rect);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("아이템 선택", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(22)))
            {
                _showItemPicker = false;
                EditorGUILayout.EndHorizontal();
                GUILayout.EndArea();
                return;
            }
            EditorGUILayout.EndHorizontal();

            _itemPickerSearch = EditorGUILayout.TextField(_itemPickerSearch, EditorStyles.toolbarSearchField);
            _itemPickerScroll = EditorGUILayout.BeginScrollView(_itemPickerScroll);

            foreach (var item in _items.Where(MatchesPickerSearch))
            {
                EditorGUILayout.BeginHorizontal("helpBox");
                EditorGUILayout.BeginVertical();
                GUILayout.Label(item.itemName, EditorStyles.boldLabel);
                GUILayout.Label($"ID: {item.itemId} | {item.itemType}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                if (GUILayout.Button("선택", GUILayout.Width(50), GUILayout.Height(30)))
                {
                    _itemPickerCallback?.Invoke(item.itemId);
                    _showItemPicker = false;
                    Repaint();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private bool MatchesPickerSearch(ItemSO item)
        {
            if (string.IsNullOrWhiteSpace(_itemPickerSearch))
                return true;

            string lower = _itemPickerSearch.ToLower();
            return item.itemId.ToString().Contains(lower)
                   || (!string.IsNullOrEmpty(item.itemName) && item.itemName.ToLower().Contains(lower));
        }

        private static RecipeData CloneRecipe(RecipeData src)
        {
            return new RecipeData
            {
                recipeID = src.recipeID,
                recipeName = src.recipeName,
                description = src.description,
                resultItemID = src.resultItemID,
                resultQuantity = src.resultQuantity,
                costType = src.costType,
                costAmount = src.costAmount,
                castTimeSeconds = src.castTimeSeconds,
                category = src.category,
                isDebugUnlocked = src.isDebugUnlocked,
            };
        }

        private static IngredientData CloneIngredient(IngredientData src)
        {
            return new IngredientData
            {
                recipeID = src.recipeID,
                ingredientItemID = src.ingredientItemID,
                requiredQuantity = src.requiredQuantity,
            };
        }

        private static RecipeUnlockCondition CloneUnlock(RecipeUnlockCondition src)
        {
            return new RecipeUnlockCondition
            {
                recipeID = src.recipeID,
                conditionType = src.conditionType,
                conditionValue = src.conditionValue,
                conditionValue2 = src.conditionValue2,
                conditionStringValue = src.conditionStringValue,
            };
        }
    }
    #endif
}
