// 일반 고사목 원본 GUID만 밑동 교체형 파괴로 연결한다. 보호 식생과 원본 프리팹은 보존한다.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling) return "BUSY";
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");
if (!scene.IsValid() || !scene.isLoaded || scene.isDirty) return "BUSY scene";
var targets = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>(true)).Where(t => !t.Definition.reaction.canBreak && t.Definition.definitionId == "environment.ArenaTree" && UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject))) == "e806f7cbe5fe705488281b1bc7c72041").ToArray();
if (targets.Length == 0) return "DONE";
foreach (var target in targets)
    if (target.GetComponentInParent<UPlayGround.GameActor>(true) != null || UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(target.gameObject) != target.gameObject) throw new System.Exception("고사목 소유권 확인 필요");
string prefabPath = "Assets/03.Prefabs/Environment/Reactive/TreeRemnants/TreeRemnant_Dead.prefab";
string recipePath = "Assets/10.Datas/Environment/TreeDestruction/TreeFall_Dead.asset";
string definitionPath = "Assets/10.Datas/Environment/TreeDestruction/Tree_Dead.asset";
if (UnityEditor.AssetDatabase.LoadMainAssetAtPath(prefabPath) != null || UnityEditor.AssetDatabase.LoadMainAssetAtPath(recipePath) != null || UnityEditor.AssetDatabase.LoadMainAssetAtPath(definitionPath) != null) throw new System.Exception("기존 산출물 덮어쓰기 거부");
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.VegetationReactionCatalogSO>("Assets/10.Datas/Environment/Vegetation/Catalog.asset");
var materials = catalog.surfaces.ToDictionary(s => s.original, s => s.reactive);
var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
GameObject prefab = null;
try
{
    var source = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UnityEditor.AssetDatabase.GUIDToAssetPath("edcb24b795426f64ba5d0912072e2794"));
    var root = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source, preview); root.name = "TreeRemnant_Dead";
    foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterials = renderer.sharedMaterials.Select(m => materials[m]).ToArray();
    foreach (var t in root.GetComponentsInChildren<Transform>(true)) { t.gameObject.layer = LayerMask.NameToLayer("Ground"); UnityEditor.GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0); }
    root.SetActive(false);
    prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
    if (prefab == null) throw new System.Exception("고사목 밑동 저장 실패");
}
finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview); }
var recipe = ScriptableObject.CreateInstance<UPlayGround.Data.ReactiveEnvironment.EnvironmentDestructionRecipeSO>();
recipe.mode = UPlayGround.Data.ReactiveEnvironment.EnvironmentDestructionMode.Swap;
recipe.useLocalDimensions = true; recipe.changesNavigation = true;
UnityEditor.AssetDatabase.CreateAsset(recipe, recipePath);
var definition = UnityEngine.Object.Instantiate(targets[0].Definition); definition.name = "Tree_Dead";
definition.reaction.canBreak = true; definition.destruction = recipe;
definition.impactDrops = new System.Collections.Generic.List<UPlayGround.Data.Item.ItemDropList>(UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.ReactiveEnvironmentDefinitionSO>("Assets/10.Datas/Environment/TreeDestruction/Tree_A.asset").impactDrops);
UnityEditor.AssetDatabase.CreateAsset(definition, definitionPath);
UnityEditor.AssetDatabase.SaveAssetIfDirty(recipe); UnityEditor.AssetDatabase.SaveAssetIfDirty(definition);
UnityEditor.Undo.IncrementCurrentGroup(); int group = UnityEditor.Undo.GetCurrentGroup();
try
{
    foreach (var target in targets)
    {
        var root = target.gameObject;
        UnityEditor.PrefabUtility.UnpackPrefabInstance(root, UnityEditor.PrefabUnpackMode.OutermostRoot, UnityEditor.InteractionMode.UserAction);
        var children = root.transform.Cast<Transform>().ToArray();
        var intact = new GameObject("Intact"); intact.transform.SetParent(root.transform, false); UnityEditor.Undo.RegisterCreatedObjectUndo(intact, "고사목 원형");
        foreach (var child in children)
        {
            var p = child.localPosition; var r = child.localRotation; var s = child.localScale;
            UnityEditor.Undo.SetTransformParent(child, intact.transform, "고사목 원형 보존"); child.localPosition = p; child.localRotation = r; child.localScale = s;
        }
        var oldRenderer = root.GetComponent<MeshRenderer>(); var oldFilter = root.GetComponent<MeshFilter>(); var oldCollider = root.GetComponent<MeshCollider>();
        var filter = UnityEditor.Undo.AddComponent<MeshFilter>(intact); UnityEditor.EditorUtility.CopySerialized(oldFilter, filter);
        var renderer = UnityEditor.Undo.AddComponent<MeshRenderer>(intact); UnityEditor.EditorUtility.CopySerialized(oldRenderer, renderer);
        var collider = UnityEditor.Undo.AddComponent<MeshCollider>(intact); UnityEditor.EditorUtility.CopySerialized(oldCollider, collider); intact.layer = root.layer;
        UnityEditor.GameObjectUtility.SetStaticEditorFlags(intact, UnityEditor.GameObjectUtility.GetStaticEditorFlags(root));
        var remnant = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, root.transform); remnant.name = "Remnant"; remnant.SetActive(false); UnityEditor.Undo.RegisterCreatedObjectUndo(remnant, "고사목 밑동");
        UnityEditor.Undo.AddComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentDestruction>(root);
        var serialized = new UnityEditor.SerializedObject(target);
        serialized.FindProperty("_definition").objectReferenceValue = definition;
        serialized.FindProperty("_intact").objectReferenceValue = intact; serialized.FindProperty("_remnant").objectReferenceValue = remnant;
        var bindings = serialized.FindProperty("_surfaceBindings"); bindings.GetArrayElementAtIndex(0).FindPropertyRelative("renderer").objectReferenceValue = renderer;
        foreach (var surface in remnant.GetComponentsInChildren<Renderer>(true))
        {
            int next = bindings.arraySize; bindings.arraySize++; var binding = bindings.GetArrayElementAtIndex(next);
            binding.FindPropertyRelative("renderer").objectReferenceValue = surface;
            var slots = binding.FindPropertyRelative("slots"); slots.arraySize = surface.sharedMaterials.Length;
            for (int i = 0; i < slots.arraySize; i++) slots.GetArrayElementAtIndex(i).intValue = i;
            binding.FindPropertyRelative("excludedSlots").arraySize = 0;
        }
        foreach (string field in new[] { "_rainSamples", "_heatSamples", "_burnAnchors" })
        {
            var samples = serialized.FindProperty(field); int next = samples.arraySize; samples.arraySize++; samples.GetArrayElementAtIndex(next).objectReferenceValue = remnant.transform;
        }
        var hit = serialized.FindProperty("_hitColliders");
        var colliders = root.GetComponentsInChildren<Collider>(true).Where(c => c != oldCollider).ToArray(); hit.arraySize = colliders.Length;
        for (int i = 0; i < colliders.Length; i++) hit.GetArrayElementAtIndex(i).objectReferenceValue = colliders[i];
        serialized.ApplyModifiedProperties();
        UnityEditor.Undo.DestroyObjectImmediate(oldRenderer); UnityEditor.Undo.DestroyObjectImmediate(oldFilter); UnityEditor.Undo.DestroyObjectImmediate(oldCollider);
        typeof(UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(target, null);
        if (!target.IsAuthoringValid()) throw new System.Exception("고사목 구성 오류");
    }
    UnityEditor.Undo.FlushUndoRecordObjects(); UnityEditor.Undo.CollapseUndoOperations(group);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new System.Exception("씬 저장 실패");
    System.IO.File.AppendAllText("output/ReactiveEnvironment/TreeDestruction-20261115/converted.tsv", string.Join("\n", targets.Select(t => t.GetComponent<UPlayGround.Components.SceneEntityId>().Guid + "\tDead\t" + t.name).ToArray()) + "\n");
    return "Dead=" + targets.Length + " dirty=" + scene.isDirty;
}
catch { UnityEditor.Undo.RevertAllDownToGroup(group); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene); throw; }
