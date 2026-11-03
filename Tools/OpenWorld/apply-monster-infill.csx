// 검증된 기존 프리팹을 사용해 숲 조우만 증분 추가한다. 기존 배치와 에셋은 변경하지 않는다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.isDirty || scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
string folder="output/OpenWorld/MonsterPopulation/Infill";
string planPath="Tools/OpenWorld/monster-population-resolved.json";
string additionName="숲과 유적 내부 보강";
var parent=GameObject.Find("탐험 콘텐츠/필드 몬스터 성장 배치");
if(parent==null || parent.transform.Find(additionName)!=null || System.IO.File.Exists(folder+"/application.txt"))throw new Exception("기존 배치가 없거나 이미 숲 보강이 적용됐습니다.");
if(!System.IO.File.Exists(folder+"/scene.before.unity.gz"))throw new Exception("작업 전 씬 백업 필요");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var addition=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/monster-infill-resolved.json"),typeof(Dictionary<string,object>));
var groups=(Dictionary<string,object>[])read(addition["groups"].ToString(),typeof(Dictionary<string,object>[]));
string originalText=System.IO.File.ReadAllText(planPath);
var original=(Dictionary<string,object>)read(originalText,typeof(Dictionary<string,object>));
var originals=(Dictionary<string,object>[])read(original["groups"].ToString(),typeof(Dictionary<string,object>[]));
var database=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDatabase>("Assets/10.Datas/Actor/DataBase/ActorDatabase.asset");
var validate=typeof(UPlayGround.Editor.P09Builder.P09ProgressionBuilder).GetMethod("ValidateCombat",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
var definitions=new Dictionary<string,UPlayGround.Data.Actor.ActorDefinitionSO>();
foreach(var group in groups)foreach(var member in (Dictionary<string,object>[])read(group["members"].ToString(),typeof(Dictionary<string,object>[]))){
    string id=(string)member["source"];
    if(definitions.ContainsKey(id))continue;
    var d=database.GetDefinition(id);
    if(d==null || d.prefab==null || d.prefab.GetComponent<UPlayGround.MonsterActor>().Definition!=d || d.EffectiveRecruitableAs!=UPlayGround.Data.EnumType.CharacterActorType.None)throw new Exception("등록/영입/프리팹 오류: "+id);
    validate.Invoke(null,new object[]{d});definitions.Add(id,d);
}
Undo.IncrementCurrentGroup();int undoGroup=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("표시된 숲 내부 몬스터 보강");
GameObject root=null;
try {
    root=new GameObject(additionName);root.transform.SetParent(parent.transform,false);Undo.RegisterCreatedObjectUndo(root,"숲 내부 조우 추가");
    foreach(var group in groups){
        System.IO.File.WriteAllText(folder+"/phase.txt","조우 생성 중: "+group["id"]);
        var members=(Dictionary<string,object>[])read(group["members"].ToString(),typeof(Dictionary<string,object>[]));
        var go=new GameObject(group["id"]+"_Lv"+group["level"]+"_"+group["route"]);go.transform.SetParent(root.transform,false);
        go.transform.position=new Vector3(members.Average(m=>Convert.ToSingle(m["x"])),members.Average(m=>Convert.ToSingle(m["y"])),members.Average(m=>Convert.ToSingle(m["z"])));
        var controller=go.AddComponent<UPlayGround.Group.MonsterGroupController>();var settings=new SerializedObject(controller);
        settings.FindProperty("_meleeSlotCap").intValue=1;settings.FindProperty("_rangedSlotCap").intValue=1;settings.ApplyModifiedPropertiesWithoutUndo();
        foreach(var member in members){
            var d=definitions[(string)member["source"]];
            if(d.EffectiveLevel!=Convert.ToInt32(member["level"]))throw new Exception("레벨 불일치: "+d.actorId);
            var actor=(GameObject)PrefabUtility.InstantiatePrefab(d.prefab,go.transform);actor.name=d.actorId;
            actor.transform.SetPositionAndRotation(new Vector3(Convert.ToSingle(member["x"]),Convert.ToSingle(member["y"]),Convert.ToSingle(member["z"])),Quaternion.Euler(0,Convert.ToSingle(member["yaw"]),0));
            var identity=actor.GetComponent<UPlayGround.Components.SceneEntityId>()??actor.AddComponent<UPlayGround.Components.SceneEntityId>();identity.EditorSetGuid((string)member["guid"]);PrefabUtility.RecordPrefabInstancePropertyModifications(identity);
        }
    }
    original["groups"]=originals.Concat(groups).ToArray();original["infillZones"]=addition["zones"];
    var serialize=json.GetMethod("SerializeObject",new[]{typeof(object)});
    System.IO.File.WriteAllText(planPath,(string)serialize.Invoke(null,new object[]{original}));
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
    Undo.CollapseUndoOperations(undoGroup);
    System.IO.File.WriteAllLines(folder+"/application.txt",new[]{"추가 조우="+groups.Length+" / 추가 몬스터="+root.GetComponentsInChildren<UPlayGround.MonsterActor>(true).Length,"전체 신규 조우="+(originals.Length+groups.Length)+" / 전체 신규 몬스터="+parent.GetComponentsInChildren<UPlayGround.MonsterActor>(true).Length,"추가 사용 Definition="+definitions.Count+" / 기존 데이터·배치·진행 ID 보존","실제 Play Mode 전투·성능=사용자 검수 대기"});
    return System.IO.File.ReadAllText(folder+"/application.txt");
}catch{
    Undo.RevertAllDownToGroup(undoGroup);if(root!=null)UnityEngine.Object.DestroyImmediate(root);
    System.IO.File.WriteAllText(planPath,originalText);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);throw;
}
