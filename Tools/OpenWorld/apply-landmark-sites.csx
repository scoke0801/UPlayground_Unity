var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||scene.isDirty||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
var world=GameObject.Find("AuthoredEnvironment");if(world.transform.Find("랜드마크 유적 구역")!=null)throw new Exception("중복 적용 금지");
var folder="output/OpenWorld/LandmarkSites";System.IO.Directory.CreateDirectory(folder+"/backup");
var terrain=Terrain.activeTerrain;var data=terrain.terrainData;var origin=terrain.transform.position;
foreach(var p in new[]{scene.path,AssetDatabase.GetAssetPath(data)}){var dest=folder+"/backup/"+System.IO.Path.GetFileName(p);if(System.IO.File.Exists(dest))throw new Exception("백업 존재: 이전 적용 상태 확인 필요");System.IO.File.Copy(p,dest);}
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/landmark-sites-plan.json"),typeof(Dictionary<string,object>));
var sites=(Dictionary<string,object>[])read(plan["sites"].ToString(),typeof(Dictionary<string,object>[]));var rows=(Dictionary<string,object>[])read(plan["placements"].ToString(),typeof(Dictionary<string,object>[]));
var assets=rows.Select(r=>r["path"].ToString()).Distinct().ToDictionary(p=>p,p=>AssetDatabase.LoadAssetAtPath<GameObject>(p));
foreach(var kv in assets)if(kv.Value==null||kv.Value.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Length>0||kv.Value.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>m==null||m.shader==null||m.shader.name.Contains("InternalError")))||kv.Value.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new Exception("에셋 검사 실패 "+kv.Key);
Func<string> identities=()=>string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s));
var before=identities();System.IO.File.WriteAllText(folder+"/identities-before.txt",before);
var regions=new Dictionary<string,Transform>();var root=new GameObject("랜드마크 유적 구역").transform;root.SetParent(world.transform,false);
var heights=data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution);var original=(float[,])heights.Clone();float hx=data.size.x/(data.heightmapResolution-1),hz=data.size.z/(data.heightmapResolution-1);int foundations=0;
var report=new System.Text.StringBuilder();
try{
 foreach(var site in sites){var group=new GameObject(site["name"].ToString()).transform;group.SetParent(root,false);regions.Add(site["id"].ToString(),group);}
 foreach(var row in rows){
  var go=(GameObject)PrefabUtility.InstantiatePrefab(assets[row["path"].ToString()],regions[row["site"].ToString()]);go.name=row["id"]+"_"+go.name;
  float x=Convert.ToSingle(row["x"]),z=Convert.ToSingle(row["z"]);var p=new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+origin.y,z);
  go.transform.position=p;go.transform.rotation=Quaternion.Euler(0,Convert.ToSingle(row["yaw"]),0);go.transform.localScale=Vector3.one*Convert.ToSingle(row["scale"]);
  var rs=go.GetComponentsInChildren<MeshRenderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);go.transform.position+=Vector3.up*(p.y-b.min.y-.1f);
  if(row["kind"].ToString()=="structure"&&b.size.x*b.size.z>25){
   int x0=Mathf.Max(0,Mathf.FloorToInt((b.min.x-origin.x-6)/hx)),x1=Mathf.Min(data.heightmapResolution-1,Mathf.CeilToInt((b.max.x-origin.x+6)/hx));
   int z0=Mathf.Max(0,Mathf.FloorToInt((b.min.z-origin.z-6)/hz)),z1=Mathf.Min(data.heightmapResolution-1,Mathf.CeilToInt((b.max.z-origin.z+6)/hz));
   for(int iz=z0;iz<=z1;iz++)for(int ix=x0;ix<=x1;ix++){
    float wx=origin.x+ix*hx,wz=origin.z+iz*hz,dx=Mathf.Max(0,b.min.x-wx,wx-b.max.x),dz=Mathf.Max(0,b.min.z-wz,wz-b.max.z),weight=1-Mathf.SmoothStep(0,1,Mathf.Sqrt(dx*dx+dz*dz)/6);
    float target=(p.y-origin.y)/data.size.y;if(weight>0&&Mathf.Abs(target-original[iz,ix])*data.size.y*weight>5)throw new Exception("기초 절개 5m 초과: "+go.name);
    heights[iz,ix]=Mathf.Lerp(heights[iz,ix],target,weight);
   }foundations++;
  }
  PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
 }
 if(before!=identities())throw new Exception("진행 개체 변경");data.SetHeights(0,0,heights);
}catch{UnityEngine.Object.DestroyImmediate(root.gameObject);data.SetHeights(0,0,original);throw;}
// 이전 빈 공간 저작의 장식만 새 시설 안에서 걷어 내며, 기존 지역·진행 개체는 건드리지 않는다.
int cleared=0;var old=world.transform.Find("빈 공간 생활권");
if(old!=null)foreach(var t in old.GetComponentsInChildren<Transform>(true).Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).ToArray()){
 if(t.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Length>0)continue;
 var p=t.position;if(sites.Any(s=>Mathf.Abs(p.x-Convert.ToSingle(s["x"]))<Convert.ToSingle(s["w"])/2+5&&Mathf.Abs(p.z-Convert.ToSingle(s["z"]))<Convert.ToSingle(s["d"])/2+5)){UnityEngine.Object.DestroyImmediate(t.gameObject);cleared++;}
}
// 사각 프리미티브 바닥 대신 기존 지형에 마당과 연결 길을 혼합한다.
var lines=new List<float[][]>();var widths=new List<float>();var yards=new List<Dictionary<string,object>>();
foreach(var site in sites){foreach(var line in (Dictionary<string,object>[])read(site["paths"].ToString(),typeof(Dictionary<string,object>[]))){lines.Add((float[][])read(line["points"].ToString(),typeof(float[][])));widths.Add(Convert.ToSingle(line["width"]));}yards.AddRange((Dictionary<string,object>[])read(site["yards"].ToString(),typeof(Dictionary<string,object>[])));}
var map=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);float sx=data.size.x/data.alphamapWidth,sz=data.size.z/data.alphamapHeight;
for(int iz=0;iz<data.alphamapHeight;iz++)for(int ix=0;ix<data.alphamapWidth;ix++){
 var p=new Vector2(origin.x+(ix+.5f)*sx,origin.z+(iz+.5f)*sz);float weight=0;int layer=5;
 foreach(var yard in yards){float dx=(p.x-Convert.ToSingle(yard["x"]))/Convert.ToSingle(yard["rx"]),dz=(p.y-Convert.ToSingle(yard["z"]))/Convert.ToSingle(yard["rz"]);float w=Mathf.Clamp01((1-Mathf.Sqrt(dx*dx+dz*dz))*4)*.85f;if(w>weight){weight=w;layer=Convert.ToInt32(yard["layer"]);}}
 for(int q=0;q<lines.Count;q++)for(int k=1;k<lines[q].Length;k++){var a=new Vector2(lines[q][k-1][0],lines[q][k-1][1]);var b=new Vector2(lines[q][k][0],lines[q][k][1]);var d=b-a;float f=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);float w=Mathf.Clamp01((widths[q]*.5f+2-Vector2.Distance(p,a+d*f))/2)*.9f;if(w>weight){weight=w;layer=5;}}
 if(weight>0)for(int l=0;l<data.alphamapLayers;l++)map[iz,ix,l]=Mathf.Lerp(map[iz,ix,l],l==layer?1:0,weight);
}
data.SetAlphamaps(0,0,map);terrain.Flush();EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);Physics.SyncTransforms();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
report.AppendLine("시설="+sites.Length+" / 프리팹="+rows.Length+" / 건물 기초="+foundations+" / 이전 장식 정리="+cleared+" / 기존 ID·좌표 보존="+(before==identities()));System.IO.File.WriteAllText(folder+"/application.txt",report.ToString());return report.ToString();
