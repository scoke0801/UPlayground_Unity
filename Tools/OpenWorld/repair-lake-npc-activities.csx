// 저장된 현장의 참조와 ID를 보존하는 증분 저작. 전체 월드 생성기는 실행하지 않는다.
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorUtility.scriptCompilationFailed)
    throw new Exception("컴파일이 완료된 편집 모드가 필요합니다.");
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");
if (!scene.isLoaded || scene.isDirty) throw new Exception("저장된 LakeVillageWorld가 필요합니다.");
const string report = "output/NpcDailyAnimations/LakeFix";
System.IO.Directory.CreateDirectory(report);
if (!System.IO.File.Exists(report + "/LakeVillageWorld.before.unity"))
    System.IO.File.Copy(scene.path, report + "/LakeVillageWorld.before.unity");
var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
Func<string, Transform> find = name => all.Single(t => t.name == name);
var identities = all.SelectMany(t => t.GetComponents<UPlayGround.Components.SceneEntityId>()).Select(i => i.Guid).OrderBy(i => i).ToArray();
var logs = new List<string>();
Func<GameObject, Bounds> bounds = go => {
    var rs = go.GetComponentsInChildren<MeshRenderer>(true);
    if (rs.Length == 0) throw new Exception("시각 메시 누락: " + go.name);
    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
};
Func<Vector3, Vector3> ground = p => {
    var hits = Physics.RaycastAll(new Vector3(p.x, 200, p.z), Vector3.down, 400, ~0, QueryTriggerInteraction.Ignore)
        .Where(h => h.collider is TerrainCollider).OrderByDescending(h => h.point.y).ToArray();
    if (hits.Length == 0) throw new Exception("지면 누락: " + p);
    return hits[0].point;
};
Action<UnityEngine.Object, string, object> set = (target, path, value) => {
    Undo.RecordObject(target, "생활 행동·식탁 접촉 수정");
    var so = new SerializedObject(target); var p = so.FindProperty(path);
    if (value is bool) p.boolValue = (bool)value;
    else if (value is float) p.floatValue = (float)value;
    else if (value is int) p.intValue = (int)value;
    else if (value is string) p.stringValue = (string)value;
    else p.objectReferenceValue = value as UnityEngine.Object;
    so.ApplyModifiedProperties();
};
const string motions = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/";
Func<string, UPlayGround.Animation.MotionSetAsset> motion = name => {
    var m = AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>(motions + name + ".asset");
    if (m == null) throw new Exception("모션 누락: " + name); return m;
};
var pack = motion("LivingWorld/LW_DeparturePack");
var inspect = motion("LivingWorld/LW_CargoInspect");
var idle = motion("Npc_Female_Idle");
Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("호수 마을 식탁·피난민 생활 행동");
try {
    // 외형 전체 bounds의 상단에는 컵·촛대가 포함된다. 실제 메시의 상판을 직접 측정한다.
    var placements = new[] {
        new[] { "RedTree_Sodam_HerbTray", "RedTree_Bakery_Worktable" },
        new[] { "R09_SmallerMealPortion", "RedTree_Bakery_Worktable" },
        new[] { "R05_Document_warning", "R05_Table_warning" },
        new[] { "R05_Document_offering", "R05_Table_offering" },
        new[] { "R05_Document_inquiry", "R05_Table_inquiry" },
        new[] { "R05_RecordsToShare", "R05_SharingTable" },
        new[] { "R05_SharedCopies", "R05_SharingTable" },
        new[] { "R06_DeliveryList", "R06_DeliveryTable" }
    };
    foreach (var row in placements) {
        var prop = find(row[0]); var table = find(row[1]);
        var mesh = table.GetComponentsInChildren<MeshFilter>(true).First();
        var probe = new GameObject("__상판접촉검사");
        try {
            probe.transform.SetPositionAndRotation(mesh.transform.position, mesh.transform.rotation);
            probe.transform.localScale = mesh.transform.lossyScale;
            var collider = probe.AddComponent<MeshCollider>(); collider.sharedMesh = mesh.sharedMesh;
            Physics.SyncTransforms();
            var before = bounds(prop.gameObject); var target = before.center;
            // 원래 좌우 배치는 보존하고 상판 폭을 벗어난 부분만 안으로 옮긴다.
            var local = table.InverseTransformPoint(target);
            local.x = Mathf.Clamp(local.x, -0.55f, 0.55f);
            local.z = Mathf.Clamp(local.z, -0.12f, 0.12f);
            target = table.TransformPoint(local);
            RaycastHit hit;
            if (!collider.Raycast(new Ray(target + Vector3.up * 4, Vector3.down), out hit, 8) || hit.normal.y < 0.9f)
                throw new Exception("실제 상판 없음: " + row[0]);
            Undo.RecordObject(prop, "식탁 상판 접촉 보정");
            prop.position += new Vector3(target.x - before.center.x, hit.point.y + 0.003f - before.min.y, target.z - before.center.z);
            PrefabUtility.RecordPrefabInstancePropertyModifications(prop);
            logs.Add(row[0] + " before=" + before.min.y.ToString("F4") + " surface=" + hit.point.y.ToString("F4") + " gap=" + (bounds(prop.gameObject).min.y - hit.point.y).ToString("F4"));
        } finally { UnityEngine.Object.DestroyImmediate(probe); }
    }

    var root = scene.GetRootGameObjects().SingleOrDefault(g => g.name == "LakeCampActivities");
    if (root == null) {
        root = new GameObject("LakeCampActivities");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        Undo.RegisterCreatedObjectUndo(root, "피난 주민 일과");
    }
    Func<string, Vector3, float, UPlayGround.Animation.MotionSetAsset, UPlayGround.Components.LivingWorld.NpcWorkPoint> point = (name, pos, yaw, m) => {
        var child = root.transform.Find(name);
        if (child == null) { var go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go, "생활 작업점"); go.transform.SetParent(root.transform); child = go.transform; }
        Undo.RecordObject(child, "생활 작업점 정렬");
        child.SetPositionAndRotation(ground(pos) + Vector3.up * 0.02f, Quaternion.Euler(0, yaw, 0));
        var p = child.GetComponent<UPlayGround.Components.LivingWorld.NpcWorkPoint>();
        if (p == null) p = Undo.AddComponent<UPlayGround.Components.LivingWorld.NpcWorkPoint>(child.gameObject);
        set(p, "_mapId", "LakeOfLifeScenario"); set(p, "_pointId", name); set(p, "_motion", m);
        return p;
    };
    var cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/WorldCities/CargoCrate.prefab");
    if (cratePrefab == null) throw new Exception("짐 모델 누락");
    for (int i = 0; i < 2; i++) {
        var actor = find(i == 0 ? "RedTree_RefugeeA" : "RedTree_RefugeeB").GetComponent<UPlayGround.NpcActor>();
        string id = "lake.camp." + i;
        var work = point(id + ".pack", new Vector3(991.5f + i * 3.6f, 0, 469.4f), 0, i == 0 ? pack : inspect);
        var check = point(id + ".check", new Vector3(991.5f + i * 3.6f, 0, 473f), 180, inspect);
        var rest = point(id + ".rest", new Vector3(990.5f + i * 5.6f, 0, 471.1f), i == 0 ? 90 : 270, idle);
        foreach (var p in new[] { work, check }) {
            string name = p.PointId + ".supplies";
            if (root.transform.Find(name) == null) {
                var crate = (GameObject)PrefabUtility.InstantiatePrefab(cratePrefab, scene);
                Undo.RegisterCreatedObjectUndo(crate, "정리할 실제 짐");
                crate.name = name; crate.transform.SetParent(root.transform);
                var b = bounds(crate); crate.transform.localScale *= 0.65f / b.size.y;
                b = bounds(crate); Vector3 at = ground(p.transform.position + p.transform.forward * 0.65f);
                crate.transform.position += new Vector3(at.x - b.center.x, at.y - b.min.y, at.z - b.center.z);
            }
            // 작업물 때문에 보행 캡슐이 손 작업점에서 밀려나지 않게 한다.
            foreach (var collider in root.transform.Find(name).GetComponentsInChildren<Collider>(true)) {
                Undo.RecordObject(collider, "작업 소품 충돌 해제"); collider.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            }
        }
        var routine = actor.GetComponent<UPlayGround.Components.LivingWorld.NpcRoutine>();
        if (routine == null) routine = Undo.AddComponent<UPlayGround.Components.LivingWorld.NpcRoutine>(actor.gameObject);
        set(routine, "_actor", actor); set(routine, "_mapId", "LakeOfLifeScenario");
        set(routine, "_isPlacementReady", true); set(routine, "_startDelaySeconds", i == 0 ? 1f : 4f);
        set(routine, "_arrivalDistance", 0.04f);
        var data = new SerializedObject(routine); var steps = data.FindProperty("_steps"); steps.arraySize = 2;
        for (int s = 0; s < 2; s++) {
            var step = steps.GetArrayElementAtIndex(s);
            step.FindPropertyRelative("point").objectReferenceValue = s == 0 ? work : check;
            step.FindPropertyRelative("seconds").floatValue = s == 0 ? 12f + i * 5f : 5f + i * 3f;
            step.FindPropertyRelative("waypoints").arraySize = 0;
        }
        data.FindProperty("_rest.point").objectReferenceValue = rest;
        data.FindProperty("_rest.seconds").floatValue = 4f + i * 2f;
        data.ApplyModifiedProperties();
        logs.Add(actor.name + " packing -> supplies check -> short rest; existing actor/SceneEntityId preserved");
    }
    Physics.SyncTransforms();
    var after = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(i => i.Guid).OrderBy(i => i).ToArray();
    if (!identities.SequenceEqual(after)) throw new Exception("진행 ID 변경");
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new Exception("씬 저장 실패");
    Undo.CollapseUndoOperations(group);
    System.IO.File.WriteAllLines(report + "/applied.txt", logs);
} catch { Undo.RevertAllDownToGroup(group); throw; }
return string.Join("\n", logs);

