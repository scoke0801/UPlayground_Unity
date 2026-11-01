var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||scene.isDirty||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
var folder="output/OpenWorld/HarborArtRevision";
if(!System.IO.File.Exists(folder+"/backup/LakeVillageWorld.unity"))throw new Exception("작업 전 백업 필요");
var world=GameObject.Find("AuthoredEnvironment").transform.Find("월드 구간");var previous=world.Find("항구");
if(previous.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Length>0)throw new Exception("진행 개체 분리 필요");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var rows=(Dictionary<string,object>[])read(System.IO.File.ReadAllText("Tools/OpenWorld/harbor-revision-plan.json"),typeof(Dictionary<string,object>[]));
var assets=rows.Select(r=>r["path"].ToString()).Distinct().ToDictionary(p=>p,p=>AssetDatabase.LoadAssetAtPath<GameObject>(p));
foreach(var entry in assets)if(entry.Value==null||entry.Value.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>m==null))||entry.Value.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new Exception("에셋 오류 "+entry.Key);
Func<string> identities=()=>string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s));
var before=identities();System.IO.File.WriteAllText(folder+"/identities-before.txt",before);
var root=new GameObject("항구_작업중").transform;root.SetParent(world,false);
var groups=new Dictionary<string,Transform>();Func<string,Transform> group=null;
group=path=>{Transform result;if(groups.TryGetValue(path,out result))return result;int k=path.LastIndexOf('/');result=new GameObject(k<0?path:path.Substring(k+1)).transform;result.SetParent(k<0?root:group(path.Substring(0,k)),false);groups.Add(path,result);return result;};
var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>();
try{
 foreach(var row in rows){
  var go=(GameObject)PrefabUtility.InstantiatePrefab(assets[row["path"].ToString()],group(row["group"].ToString()));
  var p=(float[])read(row["position"].ToString(),typeof(float[]));var s=(float[])read(row["scale"].ToString(),typeof(float[]));
  var position=new Vector3(p[0],p[1],p[2]);if(row["mode"].ToString()=="ground")position.y=terrain.SampleHeight(position)+terrain.transform.position.y;
  go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,Convert.ToSingle(row["yaw"]),0);go.transform.localScale=new Vector3(s[0],s[1],s[2]);
  var rs=go.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
  string align=row["align"].ToString();if(align!="origin")go.transform.position+=Vector3.up*(position.y-(align=="top"?b.max.y:b.min.y));
  if(Convert.ToBoolean(row["decorative"]))foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
  PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
 }
 if(before!=identities())throw new Exception("진행 ID·좌표 변경");
}catch{UnityEngine.Object.DestroyImmediate(root.gameObject);throw;}
foreach(var name in new[]{"바다","기존 지형과 소품"}){var t=previous.Find(name);if(t!=null)t.SetParent(root,true);}
UnityEngine.Object.DestroyImmediate(previous.gameObject);root.name="항구";
Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
System.IO.File.WriteAllText(folder+"/identities-after.txt",identities());
System.IO.File.WriteAllText(folder+"/application.txt","배치="+rows.Length+" / 그룹="+groups.Count+" / 진행 ID·좌표 동일="+(before==identities()));
return "항구 전면 교체 "+rows.Length;
