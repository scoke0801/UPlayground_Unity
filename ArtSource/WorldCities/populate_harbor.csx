// 기존 NPC 외형·생활 컴포넌트를 재사용한다. 호수 인물의 식별자와 서사는 복제하지 않는다.
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("SaltwindHarbor");
if(!scene.IsValid()||scene.isDirty||UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new System.Exception("저장된 항구 Edit Mode 필요");
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
if(scene.GetRootGameObjects().Any(g=>g.name=="HarborResidents"))throw new System.Exception("기존 항구 주민 보존");
string directory="Assets/10.Datas/World/WorldCities/SaltwindHarbor/Life";
string prefabDirectory="Assets/03.Prefabs/Characters/HarborResidents";
var database=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDatabase>("Assets/10.Datas/Actor/DataBase/ActorDatabase.asset");
var original=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Merchant.MerchantCatalogSO>("Assets/10.Datas/Merchant/Merchant_Penny.asset");
if(database==null||original==null)throw new System.Exception("NPC DB 또는 기존 상점 누락");
var expectedPaths=new System.Collections.Generic.List<string>();
foreach(string name in new[]{"Goeun","Taesu","Minsu","Bora","Dongjin","Haejin","Yuna","Seongho"}){
    string id="NPC_Harbor_"+name;
    if(database.All.Any(d=>d!=null&&d.actorId==id))throw new System.Exception("기존 NPC 보존: "+id);
    expectedPaths.Add(directory+"/"+id+".asset");expectedPaths.Add(directory+"/"+id+"_Definition.asset");expectedPaths.Add(prefabDirectory+"/"+id+".prefab");
}
foreach(string name in new[]{"Merchant_HarborSupplies","Merchant_HarborCatch","HarborFishing","HarborRest"})expectedPaths.Add(directory+"/"+name+".asset");
foreach(string path in expectedPaths)if(System.IO.File.Exists(path)||UnityEditor.AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new System.Exception("기존 에셋 보존: "+path);
foreach(string name in new[]{"NPC_Shop","NPC_Jun","Npc_Daon","Npc_Yeonmok","Npc_Sodam"}){
    var source=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/ScenarioResidents/Npc/"+name+"/"+name+".prefab");
    if(source==null||source.GetComponent<UPlayGround.NpcActor>()==null||source.GetComponent<UPlayGround.Components.NpcBrain>()==null)throw new System.Exception("원본 주민 구성 누락: "+name);
    if(new UnityEditor.SerializedObject(source.GetComponent<UPlayGround.NpcActor>()).FindProperty("_definition").objectReferenceValue==null)throw new System.Exception("원본 정의 누락: "+name);
}
foreach(string path in new[]{"Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/LivingWorld/LW_MedicinePack.asset","Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Eat_Loop.asset","Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Male_Idle.asset","Assets/10.Datas/Actor/Interaction/FishingZone.asset","Assets/10.Datas/Actor/Interaction/RestPoint.asset"})if(UnityEditor.AssetDatabase.LoadMainAssetAtPath(path)==null)throw new System.Exception("참조 누락: "+path);
string backup="output/SaltwindHarbor/BeforeResidents_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss");System.IO.Directory.CreateDirectory(backup);
System.IO.File.Copy(scene.path,backup+"/SaltwindHarbor.unity");System.IO.File.Copy("Assets/10.Datas/Actor/DataBase/ActorDatabase.asset",backup+"/ActorDatabase.asset");
string databaseJson=UnityEditor.EditorJsonUtility.ToJson(database);System.IO.File.WriteAllText(backup+"/ActorDatabase.json",databaseJson);
var originalDefinitions=database.All.ToArray();
System.IO.Directory.CreateDirectory(prefabDirectory);UnityEditor.AssetDatabase.Refresh();
Transform root=null;Transform interactions=null;
try{
root=new GameObject("HarborResidents").transform;
var catalogs=new UPlayGround.Data.Merchant.MerchantCatalogSO[2];
for(int i=0;i<2;i++){
    var c=UnityEngine.Object.Instantiate(original);c.name=i==0?"Merchant_HarborSupplies":"Merchant_HarborCatch";
    var so=new UnityEditor.SerializedObject(c);so.FindProperty("_merchantId").stringValue=i==0?"merchant_saltwind_supplies":"merchant_saltwind_catch";
    so.FindProperty("_displayName").stringValue=i==0?"고은의 여행 물품":"태수의 생선과 재료 매입";
    var offers=so.FindProperty("_offers");
    for(int j=offers.arraySize-1;j>=0;j--)if(i==0?j>=4:j<4)offers.DeleteArrayElementAtIndex(j);
    so.ApplyModifiedPropertiesWithoutUndo();string error;if(!c.TryValidate(out error))throw new System.Exception(error);
    UnityEditor.AssetDatabase.CreateAsset(c,directory+"/"+c.name+".asset");catalogs[i]=c;
}
var ids=new[]{"Goeun","Taesu","Minsu","Bora","Dongjin","Haejin","Yuna","Seongho"};
var labels=new[]{"고은","태수","민수","보라","동진","해진","유나","성호"};
var sourceNames=new[]{"NPC_Shop","NPC_Jun","NPC_Jun","Npc_Daon","Npc_Yeonmok","Npc_Sodam","Npc_Daon","Npc_Yeonmok"};
var positions=new[]{new Vector3(172.3f,10.08f,152.2f),new Vector3(188.7f,10.08f,161.2f),new Vector3(145,10.08f,221.25f),new Vector3(164,10.08f,202),new Vector3(161,10.08f,228),new Vector3(192,10.08f,197),new Vector3(200.5f,10.08f,224.8f),new Vector3(119,10.08f,252)};
var rotations=new[]{90f,270f,180f,0f,90f,240f,270f,300f};
var actors=new System.Collections.Generic.List<UPlayGround.NpcActor>();
string motionRoot="Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/";
for(int i=0;i<ids.Length;i++){
    string id="NPC_Harbor_"+ids[i];if(database.Contains(id))throw new System.Exception("이미 등록된 주민: "+id);
    string source="Assets/03.Prefabs/Characters/ScenarioResidents/Npc/"+sourceNames[i]+"/"+sourceNames[i]+".prefab";
    var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(source);if(prefab==null)throw new System.Exception(source);
    var actorSource=prefab.GetComponent<UPlayGround.NpcActor>();
    var sourceSettings=new UnityEditor.SerializedObject(actorSource);
    var data=ScriptableObject.CreateInstance<UPlayGround.Data.Actor.NpcActorSO>();data.name=id;data.actorName=labels[i];data.storyEntries=new UPlayGround.Story.StoryEntrySO[0];data.dialogueGraph=null;data.merchantCatalog=i<2?catalogs[i]:null;
    data.description=i==0?"길을 떠나기 전에 필요한 물건을 살 수 있다.":i==1?"잡은 생선과 모은 재료를 판다.":"소금바람항 주민";
    UnityEditor.AssetDatabase.CreateAsset(data,directory+"/"+id+".asset");
    var definition=UnityEngine.Object.Instantiate((UPlayGround.Data.Actor.ActorDefinitionSO)sourceSettings.FindProperty("_definition").objectReferenceValue);
    definition.name=id+"_Definition";definition.actorId=id;definition.displayName=labels[i];definition.npcData=data;definition.description="항구 생활 주민. 기존 외형을 재사용하며 호수 서사에는 참여하지 않는다.";
    UnityEditor.AssetDatabase.CreateAsset(definition,directory+"/"+definition.name+".asset");
    var g=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,scene);g.name=id;g.transform.SetParent(root);g.transform.position=positions[i];g.transform.rotation=Quaternion.Euler(0,rotations[i],0);
    var actor=g.GetComponent<UPlayGround.NpcActor>();var settings=new UnityEditor.SerializedObject(actor);
    settings.FindProperty("_definition").objectReferenceValue=definition;settings.FindProperty("_actorId").stringValue=id;settings.FindProperty("_data").objectReferenceValue=data;settings.ApplyModifiedPropertiesWithoutUndo();UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
    var brain=g.GetComponent<UPlayGround.Components.NpcBrain>();var b=new UnityEditor.SerializedObject(brain);
    b.FindProperty("_enableWander").boolValue=i==3||i==5;b.FindProperty("_patrolRadius").floatValue=i==3?2.4f:1.6f;b.FindProperty("_patrolWaitTime").floatValue=6+i;
    b.FindProperty("_idleLookAngle").floatValue=i<2?18:28;
    b.FindProperty("_workMotion").objectReferenceValue=i==2?UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>(motionRoot+"LivingWorld/LW_MedicinePack.asset"):i==6?UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>(motionRoot+"Npc_Eat_Loop.asset"):null;
    b.ApplyModifiedPropertiesWithoutUndo();UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(brain);
    g.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
    var variant=UnityEditor.PrefabUtility.SaveAsPrefabAsset(g,prefabDirectory+"/"+id+".prefab");
    g.transform.SetPositionAndRotation(positions[i],Quaternion.Euler(0,rotations[i],0));UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);
    if(variant==null)throw new System.Exception("프리팹 저장 실패: "+id);
    definition.prefab=variant;UnityEditor.EditorUtility.SetDirty(definition);UnityEditor.AssetDatabase.SaveAssetIfDirty(definition);
    database.AddDefinition(definition);actors.Add(actor);
}
// 일꾼 한 명은 짐터와 부두를 오가고 저녁에는 지붕 아래서 쉰다.
var worker=actors[4];var routine=worker.gameObject.AddComponent<UPlayGround.Components.LivingWorld.NpcRoutine>();
var workPoints=new UPlayGround.Components.LivingWorld.NpcWorkPoint[3];
var pointPositions=new[]{new Vector3(161,10.05f,228),new Vector3(165,10.22f,239),new Vector3(157,10.05f,220)};
for(int i=0;i<3;i++){
    var p=new GameObject("HarborWorkPoint_"+i);p.transform.SetParent(root);p.transform.position=pointPositions[i];p.transform.rotation=Quaternion.Euler(0,i==1?90:180,0);
    var point=p.AddComponent<UPlayGround.Components.LivingWorld.NpcWorkPoint>();var so=new UnityEditor.SerializedObject(point);
    so.FindProperty("_mapId").stringValue="SaltwindHarbor";so.FindProperty("_pointId").stringValue="saltwind.dock."+i;so.FindProperty("_isSheltered").boolValue=i==2;
    so.FindProperty("_motion").objectReferenceValue=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>(motionRoot+"Npc_Male_Idle.asset");so.ApplyModifiedPropertiesWithoutUndo();workPoints[i]=point;
}
var routineSettings=new UnityEditor.SerializedObject(routine);routineSettings.FindProperty("_actor").objectReferenceValue=worker;routineSettings.FindProperty("_mapId").stringValue="SaltwindHarbor";routineSettings.FindProperty("_isPlacementReady").boolValue=true;routineSettings.FindProperty("_startDelaySeconds").floatValue=2;
var steps=routineSettings.FindProperty("_steps");steps.arraySize=2;for(int i=0;i<2;i++){var step=steps.GetArrayElementAtIndex(i);step.FindPropertyRelative("point").objectReferenceValue=workPoints[i];step.FindPropertyRelative("seconds").floatValue=16+i*7;step.FindPropertyRelative("waypoints").arraySize=0;}
routineSettings.FindProperty("_rest").FindPropertyRelative("point").objectReferenceValue=workPoints[2];routineSettings.FindProperty("_rest").FindPropertyRelative("seconds").floatValue=25;routineSettings.ApplyModifiedPropertiesWithoutUndo();
var approach=new GameObject("HarborDockApproach").transform;approach.SetParent(root);approach.position=new Vector3(165,10.05f,226);
foreach(var step in new[]{routineSettings.FindProperty("_steps").GetArrayElementAtIndex(0),routineSettings.FindProperty("_steps").GetArrayElementAtIndex(1),routineSettings.FindProperty("_rest")}){var waypoints=step.FindPropertyRelative("waypoints");waypoints.arraySize=1;waypoints.GetArrayElementAtIndex(0).objectReferenceValue=approach;}routineSettings.ApplyModifiedPropertiesWithoutUndo();
interactions=new GameObject("HarborActivities").transform;
var entityType=typeof(UPlayGround.Components.SceneEntityId);
for(int i=0;i<2;i++){
    bool fishing=i==0;var originalData=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.InteractableActorSO>("Assets/10.Datas/Actor/Interaction/"+(fishing?"FishingZone":"RestPoint")+".asset");
    var data=UnityEngine.Object.Instantiate(originalData);data.name=fishing?"HarborFishing":"HarborRest";data.actorName=fishing?"부두 낚시터":"바닷바람 쉼터";data.description=fishing?"잡은 생선은 시장의 태수에게 팔 수 있다.":"잠깐 쉬며 몸을 추스른다.";
    if(fishing){data.fishingDepleteCatchCount=3;data.regrowGameMinutes=180;data.dropItems[0].maximumDropCount=2;data.dropItems[0].itemData=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Item.ItemSO>("Assets/10.Datas/Item/Item_Monster_Common_Fish.asset");if(data.dropItems[0].itemData==null)throw new System.Exception("물고기 아이템 누락");}
    UnityEditor.AssetDatabase.CreateAsset(data,directory+"/"+data.name+".asset");
    var g=new GameObject(fishing?"HarborFishing":"HarborRest");g.transform.SetParent(interactions);g.transform.position=fishing?new Vector3(195,10.25f,246):new Vector3(129.5f,10.08f,226.5f);g.transform.rotation=Quaternion.identity;g.layer=LayerMask.NameToLayer("InteractableObject");
    var collider=g.AddComponent<BoxCollider>();collider.isTrigger=true;collider.size=new Vector3(2.4f,2,2.4f);collider.center=Vector3.up;
    MonoBehaviour component=fishing?(MonoBehaviour)g.AddComponent<UPlayGround.GatheringActor>():g.AddComponent<UPlayGround.RestPointActor>();
    var so=new UnityEditor.SerializedObject(component);so.FindProperty(fishing?"_interactableData":"_data").objectReferenceValue=data;so.ApplyModifiedPropertiesWithoutUndo();
    var entity=g.AddComponent(entityType);entityType.GetMethod("EditorSetGuid").Invoke(entity,new object[]{fishing?"saltwind.fishing.eastpier":"saltwind.rest.westquay"});
}
if(database.All.Count!=originalDefinitions.Length+8||!originalDefinitions.SequenceEqual(database.All.Take(originalDefinitions.Length)))throw new System.Exception("기존 DB 등록 순서·참조 보존 검사 실패");
foreach(var actor in actors)if(actor.GetComponentsInChildren<Transform>(true).Any(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new System.Exception("Missing Script: "+actor.name);
UnityEditor.AssetDatabase.SaveAssetIfDirty(database);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("씬 저장 실패");
System.IO.File.WriteAllText("output/SaltwindHarbor/residents-applied.txt","주민 8명, 상점 2개, 부두 일과, 낚시·회복 배치\n백업: "+backup+"\n기존 DB 참조·순서 보존, Missing Script 0");
return "주민 8명, 상점 2개, 부두 일과, 낚시·회복 배치. 백업: "+backup;
}catch{
    // 씬과 공용 DB 연결을 복구하고 생성 에셋은 진단용 목록과 함께 보존한다.
    if(root!=null)UnityEngine.Object.DestroyImmediate(root.gameObject);if(interactions!=null)UnityEngine.Object.DestroyImmediate(interactions.gameObject);
    UnityEditor.EditorJsonUtility.FromJsonOverwrite(databaseJson,database);database.InvalidateLookup();UnityEditor.EditorUtility.SetDirty(database);UnityEditor.AssetDatabase.SaveAssetIfDirty(database);
    System.IO.File.WriteAllLines(backup+"/CreatedAssetsAfterFailure.txt",expectedPaths.Where(path=>System.IO.File.Exists(path)).ToArray());
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    throw;
}
