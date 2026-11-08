// 현재 저장본에만 적용하는 일회성 증분 저작. 원본 프리팹과 기존 NavMesh는 보존한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
string folder="output/OpenWorld/EnvironmentForm";
if(scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed)throw new Exception("저장된 월드 Edit Mode와 정상 컴파일 필요");
if(System.IO.File.Exists(folder+"/escarpment.txt"))throw new Exception("재실행 금지");
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
var protectedObjects=all.Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&t.position.x>300&&t.position.x<540&&t.position.z>1070&&t.position.z<1490&&!oldRocks.Contains(t)&&t.parent.name!="사면과 거점 후속 저작"&&t.GetComponentsInChildren<MeshRenderer>(true).Length>0).Select(bounds).ToArray();
var heights=new List<float[,]>();var paints=new List<float[,,]>();var starts=new List<Vector2Int>();var paintStarts=new List<Vector2Int>();var moved=new List<string>();int count=0,paintCount=0;
Action<Transform> record=t=>{moved.Add(GlobalObjectId.GetGlobalObjectIdSlow(PrefabUtility.GetPrefabInstanceHandle(t)).ToString());PrefabUtility.RecordPrefabInstancePropertyModifications(t);};
var root=GameObject.Find("AuthoredEnvironment/사면과 거점 후속 저작");Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("넓은 절개 단과 침식 홈");try{
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
UnityEngine.AI.NavMeshHit hit;if(UnityEngine.AI.NavMesh.SamplePosition(p,out hit,22,1<<UnityEngine.AI.NavMesh.GetAreaFromName("Walkable")))weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(10,22,hit.distance));
foreach(var id in ids){float d=Vector2.Distance(new Vector2(wx,wz),new Vector2(id.transform.position.x,id.transform.position.z));if(d<23)weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(16,23,d));}
foreach(var b in protectedObjects){float bx=Mathf.Max(b.min.x-wx,0,wx-b.max.x),bz=Mathf.Max(b.min.z-wz,0,wz-b.max.z);float d=Mathf.Sqrt(bx*bx+bz*bz);if(d<8)weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,8,d));}
if(weight<=0)continue;
// 절개 단을 연속 곡선으로 연결해 급격한 높이 경계를 피한다.
float phase=Mathf.Repeat(height,step)/step;float terraced=(Mathf.Floor(height/step)+(.5f+.5f*(float)Math.Tanh((phase-.5f)*7)))*step-height;
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
foreach(var f in (object[][])read(plan["rocks"].ToString(),typeof(object[][]))){var t=root.transform.Find(f[0].ToString());Undo.RecordObject(t,"암반 접합");float x=Convert.ToSingle(f[2]),z=Convert.ToSingle(f[3]);var p=new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+origin.y,z);var normal=data.GetInterpolatedNormal(x/data.size.x,(z-origin.z)/data.size.z);t.localScale=new Vector3(Convert.ToSingle(f[4]),Convert.ToSingle(f[5]),Convert.ToSingle(f[6]));t.rotation=Quaternion.FromToRotation(Vector3.up,normal)*Quaternion.Euler(0,Convert.ToSingle(f[7]),0);t.position=p;var mesh=t.GetComponentInChildren<MeshFilter>();t.position+=p-mesh.transform.TransformPoint(mesh.sharedMesh.bounds.center)-normal*(z<1300?7:5);PrefabUtility.RecordPrefabInstancePropertyModifications(t);}

for(int i=0;i<oldRocks.Length;i++){float delta=terrain.SampleHeight(oldRocks[i].position)+origin.y-oldGround[i];if(Mathf.Abs(delta)<.01f)continue;Undo.RecordObject(oldRocks[i],"기존 암반 접합");oldRocks[i].position+=Vector3.up*delta;record(oldRocks[i]);}
foreach(var name in new[]{"채석장_하부파편","채석장_파편편석","채석장_고립전석"}){
var t=GameObject.Find("AuthoredEnvironment/환경 후속 개선/"+name).transform;Undo.RecordObject(t,"분리 파편 암반 연결");float z=name=="채석장_하부파편"?1198:name=="채석장_파편편석"?1184:1210;var p=new Vector3(386,terrain.SampleHeight(new Vector3(386,0,z))+origin.y,z);var normal=data.GetInterpolatedNormal(386/data.size.x,(z-origin.z)/data.size.z);t.position=p;var mesh=t.GetComponentInChildren<MeshFilter>();t.position+=p-mesh.transform.TransformPoint(mesh.sharedMesh.bounds.center)-normal*1.5f;record(t);
}
Physics.SyncTransforms();terrain.Flush();EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("저장 실패");
System.IO.File.WriteAllLines(folder+"/moved-transforms.txt",System.IO.File.ReadAllLines(folder+"/moved-transforms.txt").Concat(moved).Distinct().ToArray());Undo.CollapseUndoOperations(undo);System.IO.File.WriteAllText(folder+"/escarpment.txt","heightSamples="+count+" paintSamples="+paintCount);return count;
}catch(Exception e){Undo.RevertAllDownToGroup(undo);for(int i=heights.Count-1;i>=0;i--)data.SetHeights(starts[i].x,starts[i].y,heights[i]);for(int i=paints.Count-1;i>=0;i--)data.SetAlphamaps(paintStarts[i].x,paintStarts[i].y,paints[i]);System.IO.File.WriteAllText(folder+"/escarpment-error.txt",e.ToString());throw;}
