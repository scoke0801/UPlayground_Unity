// Unity MCP execute_code에서 새 배치용 프리팹과 설정만 생성한다.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling
    || UnityEditor.EditorApplication.isUpdating || UnityEditor.EditorUtility.scriptCompilationFailed) return "BUSY";

const string farm = "Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/";
const string environment = "Assets/03.Prefabs/Environment/";
const string data = "Assets/10.Datas/Environment/";
const string propFolder = environment + "ReactiveProps";
var targets = new[] {
    new { name = "FarmRedTree", source = "Foliage/Trees/PF_Farm_GenTree_C_FullTree_01_Red.prefab", tree = true, wood = true, radius = 1.5f, height = 23.3f },
    new { name = "FarmBarrel", source = "Props/OutdoorProps/PF_Farm_Barrel_01.prefab", tree = false, wood = true, radius = 0.45f, height = 1.16f },
    new { name = "FarmVase", source = "Props/Interior/LivingRoom/PF_Farm_Vase_01.prefab", tree = false, wood = false, radius = 0.17f, height = 0.48f }
};
var red = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.ReactiveEnvironmentDefinitionSO>(data + "TreeDestruction/Tree_C_Red.asset");
var ceramic = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.ReactiveEnvironmentDefinitionSO>(data + "Profiles/CargoCrate.asset");
var redRemnant = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(environment + "Reactive/TreeRemnants/TreeRemnant_C_Red.prefab");
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.VegetationReactionCatalogSO>(data + "Vegetation/Catalog.asset");
var models = targets.Select(target=>UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(farm + target.source)).ToArray();
int ground = LayerMask.NameToLayer("Ground");
int hitLayer = LayerMask.NameToLayer("InteractableObject");
if (ground < 0 || hitLayer < 0 || models.Any(model=>model == null) || redRemnant == null
    || red == null || red.presentation == null || ceramic == null || catalog == null)
    throw new System.InvalidOperationException("저작 원본 또는 레이어 누락");
var reactive = catalog.surfaces.ToDictionary(entry=>entry.original, entry=>entry.reactive);
var created = new System.Collections.Generic.List<string>();
var report = new System.Collections.Generic.List<object>();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
System.Action<UnityEditor.SerializedProperty, UnityEngine.Object[]> refs = (property, objects) => {
    property.arraySize = objects.Length;
    for (int i=0; i<objects.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
};
System.Func<string, Transform, Vector3, Transform> child = (name, parent, position) => {
    var node = new GameObject(name).transform;
    node.SetParent(parent, false);
    node.localPosition = position;
    return node;
};
System.Action<UnityEngine.Object,string> writeAsset = (asset,path) => {
    if (System.IO.File.Exists(path) || UnityEditor.AssetDatabase.LoadMainAssetAtPath(path) != null)
        throw new System.InvalidOperationException("기존 에셋 덮어쓰기 방지 " + path);
    UnityEditor.AssetDatabase.CreateAsset(asset,path);
    created.Add(path);
};
var dependencies = UnityEditor.AssetDatabase.GetDependencies(models.Concat(new[] { redRemnant })
    .Select(UnityEditor.AssetDatabase.GetAssetPath).ToArray(), true)
    .Where(path=>path.StartsWith("Assets/ExternalAssets/", System.StringComparison.Ordinal))
    .SelectMany(path=>new[] { path, path + ".meta" }).Where(System.IO.File.Exists).ToArray();
System.Func<string,string> hash = path => {
    using (var algorithm = System.Security.Cryptography.SHA256.Create())
    using (var stream = System.IO.File.OpenRead(path)) return System.Convert.ToBase64String(algorithm.ComputeHash(stream));
};
var before = dependencies.ToDictionary(path=>path,hash);
try
{
    if (!UnityEditor.AssetDatabase.IsValidFolder(propFolder))
    {
        if (string.IsNullOrEmpty(UnityEditor.AssetDatabase.CreateFolder("Assets/03.Prefabs/Environment","ReactiveProps")))
            throw new System.InvalidOperationException("소품 프리팹 폴더 생성 실패");
        created.Add(propFolder);
    }
    var swap = ScriptableObject.CreateInstance<UPlayGround.Data.ReactiveEnvironment.EnvironmentDestructionRecipeSO>();
    swap.name = "FragilePropSwap";
    swap.completionPolicy = UPlayGround.Data.ReactiveEnvironment.EnvironmentCompletionPolicy.ClearAfterSettlement;
    swap.trunkLength = 0.6f;
    writeAsset(swap,data + "Profiles/FragilePropSwap.asset");
    var makeMaterial = typeof(UPlayGround.EditorTools.VegetationReactionCatalogBuilder).GetMethod(
        "CreateMaterialCopy",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
    for (int index=0; index<targets.Length; index++)
    {
        var variant = targets[index];
        string prefabPath = (variant.tree ? environment + "ReactiveTrees" : propFolder) + "/PF_Reactive" + variant.name + ".prefab";
        if (System.IO.File.Exists(prefabPath) || UnityEditor.AssetDatabase.LoadMainAssetAtPath(prefabPath)!=null)
            throw new System.InvalidOperationException("기존 프리팹 덮어쓰기 방지 " + prefabPath);
        var profile = UnityEngine.Object.Instantiate(variant.wood ? red : ceramic);
        profile.name = variant.name;
        profile.definitionId = "environment." + variant.name;
        if (!variant.tree)
        {
            profile.reaction.canBreak = true;
            profile.reaction.maxDurability = variant.wood ? 50 : 25;
            profile.reaction.initialFuel = variant.wood ? 12 : 0;
            profile.reaction.bluntMultiplier = 1;
            profile.spreadRadius = variant.wood ? 2 : 0;
            profile.destruction = swap;
            if (variant.wood) profile.impactDrops.Single().maximumDropCount = 1;
            var presentation = UnityEngine.Object.Instantiate(red.presentation);
            presentation.name = variant.name + "Presentation";
            presentation.leaves = null;
            presentation.effectScale = variant.wood ? 0.55f : 0.25f;
            presentation.woundRadius = variant.wood ? 0.15f : 0;
            if (!variant.wood)
            {
                presentation.impact = null;
                presentation.impactSound = null;
                presentation.clearingChips = null;
                presentation.clearingSound = null;
            }
            profile.presentation = presentation;
            writeAsset(presentation,data + "Profiles/" + presentation.name + ".asset");
        }
        if (!profile.IsValid()) throw new System.InvalidOperationException("반응 정의 오류 " + variant.name);
        string profilePath = data + (variant.tree ? "TreeDestruction/" : "Profiles/") + variant.name + ".asset";
        writeAsset(profile,profilePath);

        var root = new GameObject("PF_Reactive" + variant.name);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
        var intact = child("Intact",root.transform,Vector3.zero);
        var visual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(models[index],intact);
        var physical = visual.GetComponentsInChildren<Collider>(true).Where(collider=>!collider.isTrigger).ToArray();
        if (physical.Length != 1) throw new System.InvalidOperationException("구매 원형 Collider 구성 변경 " + variant.name);
        var hit = child("Hit",intact,Vector3.zero).gameObject.AddComponent<CapsuleCollider>();
        hit.isTrigger = true;
        hit.radius = variant.radius;
        hit.height = variant.height;
        hit.center = Vector3.up * (hit.height * 0.5f);
        foreach (var node in intact.GetComponentsInChildren<Transform>(true))
        {
            node.gameObject.layer = node == hit.transform ? hitLayer : ground;
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(node.gameObject,0);
        }
        var standingSample = child("SurfaceSample",intact,Vector3.up * (variant.tree ? 1.9f : variant.height*0.65f));
        var remnant = variant.tree ? (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(redRemnant,root.transform)
            : child("Remnant",root.transform,Vector3.zero).gameObject;
        remnant.name = "Remnant";
        if (variant.tree)
            UnityEditor.PrefabUtility.UnpackPrefabInstance(remnant,UnityEditor.PrefabUnpackMode.OutermostRoot,UnityEditor.InteractionMode.AutomatedAction);
        var pivot = remnant.transform.Find("CutPivot");
        var fallback = remnant.transform.Find("BlockedLog");
        var samples = variant.tree ? new[] { standingSample, remnant.transform.Find("Stump/StumpSample"),
            pivot.Find("TrunkSample"), fallback.Find("LogSample") } : new[] { standingSample };
        var destruction = root.AddComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentDestruction>();
        var structure = new UnityEditor.SerializedObject(destruction);
        structure.FindProperty("_fallPivot").objectReferenceValue = pivot;
        structure.FindProperty("_blockedFallback").objectReferenceValue = fallback == null ? null : fallback.gameObject;
        refs(structure.FindProperty("_shelterColliders"),physical);
        refs(structure.FindProperty("_remnantObstacles"),remnant.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>(true));
        structure.ApplyModifiedPropertiesWithoutUndo();

        foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            var slots = renderer.sharedMaterials;
            for (int slot=0; slot<slots.Length; slot++)
            {
                var original = slots[slot];
                Material mapped;
                if (!reactive.TryGetValue(original,out mapped))
                {
                    string materialGuid; long localId;
                    if (!UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original,out materialGuid,out localId))
                        throw new System.InvalidOperationException("원본 재질 식별자 누락");
                    string shaderId = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(original.shader));
                    var shader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/06.Shaders/Environment/Vegetation/" + shaderId + ".shader");
                    if (shader == null || UnityEditor.ShaderUtil.ShaderHasError(shader))
                        throw new System.InvalidOperationException("호환 셰이더 오류 " + original.shader.name);
                    string path = data + "Vegetation/Materials/" + materialGuid + "_" + localId + ".mat";
                    mapped = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mapped == null)
                    {
                        mapped = (Material)makeMaterial.Invoke(null,new object[] { original,shader });
                        writeAsset(mapped,path);
                    }
                    reactive.Add(original,mapped);
                }
                slots[slot] = mapped;
            }
            renderer.sharedMaterials = slots;
        }
        root.AddComponent<UPlayGround.Components.SceneEntityId>().EditorSetGuid("");
        var target = root.AddComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>();
        var settings = new UnityEditor.SerializedObject(target);
        settings.FindProperty("_definition").objectReferenceValue = profile;
        settings.FindProperty("_intact").objectReferenceValue = intact.gameObject;
        settings.FindProperty("_remnant").objectReferenceValue = remnant;
        settings.FindProperty("_impactAnchor").objectReferenceValue = standingSample;
        if (pivot != null) settings.FindProperty("_landingAnchor").objectReferenceValue = pivot.Find("Landing");
        refs(settings.FindProperty("_hitColliders"),new[] { hit });
        foreach(string field in new[] { "_rainSamples","_heatSamples","_burnAnchors" }) refs(settings.FindProperty(field),samples);
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        var bindings = settings.FindProperty("_surfaceBindings");
        bindings.arraySize = renderers.Length;
        for (int i=0;i<renderers.Length;i++)
        {
            var binding = bindings.GetArrayElementAtIndex(i);
            binding.FindPropertyRelative("renderer").objectReferenceValue = renderers[i];
            var slots = binding.FindPropertyRelative("slots");
            slots.arraySize = renderers[i].sharedMaterials.Length;
            for (int slot=0;slot<slots.arraySize;slot++) slots.GetArrayElementAtIndex(slot).intValue = slot;
            binding.FindPropertyRelative("excludedSlots").arraySize = 0;
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
        var validate = UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentSurface.Validate(
            root.transform,UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentSurface.BindAll(renderers),null);
        if (validate != null) throw new System.InvalidOperationException(validate);
        remnant.SetActive(false);
        var saved = UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
        if (saved == null) throw new System.InvalidOperationException("프리팹 저장 오류 " + prefabPath);
        created.Add(prefabPath);
        if (saved.GetComponent<UPlayGround.Components.SceneEntityId>().HasGuid)
            throw new System.InvalidOperationException("프리팹 배치 GUID가 비어 있지 않습니다.");
        report.Add(new { prefabPath, profilePath, lodRenderers = renderers.Length });
        UnityEngine.Object.DestroyImmediate(root);
    }
    if (before.Any(pair=>hash(pair.Key)!=pair.Value)) throw new System.InvalidOperationException("구매 원본 변경 감지");
    return new { prefabs = report, sourceFilesChecked = before.Count, created = created.Count };
}
catch
{
    for(int i=created.Count-1;i>=0;i--) UnityEditor.AssetDatabase.DeleteAsset(created[i]);
    throw;
}
finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
