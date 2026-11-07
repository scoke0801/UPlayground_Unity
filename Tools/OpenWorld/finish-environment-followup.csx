// 첫 전후 캡처에서 드러난 사면 단차와 따로 붙은 암반을 한 번 보정한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string folder="output/OpenWorld/EnvironmentFollowup";
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity"||!System.IO.File.Exists(folder+"/applied.txt")||System.IO.File.Exists(folder+"/finished.txt"))throw new Exception("후속 보정 상태 불일치");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/environment-followup-plan.json"),typeof(Dictionary<string,object>));Func<object,float[]> numbers=o=>(float[])read(o.ToString(),typeof(float[]));
var terrain=GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();var data=terrain.terrainData;var origin=terrain.transform.position;
var loaded=UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(folder+"/backup/Assets/10.Datas/World/OpenWorld/Terrain.asset");var original=loaded.OfType<TerrainData>().Single();
var heights=original.GetHeights(0,0,original.heightmapResolution,original.heightmapResolution);var ids=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).ToArray();
int samples=0;
foreach(var f in (Dictionary<string,object>[])read(plan["heightFeatures"].ToString(),typeof(Dictionary<string,object>[]))){
var c=numbers(f["center"]);var r=numbers(f["radius"]);
int x0=Mathf.FloorToInt((c[0]-r[0]-origin.x)/data.size.x*(data.heightmapResolution-1)),z0=Mathf.FloorToInt((c[1]-r[1]-origin.z)/data.size.z*(data.heightmapResolution-1));
int w=Mathf.CeilToInt(r[0]*2/data.size.x*(data.heightmapResolution-1))+2,h=Mathf.CeilToInt(r[1]*2/data.size.z*(data.heightmapResolution-1))+2;
for(int z=z0;z<z0+h;z++)for(int x=x0;x<x0+w;x++){
float wx=origin.x+x*data.size.x/(data.heightmapResolution-1),wz=origin.z+z*data.size.z/(data.heightmapResolution-1),dx=(wx-c[0])/r[0],dz=(wz-c[1])/r[1],d=dx*dx+dz*dz;if(d>=1)continue;
float nx=(wx-origin.x)/data.size.x,nz=(wz-origin.z)/data.size.z,slope=original.GetSteepness(nx,nz);if(slope<=32)continue;
var p=new Vector3(wx,original.GetInterpolatedHeight(nx,nz)+origin.y,wz);UnityEngine.AI.NavMeshHit hit;float navWeight=1;
if(UnityEngine.AI.NavMesh.SamplePosition(p,out hit,22,-1))navWeight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(9,22,Vector3.Distance(p,hit.position)));
float nearest=1000;foreach(var id in ids){float distance=Vector2.Distance(new Vector2(wx,wz),new Vector2(id.transform.position.x,id.transform.position.z));nearest=Mathf.Min(nearest,distance);}
float weight=Mathf.Pow(1-d,2)*Mathf.SmoothStep(0,1,Mathf.InverseLerp(32,48,slope))*navWeight*Mathf.SmoothStep(0,1,Mathf.InverseLerp(15,28,nearest));
if(weight<=0)continue;heights[z,x]+=Convert.ToSingle(f["delta"])*weight/data.size.y;samples++;
}}
data.SetHeights(0,0,heights);terrain.Flush();
int rocks=0;foreach(var f in (Dictionary<string,object>[])read(plan["rocks"].ToString(),typeof(Dictionary<string,object>[]))){
var pos=numbers(f["position"]);if(pos[1]<0)continue;var t=GameObject.Find("AuthoredEnvironment/환경 후속 개선/"+(string)f["name"]).transform;
float nx=(pos[0]-origin.x)/data.size.x,nz=(pos[1]-origin.z)/data.size.z;var normal=data.GetInterpolatedNormal(nx,nz);var rot=numbers(f["rotation"]);
t.rotation=Quaternion.FromToRotation(Vector3.up,normal)*Quaternion.Euler(rot[0]*.3f,rot[1],0);
var mesh=t.GetComponentInChildren<MeshFilter>();var local=mesh.sharedMesh.bounds;var p=new Vector3(pos[0],data.GetInterpolatedHeight(nx,nz)+origin.y,pos[1]);
t.position=p;t.position+=p-mesh.transform.TransformPoint(local.center)-normal*.7f;
PrefabUtility.RecordPrefabInstancePropertyModifications(t);rocks++;
}
Physics.SyncTransforms();EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("보정 씬 저장 실패");
string result="smoothHeightSamples="+samples+" slopeEmbeddedRocks="+rocks;System.IO.File.WriteAllText(folder+"/finished.txt",result);
foreach(var obj in loaded)UnityEngine.Object.DestroyImmediate(obj);return result;
