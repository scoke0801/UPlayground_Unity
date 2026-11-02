var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||scene.isDirty||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
var folder="output/OpenWorld/EmptySpace";System.IO.Directory.CreateDirectory(folder+"/backup");
var terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>()).Single();var data=terrain.terrainData;var origin=terrain.transform.position;
var world=scene.GetRootGameObjects().First(g=>g.name=="AuthoredEnvironment");
if(world.transform.Find("빈 공간 생활권")!=null)throw new Exception("이미 적용됨. 결과와 씬을 먼저 확인하세요.");
foreach(var p in new[]{scene.path,AssetDatabase.GetAssetPath(data)}){var dest=folder+"/backup/"+System.IO.Path.GetFileName(p);if(System.IO.File.Exists(dest))throw new Exception("백업 존재: 중복 적용 확인 필요");System.IO.File.Copy(p,dest);}
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/empty-space-plan.json"),typeof(Dictionary<string,object>));
var rows=(Dictionary<string,object>[])read(plan["placements"].ToString(),typeof(Dictionary<string,object>[]));
var routes=(Dictionary<string,object>[])read(plan["routes"].ToString(),typeof(Dictionary<string,object>[]));
var patches=(Dictionary<string,object>[])read(plan["patches"].ToString(),typeof(Dictionary<string,object>[]));
var assets=rows.Select(r=>r["path"].ToString()).Distinct().ToDictionary(p=>p,p=>AssetDatabase.LoadAssetAtPath<GameObject>(p));
foreach(var kv in assets){if(kv.Value==null||kv.Value.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)||kv.Value.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>m==null||m.shader==null||m.shader.name.Contains("InternalError")))||kv.Value.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Length>0)throw new Exception("장식 에셋 검사 실패 "+kv.Key);}
Func<string> identities=()=>string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s));
var before=identities();System.IO.File.WriteAllText(folder+"/identities-before.txt",before);
var existing=world.GetComponentsInChildren<Transform>(true).Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).Select(t=>t.position).ToArray();
var root=new GameObject("빈 공간 생활권").transform;root.SetParent(world.transform,false);var groups=new Dictionary<string,Transform>();
var applied=new System.Text.StringBuilder("id,group,x,y,z,kind\n");int skipped=0,count=0;
var heights=data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution);var originalHeights=(float[,])heights.Clone();
float hx=data.size.x/(data.heightmapResolution-1),hz=data.size.z/(data.heightmapResolution-1);
try{
 foreach(var row in rows){
  string group=row["group"].ToString(),kind=row["kind"].ToString();float x=Convert.ToSingle(row["x"]),z=Convert.ToSingle(row["z"]),scale=Convert.ToSingle(row["scale"]);
  if(row.ContainsKey("maxSlope")&&data.GetSteepness((x-origin.x)/data.size.x,(z-origin.z)/data.size.z)>Convert.ToSingle(row["maxSlope"])){skipped++;continue;}
  if((kind=="tree"||kind=="rock")&&existing.Any(p=>new Vector2(p.x-x,p.z-z).sqrMagnitude<64)){skipped++;continue;}
  if(!groups.ContainsKey(group)){var g=new GameObject(group).transform;g.SetParent(root,false);groups.Add(group,g);}
  var go=(GameObject)PrefabUtility.InstantiatePrefab(assets[row["path"].ToString()],groups[group]);go.name=row["id"].ToString()+"_"+go.name;
  var position=new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+origin.y,z);go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,Convert.ToSingle(row["yaw"]),0);go.transform.localScale=Vector3.one*scale;
  var rs=go.GetComponentsInChildren<Renderer>(true);var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
  if(kind=="building"){
   int x0=Mathf.Max(0,Mathf.FloorToInt((bounds.min.x-origin.x-3)/hx)),x1=Mathf.Min(data.heightmapResolution-1,Mathf.CeilToInt((bounds.max.x-origin.x+3)/hx));
   int z0=Mathf.Max(0,Mathf.FloorToInt((bounds.min.z-origin.z-3)/hz)),z1=Mathf.Min(data.heightmapResolution-1,Mathf.CeilToInt((bounds.max.z-origin.z+3)/hz));
   for(int iz=z0;iz<=z1;iz++)for(int ix=x0;ix<=x1;ix++){
    float wx=origin.x+ix*hx,wz=origin.z+iz*hz,dx=Mathf.Max(0,bounds.min.x-wx,wx-bounds.max.x),dz=Mathf.Max(0,bounds.min.z-wz,wz-bounds.max.z);
    float weight=1-Mathf.SmoothStep(0,1,Mathf.Sqrt(dx*dx+dz*dz)/3);float target=(position.y-origin.y)/data.size.y;
    if(weight>0&&Mathf.Abs(target-originalHeights[iz,ix])*data.size.y>1.5f)throw new Exception("기초 정지량 초과 "+go.name);
    heights[iz,ix]=Mathf.Lerp(heights[iz,ix],target,weight);
   }
  }
  if(kind=="tree")go.transform.position+=Vector3.down*.12f;
  else {float low=position.y;if(kind=="rock")for(int k=0;k<4;k++){float px=k%2==0?bounds.min.x:bounds.max.x,pz=k<2?bounds.min.z:bounds.max.z;low=Mathf.Min(low,terrain.SampleHeight(new Vector3(px,0,pz))+origin.y);}go.transform.position+=Vector3.up*(low-bounds.min.y-(kind=="rock"?.18f:.06f));}
  // 작은 지면 소품은 지표 경사를 따라 기울여 사면에서 뜨는 모서리를 줄인다.
  if(kind=="prop"){var normal=data.GetInterpolatedNormal((x-origin.x)/data.size.x,(z-origin.z)/data.size.z);go.transform.rotation=Quaternion.FromToRotation(Vector3.up,normal)*go.transform.rotation;}
  PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);count++;
  applied.AppendLine(row["id"]+","+group+","+x+","+go.transform.position.y+","+z+","+kind);
 }
 if(before!=identities())throw new Exception("진행 ID·좌표 보존 실패");
 data.SetHeights(0,0,heights);
}catch{UnityEngine.Object.DestroyImmediate(root.gameObject);data.SetHeights(0,0,originalHeights);throw;}
// 기존 지표를 보존하며 새로운 보행 흔적과 마당·밭에만 색을 섞는다.
var lines=new List<Vector2[]>();foreach(var route in routes)lines.Add(((float[][])read(route["points"].ToString(),typeof(float[][]))).Select(p=>new Vector2(p[0],p[1])).ToArray());
float sx=data.size.x/data.alphamapWidth,sz=data.size.z/data.alphamapHeight;
var map=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);
for(int iz=0;iz<data.alphamapHeight;iz++)for(int ix=0;ix<data.alphamapWidth;ix++){
 var p=new Vector2(origin.x+(ix+.5f)*sx,origin.z+(iz+.5f)*sz);float weight=0;int layer=5;
 foreach(var line in lines)for(int k=1;k<line.Length;k++){var a=line[k-1];var d=line[k]-a;float f=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);float dist=Vector2.Distance(p,a+d*f);float w=Mathf.Clamp01((2.6f-dist)/1.3f)*.75f;if(w>weight){weight=w;layer=5;}}
 foreach(var patch in patches){float dx=(p.x-Convert.ToSingle(patch["x"]))/Convert.ToSingle(patch["rx"]),dz=(p.y-Convert.ToSingle(patch["z"]))/Convert.ToSingle(patch["rz"]);float w=Mathf.Clamp01((1-Mathf.Sqrt(dx*dx+dz*dz))*4)*Convert.ToSingle(patch["strength"]);if(w>weight){weight=w;layer=Convert.ToInt32(patch["layer"]);}}
 if(weight<=0)continue;for(int l=0;l<data.alphamapLayers;l++)map[iz,ix,l]=Mathf.Lerp(map[iz,ix,l],l==layer?1:0,weight);
}
data.SetAlphamaps(0,0,map);terrain.Flush();EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
System.IO.File.WriteAllText(folder+"/placements.csv",applied.ToString());System.IO.File.WriteAllText(folder+"/identities-after.txt",identities());
System.IO.File.WriteAllText(folder+"/application.txt","배치="+count+" / 근접·경사 제외="+skipped+" / 구역="+groups.Count+" / 진행 ID 보존="+(before==identities()));
return System.IO.File.ReadAllText(folder+"/application.txt");
