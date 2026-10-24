var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("SaltwindHarbor");
if(!scene.IsValid()||UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new System.Exception("항구 Edit Mode 필요");
var report=new System.Collections.Generic.List<string>();var failures=new System.Collections.Generic.List<string>();
System.Action<bool,string> check=(ok,message)=>{report.Add((ok?"PASS ":"FAIL ")+message);if(!ok)failures.Add(message);};
var roots=scene.GetRootGameObjects();
var npcs=roots.SelectMany(g=>g.GetComponentsInChildren<UPlayGround.NpcActor>(true)).ToArray();
check(npcs.Length==8,"주민 8명");
var database=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDatabase>("Assets/10.Datas/Actor/DataBase/ActorDatabase.asset");
var ids=new System.Collections.Generic.HashSet<string>();
foreach(var npc in npcs){
    var so=new UnityEditor.SerializedObject(npc);var definition=(UPlayGround.Data.Actor.ActorDefinitionSO)so.FindProperty("_definition").objectReferenceValue;
    check(definition!=null&&ids.Add(definition.actorId),npc.name+" 독립 정의·ID");if(definition==null)continue;
    check(database.All.Count(d=>d!=null&&d.actorId==definition.actorId)==1,npc.name+" DB 단일 등록");
    check(so.FindProperty("_data").objectReferenceValue==definition.npcData&&definition.npcData!=null&&definition.npcData.dialogueGraph==null&&definition.npcData.storyEntries.Length==0,npc.name+" 호수 서사 분리");
    var prefab=definition.prefab;check(prefab!=null&&UnityEditor.AssetDatabase.GetAssetPath(prefab).StartsWith("Assets/03.Prefabs/Characters/HarborResidents/"),npc.name+" 항구 프리팹");
    if(prefab!=null){var p=new UnityEditor.SerializedObject(prefab.GetComponent<UPlayGround.NpcActor>());check(p.FindProperty("_definition").objectReferenceValue==definition&&p.FindProperty("_data").objectReferenceValue==definition.npcData,npc.name+" 스폰 데이터 일치");}
}
string directory="Assets/10.Datas/World/WorldCities/SaltwindHarbor/Life/";
var selector=typeof(UPlayGround.UI.UI_Scene_Merchant).GetMethod("GetInitialTradeMode",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
check(selector!=null,"상점 초기 탭 코드 컴파일");
var catalogPaths=new[]{"Assets/10.Datas/Merchant/Merchant_Penny.asset",directory+"Merchant_HarborSupplies.asset",directory+"Merchant_HarborCatch.asset"};
var merchantIds=new System.Collections.Generic.HashSet<string>();
for(int i=0;i<catalogPaths.Length;i++){
    var catalog=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Merchant.MerchantCatalogSO>(catalogPaths[i]);string error="카탈로그 없음";
    check(catalog!=null&&catalog.TryValidate(out error),"카탈로그 유효성 "+catalogPaths[i]+" "+error);if(catalog==null)continue;
    check(merchantIds.Add(catalog.MerchantId),"상점 저장 키 분리 "+catalog.MerchantId);
    if(selector!=null)check((UPlayGround.Data.Merchant.MerchantTradeMode)selector.Invoke(null,new object[]{catalog.Offers})==(i==2?UPlayGround.Data.Merchant.MerchantTradeMode.Sell:UPlayGround.Data.Merchant.MerchantTradeMode.Buy),"초기 탭 "+catalog.name);
}
var fishData=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.InteractableActorSO>(directory+"HarborFishing.asset");
var buyer=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Merchant.MerchantCatalogSO>(catalogPaths[2]);
UPlayGround.Data.Merchant.MerchantOffer offer;
check(fishData!=null&&fishData.dropItems.Count==1&&fishData.dropItems[0].itemData.itemId==100008&&buyer.TryGetOffer(100008,out offer)&&offer.CanSell,"낚시 물고기 → 태수 매입 연결");
check(fishData.fishingDepleteCatchCount==3&&fishData.regrowGameMinutes==180,"낚시 소진·게임 시간 재생성 설정");
var entities=roots.SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Where(e=>e.transform.parent!=null&&e.transform.parent.name=="HarborActivities").ToArray();
check(entities.Length==2&&entities.All(e=>e.HasGuid)&&entities.Select(e=>e.Guid).Distinct().Count()==2,"생활 기능 저장 ID 2개 고유");
foreach(var entity in entities)report.Add("ENTITY "+entity.name+"="+entity.Guid);
var routines=roots.SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.LivingWorld.NpcRoutine>(true)).ToArray();
check(routines.Length==1,"부두 일과 1개");
if(routines.Length==1){var r=new UnityEditor.SerializedObject(routines[0]);var steps=r.FindProperty("_steps");check(steps.arraySize==2&&steps.GetArrayElementAtIndex(0).FindPropertyRelative("point").objectReferenceValue!=null&&steps.GetArrayElementAtIndex(1).FindPropertyRelative("point").objectReferenceValue!=null&&r.FindProperty("_rest").FindPropertyRelative("point").objectReferenceValue!=null,"작업 2점·휴식 1점 연결");}
int missing=0,materials=0;
foreach(var root in roots)foreach(var t in root.GetComponentsInChildren<Transform>(true)){
    missing+=UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
    foreach(var r in t.GetComponents<Renderer>())foreach(var material in r.sharedMaterials)if(material==null||material.shader==null||material.shader.name=="Hidden/InternalErrorShader")materials++;
}
check(missing==0,"Missing Script 0");check(materials==0,"누락·오류 재질 0");
var others=UnityEngine.Object.FindObjectsByType<Unity.AI.Navigation.NavMeshSurface>(FindObjectsSortMode.None).Where(s=>s.gameObject.scene!=scene&&s.isActiveAndEnabled).ToArray();
try{
    foreach(var surface in others)surface.RemoveData();Physics.SyncTransforms();
    UnityEngine.AI.NavMeshHit start;bool hasStart=UnityEngine.AI.NavMesh.SamplePosition(new Vector3(180,10,55),out start,2,-1);check(hasStart,"진입로 NavMesh");
    var destinations=npcs.Select(n=>n.transform).Concat(entities.Select(e=>e.transform)).Concat(roots.SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.LivingWorld.NpcWorkPoint>()).Select(p=>p.transform));
    foreach(var destination in destinations){UnityEngine.AI.NavMeshHit end;var path=new UnityEngine.AI.NavMeshPath();bool ok=hasStart&&UnityEngine.AI.NavMesh.SamplePosition(destination.position,out end,.8f,-1)&&UnityEngine.AI.NavMesh.CalculatePath(start.position,end.position,-1,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;check(ok,"항구 단독 경로 "+destination.name);}
    foreach(var npc in npcs){var p=npc.transform.position;var blockers=Physics.OverlapCapsule(p+Vector3.up*.5f,p+Vector3.up*1.5f,.28f,LayerMask.GetMask("Ground"),QueryTriggerInteraction.Ignore).Where(c=>c.gameObject.scene==scene&&!c.transform.IsChildOf(npc.transform)).ToArray();check(blockers.Length==0,"NPC 배치 충돌 "+npc.name+" "+string.Join(",",blockers.Select(c=>c.name).ToArray()));}
    var residentRoot=roots.First(g=>g.name=="HarborResidents").transform;var approach=residentRoot.Find("HarborDockApproach");
    foreach(var point in residentRoot.GetComponentsInChildren<UPlayGround.Components.LivingWorld.NpcWorkPoint>()){
        var from=point.transform.position;var delta=approach.position-from;
        var blockers=Physics.CapsuleCastAll(from+Vector3.up*.5f,from+Vector3.up*1.5f,.28f,delta.normalized,delta.magnitude,LayerMask.GetMask("Ground"),QueryTriggerInteraction.Ignore).Where(h=>h.collider.gameObject.scene==scene).ToArray();
        check(blockers.Length==0,"일과 직선 경유 경로 "+point.name+" "+string.Join(",",blockers.Select(h=>h.collider.name).ToArray()));
    }
}finally{foreach(var surface in others)surface.AddData();}
System.IO.Directory.CreateDirectory("output/SaltwindHarbor");System.IO.File.WriteAllLines("output/SaltwindHarbor/life-validation.txt",report.ToArray());
if(failures.Count>0)throw new System.Exception(string.Join("\n",failures.ToArray()));
return report;
