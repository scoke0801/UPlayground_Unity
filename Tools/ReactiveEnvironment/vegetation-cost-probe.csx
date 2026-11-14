var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.isDirty || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling) return "BUSY";
var log = new System.Text.StringBuilder();
var clock = System.Diagnostics.Stopwatch.StartNew(); long last = 0;
System.Action<string> lap = name => { long now = clock.ElapsedMilliseconds; log.AppendLine(name + "=" + (now-last)); last=now; };
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.VegetationReactionCatalogSO>("Assets/10.Datas/Environment/Vegetation/Catalog.asset");
var candidates = UPlayGround.EditorTools.VegetationReactionAuthoring.Collect(scene,catalog);
var plants = new System.Collections.Generic.List<UPlayGround.EditorTools.VegetationReactionAuthoring.Plant>();
foreach (var plant in candidates) if (plant.root.GetComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>() == null) { plants.Add(plant); break; }
UPlayGround.Gameplay.World.LivingWorld.EnvironmentController weather = null;
foreach (var root in scene.GetRootGameObjects()) { var candidate=root.GetComponentInChildren<UPlayGround.Gameplay.World.LivingWorld.EnvironmentController>(true); if(candidate!=null)weather=candidate; }
const float MinimumInputThickness = 0.001f;
System.Action<UnityEditor.SerializedProperty,UnityEngine.Object[]> SetObjects = (array,objects) => {array.arraySize=objects.Length; for(int i=0;i<objects.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=objects[i];};
var boundsMethod=typeof(UPlayGround.EditorTools.VegetationReactionAuthoring).GetMethod("LocalBounds",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
System.Func<UPlayGround.EditorTools.VegetationReactionAuthoring.Plant,UnityEngine.Bounds> LocalBounds = p => (UnityEngine.Bounds)boundsMethod.Invoke(null,new object[]{p});
lap("collect");
UnityEditor.Undo.IncrementCurrentGroup(); int outer=UnityEditor.Undo.GetCurrentGroup();
UnityEditor.EditorApplication.LockReloadAssemblies(); UnityEditor.AssetDatabase.DisallowAutoRefresh();
try {
            if (EditorApplication.isPlayingOrWillChangePlaymode || weather == null) throw new InvalidOperationException("Edit Mode와 실제 날씨 연결이 필요합니다.");
            var materials = new System.Collections.Generic.Dictionary<Material, Material>();
            foreach (var surface in catalog.surfaces) materials.Add(surface.original, surface.reactive);
            foreach (var plant in plants)
            foreach (var renderer in plant.surfaces)
            foreach (var material in renderer.sharedMaterials)
                if (material == null || !UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentSurface.SupportsReaction(material) && !materials.ContainsKey(material))
                    throw new InvalidOperationException("재질 대응 누락: " + renderer.name);

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("식생 반응 연결");
            int changed = 0;
            try
            {
                foreach (var plant in plants)
                {
                    if (plant.root.GetComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>() != null) continue;
                    UPlayGround.EditorTools.ReactiveEnvironmentAuthoring.Apply(plant.root, plant.definition, weather);
                    lap("apply base");
                    var metadata = plant.root.GetComponent<UPlayGround.Components.WorldPlacementMetadata>();
                    if (metadata != null)
                    {
                        Undo.RecordObject(metadata, "저작 식생 씬 구성 보존");
                        metadata.EditorSetBakeMode(UPlayGround.Components.WorldPlacementMetadata.PlacementBakeMode.SceneObject);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(metadata);
                    }
                    lap("metadata");
                    var reaction = plant.root.GetComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>();
                    var serialized = new SerializedObject(reaction);
                    var bindings = serialized.FindProperty("_surfaceBindings");
                    bindings.arraySize = plant.surfaces.Count;
                    var selected = new System.Collections.Generic.HashSet<Renderer>(plant.surfaces);
                    var excluded = new System.Collections.Generic.List<Renderer>();
                    foreach (var renderer in plant.root.GetComponentsInChildren<Renderer>(true))
                        if (!selected.Contains(renderer)) excluded.Add(renderer);
                    SetObjects(serialized.FindProperty("_excludedSurfaces"), excluded.ToArray());
                    for (int i = 0; i < plant.surfaces.Count; i++)
                    {
                        var renderer = plant.surfaces[i];
                        Undo.RecordObject(renderer, "식생 파생 재질 연결");
                        var slots = renderer.sharedMaterials;
                        for (int j = 0; j < slots.Length; j++)
                            if (materials.TryGetValue(slots[j], out var replacement)) slots[j] = replacement;
                        renderer.sharedMaterials = slots;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                        var binding = bindings.GetArrayElementAtIndex(i);
                        binding.FindPropertyRelative("renderer").objectReferenceValue = renderer;
                        var indices = binding.FindPropertyRelative("slots"); indices.arraySize = slots.Length;
                        for (int j = 0; j < slots.Length; j++) indices.GetArrayElementAtIndex(j).intValue = j;
                        binding.FindPropertyRelative("excludedSlots").arraySize = 0;
                    }
                    lap("renderer bindings");
                    var identity = plant.root.GetComponent<UPlayGround.Components.SceneEntityId>();
                    if (!identity.HasGuid) { Undo.RecordObject(identity, "최초 식생 배치 ID"); identity.EditorSetGuid(Guid.NewGuid().ToString("N")); }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(identity);
                    lap("identity");
                    // 기존 충돌은 보존하고 반응 입력 전용 trigger만 별도로 둔다.
                    var proxy = new GameObject("EnvironmentReactionHit");
                    Undo.RegisterCreatedObjectUndo(proxy, "식생 반응 입력 범위");
                    Undo.SetTransformParent(proxy.transform, plant.root.transform, "식생 입력 소유");
                    proxy.transform.localPosition = Vector3.zero;
                    proxy.transform.localRotation = Quaternion.identity;
                    proxy.transform.localScale = Vector3.one;
                    proxy.layer = LayerMask.NameToLayer("InteractableObject");
                    if (proxy.layer < 0) throw new InvalidOperationException("환경 피격 레이어가 없습니다.");
                    var collider = Undo.AddComponent<BoxCollider>(proxy);
                    collider.isTrigger = true;
                    Bounds local = LocalBounds(plant);
                    collider.center = local.center;
                    // 평면 메시도 물리 조회에 유효한 양의 부피를 갖게 한다.
                    collider.size = Vector3.Max(local.size, Vector3.one * MinimumInputThickness);
                    lap("proxy collider");
                    var hitColliders = new System.Collections.Generic.List<Collider>();
                    foreach (var candidate in plant.root.GetComponentsInChildren<Collider>(true))
                        if (candidate.GetComponentInParent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>(true) == reaction) hitColliders.Add(candidate);
                    SetObjects(serialized.FindProperty("_hitColliders"), hitColliders.ToArray());
                    lap("collider bindings");
                    var samples = plant.root.transform.Find("ReactionSamples");
                    Undo.RecordObject(samples, "식생 표면 샘플");
                    samples.localPosition = local.center;
                    SetObjects(serialized.FindProperty("_burnAnchors"), new Transform[] { samples });
                    lap("samples");
                    serialized.ApplyModifiedProperties();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(reaction);
                    lap("serialize");
                    changed++;
                }
                    lap("loop done");
                Undo.CollapseUndoOperations(group);
                return log.ToString();
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
} finally {
UnityEditor.Undo.RevertAllDownToGroup(outer);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.AssetDatabase.AllowAutoRefresh(); UnityEditor.EditorApplication.UnlockReloadAssemblies();
System.IO.File.WriteAllText("output/ReactiveEnvironment/VegetationConversion-20261114/authoring-cost.txt",log.ToString());
}
