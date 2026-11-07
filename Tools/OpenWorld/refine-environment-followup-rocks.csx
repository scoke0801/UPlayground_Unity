// 분리되어 보이는 조각을 겹친 암반 덩어리로 묶는 최종 일회성 보정이다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string folder="output/OpenWorld/EnvironmentFollowup";
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity"||!System.IO.File.Exists(folder+"/finished.txt")||System.IO.File.Exists(folder+"/rock-refined.txt"))throw new Exception("암반 최종 보정 상태 불일치");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/environment-followup-plan.json"),typeof(Dictionary<string,object>));Func<object,float[]> numbers=o=>(float[])read(o.ToString(),typeof(float[]));
var terrain=GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();var data=terrain.terrainData;var origin=terrain.transform.position;
foreach(var f in (Dictionary<string,object>[])read(plan["rocks"].ToString(),typeof(Dictionary<string,object>[]))){
var pos=numbers(f["position"]);if(pos[1]<0)continue;var t=GameObject.Find("AuthoredEnvironment/환경 후속 개선/"+(string)f["name"]).transform;var scale=numbers(f["scale"]);t.localScale=new Vector3(scale[0],scale[1],scale[2]);
float nx=(pos[0]-origin.x)/data.size.x,nz=(pos[1]-origin.z)/data.size.z;var normal=data.GetInterpolatedNormal(nx,nz);var rot=numbers(f["rotation"]);t.rotation=Quaternion.FromToRotation(Vector3.up,normal)*Quaternion.Euler(rot[0]*.3f,rot[1],0);
var mesh=t.GetComponentInChildren<MeshFilter>();var p=new Vector3(pos[0],data.GetInterpolatedHeight(nx,nz)+origin.y,pos[1]);t.position=p;t.position+=p-mesh.transform.TransformPoint(mesh.sharedMesh.bounds.center)-normal*.7f;PrefabUtility.RecordPrefabInstancePropertyModifications(t);
}
Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("암반 씬 저장 실패");System.IO.File.WriteAllText(folder+"/rock-refined.txt","PASS 겹친 암반과 능선 배치 저장");return "PASS";
