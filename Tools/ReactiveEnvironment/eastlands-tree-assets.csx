// Unity MCP execute_code에서 실행한다. 구매 원본과 기존 에셋은 읽기 전용이다.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling
    || UnityEditor.EditorApplication.isUpdating || UnityEditor.EditorUtility.scriptCompilationFailed) return "BUSY";

const string sourceRoot = "Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/";
const string dataRoot = "Assets/10.Datas/Environment/TreeDestruction/";
const string prefabFolder = "Assets/03.Prefabs/Environment/ReactiveTrees";
const string materialFolder = "Assets/10.Datas/Environment/Vegetation/Materials/";
var candidates = new[] {
    new { name = "EastLandsConiferStraight", source = "Conifers/PF_EastLands_Conifer_Straight_01",
        cut = new Vector3(-0.21f, 1.927f, -0.45f), length = 22.25f, radius = 0.75f, logScale = 0.4f },
    new { name = "EastLandsConiferTwisted", source = "Conifers/PF_EastLands_Conifer_Twisted_01",
        cut = new Vector3(-0.095f, 2.078f, -0.97f), length = 18.85f, radius = 1.4f, logScale = 0.5f },
    new { name = "EastLandsBambooTall", source = "Bamboos/PF_EastLands_Bamboo_Tall_01",
        cut = new Vector3(0, 2.808f, -0.035f), length = 17.15f, radius = 0.26f, logScale = 0f },
    new { name = "EastLandsOsmanthus", source = "Osmanthus/PF_EastLands_Osmanthus_01",
        cut = new Vector3(-0.147f, 1.539f, -0.221f), length = 14.72f, radius = 0.59f, logScale = 0.34f },
    new { name = "EastLandsBambooGroup", source = "Bamboos/PF_EastLands_Bamboo_Group_01",
        cut = new Vector3(-0.06f, 1.818f, -0.04f), length = 15.97f, radius = 0.92f, logScale = 0f },
    new { name = "EastLandsBambooSmall", source = "Bamboos/PF_EastLands_Bamboo_Small_01",
        cut = new Vector3(-0.095f, 1.294f, -0.065f), length = 7.74f, radius = 0.11f, logScale = 0f },
    new { name = "EastLandsBambooMedium01", source = "Bamboos/PF_EastLands_Bamboo_Medium_01",
        cut = new Vector3(0.021f, 1.192f, -0.013f), length = 14.19f, radius = 0.17f, logScale = 0f },
    new { name = "EastLandsBambooMedium02", source = "Bamboos/PF_EastLands_Bamboo_Medium_02",
        cut = new Vector3(-0.032f, 2.094f, 0.006f), length = 12.63f, radius = 0.15f, logScale = 0f },
    new { name = "EastLandsBambooSmall02", source = "Bamboos/PF_EastLands_Bamboo_Small_02",
        cut = new Vector3(0.078f, 0.866f, 0.018f), length = 2.59f, radius = 0.07f, logScale = 0f },
    new { name = "EastLandsBambooTall02", source = "Bamboos/PF_EastLands_Bamboo_Tall_02",
        cut = new Vector3(-0.059f, 1.993f, 0.031f), length = 17.45f, radius = 0.18f, logScale = 0f },
    new { name = "EastLandsConiferStraight02", source = "Conifers/PF_EastLands_Conifer_Straight_02",
        cut = new Vector3(0.046f, 2.445f, -0.175f), length = 17.43f, radius = 0.61f, logScale = 0.35f },
    new { name = "EastLandsConiferTwisted02", source = "Conifers/PF_EastLands_Conifer_Twisted_02",
        cut = new Vector3(-0.214f, 1.713f, -0.226f), length = 13.11f, radius = 0.84f, logScale = 0.35f },
    new { name = "EastLandsConiferMulti01", source = "Conifers/PF_EastLands_Conifer_Multi_01",
        cut = new Vector3(0.469f, 2.617f, -0.411f), length = 22.65f, radius = 1.72f, logScale = 0.55f },
    new { name = "EastLandsConiferMulti02", source = "Conifers/PF_EastLands_Conifer_Multi_02",
        cut = new Vector3(-0.181f, 2.132f, 0.074f), length = 17.73f, radius = 1.28f, logScale = 0.45f },
    new { name = "EastLandsOsmanthus02", source = "Osmanthus/PF_EastLands_Osmanthus_02",
        cut = new Vector3(0.028f, 0.987f, -0.013f), length = 12.73f, radius = 0.56f, logScale = 0.25f },
    new { name = "EastLandsOsmanthus03", source = "Osmanthus/PF_EastLands_Osmanthus_03",
        cut = new Vector3(0.345f, 1.728f, -0.080f), length = 13.23f, radius = 0.79f, logScale = 0.28f },
    new { name = "EastLandsOsmanthus04", source = "Osmanthus/PF_EastLands_Osmanthus_04",
        cut = new Vector3(0.085f, 0.702f, -0.172f), length = 9.67f, radius = 0.73f, logScale = 0.24f }
};
var variants = candidates.Where(candidate => {
    string name = candidate.name;
    var paths = new[] { prefabFolder + "/PF_Reactive" + name + ".prefab",
        dataRoot + "Tree_" + name + ".asset", dataRoot + "TreeFall_" + name + ".asset" };
    int existing = paths.Count(path => System.IO.File.Exists(path) || UnityEditor.AssetDatabase.LoadMainAssetAtPath(path) != null);
    if (existing != 0 && existing != paths.Length) throw new System.InvalidOperationException("같은 이름의 일부 에셋이 이미 있습니다: " + name);
    return existing == 0;
}).ToArray();
if (variants.Length == 0) return "모든 타입이 이미 저작돼 있습니다.";
var definitionTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.ReactiveEnvironmentDefinitionSO>(dataRoot + "Tree_A.asset");
var fallbackModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(sourceRoot + "Props/Logs/PF_EastLands_Log_Medium.prefab");
if (definitionTemplate == null || !definitionTemplate.IsValid() || fallbackModel == null)
    throw new System.InvalidOperationException("기존 나무 정의 또는 대체 통나무가 누락됐습니다.");
int groundLayer = LayerMask.NameToLayer("Ground");
int hitLayer = LayerMask.NameToLayer("InteractableObject");
if (groundLayer < 0 || hitLayer < 0) throw new System.InvalidOperationException("나무 충돌 레이어가 누락됐습니다.");
var sources = variants.Select(variant => {
    string[] source = variant.source.Split('/');
    string full = sourceRoot + "Foliage/Trees/" + variant.source;
    string parts = sourceRoot + "Foliage/Trees/" + source[0] + "/Trunks and Stumps/" + source[1];
    var trunkPaths = System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(parts),
        System.IO.Path.GetFileName(parts) + "_Trunk*.prefab").Select(path => path.Replace('\\', '/'))
        .OrderBy(path => path, System.StringComparer.Ordinal).ToArray();
    var models = new[] { full + ".prefab", parts + "_Stump.prefab" }.Concat(trunkPaths)
        .Select(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
    if (models.Length < 3 || models.Any(model => model == null)) throw new System.InvalidOperationException("원형/절단 모델 누락 " + full);
    return models;
}).ToArray();
var dependencies = UnityEditor.AssetDatabase.GetDependencies(sources.SelectMany(models => models)
    .Concat(new[] { fallbackModel }).Select(UnityEditor.AssetDatabase.GetAssetPath).ToArray(), true)
    .Where(path => path.StartsWith("Assets/ExternalAssets/", System.StringComparison.Ordinal))
    .SelectMany(path => new[] { path, path + ".meta" }).Where(System.IO.File.Exists).ToArray();
System.Func<string, string> hash = path => {
    using (var algorithm = System.Security.Cryptography.SHA256.Create())
    using (var stream = System.IO.File.OpenRead(path))
        return System.Convert.ToBase64String(algorithm.ComputeHash(stream));
};
var sourceHashes = dependencies.ToDictionary(path => path, hash);
var created = new System.Collections.Generic.List<string>();
var materials = new System.Collections.Generic.Dictionary<Material, Material>();
var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var report = new System.Collections.Generic.List<object>();
System.Func<string, Transform, Vector3, Transform> child = (name, parent, position) => {
    var node = new GameObject(name);
    node.transform.SetParent(parent, false);
    node.transform.localPosition = position;
    return node.transform;
};
System.Action<UnityEditor.SerializedProperty, UnityEngine.Object[]> setRefs = (property, values) => {
    property.arraySize = values.Length;
    for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
};
try
{
    if (!UnityEditor.AssetDatabase.IsValidFolder(prefabFolder))
    {
        if (string.IsNullOrEmpty(UnityEditor.AssetDatabase.CreateFolder("Assets/03.Prefabs/Environment", "ReactiveTrees")))
            throw new System.InvalidOperationException("프리팹 폴더 생성 실패");
        created.Add(prefabFolder);
    }
    var materialCopy = typeof(UPlayGround.EditorTools.VegetationReactionCatalogBuilder).GetMethod("CreateMaterialCopy",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
    for (int index = 0; index < variants.Length; index++)
    {
        var variant = variants[index];
        var recipe = UnityEngine.Object.Instantiate(definitionTemplate.destruction);
        recipe.name = "TreeFall_" + variant.name;
        recipe.trunkLength = variant.length;
        recipe.trunkRadius = variant.radius;
        recipe.changesNavigation = true;
        string recipePath = dataRoot + recipe.name + ".asset";
        UnityEditor.AssetDatabase.CreateAsset(recipe, recipePath);
        created.Add(recipePath);
        var definition = UnityEngine.Object.Instantiate(definitionTemplate);
        definition.name = "Tree_" + variant.name;
        definition.definitionId = "environment." + variant.name;
        definition.destruction = recipe;
        if (!definition.IsValid()) throw new System.InvalidOperationException(definition.name + " 정의 오류");
        string definitionPath = dataRoot + definition.name + ".asset";
        UnityEditor.AssetDatabase.CreateAsset(definition, definitionPath);
        created.Add(definitionPath);

        var root = new GameObject("PF_Reactive" + variant.name);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
        var intact = child("Intact", root.transform, Vector3.zero);
        var standing = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(sources[index][0], intact);
        var physicalColliders = standing.GetComponentsInChildren<CapsuleCollider>(true);
        if (physicalColliders.Length == 0 || physicalColliders.Any(collider =>
            standing.transform.worldToLocalMatrix * collider.transform.localToWorldMatrix != Matrix4x4.identity || collider.direction != 1))
            throw new System.InvalidOperationException("원형 줄기 Collider가 변경됐습니다.");
        var hitNode = child("TrunkHit", intact, Vector3.zero);
        var hits = new System.Collections.Generic.List<CapsuleCollider>();
        var standingColliders = new System.Collections.Generic.List<CapsuleCollider>();
        var obstacles = new System.Collections.Generic.List<UnityEngine.AI.NavMeshObstacle>();
        foreach (var physical in physicalColliders)
        {
            float hitTop = physical.center.y + physical.height * 0.5f;
            var hit = hitNode.gameObject.AddComponent<CapsuleCollider>();
            hit.isTrigger = true;
            hit.radius = physical.radius;
            hit.height = Mathf.Max(physical.radius * 2, hitTop);
            hit.center = new Vector3(physical.center.x, hit.height * 0.5f, physical.center.z);
            hits.Add(hit);
            var physicalNode = child("StandingTrunk_" + hits.Count, intact, Vector3.zero).gameObject;
            var obstacle = physicalNode.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Capsule;
            obstacle.radius = hit.radius;
            obstacle.height = hit.height;
            obstacle.center = hit.center;
            obstacle.carving = true;
            obstacles.Add(obstacle);
            // 근거리 LOD의 활성 상태와 관계없이 물리 충돌을 유지한다.
            var standingCollider = physicalNode.AddComponent<CapsuleCollider>();
            standingCollider.center = physical.center;
            standingCollider.radius = physical.radius;
            standingCollider.height = physical.height;
            standingColliders.Add(standingCollider);
            physical.enabled = false;
        }

        var remnant = child("Remnant", root.transform, Vector3.zero);
        var stump = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(sources[index][1], remnant);
        var pivot = child("CutPivot", remnant, variant.cut);
        // 벌어진 줄기들은 제자리에서 조립하고 같은 파괴 상태로 넘어간다.
        foreach (var trunkModel in sources[index].Skip(2))
        {
            var trunk = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(trunkModel, pivot);
            trunk.transform.localPosition -= variant.cut;
        }
        var fallback = child("BlockedLog", remnant, Vector3.zero);
        // 대나무는 막힘 때 구매 밑동의 절단 형상만 남겨 마무리한다.
        if (variant.logScale > 0)
        {
            var log = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(fallbackModel, fallback);
            log.transform.localScale = Vector3.one * variant.logScale;
            log.transform.localPosition = Vector3.up * (1.184f * variant.logScale);
        }
        foreach (var collider in remnant.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        var standingSample = child("StandingSample", intact, variant.cut);
        var stumpSample = child("CutSample", remnant, variant.cut);
        var trunkSample = child("TrunkSample", pivot, Vector3.up * (variant.length * 0.5f));
        var landing = child("Landing", pivot, Vector3.up * (variant.length - variant.radius));
        var samples = new[] { standingSample, stumpSample, trunkSample };

        foreach (var node in root.GetComponentsInChildren<Transform>(true))
        {
            node.gameObject.layer = node == hitNode ? hitLayer : groundLayer;
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(node.gameObject, 0);
        }
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            var slots = renderer.sharedMaterials;
            for (int slot = 0; slot < slots.Length; slot++)
            {
                var original = slots[slot];
                Material reactive;
                if (!materials.TryGetValue(original, out reactive))
                {
                    string guid;
                    long localId;
                    if (!UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original, out guid, out localId))
                        throw new System.InvalidOperationException("재질 ID가 누락됐습니다.");
                    string shaderGuid = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(original.shader));
                    var shader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(
                        UPlayGround.EditorTools.VegetationReactionShaderAuthoring.OutputFolder + "/" + shaderGuid + ".shader");
                    if (shader == null || UnityEditor.ShaderUtil.ShaderHasError(shader))
                        throw new System.InvalidOperationException("반응 셰이더 누락 또는 컴파일 오류 " + original.shader.name);
                    string path = materialFolder + guid + "_" + localId + ".mat";
                    reactive = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (reactive == null)
                    {
                        if (System.IO.File.Exists(path)) throw new System.InvalidOperationException("기존 재질 경로 충돌 " + path);
                        reactive = (Material)materialCopy.Invoke(null, new object[] { original, shader });
                        UnityEditor.AssetDatabase.CreateAsset(reactive, path);
                        created.Add(path);
                    }
                    if (reactive.shader != shader || !UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentSurface.SupportsReaction(reactive))
                        throw new System.InvalidOperationException("기존 반응 재질이 예상 계약과 다릅니다 " + path);
                    materials.Add(original, reactive);
                }
                slots[slot] = reactive;
            }
            renderer.sharedMaterials = slots;
        }

        root.AddComponent<UPlayGround.Components.SceneEntityId>().EditorSetGuid("");
        var destruction = root.AddComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentDestruction>();
        var structure = new UnityEditor.SerializedObject(destruction);
        structure.FindProperty("_fallPivot").objectReferenceValue = pivot;
        structure.FindProperty("_blockedFallback").objectReferenceValue = fallback.gameObject;
        setRefs(structure.FindProperty("_shelterColliders"), standingColliders.ToArray());
        setRefs(structure.FindProperty("_standingObstacles"), obstacles.ToArray());
        structure.ApplyModifiedPropertiesWithoutUndo();
        var target = root.AddComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>();
        var serialized = new UnityEditor.SerializedObject(target);
        serialized.FindProperty("_definition").objectReferenceValue = definition;
        serialized.FindProperty("_intact").objectReferenceValue = intact.gameObject;
        serialized.FindProperty("_remnant").objectReferenceValue = remnant.gameObject;
        serialized.FindProperty("_impactAnchor").objectReferenceValue = standingSample;
        serialized.FindProperty("_landingAnchor").objectReferenceValue = landing;
        setRefs(serialized.FindProperty("_hitColliders"), hits.ToArray());
        foreach (string field in new[] { "_rainSamples", "_heatSamples", "_burnAnchors" })
            setRefs(serialized.FindProperty(field), samples);
        var bindings = serialized.FindProperty("_surfaceBindings");
        bindings.arraySize = renderers.Length;
        for (int i = 0; i < renderers.Length; i++)
        {
            var binding = bindings.GetArrayElementAtIndex(i);
            binding.FindPropertyRelative("renderer").objectReferenceValue = renderers[i];
            var slots = binding.FindPropertyRelative("slots");
            slots.arraySize = renderers[i].sharedMaterials.Length;
            for (int slot = 0; slot < slots.arraySize; slot++) slots.GetArrayElementAtIndex(slot).intValue = slot;
            binding.FindPropertyRelative("excludedSlots").arraySize = 0;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        fallback.gameObject.SetActive(false);
        pivot.gameObject.SetActive(false);
        remnant.gameObject.SetActive(false);
        string prefabPath = prefabFolder + "/" + root.name + ".prefab";
        var saved = UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        if (saved == null) throw new System.InvalidOperationException("프리팹 저장 실패 " + prefabPath);
        created.Add(prefabPath);
        if (saved.GetComponent<UPlayGround.Components.SceneEntityId>().HasGuid)
            throw new System.InvalidOperationException("프리팹에 배치 ID가 직렬화됐습니다.");
        report.Add(new { prefabPath, definitionPath, recipePath, renderers = renderers.Length, trunkParts = sources[index].Length - 2 });
        UnityEngine.Object.DestroyImmediate(root);
    }
    if (sourceHashes.Any(pair => hash(pair.Key) != pair.Value))
        throw new System.InvalidOperationException("구매 원본 해시가 변경됐습니다.");
    return new { trees = report, materialCopies = materials.Count, sourceFilesChecked = sourceHashes.Count };
}
catch
{
    for (int i = created.Count - 1; i >= 0; i--) UnityEditor.AssetDatabase.DeleteAsset(created[i]);
    throw;
}
finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview); }
