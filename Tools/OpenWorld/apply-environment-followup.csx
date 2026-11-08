// 최신 씬에 한 번만 적용하는 증분 저작이다. 원본 프리팹과 기존 NavMesh는 수정하지 않는다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string folder="output/OpenWorld/EnvironmentFollowup";
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||scene.isDirty||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
if(System.IO.File.Exists(folder+"/applied.txt")||GameObject.Find("AuthoredEnvironment/환경 후속 개선")!=null)throw new Exception("이미 적용됨: 재실행 금지");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/environment-followup-plan.json"),typeof(Dictionary<string,object>));
Func<string,Dictionary<string,object>[]> items=key=>(Dictionary<string,object>[])read(plan[key].ToString(),typeof(Dictionary<string,object>[]));
Func<object,float[]> numbers=o=>(float[])read(o.ToString(),typeof(float[]));
var env=GameObject.Find("AuthoredEnvironment");var terrain=env.GetComponentInChildren<Terrain>();var data=terrain.terrainData;var origin=terrain.transform.position;
var identities=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).ToArray();
var baseline=string.Join("\n",identities.Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s).ToArray());
if(baseline!=System.IO.File.ReadAllText(folder+"/identities-before.txt").Replace("\r","").TrimEnd('\n'))throw new Exception("진행 기준 불일치");
var heights=new List<float[,]>();var heightStarts=new List<Vector2Int>();var paints=new List<float[,,]>();var paintStarts=new List<Vector2Int>();
var changed=new List<string>();int heightCount=0,paintCount=0;
Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("월드 환경 후속 개선");
Func<Transform,Bounds> bounds=t=>{var rs=t.GetComponentsInChildren<MeshRenderer>(true);if(rs.Length==0)throw new Exception("메시 없음 "+t.name);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;};
try{
foreach(var f in items("heightFeatures")){
var c=numbers(f["center"]);var r=numbers(f["radius"]);
int x0=Mathf.FloorToInt((c[0]-r[0]-origin.x)/data.size.x*(data.heightmapResolution-1)),z0=Mathf.FloorToInt((c[1]-r[1]-origin.z)/data.size.z*(data.heightmapResolution-1));
int w=Mathf.CeilToInt(r[0]*2/data.size.x*(data.heightmapResolution-1))+2,h=Mathf.CeilToInt(r[1]*2/data.size.z*(data.heightmapResolution-1))+2;
var values=data.GetHeights(x0,z0,w,h);heights.Add((float[,])values.Clone());heightStarts.Add(new Vector2Int(x0,z0));
for(int z=0;z<h;z++)for(int x=0;x<w;x++){
float wx=origin.x+(x+x0)*data.size.x/(data.heightmapResolution-1),wz=origin.z+(z+z0)*data.size.z/(data.heightmapResolution-1);
float dx=(wx-c[0])/r[0],dz=(wz-c[1])/r[1],distance=dx*dx+dz*dz;if(distance>=1)continue;
float slope=data.GetSteepness((wx-origin.x)/data.size.x,(wz-origin.z)/data.size.z);if(slope<32)continue;
// 보행면과 진행 개체 가까이는 높이를 바꾸지 않는다.
var p=new Vector3(wx,values[z,x]*data.size.y+origin.y,wz);UnityEngine.AI.NavMeshHit hit;
if(UnityEngine.AI.NavMesh.SamplePosition(p,out hit,9,-1)||identities.Any(i=>(new Vector2(i.transform.position.x-wx,i.transform.position.z-wz)).sqrMagnitude<225))continue;
float weight=Mathf.Pow(1-distance,2)*Mathf.SmoothStep(0,1,Mathf.InverseLerp(32,45,slope));
values[z,x]+=Convert.ToSingle(f["delta"])*weight/data.size.y;heightCount++;
}data.SetHeights(x0,z0,values);
}
var root=new GameObject("환경 후속 개선");Undo.RegisterCreatedObjectUndo(root,"암반 군집");root.transform.SetParent(env.transform,false);
foreach(var f in items("rocks")){
string asset=(string)f["prefab"];var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(asset);if(prefab==null)throw new Exception("프리팹 없음 "+asset);
if(prefab.GetComponentsInChildren<MonoBehaviour>(true).Length!=0)throw new Exception("환경 외 스크립트 프리팹 "+asset);
var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform);Undo.RegisterCreatedObjectUndo(g,"암반 배치");g.name=(string)f["name"];
var p=numbers(f["position"]);var scale=numbers(f["scale"]);var rotation=numbers(f["rotation"]);
g.transform.localScale=new Vector3(scale[0],scale[1],scale[2]);g.transform.rotation=Quaternion.Euler(rotation[0],rotation[1],rotation[2]);
var point=new Vector3(p[0],0,p[1]);point.y=terrain.SampleHeight(point)+origin.y;g.transform.position=point;
var b=bounds(g.transform);g.transform.position+=Vector3.up*(point.y-b.min.y-Convert.ToSingle(f["embed"]));b=bounds(g.transform);
if(identities.Any(i=>b.SqrDistance(i.transform.position)<25))throw new Exception("진행 개체 근접 암반 "+g.name);
foreach(var renderer in g.GetComponentsInChildren<MeshRenderer>(true))if(renderer.sharedMaterials.Any(m=>m==null))throw new Exception("재질 누락 "+g.name);
PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(g);
}
foreach(var f in items("moves")){
string path=(string)f["path"];int slash=path.LastIndexOf('/');var parent=GameObject.Find(path.Substring(0,slash));var from=numbers(f["from"]);
var candidates=parent.transform.Cast<Transform>().Where(candidate=>candidate.name==path.Substring(slash+1)&&Vector2.Distance(new Vector2(candidate.position.x,candidate.position.z),new Vector2(from[0],from[1]))<.1f).ToArray();
if(candidates.Length!=1)throw new Exception("배치 식별 모호 "+path);var t=candidates[0];
if(t.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Length!=0)throw new Exception("진행 개체 이동 금지 "+path);
Undo.RecordObject(t,"기존 소품 접합");changed.Add(GlobalObjectId.GetGlobalObjectIdSlow(PrefabUtility.GetPrefabInstanceHandle(t)).ToString());
var p=numbers(f["position"]);var point=new Vector3(p[0],0,p[1]);point.y=terrain.SampleHeight(point)+origin.y;
t.SetPositionAndRotation(point,Quaternion.Euler(0,Convert.ToSingle(f["yaw"]),0));var b=bounds(t);t.position+=Vector3.up*(point.y-b.min.y-Convert.ToSingle(f["embed"]));
if(identities.Any(i=>bounds(t).SqrDistance(i.transform.position)<2.25f))throw new Exception("진행 개체 근접 소품 "+path);
PrefabUtility.RecordPrefabInstancePropertyModifications(t);
}
foreach(var f in items("paint")){
var c=numbers(f["center"]);var r=numbers(f["radius"]);var weights=numbers(f["weights"]);if(weights.Length!=data.alphamapLayers||Mathf.Abs(weights.Sum()-1)>.001f)throw new Exception("표면 가중치 오류");
int x0=Mathf.Clamp(Mathf.FloorToInt((c[0]-r[0]*1.2f-origin.x)/data.size.x*data.alphamapWidth),0,data.alphamapWidth-1),z0=Mathf.Clamp(Mathf.FloorToInt((c[1]-r[1]*1.2f-origin.z)/data.size.z*data.alphamapHeight),0,data.alphamapHeight-1);
int x1=Mathf.Clamp(Mathf.CeilToInt((c[0]+r[0]*1.2f-origin.x)/data.size.x*data.alphamapWidth),0,data.alphamapWidth-1),z1=Mathf.Clamp(Mathf.CeilToInt((c[1]+r[1]*1.2f-origin.z)/data.size.z*data.alphamapHeight),0,data.alphamapHeight-1);
var a=data.GetAlphamaps(x0,z0,x1-x0+1,z1-z0+1);paints.Add((float[,,])a.Clone());paintStarts.Add(new Vector2Int(x0,z0));
for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++){
float wx=origin.x+(x+x0+.5f)/data.alphamapWidth*data.size.x,wz=origin.z+(z+z0+.5f)/data.alphamapHeight*data.size.z;
float dx=(wx-c[0])/r[0],dz=(wz-c[1])/r[1],noise=Mathf.PerlinNoise(wx*.22f,wz*.22f),distance=Mathf.Sqrt(dx*dx+dz*dz)+(noise-.5f)*.25f;
float blend=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,1.1f,distance)))*Convert.ToSingle(f["strength"]);if(blend<=0)continue;
for(int l=0;l<weights.Length;l++)a[z,x,l]=Mathf.Lerp(a[z,x,l],weights[l],blend);paintCount++;
}data.SetAlphamaps(x0,z0,a);
}
var after=string.Join("\n",identities.Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s).ToArray());if(after!=baseline)throw new Exception("진행 개체 변경");
terrain.Flush();Physics.SyncTransforms();EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
Undo.CollapseUndoOperations(undo);System.IO.File.WriteAllLines(folder+"/moved-transforms.txt",changed);
string result="heightSamples="+heightCount+" rocks="+items("rocks").Length+" movedProps="+changed.Count+" paintSamples="+paintCount;System.IO.File.WriteAllText(folder+"/applied.txt",result);return result;
}catch{
Undo.RevertAllDownToGroup(undo);
for(int i=heights.Count-1;i>=0;i--)data.SetHeights(heightStarts[i].x,heightStarts[i].y,heights[i]);
for(int i=paints.Count-1;i>=0;i--)data.SetAlphamaps(paintStarts[i].x,paintStarts[i].y,paints[i]);
EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);throw;
}
