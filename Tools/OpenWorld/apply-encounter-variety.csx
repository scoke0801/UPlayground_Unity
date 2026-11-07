// monster-variety-resolved.json의 입문 구간 조우와 4인 거점 보강을 씬에 적용한다. 적용 완료 후 재실행하지 않는다.
// 기존 조우·진행 ID·좌표는 바꾸지 않고, 거점은 기존 그룹에 멤버만 추가한다. 실패하면 Undo와 계획 JSON을 모두 되돌린다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.isDirty || scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
string folder="output/OpenWorld/MonsterPopulation/Improvement";
string planPath="Tools/OpenWorld/monster-population-resolved.json";
string additionName="입문 구간 조우 보강";
var parent=GameObject.Find("탐험 콘텐츠/필드 몬스터 성장 배치");
if(parent==null || parent.transform.Find(additionName)!=null || System.IO.File.Exists(folder+"/application.txt"))throw new Exception("기존 배치가 없거나 이미 적용됐습니다.");
if(!System.IO.File.Exists(folder+"/scene.before.unity.gz"))throw new Exception("작업 전 씬 백업 필요");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
var serialize=json.GetMethod("SerializeObject",new[]{typeof(object)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
// 계획 그룹의 members는 파일에서 읽으면 JSON 토큰, 이번 실행에서 추가했으면 배열이다. 두 경우를 모두 받는다.
Func<Dictionary<string,object>,Dictionary<string,object>[]> membersOf=g=>g["members"] as Dictionary<string,object>[] ?? (Dictionary<string,object>[])read(g["members"].ToString(),typeof(Dictionary<string,object>[]));
var variety=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/monster-variety-resolved.json"),typeof(Dictionary<string,object>));
var starters=(Dictionary<string,object>[])read(variety["starterGroups"].ToString(),typeof(Dictionary<string,object>[]));
var camps=(Dictionary<string,object>[])read(variety["campAugments"].ToString(),typeof(Dictionary<string,object>[]));
string originalText=System.IO.File.ReadAllText(planPath);
var original=(Dictionary<string,object>)read(originalText,typeof(Dictionary<string,object>));
var originals=((Dictionary<string,object>[])read(original["groups"].ToString(),typeof(Dictionary<string,object>[]))).ToList();
var database=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDatabase>("Assets/10.Datas/Actor/DataBase/ActorDatabase.asset");
var identities=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).ToArray();
System.IO.File.WriteAllLines(folder+"/identities-before.txt",identities.Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")));
Func<Dictionary<string,object>,UPlayGround.Data.Actor.ActorDefinitionSO> resolve=m=>{
    var d=database.GetDefinition((string)m["source"]);
    if(d==null || d.prefab==null || d.prefab.GetComponent<UPlayGround.MonsterActor>().Definition!=d || d.EffectiveRecruitableAs!=UPlayGround.Data.EnumType.CharacterActorType.None)throw new Exception("등록/영입/프리팹 오류: "+m["source"]);
    if(d.EffectiveLevel!=Convert.ToInt32(m["level"]))throw new Exception("레벨 불일치: "+d.actorId);
    return d;
};
Action<Dictionary<string,object>,Transform> spawn=(m,groupTransform)=>{
    var d=resolve(m);
    var actor=(GameObject)PrefabUtility.InstantiatePrefab(d.prefab,groupTransform);actor.name=d.actorId;
    actor.transform.SetPositionAndRotation(new Vector3(Convert.ToSingle(m["x"]),Convert.ToSingle(m["y"]),Convert.ToSingle(m["z"])),Quaternion.Euler(0,Convert.ToSingle(m["yaw"]),0));
    var identity=actor.GetComponent<UPlayGround.Components.SceneEntityId>()??actor.AddComponent<UPlayGround.Components.SceneEntityId>();
    identity.EditorSetGuid((string)m["guid"]);PrefabUtility.RecordPrefabInstancePropertyModifications(identity);
    Undo.RegisterCreatedObjectUndo(actor,"조우 멤버 추가");
};
Undo.IncrementCurrentGroup();int undoGroup=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("입문 구간 조우·거점 보강");
GameObject root=null;
int campMembers=0;
try {
    root=new GameObject(additionName);root.transform.SetParent(parent.transform,false);Undo.RegisterCreatedObjectUndo(root,"입문 구간 조우 추가");
    foreach(var group in starters){
        var members=(Dictionary<string,object>[])read(group["members"].ToString(),typeof(Dictionary<string,object>[]));
        var go=new GameObject(group["id"]+"_Lv"+group["level"]+"_"+group["route"]);go.transform.SetParent(root.transform,false);
        go.transform.position=new Vector3(members.Average(m=>Convert.ToSingle(m["x"])),members.Average(m=>Convert.ToSingle(m["y"])),members.Average(m=>Convert.ToSingle(m["z"])));
        var controller=go.AddComponent<UPlayGround.Group.MonsterGroupController>();var settings=new SerializedObject(controller);
        settings.FindProperty("_meleeSlotCap").intValue=1;settings.FindProperty("_rangedSlotCap").intValue=1;settings.ApplyModifiedPropertiesWithoutUndo();
        foreach(var m in members)spawn(m,go.transform);
        originals.Add(new Dictionary<string,object>{{"id",group["id"]},{"route",group["route"]},{"level",group["level"]},{"members",members}});
    }
    foreach(var camp in camps){
        var groupGo=GameObject.Find((string)camp["groupPath"]);
        if(groupGo==null || groupGo.GetComponent<UPlayGround.Group.MonsterGroupController>()==null)throw new Exception("거점 그룹 없음: "+camp["groupPath"]);
        string anchor=(string)camp["anchorGuid"];
        if(!groupGo.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Any(c=>c.Guid==anchor))throw new Exception("거점 기준 개체 불일치: "+camp["groupPath"]);
        var members=(Dictionary<string,object>[])read(camp["members"].ToString(),typeof(Dictionary<string,object>[]));
        foreach(var m in members){spawn(m,groupGo.transform);campMembers++;}
        var planned=originals.FirstOrDefault(g=>membersOf(g).Any(m=>(string)m["guid"]==anchor));
        if(planned==null)throw new Exception("계획에 거점 그룹 없음: "+camp["groupPath"]);
        planned["members"]=membersOf(planned).Concat(members).ToArray();
        planned["camp"]=true;
    }
    original["groups"]=originals.ToArray();
    System.IO.File.WriteAllText(planPath,(string)serialize.Invoke(null,new object[]{original}));
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
    Undo.CollapseUndoOperations(undoGroup);
    int starterMembers=root.GetComponentsInChildren<UPlayGround.MonsterActor>(true).Length;
    System.IO.File.WriteAllLines(folder+"/application.txt",new[]{
        "입문 구간 신규 조우="+starters.Length+" / 몬스터="+starterMembers,
        "4인 거점 보강="+camps.Length+" / 추가 몬스터="+campMembers,
        "필드 성장 배치 전체 몬스터="+parent.GetComponentsInChildren<UPlayGround.MonsterActor>(true).Length+" / 조우="+parent.GetComponentsInChildren<UPlayGround.Group.MonsterGroupController>(true).Length,
        "기존 진행 ID·좌표 보존(identities-before.txt), 실제 Play Mode 전투·성능=사용자 검수 대기"});
    return System.IO.File.ReadAllText(folder+"/application.txt");
}catch{
    Undo.RevertAllDownToGroup(undoGroup);if(root!=null)UnityEngine.Object.DestroyImmediate(root);
    System.IO.File.WriteAllText(planPath,originalText);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);throw;
}
