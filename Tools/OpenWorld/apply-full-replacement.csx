var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||scene.isDirty||scene.path!=UPlayGround.Editor.OpenWorldSliceAuthoring.HostPath)throw new Exception("저장된 대상 월드 Edit Mode 필요");
var folder="output/OpenWorld/FullReplacement";
if(!System.IO.File.Exists(folder+"/backup/LakeVillageWorld.unity"))throw new Exception("백업 필요");
var env=GameObject.Find("AuthoredEnvironment").transform;
if(env.Find("월드 구간")!=null)throw new Exception("이미 적용된 월드입니다.");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var rows=(Dictionary<string,object>[])read(System.IO.File.ReadAllText("Tools/OpenWorld/full-replacement-plan.json"),typeof(Dictionary<string,object>[]));
var assets=rows.Select(r=>r["prefab"].ToString()).Distinct().ToDictionary(p=>p,p=>AssetDatabase.LoadAssetAtPath<GameObject>(p));
foreach(var entry in assets){if(entry.Value==null||entry.Value.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh==null)||entry.Value.GetComponentsInChildren<MeshRenderer>(true).Any(r=>r.sharedMaterials.Any(m=>m==null)))throw new Exception("원본 메시·재질 확인 필요 "+entry.Key);}
var names=new[]{"StonegateCitadel_Environment","SaltwindHarbor_Environment","DroughtArt","VillageExtension","AuthoredDungeons","EnvironmentKitRefresh"};
var old=names.Select(n=>env.Find(n)).ToArray();
if(old.Any(t=>t==null))throw new Exception("교체 원본 계층 불일치");
if(old.Any(t=>t.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Length>0||t.GetComponentsInChildren<UPlayGround.GameActor>(true).Length>0))throw new Exception("진행 개체가 교체 대상에 포함됨");
Func<string> identities=()=>string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s));
string before=identities();System.IO.File.WriteAllText(folder+"/identities-before.txt",before);
var terrain=env.GetComponentInChildren<Terrain>();
var world=new GameObject("월드 구간").transform;world.SetParent(env,false);
var groups=new Dictionary<string,Transform>();
Func<string,Transform> group=null;
group=path=>{Transform found;if(groups.TryGetValue(path,out found))return found;int split=path.LastIndexOf('/');var parent=split<0?world:group(path.Substring(0,split));var result=new GameObject(split<0?path:path.Substring(split+1)).transform;result.SetParent(parent,false);groups.Add(path,result);return result;};
var created=new List<GameObject>();
try{
 foreach(var row in rows){
  var go=(GameObject)PrefabUtility.InstantiatePrefab(assets[row["prefab"].ToString()],group(row["group"].ToString()));
  created.Add(go);var p=(float[])read(row["position"].ToString(),typeof(float[]));var s=(float[])read(row["scale"].ToString(),typeof(float[]));
  var position=new Vector3(p[0],p[1],p[2]);if(Convert.ToBoolean(row["ground"]))position.y=terrain.SampleHeight(position)+terrain.transform.position.y;
  go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,Convert.ToSingle(row["yaw"]),0);go.transform.localScale=new Vector3(s[0],s[1],s[2]);
  var renderers=go.GetComponentsInChildren<MeshRenderer>(true);var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
  string align=row["align"].ToString();float delta=align=="top"?position.y-b.max.y:align=="road"?position.y+.05f-b.max.y:position.y-b.min.y-.03f;
  go.transform.position+=Vector3.up*delta;
  // 직사각 바닥은 장식 메시의 삼각형 충돌 틈에 의존하지 않는다.
  if(row["prefab"].ToString().EndsWith("/Dng_Floor_4x4.prefab")){
   foreach(var c in go.GetComponentsInChildren<MeshCollider>())UnityEngine.Object.DestroyImmediate(c);
   var box=go.AddComponent<BoxCollider>();var mesh=go.GetComponentInChildren<MeshFilter>().sharedMesh.bounds;box.center=mesh.center;box.size=mesh.size;
  }
  PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
 }
 if(before!=identities())throw new Exception("진행 ID 또는 좌표 변경");
}catch{UnityEngine.Object.DestroyImmediate(world.gameObject);throw;}
// 바다는 건축 프리팹이 아니므로 지형과 함께 보존한다.
var sea=old[1].Find("CoastalWater");if(sea!=null)sea.SetParent(group("항구/바다"),true);
foreach(var t in old)UnityEngine.Object.DestroyImmediate(t.gameObject);
Func<Vector3,string> region=p=>{
 if(p.x>=400&&p.x<=770&&p.z<300)return "돌등성";
 if(p.x>=1800&&p.z<0)return "항구";
 if(p.x>=900&&p.x<=1120&&p.z>=330&&p.z<=525)return "PoorVillage";
 if(p.x<380&&p.z>=490&&p.z<625)return "던전/묻힌 등불전";
 if(p.x>=550&&p.x<=630&&p.z>=815&&p.z<900)return "던전/사슬 제단";
 if(p.x>=1830&&p.x<=1970&&p.z>=830&&p.z<=970)return "던전/가시울 옛터";
 if(p.x>=570&&p.x<=730&&p.z>=475&&p.z<=625)return "던전/옛 회랑";
 if(p.x>1450&&p.z>350)return "그늘숲";
 if(p.x<750&&p.z>400)return "고갯길";
 if(p.z>625)return "호수와 사당";
 if(p.z>=400)return "붉은나무숲";
 return "교역로와 들판";
};
var common=new HashSet<string>(new[]{"SculptedTerrain","PlayBoundaries","NavigationPreferences"});
var units=new List<Transform>();var containers=new List<Transform>();
Action<Transform> collect=null;
collect=t=>{
 if(t==world||common.Contains(t.name)||t.GetComponent<Terrain>()!=null)return;
 bool container=t.GetComponents<Component>().Length==1&&!PrefabUtility.IsPartOfPrefabInstance(t.gameObject)&&t.childCount>0;
 if(container){containers.Add(t);foreach(Transform child in t)collect(child);}else units.Add(t);
};
foreach(var t in env.Cast<Transform>().ToArray())collect(t);
foreach(var t in units){
 var rs=t.GetComponentsInChildren<MeshRenderer>(true);var p=t.position;if(rs.Length>0){var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);p=b.center;}
 string category=t.GetComponentInChildren<LODGroup>(true)!=null?"식생":"기존 지형과 소품";
 t.SetParent(group(region(p)+"/"+category),true);
 if(PrefabUtility.IsPartOfPrefabInstance(t.gameObject))PrefabUtility.RecordPrefabInstancePropertyModifications(t);
}
for(int i=containers.Count-1;i>=0;i--)if(containers[i]!=null&&containers[i].childCount==0)UnityEngine.Object.DestroyImmediate(containers[i].gameObject);
if(before!=identities())throw new Exception("최종 진행 ID 또는 좌표 변경");
Physics.SyncTransforms();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
System.IO.File.WriteAllText(folder+"/identities-after.txt",identities());
System.IO.File.WriteAllLines(folder+"/application.txt",new[]{"신규 프리팹="+created.Count,"구간 및 하위 그룹="+groups.Count,"기존 환경 그룹핑="+units.Count,"진행 ID·좌표 보존=True","NavMesh 재생성 대기"});
return "전면 교체 "+created.Count+"개 / 그룹 "+groups.Count+"개 / 기존 환경 정리 "+units.Count+"개";
