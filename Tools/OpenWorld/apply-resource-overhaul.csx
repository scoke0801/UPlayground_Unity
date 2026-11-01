var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||scene.path!=UPlayGround.Editor.OpenWorldSliceAuthoring.HostPath)throw new Exception("대상 월드 Edit Mode 필요");
var folder="output/OpenWorld/ResourceOverhaul";
if(!System.IO.File.Exists(folder+"/backup/LakeVillageWorld.unity"))throw new Exception("작업 전 백업 필요");
var serializer=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=serializer.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(json,type)=>parse.Invoke(null,new object[]{json,type});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/resource-overhaul-plan.json"),typeof(Dictionary<string,object>));
var entries=(Dictionary<string,object>[])read(plan["units"].ToString(),typeof(Dictionary<string,object>[]));
string progressPath=folder+"/progress.txt";int start=System.IO.File.Exists(progressPath)?int.Parse(System.IO.File.ReadAllText(progressPath)):0;
if(start>=entries.Length)return "이미 적용 완료 "+start;
var env=GameObject.Find("AuthoredEnvironment").transform;var terrain=env.GetComponentInChildren<Terrain>();
if(start==0){
 if(scene.isDirty)throw new Exception("시작 전 씬 저장 필요");
 var roots=new HashSet<GameObject>();
 foreach(var entry in entries){
  var source=EditorUtility.InstanceIDToObject(Convert.ToInt32(entry["instanceId"])) as GameObject;
  if(source==null)throw new Exception("조사 이후 씬이 바뀌었습니다. 다시 조사하세요.");
  string target=entry["target"].ToString();
  if(target.Length>0){var asset=AssetDatabase.LoadAssetAtPath<GameObject>(target);if(asset==null||!asset.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh!=null)||asset.GetComponentsInChildren<MeshRenderer>(true).Any(r=>r.sharedMaterials.Any(m=>m==null)))throw new Exception("프리팹 메시·재질 누락 "+target);}
  var root=PrefabUtility.GetOutermostPrefabInstanceRoot(source);if(root!=null&&root!=source)roots.Add(root);
 }
 foreach(var root in roots)PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
}
Func<GameObject,Bounds> getBounds=go=>{
 bool first=true;var b=new Bounds();
 foreach(var f in go.GetComponentsInChildren<MeshFilter>(true)){
  if(f.sharedMesh==null)continue;var mesh=f.sharedMesh.bounds;var m=go.transform.worldToLocalMatrix*f.transform.localToWorldMatrix;
  for(int k=0;k<8;k++){var p=m.MultiplyPoint3x4(mesh.center+Vector3.Scale(mesh.extents,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1)));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}
 }if(first)throw new Exception("교체 메시 없음 "+go.name);return b;
};
var cache=new Dictionary<string,Bounds>();var changed=new Dictionary<int,GameObject>();var originals=new List<GameObject>();var log=new List<string>();
var water=AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/OpenWorld/ResourceOverhaul/Water.mat");
int end=Mathf.Min(entries.Length,start+1400);int current=start;
try{
 for(;current<end;current++){
  var entry=entries[current];var source=EditorUtility.InstanceIDToObject(Convert.ToInt32(entry["instanceId"])) as GameObject;
  if(source==null)throw new Exception("교체 원본 누락 "+entry["path"]);
  string action=entry["action"].ToString();
  if(action=="water"){
   if(water==null)throw new Exception("수면 재질 준비 필요");
   foreach(var r in source.GetComponentsInChildren<MeshRenderer>(true))r.sharedMaterials=Enumerable.Repeat(water,r.sharedMaterials.Length).ToArray();
   log.Add(current+" | water | "+entry["path"]);continue;
  }
  originals.Add(source);
  if(action=="delete"){log.Add(current+" | delete | "+entry["path"]);continue;}
  string target=entry["target"].ToString();string mode=entry["mode"].ToString();
  if(target.Length==0)throw new Exception("미분류 대상 "+entry["path"]);
  var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(target),source.transform.parent);
  go.name=source.name+"_R2";go.layer=source.layer;
  go.transform.localPosition=source.transform.localPosition;go.transform.localRotation=source.transform.localRotation;go.transform.localScale=Vector3.one;
  Bounds b;if(!cache.TryGetValue(target,out b)){b=getBounds(go);cache.Add(target,b);}
  var oldSize=(float[])read(entry["size"].ToString(),typeof(float[]));var oldCenter=(float[])read(entry["center"].ToString(),typeof(float[]));
  var size=new Vector3(oldSize[0],oldSize[1],oldSize[2]);var center=new Vector3(oldCenter[0],oldCenter[1],oldCenter[2]);
  var factor=new Vector3(Mathf.Max(.02f,size.x)/Mathf.Max(.02f,b.size.x),Mathf.Max(.04f,size.y)/Mathf.Max(.02f,b.size.y),Mathf.Max(.02f,size.z)/Mathf.Max(.02f,b.size.z));
  if(mode=="natural"||mode=="prop"||mode=="building"){
   float uniform=Mathf.Min(factor.x,factor.z,mode=="building"?Mathf.Max(factor.y,Mathf.Min(factor.x,factor.z)):factor.y);
   uniform=Mathf.Max(.03f,uniform);factor=Vector3.one*uniform;
   center.y=size.y*-.5f+center.y+b.size.y*uniform*.5f;
  }
  var offset=center-Vector3.Scale(b.center,factor);
  go.transform.localPosition=source.transform.localPosition+source.transform.localRotation*Vector3.Scale(source.transform.localScale,offset);
  go.transform.localScale=Vector3.Scale(source.transform.localScale,factor);
  if(mode=="building"){
   var rs=go.GetComponentsInChildren<MeshRenderer>();float bottom=rs.Min(r=>r.bounds.min.y);
   float ground=Mathf.Max(source.transform.position.y,terrain.SampleHeight(go.transform.position)+terrain.transform.position.y);
   go.transform.position+=Vector3.up*(ground-bottom-.03f);
  }
  if(Convert.ToInt32(entry["colliders"])==0)foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
  go.SetActive(source.activeSelf);PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(go);
  foreach(var t in source.GetComponentsInChildren<Transform>(true)){changed[t.gameObject.GetInstanceID()]=go;changed[t.GetInstanceID()]=go;}
  log.Add(current+" | replace | "+entry["path"]+" | "+target);
 }
 // 주민 일과의 전후 소품 참조를 새 인스턴스로 옮긴 뒤 원본을 삭제한다.
 if(changed.Count>0)foreach(var root in scene.GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true)){
  if(c==null||c.transform.IsChildOf(env))continue;
  var so=new SerializedObject(c);var p=so.GetIterator();bool modified=false;
  while(p.Next(true)){
   if(p.propertyType!=SerializedPropertyType.ObjectReference||p.objectReferenceValue==null)continue;
   GameObject replacement;if(!changed.TryGetValue(p.objectReferenceValue.GetInstanceID(),out replacement))continue;
   if(p.objectReferenceValue is GameObject)p.objectReferenceValue=replacement;
   else if(p.objectReferenceValue is Transform)p.objectReferenceValue=replacement.transform;
   else throw new Exception("참조 타입 수동 연결 필요 "+c.name+"."+p.propertyPath);
   modified=true;
  }if(modified)so.ApplyModifiedPropertiesWithoutUndo();
 }
 foreach(var source in originals)UnityEngine.Object.DestroyImmediate(source);
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
 System.IO.File.AppendAllLines(folder+"/application.txt",log);
 System.IO.File.WriteAllText(progressPath,end.ToString());
 return "적용 "+end+"/"+entries.Length;
}catch(Exception ex){System.IO.File.WriteAllText(folder+"/failure.txt","start="+start+" current="+current+"\n"+ex);throw;}

