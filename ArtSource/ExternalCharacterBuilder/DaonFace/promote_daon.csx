var output="output/DaonProductionIntegration";
var canonicalPath="Assets/03.Prefabs/Characters/ScenarioResidents/Npc/Npc_Daon/Npc_Daon.prefab";
var presetPath="Assets/10.Datas/Actor/ScenarioResidents/Npc_Daon_BuildPreset.asset";
var harborPath="Assets/01.Scenes/Scenario/SaltwindHarbor.unity";
var dependents=new[]{"Assets/03.Prefabs/Characters/HarborResidents/NPC_Harbor_Bora.prefab","Assets/03.Prefabs/Characters/HarborResidents/NPC_Harbor_Yuna.prefab","Assets/03.Prefabs/Environment/Farming/FarmWorker_0.prefab"};
if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("편집 모드 필요");
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new System.Exception("미저장 씬 보존");
if(System.IO.File.Exists(output+"/promotion.txt"))throw new System.Exception("적용 완료 기록을 보존합니다.");
var canonical=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(canonicalPath);
var approved=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/03.Prefabs/Characters/ExternalModels/Daon_P09Compatible.prefab");
var approvedPreset=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>("Assets/10.Datas/Actor/ExternalModels/Daon_P09_BuildPreset.asset");
var preset=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Editor.P09Builder.BuildPreset>(presetPath);
var originalPresetJson=UnityEditor.EditorJsonUtility.ToJson(preset);
var guidBefore=UnityEditor.AssetDatabase.AssetPathToGUID(canonicalPath);
var behaviors=canonical.GetComponentsInChildren<UnityEngine.Component>(true).Where(c=>c!=null&&!(c is UnityEngine.Renderer)).ToDictionary(c=>UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(c).ToString(),c=>UnityEditor.EditorJsonUtility.ToJson(c));
var renderPaths=approvedPreset.config.FaceMeshProfile.rendererPaths.Concat(new[]{"Mesh/Hair/Hair_03/Hair_03"}).ToArray();
System.Func<UnityEngine.GameObject,object[][]> capture=root=>renderPaths.Select(path=>{var skin=root.transform.Find(path).GetComponent<UnityEngine.SkinnedMeshRenderer>();return new object[]{path,skin.sharedMesh,skin.sharedMaterials};}).ToArray();
System.Action<UnityEngine.GameObject,object[][]> restore=(root,rows)=>{foreach(var row in rows){var skin=root.transform.Find((string)row[0]).GetComponent<UnityEngine.SkinnedMeshRenderer>();skin.sharedMesh=(UnityEngine.Mesh)row[1];skin.sharedMaterials=(UnityEngine.Material[])row[2];if(UnityEditor.PrefabUtility.IsPartOfPrefabInstance(skin))UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(skin);}};
var snapshots=dependents.ToDictionary(path=>path,path=>capture(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path)));
var harbor=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(harborPath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
var residents=harbor.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.NpcActor>(true)).Where(a=>UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(a.gameObject)==canonicalPath).ToArray();
if(residents.Length!=2)throw new System.Exception("항구 파생 주민 수 변경: "+residents.Length);
var residentIds=residents.Select(a=>new UnityEditor.SerializedObject(a).FindProperty("_actorId").stringValue).ToArray();
var sceneSnapshots=residents.Select(a=>capture(a.gameObject)).ToArray();
var componentSnapshots=residents.Select(a=>UnityEditor.EditorJsonUtility.ToJson(a)).ToArray();
var originalIds=System.Text.RegularExpressions.Regex.Matches(System.IO.File.ReadAllText(canonicalPath),@"(?m)^--- !u!\d+ &(-?\d+)").Cast<System.Text.RegularExpressions.Match>().Select(m=>m.Groups[1].Value).OrderBy(v=>v).ToArray();
bool saved=false;
try
{
    var stage=UnityEditor.PrefabUtility.LoadPrefabContents(canonicalPath);
    try
    {
        approvedPreset.config.FaceMeshProfile.Apply(stage);
        var hair=stage.transform.Find("Mesh/Hair/Hair_03/Hair_03").GetComponent<UnityEngine.SkinnedMeshRenderer>();
        var sourceHair=approved.transform.Find("Mesh/Hair/Hair_03/Hair_03").GetComponent<UnityEngine.SkinnedMeshRenderer>();
        if(!hair.bones.Select(b=>b.name).SequenceEqual(sourceHair.bones.Select(b=>b.name)))throw new System.Exception("헤어 본 계약 불일치");
        hair.sharedMesh=sourceHair.sharedMesh;hair.sharedMaterials=sourceHair.sharedMaterials;
        if(stage.GetComponentsInChildren<UnityEngine.Transform>(true).Any(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new System.Exception("Missing Script");
        if(UnityEditor.PrefabUtility.SaveAsPrefabAsset(stage,canonicalPath)==null)throw new System.Exception("정규 프리팹 저장 실패");
        saved=true;
    }
    finally{UnityEditor.PrefabUtility.UnloadPrefabContents(stage);}
    preset.config.FaceMeshProfile=approvedPreset.config.FaceMeshProfile;
    preset.config.HairStyleSo=approvedPreset.config.HairStyleSo;preset.config.HairColorSo=approvedPreset.config.HairColorSo;
    preset.description="새 얼굴 Daon_P09_Face와 P09 헤어 03·방어구 05의 정규 다온. 기존 NPC 데이터·식별·월드 배치 보존. 외형 갱신은 기존 프리팹에 적용하며 전체 재생성하지 않는다.";
    UnityEditor.EditorUtility.SetDirty(preset);UnityEditor.AssetDatabase.SaveAssetIfDirty(preset);
    foreach(var path in dependents)
    {
        var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
        try{restore(root,snapshots[path]);if(UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path)==null)throw new System.Exception("파생 외형 보존 실패: "+path);}
        finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
    }
    // 정규 다온을 공용 원본으로 쓰는 다른 인물의 얼굴은 현재 외형으로 고정한다.
    var updatedResidents=harbor.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.NpcActor>(true)).ToArray();
    for(int i=0;i<residentIds.Length;i++)
    {
        var actor=updatedResidents.Single(a=>new UnityEditor.SerializedObject(a).FindProperty("_actorId").stringValue==residentIds[i]);
        restore(actor.gameObject,sceneSnapshots[i]);
        if(UnityEditor.EditorJsonUtility.ToJson(actor)!=componentSnapshots[i])throw new System.Exception("항구 NPC 데이터 변경");
    }
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(harbor);
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(harbor))throw new System.Exception("항구 외형 override 저장 실패");
    var ids=System.Text.RegularExpressions.Regex.Matches(System.IO.File.ReadAllText(canonicalPath),@"(?m)^--- !u!\d+ &(-?\d+)").Cast<System.Text.RegularExpressions.Match>().Select(m=>m.Groups[1].Value).OrderBy(v=>v).ToArray();
    if(!ids.SequenceEqual(originalIds))throw new System.Exception("정규 프리팹 local fileID 변경");
    var reloaded=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(canonicalPath);
    foreach(var c in reloaded.GetComponentsInChildren<UnityEngine.Component>(true).Where(c=>c!=null&&!(c is UnityEngine.Renderer)))
    {
        var key=UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(c).ToString();
        if(!behaviors.ContainsKey(key)||behaviors[key]!=UnityEditor.EditorJsonUtility.ToJson(c))throw new System.Exception("외형 외 컴포넌트 변경: "+c.GetType().Name+"/"+c.name);
    }
    if(UnityEditor.AssetDatabase.AssetPathToGUID(canonicalPath)!=guidBefore)throw new System.Exception("정규 GUID 변경");
    var report="Canonical GUID preserved: "+guidBefore+"\nLocal fileIDs preserved: "+ids.Length+"\nNon-renderer component snapshots preserved: "+behaviors.Count+"\nDependent prefab appearances preserved: "+dependents.Length+"\nHarbor resident appearances preserved: "+residentIds.Length+"\nCanonical preset updated; ActorDefinition/NpcData unchanged.\n";
    System.IO.File.WriteAllText(output+"/promotion.txt",report);return report;
}
catch
{
    if(harbor.IsValid()&&harbor.isLoaded)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(harbor,true);
    if(saved)
    {
        var paths=new[]{canonicalPath,presetPath,harborPath,"Assets/04.Images/UI/Portrait/Npc/Npc_Daon_Dialogue_Portrait.png"}.Concat(dependents).ToArray();
        for(int i=0;i<paths.Length;i++)System.IO.File.Copy(output+"/Backup/"+i+".backup",paths[i],true);
        UnityEditor.EditorJsonUtility.FromJsonOverwrite(originalPresetJson,preset);
        UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    }
    throw;
}
finally{if(harbor.IsValid()&&harbor.isLoaded)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(harbor,true);}
