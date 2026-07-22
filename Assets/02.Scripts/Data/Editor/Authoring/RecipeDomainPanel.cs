#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Crafting;
using UPlayGround.Data.Item;
using UPlayGround.Data.Path;
using IOPath = System.IO.Path;

namespace UPlayGround.Data.Editor.Authoring
{
    [InitializeOnLoad]
    internal static class RecipeDomainRegistration
    {
        static RecipeDomainRegistration()
        {
            DataAuthoringDomainRegistry.Register(
                RecipeDomainPanel.DomainKey,
                "제작",
                () => new RecipeDomainPanel(),
                300);
        }
    }

    /// <summary>
    /// RecipeDatabase 내부의 레시피·재료·언락 조건을 하나의 작업 복사본으로 편집합니다.
    /// </summary>
    public sealed partial class RecipeDomainPanel : DataDomainPanel<RecipeData>, IDataDomainUnsavedChanges
    {
        public const string DomainKey = "recipes";
        private const string ImportMenuPath = "UPlayGround/게임플레이/제작/레시피 데이터 가져오기";

        private RecipeDatabase _database;
        private readonly List<RecipeData> _recipes = new List<RecipeData>();
        private readonly List<IngredientData> _ingredients = new List<IngredientData>();
        private readonly List<RecipeUnlockCondition> _unlockConditions = new List<RecipeUnlockCondition>();
        private readonly Dictionary<int, ItemSO> _itemCache = new Dictionary<int, ItemSO>();
        private bool _workingCopyLoaded;
        private bool _isDirty;
        private ToolbarButton _saveButton;
        private ToolbarMenu _actionsMenu;

        public bool HasUnsavedChanges => _isDirty;
        public event Action UnsavedChangesChanged;

        public override string DomainId => DomainKey;
        public override string DisplayName => "제작";
        public override Texture2D Icon => EditorGUIUtility.IconContent("d_Toolbar Plus").image as Texture2D;
        protected override float ListPanelWidth => 310f;
        protected override string CreateButtonLabel => "+ 새 레시피";
        protected override bool CanCreate => true;
        protected override bool CanDuplicate(RecipeData asset) => asset != null;
        protected override bool CanDelete(RecipeData asset) => asset != null;

        protected override IEnumerable<RecipeData> LoadAssets()
        {
            if (!_workingCopyLoaded)
            {
                FindDefaultDatabase();
                LoadItemCache();
                LoadWorkingCopy();
            }

            return _recipes.OrderBy(recipe => recipe.recipeID);
        }

        protected override string KeyOf(RecipeData asset) => asset?.recipeID.ToString();

        protected override string LabelOf(RecipeData asset)
        {
            if (asset == null)
                return string.Empty;

            int ingredientCount = _ingredients.Count(row => row.recipeID == asset.recipeID);
            string name = string.IsNullOrWhiteSpace(asset.recipeName) ? "(이름 없음)" : asset.recipeName;
            return $"{name}  ·  {asset.recipeID}  ·  {CategoryName(asset.category)}  ·  재료 {ingredientCount}";
        }

        protected override Sprite IconOf(RecipeData asset)
        {
            return asset != null && _itemCache.TryGetValue(asset.resultItemID, out ItemSO item)
                ? item.icon
                : null;
        }

        protected override IEnumerable<DataDomainFilter<RecipeData>> CreateFilters()
        {
            yield return new DataDomainFilter<RecipeData>("소비", recipe => recipe.category == CraftingCategory.Consumable);
            yield return new DataDomainFilter<RecipeData>("장비", recipe => recipe.category == CraftingCategory.Equipment);
            yield return new DataDomainFilter<RecipeData>("재료", recipe => recipe.category == CraftingCategory.Material);
            yield return new DataDomainFilter<RecipeData>("특수", recipe => recipe.category == CraftingCategory.Special);
        }

        protected override IEnumerable<DataAuthoringIssue> GetIssues(RecipeData recipe)
        {
            if (HasDuplicateKey(recipe))
            {
                yield return new DataAuthoringIssue(
                    DataAuthoringIssueSeverity.Error,
                    $"레시피 ID {recipe.recipeID}가 중복됩니다.",
                    _database);
            }

            if (recipe.resultItemID == 0 || !_itemCache.ContainsKey(recipe.resultItemID))
            {
                yield return new DataAuthoringIssue(
                    DataAuthoringIssueSeverity.Error,
                    $"결과 아이템 ID {recipe.resultItemID}를 찾을 수 없습니다.",
                    _database);
            }

            foreach (IngredientData ingredient in _ingredients.Where(row => row.recipeID == recipe.recipeID))
            {
                if (ingredient.ingredientItemID == 0 || !_itemCache.ContainsKey(ingredient.ingredientItemID))
                {
                    yield return new DataAuthoringIssue(
                        DataAuthoringIssueSeverity.Error,
                        $"재료 아이템 ID {ingredient.ingredientItemID}를 찾을 수 없습니다.",
                        _database);
                }
                if (ingredient.requiredQuantity <= 0)
                {
                    yield return new DataAuthoringIssue(
                        DataAuthoringIssueSeverity.Warning,
                        $"재료 {ingredient.ingredientItemID}의 필요 수량이 0 이하입니다.",
                        _database);
                }
            }
        }

        protected override void AddToolbarActions(Toolbar toolbar)
        {
            _saveButton = new ToolbarButton(SaveDatabase) { text = "저장됨" };
            toolbar.Add(_saveButton);

            _actionsMenu = new ToolbarMenu { text = "제작 작업" };
            _actionsMenu.menu.AppendAction("RecipeDatabase 선택...", _ => SelectDatabase());
            _actionsMenu.menu.AppendAction("새 RecipeDatabase 생성...", _ => CreateDatabase());
            _actionsMenu.menu.AppendAction("현재 DB 프로젝트에서 선택", _ => Selection.activeObject = _database,
                _ => _database != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            _actionsMenu.menu.AppendSeparator();
            _actionsMenu.menu.AppendAction("CSV 내보내기...", _ => ExportCsv());
            _actionsMenu.menu.AppendAction("CSV 가져오기...", _ => ExecuteMenu(ImportMenuPath));
            _actionsMenu.menu.AppendAction("레시피 데이터 생성기...", _ => DataAuthoringToolBridge.Execute(
                DataAuthoringToolBridge.RecipeGenerator,
                "레시피 데이터 생성기"));
            _actionsMenu.menu.AppendSeparator();
            _actionsMenu.menu.AppendAction("DB에서 다시 불러오기", _ => ReloadFromDatabase());
            toolbar.Add(_actionsMenu);
            UpdateToolbarState();
        }

        protected override void CreateNew()
        {
            if (!EnsureDatabase())
                return;

            int nextId = _recipes.Count == 0 ? 1 : _recipes.Max(recipe => recipe.recipeID) + 1;
            var recipe = new RecipeData
            {
                recipeID = nextId,
                recipeName = $"새 레시피 {nextId}",
                description = string.Empty,
                resultQuantity = 1,
                castTimeSeconds = 2f,
                costType = CostType.Free
            };
            _recipes.Add(recipe);
            MarkDirty(recipe);
            RefreshAssets(recipe);
        }

        protected override RecipeData Duplicate(RecipeData source)
        {
            int nextId = _recipes.Count == 0 ? 1 : _recipes.Max(recipe => recipe.recipeID) + 1;
            var copy = new RecipeData
            {
                recipeID = nextId,
                recipeName = (source.recipeName ?? string.Empty) + " (복사)",
                description = source.description,
                resultItemID = source.resultItemID,
                resultQuantity = source.resultQuantity,
                costType = source.costType,
                costAmount = source.costAmount,
                castTimeSeconds = source.castTimeSeconds,
                category = source.category,
                isDebugUnlocked = source.isDebugUnlocked
            };
            _recipes.Add(copy);

            foreach (IngredientData ingredient in _ingredients.Where(row => row.recipeID == source.recipeID).ToList())
            {
                _ingredients.Add(new IngredientData
                {
                    recipeID = nextId,
                    ingredientItemID = ingredient.ingredientItemID,
                    requiredQuantity = ingredient.requiredQuantity
                });
            }

            MarkDirty(copy);
            return copy;
        }

        protected override bool Delete(RecipeData recipe)
        {
            if (!EditorUtility.DisplayDialog(
                    "레시피 삭제",
                    $"'{recipe.recipeName}' 레시피와 연결된 재료·언락 조건을 삭제할까요?",
                    "삭제",
                    "취소"))
            {
                return false;
            }

            int id = recipe.recipeID;
            _recipes.Remove(recipe);
            _ingredients.RemoveAll(row => row.recipeID == id);
            _unlockConditions.RemoveAll(row => row.recipeID == id);
            SetDirtyState(true);
            return true;
        }

        public override void OnReload()
        {
            if (!_isDirty)
            {
                _workingCopyLoaded = false;
                RefreshAssets();
            }
        }

        private void FindDefaultDatabase()
        {
            if (_database != null)
                return;

            string guid = AssetDatabase.FindAssets("t:RecipeDatabase").FirstOrDefault();
            if (!string.IsNullOrEmpty(guid))
                _database = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private void LoadItemCache()
        {
            _itemCache.Clear();
            foreach (ItemSO item in AssetDatabase.FindAssets("t:ItemSO")
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .Select(AssetDatabase.LoadAssetAtPath<ItemSO>)
                         .Where(item => item != null))
            {
                if (!_itemCache.ContainsKey(item.itemId))
                    _itemCache.Add(item.itemId, item);
            }
        }

        private void LoadWorkingCopy()
        {
            _recipes.Clear();
            _ingredients.Clear();
            _unlockConditions.Clear();

            if (_database != null)
            {
                _recipes.AddRange(_database.AllRecipes.Where(row => row != null).Select(Clone));
                _ingredients.AddRange(_database.AllIngredients.Where(row => row != null).Select(Clone));
                _unlockConditions.AddRange(_database.AllUnlockConditions.Where(row => row != null).Select(Clone));
            }

            _workingCopyLoaded = true;
            SetDirtyState(false);
        }

        private void SaveDatabase()
        {
            if (!EnsureDatabase())
                return;

            Undo.RecordObject(_database, "레시피 데이터 저장");
            _database.SetRecipes(_recipes.Select(Clone).ToList());
            _database.SetIngredients(_ingredients.Select(Clone).ToList());
            _database.SetUnlockConditions(_unlockConditions.Select(Clone).ToList());
            EditorUtility.SetDirty(_database);
            AssetDatabase.SaveAssets();
            SetDirtyState(false);
            Debug.Log($"[RecipeDomainPanel] 저장 완료 — 레시피 {_recipes.Count}개 / 재료 {_ingredients.Count}개 / 언락 {_unlockConditions.Count}개", _database);
        }

        bool IDataDomainUnsavedChanges.SaveChanges()
        {
            SaveDatabase();
            return !_isDirty;
        }

        void IDataDomainUnsavedChanges.DiscardChanges()
        {
            LoadWorkingCopy();
            RefreshAssets();
        }

        private void ReloadFromDatabase()
        {
            if (_isDirty && !EditorUtility.DisplayDialog("변경 취소", "저장하지 않은 제작 데이터 변경을 버리고 DB에서 다시 불러올까요?", "다시 불러오기", "취소"))
                return;

            LoadWorkingCopy();
            RefreshAssets();
        }

        private bool EnsureDatabase()
        {
            if (_database != null)
                return true;

            CreateDatabase();
            return _database != null;
        }

        private void SelectDatabase()
        {
            if (!ConfirmDiscardChanges())
                return;

            string absolutePath = EditorUtility.OpenFilePanel("RecipeDatabase 선택", Application.dataPath, "asset");
            if (string.IsNullOrEmpty(absolutePath))
                return;
            if (!absolutePath.Replace('\\', '/').StartsWith(Application.dataPath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("선택 오류", "프로젝트 Assets 폴더 안의 RecipeDatabase를 선택하세요.", "확인");
                return;
            }

            string assetPath = "Assets" + absolutePath.Substring(Application.dataPath.Length).Replace('\\', '/');
            RecipeDatabase selected = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(assetPath);
            if (selected == null)
            {
                EditorUtility.DisplayDialog("선택 오류", "선택한 에셋은 RecipeDatabase가 아닙니다.", "확인");
                return;
            }

            _database = selected;
            LoadWorkingCopy();
            RefreshAssets();
        }

        private void CreateDatabase()
        {
            if (!ConfirmDiscardChanges())
                return;

            string path = EditorUtility.SaveFilePanelInProject("새 RecipeDatabase 생성", "RecipeDatabase", "asset", "저장 위치를 선택하세요.");
            if (string.IsNullOrEmpty(path))
                return;

            var database = ScriptableObject.CreateInstance<RecipeDatabase>();
            AssetDatabase.CreateAsset(database, path);
            AssetDatabase.SaveAssets();
            _database = database;
            LoadWorkingCopy();
            RefreshAssets();
            Selection.activeObject = database;
        }

        private bool ConfirmDiscardChanges()
        {
            return !_isDirty || EditorUtility.DisplayDialog(
                "저장하지 않은 변경",
                "현재 제작 데이터 변경을 버리고 다른 DB를 열까요?",
                "변경 버리기",
                "취소");
        }

        private void MarkDirty(RecipeData recipe)
        {
            SetDirtyState(true);
            NotifyAssetChanged(recipe);
        }

        private void SetDirtyState(bool dirty)
        {
            if (_isDirty == dirty)
            {
                UpdateToolbarState();
                return;
            }

            _isDirty = dirty;
            UpdateToolbarState();
            UnsavedChangesChanged?.Invoke();
        }

        private void UpdateToolbarState()
        {
            if (_saveButton != null)
            {
                _saveButton.text = _isDirty ? "● 저장" : "저장됨";
                _saveButton.SetEnabled(_database != null && _isDirty);
                _saveButton.style.color = _isDirty ? new StyleColor(new Color(1f, 0.65f, 0f)) : StyleKeyword.Null;
            }

            if (_actionsMenu != null)
                _actionsMenu.text = _database != null ? $"제작 작업 · {_database.name}" : "제작 작업 · DB 없음";
        }

        private void ExportCsv()
        {
            string directory = EditorUtility.SaveFolderPanel("CSV 내보낼 폴더 선택", Application.dataPath, string.Empty);
            if (string.IsNullOrEmpty(directory))
                return;

            var builder = new StringBuilder();
            builder.AppendLine("recipeID,recipeName,resultItemID,resultQuantity,costType,costAmount,castTimeSeconds,category,description,isDebugUnlocked");
            foreach (RecipeData recipe in _recipes)
                builder.AppendLine($"{recipe.recipeID},{Csv(recipe.recipeName)},{recipe.resultItemID},{recipe.resultQuantity},{recipe.costType},{recipe.costAmount},{recipe.castTimeSeconds},{recipe.category},{Csv(recipe.description)},{recipe.isDebugUnlocked.ToString().ToUpperInvariant()}");
            File.WriteAllText(IOPath.Combine(directory, "recipe_master.csv"), builder.ToString(), Encoding.UTF8);

            builder.Clear();
            builder.AppendLine("recipeID,ingredientItemID,requiredQuantity");
            foreach (IngredientData ingredient in _ingredients)
                builder.AppendLine($"{ingredient.recipeID},{ingredient.ingredientItemID},{ingredient.requiredQuantity}");
            File.WriteAllText(IOPath.Combine(directory, "recipe_ingredients.csv"), builder.ToString(), Encoding.UTF8);

            builder.Clear();
            builder.AppendLine("recipeID,conditionType,conditionValue,conditionValue2,conditionStringValue");
            foreach (RecipeUnlockCondition condition in _unlockConditions)
                builder.AppendLine($"{condition.recipeID},{condition.conditionType},{condition.conditionValue},{condition.conditionValue2},{Csv(condition.conditionStringValue)}");
            File.WriteAllText(IOPath.Combine(directory, "recipe_unlocks.csv"), builder.ToString(), Encoding.UTF8);

            EditorUtility.DisplayDialog("내보내기 완료", $"CSV 3개 파일을 저장했습니다.\n{directory}", "확인");
        }

        private static string Csv(string value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";

        private static void ExecuteMenu(string menuPath)
        {
            if (!EditorApplication.ExecuteMenuItem(menuPath))
                EditorUtility.DisplayDialog("도구 열기 실패", $"메뉴를 실행하지 못했습니다.\n{menuPath}", "확인");
        }

        private static RecipeData Clone(RecipeData value) => new RecipeData
        {
            recipeID = value.recipeID,
            recipeName = value.recipeName,
            description = value.description,
            resultItemID = value.resultItemID,
            resultQuantity = value.resultQuantity,
            costType = value.costType,
            costAmount = value.costAmount,
            castTimeSeconds = value.castTimeSeconds,
            category = value.category,
            isDebugUnlocked = value.isDebugUnlocked
        };

        private static IngredientData Clone(IngredientData value) => new IngredientData
        {
            recipeID = value.recipeID,
            ingredientItemID = value.ingredientItemID,
            requiredQuantity = value.requiredQuantity
        };

        private static RecipeUnlockCondition Clone(RecipeUnlockCondition value) => new RecipeUnlockCondition
        {
            recipeID = value.recipeID,
            conditionType = value.conditionType,
            conditionValue = value.conditionValue,
            conditionValue2 = value.conditionValue2,
            conditionStringValue = value.conditionStringValue
        };

        private static string CategoryName(CraftingCategory category) => category switch
        {
            CraftingCategory.Consumable => "소비",
            CraftingCategory.Equipment => "장비",
            CraftingCategory.Material => "재료",
            CraftingCategory.Special => "특수",
            _ => "기타"
        };
    }
}
#endif
