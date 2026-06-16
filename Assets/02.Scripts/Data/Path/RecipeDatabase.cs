using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UPlayGround.Data.Crafting;

namespace UPlayGround.Data.Path
{
    /// <summary>
    /// 레시피 ScriptableObject 데이터베이스.
    /// ItemDatabase와 동일한 방식으로 Addressables를 통해 로드된다.
    /// Addressable 키: "RecipeDatabase"
    /// </summary>
    [CreateAssetMenu(fileName = "RecipeDatabase", menuName = "UPlayGround/PathDatabase/Recipe")]
    public class RecipeDatabase : ScriptableObject
    {
        [SerializeField] private List<RecipeData>            recipes          = new List<RecipeData>();
        [SerializeField] private List<IngredientData>        ingredients      = new List<IngredientData>();
        [SerializeField] private List<RecipeUnlockCondition> unlockConditions = new List<RecipeUnlockCondition>();

        // 런타임 캐시
        private Dictionary<int, RecipeData>             _recipeDict;
        private Dictionary<int, List<IngredientData>>   _ingredientsDict;
        private Dictionary<int, RecipeUnlockCondition>  _unlockDict;

        public IReadOnlyList<RecipeData>            AllRecipes          => recipes;
        public IReadOnlyList<IngredientData>        AllIngredients      => ingredients;
        public IReadOnlyList<RecipeUnlockCondition> AllUnlockConditions => unlockConditions;

        /// <summary> 런타임 딕셔너리 캐시 빌드. RecipeManager.Init()에서 호출. </summary>
        public void Initialize()
        {
            _recipeDict = new Dictionary<int, RecipeData>();
            foreach (var recipe in recipes)
            {
                if (recipe == null)
                    continue;

                if (_recipeDict.ContainsKey(recipe.recipeID))
                {
                    Debug.LogWarning($"[RecipeDatabase] 중복 recipeID 발견: {recipe.recipeID}. 첫 번째 레시피를 사용합니다.", this);
                    continue;
                }

                _recipeDict.Add(recipe.recipeID, recipe);
            }

            _ingredientsDict = new Dictionary<int, List<IngredientData>>();
            foreach (var ingr in ingredients)
            {
                if (!_ingredientsDict.ContainsKey(ingr.recipeID))
                    _ingredientsDict[ingr.recipeID] = new List<IngredientData>();
                _ingredientsDict[ingr.recipeID].Add(ingr);
            }

            _unlockDict = new Dictionary<int, RecipeUnlockCondition>();
            foreach (var cond in unlockConditions)
            {
                // 같은 recipeID가 여럿이면 첫 번째만 사용
                if (!_unlockDict.ContainsKey(cond.recipeID))
                    _unlockDict[cond.recipeID] = cond;
            }

            Debug.Log($"[RecipeDatabase] 초기화 완료 — 레시피 {_recipeDict.Count}개");
        }

        public RecipeData                GetRecipe(RecipeIdType recipeID)          => GetRecipe((int)recipeID);
        public List<IngredientData>     GetIngredients(RecipeIdType recipeID)     => GetIngredients((int)recipeID);
        public RecipeUnlockCondition    GetUnlockCondition(RecipeIdType recipeID) => GetUnlockCondition((int)recipeID);

        public RecipeData GetRecipe(int recipeID)
        {
            return _recipeDict != null && _recipeDict.TryGetValue(recipeID, out var r) ? r : null;
        }

        public List<IngredientData> GetIngredients(int recipeID)
        {
            if (_ingredientsDict != null && _ingredientsDict.TryGetValue(recipeID, out var list))
                return list;
            return new List<IngredientData>();
        }

        public RecipeUnlockCondition GetUnlockCondition(int recipeID)
        {
            return _unlockDict != null && _unlockDict.TryGetValue(recipeID, out var c) ? c : null;
        }

        public List<int> GetAllRecipeIDs()
        {
            if (_recipeDict != null)
                return _recipeDict.Keys.ToList();

            return recipes
                .Where(r => r != null)
                .Select(r => r.recipeID)
                .Distinct()
                .ToList();
        }

        // ──── 에디터 임포터용 ────

        public void SetRecipes(List<RecipeData> data)
        {
            recipes = data;
        }

        public void SetIngredients(List<IngredientData> data)
        {
            ingredients = data;
        }

        public void SetUnlockConditions(List<RecipeUnlockCondition> data)
        {
            unlockConditions = data;
        }

#if UNITY_EDITOR
        [ContextMenu("데이터베이스 새로고침")]
        private void RefreshEditor()
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"[RecipeDatabase] 저장 완료");
        }
#endif
    }
}
