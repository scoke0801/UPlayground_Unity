// 첫 화면에서 확인한 고립 암반과 상부 사면의 부족을 보정하는 일회성 추가 저작이다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string folder="output/OpenWorld/EnvironmentForm";
if(scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity"||scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed||System.IO.File.Exists(folder+"/refined.txt"))throw new Exception("보정 상태 불일치");
var env=GameObject.Find("AuthoredEnvironment");var terrain=env.GetComponentInChildren<Terrain>();var data=terrain.terrainData;var origin=terrain.transform.position;var root=GameObject.Find("AuthoredEnvironment/사면과 거점 후속 저작");
var ids=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).ToArray();
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/environment-form-plan.json"),typeof(Dictionary<string,object>));
int x0=375,z0=2526,w=365,h=502;var original=data.GetHeights(x0,z0,w,h);var heights=(float[,])original.Clone();int samples=0;
var oldRocks=GameObject.Find("AuthoredEnvironment/환경 후속 개선").transform.Cast<Transform>().Where(t=>t.position.z>1000).ToArray();var oldGround=oldRocks.Select(t=>terrain.SampleHeight(t.position)+origin.y).ToArray();
var nearIds=ids.Where(i=>i.transform.position.x<470&&i.transform.position.z>1000&&i.transform.position.z<1420).ToArray();
var scenery=env.GetComponentsInChildren<Transform>(true).Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)&&t.position.x<460&&t.position.x>210&&t.position.z>1020&&t.position.z<1400&&t.parent!=root.transform&&t.parent.name!="환경 후속 개선").ToArray();
var protectedBounds=scenery.SelectMany(t=>t.GetComponentsInChildren<MeshRenderer>(true)).Select(r=>r.bounds).ToArray();
for(int z=0;z<h;z++)for(int x=0;x<w;x++){
float wx=origin.x+(x+x0)*data.size.x/(data.heightmapResolution-1),wz=origin.z+(z+z0)*data.size.z/(data.heightmapResolution-1);
float dx=(wx-326)/105,dz=(wz-1198)/170,d=Mathf.Sqrt(dx*dx+dz*dz);if(d>=1)continue;
float height=heights[z,x]*data.size.y;float slope=data.GetSteepness(wx/data.size.x,(wz-origin.z)/data.size.z);float weight=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.6f,1,d)))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(16,30,slope));
UnityEngine.AI.NavMeshHit hit;if(UnityEngine.AI.NavMesh.SamplePosition(new Vector3(wx,height,wz),out hit,22,-1))weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(10,22,hit.distance));
foreach(var id in nearIds){float distance=Vector2.Distance(new Vector2(wx,wz),new Vector2(id.transform.position.x,id.transform.position.z));if(distance<23)weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(16,23,distance));}
foreach(var b in protectedBounds){float bx=Mathf.Max(b.min.x-wx,0,wx-b.max.x),bz=Mathf.Max(b.min.z-wz,0,wz-b.max.z);float distance=Mathf.Sqrt(bx*bx+bz*bz);if(distance<8)weight*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,8,distance));}
float phase=Mathf.Repeat(height,26)/26;float terraced=(Mathf.Floor(height/26)+.5f+.5f*(float)Math.Tanh((phase-.5f)*7))*26-height;
float channel=(wz-1208-(wx-326)*.62f)/15;float ridge=18*Mathf.Sin((dz+.2f)*Mathf.PI)*Mathf.Exp(-dx*dx*2);
float delta=(terraced*.9f+ridge-24*Mathf.Exp(-channel*channel))*weight;if(Mathf.Abs(delta)<.001f)continue;heights[z,x]+=delta/data.size.y;samples++;
}
Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("상부 절개와 암반 접합 보정");
try{
data.SetHeights(x0,z0,heights);
var moved=System.IO.File.ReadAllLines(folder+"/moved-transforms.txt").ToList();for(int i=0;i<oldRocks.Length;i++){float delta=terrain.SampleHeight(oldRocks[i].position)+origin.y-oldGround[i];if(Mathf.Abs(delta)<.01f)continue;Undo.RecordObject(oldRocks[i],"기존 암반 접지");oldRocks[i].position+=Vector3.up*delta;PrefabUtility.RecordPrefabInstancePropertyModifications(oldRocks[i]);moved.Add(GlobalObjectId.GetGlobalObjectIdSlow(PrefabUtility.GetPrefabInstanceHandle(oldRocks[i])).ToString());}System.IO.File.WriteAllLines(folder+"/moved-transforms.txt",moved.Distinct().ToArray());
foreach(var f in (object[][])read(plan["rocks"].ToString(),typeof(object[][]))){var t=root.transform.Find(f[0].ToString());Undo.RecordObject(t,"암반 접합");float x=Convert.ToSingle(f[2]),z=Convert.ToSingle(f[3]);var p=new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+origin.y,z);var normal=data.GetInterpolatedNormal(x/data.size.x,(z-origin.z)/data.size.z);t.localScale=new Vector3(Convert.ToSingle(f[4]),Convert.ToSingle(f[5]),Convert.ToSingle(f[6]));t.rotation=Quaternion.FromToRotation(Vector3.up,normal)*Quaternion.Euler(0,Convert.ToSingle(f[7]),0);t.position=p;var mesh=t.GetComponentInChildren<MeshFilter>();t.position+=p-mesh.transform.TransformPoint(mesh.sharedMesh.bounds.center)-normal*(z<1300?7:5);PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
Func<Transform,Bounds> bounds=t=>{var rs=t.GetComponentsInChildren<MeshRenderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;};
foreach(var f in (object[][])read(plan["additions"].ToString(),typeof(object[][]))){if(!f[0].ToString().Contains("병영침상"))continue;var t=root.transform.Find(f[0].ToString());Undo.RecordObject(t,"침상 공간 접합");var p=new Vector3(Convert.ToSingle(f[2]),0,Convert.ToSingle(f[3]));p.y=terrain.SampleHeight(p)+origin.y;t.position=p;t.position+=Vector3.up*(p.y-bounds(t).min.y-.025f);PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
var fort=GameObject.Find("AuthoredEnvironment/던전 공간 재저작/숲 보루의 전실과 병영 폐허").transform;
var shelf=fort.Cast<Transform>().Single(t=>t.name.StartsWith("shelf_")&&t.position.x>1850);Undo.RecordObject(shelf,"병영 선반 접지");var q=new Vector3(1871,0,628);q.y=terrain.SampleHeight(q)+origin.y;shelf.position=q;shelf.position+=Vector3.up*(q.y-bounds(shelf).min.y-.025f);PrefabUtility.RecordPrefabInstancePropertyModifications(shelf);
var barrel=fort.Cast<Transform>().Single(t=>t.name.StartsWith("barrel_")&&t.position.x>1850);Undo.RecordObject(barrel,"병영 물통 접합");q=new Vector3(1873,0,625);q.y=terrain.SampleHeight(q)+origin.y;barrel.position=q;barrel.position+=Vector3.up*(q.y-bounds(barrel).min.y-.025f);PrefabUtility.RecordPrefabInstancePropertyModifications(barrel);
Physics.SyncTransforms();terrain.Flush();EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("저장 실패");Undo.CollapseUndoOperations(undo);System.IO.File.WriteAllText(folder+"/refined.txt","heightSamples="+samples);return samples;
}catch{Undo.RevertAllDownToGroup(undo);data.SetHeights(x0,z0,original);throw;}
