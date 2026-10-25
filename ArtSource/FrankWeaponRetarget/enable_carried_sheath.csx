// 납도 완료 포즈를 휴대 기준으로 사용하고, 이전 리그의 칼집 커브만 제거한다.
const string prefabPath = "Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab";
const string folder = "Assets/07.Animations/Player/Raon/FrankWeapon";
const string backupFolder = "output/FrankWeaponRetarget/CarriedSheath/Before";
if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
    throw new System.InvalidOperationException("편집 모드에서 컴파일 오류 없이 실행해야 합니다.");
var clips = AssetDatabase.FindAssets("t:AnimationClip", new[] { folder })
    .Select(g => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
var unequip = clips.Single(c => c.name == "Raon_Frank_RPG_Katana_Unequip");
System.IO.Directory.CreateDirectory(backupFolder);
foreach (string path in clips.Select(AssetDatabase.GetAssetPath).Concat(new[] { prefabPath }))
{
    string backup = backupFolder + "/" + System.IO.Path.GetFileName(path);
    if (System.IO.File.Exists(backup)) throw new System.InvalidOperationException("기존 작업 백업을 확인하세요: " + backup);
    System.IO.File.Copy(path, backup);
}
var root = PrefabUtility.LoadPrefabContents(prefabPath);
try
{
    var sheath = root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Sword_Case");
    string path = AnimationUtility.CalculateTransformPath(sheath, root.transform);
    var visual = sheath.Find("FrankSheathVisual");
    System.Func<string, float> end = property => AnimationUtility.GetEditorCurve(unequip,
        EditorCurveBinding.FloatCurve(path, typeof(Transform), property)).Evaluate(unequip.length);
    sheath.localPosition = new Vector3(end("m_LocalPosition.x"), end("m_LocalPosition.y"), end("m_LocalPosition.z"));
    sheath.localRotation = new Quaternion(end("m_LocalRotation.x"), end("m_LocalRotation.y"), end("m_LocalRotation.z"), end("m_LocalRotation.w"));
    visual.localScale = Vector3.one;
    // 납도 외에는 허리 본을 따라간다. 검·몸·판정 타이밍 커브는 그대로 둔다.
    foreach (var clip in clips.Where(c => c != unequip))
    {
        Undo.RecordObject(clip, "휴대 칼집 기준 자세 적용");
        foreach (var binding in AnimationUtility.GetCurveBindings(clip).Where(b => b.path == path && b.type == typeof(Transform)))
            AnimationUtility.SetEditorCurve(clip, binding, null);
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssetIfDirty(clip);
    }
    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
    return new { prefab = prefabPath, carryPosition = sheath.localPosition.ToString("F6"), clips = clips.Length - 1, unequipPreserved = true };
}
finally { PrefabUtility.UnloadPrefabContents(root); }
