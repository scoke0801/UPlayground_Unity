// 납도 완료 포즈를 휴대 기준으로 사용한다. 정적인 칼집 커브만 제거하며,
// 동적인 칼집 커브는 DynamicSheathRetarget에서 라온 좌표계로 다시 구운 결과를 보존한다.
const string prefabPath = "Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab";
const string folder = "Assets/07.Animations/Player/Raon/FrankWeapon";
const string backupFolder = "output/FrankWeaponRetarget/CarriedSheath/Before";
if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
    throw new System.InvalidOperationException("편집 모드에서 컴파일 오류 없이 실행해야 합니다.");
var clips = AssetDatabase.FindAssets("t:AnimationClip", new[] { folder })
    .Select(g => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
var unequip = clips.Single(c => c.name == "Raon_Frank_RPG_Katana_Unequip");
System.Func<AnimationClip, string, bool> hasDynamicSheath = (clip, path) =>
{
    System.Func<string, float, float> value = (property, time) =>
    {
        var curve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property));
        return curve == null ? 0f : curve.Evaluate(time);
    };

    Vector3 initialPosition = new Vector3(
        value("m_LocalPosition.x", 0f), value("m_LocalPosition.y", 0f), value("m_LocalPosition.z", 0f));
    Quaternion initialRotation = new Quaternion(
        value("m_LocalRotation.x", 0f), value("m_LocalRotation.y", 0f),
        value("m_LocalRotation.z", 0f), value("m_LocalRotation.w", 0f));
    int frameCount = Mathf.CeilToInt(clip.length * 60f);
    for (int frame = 1; frame <= frameCount; frame++)
    {
        float time = Mathf.Min(frame / 60f, clip.length);
        Vector3 position = new Vector3(
            value("m_LocalPosition.x", time), value("m_LocalPosition.y", time), value("m_LocalPosition.z", time));
        Quaternion rotation = new Quaternion(
            value("m_LocalRotation.x", time), value("m_LocalRotation.y", time),
            value("m_LocalRotation.z", time), value("m_LocalRotation.w", time));
        if (Vector3.Distance(initialPosition, position) > 0.0001f
            || Quaternion.Angle(initialRotation, rotation) > 0.1f)
            return true;
    }

    return false;
};
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
    // 정적 클립만 허리 본을 따르게 한다. 동적 클립의 검집 경로는 별도 리타깃 결과다.
    foreach (var clip in clips.Where(c => c != unequip))
    {
        if (!hasDynamicSheath(clip, path))
        {
            Undo.RecordObject(clip, "휴대 칼집 기준 자세 적용");
            foreach (var binding in AnimationUtility.GetCurveBindings(clip).Where(b => b.path == path && b.type == typeof(Transform)))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssetIfDirty(clip);
        }
    }
    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
    return new { prefab = prefabPath, carryPosition = sheath.localPosition.ToString("F6"), clips = clips.Length - 1, unequipPreserved = true };
}
finally { PrefabUtility.UnloadPrefabContents(root); }
