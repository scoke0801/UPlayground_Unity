// 기존 프리팹의 구조·GUID를 보존하고 공유 머티리얼 참조만 갱신한다.
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
    throw new System.InvalidOperationException("컴파일이 끝난 편집 모드에서 실행하세요.");
string prefabPath = "Assets/03.Prefabs/Characters/ScenarioResidents/Npc/Npc_Daon/Npc_Daon.prefab";
string presetPath = "Assets/10.Datas/Actor/ScenarioResidents/Npc_Daon_BuildPreset.asset";
var profile = AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.P09ToonLookProfileSO>("Assets/10.Datas/Actor/ScenarioResidents/ToonLook/P09_Daon_ToonLook.asset");
var preset = AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>(presetPath);
if (profile == null || preset == null) throw new System.InvalidOperationException("프로파일 또는 프리셋 누락");
profile.ValidateBindings();
byte[] prefabBackup = System.IO.File.ReadAllBytes(prefabPath);
byte[] presetBackup = System.IO.File.ReadAllBytes(presetPath);
string presetJson = EditorJsonUtility.ToJson(preset);
string guid = AssetDatabase.AssetPathToGUID(prefabPath);
int changed = 0;
try
{
    var root = PrefabUtility.LoadPrefabContents(prefabPath);
    try
    {
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                throw new System.InvalidOperationException("Missing Script: " + transform.name);
        changed = profile.Apply(root);
        if (profile.Apply(root) != 0) throw new System.InvalidOperationException("반복 적용 불안정");
        if (changed > 0)
        {
            bool success;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out success);
            if (!success) throw new System.InvalidOperationException("프리팹 저장 실패");
        }
    }
    finally { PrefabUtility.UnloadPrefabContents(root); }
    preset.config.ToonLookProfile = profile;
    EditorUtility.SetDirty(preset);
    AssetDatabase.SaveAssetIfDirty(preset);
    if (guid != AssetDatabase.AssetPathToGUID(prefabPath)) throw new System.InvalidOperationException("GUID 변경");
    var saved = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    foreach (var renderer in saved.GetComponentsInChildren<Renderer>(true))
        foreach (var material in renderer.sharedMaterials)
            if (profile.Resolve(renderer.name, material) != material)
                throw new System.InvalidOperationException("재로드 후 원본 재질 잔존: " + renderer.name);
}
catch
{
    EditorJsonUtility.FromJsonOverwrite(presetJson, preset);
    System.IO.File.WriteAllBytes(prefabPath, prefabBackup);
    System.IO.File.WriteAllBytes(presetPath, presetBackup);
    AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
    AssetDatabase.ImportAsset(presetPath, ImportAssetOptions.ForceUpdate);
    throw;
}
System.IO.Directory.CreateDirectory("output/P09ToonLook");
System.IO.File.WriteAllText("output/P09ToonLook/apply-result.txt", "교체 슬롯: " + changed + "\n프리팹 GUID 보존: " + guid + "\nMissing Script 0, 재적용 변경 0, 재로드 참조 검사 통과.");
return new { changed, guid };
