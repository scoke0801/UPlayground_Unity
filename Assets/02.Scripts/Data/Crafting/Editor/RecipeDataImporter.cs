#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Crafting;
using UPlayGround.Data.Path;

/// <summary>
/// CSV → RecipeDatabase.asset 변환 에디터 윈도우.
/// 메뉴: UPlayGround / Crafting / Import Recipe Data
///
/// CSV 형식 (헤더 1행 스킵):
///   recipe_master.csv     : recipeID, recipeName, resultItemID, resultQuantity, costType(Free|Gold), costAmount, castTimeSeconds, category, description, isDebugUnlocked(TRUE|FALSE)
///   recipe_ingredients.csv: recipeID, ingredientItemID, requiredQuantity
///   recipe_unlocks.csv    : recipeID, conditionType(None|MonsterKill|...), conditionValue, conditionValue2
/// </summary>
public class RecipeDataImporter : EditorWindow
{
    private const string PREF_RECIPE_PATH      = "RecipeDataImporter_RecipePath";
    private const string PREF_INGREDIENT_PATH  = "RecipeDataImporter_IngredientPath";
    private const string PREF_UNLOCK_PATH      = "RecipeDataImporter_UnlockPath";
    private const string PREF_OUTPUT_PATH      = "RecipeDataImporter_OutputPath";

    private string _csvRecipePath;
    private string _csvIngredientPath;
    private string _csvUnlockPath;
    private string _outputAssetPath;

    [MenuItem("UPlayGround/게임플레이/제작/레시피 데이터 가져오기")]
    public static void ShowWindow()
    {
        GetWindow<RecipeDataImporter>("Recipe Data Importer");
    }

    private void OnEnable()
    {
        // EditorPrefs에서 경로 로드
        _csvRecipePath      = EditorPrefs.GetString(PREF_RECIPE_PATH,      "Assets/10.Datas/Crafting/CSV/recipe_master.csv");
        _csvIngredientPath  = EditorPrefs.GetString(PREF_INGREDIENT_PATH,  "Assets/10.Datas/Crafting/CSV/recipe_ingredients.csv");
        _csvUnlockPath      = EditorPrefs.GetString(PREF_UNLOCK_PATH,      "Assets/10.Datas/Crafting/CSV/recipe_unlocks.csv");
        _outputAssetPath    = EditorPrefs.GetString(PREF_OUTPUT_PATH,      "Assets/10.Datas/Crafting/RecipeDatabase.asset");
    }

    private void OnGUI()
    {
        GUILayout.Label("Recipe Data Importer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("CSV 경로", EditorStyles.boldLabel);
        _csvRecipePath     = EditorGUILayout.TextField("Recipe Master",    _csvRecipePath);
        _csvIngredientPath = EditorGUILayout.TextField("Ingredients",      _csvIngredientPath);
        _csvUnlockPath     = EditorGUILayout.TextField("Unlock Conditions",_csvUnlockPath);

        EditorGUILayout.Space();
        _outputAssetPath = EditorGUILayout.TextField("출력 Asset 경로",     _outputAssetPath);
        EditorGUILayout.Space();

        if (GUILayout.Button("Import", GUILayout.Height(36)))
            Import();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "CSV 첫 행은 헤더로 스킵됩니다.\n" +
            "recipe_master.csv 컬럼 순서:\n" +
            "  recipeID, recipeName, resultItemID, resultQuantity,\n" +
            "  costType(Free|Gold), costAmount, castTimeSeconds,\n" +
            "  category, description, isDebugUnlocked(TRUE|FALSE)\n\n" +
            "recipe_ingredients.csv 컬럼 순서:\n" +
            "  recipeID, ingredientItemID, requiredQuantity\n\n" +
            "recipe_unlocks.csv 컬럼 순서:\n" +
            "  recipeID, conditionType, conditionValue, conditionValue2",
            MessageType.Info);
    }

    private void Import()
    {
        if (!File.Exists(_csvRecipePath))
        {
            EditorUtility.DisplayDialog("오류", $"파일 없음: {_csvRecipePath}", "확인");
            return;
        }

        var recipes     = ParseRecipes(_csvRecipePath);
        var ingredients = File.Exists(_csvIngredientPath) ? ParseIngredients(_csvIngredientPath) : new List<IngredientData>();
        var unlocks     = File.Exists(_csvUnlockPath)     ? ParseUnlocks(_csvUnlockPath)         : new List<RecipeUnlockCondition>();

        // 출력 폴더 보장
        string dir = Path.GetDirectoryName(_outputAssetPath).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(dir))
        {
            var parts = dir.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        var db = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(_outputAssetPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<RecipeDatabase>();
            AssetDatabase.CreateAsset(db, _outputAssetPath);
        }

        db.SetRecipes(recipes);
        db.SetIngredients(ingredients);
        db.SetUnlockConditions(unlocks);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // EditorPrefs에 경로 저장 (다음 실행 시 복원)
        EditorPrefs.SetString(PREF_RECIPE_PATH,     _csvRecipePath);
        EditorPrefs.SetString(PREF_INGREDIENT_PATH, _csvIngredientPath);
        EditorPrefs.SetString(PREF_UNLOCK_PATH,     _csvUnlockPath);
        EditorPrefs.SetString(PREF_OUTPUT_PATH,     _outputAssetPath);

        EditorUtility.DisplayDialog("완료",
            $"RecipeDatabase 임포트 성공!\n" +
            $"  레시피:    {recipes.Count}개\n" +
            $"  재료:      {ingredients.Count}개\n" +
            $"  언락 조건: {unlocks.Count}개",
            "확인");
    }

    // ──── 파싱 ────

    private List<RecipeData> ParseRecipes(string path)
    {
        var list  = new List<RecipeData>();
        var lines = File.ReadAllLines(path);

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var p = SplitCSV(line);
            if (p.Length < 10) { Debug.LogWarning($"[Importer] recipe_master 행 {i+1} 컬럼 부족, 스킵"); continue; }

            try
            {
                list.Add(new RecipeData
                {
                    recipeID        = int.Parse(p[0]),
                    recipeName      = p[1],
                    resultItemID    = int.Parse(p[2]),
                    resultQuantity  = int.Parse(p[3]),
                    costType        = (CostType)Enum.Parse(typeof(CostType), p[4], ignoreCase: true),
                    costAmount      = int.Parse(p[5]),
                    castTimeSeconds = float.Parse(p[6]),
                    category        = (CraftingCategory)Enum.Parse(typeof(CraftingCategory), p[7], ignoreCase: true),
                    description     = p[8],
                    isDebugUnlocked = p[9].Trim().ToUpper() == "TRUE",
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Importer] recipe_master 행 {i+1} 파싱 오류: {e.Message}");
            }
        }
        return list;
    }

    private List<IngredientData> ParseIngredients(string path)
    {
        var list  = new List<IngredientData>();
        var lines = File.ReadAllLines(path);

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var p = SplitCSV(line);
            if (p.Length < 3) { Debug.LogWarning($"[Importer] recipe_ingredients 행 {i+1} 컬럼 부족, 스킵"); continue; }

            try
            {
                list.Add(new IngredientData
                {
                    recipeID          = int.Parse(p[0]),
                    ingredientItemID  = int.Parse(p[1]),
                    requiredQuantity  = int.Parse(p[2]),
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Importer] recipe_ingredients 행 {i+1} 파싱 오류: {e.Message}");
            }
        }
        return list;
    }

    private List<RecipeUnlockCondition> ParseUnlocks(string path)
    {
        var list  = new List<RecipeUnlockCondition>();
        var lines = File.ReadAllLines(path);

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var p = SplitCSV(line);
            if (p.Length < 4) { Debug.LogWarning($"[Importer] recipe_unlocks 행 {i+1} 컬럼 부족, 스킵"); continue; }

            try
            {
                list.Add(new RecipeUnlockCondition
                {
                    recipeID       = int.Parse(p[0]),
                    conditionType  = (UnlockConditionType)Enum.Parse(typeof(UnlockConditionType), p[1], ignoreCase: true),
                    conditionValue = int.Parse(p[2]),
                    conditionValue2= int.Parse(p[3]),
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Importer] recipe_unlocks 행 {i+1} 파싱 오류: {e.Message}");
            }
        }
        return list;
    }

    /// <summary> 쉼표 구분 + 큰따옴표 처리 </summary>
    private static string[] SplitCSV(string line)
    {
        var result = new List<string>();
        bool inQuote = false;
        var  current = new System.Text.StringBuilder();

        foreach (char c in line)
        {
            if (c == '"') { inQuote = !inQuote; continue; }
            if (c == ',' && !inQuote) { result.Add(current.ToString().Trim()); current.Clear(); continue; }
            current.Append(c);
        }
        result.Add(current.ToString().Trim());
        return result.ToArray();
    }
}
#endif
