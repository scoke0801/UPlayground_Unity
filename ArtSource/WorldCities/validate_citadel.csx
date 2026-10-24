var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("StonegateCitadel");
if(!scene.IsValid()||Application.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new System.Exception("돌등성 Edit Mode 컴파일 완료 후 실행하세요.");
var roots=scene.GetRootGameObjects();var environment=roots.First(r=>r.name=="CityEnvironment");
var residents=roots.First(r=>r.name=="CitadelResidents");
var report=new System.Collections.Generic.List<string>();var errors=new System.Collections.Generic.List<string>();
int missing=0,invalidMaterials=0;
foreach(var root in roots)foreach(var t in root.GetComponentsInChildren<Transform>(true)){
    missing+=UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
    foreach(var r in t.GetComponents<Renderer>())foreach(var m in r.sharedMaterials)if(m==null||m.shader==null||m.shader.name=="Hidden/InternalErrorShader")invalidMaterials++;
}
report.Add("Missing scripts="+missing);report.Add("Invalid materials="+invalidMaterials);
if(missing>0||invalidMaterials>0)errors.Add("컴포넌트/재질 누락");
var database=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDatabase>("Assets/10.Datas/Actor/DataBase/ActorDatabase.asset");
var actors=residents.GetComponentsInChildren<UPlayGround.NpcActor>();
if(actors.Length!=12)errors.Add("주민 수="+actors.Length);
foreach(var actor in actors){
    var settings=new UnityEditor.SerializedObject(actor);
    string id=settings.FindProperty("_actorId").stringValue;
    var definition=settings.FindProperty("_definition").objectReferenceValue as UPlayGround.Data.Actor.ActorDefinitionSO;
    var data=settings.FindProperty("_data").objectReferenceValue as UPlayGround.Data.Actor.NpcActorSO;
    if(definition==null||definition.actorId!=id||definition.prefab==null||definition.npcData!=data||database.All.Count(d=>d!=null&&d.actorId==id)!=1)errors.Add(id+" 데이터/DB 연결");
    var animator=actor.GetComponent<Animator>();
    if(animator==null||animator.avatar==null||!animator.avatar.isValid||!animator.avatar.isHuman)errors.Add(id+" 아바타");
    var animancer=actor.GetComponent<Animancer.AnimancerComponent>();
    if(new UnityEditor.SerializedObject(animancer).FindProperty("_Animator").objectReferenceValue!=animator)errors.Add(id+" Animancer 연결");
    var motor=actor.GetComponent<KinematicCharacterController.KinematicCharacterMotor>();
    if(new UnityEditor.SerializedObject(motor).FindProperty("Capsule").objectReferenceValue!=actor.GetComponent<CapsuleCollider>())errors.Add(id+" 모터 콜라이더 연결");
    foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
        if(skin.sharedMesh==null||skin.rootBone==null||!skin.rootBone.IsChildOf(actor.transform)||skin.bones.Any(b=>b==null||!b.IsChildOf(actor.transform)))errors.Add(id+" 스킨 본 연결 "+skin.name);
    }
    if(data!=null&&data.merchantCatalog!=null){string error;if(!data.merchantCatalog.TryValidate(out error))errors.Add(id+" 상점: "+error);report.Add(id+" catalog="+data.merchantCatalog.MerchantId+", offers="+data.merchantCatalog.Offers.Count);}
}
report.Add("NPC count="+actors.Length);
// 다른 열린 씬의 내비게이션이 같은 좌표의 실패를 가리지 못하게 검사 동안만 제외한다.
var surface=environment.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
var otherSurfaces=UnityEngine.Object.FindObjectsByType<Unity.AI.Navigation.NavMeshSurface>(FindObjectsSortMode.None).Where(s=>s!=surface&&s.isActiveAndEnabled&&s.navMeshData!=null).ToArray();
foreach(var other in otherSurfaces)other.RemoveData();
try{
    surface.BuildNavMesh();var generated=surface.navMeshData;
    if(generated==null)throw new System.Exception("NavMesh 생성 실패");
    var previous=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>("Assets/10.Datas/World/WorldCities/StonegateCitadel/Navigation.asset");
    surface.RemoveData();UnityEditor.EditorUtility.CopySerialized(generated,previous);previous.name="Navigation";surface.navMeshData=previous;surface.AddData();UnityEditor.EditorUtility.SetDirty(previous);UnityEditor.AssetDatabase.SaveAssetIfDirty(previous);
    if(!UnityEditor.AssetDatabase.Contains(generated))UnityEngine.Object.DestroyImmediate(generated);
    Physics.SyncTransforms();
    var destinations=new System.Collections.Generic.Dictionary<string,Vector3>();
    destinations.Add("Gate",new Vector3(180,10,136));destinations.Add("Haru",new Vector3(171,10,148));destinations.Add("Rest",new Vector3(171,10,205));destinations.Add("Dam",new Vector3(220,10,215));destinations.Add("Keep",new Vector3(180,10,221));destinations.Add("HarborExit",new Vector3(218,10,102));destinations.Add("LakeExit",new Vector3(177,10,40));
    foreach(var actor in actors)destinations.Add(actor.name,actor.transform.position);
    foreach(var entry in destinations){
        UnityEngine.AI.NavMeshHit start,end;
        bool foundStart=UnityEngine.AI.NavMesh.SamplePosition(new Vector3(180,10,55),out start,1,UnityEngine.AI.NavMesh.AllAreas);
        var target=entry.Value;
        // 외곽 출구는 언덕 위에 있으므로 도시 안쪽의 고정 높이로 검사하면 정상 경로도 실패한다.
        target.y=environment.GetComponentInChildren<Terrain>().SampleHeight(target)+.1f;
        bool foundEnd=UnityEngine.AI.NavMesh.SamplePosition(target,out end,.85f,UnityEngine.AI.NavMesh.AllAreas);
        var path=new UnityEngine.AI.NavMeshPath();
        bool valid=foundStart&&foundEnd&&UnityEngine.AI.NavMesh.CalculatePath(start.position,end.position,UnityEngine.AI.NavMesh.AllAreas,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;
        report.Add("Path "+entry.Key+"="+valid);if(!valid)errors.Add("경로 "+entry.Key);
    }
    var obstacles=environment.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger&&!(c is TerrainCollider)).ToArray();
    foreach(var actor in actors){
        var capsule=actor.GetComponent<CapsuleCollider>();
        foreach(var obstacle in obstacles){
            if(!capsule.bounds.Intersects(obstacle.bounds))continue;
            Vector3 direction;float distance;
            if(Physics.ComputePenetration(capsule,capsule.transform.position,capsule.transform.rotation,obstacle,obstacle.transform.position,obstacle.transform.rotation,out direction,out distance)&&distance>.08f)errors.Add(actor.name+" 겹침 "+obstacle.name+"="+distance.ToString("F2"));
        }
    }
}finally{foreach(var other in otherSurfaces)if(other!=null&&other.isActiveAndEnabled)other.AddData();}
report.Add("Errors="+errors.Count);report.AddRange(errors);
System.IO.Directory.CreateDirectory("output/CitadelImprovement");System.IO.File.WriteAllLines("output/CitadelImprovement/validation.txt",report.ToArray());
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
return string.Join("\n",report.ToArray());
