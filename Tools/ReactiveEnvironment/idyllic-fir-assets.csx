// Blender 절단 FBX와 원본 LOD 재질을 연결하는 Unity MCP 저작 단계.
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isCompiling
    || UnityEditor.EditorApplication.isUpdating || UnityEditor.EditorUtility.scriptCompilationFailed) return "BUSY";
const string role = "IdyllicFir02";
const string modelFolder = "Assets/08.Models/ReactiveTrees/";
const string dataFolder = "Assets/10.Datas/Environment/TreeDestruction/";
const string materialsFolder = "Assets/10.Datas/Environment/Vegetation/Materials/";
const string prefabPath = "Assets/03.Prefabs/Environment/ReactiveTrees/PF_ReactiveIdyllicFir02.prefab";
var source = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/Fir_02.prefab");
var model = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(modelFolder + "Fir02_FallParts.fbx");
var grain = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(modelFolder + "Fir02_EndGrain.png");
var template = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.ReactiveEnvironment.ReactiveEnvironmentDefinitionSO>(dataFolder + "Tree_A.asset");
if (source == null || model == null || grain == null || template == null || System.IO.File.Exists(prefabPath))
    throw new System.InvalidOperationException("Fir 원본·절단 데이터 누락 또는 기존 출력 경로 충돌");
var originalLod = source.GetComponentInChildren<LODGroup>();
var levels = originalLod.GetLODs();
var renderParts = model.GetComponentsInChildren<MeshRenderer>(true).ToDictionary(renderer=>renderer.name);
if (levels.Length != 3 || renderParts.Count != 6) throw new System.InvalidOperationException("LOD·파손 메시 수 불일치");
var created = new System.Collections.Generic.List<string>();
var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
System.Action<UnityEngine.Object,string> write = (asset,path) => {
    if (System.IO.File.Exists(path) || UnityEditor.AssetDatabase.LoadMainAssetAtPath(path) != null)
        throw new System.InvalidOperationException("기존 에셋 덮어쓰기 방지 " + path);
    UnityEditor.AssetDatabase.CreateAsset(asset,path);
    created.Add(path);
};
System.Func<Transform,string,Vector3,Transform> child = (parent,name,position) => {
    var obj = new GameObject(name);
    obj.transform.SetParent(parent,false);
    obj.transform.localPosition = position;
    return obj.transform;
};
System.Action<UnityEditor.SerializedProperty,UnityEngine.Object[]> refs=(property,objects)=>{
    property.arraySize=objects.Length;
    for(int i=0;i<objects.Length;i++) property.GetArrayElementAtIndex(i).objectReferenceValue=objects[i];
};
try
{
    var copy = typeof(UPlayGround.EditorTools.VegetationReactionCatalogBuilder).GetMethod("CreateMaterialCopy",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
    var originalMaterials=levels[0].renderers.Single().sharedMaterials;
    if(levels.Any(l=>!l.renderers.Single().sharedMaterials.SequenceEqual(originalMaterials)))
        throw new System.InvalidOperationException("원본 Fir LOD의 재질 순서 불일치");
    var materials = originalMaterials.Select(original=>{
        string key = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(original.shader));
        var shader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/06.Shaders/Environment/Vegetation/"+key+".shader");
        if(shader == null || UnityEditor.ShaderUtil.ShaderHasError(shader))
            throw new System.InvalidOperationException("반응 셰이더 오류 "+original.shader.name);
        string materialGuid;
        long local;
        if(!UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original,out materialGuid,out local))
            throw new System.InvalidOperationException("원본 Fir 재질 식별자 누락");
        string path=materialsFolder+materialGuid+"_"+local+".mat";
        var material=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material == null) {
            material=(Material)copy.Invoke(null,new object[]{original,shader});
            write(material,path);
        }
        if(material.shader!=shader) throw new System.InvalidOperationException("프로젝트 재질 셰이더 불일치 "+path);
        return material;
    }).ToArray();
    // 방향성 낙하의 강체 포즈에서 원본 지면 기준 바람이 잎 카드에 과도하게 적용된다.
    var remnantLeaves = new Material(materials[0]);
    remnantLeaves.SetFloat("_Wind_Strength",0);
    remnantLeaves.SetFloat("_Bend_Strength",0);
    write(remnantLeaves,materialsFolder+"Fir02_RemnantLeaves.mat");
    var cap = new Material(Shader.Find("UPlayGround/Environment/Reactive Toon"));
    cap.SetTexture("_BaseMap",grain);
    cap.SetFloat("_SurfaceRole",2);
    cap.SetFloat("_CrackOpacity",0);
    cap.SetFloat("_OutlineWidth",0);
    write(cap,materialsFolder+"Fir02_EndGrain.mat");
    var recipe = UnityEngine.Object.Instantiate(template.destruction);
    recipe.name="TreeFall_"+role;
    recipe.trunkLength=8.05f;
    recipe.trunkRadius=0.39f;
    recipe.changesNavigation=true;
    write(recipe,dataFolder+recipe.name+".asset");
    var profile=UnityEngine.Object.Instantiate(template);
    profile.name="Tree_"+role;
    profile.definitionId="environment."+role;
    profile.destruction=recipe;
    if(!profile.IsValid()) throw new System.InvalidOperationException("Fir 파괴 정의 오류");
    write(profile,dataFolder+profile.name+".asset");
    var root=new GameObject("PF_Reactive"+role);
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
    var intact=child(root.transform,"Intact",Vector3.zero);
    var standing=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source,intact);
    foreach(var original in standing.GetComponentsInChildren<MeshRenderer>(true)) original.sharedMaterials=materials;
    var physical=standing.GetComponentsInChildren<CapsuleCollider>(true).Single();
    var hit=child(intact,"TrunkHit",Vector3.zero).gameObject.AddComponent<CapsuleCollider>();
    hit.isTrigger=true;
    hit.radius=physical.radius;
    hit.height=physical.height;
    hit.center=physical.transform.TransformPoint(physical.center);
    if(physical.direction!=1 || physical.transform.lossyScale!=Vector3.one)
        throw new System.InvalidOperationException("원본 피격 Capsule 축·배율 불일치");
    var remnant=child(root.transform,"Remnant",Vector3.zero);
    const float cut=1.08f;
    var pivot=child(remnant,"CutPivot",Vector3.up*cut);
    var stump=child(remnant,"Stump",Vector3.zero);
    var upper=child(pivot,"Upper",Vector3.down*cut);
    var blocked=child(remnant,"BlockedLog",Vector3.zero);
    foreach(var groupRoot in new[]{stump,upper})
    {
        var lodGroup=groupRoot.gameObject.AddComponent<LODGroup>();
        var lods=new LOD[3];
        for(int lod=0;lod<lods.Length;lod++)
        {
            var imported=renderParts["Fir02_"+groupRoot.name+"_LOD"+lod];
            var matrix=model.transform.worldToLocalMatrix*imported.transform.localToWorldMatrix;
            var node=child(groupRoot,groupRoot.name+"_LOD"+lod,matrix.GetColumn(3));
            node.localRotation=matrix.rotation;
            node.localScale=matrix.lossyScale;
            var mesh=node.gameObject.AddComponent<MeshFilter>();
            mesh.sharedMesh=imported.GetComponent<MeshFilter>().sharedMesh;
            var view=node.gameObject.AddComponent<MeshRenderer>();
            view.sharedMaterials=imported.sharedMaterials.Select(material=>{
                if(material.name.StartsWith("Leaves")) return remnantLeaves;
                if(material.name.StartsWith("Trunk")) return materials[1];
                if(material.name=="EndGrain") return cap;
                throw new System.InvalidOperationException("절단 재질 슬롯 변경 "+material.name);
            }).ToArray();
            float expectedY=groupRoot==stump ? view.bounds.max.y : view.bounds.min.y;
            if(Mathf.Abs(expectedY-cut)>0.005f || view.sharedMaterials.Length!=mesh.sharedMesh.subMeshCount)
                throw new System.InvalidOperationException("Unity FBX 절단 높이·슬롯 불일치 "+node.name+" height="+expectedY);
            lods[lod]=new LOD(levels[lod].screenRelativeTransitionHeight,new Renderer[]{view});
        }
        lodGroup.fadeMode=originalLod.fadeMode;
        lodGroup.animateCrossFading=originalLod.animateCrossFading;
        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();
    }
    var standingSample=child(intact,"StandingSample",Vector3.up*cut);
    var cutSample=child(stump,"CutSample",Vector3.up*cut);
    var trunkSample=child(pivot,"TrunkSample",Vector3.up*4);
    var landing=child(pivot,"Landing",Vector3.up*7.6f);
    foreach(var node in root.GetComponentsInChildren<Transform>(true))
    {
        node.gameObject.layer=LayerMask.NameToLayer(node==hit.transform ? "InteractableObject" : "Ground");
        UnityEditor.GameObjectUtility.SetStaticEditorFlags(node.gameObject,0);
    }
    var id=root.AddComponent<UPlayGround.Components.SceneEntityId>();
    id.EditorSetGuid("");
    var destruction=root.AddComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentDestruction>();
    var structure=new UnityEditor.SerializedObject(destruction);
    structure.FindProperty("_fallPivot").objectReferenceValue=pivot;
    structure.FindProperty("_blockedFallback").objectReferenceValue=blocked.gameObject;
    refs(structure.FindProperty("_shelterColliders"),new[]{physical});
    structure.ApplyModifiedPropertiesWithoutUndo();
    var reaction=root.AddComponent<UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject>();
    var settings=new UnityEditor.SerializedObject(reaction);
    settings.FindProperty("_definition").objectReferenceValue=profile;
    settings.FindProperty("_intact").objectReferenceValue=intact.gameObject;
    settings.FindProperty("_remnant").objectReferenceValue=remnant.gameObject;
    settings.FindProperty("_impactAnchor").objectReferenceValue=standingSample;
    settings.FindProperty("_landingAnchor").objectReferenceValue=landing;
    refs(settings.FindProperty("_hitColliders"),new[]{hit});
    foreach(string key in new[]{"_rainSamples","_heatSamples","_burnAnchors"})
        refs(settings.FindProperty(key),new[]{standingSample,cutSample,trunkSample});
    var renderers=root.GetComponentsInChildren<MeshRenderer>(true);
    var bindings=settings.FindProperty("_surfaceBindings");
    bindings.arraySize=renderers.Length;
    for(int i=0;i<renderers.Length;i++)
    {
        var binding=bindings.GetArrayElementAtIndex(i);
        binding.FindPropertyRelative("renderer").objectReferenceValue=renderers[i];
        var slots=binding.FindPropertyRelative("slots");
        slots.arraySize=renderers[i].sharedMaterials.Length;
        for(int slot=0;slot<slots.arraySize;slot++) slots.GetArrayElementAtIndex(slot).intValue=slot;
        binding.FindPropertyRelative("excludedSlots").arraySize=0;
    }
    settings.ApplyModifiedPropertiesWithoutUndo();
    var surfaceError=UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentSurface.Validate(root.transform,
        UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentSurface.BindAll(renderers),null);
    if(surfaceError!=null) throw new System.InvalidOperationException(surfaceError);
    pivot.gameObject.SetActive(false);
    blocked.gameObject.SetActive(false);
    remnant.gameObject.SetActive(false);
    var prefab=UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
    if(prefab==null) throw new System.InvalidOperationException("Fir 프리팹 저장 실패");
    created.Add(prefabPath);
    if(prefab.GetComponent<UPlayGround.Components.SceneEntityId>().HasGuid) throw new System.InvalidOperationException("배치 GUID가 프리팹에 저장됨");
    return new { prefabPath, meshes = renderParts.Count, standingLods=levels.Length };
}
catch {
    for(int i=created.Count-1;i>=0;i--) UnityEditor.AssetDatabase.DeleteAsset(created[i]);
    throw;
}
finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview); }
