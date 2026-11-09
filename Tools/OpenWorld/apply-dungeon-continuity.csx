var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("컴파일이 정상인 LakeVillageWorld Edit Mode가 필요합니다.");
string folder="output/OpenWorld/DungeonContinuity";
System.IO.Directory.CreateDirectory(folder);
if(System.IO.File.Exists(folder+"/applied.json"))throw new Exception("적용 완료한 일회성 저작입니다.");
var jt=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=jt.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
Func<object,string> json=o=>(string)jt.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{o});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/dungeon-continuity-plan.json"),typeof(Dictionary<string,object>));
var sites=(Dictionary<string,object>[])read(plan["sites"].ToString(),typeof(Dictionary<string,object>[]));
var runs=(Dictionary<string,object>[])read(plan["runs"].ToString(),typeof(Dictionary<string,object>[]));
var gates=(Dictionary<string,object>[])read(plan["gates"].ToString(),typeof(Dictionary<string,object>[]));
var rewards=(Dictionary<string,object>[])read(plan["rewards"].ToString(),typeof(Dictionary<string,object>[]));
Func<Dictionary<string,object>,string,float> number=(r,k)=>Convert.ToSingle(r[k]);
Func<Dictionary<string,object>,string,Vector2> point=(r,k)=>{var a=(float[])read(r[k].ToString(),typeof(float[]));return new Vector2(a[0],a[1]);};
var environment=GameObject.Find("AuthoredEnvironment");
var terrain=environment.GetComponentInChildren<Terrain>();
Func<float,float,float> ground=(x,z)=>terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
var parents=sites.ToDictionary(s=>s["id"].ToString(),s=>GameObject.Find(s.ContainsKey("parent")?s["parent"].ToString():"AuthoredEnvironment/던전 공간 재저작/"+s["name"]).transform);
if(parents.Values.Any(t=>t==null||t.Find("연속 석벽과 보상")!=null))throw new Exception("대상 또는 중복 저작 확인 필요");
string kit="Assets/ExternalAssets/Environment/Fantastic Dungeon Pack/prefabs/";
var wallAsset=AssetDatabase.LoadAssetAtPath<GameObject>(kit+"MODULAR/02_COMPS/Wall/PivotMiddle/COMP_Wall_01_M_straight_med.prefab");
var gateAsset=AssetDatabase.LoadAssetAtPath<GameObject>(kit+"MODULAR/02_COMPS/Gateway/COMP_Door_01_large.prefab");
var floorAsset=AssetDatabase.LoadAssetAtPath<GameObject>(kit+"MODULAR/01_PARTS/Floor/PivotMiddle/P_MOD_Floor_01_M_straight_med.prefab");
if(floorAsset==null)floorAsset=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.FindAssets("P_MOD_Floor_01_O_straight_med t:Prefab",new[]{kit.TrimEnd('/')}).Select(AssetDatabase.GUIDToAssetPath).Single(p=>System.IO.Path.GetFileNameWithoutExtension(p)=="P_MOD_Floor_01_O_straight_med"));
var chestAsset=AssetDatabase.LoadAssetAtPath<GameObject>(kit+"PROPS/containers/chests/P_PROP_chest_dungeon_01.prefab");
foreach(var asset in new[]{wallAsset,gateAsset,floorAsset,chestAsset})if(asset==null||asset.GetComponentsInChildren<MonoBehaviour>(true).Length>0)throw new Exception("구조·상자 원본 에셋 확인 필요");
var items=rewards.Select(r=>r["item"].ToString()).Distinct().ToDictionary(k=>k,k=>AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Item.ItemSO>("Assets/10.Datas/Item/Consume/"+k+".asset"));
if(items.Values.Any(i=>i==null))throw new Exception("보상 아이템 누락");
Func<string> identities=()=>string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(v=>v));
string before=identities();
System.IO.File.WriteAllText(folder+"/identities-before.txt",before);
if(!System.IO.File.Exists(folder+"/LakeVillageWorld.before.unity"))if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,folder+"/LakeVillageWorld.before.unity",true))throw new Exception("작업 전 백업 실패");
// Renderer.bounds는 방금 회전·배율을 바꾼 프리팹에서 갱신이 늦을 수 있어 메시 꼭짓점을 직접 변환한다.
Func<GameObject,Bounds> bounds=g=>{
 bool first=true;var b=new Bounds();
 foreach(var f in g.GetComponentsInChildren<MeshFilter>().Where(f=>f.gameObject.activeInHierarchy&&f.sharedMesh!=null)){
  var local=f.sharedMesh.bounds;
  foreach(float x in new[]{local.min.x,local.max.x})foreach(float y in new[]{local.min.y,local.max.y})foreach(float z in new[]{local.min.z,local.max.z}){
   var p=f.transform.TransformPoint(new Vector3(x,y,z));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);
  }
 }if(first)throw new Exception("메시 경계 없음: "+g.name);return b;
};
Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("던전 문과 벽 연결·제단 확장·보물상자 배치");
var groups=new Dictionary<string,Transform>();var wallChecks=new List<object>();var doorChecks=new List<object>();
string interactionPath="Assets/10.Datas/Actor/Interaction/DungeonTreasure.asset";
bool createdInteraction=false;int wallCount=0,removedCount=0;
try{
 foreach(var pair in parents){var g=new GameObject("연속 석벽과 보상");g.transform.SetParent(pair.Value,false);Undo.RegisterCreatedObjectUndo(g,"던전 보수");groups.Add(pair.Key,g.transform);}
 var interaction=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.InteractableActorSO>(interactionPath);
 if(interaction==null){interaction=ScriptableObject.CreateInstance<UPlayGround.Data.Actor.InteractableActorSO>();interaction.actorName="보물상자";interaction.description="뚜껑을 열어 안에 든 물건을 꺼낸다.";interaction.interactionObjectType=UPlayGround.Data.EnumType.InteractionObjectType.DROP_ITEM;interaction.interactionCompleteDuration=1.5f;interaction.interactionMotionSlot=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.InteractableActorSO>("Assets/10.Datas/Actor/Interaction/DropItem.asset").interactionMotionSlot;interaction.showInfoUI=true;interaction.showShakeEffect=false;AssetDatabase.CreateAsset(interaction,interactionPath);createdInteraction=true;}
 var doorBounds=new List<Bounds>();
 foreach(var row in gates){
  string id=row["site"].ToString();float x=number(row,"x"),z=number(row,"z"),scale=number(row,"scale");
  var g=(GameObject)PrefabUtility.InstantiatePrefab(gateAsset,groups[id]);g.name="연결 석문_"+doorBounds.Count;
  foreach(var t in g.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("MOD_Gateway_Door_01_large_"))){t.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}
  g.transform.localScale=new Vector3(scale,1.05f,1);g.transform.rotation=Quaternion.Euler(0,number(row,"yaw"),0);
  float y=row.ContainsKey("base")?number(row,"base"):ground(x,z)-.12f;var b=bounds(g);g.transform.position+=new Vector3(x-b.center.x,y-b.min.y,z-b.center.z);
  b=bounds(g);doorBounds.Add(b);doorChecks.Add(new{site=id,x,z,y,min=new[]{b.min.x,b.min.y,b.min.z},max=new[]{b.max.x,b.max.y,b.max.z}});
  PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);
 }
 for(int ri=0;ri<runs.Length;ri++){
  var row=runs[ri];string id=row["site"].ToString();var a=point(row,"a");var b=point(row,"b");var d=(b-a).normalized;float length=Vector2.Distance(a,b);
  var intervals=new List<Vector2>{new Vector2(0,length)};
  for(int gi=0;gi<gates.Length;gi++){
   var gate=gates[gi];if(gate["site"].ToString()!=id)continue;
   var p=new Vector2(number(gate,"x"),number(gate,"z"));
   if(Mathf.Abs((p.x-a.x)*d.y-(p.y-a.y)*d.x)>.15f)continue;
   bool horizontal=Mathf.Abs(d.x)>.5f;if(horizontal!=(Mathf.Abs(number(gate,"yaw"))<1))continue;
   // 기단의 가장 넓은 외곽이 아니라 기둥 몸통 안쪽까지 벽을 물려 상부에도 틈이 남지 않게 한다.
   float center=Vector2.Dot(p-a,d);float half=2.4f*number(gate,"scale");
   var split=new List<Vector2>();foreach(var v in intervals){float lo=Mathf.Max(v.x,center-half),hi=Mathf.Min(v.y,center+half);if(lo>=hi){split.Add(v);continue;}if(lo>v.x)split.Add(new Vector2(v.x,lo));if(hi<v.y)split.Add(new Vector2(hi,v.y));}intervals=split;
  }
  foreach(var segment in intervals){
   float span=segment.y-segment.x;if(span<.08f)continue;int count=Mathf.CeilToInt(span/4);
   for(int i=0;i<count;i++){
    var p=a+d*(segment.x+span*(i+.5f)/count);var p0=a+d*(segment.x+span*i/count);var p1=a+d*(segment.x+span*(i+1)/count);
    float low=row.ContainsKey("base")?number(row,"base"):Mathf.Min(ground(p0.x,p0.y),ground(p1.x,p1.y))-.2f;
    float high=row.ContainsKey("base")?low:Mathf.Max(ground(p0.x,p0.y),ground(p1.x,p1.y))-.2f;
    var g=(GameObject)PrefabUtility.InstantiatePrefab(wallAsset,groups[id]);g.name="연결 석벽_"+wallCount++;
    g.transform.localScale=new Vector3((span/count+.06f)/4,(number(row,"height")+high-low)/4.35465f,1);
    g.transform.rotation=Quaternion.Euler(0,-Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg,0);
    var wb=bounds(g);g.transform.position+=new Vector3(p.x-wb.center.x,low-wb.min.y,p.y-wb.center.z);
    PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);
    wallChecks.Add(new{site=id,run=ri,a=new[]{p0.x,p0.y},b=new[]{p1.x,p1.y},baseY=low,height=number(row,"height")+high-low});
   }
  }
 }
 // 중앙 사슬 구조의 바닥 높이를 고정하고 가로 규모를 늘려 외벽 안에 배치한다.
 var chain=parents["chain"].Find("ChainTemple");Undo.RecordObject(chain,"사슬 제단 규모");float size=number(plan,"chainScale");
 chain.localScale=new Vector3(size,1.15f,size);chain.position=new Vector3(590+(600-590)*size,140,860);PrefabUtility.RecordPrefabInstancePropertyModifications(chain);
 for(int x=-36;x<=36;x+=4)for(int z=-36;z<=32;z+=4){if(Mathf.Abs(x)<24&&Mathf.Abs(z)<28)continue;float wx=590+x,wz=860+z;var g=(GameObject)PrefabUtility.InstantiatePrefab(floorAsset,groups["chain"]);g.name="제단 외곽 석재 회랑";var b=bounds(g);g.transform.position+=new Vector3(wx-b.center.x,ground(wx,wz)+.04f-b.max.y,wz-b.center.z);PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);}
 foreach(var row in rewards){
  string id=row["site"].ToString();float x=number(row,"x"),z=number(row,"z");float y=row.ContainsKey("base")?number(row,"base"):ground(x,z)+.08f;
  var g=new GameObject("보물상자");g.transform.SetParent(groups[id],false);g.transform.position=new Vector3(x,y,z);g.layer=LayerMask.NameToLayer("InteractableObject");
  var collider=g.AddComponent<BoxCollider>();collider.size=new Vector3(1.5f,1.3f,1.2f);collider.center=Vector3.up*.65f;
  var visual=(GameObject)PrefabUtility.InstantiatePrefab(chestAsset,g.transform);visual.transform.localScale=Vector3.one*1.8f;
  var vb=bounds(visual);visual.transform.position+=new Vector3(x-vb.center.x,y-vb.min.y,z-vb.center.z);
  foreach(var c in visual.GetComponentsInChildren<Collider>()){c.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}PrefabUtility.RecordPrefabInstancePropertyModifications(visual.transform);
  g.AddComponent<UPlayGround.DropItemActor>().Init(items[row["item"].ToString()],Convert.ToInt32(row["count"]),interaction);
  g.AddComponent<UPlayGround.Components.SceneEntityId>().EditorSetGuid(row["guid"].ToString());
 }
 // 기존 장식과 포장은 보존하고, 불연속 구조 모듈만 교체한다.
 foreach(var pair in parents){
  if(pair.Key=="chain")continue;
  Transform source=pair.Key=="corridor"?pair.Value.Find("석조 회랑 개선"):pair.Value;
  var remove=source.Cast<Transform>().Where(t=>t.name.StartsWith("wall_")||t.name.StartsWith("tall_")||t.name.StartsWith("low_")||t.name.StartsWith("arch_")||t.name.StartsWith("column_")||t.name.StartsWith("stump_")||pair.Key=="corridor"&&(t.name.StartsWith("Explore_")||t.name=="석조 아치")).ToArray();
  foreach(var t in remove){if(t.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Length>0)throw new Exception("진행 개체 포함 구조 삭제 차단");Undo.DestroyObjectImmediate(t.gameObject);removedCount++;}
 }
 var expected=before.Split('\n').Concat(groups.Values.SelectMany(t=>t.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4"))).OrderBy(v=>v);
 if(string.Join("\n",expected)!=identities())throw new Exception("기존 진행 ID·좌표 변경 감지");
 int missing=groups.Values.SelectMany(t=>t.GetComponentsInChildren<Transform>(true)).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));if(missing!=0)throw new Exception("Missing Script 발생");
 Physics.SyncTransforms();AssetDatabase.SaveAssetIfDirty(interaction);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
 if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
 Undo.CollapseUndoOperations(undo);
 System.IO.File.WriteAllText(folder+"/walls.json",json(wallChecks));System.IO.File.WriteAllText(folder+"/doors.json",json(doorChecks));
 var report=new{walls=wallCount,gates=gates.Length,rewards=rewards.Length,removed=removedCount,missing,existingIdentitiesPreserved=true,navigation="재생성 대기"};System.IO.File.WriteAllText(folder+"/applied.json",json(report));return json(report);
}catch{Undo.RevertAllDownToGroup(undo);if(createdInteraction)AssetDatabase.DeleteAsset(interactionPath);throw;}

