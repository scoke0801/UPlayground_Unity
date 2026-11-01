var host=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var root=GameObject.Find("AuthoredEnvironment").transform.Find("월드 구간/항구");
var lines=new List<string>();int failures=0;
var names=new[]{"진입로와 시장","시장과 안벽","낚시 잔교","등대 방파제"};
var points=new[]{new[]{new Vector3(1920,50,-230),new Vector3(1920,50,-288)},new[]{new Vector3(1920,50,-288),new Vector3(1920,50,-326)},new[]{new Vector3(1920,50,-326),new Vector3(1905,50,-346)},new[]{new Vector3(1920,50,-326),new Vector3(1981,50,-352)}};
for(int i=0;i<points.Length;i++)foreach(int direction in new[]{0,1}){
 UnityEngine.AI.NavMeshHit a,b;bool sampleA=UnityEngine.AI.NavMesh.SamplePosition(points[i][direction],out a,1,-1),sampleB=UnityEngine.AI.NavMesh.SamplePosition(points[i][1-direction],out b,1,-1);
 var path=new UnityEngine.AI.NavMeshPath();bool ok=sampleA&&sampleB&&UnityEngine.AI.NavMesh.CalculatePath(a.position,b.position,-1,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;
 if(!ok)failures++;lines.Add(names[i]+" dir="+direction+" ok="+ok+" "+path.status);
}
int actors=0,blocked=0,unsupported=0;
var content=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/01.Scenes/Scenario/OpenWorld/Regions/SaltwindHarborRegion.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
try{Physics.SyncTransforms();foreach(var actor in content.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.GameActor>(true))){
 actors++;var p=actor.transform.position;
 var hits=Physics.OverlapCapsule(p+Vector3.up*.65f,p+Vector3.up*1.5f,.25f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Where(c=>c.gameObject.scene==host&&c.GetComponent<Terrain>()==null).ToArray();
 bool support=Physics.RaycastAll(p+Vector3.up*.4f,Vector3.down,2,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(h=>h.collider.gameObject.scene==host&&h.normal.y>.5f);
 if(hits.Length>0)blocked++;if(!support)unsupported++;
 lines.Add(actor.name+" blocked="+string.Join(",",hits.Select(c=>c.name).ToArray())+" support="+support);
}}finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(content,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(host);}
string ids=string.Join("\n",host.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s));
bool identity=ids==System.IO.File.ReadAllText("output/OpenWorld/HarborArtRevision/identities-before.txt");
int missing=root.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
int materials=root.GetComponentsInChildren<Renderer>(true).Count(r=>r.sharedMaterials.Any(m=>m==null));
int legacy=root.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="HarborHouse"||t.name=="HarborPier"||t.name=="HarborLighthouse"||t.name=="HarborWarehouse"||t.name=="CoastalBoat"||t.name=="Dng_Floor_4x4");
int invalid=0;foreach(string guid in AssetDatabase.FindAssets("t:Mesh",new[]{"Assets/03.Prefabs/Environment/HarborRevision"})){var m=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));foreach(var v in m.vertices)if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z))invalid++;}
lines.Add("Actors="+actors+" Blocked="+blocked+" Unsupported="+unsupported+" IdentityMatch="+identity+" MissingScript="+missing+" MissingMaterial="+materials+" Legacy="+legacy+" InvalidVertices="+invalid);
lines.Add("PathFailures="+failures+" / Editor API 검사, 실제 보행·상호작용과 미술 인수는 사용자 확인 대기");
System.IO.File.WriteAllLines("output/OpenWorld/HarborArtRevision/audit.txt",lines);return string.Join("\n",lines.ToArray());
