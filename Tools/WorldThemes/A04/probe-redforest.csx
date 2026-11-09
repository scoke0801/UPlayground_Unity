// A00 lease에서만 실행하는 현황 조사다. 씬/에셋/지형/내비게이션은 저장하거나 변경하지 않는다.
var host=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");
if(!host.isLoaded||UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("열린 Host Edit Mode 필요");
var roots=host.GetRootGameObjects();var terrain=roots.SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();var data=terrain.terrainData;
var all=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
Func<Transform,string> path=t=>{string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;};
var reference=new Bounds(new Vector3(1145f,200f,552.5f),new Vector3(140f,1200f,135f));
var environment=roots.Single(g=>g.name=="AuthoredEnvironment");
var red=environment.transform.Find("월드 구간/붉은나무숲");
if(red==null)throw new Exception("정확한 붉은숲 환경 경로 없음. 이름 폴백/수정 없이 조사 중단");
Physics.SyncTransforms();
var nativeBounds=red.GetComponentsInChildren<Component>(true).Where(c=>c is Renderer||c is Collider).Select(c=>{var b=c is Renderer?((Renderer)c).bounds:((Collider)c).bounds;return new{path=path(c.transform),type=c.GetType().FullName,globalObjectId=UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(c).ToString(),min=new[]{b.min.x,b.min.y,b.min.z},max=new[]{b.max.x,b.max.y,b.max.z},active=c.gameObject.activeInHierarchy};}).ToArray();
Func<Vector3,float,Collider[]> hostOverlap=(p,r)=>Physics.OverlapSphere(p,r,-1,QueryTriggerInteraction.Ignore).Where(c=>c.gameObject.scene==host&&!(c is TerrainCollider)).ToArray();
var objects=red.GetComponentsInChildren<Transform>(true).Select(t=>UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject)).Where(g=>g!=null).Distinct().Where(g=>reference.Contains(g.transform.position)).Select(g=>new{path=path(g.transform),position=new float[]{g.transform.position.x,g.transform.position.y,g.transform.position.z},prefab=UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g),rendererCount=g.GetComponentsInChildren<Renderer>(true).Length,colliderCount=g.GetComponentsInChildren<Collider>(true).Length}).ToArray();
var landmarks=all.Where(t=>t.name.Contains("동굴")||t.name.StartsWith("R08_")&&t.name.Contains("root")||t.name.Contains("상징목")).Select(t=>new{path=path(t),position=new float[]{t.position.x,t.position.y,t.position.z},childRenderers=t.GetComponentsInChildren<Renderer>(true).Length}).ToArray();
var anchors=all.Where(t=>reference.Contains(t.position)).SelectMany(t=>t.GetComponents<MonoBehaviour>().Where(c=>c!=null&&c.GetType().Namespace!=null&&(c.GetType().Namespace.Contains("Interaction")||c.GetType().Namespace.Contains("FlowGraph")||c.GetType().Name=="SceneEntityId")).Select(c=>new{path=path(t),position=new float[]{t.position.x,t.position.y,t.position.z},type=c.GetType().FullName})).ToArray();
var samples=new List<object>();
for(float z=520f;z<=585f;z+=5f)for(float x=1110f;x<=1180f;x+=5f){
    float nx=(x-terrain.transform.position.x)/data.size.x,nz=(z-terrain.transform.position.z)/data.size.z;var p=new Vector3(x,data.GetInterpolatedHeight(nx,nz)+terrain.transform.position.y,z);
    UnityEngine.AI.NavMeshHit hit;bool nav=UnityEngine.AI.NavMesh.SamplePosition(p,out hit,2f,-1);
    var alpha=data.GetAlphamaps(Mathf.Clamp((int)(nx*data.alphamapWidth),0,data.alphamapWidth-1),Mathf.Clamp((int)(nz*data.alphamapHeight),0,data.alphamapHeight-1),1,1);
    samples.Add(new{x=x,z=z,height=p.y,slope=data.GetSteepness(nx,nz),roadWeight=alpha[0,0,5],darkEarthWeight=alpha[0,0,1],grassWeight=alpha[0,0,4],navSample=nav,navPosition=nav?new float[]{hit.position.x,hit.position.y,hit.position.z}:null,hostObstaclePaths=hostOverlap(p+Vector3.up,0.4f).Select(c=>path(c.transform)).ToArray()});
}
var result=new{status="읽기 현황. core 후보이며 편집 허가 아님",hostDirty=host.isDirty,terrain=UnityEditor.AssetDatabase.GetAssetPath(data),exactRedRoot=path(red),redRootGlobalId=UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(red).ToString(),nativeBounds=nativeBounds,objects=objects,landmarks=landmarks,protectedAnchors=anchors,samples=samples,physicsSceneFilter="hit.gameObject.scene == host; 다른 ArtReview 중복 Terrain/물체 제외",navWarning="NavMesh.SamplePosition은 loaded nav의 전역 질의이므로 위치·소속 데이터 별도 확인 전 Host 경로 통과로 판정하지 않음"};
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);string output=(string)json.GetMethod("SerializeObject",new Type[]{typeof(object)}).Invoke(null,new object[]{result});System.IO.Directory.CreateDirectory("output/WorldThemeProduction/RedTreeForest");System.IO.File.WriteAllText("output/WorldThemeProduction/RedTreeForest/live-probe.json",output);
return "A04 조사: 기존환경 "+objects.Length+"/앵커 "+anchors.Length+"/지형·Nav 표본 "+samples.Count+". Host dirty="+host.isDirty;
