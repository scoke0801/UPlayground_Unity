// A00 lease로 실행하는 읽기 조사. 씬 전환·임포트·저장·Play·베이크를 하지 않는다.
var host=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");
if(!host.isLoaded||UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("열린 Host Edit Mode 필요");
bool dirty=host.isDirty;
var roots=host.GetRootGameObjects();var terrain=roots.SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single(t=>t.terrainData!=null);var data=terrain.terrainData;
var all=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var reference=new Bounds(new Vector3(1915f,100f,-325f),new Vector3(200f,1200f,140f));
Func<Vector3,float[]> vec=p=>new[]{p.x,p.y,p.z};
Func<Transform,string> path=t=>{string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;};
Func<UnityEngine.Object,string> guid=o=>o==null?null:UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(o));
Func<UnityEngine.Object,string> globalId=o=>UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(o).ToString();
var renderers=roots.SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r.bounds.Intersects(reference)).Select(r=>new{path=path(r.transform),globalId=globalId(r),position=vec(r.transform.position),rotation=vec(r.transform.eulerAngles),scale=vec(r.transform.lossyScale),enabled=r.enabled,active=r.gameObject.activeInHierarchy,boundsMin=vec(r.bounds.min),boundsMax=vec(r.bounds.max),hasPropertyBlock=r.HasPropertyBlock(),materials=r.sharedMaterials.Select((m,i)=>new{slot=i,path=UnityEditor.AssetDatabase.GetAssetPath(m),guid=guid(m),shader=m==null||m.shader==null?null:m.shader.name}).ToArray()}).ToArray();
var colliders=roots.SelectMany(g=>g.GetComponentsInChildren<Collider>(true)).Where(c=>c.bounds.Intersects(reference)).Select(c=>new{path=path(c.transform),globalId=globalId(c),type=c.GetType().Name,enabled=c.enabled,active=c.gameObject.activeInHierarchy,trigger=c.isTrigger,boundsMin=vec(c.bounds.min),boundsMax=vec(c.bounds.max)}).ToArray();
var coastal=all.Single(t=>t.name=="CoastalWater");var waterRenderer=coastal.GetComponent<Renderer>();
if(waterRenderer==null)throw new Exception("CoastalWater Renderer 조사 필요");
var volumes=all.SelectMany(t=>t.GetComponents<Component>()).Where(c=>c!=null&&c.GetType().Name=="NavMeshModifierVolume").Select(c=>{
    var so=new UnityEditor.SerializedObject(c);var center=so.FindProperty("m_Center");var size=so.FindProperty("m_Size");var area=so.FindProperty("m_Area");
    var localCenter=center==null?Vector3.zero:center.vector3Value;var localSize=size==null?Vector3.zero:size.vector3Value;
    var corners=new List<Vector3>();for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)corners.Add(c.transform.TransformPoint(localCenter+Vector3.Scale(localSize*.5f,new Vector3(x,y,z))));
    var b=new Bounds(corners[0],Vector3.zero);foreach(var p in corners)b.Encapsulate(p);
    return new{path=path(c.transform),localCenter=vec(localCenter),localSize=vec(localSize),worldCorners=corners.Select(vec).ToArray(),area=area==null?-1:area.intValue,intersects=b.Intersects(reference)};
}).Where(v=>v.intersects).ToArray();
var samples=new List<object>();float waterY=coastal.position.y;
for(float z=-395;z<=-255;z+=5)for(float x=1815;x<=2015;x+=5){
    float nx=(x-terrain.transform.position.x)/data.size.x,nz=(z-terrain.transform.position.z)/data.size.z;if(nx<0||nx>1||nz<0||nz>1)continue;
    float ground=data.GetInterpolatedHeight(nx,nz)+terrain.transform.position.y;
    var p=new Vector3(x,ground,z);UnityEngine.AI.NavMeshHit hit;bool nav=UnityEngine.AI.NavMesh.SamplePosition(p,out hit,2f,-1);
    var alpha=data.GetAlphamaps(Mathf.Clamp((int)(nx*data.alphamapWidth),0,data.alphamapWidth-1),Mathf.Clamp((int)(nz*data.alphamapHeight),0,data.alphamapHeight-1),1,1);
    samples.Add(new{x=x,z=z,terrainHeight=ground,terrainDepth=waterY-ground,slope=data.GetSteepness(nx,nz),alpha=Enumerable.Range(0,alpha.GetLength(2)).Select(i=>alpha[0,0,i]).ToArray(),navSample=nav,navAreaMask=nav?hit.mask:0,navPosition=nav?vec(hit.position):null});
}
var markers=new[]{new Vector3(1905f,50.25f,-346f),new Vector3(1970.5f,50.08f,-326.5f),new Vector3(1935f,50.05f,-326f),new Vector3(1920f,50.25f,-244f),new Vector3(1920f,50.349534f,-155f),new Vector3(1914f,50.290037f,-170f)};
var anchorSupport=markers.Select(p=>new{position=vec(p),terrainHeight=terrain.SampleHeight(p)+terrain.transform.position.y,hits=Physics.RaycastAll(p+Vector3.up*4f,Vector3.down,10f,-1,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).Select(h=>new{path=path(h.collider.transform),point=vec(h.point),distance=h.distance}).ToArray()}).ToArray();
if(host.isDirty!=dirty)throw new Exception("Host dirty 상태 변동. A00 확인 필요");
var result=new{status="읽기 조사만. 물/지형/경로 수정 없음",hostDirty=host.isDirty,scatter=data.detailScatterMode.ToString(),detailPrototypeCount=data.detailPrototypes.Length,layers=data.terrainLayers.Select((l,i)=>new{index=i,path=UnityEditor.AssetDatabase.GetAssetPath(l),guid=guid(l)}).ToArray(),coastalWater=new{path=path(coastal),globalId=globalId(coastal.gameObject),position=vec(coastal.position),scale=vec(coastal.lossyScale),boundsMin=vec(waterRenderer.bounds.min),boundsMax=vec(waterRenderer.bounds.max),scripts=coastal.GetComponents<MonoBehaviour>().Where(c=>c!=null).Select(c=>c.GetType().FullName).ToArray()},renderers=renderers,colliders=colliders,navModifierVolumes=volumes,terrainSamples=samples,anchorSupport=anchorSupport,limits=new[]{"Region 씬을 로드하지 않았다. Region 액터/상호작용 등록·충돌은 미검증", "Physics broadphase가 이전 변경과 동기화되었는지 A00가 별도 확인해야 한다", "수면 깊이는 Terrain 기준이고 부두/다른 메시 지지면과 구분한다", "NavMesh 표본 성공은 양방향 보행·낙하 복귀 통과가 아니다", "URP/수면 반사·잔물결·거품/TPS·LOD·성능·사용자 인수 미수행"}};
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
string output=(string)json.GetMethod("SerializeObject",new Type[]{typeof(object)}).Invoke(null,new object[]{result});System.IO.Directory.CreateDirectory("output/WorldThemeProduction/SaltwindHarbor");System.IO.File.WriteAllText("output/WorldThemeProduction/SaltwindHarbor/live-probe.json",output);
return "A08 읽기 조사: renderer="+renderers.Length+", collider="+colliders.Length+", terrain="+samples.Count+". Region 로드/저장 없음";
