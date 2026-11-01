var host=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var env=GameObject.Find("AuthoredEnvironment").transform;var castle=env.Find("월드 구간/돌등성");
var lines=new List<string>();int failures=0;
var names=new[]{"성문 통과","중앙 광장 연결","도시쪽 본성 계단","서쪽 골목","동쪽 골목","성밖 진입로"};
var pairs=new[]{new[]{new Vector3(580,62.3f,224),new Vector3(580,62.3f,192)},new[]{new Vector3(580,62.3f,148),new Vector3(580,62.3f,105)},new[]{new Vector3(580,62.3f,73),new Vector3(580,74,36.5f)},new[]{new Vector3(556,62.3f,179),new Vector3(549,62.3f,72)},new[]{new Vector3(610,62.3f,190),new Vector3(608,62.3f,88)},new[]{new Vector3(580,62.3f,224),new Vector3(655,62.3f,229)}};
for(int i=0;i<pairs.Length;i++)foreach(int direction in new[]{0,1}){
 UnityEngine.AI.NavMeshHit a,b;bool sampleA=UnityEngine.AI.NavMesh.SamplePosition(pairs[i][direction],out a,2,-1),sampleB=UnityEngine.AI.NavMesh.SamplePosition(pairs[i][1-direction],out b,2,-1);
 var path=new UnityEngine.AI.NavMeshPath();bool ok=sampleA&&sampleB&&UnityEngine.AI.NavMesh.CalculatePath(a.position,b.position,-1,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;
 float length=0;for(int k=1;k<path.corners.Length;k++)length+=Vector3.Distance(path.corners[k-1],path.corners[k]);
 if(i==2&&length>50)ok=false;
 if(!ok)failures++;lines.Add(names[i]+" dir="+direction+" ok="+ok+" "+path.status+" length="+length.ToString("F2")+" start="+a.position+" end="+b.position);
}
// 지형으로 우회 가능하다는 이유로 도로의 시각적 단절을 통과시키지 않는다.
var roadIntervals=new List<Vector2>();
foreach(var path in new[]{"도로/성문에서 시장","도로/시장과 본성","시장/작은 중앙 광장"})foreach(var t in castle.Find(path).GetComponentsInChildren<Transform>(true)){
 var r=t.GetComponent<MeshRenderer>();if(r==null)continue;var b=r.bounds;if(b.min.x<=580&&b.max.x>=580)roadIntervals.Add(new Vector2(b.min.z,b.max.z));
}
var sorted=roadIntervals.OrderBy(v=>v.x).ToArray();float endZ=sorted[0].y,maxGap=0;
for(int i=1;i<sorted.Length;i++){maxGap=Mathf.Max(maxGap,sorted[i].x-endZ);endZ=Mathf.Max(endZ,sorted[i].y);}
if(maxGap>.05f)failures++;lines.Add("중앙 포장 최대 간격="+maxGap.ToString("F4")+"m");
int actors=0,blocked=0,unsupported=0;
var content=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/01.Scenes/Scenario/OpenWorld/Regions/StonegateCitadelRegion.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
try{Physics.SyncTransforms();foreach(var actor in content.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.GameActor>(true))){
 actors++;var p=actor.transform.position;
 var hits=Physics.OverlapCapsule(p+Vector3.up*.65f,p+Vector3.up*1.5f,.25f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Where(c=>c.gameObject.scene==host&&c.GetComponent<Terrain>()==null).ToArray();
 bool support=Physics.RaycastAll(p+Vector3.up*.4f,Vector3.down,2,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(h=>h.collider.gameObject.scene==host&&h.normal.y>.5f);
 if(hits.Length>0)blocked++;if(!support)unsupported++;
 if(hits.Length>0||!support)lines.Add(actor.name+" blocked="+string.Join(",",hits.Select(c=>c.name).ToArray())+" support="+support);
}}finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(content,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(host);}
string identities=string.Join("\n",host.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s));
bool ids=identities==System.IO.File.ReadAllText("output/OpenWorld/CitadelArtRevision/identities-before.txt");
int missing=host.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
lines.Add("Actors="+actors+" Blocked="+blocked+" Unsupported="+unsupported+" IdentityMatch="+ids+" MissingScript="+missing);
lines.Add("PathFailures="+failures+"; Editor API 검사. 실제 입력 보행 및 미술 인수 별도.");
System.IO.File.WriteAllLines("output/OpenWorld/CitadelArtRevision/audit.txt",lines);return string.Join("\n",lines.ToArray());
