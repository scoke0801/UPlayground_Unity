var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>();
var names=new[]{"성문과 시장","마을 입구","항구와 잔교","등불전","제단 상부","옛터 보상","동쪽 작업 부두","낚시 잔교"};
var paths=new[]{new[]{new Vector3(580,62,230),new Vector3(580,62,80)},new[]{new Vector3(976,63,425),new Vector3(970,64,460)},new[]{new Vector3(1920,50,-226),new Vector3(1920,50,-350)},new[]{new Vector3(330,104,510),new Vector3(330,104,585)},new[]{new Vector3(590,140,824),new Vector3(591.2f,144.5f,859.9f),new Vector3(580,140,860)},new[]{new Vector3(1850,110,912),new Vector3(1930,110,910)},new[]{new Vector3(1970,50,-330),new Vector3(1981,50,-352)},new[]{new Vector3(1905,50,-330),new Vector3(1905,50.25f,-346)}};
var lines=new List<string>();int failures=0;
for(int i=0;i<paths.Length;i++){
 var sampled=new List<Vector3>();
 foreach(var p in paths[i]){UnityEngine.AI.NavMeshHit hit;bool ok=UnityEngine.AI.NavMesh.SamplePosition(p,out hit,3,-1);if(!ok)failures++;sampled.Add(hit.position);lines.Add(names[i]+" | sample "+p+" -> "+ok+" "+hit.position);}
 for(int k=1;k<sampled.Count;k++)foreach(int dir in new[]{0,1}){var path=new UnityEngine.AI.NavMeshPath();bool ok=UnityEngine.AI.NavMesh.CalculatePath(sampled[dir==0?k-1:k],sampled[dir==0?k:k-1],-1,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;if(!ok)failures++;lines.Add(names[i]+" | pair "+(k-1)+"-"+k+" dir="+dir+" "+ok+" "+path.status);}
}
var world=GameObject.Find("AuthoredEnvironment").transform.Find("월드 구간");
int missing=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
int meshes=world.GetComponentsInChildren<MeshFilter>(true).Count(f=>f.sharedMesh==null);
int materials=world.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.sharedMaterials.Any(m=>m==null));
string identities=string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s));
bool identityMatch=System.IO.File.ReadAllText("output/OpenWorld/FullReplacement/identities-before.txt")==identities;
lines.Add("MissingScript="+missing+" MissingMesh="+meshes+" MissingMaterial="+materials+" IdentityMatch="+identityMatch);
lines.Add("PathFailures="+failures+"; Editor API 검사, 실제 입력 보행 미검증");
int actors=0,blocked=0,unsupported=0;
foreach(var name in new[]{"StonegateCitadel","SaltwindHarbor"}){
 var content=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/01.Scenes/Scenario/OpenWorld/Regions/"+name+"Region.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
 try{foreach(var actor in content.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.GameActor>(true))){
  actors++;var p=actor.transform.position;
  var hits=Physics.OverlapCapsule(p+Vector3.up*.65f,p+Vector3.up*1.5f,.25f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Where(c=>c.gameObject.scene==scene&&c.GetComponent<Terrain>()==null).ToArray();
  bool support=Physics.RaycastAll(p+Vector3.up*.2f,Vector3.down,2,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(h=>h.collider.gameObject.scene==scene&&h.normal.y>.5f);
  if(hits.Length>0)blocked++;if(!support)unsupported++;
  if(hits.Length>0||!support)lines.Add(actor.name+" blocked="+string.Join(",",hits.Select(c=>c.name).ToArray())+" support="+support);
 }}finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(content,true);}
}
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
lines.Add("CityActors="+actors+" Blocked="+blocked+" Unsupported="+unsupported);
System.IO.File.WriteAllLines("output/OpenWorld/FullReplacement/audit.txt",lines);
return string.Join("\n",lines.ToArray());
