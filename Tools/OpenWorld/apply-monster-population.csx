// 새 배치와 지역 전용 성장 파생본만 생성한다. 원본 프리팹·성장 데이터는 보존한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.isDirty || scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("컴파일이 끝난 저장 월드 Edit Mode 필요");
string folder="output/OpenWorld/MonsterPopulation";
string dataRoot="Assets/10.Datas/World/OpenWorld/Monsters";
string rootName="필드 몬스터 성장 배치";
if(GameObject.Find(rootName)!=null || AssetDatabase.IsValidFolder(dataRoot))throw new Exception("이미 적용했거나 결과 경로가 존재합니다. 재실행 금지");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/monster-population-resolved.json"),typeof(Dictionary<string,object>));
var groups=(Dictionary<string,object>[])read(plan["groups"].ToString(),typeof(Dictionary<string,object>[]));
var overrides=(Dictionary<string,object>[])read(plan["existingOverrides"].ToString(),typeof(Dictionary<string,object>[]));
var definitions=AssetDatabase.FindAssets("t:ActorDefinitionSO").Select(AssetDatabase.GUIDToAssetPath).Select(p=>AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDefinitionSO>(p)).Where(d=>d.actorType.HasFlag(UPlayGround.Data.EnumType.ActorType.Monster)).ToDictionary(d=>d.actorId);
var members=groups.SelectMany(g=>(Dictionary<string,object>[])read(g["members"].ToString(),typeof(Dictionary<string,object>[]))).ToArray();
var requests=members.Concat(overrides).GroupBy(m=>m["source"]+"/"+m["level"]).Select(g=>g.First()).ToArray();
var database=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDatabase>("Assets/10.Datas/Actor/DataBase/ActorDatabase.asset");
var codex=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Codex.MonsterCodexDatabaseSO>("Assets/10.Datas/Codex/Monster/MonsterCodexDatabase.asset");
var validate=typeof(UPlayGround.Editor.P09Builder.P09ProgressionBuilder).GetMethod("ValidateCombat",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
foreach(var sourceId in requests.Select(m=>(string)m["source"]).Distinct()) {
    var source=definitions[sourceId];
    validate.Invoke(null,new object[]{source});
    if(source.EffectiveMonsterScaling==null || source.attributeProfile==null || source.monsterProfile==null || source.poiseData==null || source.EffectiveBreakGaugeData==null || codex.GetEntry(sourceId)==null || source.EffectiveRecruitableAs!=UPlayGround.Data.EnumType.CharacterActorType.None)throw new Exception("파생 원본 연결 오류: "+sourceId);
}
var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).GroupBy(t=>AnimationUtility.CalculateTransformPath(t,null)).ToDictionary(g=>g.Key,g=>g.First(), StringComparer.Ordinal);
foreach(var entry in overrides)if(!transforms.ContainsKey((string)entry["path"]))throw new Exception("기존 개체 없음: "+entry["path"]);
System.IO.Directory.CreateDirectory(folder);
if(System.IO.File.Exists(folder+"/scene.before.unity"))throw new Exception("기존 백업이 있습니다. 적용 상태를 먼저 확인하세요.");
System.IO.File.Copy(scene.path,folder+"/scene.before.unity");
string databaseBackup=EditorJsonUtility.ToJson(database),codexBackup=EditorJsonUtility.ToJson(codex);
System.IO.File.WriteAllText(folder+"/database.before.json",databaseBackup);
System.IO.File.WriteAllText(folder+"/codex.before.json",codexBackup);
var created=new List<string>();
var resolved=new Dictionary<string,UPlayGround.Data.Actor.ActorDefinitionSO>();
var oldDefinitions=new Dictionary<UPlayGround.MonsterActor,UPlayGround.Data.Actor.ActorDefinitionSO>();
GameObject root=null;
Action<UnityEngine.Object> save=o=>{EditorUtility.SetDirty(o);AssetDatabase.SaveAssetIfDirty(o);};
Func<ScriptableObject,string,ScriptableObject> clone=(source,path)=>{var asset=UnityEngine.Object.Instantiate(source);asset.name=System.IO.Path.GetFileNameWithoutExtension(path);created.Add(path);AssetDatabase.CreateAsset(asset,path);return asset;};
int undoGroup=Undo.GetCurrentGroup();Undo.IncrementCurrentGroup();undoGroup=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("호수 월드 필드 몬스터 배치");
try {
    AssetDatabase.CreateFolder("Assets/10.Datas/World/OpenWorld","Monsters");
    foreach(var request in requests) {
        string sourceId=(string)request["source"];int level=Convert.ToInt32(request["level"]);
        var source=definitions[sourceId];string key=sourceId+"/"+level;
        if(source.EffectiveLevel==level){resolved.Add(key,source);continue;}
        string id="LakeWorld_"+sourceId+"_Lv"+level.ToString("00");
        string path=dataRoot+"/"+id;
        AssetDatabase.CreateFolder(dataRoot,id);
        var d=(UPlayGround.Data.Actor.ActorDefinitionSO)clone(source,path+"/Definition.asset");
        d.actorId=id;d.level=level;d.grade=source.EffectiveGrade;d.prefab=null;d.attributeProfile=null;
        d.monsterScaling=source.EffectiveMonsterScaling;
        d.monsterProfile=(UPlayGround.Data.Actor.MonsterActorProfileSO)clone(source.monsterProfile,path+"/Profile.asset");
        d.monsterProfile.level=level;d.monsterProfile.grade=d.grade;
        d.poiseData=(UPlayGround.Data.Enemy.PoiseSO)clone(source.poiseData,path+"/Poise.asset");
        d.breakGaugeData=(UPlayGround.Data.Enemy.MonsterBreakGaugeSO)clone(source.EffectiveBreakGaugeData,path+"/BreakGauge.asset");
        d.monsterProfile.breakGaugeData=d.breakGaugeData;
        var policy=d.breakGaugeData.gradePolicy;
        policy.weakGaugeMultiplier=policy.normalGaugeMultiplier=policy.eliteGaugeMultiplier=policy.bossGaugeMultiplier=1f;
        var reward=UPlayGround.Tool.Editor.Balance.MonsterExperienceRewardService.Calculate(d,new UPlayGround.Tool.Editor.Balance.MonsterExperienceRewardService.Options{LevelCurve=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Party.LevelCurveSO>("Assets/10.Datas/Party/Growth/LevelCurve_Default.asset"),PlayerLevel=level,RecordUndo=false});
        d.expReward=reward.Reward;d.monsterProfile.expReward=d.expReward;
        created.Add(path+"/AttributeProfile_"+id+".asset");
        UPlayGround.Tool.Editor.Balance.MonsterStatBakeService.Bake(d,new UPlayGround.Tool.Editor.Balance.MonsterStatBakeService.Options{StatSavePath=path,UseStableStatAssetPath=true,RecordUndo=false,SyncGeneratedPoise=true,SyncExistingBreakGauge=true});
        string prefabPath=path+"/"+id+".prefab";
        created.Add(prefabPath);
        if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source.prefab),prefabPath))throw new Exception("프리팹 복사 실패: "+id);
        var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
        try {
            prefab.name=id;
            var actor=prefab.GetComponent<UPlayGround.MonsterActor>();var serialized=new SerializedObject(actor);
            serialized.FindProperty("_definition").objectReferenceValue=d;
            serialized.FindProperty("_actorId").stringValue=id;
            serialized.FindProperty("_level").intValue=level;
            serialized.FindProperty("_grade").intValue=(int)d.grade;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var poise=prefab.GetComponent<UPlayGround.Components.PoiseStat>();
            if(poise!=null){var value=new SerializedObject(poise);value.FindProperty("_data").objectReferenceValue=d.poiseData;value.ApplyModifiedPropertiesWithoutUndo();}
            bool success;d.prefab=PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath,out success);if(!success)throw new Exception("프리팹 저장 실패: "+id);
        } finally {PrefabUtility.UnloadPrefabContents(prefab);}
        foreach(var asset in new UnityEngine.Object[]{d,d.monsterProfile,d.attributeProfile,d.poiseData,d.breakGaugeData})save(asset);
        validate.Invoke(null,new object[]{d});
        database.AddDefinition(d);
        var entry=(UPlayGround.Data.Codex.MonsterCodexEntrySO)clone(codex.GetEntry(sourceId),path+"/Codex.asset");
        entry.actorId=id;codex.AddEntry(entry);save(entry);
        resolved.Add(key,d);
    }
    root=new GameObject(rootName);Undo.RegisterCreatedObjectUndo(root,"필드 몬스터 배치");
    root.transform.SetParent(GameObject.Find("탐험 콘텐츠").transform,false);
    foreach(var group in groups) {
        var go=new GameObject(group["id"]+"_Lv"+group["level"]+"_"+group["route"]);go.transform.SetParent(root.transform,false);
        var groupMembers=(Dictionary<string,object>[])read(group["members"].ToString(),typeof(Dictionary<string,object>[]));
        go.transform.position=new Vector3(groupMembers.Average(m=>Convert.ToSingle(m["x"])),groupMembers.Average(m=>Convert.ToSingle(m["y"])),groupMembers.Average(m=>Convert.ToSingle(m["z"])));
        var controller=go.AddComponent<UPlayGround.Group.MonsterGroupController>();
        var settings=new SerializedObject(controller);
        settings.FindProperty("_meleeSlotCap").intValue=1;settings.FindProperty("_rangedSlotCap").intValue=1;
        settings.ApplyModifiedPropertiesWithoutUndo();
        foreach(var m in (Dictionary<string,object>[])read(group["members"].ToString(),typeof(Dictionary<string,object>[]))) {
            var d=resolved[m["source"]+"/"+m["level"]];
            var actor=(GameObject)PrefabUtility.InstantiatePrefab(d.prefab,go.transform);
            actor.name=d.actorId;
            actor.transform.SetPositionAndRotation(new Vector3(Convert.ToSingle(m["x"]),Convert.ToSingle(m["y"]),Convert.ToSingle(m["z"])),Quaternion.Euler(0,Convert.ToSingle(m["yaw"]),0));
            var identity=actor.GetComponent<UPlayGround.Components.SceneEntityId>()??actor.AddComponent<UPlayGround.Components.SceneEntityId>();identity.EditorSetGuid((string)m["guid"]);
            PrefabUtility.RecordPrefabInstancePropertyModifications(identity);
        }
    }
    foreach(var item in overrides) {
        var actor=transforms[(string)item["path"]].GetComponent<UPlayGround.MonsterActor>();
        oldDefinitions.Add(actor,actor.Definition);Undo.RecordObject(actor,"지역 몬스터 성장 조정");
        var data=new SerializedObject(actor);data.FindProperty("_definition").objectReferenceValue=resolved[item["source"]+"/"+item["level"]];data.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
    }
    save(database);save(codex);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
    Undo.CollapseUndoOperations(undoGroup);
    System.IO.File.WriteAllLines(folder+"/application.txt",new[]{"신규 조우="+groups.Length+" / 신규 몬스터="+members.Length+" / 기존 지역 몬스터 성장 조정="+overrides.Length,"지역 파생 Definition="+resolved.Values.Distinct().Count(d=>AssetDatabase.GetAssetPath(d).StartsWith(dataRoot)),"기존 P09 장비·GAS·BT·MotionSet 및 원본 몬스터 데이터 보존","Play Mode 전투 감각·파일 저장/재진입=사용자 테스트 대기"});
    return System.IO.File.ReadAllText(folder+"/application.txt");
} catch {
    Undo.RevertAllDownToGroup(undoGroup);
    foreach(var pair in oldDefinitions)if(pair.Key!=null){var data=new SerializedObject(pair.Key);data.FindProperty("_definition").objectReferenceValue=pair.Value;data.ApplyModifiedPropertiesWithoutUndo();}
    if(root!=null)UnityEngine.Object.DestroyImmediate(root);
    EditorJsonUtility.FromJsonOverwrite(databaseBackup,database);database.InvalidateLookup();save(database);
    EditorJsonUtility.FromJsonOverwrite(codexBackup,codex);codex.InvalidateLookup();save(codex);
    for(int i=created.Count-1;i>=0;i--)AssetDatabase.DeleteAsset(created[i]);
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    throw;
}

