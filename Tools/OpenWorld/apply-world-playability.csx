// 사전 검사와 Undo로 기존 진행 ID를 보존하며 저장된 월드에 한 번만 적용한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string folder="output/OpenWorld/WorldPlayability";
if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.isDirty || scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
if(System.IO.File.Exists(folder+"/application.txt"))throw new Exception("이미 적용한 작업입니다.");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/world-playability-resolved.json"),typeof(Dictionary<string,object>));
var sites=(Dictionary<string,object>[])read(plan["sites"].ToString(),typeof(Dictionary<string,object>[]));
Func<Dictionary<string,object>,Vector3> position=r=>{var a=(float[])read(r["position"].ToString(),typeof(float[]));return new Vector3(a[0],a[1],a[2]);};
var existing=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).ToDictionary(c=>c.Guid);
var baseline=existing.ToDictionary(p=>p.Key,p=>EditorJsonUtility.ToJson(p.Value));
var definitions=AssetDatabase.FindAssets("t:ActorDefinitionSO").Select(AssetDatabase.GUIDToAssetPath).Select(p=>AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDefinitionSO>(p)).Where(d=>d!=null).ToDictionary(d=>d.actorId);
var validate=typeof(UPlayGround.Editor.P09Builder.P09ProgressionBuilder).GetMethod("ValidateCombat",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
var database=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDatabase>("Assets/10.Datas/Actor/DataBase/ActorDatabase.asset");
var model=AssetDatabase.LoadAssetAtPath<GameObject>((string)plan["pickupModel"]);
var interaction=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.InteractableActorSO>((string)plan["interaction"]);
if(model==null||interaction==null||GameObject.Find("유적 탐험과 보급")!=null)throw new Exception("원본 데이터 또는 중복 배치 확인 필요");
foreach(var site in sites) foreach(string key in new[]{"moves","enemies","loot"}) foreach(var item in (Dictionary<string,object>[])read(site[key].ToString(),typeof(Dictionary<string,object>[]))) {
    string guid=(string)item["guid"];
    if(key=="moves") {if(!existing.ContainsKey(guid)||existing[guid].GetComponent<UPlayGround.MonsterActor>()==null)throw new Exception("이동할 몬스터 없음: "+guid);}
    else if(existing.ContainsKey(guid))throw new Exception("기존 진행 ID 충돌: "+guid);
    if(key=="enemies") {
        var d=definitions[(string)item["actorId"]];
        if(d.prefab==null||database.GetDefinition(d.actorId)!=d)throw new Exception("몬스터 DB 연결 오류: "+d.actorId);
        validate.Invoke(null,new object[]{d});
    }
    if(key=="loot"&&AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Item.ItemSO>((string)item["item"])==null)throw new Exception("보상 아이템 없음");
}
System.IO.Directory.CreateDirectory(folder+"/backup");
if(!System.IO.File.Exists(folder+"/backup/LakeVillageWorld.unity"))System.IO.File.Copy(scene.path,folder+"/backup/LakeVillageWorld.unity");
System.IO.File.WriteAllLines(folder+"/identities-before.txt",existing.Select(p=>p.Key+"|"+p.Value.transform.position.ToString("F4")));
Undo.IncrementCurrentGroup();int undoGroup=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("유적의 조우와 보상 연결");
try {
    var root=new GameObject("유적 탐험과 보급");Undo.RegisterCreatedObjectUndo(root,"유적 콘텐츠");root.transform.SetParent(GameObject.Find("탐험 콘텐츠").transform,false);
    var movedParents=new HashSet<Transform>();int moved=0,enemies=0,loot=0;
    foreach(var site in sites) {
        var group=new GameObject((string)site["name"]);group.transform.SetParent(root.transform,false);
        var members=(Dictionary<string,object>[])read(site["enemies"].ToString(),typeof(Dictionary<string,object>[]));
        if(members.Length>0) {
            group.transform.position=position(members[0]);
            var controller=group.AddComponent<UPlayGround.Group.MonsterGroupController>();
            var settings=new SerializedObject(controller);settings.FindProperty("_meleeSlotCap").intValue=1;settings.FindProperty("_rangedSlotCap").intValue=1;settings.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach(var item in (Dictionary<string,object>[])read(site["moves"].ToString(),typeof(Dictionary<string,object>[]))) {
            var actor=existing[(string)item["guid"]];Undo.RecordObject(actor.transform,"유적 내부 방어 위치");
            actor.transform.SetPositionAndRotation(position(item),Quaternion.Euler(0,Convert.ToSingle(item["yaw"]),0));
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor.transform);movedParents.Add(actor.transform.parent);moved++;
        }
        foreach(var item in members) {
            var d=definitions[(string)item["actorId"]];
            var go=(GameObject)PrefabUtility.InstantiatePrefab(d.prefab,group.transform);
            go.transform.SetPositionAndRotation(position(item),Quaternion.Euler(0,Convert.ToSingle(item["yaw"]),0));
            var identity=go.GetComponent<UPlayGround.Components.SceneEntityId>()??go.AddComponent<UPlayGround.Components.SceneEntityId>();identity.EditorSetGuid((string)item["guid"]);
            PrefabUtility.RecordPrefabInstancePropertyModifications(identity);PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);enemies++;
        }
        foreach(var item in (Dictionary<string,object>[])read(site["loot"].ToString(),typeof(Dictionary<string,object>[]))) {
            var go=new GameObject((string)site["name"]+"_보급 상자_"+loot);go.transform.SetParent(group.transform,false);go.transform.SetPositionAndRotation(position(item),Quaternion.Euler(0,Convert.ToSingle(item["yaw"]),0));
            go.layer=LayerMask.NameToLayer("InteractableObject");
            var collider=go.AddComponent<SphereCollider>();collider.radius=.8f;collider.center=Vector3.up*.6f;
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(model,go.transform);visual.transform.localPosition=Vector3.zero;
            foreach(var c in visual.GetComponentsInChildren<Collider>(true))c.enabled=false;
            go.AddComponent<UPlayGround.DropItemActor>().Init(AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Item.ItemSO>((string)item["item"]),Convert.ToInt32(item["count"]),interaction);
            go.AddComponent<UPlayGround.Components.SceneEntityId>().EditorSetGuid((string)item["guid"]);loot++;
        }
    }
    // 그룹 경계와 재스폰의 홈 위치도 새 방어 위치를 따라가야 한다.
    foreach(var parent in movedParents) {
        var actors=parent.GetComponentsInChildren<UPlayGround.MonsterActor>(true);
        var children=parent.Cast<Transform>().ToArray();var positions=children.Select(t=>t.position).ToArray();
        Undo.RecordObject(parent,"유적 조우 중심");foreach(var child in children)Undo.RecordObject(child,"조우 중심 변경 시 월드 좌표 보존");
        parent.position=new Vector3(actors.Average(a=>a.transform.position.x),actors.Average(a=>a.transform.position.y),actors.Average(a=>a.transform.position.z));
        for(int i=0;i<children.Length;i++){children[i].position=positions[i];PrefabUtility.RecordPrefabInstancePropertyModifications(children[i]);}
    }
    var after=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).ToArray();
    if(after.Any(c=>!c.HasGuid)||after.Select(c=>c.Guid).Distinct().Count()!=after.Length)throw new Exception("빈/중복 ID");
    foreach(var pair in baseline)if(EditorJsonUtility.ToJson(existing[pair.Key])!=pair.Value)throw new Exception("기존 ID 직렬화 변경: "+pair.Key);
    if(root.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))!=0)throw new Exception("Missing Script");
    Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
    Undo.CollapseUndoOperations(undoGroup);
    string result="구역=7 / 기존 ID 보존 이동="+moved+" / 신규 몬스터="+enemies+" / 신규 보상="+loot+" / 기존 ID="+baseline.Count+" / 현재 ID="+after.Length;
    System.IO.File.WriteAllText(folder+"/application.txt",result);return result;
} catch {Undo.RevertAllDownToGroup(undoGroup);throw;}
