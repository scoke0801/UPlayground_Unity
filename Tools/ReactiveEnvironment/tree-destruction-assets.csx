// Unity MCP execute_code로 실행한다. 구매 원본을 읽고 새 프로젝트 소유 잔해와 레시피만 생성한다.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling) return "BUSY";
var variants = new[] { "A", "B", "C", "C_Red", "D" };
var cuts = new[] { 1.22f, 1.05f, 1.9f, 1.9f, 2.1f };
var lengths = new[] { 16.3f, 13.7f, 21.4f, 21.4f, 20.8f };
var radii = new[] { 0.6f, 0.85f, 1.5f, 1.5f, 1.5f };
string source = "Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Foliage/Trees/";
string data = "Assets/10.Datas/Environment/TreeDestruction/";
string prefabs = "Assets/03.Prefabs/Environment/Reactive/TreeRemnants/";
System.IO.Directory.CreateDirectory(data); System.IO.Directory.CreateDirectory(prefabs);
UnityEditor.AssetDatabase.Refresh();
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.VegetationReactionCatalogSO>("Assets/10.Datas/Environment/Vegetation/Catalog.asset");
var mapping = catalog.surfaces.ToDictionary(s => s.original, s => s.reactive);
var original = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.ReactiveEnvironmentDefinitionSO>("Assets/10.Datas/Environment/Profiles/ArenaTree.asset");
var fall = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.EnvironmentDestructionRecipeSO>("Assets/10.Datas/Environment/Profiles/FarmTreeFall.asset");
var gathering = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/Reactive/PF_ReactiveFarmTree.prefab").GetComponent<UPlayGround.GatheringActor>().GetData();
var report = new System.Text.StringBuilder();
var created = new System.Collections.Generic.List<string>();
try
{
for (int index = 0; index < variants.Length; index++)
{
    string variant = variants[index]; string kind = variant == "C_Red" ? "C" : variant;
    string recipePath = data + "TreeFall_" + variant + ".asset";
    string definitionPath = data + "Tree_" + variant + ".asset";
    string remnantPath = prefabs + "TreeRemnant_" + variant + ".prefab";
    if (UnityEditor.AssetDatabase.LoadMainAssetAtPath(recipePath) != null || UnityEditor.AssetDatabase.LoadMainAssetAtPath(definitionPath) != null || UnityEditor.AssetDatabase.LoadMainAssetAtPath(remnantPath) != null)
        throw new System.InvalidOperationException("기존 파일 덮어쓰기 거부: " + variant);
    string fullPath = source + "PF_Farm_GenTree_" + kind + "_FullTree_01" + (variant == "C_Red" ? "_Red" : "") + ".prefab";
    var full = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
    var upper = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(source + "TreeParts/PF_Farm_GenTree_" + kind + "_Trunk_01.prefab");
    var lower = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(source + "TreeParts/PF_Farm_GenTree_" + kind + "_Stump_01.prefab");
    // 종류·LOD마다 슬롯 순서가 다르므로 원본 재질 참조로 연결한다.
    var materialMap = new System.Collections.Generic.Dictionary<Material, Material>(mapping);
    if (variant == "C_Red")
    {
        var close = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(UnityEditor.AssetDatabase.GUIDToAssetPath("8f12a94af1abc5a42b0bd7f6446cf24f"));
        var far = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(UnityEditor.AssetDatabase.GUIDToAssetPath("59c9140853894a949915d5f7c61f8685"));
        var redClose = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(UnityEditor.AssetDatabase.GUIDToAssetPath("45503504b4785644ebbdf66ed582a590"));
        var redFar = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(UnityEditor.AssetDatabase.GUIDToAssetPath("c0e1531a20f408145adefb353fb4f8b3"));
        materialMap[close] = mapping[redClose]; materialMap[far] = mapping[redFar];
    }
    var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
    GameObject root = null;
    try
    {
        root = new GameObject("TreeRemnant_" + variant);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        System.Func<string, Transform, Vector3, Transform> child = (name, parent, position) => {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position;
            return go.transform;
        };
        float offset = kind == "A" ? -0.5f : 0;
        var stump = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(lower, root.transform);
        stump.name = "Stump"; stump.transform.localPosition = Vector3.up * offset;
        var pivot = child("CutPivot", root.transform, Vector3.up * (cuts[index] + offset));
        var trunk = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(upper, pivot);
        trunk.name = "Trunk"; trunk.transform.localPosition = Vector3.down * cuts[index];
        foreach (var collider in trunk.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
        var capsule = pivot.gameObject.AddComponent<CapsuleCollider>();
        capsule.radius = radii[index]; capsule.height = lengths[index]; capsule.center = Vector3.up * (lengths[index] * 0.5f);
        var obstacle = pivot.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Capsule;
        obstacle.radius = radii[index]; obstacle.height = lengths[index]; obstacle.center = capsule.center;
        obstacle.carving = true; obstacle.carveOnlyStationary = true;
        var fallback = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(source + "PF_Farm_GenTree_Log_01.prefab"), root.transform);
        fallback.name = "BlockedLog"; fallback.transform.localScale = Vector3.one * 0.5f;
        var fallbackObstacle = fallback.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        fallbackObstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
        fallbackObstacle.size = new Vector3(3, 2, 7); fallbackObstacle.center = new Vector3(0, 1, 0);
        fallbackObstacle.carving = true;
        child("StumpSample", stump.transform, Vector3.up * cuts[index]);
        child("TrunkSample", pivot, Vector3.up * (lengths[index] * 0.5f));
        child("LogSample", fallback.transform, Vector3.up);
        child("Landing", pivot, Vector3.up * (lengths[index] - radii[index]));
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var slots = renderer.sharedMaterials;
            for (int slot = 0; slot < slots.Length; slot++)
            {
                Material reactive;
                if (!materialMap.TryGetValue(slots[slot], out reactive) && !mapping.TryGetValue(slots[slot], out reactive))
                    throw new System.InvalidOperationException("잔해 재질 대응 누락: " + slots[slot].name);
                slots[slot] = reactive;
            }
            renderer.sharedMaterials = slots;
        }
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            transform.gameObject.layer = LayerMask.NameToLayer("Ground");
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(transform.gameObject, 0);
        }
        fallback.SetActive(false); pivot.gameObject.SetActive(false); root.SetActive(false);
        if (UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, remnantPath) == null) throw new System.InvalidOperationException("잔해 저장 실패");
        created.Add(remnantPath);
        var recipe = UnityEngine.Object.Instantiate(fall); recipe.name = "TreeFall_" + variant;
        recipe.completionPolicy = UPlayGround.Data.ReactiveEnvironment.EnvironmentCompletionPolicy.ClearAfterSettlement;
        recipe.blockedCollapseSeconds = 0.35f; recipe.settlementSeconds = 0.1f; recipe.clearingSeconds = 0.45f;
        recipe.useLocalDimensions = true; recipe.trunkLength = lengths[index]; recipe.trunkRadius = radii[index];
        UnityEditor.AssetDatabase.CreateAsset(recipe, recipePath);
        created.Add(recipePath);
        var definition = UnityEngine.Object.Instantiate(original); definition.name = "Tree_" + variant;
        // 기존 저장은 지역+배치 GUID와 논리 정의 ID를 사용하므로 화학 계약의 ID를 유지한다.
        definition.reaction.canBreak = true; definition.destruction = recipe;
        definition.impactDrops = new System.Collections.Generic.List<UPlayGround.Data.Item.ItemDropList>(gathering.dropItems);
        UnityEditor.AssetDatabase.CreateAsset(definition, definitionPath);
        created.Add(definitionPath);
        UnityEditor.AssetDatabase.SaveAssetIfDirty(recipe); UnityEditor.AssetDatabase.SaveAssetIfDirty(definition);
        report.AppendLine(variant + "\t" + UnityEditor.AssetDatabase.AssetPathToGUID(fullPath) + "\t" + definitionPath + "\t" + remnantPath);
    }
    finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
}
}
catch
{
    foreach (string path in created) UnityEditor.AssetDatabase.DeleteAsset(path);
    throw;
}
System.IO.Directory.CreateDirectory("output/ReactiveEnvironment/TreeDestruction-20261115");
System.IO.File.WriteAllText("output/ReactiveEnvironment/TreeDestruction-20261115/assets.tsv", report.ToString());
return report.ToString();
