// 외형이 고정된 적 프리팹에서 미사용 천/머리 가지를 제거한다. 빌더와 외부 원본은 변경하지 않는다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
const string scenePath="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity";
const string folder="output/OpenWorld/WorldPlayability/FixedAppearanceV2";
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||scene.isDirty||scene.path!=scenePath)throw new Exception("저장된 월드 Edit Mode 필요");
if(System.IO.File.Exists(folder+"/application.txt"))throw new Exception("이미 적용한 최적화입니다.");
var paths=System.IO.File.ReadAllLines(folder+"/prefabs.txt");
var actors=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.MonsterActor>(true)).Where(a=>a.Definition!=null&&paths.Contains(AssetDatabase.GetAssetPath(a.Definition.prefab))).ToArray();
var protectedPaths=paths.ToDictionary(p=>p,p=>new HashSet<string>());
foreach(var actor in actors) {
    var path=AssetDatabase.GetAssetPath(actor.Definition.prefab);
    foreach(var t in actor.GetComponentsInChildren<Transform>())protectedPaths[path].Add(AnimationUtility.CalculateTransformPath(t,actor.transform));
    foreach(var modification in PrefabUtility.GetPropertyModifications(actor.gameObject)??new PropertyModification[0]) {
        var component=modification.objectReference as Component;
        var referenced=component!=null?component.gameObject:modification.objectReference as GameObject;
        if(referenced!=null&&referenced.transform.IsChildOf(actor.transform))protectedPaths[path].Add(AnimationUtility.CalculateTransformPath(referenced.transform,actor.transform));
    }
}
System.IO.Directory.CreateDirectory(folder+"/backup");
if(!System.IO.File.Exists(folder+"/backup/LakeVillageWorld.unity"))System.IO.File.Copy(scenePath,folder+"/backup/LakeVillageWorld.unity");
foreach(var path in paths) {
    var backup=folder+"/backup/"+path;
    if(System.IO.File.Exists(backup))throw new Exception("기존 백업이 있습니다. 적용 상태를 먼저 확인하세요: "+path);
    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));System.IO.File.Copy(path,backup);
}
var identityBefore=string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s));
System.IO.File.WriteAllText(folder+"/identities-before.txt",identityBefore);
// 프리팹 갱신마다 수백 개의 씬 인스턴스가 재구성되는 비용을 피한다.
UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
var roots=new Dictionary<string,GameObject>();var rows=new List<string>();bool committed=false;
Func<GameObject,string> visuals=root=>string.Join("\n",root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).Select(r=>AnimationUtility.CalculateTransformPath(r.transform,root.transform)+"|"+r.sharedMaterials.Length+"|"+string.Join(",",r.sharedMaterials.Select(m=>m==null?"NULL":AssetDatabase.GetAssetPath(m)).ToArray())).OrderBy(s=>s));
try {
    foreach(var path in paths) {
        var root=PrefabUtility.LoadPrefabContents(path);roots.Add(path,root);
        if(root.GetComponent<UPlayGround.MonsterActor>()==null||root.GetComponentsInChildren<UPlayGround.Components.CharacterEquipmentVisuals>(true).Length>0)throw new Exception("고정 외형 대상이 아닙니다: "+path);
        var originalVisuals=visuals(root);int before=root.GetComponentsInChildren<Transform>(true).Length;
        var candidates=root.GetComponentsInChildren<Transform>(true).Where(t=>t!=root.transform&&!t.gameObject.activeSelf&&t.parent.gameObject.activeInHierarchy
            &&t.GetComponentsInChildren<MagicaCloth2.MagicaCloth>(true).Length>0
            &&t.GetComponentsInChildren<MonoBehaviour>(true).All(c=>c!=null&&c.GetType().Namespace=="MagicaCloth2")
            &&!protectedPaths[path].Any(p=>p==AnimationUtility.CalculateTransformPath(t,root.transform)||p.StartsWith(AnimationUtility.CalculateTransformPath(t,root.transform)+"/",StringComparison.Ordinal))).ToList();
        bool changed=true;
        while(changed) {
            changed=false;
            var owners=new Dictionary<UnityEngine.Object,Transform>();
            foreach(var candidate in candidates)foreach(var t in candidate.GetComponentsInChildren<Transform>(true)) {
                owners[t.gameObject]=candidate;foreach(var c in t.GetComponents<Component>())if(c!=null)owners[c]=candidate;
            }
            var retained=root.GetComponentsInChildren<Component>(true).Where(c=>c!=null&&!owners.ContainsKey(c)).ToArray();
            var protectedRoots=new HashSet<Transform>();
            foreach(var component in retained) {
                var serialized=new SerializedObject(component);var property=serialized.GetIterator();
                while(property.Next(true))if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceValue!=null
                    &&!(component is Transform&&property.propertyPath.StartsWith("m_Children",StringComparison.Ordinal))) {
                    Transform owner;if(owners.TryGetValue(property.objectReferenceValue,out owner))protectedRoots.Add(owner);
                }
            }
            foreach(var protectedRoot in protectedRoots)changed|=candidates.Remove(protectedRoot);
        }
        var removing=new HashSet<Component>(candidates.SelectMany(t=>t.GetComponentsInChildren<Component>(true)).Where(c=>c!=null));
        var references=new List<Tuple<Component,string,UnityEngine.Object>>();
        foreach(var component in root.GetComponentsInChildren<Component>(true).Where(c=>c!=null&&!removing.Contains(c))) {
            var serialized=new SerializedObject(component);var property=serialized.GetIterator();
            while(property.Next(true))if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceValue!=null
                &&!(component is Transform&&property.propertyPath.StartsWith("m_Children",StringComparison.Ordinal)))
                references.Add(Tuple.Create(component,property.propertyPath,property.objectReferenceValue));
        }
        foreach(var candidate in candidates)UnityEngine.Object.DestroyImmediate(candidate.gameObject);
        if(visuals(root)!=originalVisuals)throw new Exception("활성 외형 변경: "+path);
        foreach(var reference in references) {
            var property=new SerializedObject(reference.Item1).FindProperty(reference.Item2);
            if(property==null||property.objectReferenceValue!=reference.Item3)throw new Exception("유지 참조 변경: "+path+" "+reference.Item2);
        }
        if(root.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new Exception("Missing Script: "+path);
        int after=root.GetComponentsInChildren<Transform>(true).Length;
        rows.Add(path+"|"+AssetDatabase.AssetPathToGUID(path)+"|"+before+"->"+after+"|제거 가지="+candidates.Count+"|유지 참조="+references.Count);
    }
    System.IO.File.WriteAllLines(folder+"/preflight.txt",rows);
    foreach(var pair in roots) {
        bool saved;PrefabUtility.SaveAsPrefabAsset(pair.Value,pair.Key,out saved);if(!saved)throw new Exception("프리팹 저장 실패: "+pair.Key);
    }
    committed=true;
    System.IO.File.WriteAllLines(folder+"/application.txt",rows);
} catch(Exception error) {
    foreach(var path in paths)System.IO.File.Copy(folder+"/backup/"+path,path,true);
    System.IO.File.WriteAllText(folder+"/failure.txt",error.ToString());throw;
} finally {
    foreach(var root in roots.Values)if(root!=null)PrefabUtility.UnloadPrefabContents(root);
    AssetDatabase.Refresh();
    if(!committed)foreach(var path in paths)AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
}
return "고정 외형 프리팹="+roots.Count+" / 활성 외형·유지 참조 동일 / 월드 재열기와 ID·외형 검증 필요";
