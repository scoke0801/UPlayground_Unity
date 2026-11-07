// 현재 저장본에만 적용하는 일회성 증분 저작. 원본 프리팹과 기존 NavMesh는 보존한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string folder="output/OpenWorld/EnvironmentForm";
if(scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed)throw new Exception("저장된 월드 Edit Mode와 정상 컴파일 필요");
if(System.IO.File.Exists(folder+"/applied.txt")||GameObject.Find("AuthoredEnvironment/사면과 거점 후속 저작")!=null)throw new Exception("재실행 금지");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/environment-form-plan.json"),typeof(Dictionary<string,object>));
Func<string,Dictionary<string,object>[]> items=k=>(Dictionary<string,object>[])read(plan[k].ToString(),typeof(Dictionary<string,object>[]));
Func<object,float[]> numbers=o=>(float[])read(o.ToString(),typeof(float[]));
var env=GameObject.Find("AuthoredEnvironment");var terrain=env.GetComponentInChildren<Terrain>();var data=terrain.terrainData;var origin=terrain.transform.position;
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var ids=all.SelectMany(t=>t.GetComponents<UPlayGround.Components.SceneEntityId>()).ToArray();
string baseline=string.Join("\n",ids.Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s).ToArray());
if(baseline!=System.IO.File.ReadAllText(folder+"/identities-before.txt").Replace("\r","").TrimEnd('\n'))throw new Exception("진행 기준 변경됨");
Func<Transform,Bounds> bounds=t=>{var rs=t.GetComponentsInChildren<MeshRenderer>(true);if(rs.Length==0)throw new Exception("메시 없음 "+t.name);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;};
var oldRocks=GameObject.Find("AuthoredEnvironment/환경 후속 개선").transform.Cast<Transform>().Where(t=>t.position.z>1000).ToArray();
var oldGround=oldRocks.Select(t=>terrain.SampleHeight(t.position)+origin.y).ToArray();
var protectedObjects=all.Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&t.position.x>300&&t.position.x<540&&t.position.z>1070&&t.position.z<1490&&!oldRocks.Contains(t)&&t.GetComponentsInChildren<MeshRenderer>(true).Length>0).Select(bounds).ToArray();
var heights=new List<float[,]>();var paints=new List<float[,,]>();var starts=new List<Vector2Int>();var paintStarts=new List<Vector2Int>();var moved=new List<string>();int count=0,paintCount=0;
Action<Transform> record=t=>{moved.Add(GlobalObjectId.GetGlobalObjectIdSlow(PrefabUtility.GetPrefabInstanceHandle(t)).ToString());PrefabUtility.RecordPrefabInstancePropertyModifications(t);};
Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("사면과 거점 후속 저작");
try{
foreach(var region in items("regions")){
var c=numbers(region["center"]);var radius=numbers(region["radius"]);
int x0=Mathf.FloorToInt((c[0]-radius[0]-origin.x)/data.size.x*(data.heightmapResolution-1)),z0=Mathf.FloorToInt((c[1]-radius[1]-origin.z)/data.size.z*(data.heightmapResolution-1));
int w=Mathf.CeilToInt(radius[0]*2/data.size.x*(data.heightmapResolution-1))+2,h=Mathf.CeilToInt(radius[1]*2/data.size.z*(data.heightmapResolution-1))+2;
var values=data.GetHeights(x0,z0,w,h);heights.Add((float[,])values.Clone());starts.Add(new Vector2Int(x0,z0));
float step=Convert.ToSingle(region["step"]),gullyZ=Convert.ToSingle(region["gullyZ"]),tilt=Convert.ToSingle(region["gullyTilt"]),width=Convert.ToSingle(region["gullyWidth"]);
for(int z=0;z<h;z++)for(int x=0;x<w;x++){
float wx=origin.x+(x+x0)*data.size.x/(data.heightmapResolution-1),wz=origin.z+(z+z0)*data.size.z/(data.heightmapResolution-1);
float dx=(wx-c[0])/radius[0],dz=(wz-c[1])/radius[1],distance=Mathf.Sqrt(dx*dx+dz*dz);if(distance>=1)continue;
float height=values[z,x]*data.size.y+origin.y;var p=new Vector3(wx,height,wz);
float weight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1,distance));
float slope=data.GetSteepness((wx-origin.x)/data.size.x,(wz-origin.z)/data.size.z);weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(18,34,slope));
UnityEngine.AI.NavMeshHit hit;if(UnityEngine.AI.NavMesh.SamplePosition(p,out hit,22,-1))weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(10,22,hit.distance));
foreach(var id in ids){float d=Vector2.Distance(new Vector2(wx,wz),new Vector2(id.transform.position.x,id.transform.position.z));if(d<23)weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(16,23,d));}
foreach(var b in protectedObjects){float bx=Mathf.Max(b.min.x-wx,0,wx-b.max.x),bz=Mathf.Max(b.min.z-wz,0,wz-b.max.z);float d=Mathf.Sqrt(bx*bx+bz*bz);if(d<8)weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,8,d));}
if(weight<=0)continue;
// 절개 단을 연속 곡선으로 연결해 급격한 높이 경계를 피한다.
float phase=Mathf.Repeat(height,step)/step;float terraced=(Mathf.Floor(height/step)+Mathf.SmoothStep(0,1,phase))*step-height;
float ridge=Convert.ToSingle(region["bulge"])*Mathf.Sin((dz+.15f)*Mathf.PI)*Mathf.Exp(-dx*dx*3);
float channel=(wz-gullyZ-(wx-c[0])*tilt-3*Mathf.Sin(wx*.055f))/width;
float delta=terraced*Convert.ToSingle(region["terrace"])+ridge-Convert.ToSingle(region["gullyDepth"])*Mathf.Exp(-channel*channel);
values[z,x]+=delta*weight/data.size.y;count++;
}data.SetHeights(x0,z0,values);
int ax=Mathf.FloorToInt((c[0]-radius[0]-origin.x)/data.size.x*data.alphamapWidth),az=Mathf.FloorToInt((c[1]-radius[1]-origin.z)/data.size.z*data.alphamapHeight);
int aw=Mathf.CeilToInt(radius[0]*2/data.size.x*data.alphamapWidth)+2,ah=Mathf.CeilToInt(radius[1]*2/data.size.z*data.alphamapHeight)+2;
var a=data.GetAlphamaps(ax,az,aw,ah);paints.Add((float[,,])a.Clone());paintStarts.Add(new Vector2Int(ax,az));
for(int z=0;z<ah;z++)for(int x=0;x<aw;x++){
float wx=origin.x+(x+ax+.5f)/data.alphamapWidth*data.size.x,wz=origin.z+(z+az+.5f)/data.alphamapHeight*data.size.z;
float dx=(wx-c[0])/radius[0],dz=(wz-c[1])/radius[1],d=Mathf.Sqrt(dx*dx+dz*dz);if(d>=1)continue;
float slope=data.GetSteepness((wx-origin.x)/data.size.x,(wz-origin.z)/data.size.z);
float channel=Mathf.Abs(wz-gullyZ-(wx-c[0])*tilt-3*Mathf.Sin(wx*.055f))/width;
float rock=Mathf.Lerp(.15f,.8f,Mathf.InverseLerp(25,60,slope)),gravel=Mathf.Lerp(.5f,.16f,Mathf.InverseLerp(0,2,channel));
float dirt=1-rock;float[] weights={.02f,dirt*.6f,gravel,0,0,rock,0};float sum=weights.Sum();
float blend=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,1,d)))*.85f;
for(int l=0;l<a.GetLength(2);l++)a[z,x,l]=Mathf.Lerp(a[z,x,l],weights[l]/sum,blend);paintCount++;
}data.SetAlphamaps(ax,az,a);
}
for(int i=0;i<oldRocks.Length;i++){float delta=terrain.SampleHeight(oldRocks[i].position)+origin.y-oldGround[i];if(Mathf.Abs(delta)<.01f)continue;Undo.RecordObject(oldRocks[i],"기존 암반 지형 접합");oldRocks[i].position+=Vector3.up*delta;record(oldRocks[i]);}
var root=new GameObject("사면과 거점 후속 저작");root.transform.SetParent(env.transform,false);Undo.RegisterCreatedObjectUndo(root,"사면과 거점");
foreach(var f in (object[][])read(plan["rocks"].ToString(),typeof(object[][]))){
string asset="Assets/03.Prefabs/Environment/PoorVillage/Terrain_Boulder_"+f[1]+".prefab";
var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(asset),root.transform);Undo.RegisterCreatedObjectUndo(g,"연결 암반");g.name=f[0].ToString();
float x=Convert.ToSingle(f[2]),z=Convert.ToSingle(f[3]);var p=new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+origin.y,z);
var normal=data.GetInterpolatedNormal((x-origin.x)/data.size.x,(z-origin.z)/data.size.z);
g.transform.localScale=new Vector3(Convert.ToSingle(f[4]),Convert.ToSingle(f[5]),Convert.ToSingle(f[6]));g.transform.rotation=Quaternion.FromToRotation(Vector3.up,normal)*Quaternion.Euler(0,Convert.ToSingle(f[7]),0);g.transform.position=p;
var mesh=g.GetComponentInChildren<MeshFilter>();g.transform.position+=p-mesh.transform.TransformPoint(mesh.sharedMesh.bounds.center)-normal*3;
if(ids.Any(id=>bounds(g.transform).SqrDistance(id.transform.position)<25))throw new Exception("진행 개체 근접 "+g.name);
PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(g);
}
foreach(var f in (object[][])read(plan["moves"].ToString(),typeof(object[][]))){
var t=all.Single(a=>a.parent!=null&&a.parent.name.Contains(f[0].ToString())&&a.name.StartsWith(f[1].ToString())&&Vector2.Distance(new Vector2(a.position.x,a.position.z),new Vector2(Convert.ToSingle(f[2]),Convert.ToSingle(f[3])))<.1f);
if(t.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Length!=0)throw new Exception("진행 개체 이동 금지");Undo.RecordObject(t,"기존 생활 소품 방향과 접합");
var p=new Vector3(Convert.ToSingle(f[4]),0,Convert.ToSingle(f[5]));p.y=terrain.SampleHeight(p)+origin.y;t.SetPositionAndRotation(p,Quaternion.Euler(0,Convert.ToSingle(f[6]),0));t.position+=Vector3.up*(p.y-bounds(t).min.y-.025f);record(t);
}
foreach(var f in (object[][])read(plan["additions"].ToString(),typeof(object[][]))){
string asset=f[1].ToString()=="@cart"?"Assets/ExternalAssets/Environment/Fantastic Dungeon Pack/prefabs/PROPS/misc/P_PROP_cart_dungeon_02.prefab":"Assets/03.Prefabs/Environment/"+f[1]+".prefab";
var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(asset);if(prefab==null||prefab.GetComponentsInChildren<MonoBehaviour>(true).Length!=0)throw new Exception("환경 프리팹 확인 필요 "+asset);
var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform);Undo.RegisterCreatedObjectUndo(g,"거점 용도 보완");g.name=f[0].ToString();g.transform.localScale=Vector3.one*Convert.ToSingle(f[5]);g.transform.rotation=Quaternion.Euler(0,Convert.ToSingle(f[4]),0);
var p=new Vector3(Convert.ToSingle(f[2]),0,Convert.ToSingle(f[3]));p.y=terrain.SampleHeight(p)+origin.y;g.transform.position=p;g.transform.position+=Vector3.up*(p.y-bounds(g.transform).min.y-.025f);
if(ids.Any(id=>bounds(g.transform).SqrDistance(id.transform.position)<2.25f))throw new Exception("진행 개체 근접 "+g.name);
PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(g);
}
Physics.SyncTransforms();terrain.Flush();if(EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed)throw new Exception("저장 전 컴파일 상태 변경");
if(baseline!=string.Join("\n",ids.Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s).ToArray()))throw new Exception("진행 개체 변경");
System.IO.File.WriteAllLines(folder+"/moved-transforms.txt",moved);
EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");Undo.CollapseUndoOperations(undo);
string result="heightSamples="+count+" paintSamples="+paintCount+" moved="+moved.Count+" rocks=8 props=16";System.IO.File.WriteAllText(folder+"/applied.txt",result);return result;
}catch(Exception error){
System.IO.File.WriteAllText(folder+"/last-attempt.txt",error.ToString());
Undo.RevertAllDownToGroup(undo);for(int i=heights.Count-1;i>=0;i--)data.SetHeights(starts[i].x,starts[i].y,heights[i]);for(int i=paints.Count-1;i>=0;i--)data.SetAlphamaps(paintStarts[i].x,paintStarts[i].y,paints[i]);throw;
}
