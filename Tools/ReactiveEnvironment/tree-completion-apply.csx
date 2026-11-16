// Unity MCP execute_code에서 실행한다. 배치·원본 프리팹을 저장하지 않고 명시한 5종 SO만 갱신한다.
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return "BUSY";
var guids = new[] { "09bb989aa43f5f7499259ff8b1797606", "b62f4324fc48dcb49a53f3cbed508eca", "1265961f8425a6b4eb6b1b1c1fd11bfc", "f831dfbe288f0c14ebdd4901428a2614", "e776f1f4f9736714d9bdb47fa3147b96" };
var recipes = guids.Select(g => AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.EnvironmentDestructionRecipeSO>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
var profile = AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.EnvironmentPresentationProfileSO>(AssetDatabase.GUIDToAssetPath("726d36836a6801c47997f1ccd2d82c13"));
if (recipes.Any(r => r == null || !r.IsValid) || profile == null || profile.impact == null || profile.landing == null) throw new System.InvalidOperationException("수목 정본 GUID/FX 참조 누락");
Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("일반 수목 낙하 완료 정책");
try
{
    foreach (var recipe in recipes)
    {
        Undo.RecordObject(recipe, "벌목 완료 정책");
        recipe.completionPolicy = UPlayGround.Data.ReactiveEnvironment.EnvironmentCompletionPolicy.ClearAfterSettlement;
        recipe.blockedCollapseSeconds = 0.35f; recipe.settlementSeconds = 0.1f; recipe.clearingSeconds = 0.45f;
        if (!recipe.IsValid) throw new System.InvalidOperationException("완료 레시피 검증 실패");
        EditorUtility.SetDirty(recipe);
    }
    Undo.RecordObject(profile, "벌목 마무리 FX");
    profile.clearingChips = profile.impact; profile.clearingDust = profile.landing;
    profile.clearingSound = profile.impactSound; profile.clearingParticles = 12;
    if (!profile.IsValid) throw new System.InvalidOperationException("마무리 FX 프로필 검증 실패");
    EditorUtility.SetDirty(profile);
    foreach (var recipe in recipes) AssetDatabase.SaveAssetIfDirty(recipe);
    AssetDatabase.SaveAssetIfDirty(profile);
    Undo.CollapseUndoOperations(group);
}
catch
{
    Undo.RevertAllDownToGroup(group);
    foreach (var recipe in recipes) { EditorUtility.SetDirty(recipe); AssetDatabase.SaveAssetIfDirty(recipe); }
    EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile);
    throw;
}
return new { recipes = recipes.Select(r => new { path = AssetDatabase.GetAssetPath(r), policy = r.completionPolicy.ToString(), r.blockedCollapseSeconds, r.settlementSeconds, r.clearingSeconds }).ToArray(), profile = AssetDatabase.GetAssetPath(profile), sceneDirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty };
