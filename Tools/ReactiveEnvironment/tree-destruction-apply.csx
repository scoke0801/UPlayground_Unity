// Unity MCP execute_code로 반복 실행한다. 한 번에 최대 250개, 실패 시 해당 묶음을 Undo로 되돌린다.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating) return "BUSY";
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");
if (!scene.IsValid() || !scene.isLoaded || scene.isDirty) return "BUSY scene";
var sourceGuids = new[] { "b852812b0cba20447946c8f4c553e4ab", "302271de8e339d746a63c0280d486b49", "7662606337e87c0489141120a99576a6", "d8a88c55c730b0143b9b7dd0a60c0bbc", "245ad5abd63085f4d87be2216fa0eb71" };
var variants = new[] { "A", "B", "C", "C_Red", "D" };
var targets = new System.Collections.Generic.List<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>();
var kinds = new System.Collections.Generic.List<int>();
var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>(true)).ToArray();
foreach (var target in all)
{
    if (target.Definition == null || target.Definition.reaction.canBreak || target.Definition.definitionId != "environment.ArenaTree") continue;
    var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(target.gameObject);
    int kind = System.Array.IndexOf(sourceGuids, UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(prefab)));
    if (kind < 0) continue;
    if (UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(target.gameObject) != target.gameObject)
        throw new System.InvalidOperationException("다른 구조물 안의 수목은 별도 저작 필요: " + target.name);
    if (target.GetComponentInParent<UPlayGround.GameActor>(true) != null) throw new System.InvalidOperationException("액터 소유 식생 보호");
    if (targets.Count < 250) { targets.Add(target); kinds.Add(kind); }
}
string folder = "output/ReactiveEnvironment/TreeDestruction-20261115/";
System.IO.Directory.CreateDirectory(folder);
if (!System.IO.File.Exists(folder + "baseline.json"))
{
    var baseline = all.Select(t => new {
        guid = t.GetComponent<UPlayGround.Components.SceneEntityId>().Guid,
        globalId = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(t.gameObject).ToString(),
        name = t.name, definition = UnityEditor.AssetDatabase.GetAssetPath(t.Definition),
        position = t.transform.localPosition.ToString("R"), rotation = t.transform.localRotation.ToString("R"), scale = t.transform.localScale.ToString("R"), active = t.gameObject.activeSelf,
        surfaces = t.GetComponentsInChildren<Renderer>(true).Select(r => new {
            id = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(r).ToString(),
            matrix = r.transform.localToWorldMatrix.ToString("R"), active = r.gameObject.activeSelf,
            mesh = r.GetComponent<MeshFilter>() != null ? UnityEditor.AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh) + ":" + r.GetComponent<MeshFilter>().sharedMesh.name : "",
            materials = r.sharedMaterials.Select(UnityEditor.AssetDatabase.GetAssetPath).ToArray()
        }).ToArray()
    }).ToArray();
    var json = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert");
    System.IO.File.WriteAllText(folder + "baseline.json", (string)json.GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new object[] { baseline }));
}
if (targets.Count == 0) return "DONE converted=" + all.Count(t => t.Definition != null && t.Definition.reaction.canBreak);
var definitions = variants.Select(v => UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.ReactiveEnvironmentDefinitionSO>("Assets/10.Datas/Environment/TreeDestruction/Tree_" + v + ".asset")).ToArray();
var remnants = variants.Select(v => UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/Reactive/TreeRemnants/TreeRemnant_" + v + ".prefab")).ToArray();
if (definitions.Any(d => d == null || !d.IsValid()) || remnants.Any(r => r == null)) throw new System.InvalidOperationException("생성 자산 누락");
System.Action<UnityEditor.SerializedProperty, UnityEngine.Object[]> setObjects = (property, values) => {
    property.arraySize = values.Length;
    for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
};
UnityEditor.Undo.IncrementCurrentGroup(); int group = UnityEditor.Undo.GetCurrentGroup();
UnityEditor.Undo.SetCurrentGroupName("일반 수목 파괴 연결");
var report = new System.Text.StringBuilder();
try
{
    for (int index = 0; index < targets.Count; index++)
    {
        var target = targets[index]; var root = target.gameObject; int kind = kinds[index];
        var identity = root.GetComponent<UPlayGround.Components.SceneEntityId>(); string id = identity.Guid;
        var before = root.GetComponentsInChildren<Renderer>(true).Select(r => new { renderer = r, matrix = r.transform.localToWorldMatrix, materials = r.sharedMaterials, active = r.gameObject.activeSelf }).ToArray();
        UnityEditor.PrefabUtility.UnpackPrefabInstance(root, UnityEditor.PrefabUnpackMode.OutermostRoot, UnityEditor.InteractionMode.UserAction);
        var children = root.transform.Cast<Transform>().ToArray();
        var intact = new GameObject("Intact"); intact.transform.SetParent(root.transform, false);
        UnityEditor.Undo.RegisterCreatedObjectUndo(intact, "원형 계층");
        foreach (var child in children)
        {
            var position = child.localPosition; var rotation = child.localRotation; var scale = child.localScale;
            UnityEditor.Undo.SetTransformParent(child, intact.transform, "원형 배치 보존");
            child.localPosition = position; child.localRotation = rotation; child.localScale = scale;
        }
        var remnant = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(remnants[kind], root.transform);
        remnant.name = "Remnant"; remnant.SetActive(false);
        UnityEditor.Undo.RegisterCreatedObjectUndo(remnant, "절단 잔해");
        var destruction = UnityEditor.Undo.AddComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentDestruction>(root);
        var structural = new UnityEditor.SerializedObject(destruction);
        structural.FindProperty("_fallPivot").objectReferenceValue = remnant.transform.Find("CutPivot");
        structural.FindProperty("_blockedFallback").objectReferenceValue = remnant.transform.Find("BlockedLog").gameObject;
        setObjects(structural.FindProperty("_shelterColliders"), intact.GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger).Cast<UnityEngine.Object>().ToArray());
        setObjects(structural.FindProperty("_standingObstacles"), intact.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>(true).Cast<UnityEngine.Object>().ToArray());
        setObjects(structural.FindProperty("_remnantObstacles"), remnant.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>(true).Cast<UnityEngine.Object>().ToArray());
        structural.ApplyModifiedProperties();
        var serialized = new UnityEditor.SerializedObject(target);
        serialized.FindProperty("_definition").objectReferenceValue = definitions[kind];
        serialized.FindProperty("_intact").objectReferenceValue = intact;
        serialized.FindProperty("_remnant").objectReferenceValue = remnant;
        var samples = new[] { remnant.transform.Find("Stump/StumpSample"), remnant.transform.Find("CutPivot/TrunkSample"), remnant.transform.Find("BlockedLog/LogSample") };
        foreach (string field in new[] { "_rainSamples", "_heatSamples", "_burnAnchors" })
        {
            var property = serialized.FindProperty(field);
            var values = new System.Collections.Generic.List<UnityEngine.Object>();
            for (int i = 0; i < property.arraySize; i++) values.Add(property.GetArrayElementAtIndex(i).objectReferenceValue);
            values.AddRange(samples); setObjects(property, values.ToArray());
        }
        serialized.FindProperty("_landingAnchor").objectReferenceValue = remnant.transform.Find("CutPivot/Landing");
        serialized.FindProperty("_impactAnchor").objectReferenceValue = samples[0];
        // 수관/impostor 전체 상자는 옆 나무까지 맞게 하므로 줄기 입력만 허용한다.
        var proxy = intact.transform.Find("EnvironmentReactionHit");
        foreach (var box in proxy.GetComponents<BoxCollider>()) { UnityEditor.Undo.RecordObject(box, "수관 입력 제외"); box.enabled = false; }
        var trunkHit = UnityEditor.Undo.AddComponent<CapsuleCollider>(proxy.gameObject);
        trunkHit.isTrigger = true; trunkHit.radius = definitions[kind].destruction.trunkRadius;
        trunkHit.height = Mathf.Max(trunkHit.radius * 2, definitions[kind].destruction.trunkLength + remnant.transform.Find("CutPivot").localPosition.y);
        trunkHit.center = Vector3.up * (trunkHit.height * 0.5f);
        setObjects(serialized.FindProperty("_hitColliders"), new Collider[] { trunkHit }.Concat(remnant.GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger)).Cast<UnityEngine.Object>().ToArray());
        var bindings = serialized.FindProperty("_surfaceBindings");
        foreach (var renderer in remnant.GetComponentsInChildren<Renderer>(true))
        {
            int next = bindings.arraySize; bindings.arraySize++;
            var binding = bindings.GetArrayElementAtIndex(next);
            binding.FindPropertyRelative("renderer").objectReferenceValue = renderer;
            var slots = binding.FindPropertyRelative("slots"); slots.arraySize = renderer.sharedMaterials.Length;
            for (int i = 0; i < slots.arraySize; i++) slots.GetArrayElementAtIndex(i).intValue = i;
            binding.FindPropertyRelative("excludedSlots").arraySize = 0;
        }
        serialized.ApplyModifiedProperties();
        typeof(UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(target, null);
        if (!target.IsAuthoringValid() || identity.Guid != id) throw new System.InvalidOperationException("파괴 구성/ID 오류: " + root.name);
        foreach (var surface in before)
            if (surface.renderer.transform.localToWorldMatrix != surface.matrix || surface.renderer.gameObject.activeSelf != surface.active || !surface.renderer.sharedMaterials.SequenceEqual(surface.materials))
                throw new System.InvalidOperationException("원형 외형 변경: " + root.name);
        UnityEditor.EditorUtility.SetDirty(root);
        report.AppendLine(id + "\t" + variants[kind] + "\t" + root.name);
    }
    UnityEditor.Undo.FlushUndoRecordObjects();
    UnityEditor.Undo.CollapseUndoOperations(group);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new System.InvalidOperationException("씬 저장 실패");
    System.IO.File.AppendAllText(folder + "converted.tsv", report.ToString());
    return "changed=" + targets.Count + " converted=" + all.Count(t => t.Definition != null && t.Definition.reaction.canBreak) + " dirty=" + scene.isDirty;
}
catch
{
    UnityEditor.Undo.RevertAllDownToGroup(group);
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    throw;
}
