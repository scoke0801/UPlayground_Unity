// 일반 파괴 수목의 입력을 줄기 capsule로 한정한다. 원래 물리 Collider와 배치 ID는 보존한다.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating) return "BUSY";
var bridge = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MCPForUnity.Editor.Services.Transport.Transports.StdioBridgeHost")).FirstOrDefault(t => t != null);
var queued = bridge != null ? bridge.GetProperty("QueuedCommandCount", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic) : null;
if (queued != null && (int)queued.GetValue(null, null) > 1) return "BUSY other MCP";
foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
{
    var api = assembly.GetType("UnityEditor.TestTools.TestRunner.Api.TestRunnerApi");
    var active = api != null ? api.GetMethod("IsTestExecutionActive", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public) : null;
    if (active != null && (bool)active.Invoke(null, null)) return "BUSY tests";
}
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");
if (!scene.IsValid() || !scene.isLoaded || scene.isDirty) return "BUSY scene";
var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>(true)).Where(t => t.Definition != null && t.Definition.reaction.canBreak && UnityEditor.AssetDatabase.GetAssetPath(t.Definition).StartsWith("Assets/10.Datas/Environment/TreeDestruction/")).ToArray();
var pending = all.Where(t => { var p = t.transform.Find("Intact/EnvironmentReactionHit"); return p == null || p.GetComponent<CapsuleCollider>() == null || p.GetComponents<BoxCollider>().Any(b => b.enabled) || !t.CanReceiveCollider(p.GetComponent<CapsuleCollider>()); }).Take(250).ToArray();
if (pending.Length == 0) return "DONE fixed=" + all.Length;
string folder = "output/ReactiveEnvironment/TreeDestruction-20261115/hit-fix/";
System.IO.Directory.CreateDirectory(folder);
UnityEditor.Undo.IncrementCurrentGroup(); int group = UnityEditor.Undo.GetCurrentGroup();
UnityEditor.Undo.SetCurrentGroupName("수목 줄기 피격 범위 수정");
var report = new System.Text.StringBuilder();
try
{
    foreach (var target in pending)
    {
        var proxy = target.transform.Find("Intact/EnvironmentReactionHit");
        var remnant = target.transform.Find("Remnant");
        var pivot = remnant != null ? remnant.Find("CutPivot") : null;
        if (proxy == null || pivot == null) throw new System.Exception("명시 줄기/절단 피벗 누락: " + target.name);
        string id = target.GetComponent<UPlayGround.Components.SceneEntityId>().Guid;
        var boxes = proxy.GetComponents<BoxCollider>();
        foreach (var box in boxes) { UnityEditor.Undo.RecordObject(box, "수관 입력 제외"); box.enabled = false; }
        var capsule = proxy.GetComponent<CapsuleCollider>();
        if (capsule == null) capsule = UnityEditor.Undo.AddComponent<CapsuleCollider>(proxy.gameObject);
        UnityEditor.Undo.RecordObject(capsule, "줄기 입력 치수");
        capsule.enabled = true; capsule.isTrigger = true; capsule.direction = 1;
        capsule.radius = target.Definition.destruction.trunkRadius;
        capsule.height = Mathf.Max(capsule.radius * 2, target.Definition.destruction.trunkLength + pivot.localPosition.y);
        capsule.center = Vector3.up * (capsule.height * 0.5f);
        var hits = new Collider[] { capsule }.Concat(remnant.GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger)).ToArray();
        var serialized = new UnityEditor.SerializedObject(target); var property = serialized.FindProperty("_hitColliders"); property.arraySize = hits.Length;
        for (int i = 0; i < hits.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = hits[i];
        serialized.ApplyModifiedProperties();
        typeof(UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(target, null);
        if (!target.IsAuthoringValid() || !target.CanReceiveCollider(capsule) || boxes.Any(b => target.CanReceiveCollider(b)) || target.GetComponent<UPlayGround.Components.SceneEntityId>().Guid != id) throw new System.Exception("줄기 입력 구성 오류: " + target.name);
        report.AppendLine(id + "\t" + target.Definition.name + "\t" + capsule.radius + "\t" + capsule.height);
    }
    UnityEditor.Undo.FlushUndoRecordObjects(); UnityEditor.Undo.CollapseUndoOperations(group);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new System.Exception("씬 저장 실패");
    System.IO.File.AppendAllText(folder + "applied.tsv", report.ToString());
    return "changed=" + pending.Length + " fixed=" + all.Count(t => { var p = t.transform.Find("Intact/EnvironmentReactionHit"); return p.GetComponent<CapsuleCollider>() != null && !p.GetComponents<BoxCollider>().Any(b => b.enabled) && t.CanReceiveCollider(p.GetComponent<CapsuleCollider>()); }) + " dirty=" + scene.isDirty;
}
catch { UnityEditor.Undo.RevertAllDownToGroup(group); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene); throw; }
