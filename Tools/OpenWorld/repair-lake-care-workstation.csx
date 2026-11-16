// 헤이즐·소담 작업대의 원래 충돌체·접촉 높이에 시각 모델을 맞춘다. 원본 자산과 기능 루트는 보존한다.
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
    throw new Exception("컴파일 완료된 편집 모드가 필요합니다.");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity" || scene.isDirty)
    throw new Exception("기존 미저장 변경이 없는 LakeVillageWorld가 필요합니다.");
const string folder = "output/NpcDailyAnimations/FloatingTrays";
const string food = "Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Props/Food/";
const string tablePath = "Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Props/Interior/Furniture/PF_Farm_WoodTable_01.prefab";
var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
Func<string, Transform> find = name => all.Single(t => t.name == name);
var table = find("Hazel_Worktable_R2");
var collision = find("Hazel_Worktable_Collision").GetComponent<BoxCollider>();
if (collision == null) throw new Exception("기존 작업대 충돌체 누락");
var names = new[] { "Hazel_Bottle_R2", "Hazel_Jar_R2", "RedTree_Hazel_Bottle", "Sodam_HerbTray_R2" };
var paths = new[] { food + "TGV_Bottle_01A.prefab", food + "TGV_Jar_01A.prefab", food + "TGV_Bottle_02A.prefab", food + "TGV_Bowl_01A.prefab" };
var sizes = new[] { new Vector3(.12f,.28f,.12f), new Vector3(.18f,.23f,.18f), new Vector3(.12f,.31f,.12f), new Vector3(.34f,.07f,.29f) };
var targets = new[] { table }.Concat(names.Select(find)).ToArray();
var sources = new[] { tablePath }.Concat(paths).Select(path => {
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    if (prefab == null) throw new Exception("작업물 원본 누락: " + path);
    var mesh = prefab.GetComponentsInChildren<MeshFilter>(true).First(f => f.sharedMesh != null);
    if (mesh.GetComponent<MeshRenderer>().sharedMaterials.Any(m => m == null)) throw new Exception("재질 누락: " + path);
    return mesh;
}).ToArray();
Func<string> identities = () => string.Join("\n", scene.GetRootGameObjects()
    .SelectMany(g => g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(id => id.Guid).OrderBy(id => id));
string beforeIds = identities();
System.IO.Directory.CreateDirectory(folder);
var before = new List<string>();
foreach (var t in targets) {
    var f = t.GetComponent<MeshFilter>(); var r = t.GetComponent<MeshRenderer>();
    if (f == null || r == null) throw new Exception("기능 루트의 시각 구조 변경: " + t.name);
    string guid; long localId;
    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f.sharedMesh, out guid, out localId);
    before.Add(t.name + "|instance=" + t.GetInstanceID() + "|position=" + t.position.ToString("R")
        + "|rotation=" + t.rotation.ToString("R") + "|scale=" + t.localScale.ToString("R")
        + "|mesh=" + guid + ":" + localId + "|materials=" + string.Join(",", r.sharedMaterials.Select(AssetDatabase.GetAssetPath)));
}
if (!System.IO.File.Exists(folder + "/before-targets.txt")) System.IO.File.WriteAllLines(folder + "/before-targets.txt", before);
Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("약초 작업대의 실제 접촉·소품 복구");
var rows = new List<string>();
Action<Transform, MeshFilter, Vector3, Vector3> fit = (target, source, center, size) => {
    var f = target.GetComponent<MeshFilter>(); var r = target.GetComponent<MeshRenderer>();
    Undo.RecordObjects(new UnityEngine.Object[] { target, f, r }, "기존 기능 루트의 시각 복구");
    f.sharedMesh = source.sharedMesh; r.sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
    target.localScale = Vector3.one;
    var originalSize = r.bounds.size;
    if (originalSize.x <= 0 || originalSize.y <= 0 || originalSize.z <= 0) throw new Exception("시각 크기 오류: " + target.name);
    // 대상 회전은 기존 직각 배치를 유지한다. 월드 크기 비율을 로컬 축으로 환산한다.
    var ratio = new Vector3(size.x / originalSize.x, size.y / originalSize.y, size.z / originalSize.z);
    if (target == table) {
        var local = Quaternion.Inverse(target.rotation) * ratio;
        target.localScale = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
    } else {
        float horizontal = Mathf.Min(ratio.x, ratio.z);
        target.localScale = new Vector3(horizontal, ratio.y, horizontal);
    }
    target.position += center - r.bounds.center;
    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    PrefabUtility.RecordPrefabInstancePropertyModifications(f);
    PrefabUtility.RecordPrefabInstancePropertyModifications(r);
};
GameObject probe = null;
try {
    var support = collision.bounds;
    fit(table, sources[0], support.center, support.size);
    var filter = table.GetComponent<MeshFilter>();
    probe = new GameObject("__약초작업면검사");
    probe.transform.SetPositionAndRotation(table.position, table.rotation); probe.transform.localScale = table.lossyScale;
    var surface = probe.AddComponent<MeshCollider>(); surface.sharedMesh = filter.sharedMesh;
    Physics.SyncTransforms();
    for (int i = 0; i < names.Length; i++) {
        var t = targets[i + 1]; var oldBounds = t.GetComponent<MeshRenderer>().bounds;
        var center = new Vector3(oldBounds.center.x, support.max.y, oldBounds.center.z);
        RaycastHit hit;
        if (!surface.Raycast(new Ray(center + Vector3.up * 2, Vector3.down), out hit, 4) || hit.normal.y < .9f)
            throw new Exception("소품 바로 아래 작업면 없음: " + t.name);
        center.y = hit.point.y + .003f + sizes[i].y * .5f;
        fit(t, sources[i + 1], center, sizes[i]);
        var b = t.GetComponent<MeshRenderer>().bounds;
        if (Mathf.Abs(b.min.y - hit.point.y - .003f) > .001f) throw new Exception("소품 접촉 오류: " + t.name);
        rows.Add(t.name + "|source=" + paths[i] + "|surface=" + hit.point.y.ToString("F4") + "|bottom=" + b.min.y.ToString("F4") + "|gap=" + (b.min.y - hit.point.y).ToString("F4"));
    }
    // 실제 약초 잎은 새 그릇의 윗면에 놓되 기존 배치 루트를 그대로 유지한다.
    var leaves = all.Where(t => t.name.StartsWith("SM_Farm_SmallGrass_03_LOD", StringComparison.Ordinal)
        && (t.position - new Vector3(1004.02f,63.353f,408.71f)).sqrMagnitude < .01f).ToArray();
    foreach (var leaf in leaves) {
        Undo.RecordObject(leaf, "약초 그릇 접촉");
        var renderer = leaf.GetComponent<MeshRenderer>();
        float top = targets[4].GetComponent<MeshRenderer>().bounds.max.y;
        leaf.position += Vector3.up * (top + .002f - renderer.bounds.min.y);
        PrefabUtility.RecordPrefabInstancePropertyModifications(leaf);
    }
    if (beforeIds != identities()) throw new Exception("기존 진행 ID 변경");
    UnityEngine.Object.DestroyImmediate(probe); probe = null;
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new Exception("씬 저장 실패");
    Undo.CollapseUndoOperations(group);
    System.IO.File.WriteAllLines(folder + "/applied.txt", rows);
} catch { Undo.RevertAllDownToGroup(group); throw; }
finally { if (probe != null) UnityEngine.Object.DestroyImmediate(probe); }
return string.Join("\n", rows);
