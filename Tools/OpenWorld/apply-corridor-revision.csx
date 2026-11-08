var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");
if(!scene.isLoaded||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed)throw new Exception("정상 컴파일된 월드 Edit Mode가 필요합니다.");
string folder="output/OpenWorld/DungeonWallRevision";
var source=GameObject.Find("AuthoredEnvironment/월드 구간/던전/옛 회랑/기존 지형과 소품").transform;
if(source.parent.Find("석조 회랑 개선")!=null)throw new Exception("적용 완료한 저작은 재실행하지 않습니다.");
var children=source.Cast<Transform>().ToArray();
Func<Transform,string> asset=t=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
var remove=children.Where(t=>asset(t).EndsWith("/Prop_StoneWall.prefab")||asset(t).EndsWith("/Castle_Gate.prefab")||asset(t).EndsWith("/Dng_Floor_4x4.prefab")&&t.GetComponent<MeshRenderer>().bounds.center.y>106).ToArray();
if(remove.Length!=191||remove.Any(t=>t.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Length>0))throw new Exception("교체 대상 191개와 진행 경계를 재확인해야 합니다.");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/corridor-revision-plan.json"),typeof(Dictionary<string,object>));
var paths=(Dictionary<string,string>)read(plan["assets"].ToString(),typeof(Dictionary<string,string>));
var rows=(Dictionary<string,object>[])read(plan["placements"].ToString(),typeof(Dictionary<string,object>[]));
var assets=paths.ToDictionary(p=>p.Key,p=>AssetDatabase.LoadAssetAtPath<GameObject>(p.Value));
foreach(var p in assets){if(p.Value==null||p.Value.GetComponentsInChildren<MonoBehaviour>(true).Length>0||p.Value.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh==null)||p.Value.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>m==null||m.shader==null||m.shader.name.Contains("InternalError"))))throw new Exception("원본 에셋 검증 실패: "+p.Key);}
Func<string> identities=()=>string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(v=>v));
var before=identities();
System.IO.Directory.CreateDirectory(folder+"/backup");
if(System.IO.File.Exists(folder+"/backup/LakeVillageWorld.before.unity"))throw new Exception("기존 백업을 덮어쓰지 않습니다.");
System.IO.File.Copy(scene.path,folder+"/backup/LakeVillageWorld.disk-before.unity");
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,folder+"/backup/LakeVillageWorld.before.unity",true))throw new Exception("미저장 상태를 포함한 백업 실패");
System.IO.File.WriteAllText(folder+"/identities-before.txt",before);
System.IO.File.WriteAllLines(folder+"/removed.txt",remove.Select(t=>t.name));
Func<GameObject,Bounds> bounds=g=>{var rs=g.GetComponentsInChildren<MeshRenderer>().Where(r=>r.gameObject.activeInHierarchy).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;};
Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("옛 회랑 석조 구조 교체");
try{
var root=new GameObject("석조 회랑 개선");root.transform.SetParent(source.parent,false);Undo.RegisterCreatedObjectUndo(root,"회랑 구조");
foreach(var row in rows){
string key=row["key"].ToString();var go=(GameObject)PrefabUtility.InstantiatePrefab(assets[key],root.transform);Undo.RegisterCreatedObjectUndo(go,"회랑 모듈");go.name=row["name"].ToString();
if(key=="gate")foreach(var t in go.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("MOD_Gateway_Door_01_large_"))){t.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}
if(row.ContainsKey("scale")){var v=(float[])read(row["scale"].ToString(),typeof(float[]));go.transform.localScale=new Vector3(v[0],v[1],v[2]);}
if(row.ContainsKey("height")){var b=bounds(go);go.transform.localScale=new Vector3(1,Convert.ToSingle(row["height"])/b.size.y,1);}
go.transform.rotation=Quaternion.Euler(0,Convert.ToSingle(row["yaw"]),0);
var p=(float[])read(row["position"].ToString(),typeof(float[]));var target=new Vector3(p[0],p[1],p[2]);var box=bounds(go);
go.transform.position+=new Vector3(target.x-box.center.x,target.y-box.min.y,target.z-box.center.z);
if(key=="roof")go.transform.position-=Vector3.up*.5f;
if(row.ContainsKey("decoration"))foreach(var c in go.GetComponentsInChildren<Collider>(true)){c.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(go);
}
if(before!=identities())throw new Exception("진행 ID·좌표 변경 감지");
foreach(var t in remove)Undo.DestroyObjectImmediate(t.gameObject);
Physics.SyncTransforms();
if(root.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new Exception("Missing Script 발생");
if(before!=identities())throw new Exception("교체 후 진행 ID·좌표 변경 감지");
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
Undo.CollapseUndoOperations(undo);
string result="돌담 180개·성문 4개·타일 천장 7개 교체 / 석조 모듈 "+rows.Length+"개 / 진행 ID·좌표 동일";System.IO.File.WriteAllText(folder+"/applied.txt",result);return result;
}catch{Undo.RevertAllDownToGroup(undo);throw;}
