// A00 lease에서만 실행. 선택 시설의 실제 지지면/재질/경로를 읽고 JSON만 기록한다.
var host=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");
if(!host.isLoaded||UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("열린 Host Edit Mode 필요");
bool dirty=host.isDirty;
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var deserialize=json.GetMethod("DeserializeObject",new Type[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>deserialize.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/WorldThemes/A10/ruins-corridor-study.json"),typeof(Dictionary<string,object>));
var sites=(Dictionary<string,object>[])read(plan["facilities"].ToString(),typeof(Dictionary<string,object>[]));
var site=sites.Single(s=>s["key"].ToString()==plan["probeFacilityKey"].ToString());
var bounds=(float[])read(site["referenceBounds"].ToString(),typeof(float[]));
var reference=new Bounds(new Vector3((bounds[0]+bounds[1])*.5f,200f,(bounds[2]+bounds[3])*.5f),new Vector3(bounds[1]-bounds[0],1600f,bounds[3]-bounds[2]));
Func<Vector3,float[]> vec=p=>new[]{p.x,p.y,p.z};
Func<Transform,string> path=t=>{string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;};
Func<UnityEngine.Object,string> globalId=o=>UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(o).ToString();
var roots=host.GetRootGameObjects();var terrain=roots.SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single(t=>t.terrainData!=null);var data=terrain.terrainData;
var renderers=roots.SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r.bounds.Intersects(reference)).Select(r=>new{path=path(r.transform),globalId=globalId(r),position=vec(r.transform.position),rotation=vec(r.transform.eulerAngles),scale=vec(r.transform.lossyScale),active=r.gameObject.activeInHierarchy,enabled=r.enabled,boundsMin=vec(r.bounds.min),boundsMax=vec(r.bounds.max),hasPropertyBlock=r.HasPropertyBlock(),materials=r.sharedMaterials.Select((m,i)=>new{slot=i,path=UnityEditor.AssetDatabase.GetAssetPath(m),guid=UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(m)),shader=m==null||m.shader==null?null:m.shader.name}).ToArray()}).ToArray();
var colliders=roots.SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Where(c=>c.bounds.Intersects(reference)).Select(c=>new{path=path(c.transform),globalId=globalId(c),type=c.GetType().Name,active=c.gameObject.activeInHierarchy,enabled=c.enabled,trigger=c.isTrigger,boundsMin=vec(c.bounds.min),boundsMax=vec(c.bounds.max)}).ToArray();
var protectedEntities=(Dictionary<string,object>[])read(site["protectedEntities"].ToString(),typeof(Dictionary<string,object>[]));
var supports=new List<object>();
foreach(var entity in protectedEntities){
    var pose=(float[])read(entity["position"].ToString(),typeof(float[]));var p=new Vector3(pose[0],pose[1],pose[2]);
    var hits=Physics.RaycastAll(p+Vector3.up*4f,Vector3.down,14f,-1,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).Select(h=>new{path=path(h.collider.transform),point=vec(h.point),distance=h.distance}).ToArray();
    UnityEngine.AI.NavMeshHit nav;bool sampled=UnityEngine.AI.NavMesh.SamplePosition(p,out nav,2f,-1);
    supports.Add(new{id=entity["id"].ToString(),position=vec(p),terrainHeight=terrain.SampleHeight(p)+terrain.transform.position.y,supports=hits,navSample=sampled,navPosition=sampled?vec(nav.position):null});
}
var lods=roots.SelectMany(g=>g.GetComponentsInChildren<LODGroup>(true)).Where(l=>reference.Contains(l.transform.position)).Select(l=>new{path=path(l.transform),size=l.size,fade=l.fadeMode.ToString(),levels=l.GetLODs().Select(x=>new{threshold=x.screenRelativeTransitionHeight,renderers=x.renderers.Where(r=>r!=null).Select(globalId).ToArray()}).ToArray()}).ToArray();
if(host.isDirty!=dirty)throw new Exception("조사 중 Host dirty 상태 변동");
var result=new{status="읽기 조사. 시설 core/개선/통합 승인 아님",facilityKey=site["key"].ToString(),referenceBounds=bounds,hostDirty=host.isDirty,terrain=UnityEditor.AssetDatabase.GetAssetPath(data),scatter=data.detailScatterMode.ToString(),detailPrototypeCount=data.detailPrototypes.Length,renderers=renderers,colliders=colliders,protectedSupport=supports,lods=lods,limits=new[]{"native prefab와 실제 renderer/collider 매핑은 A00 추가 확인 필요", "Terrain 높이는 실내 바닥 접지 판정이 아니다. 실제 지지 collider 표본과 구분한다", "Physics broadphase 동기화 상태는 A00가 확인", "양방향 NavMesh 경로·실제 보행·카메라/공격 예고 가독성·미술·성능 미수행"}};
string output=(string)json.GetMethod("SerializeObject",new Type[]{typeof(object)}).Invoke(null,new object[]{result});System.IO.Directory.CreateDirectory("output/WorldThemeProduction/RuinsCorridor");System.IO.File.WriteAllText("output/WorldThemeProduction/RuinsCorridor/live-probe-"+site["key"]+".json",output);
return "A10 "+site["key"]+" 읽기 조사: renderer="+renderers.Length+", collider="+colliders.Length+", 보호점="+supports.Count+". 씬/에셋 저장 없음";
